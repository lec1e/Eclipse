using System.Text.Json;
using Froststrap.Models;

namespace Froststrap.Utility.Accounts
{
    public sealed class RobloxFriendInfo
    {
        public long Id { get; set; }
        public string Username { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Presence { get; set; } = "";
        public ulong PlaceId { get; set; }
        public string JobId { get; set; } = "";
        public string LastLocation { get; set; } = "";
    }

    public sealed class RobloxFriendRequest
    {
        public long Id { get; set; }
        public string Username { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string SentAt { get; set; } = "";
        public List<string> Mutuals { get; set; } = new();
    }

    public sealed class RobloxFriendDetail
    {
        public long Id { get; set; }
        public string Username { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public string CreatedIso { get; set; } = "";
        public string Presence { get; set; } = "";
        public ulong PlaceId { get; set; }
        public string JobId { get; set; } = "";
    }

    public static class RobloxSocialApi
    {
        public static async Task<Dictionary<long, (string Combined, string Username)>> GetUserProfilesAsync(IEnumerable<long> userIds, CancellationToken ct = default)
        {
            var result = new Dictionary<long, (string, string)>();
            var ids = userIds.Where(id => id > 0).Distinct().ToList();
            for (int i = 0; i < ids.Count; i += 100)
            {
                var batch = ids.Skip(i).Take(100).ToList();
                var payload = JsonSerializer.Serialize(new
                {
                    fields = new[] { "names.combinedName", "names.username" },
                    userIds = batch
                });
                using var req = new HttpRequestMessage(HttpMethod.Post, "https://apis.roblox.com/user-profile-api/v1/user/profiles/get-profiles")
                {
                    Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
                };
                using var resp = await App.HttpClient.SendAsync(req, ct).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode) continue;
                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
                if (!doc.RootElement.TryGetProperty("profileDetails", out var details)) continue;
                foreach (var profile in details.EnumerateArray())
                {
                    long uid = profile.TryGetProperty("userId", out var u) ? u.GetInt64() : 0;
                    if (uid == 0) continue;
                    string combined = "", username = "";
                    if (profile.TryGetProperty("names", out var names))
                    {
                        combined = names.TryGetProperty("combinedName", out var c) ? c.GetString() ?? "" : "";
                        username = names.TryGetProperty("username", out var n) ? n.GetString() ?? "" : "";
                    }
                    if (string.IsNullOrEmpty(username)) username = combined;
                    result[uid] = (combined, username);
                }
            }
            return result;
        }

        public static async Task<List<RobloxFriendInfo>> GetFriendsAsync(AltManAccount account, CancellationToken ct = default)
        {
            var config = RobloxAuthConfig.From(account);
            var resp = await RobloxAuthenticatedClient.GetAsync(
                $"https://friends.roblox.com/v1/users/{account.UserId}/friends", config, ct).ConfigureAwait(false);
            var friends = new List<RobloxFriendInfo>();
            if (!resp.IsSuccess) return friends;

            using var doc = JsonDocument.Parse(resp.Body);
            if (!doc.RootElement.TryGetProperty("data", out var data)) return friends;
            foreach (var item in data.EnumerateArray())
            {
                friends.Add(new RobloxFriendInfo
                {
                    Id = item.TryGetProperty("id", out var id) ? id.GetInt64() : 0,
                    DisplayName = item.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? "" : "",
                    Username = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : ""
                });
            }

            var profiles = await GetUserProfilesAsync(friends.Select(f => f.Id), ct).ConfigureAwait(false);
            foreach (var f in friends)
            {
                if (profiles.TryGetValue(f.Id, out var p))
                {
                    f.DisplayName = p.Combined;
                    f.Username = p.Username;
                }
            }

            await EnrichPresenceAsync(account, friends, ct).ConfigureAwait(false);
            return friends;
        }

        public static async Task EnrichPresenceAsync(AltManAccount account, List<RobloxFriendInfo> friends, CancellationToken ct = default)
        {
            var ids = friends.Where(f => f.Id > 0).Select(f => f.Id).ToList();
            if (ids.Count == 0) return;

            var config = RobloxAuthConfig.From(account);
            for (int i = 0; i < ids.Count; i += 100)
            {
                var batch = ids.Skip(i).Take(100).ToArray();
                string body = JsonSerializer.Serialize(new { userIds = batch });
                var resp = await RobloxAuthenticatedClient.PostWithAutoCsrfAsync(
                    "https://presence.roblox.com/v1/presence/users", config, body, ct).ConfigureAwait(false);
                if (!resp.IsSuccess) continue;
                using var doc = JsonDocument.Parse(resp.Body);
                if (!doc.RootElement.TryGetProperty("userPresences", out var list)) continue;
                foreach (var p in list.EnumerateArray())
                {
                    long uid = p.TryGetProperty("userId", out var u) ? u.GetInt64() : 0;
                    var friend = friends.FirstOrDefault(f => f.Id == uid);
                    if (friend is null) continue;
                    int type = p.TryGetProperty("userPresenceType", out var t) ? t.GetInt32() : 0;
                    friend.Presence = type switch { 0 => "Offline", 1 => "Online", 2 => "InGame", 3 => "InStudio", _ => "Unknown" };
                    friend.LastLocation = p.TryGetProperty("lastLocation", out var loc) ? loc.GetString() ?? "" : "";
                    friend.PlaceId = p.TryGetProperty("placeId", out var pid) && pid.ValueKind == JsonValueKind.Number ? pid.GetUInt64() : 0;
                    friend.JobId = p.TryGetProperty("gameId", out var gid) ? gid.GetString() ?? "" : "";
                }
            }
        }

        public static async Task<(List<RobloxFriendRequest> Items, string? NextCursor)> GetFriendRequestsAsync(
            AltManAccount account, string? cursor = null, CancellationToken ct = default)
        {
            var config = RobloxAuthConfig.From(account);
            string url = "https://friends.roblox.com/v1/my/friends/requests?limit=50";
            if (!string.IsNullOrEmpty(cursor))
                url += "&cursor=" + Uri.EscapeDataString(cursor);

            var resp = await RobloxAuthenticatedClient.GetAsync(url, config, ct).ConfigureAwait(false);
            var items = new List<RobloxFriendRequest>();
            string? next = null;
            if (!resp.IsSuccess) return (items, null);

            using var doc = JsonDocument.Parse(resp.Body);
            next = doc.RootElement.TryGetProperty("nextPageCursor", out var nc) ? nc.GetString() : null;
            if (!doc.RootElement.TryGetProperty("data", out var data)) return (items, next);

            var ids = new List<long>();
            foreach (var item in data.EnumerateArray())
            {
                var req = new RobloxFriendRequest
                {
                    Id = item.TryGetProperty("friendRequest", out var fr) && fr.TryGetProperty("senderId", out var sid)
                        ? sid.GetInt64()
                        : item.TryGetProperty("id", out var id) ? id.GetInt64() : 0,
                    SentAt = item.TryGetProperty("friendRequest", out var fr2) && fr2.TryGetProperty("sentAt", out var sa)
                        ? sa.GetString() ?? ""
                        : ""
                };
                if (item.TryGetProperty("mutualFriendsList", out var mutuals) && mutuals.ValueKind == JsonValueKind.Array)
                {
                    foreach (var m in mutuals.EnumerateArray())
                        req.Mutuals.Add(m.GetString() ?? "");
                }
                if (req.Id > 0)
                {
                    items.Add(req);
                    ids.Add(req.Id);
                }
            }

            var profiles = await GetUserProfilesAsync(ids, ct).ConfigureAwait(false);
            foreach (var r in items)
            {
                if (profiles.TryGetValue(r.Id, out var p))
                {
                    r.DisplayName = p.Combined;
                    r.Username = p.Username;
                }
            }
            return (items, next);
        }

        public static async Task<RobloxFriendDetail?> GetUserDetailsAsync(long userId, AltManAccount account, CancellationToken ct = default)
        {
            try
            {
                using var resp = await App.HttpClient.GetAsync($"https://users.roblox.com/v1/users/{userId}", ct).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode) return null;
                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
                var detail = new RobloxFriendDetail
                {
                    Id = userId,
                    Username = doc.RootElement.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                    DisplayName = doc.RootElement.TryGetProperty("displayName", out var d) ? d.GetString() ?? "" : "",
                    Description = doc.RootElement.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                    CreatedIso = doc.RootElement.TryGetProperty("created", out var c) ? c.GetString() ?? "" : ""
                };

                var list = new List<RobloxFriendInfo> { new() { Id = userId } };
                await EnrichPresenceAsync(account, list, ct).ConfigureAwait(false);
                detail.Presence = list[0].Presence;
                detail.PlaceId = list[0].PlaceId;
                detail.JobId = list[0].JobId;
                return detail;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("RobloxSocialApi::GetUserDetails", ex);
                return null;
            }
        }

        public static async Task<long> ResolveUsernameAsync(string username, CancellationToken ct = default)
        {
            string body = JsonSerializer.Serialize(new { usernames = new[] { username }, excludeBannedUsers = true });
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://users.roblox.com/v1/usernames/users")
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            };
            using var resp = await App.HttpClient.SendAsync(req, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return 0;
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.GetArrayLength() == 0) return 0;
            return data[0].TryGetProperty("id", out var id) ? id.GetInt64() : 0;
        }

        public static Task<RobloxHttpResult> AcceptFriendRequestAsync(AltManAccount account, long userId, CancellationToken ct = default)
            => RobloxAuthenticatedClient.PostWithAutoCsrfAsync(
                $"https://friends.roblox.com/v1/users/{userId}/accept-friend-request",
                RobloxAuthConfig.From(account), null, ct);

        public static Task<RobloxHttpResult> DeclineFriendRequestAsync(AltManAccount account, long userId, CancellationToken ct = default)
            => RobloxAuthenticatedClient.PostWithAutoCsrfAsync(
                $"https://friends.roblox.com/v1/users/{userId}/decline-friend-request",
                RobloxAuthConfig.From(account), null, ct);

        public static Task<RobloxHttpResult> SendFriendRequestAsync(AltManAccount account, long userId, CancellationToken ct = default)
            => RobloxAuthenticatedClient.PostWithAutoCsrfAsync(
                $"https://friends.roblox.com/v1/users/{userId}/request-friendship",
                RobloxAuthConfig.From(account),
                JsonSerializer.Serialize(new { friendshipOriginSourceType = 0 }), ct);

        public static Task<RobloxHttpResult> UnfriendAsync(AltManAccount account, long userId, CancellationToken ct = default)
            => RobloxAuthenticatedClient.PostWithAutoCsrfAsync(
                $"https://friends.roblox.com/v1/users/{userId}/unfriend",
                RobloxAuthConfig.From(account), null, ct);
    }
}
