using System.ComponentModel;
using System.Runtime.CompilerServices;
using LucideAvalonia.Enum;

namespace Froststrap.UI.Elements.Settings
{
    public sealed class RailNavItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isVisible = true;
        private bool _isEnabled = true;

        public string Tag { get; init; } = "";
        public string Title { get; init; } = "";
        public LucideIconNames Icon { get; init; }
        public bool IsSeparator { get; init; }

        public bool IsVisible
        {
            get => _isVisible;
            set { if (_isVisible == value) return; _isVisible = value; OnPropertyChanged(); }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set { if (_isEnabled == value) return; _isEnabled = value; OnPropertyChanged(); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { if (_isSelected == value) return; _isSelected = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
