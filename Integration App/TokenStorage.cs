using System.Text.Json;

namespace GFC_ComBadge
{
    public static class TokenStorage
    {
        private static readonly string TokenStoragePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GFC_ComBadge",
            "auth.json"
        );

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static void Save(TokenResponse? tokenResponse)
        {
            if (tokenResponse == null) return;

            try
            {
                var directory = Path.GetDirectoryName(TokenStoragePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(tokenResponse, JsonOptions);
                File.WriteAllText(TokenStoragePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to cache authentication tokens locally: {ex.Message}");
            }
        }

        public static TokenResponse? Load()
        {
            try
            {
                if (!File.Exists(TokenStoragePath))
                    return null;

                var json = File.ReadAllText(TokenStoragePath);
                var token = JsonSerializer.Deserialize<TokenResponse>(json, JsonOptions);

                if (token == null)
                    return null;

                if (token.IsExpiredSoon)
                {
                    Console.WriteLine("Cached token is expired or expiring soon.");
                    return null;
                }

                return token;
            }
            catch
            {
                return null;
            }
        }

        public static void Clear()
        {
            try
            {
                if (File.Exists(TokenStoragePath))
                    File.Delete(TokenStoragePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to clear stored token: {ex.Message}");
            }
        }
    }
}