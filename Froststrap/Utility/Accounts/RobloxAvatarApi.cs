using System.Text.Json;
using Froststrap.Models;

namespace Froststrap.Utility.Accounts
{
    public sealed class RobloxInventoryCategory
    {
        public string DisplayName { get; set; } = "";
        public List<(int Id, string Name)> AssetTypes { get; set; } = new();
    }

    public sealed class RobloxInventoryItem
    {
        public long AssetId { get; set; }
        public string Name { get; set; } = "";
        public string? ThumbnailUrl { get; set; }
    }

    public static class RobloxAvatarApi
    {
        public static async Task<List<RobloxInventoryCategory>> GetCategoriesAsync(AltManAccount account, CancellationToken ct = default)
        {
            var list = new List<RobloxInventoryCategory>();
            var resp = await RobloxAuthenticatedClient.GetAsync(
                $"https://inventory.roblox.com/v1/users/{account.UserId}/categories",
                RobloxAuthConfig.From(account), ct).ConfigureAwait(false);
            if (!resp.IsSuccess) return list;

            using var doc = JsonDocument.Parse(resp.Body);
            if (!doc.RootElement.TryGetProperty("categories", out var cats)) return list;
            foreach (var cat in cats.EnumerateArray())
            {
                var ci = new RobloxInventoryCategory
                {
                    DisplayName = cat.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? "" : ""
                };
                if (cat.TryGetProperty("items", out var items))
                {
                    foreach (var it in items.EnumerateArray())
                    {
                        int id = it.TryGetProperty("id", out var iid) ? iid.GetInt32() : 0;
                        string name = it.TryGetProperty("displayName", out var n) ? n.GetString() ?? "" : "";
                        if (id != 0) ci.AssetTypes.Add((id, name));
                    }
                }
                if (ci.AssetTypes.Count > 0) list.Add(ci);
            }
            return list;
        }

        public static async Task<(List<RobloxInventoryItem> Items, string? NextCursor)> GetInventoryAsync(
            AltManAccount account, int assetTypeId, string? cursor = null, CancellationToken ct = default)
        {
            string url = $"https://inventory.roblox.com/v2/users/{account.UserId}/inventory/{assetTypeId}?limit=100&sortOrder=Desc";
            if (!string.IsNullOrEmpty(cursor))
                url += "&cursor=" + Uri.EscapeDataString(cursor);

            var resp = await RobloxAuthenticatedClient.GetAsync(url, RobloxAuthConfig.From(account), ct).ConfigureAwait(false);
            var items = new List<RobloxInventoryItem>();
            string? next = null;
            if (!resp.IsSuccess) return (items, null);

            using var doc = JsonDocument.Parse(resp.Body);
            next = doc.RootElement.TryGetProperty("nextPageCursor", out var nc) ? nc.GetString() : null;
            if (doc.RootElement.TryGetProperty("data", out var data))
            {
                foreach (var it in data.EnumerateArray())
                {
                    items.Add(new RobloxInventoryItem
                    {
                        AssetId = it.TryGetProperty("assetId", out var a) ? a.GetInt64()
                            : it.TryGetProperty("id", out var id) ? id.GetInt64() : 0,
                        Name = it.TryGetProperty("name", out var n) ? n.GetString() ?? ""
                            : it.TryGetProperty("assetName", out var an) ? an.GetString() ?? "" : ""
                    });
                }
            }
            return (items, next);
        }

        public static async Task<List<long>> GetCurrentlyWearingAsync(AltManAccount account, CancellationToken ct = default)
        {
            var resp = await RobloxAuthenticatedClient.GetAsync(
                $"https://avatar.roblox.com/v1/users/{account.UserId}/currently-wearing",
                RobloxAuthConfig.From(account), ct).ConfigureAwait(false);
            var ids = new List<long>();
            if (!resp.IsSuccess) return ids;
            using var doc = JsonDocument.Parse(resp.Body);
            if (doc.RootElement.TryGetProperty("assetIds", out var arr))
            {
                foreach (var a in arr.EnumerateArray())
                    ids.Add(a.GetInt64());
            }
            return ids;
        }

        public static async Task<string?> GetAvatarThumbnailUrlAsync(long userId, CancellationToken ct = default)
        {
            string url = $"https://thumbnails.roblox.com/v1/users/avatar?userIds={userId}&size=420x420&format=Png&isCircular=false";
            using var resp = await App.HttpClient.GetAsync(url, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.GetArrayLength() == 0) return null;
            return data[0].TryGetProperty("imageUrl", out var img) ? img.GetString() : null;
        }

        public static async Task<bool> SetWearingAssetsAsync(AltManAccount account, IEnumerable<long> assetIds, CancellationToken ct = default)
        {
            string body = JsonSerializer.Serialize(new { assetIds = assetIds.ToArray() });
            var resp = await RobloxAuthenticatedClient.PostWithAutoCsrfAsync(
                "https://avatar.roblox.com/v1/avatar/set-wearing-assets",
                RobloxAuthConfig.From(account), body, ct).ConfigureAwait(false);
            return resp.IsSuccess;
        }
    }
}
