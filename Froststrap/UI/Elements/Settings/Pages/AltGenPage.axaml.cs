using Avalonia.Controls;

namespace Froststrap.UI.Elements.Settings.Pages;

public partial class AltGenPage : UserControl
{
    public AltGenPage()
    {
        InitializeComponent();
        App.FrostRPC?.SetPage("AltGen");
    }
}
