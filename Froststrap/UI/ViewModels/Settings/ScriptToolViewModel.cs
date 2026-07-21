using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.Input;

namespace Froststrap.UI.ViewModels.Settings
{
    // Shared base for Obfuscator + Deobfuscator: input/output, busy/status, copy/paste/clear.
    public abstract class ScriptToolViewModel : NotifyPropertyChangedViewModel
    {
        protected const string LOG_IDENT = "ScriptTool";

        private string _scriptInput = "";
        public string ScriptInput
        {
            get => _scriptInput;
            set { _scriptInput = value ?? ""; OnPropertyChanged(nameof(ScriptInput)); OnPropertyChanged(nameof(HasInput)); }
        }
        public bool HasInput => _scriptInput.Trim().Length > 0;

        private string _output = "";
        public string Output
        {
            get => _output;
            set { _output = value ?? ""; OnPropertyChanged(nameof(Output)); OnPropertyChanged(nameof(HasOutput)); }
        }
        public bool HasOutput => _output.Length > 0;

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

        public ICommand CopyOutputCommand => new AsyncRelayCommand(CopyOutputAsync);
        public ICommand PasteCommand => new AsyncRelayCommand(PasteInputAsync);
        public ICommand ClearCommand => new RelayCommand(Clear);

        private async Task CopyOutputAsync()
        {
            if (string.IsNullOrEmpty(Output))
                return;
            try
            {
                await SetClipboardAsync(Output);
                IsError = false;
                StatusText = "Copied the result to your clipboard.";
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private async Task PasteInputAsync()
        {
            try
            {
                string? text = await GetClipboardAsync();
                if (!string.IsNullOrEmpty(text))
                    ScriptInput = text;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        protected virtual void Clear()
        {
            ScriptInput = "";
            Output = "";
            StatusText = "";
            IsError = false;
        }

        protected async Task RunAsync(string busyLabel, Func<Task<LeakdClient.LeakdResult>> action)
        {
            if (!HasInput)
            {
                IsError = true;
                StatusText = "Paste a script into the box first.";
                return;
            }

            IsBusy = true;
            IsError = false;
            StatusText = busyLabel;
            try
            {
                var result = await action();
                if (result.Success)
                {
                    Output = result.Output;
                    IsError = false;
                    StatusText = BuildSuccessStatus(result);
                }
                else
                {
                    IsError = true;
                    StatusText = result.Error ?? "That didn't work.";
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected virtual string BuildSuccessStatus(LeakdClient.LeakdResult r) => "Done.";

        public static async Task SetClipboardAsync(string text)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow?.Clipboard is { } clipboard)
            {
                await clipboard.SetTextAsync(text);
            }
        }

        public static async Task<string?> GetClipboardAsync()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow?.Clipboard is { } clipboard)
            {
                return await clipboard.TryGetTextAsync();
            }
            return null;
        }
    }
}
