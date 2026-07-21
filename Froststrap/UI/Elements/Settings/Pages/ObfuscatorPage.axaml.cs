using Avalonia.Controls;

namespace Froststrap.UI.Elements.Settings.Pages;

public partial class ObfuscatorPage : UserControl
{
    public ObfuscatorPage()
    {
        InitializeComponent();
        App.FrostRPC?.SetPage("Obfuscator");
    }
}
