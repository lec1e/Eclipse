using Avalonia.Controls;
using Froststrap.UI.Elements.Dialogs;
using Froststrap.UI.ViewModels.Settings;

namespace Froststrap.UI.Elements.Settings.Pages;

public partial class VersionsManagerPage : UserControl
{
    public VersionsManagerPage()
    {
        InitializeComponent();
        App.FrostRPC?.SetPage("Versions Manager");

        DataContextChanged += async (_, _) =>
        {
            if (DataContext is VersionsManagerViewModel vm)
            {
                vm.AddProfileRequested = async () =>
                {
                    var dialog = new AddVersionProfileDialog();
                    var top = TopLevel.GetTopLevel(this) as Window;
                    if (top is null) return;
                    var result = await dialog.ShowDialog<Models.Persistable.VersionProfile?>(top);
                    if (result is not null)
                        vm.AddProfileFromDialog(result);
                };
            }
        };
    }
}
