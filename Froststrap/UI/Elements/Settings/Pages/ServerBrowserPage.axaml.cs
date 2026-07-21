using Avalonia.Controls;

namespace Froststrap.UI.Elements.Settings.Pages;

public partial class ServerBrowserPage : UserControl
{
    public ServerBrowserPage()
    {
        InitializeComponent();
        App.FrostRPC?.SetPage("ServerBrowser");
    }
}
