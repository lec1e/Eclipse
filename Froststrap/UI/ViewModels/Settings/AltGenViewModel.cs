using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Froststrap.Integrations;
using Froststrap.Integrations.BloxGen;

namespace Froststrap.UI.ViewModels.Settings
{
    public class AltGenViewModel : NotifyPropertyChangedViewModel
    {
        public string ApiKey
        {
            get => App.Settings.Prop.BloxGenApiKey;
            set
            {
                App.Settings.Prop.BloxGenApiKey = value?.Trim() ?? "";
                OnPropertyChanged(nameof(ApiKey));
            }
        }

        public class AltGenType
        {
            public string Value { get; }
            public string Label { get; }
            public AltGenType(string value, string label) { Value = value; Label = label; }
            public override string ToString() => Label;
        }

        public AltGenType[] AltTypes { get; } =
        {
            new("alt", "alt - All from 2025 (Free)"),
            new("+30 days old", "+30 days old (Premium)"),
            new("+1 year old", "+1 year old (Premium)"),
            new("5+ years old", "5+ years old (Premium)"),
            new("dump", "dump - Dumps Alts (Ultra)"),
        };

        private AltGenType? _selectedAltType;
        public AltGenType SelectedAltType
        {
            get => _selectedAltType ??= AltTypes[0];
            set
            {
                _selectedAltType = value ?? AltTypes[0];
                OnPropertyChanged(nameof(SelectedAltType));
            }
        }

        public string SelectedType => SelectedAltType.Value;

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); OnPropertyChanged(nameof(CanGenerate)); }
        }

        public bool CanGenerate => !_isBusy;

        private string _status = "";
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); OnPropertyChanged(nameof(HasStatus)); }
        }
        public bool HasStatus => !string.IsNullOrEmpty(_status);

        private bool _needsRulesAgreement;
        public bool NeedsRulesAgreement
        {
            get => _needsRulesAgreement;
            set { _needsRulesAgreement = value; OnPropertyChanged(nameof(NeedsRulesAgreement)); }
        }

        private DispatcherTimer? _cooldownTimer;
        private DateTime _cooldownEndUtc;

        private bool _onCooldown;
        public bool OnCooldown
        {
            get => _onCooldown;
            set { _onCooldown = value; OnPropertyChanged(nameof(OnCooldown)); }
        }

        private string _cooldownText = "";
        public string CooldownText
        {
            get => _cooldownText;
            set { _cooldownText = value; OnPropertyChanged(nameof(CooldownText)); }
        }

        private void StartCooldown(long milliseconds)
        {
            _cooldownEndUtc = DateTime.UtcNow.AddMilliseconds(milliseconds);
            OnCooldown = true;
            UpdateCooldown();

            _cooldownTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _cooldownTimer.Tick -= OnCooldownTick;
            _cooldownTimer.Tick += OnCooldownTick;
            _cooldownTimer.Start();
        }

        private void StopCooldown()
        {
            _cooldownTimer?.Stop();
            OnCooldown = false;
            CooldownText = "";
        }

        private void OnCooldownTick(object? sender, EventArgs e) => UpdateCooldown();

        private void UpdateCooldown()
        {
            var remaining = _cooldownEndUtc - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                _cooldownTimer?.Stop();
                OnCooldown = false;
                CooldownText = "";
                Status = "Cooldown's over ù you can generate again.";
                return;
            }

            CooldownText = $"Please wait {(int)remaining.TotalMinutes:D2}:{remaining.Seconds:D2} before your next free generation.";
        }

        private string _username = "";
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(nameof(Username)); OnPropertyChanged(nameof(HasResult)); }
        }

        private string _password = "";
        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(nameof(Password)); }
        }

        private string _cookie = "";
        public string Cookie
        {
            get => _cookie;
            set { _cookie = value; OnPropertyChanged(nameof(Cookie)); OnPropertyChanged(nameof(HasResult)); }
        }

        private string _avatarUrl = "";
        public string AvatarUrl
        {
            get => _avatarUrl;
            set { _avatarUrl = value; OnPropertyChanged(nameof(AvatarUrl)); OnPropertyChanged(nameof(HasAvatar)); }
        }
        public bool HasAvatar => !string.IsNullOrEmpty(_avatarUrl);

        private string _userId = "";
        public string UserId
        {
            get => _userId;
            set { _userId = value; OnPropertyChanged(nameof(UserId)); OnPropertyChanged(nameof(HasUserId)); }
        }
        public bool HasUserId => !string.IsNullOrEmpty(_userId);

        public bool HasResult => !string.IsNullOrEmpty(_username) || !string.IsNullOrEmpty(_cookie);

        public ICommand GenerateCommand => new AsyncRelayCommand(GenerateAsync);
        public ICommand SaveKeyCommand => new RelayCommand(SaveKey);
        public ICommand CopyUsernameCommand => new AsyncRelayCommand(() => CopyToClipboardAsync(Username, "Username"));
        public ICommand CopyPasswordCommand => new AsyncRelayCommand(() => CopyToClipboardAsync(Password, "Password"));
        public ICommand CopyCookieCommand => new AsyncRelayCommand(() => CopyToClipboardAsync(Cookie, ".ROBLOSECURITY cookie"));
        public ICommand SaveToAltManCommand => new AsyncRelayCommand(SaveToAltManAsync);

        private async Task GenerateAsync()
        {
            const string LOG_IDENT = "AltGenViewModel::GenerateAsync";

            if (_isBusy)
                return;

            if (string.IsNullOrWhiteSpace(App.Settings.Prop.BloxGenApiKey))
            {
                Status = "Enter your BloxGen API key first. Don't have one? Use the \"Get a free key\" button above.";
                return;
            }

            IsBusy = true;
            Status = "Generatingù";
            NeedsRulesAgreement = false;

            try
            {
                var result = await BloxGenClient.GenerateAsync(App.Settings.Prop.BloxGenApiKey, SelectedType);

                if (result.Success)
                {
                    StopCooldown();
                    Username = result.Username ?? "";
                    Password = result.Password ?? "";
                    Cookie = result.Cookie ?? "";
                    AvatarUrl = result.AvatarUrl ?? "";
                    UserId = result.Id?.ToString() ?? "";

                    string region = string.IsNullOrEmpty(result.Region) ? "" : $" ù region {result.Region}";
                    string cost = result.Cost.HasValue ? $" ù cost {result.Cost.Value}" : "";
                    Status = $"Done ù generated a '{SelectedType}' account.{region}{cost}";
                }
                else
                {
                    NeedsRulesAgreement = (result.Error ?? "").Contains("rules", StringComparison.OrdinalIgnoreCase);

                    if (result.TimeRemaining.HasValue && result.TimeRemaining.Value > 0)
                    {
                        StartCooldown(result.TimeRemaining.Value);
                        Status = "";
                    }
                    else
                    {
                        Status = result.Error ?? "Generation failed.";
                    }

                    if (!string.IsNullOrEmpty(result.RawResponse))
                        App.Logger.WriteLine(LOG_IDENT, $"Raw BloxGen response: {result.RawResponse}");
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
                Status = $"{ex.GetType().Name}: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveToAltManAsync()
        {
            const string LOG_IDENT = "AltGenViewModel::SaveToAltManAsync";

            if (_isBusy || string.IsNullOrEmpty(Cookie))
                return;

            IsBusy = true;
            Status = "Saving to AltManù";

            try
            {
                var account = await AccountManager.Shared.AddAccountByCookieAsync(Cookie);
                if (account is null)
                {
                    Status = "Couldn't save ù Roblox didn't accept the generated cookie. Try again in a moment.";
                    return;
                }

                Status = $"Saved {account.DisplayLabel} to AltMan. Open that tab to launch it.";
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
                Status = $"{ex.GetType().Name}: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void SaveKey()
        {
            App.Settings.Prop.BloxGenApiKey = (App.Settings.Prop.BloxGenApiKey ?? "").Trim();
            App.Settings.Save();
            OnPropertyChanged(nameof(ApiKey));
            Status = string.IsNullOrEmpty(App.Settings.Prop.BloxGenApiKey)
                ? "Cleared the saved API key."
                : "API key saved ù you can generate whenever you like now.";
        }

        private async Task CopyToClipboardAsync(string value, string label)
        {
            if (string.IsNullOrEmpty(value))
                return;

            try
            {
                await ScriptToolViewModel.SetClipboardAsync(value);
                Status = $"{label} copied to clipboard.";
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("AltGenViewModel::CopyToClipboard", ex);
            }
        }
    }
}
