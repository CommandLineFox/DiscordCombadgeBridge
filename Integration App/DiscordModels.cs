using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GFC_ComBadge
{
    public static class DiscordOAuth
    {
        private static readonly HttpClient Http = new();
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public static async Task<TokenResponse> ExchangeCodeAsync(string clientId, string clientSecret, string code, CancellationToken cancellationToken)
        {
            return await SendTokenRequestAsync(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["grant_type"] = "authorization_code",
                ["code"] = code
            }, cancellationToken);
        }

        public static async Task<TokenResponse> RefreshTokenAsync(string clientId, string clientSecret, string refreshToken, CancellationToken cancellationToken)
        {
            return await SendTokenRequestAsync(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            }, cancellationToken);
        }

        private static async Task<TokenResponse> SendTokenRequestAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token");
            request.Content = new FormUrlEncodedContent(form);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            using var response = await Http.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"OAuth failed: {json}");

            var token = JsonSerializer.Deserialize<TokenResponse>(json, JsonOptions)
                ?? throw new InvalidOperationException("Could not parse OAuth response.");

            token.ReceivedAt = DateTimeOffset.UtcNow;
            return token;
        }
    }

    public sealed class TokenResponse
    {
        [JsonPropertyName("access_token")] public required string AccessToken { get; init; }
        [JsonPropertyName("token_type")] public string? TokenType { get; init; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; init; }
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; init; }
        [JsonPropertyName("scope")] public string? Scope { get; init; }
        [JsonPropertyName("received_at")] public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
        [JsonIgnore] public DateTimeOffset ExpiresAt => ReceivedAt.AddSeconds(ExpiresIn);
        [JsonIgnore] public bool IsExpiredSoon => DateTimeOffset.UtcNow >= ExpiresAt.AddMinutes(-2);
        [JsonIgnore] public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    }

    public sealed class AuthenticateData
    {
        [JsonPropertyName("user")] public required DiscordUser User { get; init; }
        [JsonPropertyName("scopes")] public required string[] Scopes { get; init; }
        [JsonPropertyName("expires")] public required DateTimeOffset Expires { get; init; }
        [JsonPropertyName("application")] public required DiscordApplication Application { get; init; }
    }

    public sealed class DiscordUser
    {
        [JsonPropertyName("id")] public required string Id { get; init; }
        [JsonPropertyName("username")] public required string Username { get; init; }
        [JsonPropertyName("discriminator")] public string? Discriminator { get; init; }
        [JsonPropertyName("global_name")] public string? GlobalName { get; init; }
        [JsonPropertyName("avatar")] public string? Avatar { get; init; }
    }

    public sealed class DiscordApplication
    {
        [JsonPropertyName("id")] public required string Id { get; init; }
        [JsonPropertyName("name")] public required string Name { get; init; }
        [JsonPropertyName("description")] public string? Description { get; init; }
        [JsonPropertyName("icon")] public string? Icon { get; init; }
        [JsonPropertyName("rpc_origins")] public string[]? RpcOrigins { get; init; }
    }
}