using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Froststrap.UI.ViewModels.Settings
{
    public class NewsViewModel : NotifyPropertyChangedViewModel
    {
        private const string LOG_IDENT = "NewsViewModel";

        private readonly List<AnnouncementRow> _announcements = new();
        private readonly List<ReleaseNoteRow> _releaseNotes = new();

        public NewsViewModel()
        {
            _ = LoadAnnouncementsAsync(false);
            _ = LoadReleasesAsync(false);
        }

        public ObservableCollection<AnnouncementRow> Announcements { get; } = new();

        private bool _announcementsLoading;
        public bool AnnouncementsLoading
        {
            get => _announcementsLoading;
            set { _announcementsLoading = value; OnPropertyChanged(nameof(AnnouncementsLoading)); }
        }

        private string _announcementSearch = "";
        public string AnnouncementSearch
        {
            get => _announcementSearch;
            set { _announcementSearch = value ?? ""; OnPropertyChanged(nameof(AnnouncementSearch)); RebuildAnnouncements(); }
        }

        private string _announcementsFooter = "";
        public string AnnouncementsFooter
        {
            get => _announcementsFooter;
            set { _announcementsFooter = value; OnPropertyChanged(nameof(AnnouncementsFooter)); }
        }

        public string AnnouncementsSourceUrl => "https://devforum.roblox.com/c/updates/announcements/36";

        public ICommand RefreshAnnouncementsCommand => new AsyncRelayCommand(() => LoadAnnouncementsAsync(true));

        private async Task LoadAnnouncementsAsync(bool force)
        {
            AnnouncementsLoading = true;
            AnnouncementsFooter = "Loadingù";
            try
            {
                var topics = await RobloxNewsClient.GetAnnouncementsAsync(force);
                _announcements.Clear();
                foreach (var t in topics)
                    _announcements.Add(new AnnouncementRow(t));
                RebuildAnnouncements();

                if (topics.Count == 0)
                {
                    AnnouncementsFooter = "Couldn't reach DevForum. Check your connection and refresh.";
                }
                else
                {
                    var age = DateTime.Now - RobloxNewsClient.AnnouncementsFetchedAt;
                    string cached = age.TotalMinutes < 1 ? "just now" : $"cached {(int)age.TotalMinutes} min ago";
                    AnnouncementsFooter = $"{topics.Count} topics ù {cached}";
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT + "::Announcements", ex);
                AnnouncementsFooter = "Couldn't load announcements.";
            }
            finally
            {
                AnnouncementsLoading = false;
            }
        }

        private void RebuildAnnouncements()
        {
            Announcements.Clear();
            foreach (var r in _announcements)
            {
                if (_announcementSearch.Length == 0
                    || r.Title.Contains(_announcementSearch, StringComparison.OrdinalIgnoreCase)
                    || r.Excerpt.Contains(_announcementSearch, StringComparison.OrdinalIgnoreCase))
                {
                    Announcements.Add(r);
                }
            }
        }

        public ObservableCollection<ReleaseNoteRow> ReleaseNotes { get; } = new();
        public ObservableCollection<int> Releases { get; } = new();

        private bool _suppressReleaseLoad;
        private int? _selectedRelease;
        public int? SelectedRelease
        {
            get => _selectedRelease;
            set
            {
                if (_selectedRelease == value)
                    return;
                _selectedRelease = value;
                OnPropertyChanged(nameof(SelectedRelease));
                OnPropertyChanged(nameof(ReleaseDocsUrl));
                if (!_suppressReleaseLoad && value.HasValue)
                    _ = LoadReleaseNotesAsync(value.Value, false);
            }
        }

        private bool _releaseNotesLoading;
        public bool ReleaseNotesLoading
        {
            get => _releaseNotesLoading;
            set { _releaseNotesLoading = value; OnPropertyChanged(nameof(ReleaseNotesLoading)); }
        }

        private string _releaseNoteSearch = "";
        public string ReleaseNoteSearch
        {
            get => _releaseNoteSearch;
            set { _releaseNoteSearch = value ?? ""; OnPropertyChanged(nameof(ReleaseNoteSearch)); RebuildReleaseNotes(); }
        }

        private string _typeFilter = "All";
        public string TypeFilter
        {
            get => _typeFilter;
            set { _typeFilter = value ?? "All"; OnPropertyChanged(nameof(TypeFilter)); RebuildReleaseNotes(); }
        }

        private string _statusFilter = "All";
        public string StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value ?? "All"; OnPropertyChanged(nameof(StatusFilter)); RebuildReleaseNotes(); }
        }

        public IReadOnlyList<string> TypeFilterOptions { get; } = new[] { "All", "Improvements", "Fixes" };
        public IReadOnlyList<string> StatusFilterOptions { get; } = new[] { "All", "Live", "Pending" };

        private int _impCount, _fixCount, _liveCount, _pendingCount;
        public string ImpCountText => $"Imp {_impCount}";
        public string FixCountText => $"Fix {_fixCount}";
        public string LiveCountText => $"Live {_liveCount}";
        public string PendCountText => $"Pend {_pendingCount}";
        public string ReleaseNotesSummary => $"{_liveCount} live ù {_pendingCount} pending";

        private string _releaseNotesFooter = "";
        public string ReleaseNotesFooter
        {
            get => _releaseNotesFooter;
            set { _releaseNotesFooter = value; OnPropertyChanged(nameof(ReleaseNotesFooter)); }
        }

        public string ReleaseForumUrl => "https://devforum.roblox.com/c/updates/release-notes/62";
        public string ReleaseDocsUrl =>
            _selectedRelease.HasValue
                ? $"https://create.roblox.com/docs/en-us/release-notes/release-notes-{_selectedRelease.Value}"
                : "https://create.roblox.com/docs/release-notes";

        public ICommand RefreshReleaseNotesCommand => new AsyncRelayCommand(RefreshReleasesAsync);
        public ICommand SetTypeFilterCommand => new RelayCommand<string>(s => { if (s != null) TypeFilter = s; });
        public ICommand SetStatusFilterCommand => new RelayCommand<string>(s => { if (s != null) StatusFilter = s; });

        private async Task RefreshReleasesAsync() => await LoadReleasesAsync(true);

        private async Task LoadReleasesAsync(bool force)
        {
            try
            {
                var numbers = await RobloxNewsClient.GetReleaseNumbersAsync(force);
                if (numbers.Count == 0)
                {
                    if (_releaseNotes.Count == 0)
                        ReleaseNotesFooter = "Couldn't reach the release-notes list.";
                    return;
                }

                if (!Releases.SequenceEqual(numbers))
                {
                    int? previous = _selectedRelease;
                    _suppressReleaseLoad = true;
                    Releases.Clear();
                    foreach (var n in numbers)
                        Releases.Add(n);
                    _selectedRelease = previous.HasValue && numbers.Contains(previous.Value) ? previous : numbers[0];
                    OnPropertyChanged(nameof(SelectedRelease));
                    OnPropertyChanged(nameof(ReleaseDocsUrl));
                    _suppressReleaseLoad = false;
                }

                if (_selectedRelease.HasValue)
                    await LoadReleaseNotesAsync(_selectedRelease.Value, force);
                else
                    SelectedRelease = numbers[0];
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT + "::Releases", ex);
            }
        }

        private async Task LoadReleaseNotesAsync(int release, bool force)
        {
            ReleaseNotesLoading = true;
            ReleaseNotesFooter = "Loadingù";
            try
            {
                var notes = await RobloxNewsClient.GetReleaseNotesAsync(release, force);
                if (release != _selectedRelease)
                    return;

                _releaseNotes.Clear();
                foreach (var e in notes.Entries)
                    _releaseNotes.Add(new ReleaseNoteRow(e));

                _impCount = notes.Entries.Count(e => e.Type.StartsWith("Improvement", StringComparison.OrdinalIgnoreCase));
                _fixCount = notes.Entries.Count(e => e.Type.StartsWith("Fix", StringComparison.OrdinalIgnoreCase));
                _liveCount = notes.Entries.Count(e => e.Status.Equals("Live", StringComparison.OrdinalIgnoreCase));
                _pendingCount = notes.Entries.Count(e => e.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase));
                OnPropertyChanged(nameof(ImpCountText));
                OnPropertyChanged(nameof(FixCountText));
                OnPropertyChanged(nameof(LiveCountText));
                OnPropertyChanged(nameof(PendCountText));
                OnPropertyChanged(nameof(ReleaseNotesSummary));

                RebuildReleaseNotes();

                ReleaseNotesFooter = notes.Entries.Count == 0
                    ? $"No notes found for release {release} yet."
                    : $"Release {release} ù {notes.Entries.Count} notes";
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT + "::ReleaseNotes", ex);
                if (release == _selectedRelease)
                    ReleaseNotesFooter = "Couldn't load release notes.";
            }
            finally
            {
                if (release == _selectedRelease)
                    ReleaseNotesLoading = false;
            }
        }

        private void RebuildReleaseNotes()
        {
            ReleaseNotes.Clear();
            foreach (var r in _releaseNotes)
            {
                if (!_typeFilter.Equals("All", StringComparison.OrdinalIgnoreCase)
                    && !r.Type.StartsWith(_typeFilter.TrimEnd('s'), StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!_statusFilter.Equals("All", StringComparison.OrdinalIgnoreCase)
                    && !r.Status.Equals(_statusFilter, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (_releaseNoteSearch.Length > 0
                    && !r.Text.Contains(_releaseNoteSearch, StringComparison.OrdinalIgnoreCase))
                    continue;
                ReleaseNotes.Add(r);
            }
        }
    }

    public class AnnouncementRow : NotifyPropertyChangedViewModel
    {
        private readonly RobloxNewsClient.DevForumTopic _t;
        public AnnouncementRow(RobloxNewsClient.DevForumTopic t) => _t = t;

        public string Title => _t.Title;
        public string Excerpt => _t.Excerpt;
        public string Url => _t.Url;
        public bool IsPinned => _t.Pinned;
        public string RepliesText => Compact(_t.Replies);
        public string ViewsText => Compact(_t.Views);
        public string LikesText => Compact(_t.Likes);
        public string AgeText => Age(_t.CreatedAt);

        private bool _expanded;
        public bool IsExpanded
        {
            get => _expanded;
            set
            {
                _expanded = value;
                OnPropertyChanged(nameof(IsExpanded));
                OnPropertyChanged(nameof(ChevronText));
            }
        }
        public string ChevronText => _expanded ? "?" : "?";

        public ICommand ToggleCommand => new RelayCommand(() => IsExpanded = !IsExpanded);

        private static string Compact(int n) =>
            n >= 1000 ? $"{n / 1000.0:0.#}K" : n.ToString();

        private static string Age(DateTime dt)
        {
            if (dt == DateTime.MinValue)
                return "";
            var span = DateTime.Now - dt;
            if (span.TotalMinutes < 60)
                return $"{Math.Max(1, (int)span.TotalMinutes)}m ago";
            if (span.TotalHours < 24)
                return $"{(int)span.TotalHours}h ago";
            return $"{(int)span.TotalDays}d ago";
        }
    }

    public class ReleaseNoteRow
    {
        private readonly RobloxNewsClient.ReleaseNoteEntry _e;
        public ReleaseNoteRow(RobloxNewsClient.ReleaseNoteEntry e) => _e = e;

        public string Text => _e.Text;
        public string Type => _e.Type;
        public string Status => _e.Status;
        public bool IsFix => _e.Type.StartsWith("Fix", StringComparison.OrdinalIgnoreCase);
        public string TypeBadge => IsFix ? "FIX" : "IMP";
        public bool IsPending => _e.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase);
        public bool HasStatus => !string.IsNullOrEmpty(_e.Status);
    }
}
