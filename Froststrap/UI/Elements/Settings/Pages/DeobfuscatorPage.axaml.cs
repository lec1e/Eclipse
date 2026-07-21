using Avalonia.Controls;

namespace Froststrap.UI.Elements.Settings.Pages;

public partial class DeobfuscatorPage : UserControl
{
    public DeobfuscatorPage()
    {
        InitializeComponent();
        App.FrostRPC?.SetPage("Deobfuscator");
    }
}
