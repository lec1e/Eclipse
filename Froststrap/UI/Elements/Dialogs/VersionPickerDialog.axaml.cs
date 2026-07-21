using Avalonia.Controls;
using Avalonia.Interactivity;
using Froststrap.Models.Persistable;
using Froststrap.UI.ViewModels.Dialogs;

namespace Froststrap.UI.Elements.Dialogs;

public partial class VersionPickerDialog : Window
{
    private readonly VersionPickerViewModel _vm = new();

    public VersionProfile? PickedProfile => _vm.SelectedProfile;

    public VersionPickerDialog()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);

    private void Launch_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm.SelectedProfile is null)
            return;
        Close(true);
    }
}
