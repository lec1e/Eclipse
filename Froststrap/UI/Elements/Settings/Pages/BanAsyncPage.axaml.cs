using Avalonia.Controls;

namespace Froststrap.UI.Elements.Settings.Pages;

public partial class BanAsyncPage : UserControl
{
    public BanAsyncPage()
    {
        InitializeComponent();
        App.FrostRPC?.SetPage("BanAsync");
    }
}
