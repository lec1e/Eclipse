using Avalonia.Controls;
using Avalonia.Interactivity;
using Froststrap.Models.Persistable;
using Froststrap.UI.ViewModels.Dialogs;

namespace Froststrap.UI.Elements.Dialogs;

public partial class AddVersionProfileDialog : Window
{
    private readonly AddVersionProfileViewModel _vm = new();

    public AddVersionProfileDialog()
    {
        InitializeComponent();
        DataContext = _vm;
        _vm.Completed += profile => Close(profile);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
}
