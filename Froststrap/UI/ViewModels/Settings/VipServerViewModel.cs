namespace Froststrap.UI.ViewModels.Settings
{
    public class VipServerViewModel : NotifyPropertyChangedViewModel
    {
        public bool VipServerPromptEnabled
        {
            get => App.Settings.Prop.EnableVipServerPrompt;
            set
            {
                App.Settings.Prop.EnableVipServerPrompt = value;
                OnPropertyChanged(nameof(VipServerPromptEnabled));
            }
        }
    }
}
