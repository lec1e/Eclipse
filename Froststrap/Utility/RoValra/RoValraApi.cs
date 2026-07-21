using Froststrap.Models.APIs.RoValra;

namespace Froststrap.Utility.RoValra
{
    /// <summary>
    /// Thin client for the public RoValra APIs used by the Region Selector / ping pipeline.
    /// See https://github.com/NotValra/RoValra — data from https://apis.rovalra.com
    /// </summary>
    public static class RoValraApi
    {
        private const string LOG_IDENT = "RoValraApi";
        private const string BaseUrl = "https://apis.rovalra.com";

        public static async Task<List<DatacenterEntry>?> GetDatacentersAsync(CancellationToken ct = default)
        {
            try
            {
                return await Http.GetJson<List<DatacenterEntry>>($"{BaseUrl}/v1/datacenters/list");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException($"{LOG_IDENT}::GetDatacenters", ex);
                return null;
            }
        }

        public static async Task<RoValraRegionServersResponse?> GetServersByRegionAsync(
            long placeId,
            string country,
            string? city = null,
            string? cursor = null,
            CancellationToken ct = default)
        {
            try
            {
                var qs = new List<string>
                {
                    $"place_id={placeId}",
                    $"country={Uri.EscapeDataString(country)}",
                    $"cursor={Uri.EscapeDataString(cursor ?? "0")}"
                };
                if (!string.IsNullOrWhiteSpace(city))
                    qs.Add($"city={Uri.EscapeDataString(city)}");

                string url = $"{BaseUrl}/v1/servers/region?{string.Join("&", qs)}";
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                using var resp = await App.HttpClient.SendAsync(req, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"region servers {(int)resp.StatusCode} for {country}/{city}");
                    return null;
                }

                await using var stream = await resp.Content.ReadAsStreamAsync(ct);
                return await JsonSerializer.DeserializeAsync<RoValraRegionServersResponse>(stream, cancellationToken: ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                App.Logger.WriteException($"{LOG_IDENT}::GetServersByRegion", ex);
                return null;
            }
        }

        public static async Task<Dictionary<string, RoValrasServer>> GetServerDetailsAsync(
            long placeId,
            IEnumerable<string> serverIds,
            CancellationToken ct = default)
        {
            var result = new Dictionary<string, RoValrasServer>(StringComparer.OrdinalIgnoreCase);
            var ids = serverIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (ids.Count == 0)
                return result;

            const int batchSize = 50;
            for (int i = 0; i < ids.Count; i += batchSize)
            {
                ct.ThrowIfCancellationRequested();
                var batch = ids.Skip(i).Take(batchSize).ToList();
                try
                {
                    string url =
                        $"{BaseUrl}/v1/servers/details?place_id={placeId}&server_ids={Uri.EscapeDataString(string.Join(",", batch))}";
                    using var req = new HttpRequestMessage(HttpMethod.Get, url);
                    using var resp = await App.HttpClient.SendAsync(req, ct);
                    if (!resp.IsSuccessStatusCode)
                        continue;

                    await using var stream = await resp.Content.ReadAsStreamAsync(ct);
                    var parsed = await JsonSerializer.DeserializeAsync<RoValraTimeResponse>(stream, cancellationToken: ct);
                    if (parsed?.Servers is null)
                        continue;

                    foreach (var s in parsed.Servers)
                    {
                        if (!string.IsNullOrEmpty(s.ServerId))
                            result[s.ServerId] = s;
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    App.Logger.WriteException($"{LOG_IDENT}::GetServerDetails", ex);
                }
            }

            return result;
        }
    }
}
