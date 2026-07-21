using System.Text.Json;

namespace Froststrap.Utility.Accounts
{
    public sealed class RobloxGameInfo
    {
        public string Name { get; set; } = "";
        public ulong UniverseId { get; set; }
        public ulong PlaceId { get; set; }
        public int PlayerCount { get; set; }
        public int UpVotes { get; set; }
        public int DownVotes { get; set; }
        public string CreatorName { get; set; } = "";
        public bool CreatorVerified { get; set; }
    }

    public sealed class RobloxGameDetail
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Genre { get; set; } = "";
        public ulong Visits { get; set; }
        public ulong Favorites { get; set; }
        public int Playing { get; set; }
        public int MaxPlayers { get; set; }
        public string CreatorName { get; set; } = "";
        public ulong CreatorId { get; set; }
        public string CreatedIso { get; set; } = "";
        public string UpdatedIso { get; set; } = "";
    }

    public static class RobloxGamesApi
    {
        public static async Task<List<RobloxGameInfo>> SearchGamesAsync(string query, CancellationToken ct = default)
        {
            var outList = new List<RobloxGameInfo>();
            if (string.IsNullOrWhiteSpace(query)) return outList;

            string sessionId = Guid.NewGuid().ToString();
            string url =
                "https://apis.roblox.com/search-api/omni-search?" +
                $"searchQuery={Uri.EscapeDataString(query)}&pageToken=&sessionId={Uri.EscapeDataString(sessionId)}&pageType=all";

            using var resp = await App.HttpClient.GetAsync(url, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return outList;

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            if (!doc.RootElement.TryGetProperty("searchResults", out var groups)) return outList;

            foreach (var group in groups.EnumerateArray())
            {
                if ((group.TryGetProperty("contentGroupType", out var type) ? type.GetString() : "") != "Game")
                    continue;
                if (!group.TryGetProperty("contents", out var contents)) continue;
                foreach (var g in contents.EnumerateArray())
                {
                    outList.Add(new RobloxGameInfo
                    {
                        Name = g.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                        UniverseId = g.TryGetProperty("universeId", out var u) ? u.GetUInt64() : 0,
                        PlaceId = g.TryGetProperty("rootPlaceId", out var p) ? p.GetUInt64() : 0,
                        PlayerCount = g.TryGetProperty("playerCount", out var pc) ? pc.GetInt32() : 0,
                        UpVotes = g.TryGetProperty("totalUpVotes", out var up) ? up.GetInt32() : 0,
                        DownVotes = g.TryGetProperty("totalDownVotes", out var dn) ? dn.GetInt32() : 0,
                        CreatorName = g.TryGetProperty("creatorName", out var cn) ? cn.GetString() ?? "" : "",
                        CreatorVerified = g.TryGetProperty("creatorHasVerifiedBadge", out var cv) && cv.GetBoolean()
                    });
                }
            }
            return outList;
        }

        public static async Task<RobloxGameDetail?> GetGameDetailAsync(ulong universeId, CancellationToken ct = default)
        {
            using var resp = await App.HttpClient.GetAsync(
                $"https://games.roblox.com/v1/games?universeIds={universeId}", ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.GetArrayLength() == 0) return null;
            var j = data[0];
            var d = new RobloxGameDetail
            {
                Name = j.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                Description = j.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                Genre = j.TryGetProperty("genre", out var g) ? g.GetString() ?? "" : "",
                Visits = j.TryGetProperty("visits", out var v) ? v.GetUInt64() : 0,
                Favorites = j.TryGetProperty("favoritedCount", out var f) ? f.GetUInt64() : 0,
                Playing = j.TryGetProperty("playing", out var p) ? p.GetInt32() : 0,
                MaxPlayers = j.TryGetProperty("maxPlayers", out var m) ? m.GetInt32() : 0,
                CreatedIso = j.TryGetProperty("created", out var c) ? c.GetString() ?? "" : "",
                UpdatedIso = j.TryGetProperty("updated", out var u) ? u.GetString() ?? "" : ""
            };
            if (j.TryGetProperty("creator", out var creator))
            {
                d.CreatorName = creator.TryGetProperty("name", out var cn) ? cn.GetString() ?? "" : "";
                d.CreatorId = creator.TryGetProperty("id", out var cid) ? cid.GetUInt64() : 0;
            }
            return d;
        }
    }
}
