using Avalonia.Controls;

namespace Froststrap.UI.Elements.Settings.Pages;

public partial class NewsPage : UserControl
{
    public NewsPage()
    {
        InitializeComponent();
        App.FrostRPC?.SetPage("News");
    }
}
