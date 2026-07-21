using Froststrap.Integrations;
using Froststrap.UI.ViewModels.Settings.AltMan;

namespace Froststrap.UI.ViewModels.Settings
{
    /// <summary>AltMan suite shell — Accounts / Friends / Games / History / Settings.</summary>
    public class AltManViewModel : NotifyPropertyChangedViewModel
    {
        private int _selectedTabIndex;
        private CancellationTokenSource? _autoRefreshCts;

        public AltManAccountsViewModel Accounts { get; }
        public AltManFriendsViewModel Friends { get; }
        public AltManGamesViewModel Games { get; }
        public AltManHistoryViewModel History { get; }
        public AltManSettingsViewModel SettingsTab { get; }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (SetProperty(ref _selectedTabIndex, value))
                    OnTabChanged(value);
            }
        }

        public AltManViewModel()
        {
            Accounts = new AltManAccountsViewModel();
            Friends = new AltManFriendsViewModel(this);
            Games = new AltManGamesViewModel(this);
            History = new AltManHistoryViewModel(this);
            SettingsTab = new AltManSettingsViewModel(this);

            StartAutoRefresh();
            AccountManager.Shared.ActiveAccountChanged += _ => Friends.NotifyActiveChanged();
        }

        private void OnTabChanged(int index)
        {
            switch (index)
            {
                case 1: _ = Friends.EnsureLoadedAsync(); break;
                case 3: History.Refresh(); break;
            }
        }

        private void StartAutoRefresh()
        {
            _autoRefreshCts?.Cancel();
            _autoRefreshCts = new CancellationTokenSource();
            var token = _autoRefreshCts.Token;
            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    int minutes = Math.Max(1, App.Settings.Prop.AltManStatusRefreshIntervalMinutes);
                    try { await Task.Delay(TimeSpan.FromMinutes(minutes), token); }
                    catch (OperationCanceledException) { break; }

                    try
                    {
                        foreach (var a in AccountManager.Shared.Accounts.ToList())
                            await AccountManager.RefreshAccountDetailsAsync(a);
                        AccountManager.Shared.Save();
                    }
                    catch { /* ignore background failures */ }
                }
            }, token);
        }
    }
}
