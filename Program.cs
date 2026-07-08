using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace GFC_ComBadge
{
    internal class Program
    {
        // Configuration variables populated from JSON
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

        static MuteState state = null!; // Initialized dynamically in Main
        static TokenResponse? token = null;
        static VrcOscBridge? vrc = null;

        static async Task Main(string[] args)
        {
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            // Load configuration from appsettings.json
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                // Lista svih resursa koji su uspešno spakovani u .exe
                string[] resourceNames = assembly.GetManifestResourceNames();

                string resourceName = "GFC_Combadge.appsettings.json";

                using Stream? stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    Console.Error.WriteLine("--- EMBEDDED RESOURCES FOUND IN THIS EXE ---");
                    foreach (var name in resourceNames)
                    {
                        Console.Error.WriteLine($"-> {name}");
                    }
                    Console.Error.WriteLine("--------------------------------------------\n");

                    throw new FileNotFoundException($"Could not find embedded configuration resource: {resourceName}");
                }

                var config = new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();

                ClientId = config["Discord:ClientId"] ?? throw new Exception("Missing Discord:ClientId");
                ClientSecret = config["Discord:ClientSecret"] ?? throw new Exception("Missing Discord:ClientSecret");
                VrcHost = config["VrcOsc:Host"] ?? "127.0.0.1";
                VrcInputPort = int.Parse(config["VrcOsc:InputPort"] ?? "9000");
                VrcOutputPort = int.Parse(config["VrcOsc:OutputPort"] ?? "9001");
                BadgeOscAddress = config["VrcOsc:BadgeAddress"] ?? "ComBadgePressed";
                DefaultMuted = bool.Parse(config["VrcOsc:DefaultMuted"] ?? "true");

                state = new MuteState(DefaultMuted);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Configuration Error: {ex.Message}");
                return;
            }

            Console.WriteLine("Combadge bridge booting...");
            Console.WriteLine($"Parameter: {BadgeOscAddress}");
            Console.WriteLine($"VRChat OSC: listen {VrcOutputPort}, send {VrcInputPort}");
            Console.WriteLine($"Startup default: muted = {DefaultMuted}\n");

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
                        await Task.Delay(1000, cancellationToken);
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
                    vrc?.Dispose();
                    vrc = new VrcOscBridge(VrcHost, VrcInputPort, VrcOutputPort, BadgeOscAddress);

                    Console.WriteLine("VRChat OSC bridge online.");

                    await vrc.SendChatboxAsync("ComBadge Bridge Connected!", true, false);
                    await vrc.ReceiveAsync(OnVrcMuteReceived, cancellationToken);
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
            }
        }

        static async Task<TokenResponse> EnsureUsableTokenAsync(DiscordRpcClient discord, TokenResponse? currentToken, CancellationToken cancellationToken)
        {
            if (currentToken is not null && !currentToken.IsExpiredSoon) return currentToken;

            if (currentToken?.RefreshToken is not null)
            {
                try
                {
                    return await DiscordOAuth.RefreshTokenAsync(ClientId, ClientSecret, currentToken.RefreshToken, cancellationToken);
                }
                catch { /* fallback to authorize if refresh token fails */ }
            }

            var code = await discord.AuthorizeAsync(ClientId, DiscordScopes, cancellationToken);
            return await DiscordOAuth.ExchangeCodeAsync(ClientId, ClientSecret, code, cancellationToken);
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