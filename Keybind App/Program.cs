using Microsoft.Extensions.Configuration;
using System.Reflection;
using WindowsInput;
using WindowsInput.Native;

namespace GFC_ComBadge_Keybind
{
    internal class Program
    {
        static string VrcHost = "127.0.0.1";
        static int VrcInputPort;
        static int VrcOutputPort;
        static string BadgeOscAddress = "";
        static bool DefaultMuted;

        static readonly TimeSpan vrcRetryDelay = TimeSpan.FromSeconds(2);
        static MuteState state = null!;

        static readonly object vrcLock = new();
        static VrcOscBridge? vrc = null;

        static readonly InputSimulator InputSim = new();

        static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GFC_ComBadge");
        static readonly string SetupFilePath = Path.Combine(AppDataFolder, "setup.dat");

        static async Task Main(string[] args)
        {
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = "GFC_Combadge_Keybind.appsettings.json";

                using Stream? stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    throw new FileNotFoundException($"Could not find embedded configuration resource: {resourceName}");
                }

                var config = new ConfigurationBuilder().AddJsonStream(stream).Build();

                VrcHost = config["VrcOsc:Host"] ?? "127.0.0.1";
                VrcInputPort = int.Parse(config["VrcOsc:InputPort"] ?? "9000");
                VrcOutputPort = int.Parse(config["VrcOsc:OutputPort"] ?? "9001");
                BadgeOscAddress = config["VrcOsc:BadgeAddress"] ?? "ComBadgeMuted";
                DefaultMuted = bool.Parse(config["VrcOsc:DefaultMuted"] ?? "true");

                state = new MuteState(DefaultMuted);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Configuration Error: {ex.Message}");
                return;
            }

            if (!File.Exists(SetupFilePath))
            {
                await Setup.RunSetupAsync();

                try
                {
                    Directory.CreateDirectory(AppDataFolder);
                    await File.WriteAllTextAsync(SetupFilePath, "Setup completed on: " + DateTime.Now.ToString());
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Warning: Could not save setup marker: {ex.Message}");
                }
            }

            Console.Clear();
            Console.WriteLine("Combadge Simulator Bridge booting (Global Keypress Mode)...");
            Console.WriteLine("F23 = Deafen (Startup only) | F24 = Mute (Live) - Active.");

            if (DefaultMuted)
            {
                Console.WriteLine("Initial state set to MUTED. Executing startup synchronization...");

                InputSim.Keyboard.KeyPress(VirtualKeyCode.F23);
                await Task.Delay(100);
                InputSim.Keyboard.KeyPress(VirtualKeyCode.F24);
                await Task.Delay(100);
                InputSim.Keyboard.KeyPress(VirtualKeyCode.F24);
            }

            await RunVrcLoopAsync(cts.Token);
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
                        await currentVrc.SendChatboxAsync("ComBadge Hotkey Bridge Online!", true, false);
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

        static async void OnVrcMuteReceived(bool vrcState)
        {
            state.Set(vrcState, "VRChat OSC");

            if (vrcState)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] OSC -> MUTED. Forcing Discord state...");

                InputSim.Keyboard.KeyPress(VirtualKeyCode.F23);
                await Task.Delay(150);

                InputSim.Keyboard.KeyPress(VirtualKeyCode.F23);
                await Task.Delay(100);
                InputSim.Keyboard.KeyPress(VirtualKeyCode.F24);
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] OSC -> UNMUTED. Forcing Discord state...");

                InputSim.Keyboard.KeyPress(VirtualKeyCode.F23);
                await Task.Delay(150);

                InputSim.Keyboard.KeyPress(VirtualKeyCode.F24);
            }
        }

        static async Task SafeDelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            try { await Task.Delay(delay, cancellationToken); } catch (OperationCanceledException) { }
        }
    }
}