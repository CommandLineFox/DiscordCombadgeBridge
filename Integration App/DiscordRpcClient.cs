using System.Buffers.Binary;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace GFC_ComBadge_Integration
{
    public sealed class DiscordRpcClient : IAsyncDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly NamedPipeClientStream pipe;

        private DiscordRpcClient(NamedPipeClientStream pipe)
        {
            this.pipe = pipe;
        }

        public static async Task<DiscordRpcClient> ConnectAsync(string clientId, CancellationToken cancellationToken)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("This application uses Windows named pipes for Discord IPC.");

            Exception? lastException = null;

            for (var i = 0; i < 10; i++)
            {
                NamedPipeClientStream? pipe = null;
                try
                {
                    pipe = new NamedPipeClientStream(".", $"discord-ipc-{i}", PipeDirection.InOut, PipeOptions.Asynchronous);
                    await pipe.ConnectAsync(500, cancellationToken);

                    var client = new DiscordRpcClient(pipe);
                    await client.SendAsync(0, new { v = 1, client_id = clientId }, cancellationToken);

                    var ready = await client.ReadAsync(cancellationToken);
                    Console.WriteLine($"Discord READY: {TrimForLog(ready)}");

                    return client;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    pipe?.Dispose();
                    throw;
                }
                catch (Exception ex)
                {
                    pipe?.Dispose();
                    lastException = ex;
                }
            }

            throw new InvalidOperationException("Could not connect to Discord IPC. Is Discord running?", lastException);
        }

        public async Task<string> AuthorizeAsync(string clientId, string[] scopes, CancellationToken cancellationToken)
        {
            using var response = await SendCommandAsync("AUTHORIZE", new { client_id = clientId, scopes }, cancellationToken);
            var code = response.RootElement.GetProperty("data").GetProperty("code").GetString();
            return code ?? throw new InvalidOperationException("Discord did not return an auth code.");
        }

        public async Task<AuthenticateData> AuthenticateAsync(string accessToken, CancellationToken cancellationToken)
        {
            using var response = await SendCommandAsync("AUTHENTICATE", new { access_token = accessToken }, cancellationToken);
            var data = response.RootElement.GetProperty("data");
            return data.Deserialize<AuthenticateData>(JsonOptions) ?? throw new InvalidOperationException("Could not parse authenticate response.");
        }

        public async Task SetMuteAsync(bool mute, CancellationToken cancellationToken)
        {
            using var _ = await SendCommandAsync("SET_VOICE_SETTINGS", new { mute }, cancellationToken);
        }

        private async Task<JsonDocument> SendCommandAsync(string cmd, object args, CancellationToken cancellationToken)
        {
            var nonce = Guid.NewGuid().ToString();
            await SendAsync(1, new { nonce, cmd, args }, cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                var response = await ReadAsync(cancellationToken);
                var doc = JsonDocument.Parse(response);

                if (!doc.RootElement.TryGetProperty("nonce", out var nonceProperty) || nonceProperty.GetString() != nonce)
                {
                    doc.Dispose();
                    continue;
                }

                if (doc.RootElement.TryGetProperty("evt", out var evt) && string.Equals(evt.GetString(), "ERROR", StringComparison.OrdinalIgnoreCase))
                {
                    var error = doc.RootElement.ToString();
                    doc.Dispose();
                    throw new InvalidOperationException($"Discord RPC error: {error}");
                }

                return doc;
            }
            throw new OperationCanceledException(cancellationToken);
        }

        private async Task SendAsync(int opcode, object payload, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(payload);
            var body = Encoding.UTF8.GetBytes(json);

            Span<byte> header = stackalloc byte[8];
            BinaryPrimitives.WriteInt32LittleEndian(header[..4], opcode);
            BinaryPrimitives.WriteInt32LittleEndian(header[4..], body.Length);

            await this.pipe.WriteAsync(header.ToArray(), cancellationToken);
            await this.pipe.WriteAsync(body, cancellationToken);
            await this.pipe.FlushAsync(cancellationToken);
        }

        private async Task<string> ReadAsync(CancellationToken cancellationToken)
        {
            var header = new byte[8];
            await this.pipe.ReadExactlyAsync(header, cancellationToken);

            var opcode = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(0, 4));
            var length = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4, 4));

            if (length <= 0) return "{}";

            var body = new byte[length];
            await this.pipe.ReadExactlyAsync(body, cancellationToken);
            var json = Encoding.UTF8.GetString(body);

            if (opcode is 2 or 3)
                throw new InvalidOperationException($"Discord closed IPC connection: {json}");

            return json;
        }

        public ValueTask DisposeAsync()
        {
            this.pipe.Dispose();
            return ValueTask.CompletedTask;
        }

        private static string TrimForLog(string value)
        {
            const int max = 300;
            return value.Length <= max ? value : value[..max] + "...";
        }
    }
}