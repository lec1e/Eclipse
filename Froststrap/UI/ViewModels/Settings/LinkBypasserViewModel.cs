using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Froststrap.UI.ViewModels.Settings
{
    public class LinkBypasserViewModel : NotifyPropertyChangedViewModel
    {
        private const string LOG_IDENT = "LinkBypasser";

        private string _linkInput = "";
        public string LinkInput
        {
            get => _linkInput;
            set { _linkInput = value ?? ""; OnPropertyChanged(nameof(LinkInput)); }
        }

        private string _resultUrl = "";
        public string ResultUrl
        {
            get => _resultUrl;
            set
            {
                _resultUrl = value ?? "";
                OnPropertyChanged(nameof(ResultUrl));
                OnPropertyChanged(nameof(HasResult));
            }
        }
        public bool HasResult => _resultUrl.Length > 0;

        public string ApiKey
        {
            get => App.Settings.Prop.BypassToolsApiKey;
            set { App.Settings.Prop.BypassToolsApiKey = value ?? ""; OnPropertyChanged(nameof(ApiKey)); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); OnPropertyChanged(nameof(NotBusy)); }
        }
        public bool NotBusy => !_isBusy;

        private string _statusText = "";
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(nameof(StatusText)); }
        }

        private bool _isError;
        public bool IsError
        {
            get => _isError;
            set { _isError = value; OnPropertyChanged(nameof(IsError)); }
        }

        public ICommand BypassCommand => new AsyncRelayCommand(Bypass);
        public ICommand PasteCommand => new AsyncRelayCommand(PasteLinkAsync);
        public ICommand CopyResultCommand => new AsyncRelayCommand(CopyResultAsync);

        private async Task Bypass()
        {
            IsBusy = true;
            IsError = false;
            ResultUrl = "";
            StatusText = "Bypassing… some links can take up to a minute.";
            try
            {
                var r = await BypassToolsClient.BypassAsync(LinkInput, ApiKey);
                if (r.Success)
                {
                    ResultUrl = r.ResultUrl;
                    IsError = false;
                    StatusText = r.Cached ? "Done — pulled from cache." : "Done.";
                }
                else
                {
                    IsError = true;
                    StatusText = r.Error ?? "Couldn't bypass that link.";
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task PasteLinkAsync()
        {
            try
            {
                string? text = await ScriptToolViewModel.GetClipboardAsync();
                if (!string.IsNullOrEmpty(text))
                    LinkInput = text.Trim();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private async Task CopyResultAsync()
        {
            if (!HasResult)
                return;
            try
            {
                await ScriptToolViewModel.SetClipboardAsync(ResultUrl);
                IsError = false;
                StatusText = "Copied the destination link to your clipboard.";
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }
    }
}
