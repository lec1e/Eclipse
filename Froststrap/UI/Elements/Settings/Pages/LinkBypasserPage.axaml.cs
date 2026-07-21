using Avalonia.Controls;

namespace Froststrap.UI.Elements.Settings.Pages;

public partial class LinkBypasserPage : UserControl
{
    public LinkBypasserPage()
    {
        InitializeComponent();
        App.FrostRPC?.SetPage("LinkBypasser");
    }
}
