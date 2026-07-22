using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Froststrap.Models;
using LucideAvalonia.Enum;

namespace Froststrap.UI.ViewModels.Settings
{
    public class HomeDashboardViewModel : NotifyPropertyChangedViewModel
    {
        private readonly MainWindowViewModel _host;

        public string Greeting => "Welcome back";
        public string DisplayName => "Eclipse";
        public string Tagline => "Your Roblox bootstrapper for a better experience.";
        public string VersionText => $"v{App.Version}";
        public string StatusText => "Online";

        public ObservableCollection<HomeRecentItem> RecentGames { get; } = [];
        public ObservableCollection<HomeQuickLink> QuickLinks { get; } = [];
        public bool HasRecentGames => RecentGames.Count > 0;

        public ICommand LaunchRobloxCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand OpenQuickPlayCommand { get; }
        public ICommand OpenNewsCommand { get; }
        public ICommand NavigateLinkCommand { get; }

        public HomeDashboardViewModel(MainWindowViewModel host)
        {
            _host = host;
            LaunchRobloxCommand = new RelayCommand(() => _host.SaveAndLaunchSettings());
            OpenFolderCommand = new RelayCommand(() =>
            {
                try
                {
                    if (Directory.Exists(Paths.Base))
                        Utilities.ShellExecute(Paths.Base);
                }
                catch { /* ignore */ }
            });
            OpenQuickPlayCommand = new RelayCommand(() => _host.NavigateToQuickPlayCommand.Execute(null));
            OpenNewsCommand = new RelayCommand(() => _host.NavigateToNewsCommand.Execute(null));
            NavigateLinkCommand = new RelayCommand<string>(tag =>
            {
                if (string.IsNullOrEmpty(tag)) return;
                switch (tag)
                {
                    case "quickplay": _host.NavigateToQuickPlayCommand.Execute(null); break;
                    case "altman": _host.NavigateToAltManCommand.Execute(null); break;
                    case "mods": _host.NavigateToPresetModsCommand.Execute(null); break;
                    case "appearance": _host.NavigateToAppearanceCommand.Execute(null); break;
                    case "fastflags": _host.NavigateToFastFlagsCommand.Execute(null); break;
                    case "news": _host.NavigateToNewsCommand.Execute(null); break;
                    case "serverbrowser": _host.NavigateToServerBrowserCommand.Execute(null); break;
                    case "vipserver": _host.NavigateToVipServerCommand.Execute(null); break;
                }
            });

            QuickLinks.Add(new HomeQuickLink("Quick Play", "Jump back into games", "quickplay", LucideIconNames.Gamepad2));
            QuickLinks.Add(new HomeQuickLink("AltMan", "Accounts & friends", "altman", LucideIconNames.Users));
            QuickLinks.Add(new HomeQuickLink("Mods", "Presets & custom mods", "mods", LucideIconNames.BookOpen));
            QuickLinks.Add(new HomeQuickLink("Appearance", "Theme & aurora", "appearance", LucideIconNames.Palette));
            QuickLinks.Add(new HomeQuickLink("Fast Flags", "Client tuning", "fastflags", LucideIconNames.Flag));
            QuickLinks.Add(new HomeQuickLink("News", "Roblox updates", "news", LucideIconNames.Newspaper));

            LoadRecent();
        }

        private void LoadRecent()
        {
            RecentGames.Clear();
            try
            {
                string path = Path.Combine(Paths.Cache, "GameHistory.json");
                if (!File.Exists(path))
                {
                    OnPropertyChanged(nameof(HasRecentGames));
                    return;
                }

                var entries = JsonSerializer.Deserialize<List<GameHistoryEntry>>(File.ReadAllText(path)) ?? [];
                foreach (var e in entries.Take(4))
                {
                    RecentGames.Add(new HomeRecentItem
                    {
                        PlaceId = e.PlaceId,
                        UniverseId = e.UniverseId,
                        Name = e.PlaceId > 0 ? $"Place {e.PlaceId}" : "Recent game",
                        Subtitle = e.UniverseId > 0 ? $"Universe {e.UniverseId}" : "From history"
                    });
                }
            }
            catch
            {
                /* ignore */
            }

            OnPropertyChanged(nameof(HasRecentGames));
        }
    }

    public sealed class HomeQuickLink(string title, string subtitle, string tag, LucideIconNames icon)
    {
        public string Title { get; } = title;
        public string Subtitle { get; } = subtitle;
        public string Tag { get; } = tag;
        public LucideIconNames Icon { get; } = icon;
    }

    public sealed class HomeRecentItem
    {
        public long PlaceId { get; set; }
        public long UniverseId { get; set; }
        public string Name { get; set; } = "";
        public string Subtitle { get; set; } = "";
    }
}
