using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace GFC_ComBadge
{
    internal class Program
    {
        static string ClientId = "";
        static string ClientSecret = "";
        static string VrcHost = "127.0.0.1";
        static int VrcInputPort;
        static int VrcOutputPort;
        static string BadgeOscAddress = "";
        static bool DefaultMuted;

        static readonly string[] DiscordScopes = ["rpc", "identify"];
        static readonly TimeSpan discordRetryDelay = TimeSpan.FromSeconds(2);
        static readonly TimeSpan discordRefreshInterval = TimeSpan.FromSeconds(30);
        static readonly TimeSpan vrcRetryDelay = TimeSpan.FromSeconds(2);

        static MuteState state = null!;
        static TokenResponse? token = null;

        static readonly object vrcLock = new();
        static VrcOscBridge? vrc = null;

        static readonly AutoResetEvent stateChangedEvent = new(false);

        static async Task Main(string[] args)
        {
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string[] resourceNames = assembly.GetManifestResourceNames();

                string resourceName = "GFC_Combadge.appsettings.json";

                using Stream? stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    throw new FileNotFoundException($"Could not find embedded configuration resource: {resourceName}");
                }

                var config = new ConfigurationBuilder().AddJsonStream(stream).Build();

                ClientId = config["Discord:ClientId"] ?? throw new Exception("Missing Discord:ClientId");
                ClientSecret = config["Discord:ClientSecret"] ?? throw new Exception("Missing Discord:ClientSecret");
                VrcHost = config["VrcOsc:Host"] ?? "127.0.0.1";
                VrcInputPort = int.Parse(config["VrcOsc:InputPort"] ?? "9000");
                VrcOutputPort = int.Parse(config["VrcOsc:OutputPort"] ?? "9001");
                BadgeOscAddress = config["VrcOsc:BadgeAddress"] ?? "ComBadgePressed";
                DefaultMuted = bool.Parse(config["VrcOsc:DefaultMuted"] ?? "true");

                state = new MuteState(DefaultMuted);

                token = TokenStorage.Load();
                if (token != null)
                {
                    Console.WriteLine("Loaded cached authorization tokens from AppData.");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Configuration Error: {ex.Message}");
                return;
            }

            Console.WriteLine("Combadge bridge booting...");
            var discordTask = RunDiscordLoopAsync(cts.Token);
            var vrcTask = RunVrcLoopAsync(cts.Token);

            await Task.WhenAll(discordTask, vrcTask);
        }

        static async Task RunDiscordLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine("Connecting to Discord IPC...");
                    var connectedDiscord = await DiscordRpcClient.ConnectAsync(ClientId, cancellationToken);
                    await using var ownedDiscord = connectedDiscord;

                    Console.WriteLine("Discord IPC connected.");
                    token = await EnsureUsableTokenAsync(ownedDiscord, token, cancellationToken);

                    var auth = await ownedDiscord.AuthenticateAsync(token.AccessToken, cancellationToken);
                    LogAuthentication(auth);

                    long appliedVersion = -1;
                    var lastRefresh = DateTimeOffset.MinValue;

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var snapshot = state.Get();
                        var needsApply = appliedVersion != snapshot.Version;
                        var needsRefresh = DateTimeOffset.UtcNow - lastRefresh >= discordRefreshInterval;

                        if (needsApply || needsRefresh)
                        {
                            await ownedDiscord.SetMuteAsync(snapshot.Muted, cancellationToken);
                            appliedVersion = snapshot.Version;
                            lastRefresh = DateTimeOffset.UtcNow;

                            Console.WriteLine(snapshot.Muted ? "Discord state applied: muted." : "Discord state applied: unmuted.");
                        }

                        var dynamicTimeout = discordRefreshInterval - (DateTimeOffset.UtcNow - lastRefresh);
                        if (dynamicTimeout < TimeSpan.Zero) dynamicTimeout = TimeSpan.Zero;

                        await Task.Run(() => WaitHandle.WaitAny([stateChangedEvent, cancellationToken.WaitHandle], dynamicTimeout), cancellationToken);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return; }
                catch (Exception ex)
                {
                    Console.WriteLine($"Discord loop failed: {ex.Message}");
                    await SafeDelayAsync(discordRetryDelay, cancellationToken);
                }
            }
        }

        static async Task RunVrcLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine("Starting VRChat OSC bridge...");

                    lock (vrcLock)
                    {
                        vrc?.Dispose();
                        vrc = new VrcOscBridge(VrcHost, VrcInputPort, VrcOutputPort, BadgeOscAddress);
                    }

                    Console.WriteLine("VRChat OSC bridge online.");

                    VrcOscBridge? currentVrc;
                    lock (vrcLock) { currentVrc = vrc; }

                    if (currentVrc != null)
                    {
                        await currentVrc.SendChatboxAsync("ComBadge Bridge Connected!", true, false);
                        await currentVrc.ReceiveAsync(OnVrcMuteReceived, cancellationToken);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return; }
                catch (Exception ex)
                {
                    Console.WriteLine($"VRChat OSC loop failed: {ex.Message}");
                    await SafeDelayAsync(vrcRetryDelay, cancellationToken);
                }
            }
        }

        static void OnVrcMuteReceived(bool vrcState)
        {
            var changed = state.Set(vrcState, "VRChat OSC (Strict Mode)");

            if (changed)
            {
                Console.WriteLine(vrcState
                    ? "VRChat explicitly requested: MUTED."
                    : "VRChat explicitly requested: UNMUTED.");

                stateChangedEvent.Set();
            }
        }

        static async Task<TokenResponse> EnsureUsableTokenAsync(DiscordRpcClient discord, TokenResponse? currentToken, CancellationToken cancellationToken)
        {
            if (currentToken is not null && !currentToken.IsExpiredSoon) return currentToken;

            if (currentToken?.RefreshToken is not null)
            {
                try
                {
                    Console.WriteLine("Refreshing expired access token...");
                    var refreshedToken = await DiscordOAuth.RefreshTokenAsync(ClientId, ClientSecret, currentToken.RefreshToken, cancellationToken);

                    TokenStorage.Save(refreshedToken);
                    return refreshedToken;
                }
                catch { }
            }

            Console.WriteLine("Prompting user authorization...");
            var code = await discord.AuthorizeAsync(ClientId, DiscordScopes, cancellationToken);
            var fullyAuthorizedToken = await DiscordOAuth.ExchangeCodeAsync(ClientId, ClientSecret, code, cancellationToken);

            TokenStorage.Save(fullyAuthorizedToken);
            return fullyAuthorizedToken;
        }

        static void LogAuthentication(AuthenticateData auth)
        {
            var name = auth.User.GlobalName ?? auth.User.Username;
            Console.WriteLine($"User: {name} ({auth.User.Id}) | App: {auth.Application.Name}");
        }

        static async Task SafeDelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            try { await Task.Delay(delay, cancellationToken); } catch (OperationCanceledException) { }
        }
    }
}
