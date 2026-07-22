using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Media;
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

            // placeId used for launch; tint matches mockup card moods
            RecentGames.Add(new HomeGameCard("Midnight Rail", "Last played recently", 4924922222, "#4C1D95"));
            RecentGames.Add(new HomeGameCard("Deepwoken", "Last played 2h ago", 5735554555, "#1E293B"));
            RecentGames.Add(new HomeGameCard("Catalog Avatar Creator", "Last played 5h ago", 16617882081, "#9A3412"));
            RecentGames.Add(new HomeGameCard("Da Hood", "Last played 1d ago", 2788229376, "#0F172A"));

            FeaturedGames.Add(new HomeFeaturedItem(
                "Midnight Rail",
                "Neon trains and night city runs.",
                "94%",
                "12.4K",
                4924922222,
                "#312E81"));
            FeaturedGames.Add(new HomeFeaturedItem(
                "The Strongest Battlegrounds",
                "Anime battling with flashy combat.",
                "83%",
                "45.7K",
                10450266301,
                "#7F1D1D"));

            NewsItems.Add(new HomeNewsItem("Eclipse v1.0.0 Released", "Midnight Rail UI and Home dashboard", "2d ago", LucideIconNames.Sparkles));
            NewsItems.Add(new HomeNewsItem("Roblox Update", "Client and platform notes", "5d ago", LucideIconNames.Box));
            NewsItems.Add(new HomeNewsItem("Maintenance Scheduled", "Brief downtime window this week", "1w ago", LucideIconNames.Wrench));
        }

        private async Task LoadThumbnailsAsync()
        {
            try
            {
                var cards = RecentGames.Cast<HomeThumbTarget>().Concat(FeaturedGames).ToList();
                var requests = cards.Select(c => new ThumbnailRequest
                {
                    TargetId = (ulong)c.PlaceId,
                    Type = ThumbnailType.PlaceIcon,
                    Size = "512x512",
                    Format = ThumbnailFormat.Png
                }).ToList();

                // Prefer wide game thumbnails when the batch accepts place/universe ids
                var wideRequests = cards.Select(c => new ThumbnailRequest
                {
                    TargetId = (ulong)c.PlaceId,
                    Type = ThumbnailType.GameThumbnail,
                    Size = "768x432",
                    Format = ThumbnailFormat.Png
                }).ToList();

                var urls = await Thumbnails.GetThumbnailUrlsAsync(wideRequests, CancellationToken.None);
                var missing = new List<ThumbnailRequest>();
                var missingIdx = new List<int>();
                for (int i = 0; i < cards.Count; i++)
                {
                    if (!string.IsNullOrEmpty(urls.ElementAtOrDefault(i)))
                        cards[i].ImageUrl = urls[i]!;
                    else
                    {
                        missing.Add(requests[i]);
                        missingIdx.Add(i);
                    }
                }

                if (missing.Count > 0)
                {
                    var fallback = await Thumbnails.GetThumbnailUrlsAsync(missing, CancellationToken.None);
                    for (int j = 0; j < missingIdx.Count && j < fallback.Length; j++)
                    {
                        if (!string.IsNullOrEmpty(fallback[j]))
                            cards[missingIdx[j]].ImageUrl = fallback[j]!;
                    }
                }

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
        long PlaceId { get; }
        string ImageUrl { get; set; }
        void NotifyImage();
    }

    public sealed class HomeGameCard : NotifyPropertyChangedViewModel, HomeThumbTarget
    {
        public HomeGameCard(string name, string subtitle, long placeId, string tintHex)
        {
            Name = name;
            Subtitle = subtitle;
            PlaceId = placeId;
            TintBrush = new SolidColorBrush(Color.Parse(tintHex));
        }

        public string Name { get; }
        public string Subtitle { get; }
        public long PlaceId { get; }
        public IBrush TintBrush { get; }
        public string ImageUrl { get; set; } = "";
        public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);
        public void NotifyImage()
        {
            OnPropertyChanged(nameof(ImageUrl));
            OnPropertyChanged(nameof(HasImage));
        }
    }

    public sealed class HomeFeaturedItem : NotifyPropertyChangedViewModel, HomeThumbTarget
    {
        public HomeFeaturedItem(string name, string description, string rating, string players, long placeId, string tintHex)
        {
            Name = name;
            Description = description;
            Rating = rating;
            Players = players;
            PlaceId = placeId;
            TintBrush = new SolidColorBrush(Color.Parse(tintHex));
        }

        public string Name { get; }
        public string Description { get; }
        public string Rating { get; }
        public string Players { get; }
        public long PlaceId { get; }
        public IBrush TintBrush { get; }
        public string ImageUrl { get; set; } = "";
        public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);
        public void NotifyImage()
        {
            OnPropertyChanged(nameof(ImageUrl));
            OnPropertyChanged(nameof(HasImage));
        }
    }

    public sealed class HomeNewsItem(string title, string subtitle, string when, LucideIconNames icon)
    {
        public string Title { get; } = title;
        public string Subtitle { get; } = subtitle;
        public string When { get; } = when;
        public LucideIconNames Icon { get; } = icon;
    }
}
