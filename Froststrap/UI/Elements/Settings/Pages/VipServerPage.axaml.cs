using Avalonia.Controls;

namespace Froststrap.UI.Elements.Settings.Pages;

public partial class VipServerPage : UserControl
{
    public VipServerPage()
    {
        InitializeComponent();
        App.FrostRPC?.SetPage("VipServer");
    }
}
