using Froststrap.Models.Persistable;
using System.Collections.ObjectModel;

namespace Froststrap.UI.ViewModels.Dialogs
{
    public class VersionPickerViewModel : NotifyPropertyChangedViewModel
    {
        public ObservableCollection<VersionProfile> Profiles { get; } = [];

        private VersionProfile? _selectedProfile;
        public VersionProfile? SelectedProfile
        {
            get => _selectedProfile;
            set { _selectedProfile = value; OnPropertyChanged(nameof(SelectedProfile)); }
        }

        public VersionPickerViewModel()
        {
            string activeId = App.Settings.Prop.ActiveVersionProfileId;
            foreach (var p in App.Settings.Prop.VersionProfiles)
                Profiles.Add(p);

            SelectedProfile = Profiles.FirstOrDefault(p => p.Id == activeId) ?? Profiles.FirstOrDefault();
        }
    }
}
