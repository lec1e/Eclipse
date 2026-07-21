namespace Froststrap.Models.Persistable
{
    public class VersionProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";

        // Empty means "always use the current LIVE hash from Roblox CDN".
        public string VersionGuid { get; set; } = "";

        public string? ExecutorTitle { get; set; }
        public string? ExecutorLogoUrl { get; set; }
        public bool IsBuiltIn { get; set; }
        public string InstalledVersionGuid { get; set; } = "";
        public string ExecutorRefreshKey { get; set; } = "";
        public DateTime? LastExecutorRefreshUtc { get; set; }
        public string LastNotifiedExecutorHash { get; set; } = "";
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
