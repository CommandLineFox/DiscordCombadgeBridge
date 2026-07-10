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

        public static void Save(TokenResponse? tokenResponse)
        {
            if (tokenResponse == null)
            {
                return;
            }

            try
            {
                var directory = Path.GetDirectoryName(TokenStoragePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(tokenResponse);
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
                {
                    return null;
                }

                var json = File.ReadAllText(TokenStoragePath);
                return JsonSerializer.Deserialize<TokenResponse>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}