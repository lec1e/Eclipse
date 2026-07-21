using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Froststrap.Models;
using Froststrap.Utility.Accounts.Hba;

namespace Froststrap.Utility.Accounts
{
    public readonly record struct RobloxAuthConfig(string Cookie, string HbaPrivateKey = "", bool HbaEnabled = true)
    {
        public bool HasHba => HbaEnabled && !string.IsNullOrEmpty(HbaPrivateKey);

        public static RobloxAuthConfig From(AltManAccount account) =>
            new(account.Cookie, account.HbaPrivateKey, account.HbaEnabled);
    }

    public sealed class RobloxHttpResult
    {
        public int StatusCode { get; init; }
        public string Body { get; init; } = "";
        public Dictionary<string, string> Headers { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public bool IsSuccess => StatusCode is >= 200 and < 300;
    }

    /// <summary>Cookie + CSRF + optional HBA BAT client (AltMan AuthenticatedHttp).</summary>
    public static class RobloxAuthenticatedClient
    {
        private const string LOG = "RobloxAuthenticatedClient";
        private static readonly string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

        public static async Task<RobloxHttpResult> GetAsync(string url, RobloxAuthConfig config, CancellationToken ct = default)
            => await SendAsync(HttpMethod.Get, url, config, body: null, ct).ConfigureAwait(false);

        public static async Task<RobloxHttpResult> PostAsync(string url, RobloxAuthConfig config, string? jsonBody = null, CancellationToken ct = default)
            => await SendAsync(HttpMethod.Post, url, config, jsonBody ?? "", ct).ConfigureAwait(false);

        public static async Task<RobloxHttpResult> PostWithAutoCsrfAsync(
            string url,
            RobloxAuthConfig config,
            string? jsonBody = null,
            CancellationToken ct = default)
        {
            string body = jsonBody ?? "";
            var first = await SendAsync(HttpMethod.Post, url, config, body, ct).ConfigureAwait(false);
            if (first.StatusCode == 403 && first.Headers.TryGetValue("x-csrf-token", out string? csrf) && !string.IsNullOrEmpty(csrf))
                return await SendAsync(HttpMethod.Post, url, config, body, ct, csrf).ConfigureAwait(false);
            return first;
        }

        private static async Task<RobloxHttpResult> SendAsync(
            HttpMethod method,
            string url,
            RobloxAuthConfig config,
            string? body,
            CancellationToken ct,
            string? csrf = null)
        {
            try
            {
                var handler = new HttpClientHandler { CookieContainer = new CookieContainer(), UseCookies = true };
                if (!string.IsNullOrEmpty(config.Cookie))
                    handler.CookieContainer.Add(new Cookie(".ROBLOSECURITY", config.Cookie, "/", ".roblox.com"));

                using var client = new HttpClient(handler);
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");

                using var req = new HttpRequestMessage(method, url);
                string bodyText = body ?? "";
                if (method != HttpMethod.Get)
                {
                    req.Content = new StringContent(bodyText, Encoding.UTF8, "application/json");
                }

                if (!string.IsNullOrEmpty(csrf))
                {
                    req.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", csrf);
                    req.Headers.TryAddWithoutValidation("Origin", "https://www.roblox.com");
                    req.Headers.Referrer = new Uri("https://www.roblox.com/");
                }

                if (config.HasHba)
                {
                    string? bat = HbaClient.Instance.TryGenerateBat(
                        config.HbaPrivateKey, url, method.Method, bodyText, config.Cookie);
                    if (!string.IsNullOrEmpty(bat))
                        req.Headers.TryAddWithoutValidation(BoundAuthToken.HeaderName, bat);
                }

                using var resp = await client.SendAsync(req, ct).ConfigureAwait(false);
                var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var h in resp.Headers)
                    headers[h.Key] = string.Join(",", h.Value);
                foreach (var h in resp.Content.Headers)
                    headers[h.Key] = string.Join(",", h.Value);

                // Also surface CSRF from response headers for auto-retry
                if (resp.Headers.TryGetValues("x-csrf-token", out var csrfVals))
                    headers["x-csrf-token"] = csrfVals.First();

                string text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return new RobloxHttpResult
                {
                    StatusCode = (int)resp.StatusCode,
                    Body = text,
                    Headers = headers
                };
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG, ex);
                return new RobloxHttpResult { StatusCode = 0, Body = ex.Message };
            }
        }
    }
}
