using System.Text;
using System.Text.Json;
using Froststrap.Integrations;
using Froststrap.Models;

namespace Froststrap.Utility.Accounts
{
    /// <summary>Password-protected AltMan-style backup (XOR+RLE compatible export for Eclipse store).</summary>
    public static class AltManBackup
    {
        public static string DefaultBackupDirectory
        {
            get
            {
                string dir = Path.Combine(Paths.Base, "AltMan", "backups");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static string Export(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password required.", nameof(password));

            var accounts = AccountManager.Shared.Accounts.Select(a => new
            {
                id = a.Id,
                cookie = a.Cookie,
                note = a.Note,
                isFavorite = a.IsFavorite,
                username = a.Username,
                displayName = a.DisplayName,
                userId = a.UserId,
                hbaPrivateKey = a.HbaPrivateKey,
                hbaEnabled = a.HbaEnabled
            }).ToList();

            var payload = new
            {
                version = 2,
                exportedAt = DateTime.UtcNow.ToString("o"),
                defaultAccountId = AccountManager.Shared.DefaultAccountId,
                settings = new
                {
                    statusRefreshInterval = App.Settings.Prop.AltManStatusRefreshIntervalMinutes,
                    killRobloxOnLaunch = App.Settings.Prop.AltManKillRobloxOnLaunch,
                    clearCacheOnLaunch = App.Settings.Prop.AltManClearCacheOnLaunch,
                    multiRoblox = App.Settings.Prop.MultiInstanceEnabled,
                    favoriteGames = App.Settings.Prop.AltManFavoriteGames
                },
                accounts
            };

            string json = JsonSerializer.Serialize(payload);
            byte[] compressed = RleCompress(Encoding.UTF8.GetBytes(json));
            byte[] encrypted = Xor(compressed, Encoding.UTF8.GetBytes(password));
            string path = Path.Combine(DefaultBackupDirectory, $"{DateTime.Now:yyyy-MM-dd}-backup.dat");
            File.WriteAllBytes(path, encrypted);
            return path;
        }

        public static (int Imported, string? Error) Import(string filePath, string password)
        {
            try
            {
                byte[] encrypted = File.ReadAllBytes(filePath);
                byte[] compressed = Xor(encrypted, Encoding.UTF8.GetBytes(password));
                byte[] jsonBytes = RleDecompress(compressed);
                string json = Encoding.UTF8.GetString(jsonBytes);
                using var doc = JsonDocument.Parse(json);

                int imported = 0;
                if (doc.RootElement.TryGetProperty("accounts", out var accounts))
                {
                    foreach (var row in accounts.EnumerateArray())
                    {
                        string cookie = row.TryGetProperty("cookie", out var c) ? c.GetString() ?? "" : "";
                        if (string.IsNullOrEmpty(cookie)) continue;

                        var existing = AccountManager.Shared.Accounts.FirstOrDefault(a => a.Cookie == cookie);
                        if (existing is not null)
                        {
                            if (row.TryGetProperty("note", out var n)) existing.Note = n.GetString() ?? existing.Note;
                            if (row.TryGetProperty("isFavorite", out var f)) existing.IsFavorite = f.GetBoolean();
                            if (row.TryGetProperty("hbaPrivateKey", out var h) && !string.IsNullOrEmpty(h.GetString()))
                                existing.HbaPrivateKey = h.GetString()!;
                            imported++;
                            continue;
                        }

                        var account = new AltManAccount
                        {
                            Cookie = cookie,
                            Note = row.TryGetProperty("note", out var note) ? note.GetString() ?? "" : "",
                            IsFavorite = row.TryGetProperty("isFavorite", out var fav) && fav.GetBoolean(),
                            Username = row.TryGetProperty("username", out var un) ? un.GetString() ?? "" : "",
                            DisplayName = row.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? "" : "",
                            UserId = row.TryGetProperty("userId", out var uid) ? uid.GetString() ?? "" : "",
                            HbaPrivateKey = row.TryGetProperty("hbaPrivateKey", out var hk) ? hk.GetString() ?? "" : "",
                            HbaEnabled = !row.TryGetProperty("hbaEnabled", out var he) || he.GetBoolean(),
                            Status = "Unknown"
                        };
                        AccountManager.Shared.AddAccount(account);
                        imported++;
                    }
                }

                if (doc.RootElement.TryGetProperty("settings", out var settings))
                {
                    if (settings.TryGetProperty("statusRefreshInterval", out var sri) && sri.TryGetInt32(out int interval))
                        App.Settings.Prop.AltManStatusRefreshIntervalMinutes = Math.Max(1, interval);
                    if (settings.TryGetProperty("killRobloxOnLaunch", out var k) && k.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        App.Settings.Prop.AltManKillRobloxOnLaunch = k.GetBoolean();
                    if (settings.TryGetProperty("clearCacheOnLaunch", out var cc) && cc.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        App.Settings.Prop.AltManClearCacheOnLaunch = cc.GetBoolean();
                    if (settings.TryGetProperty("favoriteGames", out var fg) && fg.ValueKind == JsonValueKind.Array)
                    {
                        App.Settings.Prop.AltManFavoriteGames.Clear();
                        foreach (var g in fg.EnumerateArray())
                        {
                            if (g.ValueKind == JsonValueKind.String)
                                App.Settings.Prop.AltManFavoriteGames.Add(g.GetString()!);
                            else if (g.TryGetProperty("placeId", out var pid))
                                App.Settings.Prop.AltManFavoriteGames.Add(pid.ToString());
                        }
                    }
                }

                AccountManager.Shared.Save();
                App.Settings.Save();
                return (imported, null);
            }
            catch (Exception ex)
            {
                return (0, ex.Message);
            }
        }

        private static byte[] Xor(byte[] data, byte[] password)
        {
            if (password.Length == 0) return data;
            byte[] outBytes = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
                outBytes[i] = (byte)(data[i] ^ password[i % password.Length]);
            return outBytes;
        }

        private static byte[] RleCompress(byte[] data)
        {
            using var ms = new MemoryStream();
            for (int i = 0; i < data.Length;)
            {
                byte c = data[i];
                int j = i;
                while (j < data.Length && data[j] == c && j - i < 255) j++;
                ms.WriteByte(c);
                ms.WriteByte((byte)(j - i));
                i = j;
            }
            return ms.ToArray();
        }

        private static byte[] RleDecompress(byte[] data)
        {
            using var ms = new MemoryStream();
            for (int i = 0; i + 1 < data.Length; i += 2)
            {
                byte c = data[i];
                int count = data[i + 1];
                for (int n = 0; n < count; n++)
                    ms.WriteByte(c);
            }
            return ms.ToArray();
        }
    }

    public static class RobloxCacheClearer
    {
        public static int ClearLocalCache()
        {
            int cleared = 0;
            string? local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(local)) return 0;

            string[] targets =
            [
                Path.Combine(local, "Roblox", "logs"),
                Path.Combine(local, "Roblox", "http"),
                Path.Combine(local, "Temp", "Roblox")
            ];

            foreach (string dir in targets)
            {
                if (!Directory.Exists(dir)) continue;
                try
                {
                    foreach (string file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                    {
                        try { File.Delete(file); cleared++; } catch { }
                    }
                }
                catch { }
            }
            return cleared;
        }
    }
}
