using Avalonia.Controls;

namespace Froststrap.UI.Elements.Settings.Pages;

public partial class HwidSpooferPage : UserControl
{
    public HwidSpooferPage()
    {
        InitializeComponent();
        App.FrostRPC?.SetPage("HWID Spoofer");
    }
}
