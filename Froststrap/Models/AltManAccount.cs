using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Froststrap.Models
{
    /// <summary>
    /// AltMan-style account record (ported from altman-maintained AccountData).
    /// Cookie / HBA private key are held in memory only; persisted as AES-256-GCM ciphertext.
    /// </summary>
    public class AltManAccount : INotifyPropertyChanged
    {
        private int _id;
        private string _displayName = "";
        private string _username = "";
        private string _userId = "";
        private string _status = "Unknown";
        private string _ageGroup = "";
        private string _voiceStatus = "";
        private long _voiceBanExpiry;
        private long _banExpiry;
        private string _note = "";
        private string _cookie = "";
        private bool _isFavorite;
        private string _lastLocation = "";
        private ulong _placeId;
        private string _jobId = "";
        private string _hbaPrivateKey = "";
        private bool _hbaEnabled = true;
        private bool _isSelected;
        private string? _versionProfileId;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Id
        {
            get => _id;
            set => Set(ref _id, value);
        }

        public string DisplayName
        {
            get => _displayName;
            set => Set(ref _displayName, value);
        }

        public string Username
        {
            get => _username;
            set => Set(ref _username, value);
        }

        public string UserId
        {
            get => _userId;
            set => Set(ref _userId, value);
        }

        public long UserIdLong => long.TryParse(UserId, out long id) ? id : 0;

        public string Status
        {
            get => _status;
            set => Set(ref _status, value);
        }

        public string AgeGroup
        {
            get => _ageGroup;
            set => Set(ref _ageGroup, value);
        }

        public string VoiceStatus
        {
            get => _voiceStatus;
            set => Set(ref _voiceStatus, value);
        }

        public long VoiceBanExpiry
        {
            get => _voiceBanExpiry;
            set => Set(ref _voiceBanExpiry, value);
        }

        public long BanExpiry
        {
            get => _banExpiry;
            set => Set(ref _banExpiry, value);
        }

        public string Note
        {
            get => _note;
            set => Set(ref _note, value);
        }

        public string Cookie
        {
            get => _cookie;
            set => Set(ref _cookie, value);
        }

        // Compatibility alias for Froststrap AccountManager consumers
        public string SecurityToken
        {
            get => Cookie;
            set => Cookie = value;
        }

        public bool IsFavorite
        {
            get => _isFavorite;
            set => Set(ref _isFavorite, value);
        }

        public string LastLocation
        {
            get => _lastLocation;
            set => Set(ref _lastLocation, value);
        }

        public ulong PlaceId
        {
            get => _placeId;
            set => Set(ref _placeId, value);
        }

        public string JobId
        {
            get => _jobId;
            set => Set(ref _jobId, value);
        }

        public string HbaPrivateKey
        {
            get => _hbaPrivateKey;
            set => Set(ref _hbaPrivateKey, value);
        }

        public bool HbaEnabled
        {
            get => _hbaEnabled;
            set => Set(ref _hbaEnabled, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => Set(ref _isSelected, value);
        }

        public string? VersionProfileId
        {
            get => _versionProfileId;
            set => Set(ref _versionProfileId, value);
        }

        public string DisplayLabel => string.IsNullOrEmpty(DisplayName) ? Username : DisplayName;

        public bool IsBannedLike => Status is "Banned" or "Warned" or "Terminated";

        private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            if (name is nameof(DisplayName) or nameof(Username))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayLabel)));
        }
    }
}
