using Avalonia.Controls;
using Avalonia.Threading;
using Froststrap.UI.ViewModels.Settings;
using Froststrap.UI.ViewModels.Settings.AltMan;

namespace Froststrap.UI.Elements.Settings.Pages;

public partial class AltManPage : UserControl
{
    public AltManPage()
    {
        InitializeComponent();
        App.FrostRPC?.SetPage("AltMan");

        DataContextChanged += (_, _) => WireAccountsGrid();
        AttachedToVisualTree += (_, _) => WireAccountsGrid();
    }

    private void WireAccountsGrid()
    {
        if (this.FindControl<DataGrid>("AccountsGrid") is not { } grid)
            return;

        grid.CellEditEnded -= OnAccountsCellEditEnded;
        grid.CellEditEnded += OnAccountsCellEditEnded;
    }

    private void OnAccountsCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
    {
        if (DataContext is not AltManViewModel shell)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            foreach (var a in shell.Accounts.Accounts)
                shell.Accounts.SyncSelectionFromRow(a);
            shell.Accounts.SaveNotesCommand.Execute(null);
        });
    }
}
