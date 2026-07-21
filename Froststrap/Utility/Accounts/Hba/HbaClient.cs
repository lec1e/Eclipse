using System.Text.Json;
using System.Text.RegularExpressions;

namespace Froststrap.Utility.Accounts.Hba
{
    public sealed class HbaWhitelistItem
    {
        public string ApiSite { get; set; } = "";
        public int SampleRate { get; set; } = 100;
    }

    public sealed class HbaTokenMetadata
    {
        public bool IsSecureAuthenticationIntentEnabled { get; set; }
        public bool IsBoundAuthTokenEnabledForAllUrls { get; set; }
        public List<HbaWhitelistItem> Whitelist { get; } = new();
        public List<string> ExemptList { get; } = new();
        public bool IsAuthenticated { get; set; }
        public DateTime FetchedAtUtc { get; set; }

        public bool IsValid =>
            IsSecureAuthenticationIntentEnabled || IsBoundAuthTokenEnabledForAllUrls || Whitelist.Count > 0;

        public bool IsExpired(TimeSpan? ttl = null)
            => DateTime.UtcNow - FetchedAtUtc > (ttl ?? TimeSpan.FromMinutes(5));
    }

    /// <summary>AltMan HBA client — metadata + when to attach BAT.</summary>
    public sealed class HbaClient
    {
        private const string MetadataUrl = "https://www.roblox.com/charts";
        private static readonly string[] ForceBatUrls = ["/account-switcher/v1/switch"];

        public static HbaClient Instance { get; } = new();

        private readonly object _lock = new();
        private HbaTokenMetadata _cache = new();

        public HbaTokenMetadata GetTokenMetadata(string? cookie = null, bool forceRefresh = false)
        {
            lock (_lock)
            {
                if (!forceRefresh && _cache.IsValid && !_cache.IsExpired())
                    return Clone(_cache);
            }

            var fresh = FetchMetadata(cookie ?? "");
            lock (_lock)
            {
                if (fresh.IsValid)
                    _cache = fresh;
                return Clone(_cache);
            }
        }

        public bool IsUrlProtected(string url, bool isAuthenticated = true, string? cookie = null)
        {
            if (!url.Contains(".roblox.com", StringComparison.OrdinalIgnoreCase))
                return false;

            foreach (string forced in ForceBatUrls)
            {
                if (url.Contains(forced, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            var metadata = GetTokenMetadata(cookie, forceRefresh: false);
            if (!isAuthenticated && !metadata.IsAuthenticated)
                return false;

            foreach (string exempt in metadata.ExemptList)
            {
                if (!string.IsNullOrEmpty(exempt) && url.Contains(exempt, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (metadata.IsBoundAuthTokenEnabledForAllUrls)
                return true;

            foreach (var item in metadata.Whitelist)
            {
                if (string.IsNullOrEmpty(item.ApiSite) || !url.Contains(item.ApiSite, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (item.SampleRate >= 100)
                    return true;
                if (item.SampleRate <= 0)
                    return false;
                return Random.Shared.Next(100) < item.SampleRate;
            }

            return false;
        }

        public string? TryGenerateBat(string privateKeyPem, string url, string method, string body, string cookie)
        {
            if (string.IsNullOrEmpty(privateKeyPem))
                return null;
            if (!IsUrlProtected(url, true, cookie))
                return null;
            return BoundAuthToken.Generate(privateKeyPem, url, method, body);
        }

        public void ClearCache()
        {
            lock (_lock)
                _cache = new HbaTokenMetadata();
        }

        private static HbaTokenMetadata FetchMetadata(string cookie)
        {
            var metadata = new HbaTokenMetadata { FetchedAtUtc = DateTime.UtcNow };
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, MetadataUrl);
                if (!string.IsNullOrEmpty(cookie))
                    req.Headers.TryAddWithoutValidation("Cookie", $".ROBLOSECURITY={cookie}");

                using var resp = App.HttpClient.Send(req);
                if (!resp.IsSuccessStatusCode)
                    return metadata;

                string html = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                ParseHtml(html, metadata);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("HbaClient::FetchMetadata", ex);
            }

            return metadata;
        }

        private static void ParseHtml(string html, HbaTokenMetadata metadata)
        {
            metadata.IsAuthenticated = html.Contains("name=\"user-data\"", StringComparison.Ordinal);

            int metaIdx = html.IndexOf("name=\"hardware-backed-authentication-data\"", StringComparison.Ordinal);
            if (metaIdx < 0)
                return;

            int tagStart = html.LastIndexOf("<meta", metaIdx, StringComparison.OrdinalIgnoreCase);
            int tagEnd = html.IndexOf('>', metaIdx);
            if (tagStart < 0 || tagEnd < 0)
                return;

            string metaTag = html[tagStart..(tagEnd + 1)];
            metadata.IsSecureAuthenticationIntentEnabled = AttrBool(metaTag, "data-is-secure-authentication-intent-enabled");
            metadata.IsBoundAuthTokenEnabledForAllUrls = AttrBool(metaTag, "data-is-bound-auth-token-enabled");

            string whitelistJson = DecodeEntities(Attr(metaTag, "data-bound-auth-token-whitelist"));
            ParseWhitelist(whitelistJson, metadata.Whitelist);

            string exemptJson = DecodeEntities(Attr(metaTag, "data-bound-auth-token-exemptlist"));
            ParseExempt(exemptJson, metadata.ExemptList);
        }

        private static string Attr(string tag, string name)
        {
            string pattern = name + "=\"";
            int start = tag.IndexOf(pattern, StringComparison.Ordinal);
            if (start < 0) return "";
            start += pattern.Length;
            int end = tag.IndexOf('"', start);
            return end < 0 ? "" : tag[start..end];
        }

        private static bool AttrBool(string tag, string name)
        {
            string v = Attr(tag, name);
            return v.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        private static string DecodeEntities(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input
                .Replace("&quot;", "\"")
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&nbsp;", " ");
        }

        private static void ParseWhitelist(string json, List<HbaWhitelistItem> list)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            try
            {
                using var doc = JsonDocument.Parse(json.Contains('[') ? EnsureArrayRoot(json) : json);
                JsonElement root = doc.RootElement;
                if (root.TryGetProperty("Whitelist", out var wl))
                    root = wl;
                if (root.ValueKind != JsonValueKind.Array)
                    return;

                foreach (var item in root.EnumerateArray())
                {
                    string site = item.TryGetProperty("apiSite", out var s) ? s.GetString() ?? "" : "";
                    if (string.IsNullOrEmpty(site)) continue;
                    int rate = 100;
                    if (item.TryGetProperty("sampleRate", out var r))
                    {
                        if (r.ValueKind == JsonValueKind.Number)
                            rate = r.GetInt32();
                        else if (r.ValueKind == JsonValueKind.String && int.TryParse(r.GetString(), out int parsed))
                            rate = parsed;
                    }
                    list.Add(new HbaWhitelistItem { ApiSite = site, SampleRate = rate });
                }
            }
            catch
            {
                // Fallback: regex scrape apiSite values
                foreach (Match m in Regex.Matches(json, "\"apiSite\"\\s*:\\s*\"([^\"]+)\""))
                    list.Add(new HbaWhitelistItem { ApiSite = m.Groups[1].Value, SampleRate = 100 });
            }
        }

        private static void ParseExempt(string json, List<string> list)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            try
            {
                using var doc = JsonDocument.Parse(json.Contains('[') ? EnsureArrayRoot(json) : json);
                JsonElement root = doc.RootElement;
                if (root.TryGetProperty("Exemptlist", out var el) || root.TryGetProperty("ExemptList", out el))
                    root = el;
                if (root.ValueKind != JsonValueKind.Array)
                    return;
                foreach (var item in root.EnumerateArray())
                {
                    string site = item.TryGetProperty("apiSite", out var s) ? s.GetString() ?? "" : "";
                    if (!string.IsNullOrEmpty(site))
                        list.Add(site);
                }
            }
            catch
            {
                foreach (Match m in Regex.Matches(json, "\"apiSite\"\\s*:\\s*\"([^\"]+)\""))
                    list.Add(m.Groups[1].Value);
            }
        }

        private static string EnsureArrayRoot(string json)
        {
            json = json.Trim();
            if (json.StartsWith('['))
                return json;
            int i = json.IndexOf('[');
            int j = json.LastIndexOf(']');
            if (i >= 0 && j > i)
                return json[i..(j + 1)];
            return json;
        }

        private static HbaTokenMetadata Clone(HbaTokenMetadata src)
        {
            var copy = new HbaTokenMetadata
            {
                IsSecureAuthenticationIntentEnabled = src.IsSecureAuthenticationIntentEnabled,
                IsBoundAuthTokenEnabledForAllUrls = src.IsBoundAuthTokenEnabledForAllUrls,
                IsAuthenticated = src.IsAuthenticated,
                FetchedAtUtc = src.FetchedAtUtc
            };
            copy.Whitelist.AddRange(src.Whitelist);
            copy.ExemptList.AddRange(src.ExemptList);
            return copy;
        }
    }
}
