using System.Net;
using LucHeart.CoreOSC;

namespace GFC_ComBadge_Integration
{
    /// <summary>
    /// Bridges VRChat avatar parameter OSC traffic to strongly typed mute state updates.
    /// </summary>
    public sealed class VrcOscBridge : IDisposable
    {
        private readonly string parameterAddress;
        private readonly OscSender sender;
        private readonly OscListener listener;

        public VrcOscBridge(string host, int vrcInputPort, int localListenPort, string parameterAddress)
        {
            this.parameterAddress = parameterAddress;
            sender = new OscSender(new IPEndPoint(IPAddress.Parse(host), vrcInputPort));
            listener = new OscListener(new IPEndPoint(IPAddress.Any, localListenPort))
            {
                EnableTransparentBundleToMessageConversion = true
            };
        }

        public async Task ReceiveAsync(Action<bool> onMuteReceived, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var message = await listener.ReceiveMessageAsync(cancellationToken);

                    if (message.Address == null || !message.Address.EndsWith(parameterAddress, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (message.Arguments.Length == 0)
                    {
                        continue;
                    }

                    var value = message.Arguments[0];

                    if (!TryCoerceBool(value, out var muted))
                    {
                        continue;
                    }

                    onMuteReceived(muted);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"OSC callback error: {ex.Message}");
                }
            }
        }

        public Task SendMuteAsync(bool muted)
        {
            return sender.SendAsync(new OscMessage(this.parameterAddress, muted));
        }

        public Task SendChatboxAsync(string text, bool sendInstant, bool playAudio)
        {
            return sender.SendAsync(new OscMessage("/chatbox/input", text, sendInstant, playAudio));
        }

        public void Dispose()
        {
            listener.Dispose();
            sender.Dispose();
        }

        private static bool TryCoerceBool(object? value, out bool result)
        {
            switch (value)
            {
                case bool b: result = b; return true;
                case int i: result = i != 0; return true;
                case long l: result = l != 0; return true;
                case float f: result = f > 0.5f; return true;
                case double d: result = d > 0.5d; return true;
                case string s when bool.TryParse(s, out var parsed): result = parsed; return true;
                default: result = false; return false;
            }
        }
    }
}