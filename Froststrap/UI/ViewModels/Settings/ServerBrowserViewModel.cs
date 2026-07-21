using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Froststrap.AppData;
using Froststrap.Utility.RoValra;

namespace Froststrap.UI.ViewModels.Settings
{
    public class ServerBrowserViewModel : NotifyPropertyChangedViewModel
    {
        private const string LOG_IDENT = "ServerBrowserViewModel";
        private const string AllRegions = "All regions";

        private long _placeId;
        private string? _cursor;
        private bool _detectingAll;
        private readonly List<ServerRowViewModel> _allServers = new();

        public ServerBrowserViewModel()
        {
            RegionFilters.Add(AllRegions);
        }

        public ObservableCollection<ServerRowViewModel> Servers { get; } = new();
        public ObservableCollection<string> RegionFilters { get; } = new();

        private string _selectedRegionFilter = AllRegions;
        public string SelectedRegionFilter
        {
            get => _selectedRegionFilter;
            set
            {
                _selectedRegionFilter = string.IsNullOrEmpty(value) ? AllRegions : value;
                OnPropertyChanged(nameof(SelectedRegionFilter));
                RebuildVisible();
            }
        }

        public IReadOnlyList<string> SortOptions { get; } = new[] { "Lowest ping", "Fewest players", "Most players", "Highest FPS" };

        private string _selectedSort = "Lowest ping";
        public string SelectedSort
        {
            get => _selectedSort;
            set
            {
                _selectedSort = string.IsNullOrEmpty(value) ? "Lowest ping" : value;
                OnPropertyChanged(nameof(SelectedSort));
                RebuildVisible();
            }
        }

        private int FetchSortOrder => _selectedSort == "Fewest players" ? 1 : 2;

        private string _placeIdInput = "";
        public string PlaceIdInput
        {
            get => _placeIdInput;
            set { _placeIdInput = value; OnPropertyChanged(nameof(PlaceIdInput)); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); OnPropertyChanged(nameof(NotBusy)); }
        }
        public bool NotBusy => !_isBusy;

        private string _status = "";
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); OnPropertyChanged(nameof(HasStatus)); }
        }
        public bool HasStatus => !string.IsNullOrEmpty(_status);

        public bool HasServers => Servers.Count > 0;
        public bool CanLoadMore => !string.IsNullOrEmpty(_cursor);

        public ICommand LoadCommand => new AsyncRelayCommand(() => LoadServersAsync(false));
        public ICommand LoadMoreCommand => new AsyncRelayCommand(() => LoadServersAsync(true));
        public ICommand DetectRegionCommand => new AsyncRelayCommand<ServerRowViewModel?>(DetectRegionAsync);
        public ICommand DetectAllRegionsCommand => new AsyncRelayCommand(DetectAllRegionsAsync);
        public ICommand JoinCommand => new RelayCommand<ServerRowViewModel?>(JoinServer);
        public ICommand JoinEmptiestCommand => new AsyncRelayCommand(JoinEmptiestAsync);

        private static long ParsePlaceId(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return 0;

            input = input.Trim();
            if (long.TryParse(input, out long direct) && direct > 0)
                return direct;

            var match = Regex.Match(input, @"games/(\d{3,19})", RegexOptions.IgnoreCase);
            if (match.Success && long.TryParse(match.Groups[1].Value, out long fromUrl))
                return fromUrl;

            var digits = Regex.Match(input, @"\d{3,19}");
            return digits.Success && long.TryParse(digits.Value, out long any) ? any : 0;
        }

        private async Task LoadServersAsync(bool append)
        {
            if (IsBusy)
                return;

            long placeId = append ? _placeId : ParsePlaceId(PlaceIdInput);
            if (placeId <= 0)
            {
                Status = "Enter a Roblox place ID or paste a game link first.";
                return;
            }

            IsBusy = true;
            Status = append ? "Loading more serversù" : "Loading serversù";

            try
            {
                var resp = await ServerBrowserClient.FetchServersAsync(placeId, append ? _cursor : null, FetchSortOrder);

                if (resp is null)
                {
                    Status = "Couldn't load servers ù Roblox's API didn't answer. Try again in a moment.";
                    return;
                }

                if (!append)
                {
                    _allServers.Clear();
                    ResetRegionFilters();
                }

                foreach (var server in resp.Data)
                    _allServers.Add(new ServerRowViewModel(placeId, server));

                _placeId = placeId;
                _cursor = resp.NextPageCursor;

                await EnrichWithRoValraAsync(
                    placeId,
                    append ? _allServers.TakeLast(resp.Data.Count).ToList() : _allServers);

                RebuildVisible();
                OnPropertyChanged(nameof(CanLoadMore));

                Status = _allServers.Count == 0
                    ? "No public servers are listed for this experience right now."
                    : $"Showing {Servers.Count} server{(Servers.Count == 1 ? "" : "s")} (ping via RoValra).";
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
                Status = $"Couldn't load servers ù {ex.GetType().Name}: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DetectRegionAsync(ServerRowViewModel? row)
        {
            if (row is null || row.IsDetecting || row.HasRegion)
                return;

            row.IsDetecting = true;
            try
            {
                await ResolveRowRegionAsync(row);
                Status = row.HasRegion
                    ? $"Server is in {row.Region}."
                    : "Couldn't detect that server's region.";
            }
            finally
            {
                row.IsDetecting = false;
                RebuildRegionFilters();
            }
        }

        private async Task DetectAllRegionsAsync()
        {
            if (_detectingAll || IsBusy)
                return;

            const int cap = 50;
            var pending = _allServers.Where(s => !s.HasRegion && !s.IsDetecting).ToList();
            int skipped = Math.Max(0, pending.Count - cap);
            pending = pending.Take(cap).ToList();

            if (pending.Count == 0)
            {
                Status = "Every loaded server already has a region.";
                return;
            }

            _detectingAll = true;
            try
            {
                Status = "Resolving regions via RoValraÖ";
                await EnrichWithRoValraAsync(_placeId, pending);
                int done = pending.Count(s => s.HasRegion);
                RebuildRegionFilters();
                Status = skipped > 0
                    ? $"Resolved {done}/{pending.Count} via RoValra ({skipped} not attempted this pass)."
                    : $"Resolved {done}/{pending.Count} region(s) via RoValra.";
            }
            finally
            {
                _detectingAll = false;
                RebuildRegionFilters();
            }
        }

        private static async Task EnrichWithRoValraAsync(long placeId, IList<ServerRowViewModel> rows)
        {
            if (rows.Count == 0)
                return;

            try
            {
                _ = await RegionPingService.GetRegionsAsync(measureLive: false);
                var details = await RoValraApi.GetServerDetailsAsync(placeId, rows.Select(r => r.JobId));
                foreach (var row in rows)
                {
                    if (!details.TryGetValue(row.JobId, out var info))
                        continue;

                    string? city = info.City;
                    string? country = info.Country;
                    if (string.IsNullOrWhiteSpace(city) && info.DatacenterId is int dcId)
                    {
                        var byDc = RegionPingService.FindByDatacenterId(dcId);
                        if (byDc is not null)
                        {
                            row.Region = byDc.DisplayName;
                            row.AccuratePingMs = byDc.PingMs;
                            row.PingIsEstimated = !byDc.IsMeasured;
                            continue;
                        }
                    }

                    var region = RegionPingService.FindByCityCountry(city, country);
                    if (region is not null)
                    {
                        row.Region = region.DisplayName;
                        row.AccuratePingMs = region.PingMs;
                        row.PingIsEstimated = !region.IsMeasured;
                    }
                    else if (!string.IsNullOrWhiteSpace(city))
                    {
                        var parts = new[] { city, info.Region, country }.Where(p => !string.IsNullOrWhiteSpace(p));
                        row.Region = string.Join(", ", parts.Distinct());
                        row.AccuratePingMs = RegionPingService.LookupPingMs(city);
                        row.PingIsEstimated = true;
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT + "::EnrichRoValra", ex);
            }
        }

        private static async Task ResolveRowRegionAsync(ServerRowViewModel row)
        {
            try
            {
                _ = await RegionPingService.GetRegionsAsync(measureLive: false);
                var details = await RoValraApi.GetServerDetailsAsync(row.PlaceId, new[] { row.JobId });
                if (details.TryGetValue(row.JobId, out var info))
                {
                    var regionInfo = info.DatacenterId is int dc
                        ? RegionPingService.FindByDatacenterId(dc)
                        : RegionPingService.FindByCityCountry(info.City, info.Country);
                    if (regionInfo is not null)
                    {
                        row.Region = regionInfo.DisplayName;
                        row.AccuratePingMs = regionInfo.PingMs;
                        row.PingIsEstimated = !regionInfo.IsMeasured;
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(info.City))
                    {
                        row.Region = string.Join(", ", new[] { info.City, info.Region, info.Country }
                            .Where(p => !string.IsNullOrWhiteSpace(p)).Distinct());
                        row.AccuratePingMs = RegionPingService.LookupPingMs(info.City);
                        row.PingIsEstimated = true;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT + "::ResolveRoValra", ex);
            }

            string? ip = await ServerBrowserClient.ResolveServerIpAsync(row.PlaceId, row.JobId);
            if (string.IsNullOrEmpty(ip))
                return;

            string? region = await RobloxDatacenters.ResolveRegionAsync(ip);
            row.Region = string.IsNullOrEmpty(region) ? "Unknown" : region;
            row.AccuratePingMs = RegionPingService.LookupPingMs(region);
            row.PingIsEstimated = true;
        }

        private void JoinServer(ServerRowViewModel? row)
        {
            if (row is null)
                return;
            JoinInstance(row.PlaceId, row.JobId);
        }

        private void JoinInstance(long placeId, string jobId)
        {
            try
            {
                string playerPath = new RobloxPlayerData().ExecutablePath;
                string deeplink = $"roblox://experiences/start?placeId={placeId}&gameInstanceId={jobId}";
                Process.Start(playerPath, deeplink);

                string shortId = jobId.Length > 8 ? jobId[..8] : jobId;
                Status = $"Joining server {shortId}ù Roblox is launching.";
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT + "::JoinInstance", ex);
                Status = $"Couldn't launch Roblox ù {ex.GetType().Name}: {ex.Message}";
            }
        }

        private async Task JoinEmptiestAsync()
        {
            if (IsBusy)
                return;

            long placeId = ParsePlaceId(PlaceIdInput);
            if (placeId <= 0)
            {
                Status = "Enter a Roblox place ID or paste a game link first.";
                return;
            }

            IsBusy = true;
            Status = "Finding the emptiest serverù";
            try
            {
                var server = await ServerBrowserClient.GetEmptiestServerAsync(placeId);
                if (server is null || string.IsNullOrEmpty(server.Id))
                {
                    Status = "Couldn't find a joinable public server right now.";
                    return;
                }

                Status = $"Joining the emptiest server ({server.Playing}/{server.MaxPlayers})ù";
                JoinInstance(placeId, server.Id);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT + "::JoinEmptiest", ex);
                Status = $"Couldn't find a server ù {ex.GetType().Name}: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ResetRegionFilters()
        {
            RegionFilters.Clear();
            RegionFilters.Add(AllRegions);
            _selectedRegionFilter = AllRegions;
            OnPropertyChanged(nameof(SelectedRegionFilter));
        }

        private void RebuildRegionFilters()
        {
            var distinct = _allServers
                .Where(s => s.HasRegion)
                .Select(s => s.Region)
                .Distinct()
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            string keep = _selectedRegionFilter;
            RegionFilters.Clear();
            RegionFilters.Add(AllRegions);
            foreach (var region in distinct)
                RegionFilters.Add(region);

            if (!RegionFilters.Contains(keep))
                keep = AllRegions;

            _selectedRegionFilter = keep;
            OnPropertyChanged(nameof(SelectedRegionFilter));
            RebuildVisible();
        }

        private void RebuildVisible()
        {
            IEnumerable<ServerRowViewModel> query = _allServers;
            if (_selectedRegionFilter != AllRegions)
                query = query.Where(s => s.Region == _selectedRegionFilter);

            query = _selectedSort switch
            {
                "Fewest players" => query.OrderBy(s => s.Playing),
                "Most players" => query.OrderByDescending(s => s.Playing),
                "Highest FPS" => query.OrderByDescending(s => s.Fps),
                _ => query.OrderBy(s => s.PingSortKey),
            };

            Servers.Clear();
            foreach (var s in query)
                Servers.Add(s);

            OnPropertyChanged(nameof(HasServers));
        }
    }

    public class ServerRowViewModel : NotifyPropertyChangedViewModel
    {
        public long PlaceId { get; }
        public string JobId { get; }
        public int Playing { get; }
        public int MaxPlayers { get; }
        public int Ping { get; }
        public double Fps { get; }

        public ServerRowViewModel(long placeId, GameServer server)
        {
            PlaceId = placeId;
            JobId = server.Id;
            Playing = server.Playing;
            MaxPlayers = server.MaxPlayers;
            Ping = server.Ping;
            Fps = server.Fps;
        }

        public string PlayersText => MaxPlayers > 0 ? $"{Playing}/{MaxPlayers}" : Playing.ToString();

        private int? _accuratePingMs;
        private bool _pingIsEstimated = true;
        public int? AccuratePingMs
        {
            get => _accuratePingMs;
            set
            {
                if (SetProperty(ref _accuratePingMs, value))
                {
                    OnPropertyChanged(nameof(PingText));
                    OnPropertyChanged(nameof(PingSortKey));
                }
            }
        }

        public bool PingIsEstimated
        {
            get => _pingIsEstimated;
            set
            {
                if (SetProperty(ref _pingIsEstimated, value))
                    OnPropertyChanged(nameof(PingText));
            }
        }

        public string PingText
        {
            get
            {
                if (AccuratePingMs is > 0)
                    return PingIsEstimated ? $"~{AccuratePingMs} ms" : $"{AccuratePingMs} ms";
                return Ping > 0 ? $"{Ping} ms*" : "ù";
            }
        }

        public string FpsText => Fps > 0 ? $"{Fps:0} FPS" : "ù";
        public int PingSortKey => AccuratePingMs is > 0 ? AccuratePingMs.Value : (Ping > 0 ? Ping + 10_000 : int.MaxValue);

        private string _region = "";
        public string Region
        {
            get => _region;
            set
            {
                _region = value ?? "";
                OnPropertyChanged(nameof(Region));
                OnPropertyChanged(nameof(RegionDisplay));
                OnPropertyChanged(nameof(HasRegion));
                OnPropertyChanged(nameof(CanDetect));
            }
        }
        public bool HasRegion => !string.IsNullOrEmpty(_region);
        public string RegionDisplay => string.IsNullOrEmpty(_region) ? "ù" : _region;

        private bool _isDetecting;
        public bool IsDetecting
        {
            get => _isDetecting;
            set
            {
                _isDetecting = value;
                OnPropertyChanged(nameof(IsDetecting));
                OnPropertyChanged(nameof(CanDetect));
            }
        }

        public bool CanDetect => !_isDetecting && !HasRegion;
    }
}
