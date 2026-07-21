namespace Froststrap.Utility
{
    // Resolves a Roblox server IP to a human-readable region.
    // Roblox game servers live in 128.116.0.0/16; the third octet identifies the datacenter.
    public static class RobloxDatacenters
    {
        private static readonly Dictionary<int, string> DatacenterMap = new()
        {
            { 1,   "Los Angeles, US" },
            { 22,  "Atlanta, US" },
            { 33,  "London, UK" },
            { 45,  "Miami, US" },
            { 48,  "Chicago, US" },
            { 55,  "Tokyo, JP" },
            { 63,  "Los Angeles, US" },
            { 95,  "Dallas, US" },
            { 99,  "Atlanta, US" },
            { 101, "Chicago, US" },
            { 115, "Seattle, US" },
            { 116, "Los Angeles, US" },
            { 119, "London, UK" },
            { 120, "Tokyo, JP" },
        };

        public static string? LookupDatacenter(string? ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return null;

            string[] parts = ip.Split('.');
            if (parts.Length != 4 || parts[0] != "128" || parts[1] != "116")
                return null;

            return int.TryParse(parts[2], out int octet) && DatacenterMap.TryGetValue(octet, out string? region)
                ? region
                : null;
        }

        public static async Task<string?> ResolveRegionAsync(string? ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return null;

            string? datacenter = LookupDatacenter(ip);
            if (datacenter is not null)
                return datacenter;

            if (GlobalCache.ServerLocation.TryGetValue(ip, out string? cached) && !string.IsNullOrEmpty(cached))
                return cached;

            try
            {
                var ipInfo = await Http.GetJson<IPInfoResponse>($"https://ipinfo.io/{ip}/json");
                if (ipInfo is not null && !string.IsNullOrEmpty(ipInfo.City))
                {
                    string location = ipInfo.City == ipInfo.Region
                        ? $"{ipInfo.Region}, {ipInfo.Country}"
                        : $"{ipInfo.City}, {ipInfo.Region}, {ipInfo.Country}";
                    GlobalCache.ServerLocation[ip] = location;
                    return location;
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("RobloxDatacenters::ResolveRegionAsync", ex);
            }

            return null;
        }
    }
}
