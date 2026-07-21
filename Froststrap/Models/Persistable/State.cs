namespace Froststrap.Models.Persistable
{
    public class State
    {
        public bool TestModeWarningShown { get; set; } = false;

        public bool IgnoreOutdatedChannel { get; set; } = false;

        public bool PromptWebView2Install { get; set; } = true;

        public string? LastPage { get; set; } = null!;

        public bool ForceReinstall { get; set; } = false;

        public WindowState SettingsWindow { get; set; } = new();

        public bool IsNavigationPaneOpen { get; set; } = true;

        public string? LastMigratedVersion { get; set; } = null;

        public List<ModConfig> Mods { get; set; } = [];

        // Eclipse / Versions Manager notify state
        public string LastNotifiedLiveHash { get; set; } = "";
        public string LastNotifiedAppVersion { get; set; } = "";
    }
}