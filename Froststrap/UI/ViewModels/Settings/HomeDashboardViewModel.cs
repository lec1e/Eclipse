using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Froststrap.Enums;
using Froststrap.Models.APIs.Roblox;
using LucideAvalonia.Enum;

namespace Froststrap.UI.ViewModels.Settings
{
    public class HomeDashboardViewModel : NotifyPropertyChangedViewModel
    {
        private readonly MainWindowViewModel _host;

        public string Greeting => "Welcome back";
        public string DisplayName => "Eclipse User";
        public string Tagline => "Your Roblox bootstrapper for a better experience.";
        public string VersionText => $"v{App.Version}";

        public ObservableCollection<HomeGameCard> RecentGames { get; } = [];
        public ObservableCollection<HomeFeaturedItem> FeaturedGames { get; } = [];
        public ObservableCollection<HomeNewsItem> NewsItems { get; } = [];

        public ICommand LaunchRobloxCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand OpenQuickPlayCommand { get; }
        public ICommand OpenNewsCommand { get; }
        public ICommand PlayPlaceCommand { get; }

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
            PlayPlaceCommand = new RelayCommand<long?>(placeId =>
            {
                if (placeId is null or <= 0) return;
                try
                {
                    Utilities.ShellExecute($"roblox://experiences/start?placeId={placeId}");
                }
                catch { /* ignore */ }
            });

            SeedContent();
            _ = LoadThumbnailsAsync();
        }

        private void SeedContent()
        {
            RecentGames.Clear();
            FeaturedGames.Clear();
            NewsItems.Clear();

            // Showcase cards matching mockup layout (popular experiences).
            RecentGames.Add(new HomeGameCard("Midnight Rail", "Last played recently", 4924922222, 4924922222));
            RecentGames.Add(new HomeGameCard("Deepwoken", "Last played 2h ago", 5735554555, 5735554555));
            RecentGames.Add(new HomeGameCard("Catalog Avatar Creator", "Last played 5h ago", 16617882081, 16617882081));
            RecentGames.Add(new HomeGameCard("Da Hood", "Last played 1d ago", 2788229376, 2788229376));

            FeaturedGames.Add(new HomeFeaturedItem(
                "Brookhaven",
                "Build, customize, and drive through the night.",
                "94%",
                "12.4K",
                4924922222,
                4924922222));
            FeaturedGames.Add(new HomeFeaturedItem(
                "The Strongest Battlegrounds",
                "Anime battling with flashy combat.",
                "83%",
                "45.7K",
                10450266301,
                10450266301));

            NewsItems.Add(new HomeNewsItem("Eclipse v1.0.0 Released", "Midnight Rail UI and Home dashboard", "2 days ago", LucideIconNames.Sparkles));
            NewsItems.Add(new HomeNewsItem("Roblox Update", "Client and platform notes", "5 days ago", LucideIconNames.Box));
            NewsItems.Add(new HomeNewsItem("Maintenance Scheduled", "Brief downtime window this week", "1 week ago", LucideIconNames.Wrench));
        }

        private async Task LoadThumbnailsAsync()
        {
            try
            {
                var cards = RecentGames.Cast<HomeThumbTarget>().Concat(FeaturedGames).ToList();
                var requests = cards.Select((c, i) => new ThumbnailRequest
                {
                    TargetId = (ulong)Math.Max(c.UniverseId, c.PlaceId),
                    Type = i < RecentGames.Count ? ThumbnailType.GameIcon : ThumbnailType.GameThumbnail,
                    Size = i < RecentGames.Count ? "512x512" : "768x432",
                    Format = ThumbnailFormat.Png
                }).ToList();

                var urls = await Thumbnails.GetThumbnailUrlsAsync(requests, CancellationToken.None);
                for (int i = 0; i < cards.Count && i < urls.Length; i++)
                {
                    if (!string.IsNullOrEmpty(urls[i]))
                        cards[i].ImageUrl = urls[i]!;
                }

                // Refresh bindings
                OnPropertyChanged(nameof(RecentGames));
                OnPropertyChanged(nameof(FeaturedGames));
                foreach (var c in RecentGames) c.NotifyImage();
                foreach (var c in FeaturedGames) c.NotifyImage();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("HomeDashboardViewModel", $"Thumbnail load failed: {ex.Message}");
            }
        }
    }

    public interface HomeThumbTarget
    {
        long UniverseId { get; }
        long PlaceId { get; }
        string ImageUrl { get; set; }
        void NotifyImage();
    }

    public sealed class HomeGameCard : NotifyPropertyChangedViewModel, HomeThumbTarget
    {
        public HomeGameCard(string name, string subtitle, long universeId, long placeId)
        {
            Name = name;
            Subtitle = subtitle;
            UniverseId = universeId;
            PlaceId = placeId;
        }

        public string Name { get; }
        public string Subtitle { get; }
        public long UniverseId { get; }
        public long PlaceId { get; }
        public string ImageUrl { get; set; } = "";
        public void NotifyImage() => OnPropertyChanged(nameof(ImageUrl));
    }

    public sealed class HomeFeaturedItem : NotifyPropertyChangedViewModel, HomeThumbTarget
    {
        public HomeFeaturedItem(string name, string description, string rating, string players, long universeId, long placeId)
        {
            Name = name;
            Description = description;
            Rating = rating;
            Players = players;
            UniverseId = universeId;
            PlaceId = placeId;
        }

        public string Name { get; }
        public string Description { get; }
        public string Rating { get; }
        public string Players { get; }
        public long UniverseId { get; }
        public long PlaceId { get; }
        public string ImageUrl { get; set; } = "";
        public void NotifyImage() => OnPropertyChanged(nameof(ImageUrl));
    }

    public sealed class HomeNewsItem(string title, string subtitle, string when, LucideIconNames icon)
    {
        public string Title { get; } = title;
        public string Subtitle { get; } = subtitle;
        public string When { get; } = when;
        public LucideIconNames Icon { get; } = icon;
    }
}
