using CommunityToolkit.Mvvm.Input;
using Froststrap.Models.APIs;
using Froststrap.Models.Persistable;
using Froststrap.Utility;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Froststrap.UI.ViewModels.Dialogs
{
    public class AddVersionProfileViewModel : NotifyPropertyChangedViewModel
    {
        public event Action<VersionProfile>? Completed;

        public ObservableCollection<string> Modes { get; } = ["From executor (WEAO)", "Manual version hash"];

        private string _selectedMode = "From executor (WEAO)";
        public string SelectedMode
        {
            get => _selectedMode;
            set
            {
                _selectedMode = value;
                OnPropertyChanged(nameof(SelectedMode));
                OnPropertyChanged(nameof(IsExecutorMode));
                OnPropertyChanged(nameof(IsManualMode));
            }
        }

        public bool IsExecutorMode => SelectedMode.StartsWith("From executor", StringComparison.Ordinal);
        public bool IsManualMode => !IsExecutorMode;

        public string ProfileName { get; set; } = "";
        public string ManualVersionGuid { get; set; } = "";

        public ObservableCollection<WeaoExploit> Exploits { get; } = [];

        private WeaoExploit? _selectedExploit;
        public WeaoExploit? SelectedExploit
        {
            get => _selectedExploit;
            set
            {
                _selectedExploit = value;
                OnPropertyChanged(nameof(SelectedExploit));
                if (value is not null && string.IsNullOrWhiteSpace(ProfileName))
                    ProfileName = value.Title;
            }
        }

        private string _status = "";
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public ICommand LoadExecutorsCommand => new AsyncRelayCommand(LoadExecutorsAsync);
        public ICommand ConfirmCommand => new RelayCommand(Confirm);

        private async Task LoadExecutorsAsync()
        {
            Status = "Loading…";
            Exploits.Clear();
            var result = await WeaoClient.GetWindowsExploitsAsync();
            if (!result.Success)
            {
                Status = result.Error ?? "Failed to load.";
                return;
            }

            foreach (var e in result.Exploits)
                Exploits.Add(e);

            Status = result.Source == WeaoSource.Mirror
                ? $"Loaded {Exploits.Count} from robloxscripts.com mirror."
                : $"Loaded {Exploits.Count} from weao.xyz.";
        }

        private void Confirm()
        {
            string name = ProfileName.Trim();
            if (string.IsNullOrEmpty(name))
            {
                Status = "Enter a profile name.";
                return;
            }

            if (IsExecutorMode)
            {
                if (SelectedExploit is null)
                {
                    Status = "Select an executor first.";
                    return;
                }

                Completed?.Invoke(new VersionProfile
                {
                    Name = name,
                    VersionGuid = SelectedExploit.RbxVersion,
                    ExecutorTitle = SelectedExploit.Title,
                    ExecutorLogoUrl = SelectedExploit.Slug?.Logo,
                    ExecutorRefreshKey = SelectedExploit.Title,
                    LastExecutorRefreshUtc = DateTime.UtcNow
                });
                return;
            }

            string guid = ManualVersionGuid.Trim();
            if (!VersionGuidValidator.IsWellFormed(guid))
            {
                Status = "Version hash must look like version- followed by 16 hex characters.";
                return;
            }

            Completed?.Invoke(new VersionProfile
            {
                Name = name,
                VersionGuid = guid
            });
        }
    }
}
