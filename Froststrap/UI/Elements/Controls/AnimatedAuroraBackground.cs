using AnimatedImage.Avalonia;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Froststrap.UI.Elements.Controls
{
    /// <summary>
    /// Dark aurora GIF background (Midnight Rail style). Playback pauses when the
    /// host window is inactive or aurora is disabled in settings.
    /// </summary>
    public class AnimatedAuroraBackground : Panel
    {
        private static readonly Uri GifUri = new("avares://Eclipse/Assets/aurora-dark.gif");
        private static readonly Uri StillUri = new("avares://Eclipse/Assets/aurora-dark-still.png");

        private readonly Image _image = new()
        {
            Stretch = Stretch.UniformToFill,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            IsHitTestVisible = false
        };

        private readonly AnimatedImageSourceUri _animatedSource = new(GifUri);
        private Bitmap? _stillBitmap;
        private Window? _hostWindow;
        private bool _paused;
        private bool _animating;

        public static readonly StyledProperty<bool> IsActiveProperty =
            AvaloniaProperty.Register<AnimatedAuroraBackground, bool>(nameof(IsActive), true);

        public bool IsActive
        {
            get => GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public AnimatedAuroraBackground()
        {
            IsHitTestVisible = false;
            ClipToBounds = true;
            Background = new SolidColorBrush(Color.FromRgb(0x05, 0x04, 0x0A));
            Children.Add(_image);

            AttachedToVisualTree += OnAttached;
            DetachedFromVisualTree += OnDetached;
        }

        private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            EnsureStill();
            SyncFromSettings();
            HookWindow(TopLevel.GetTopLevel(this) as Window);
            UpdatePlayback();
        }

        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            UnhookWindow();
            StopAnimation(showStill: false);
        }

        private void HookWindow(Window? window)
        {
            if (ReferenceEquals(_hostWindow, window))
                return;

            UnhookWindow();
            _hostWindow = window;
            if (_hostWindow is null)
                return;

            _hostWindow.Activated += OnHostActivated;
            _hostWindow.Deactivated += OnHostDeactivated;
            _paused = !_hostWindow.IsActive;
        }

        private void UnhookWindow()
        {
            if (_hostWindow is null)
                return;
            _hostWindow.Activated -= OnHostActivated;
            _hostWindow.Deactivated -= OnHostDeactivated;
            _hostWindow = null;
        }

        private void OnHostActivated(object? sender, EventArgs e)
        {
            _paused = false;
            UpdatePlayback();
        }

        private void OnHostDeactivated(object? sender, EventArgs e)
        {
            _paused = true;
            UpdatePlayback();
        }

        public void SetPaused(bool paused)
        {
            _paused = paused;
            UpdatePlayback();
        }

        public void SyncFromSettings()
        {
            bool enabled = App.Settings?.Prop?.EnableAurora ?? true;
            Opacity = enabled ? 1 : 0;
            IsVisible = enabled;
            UpdatePlayback();
        }

        /// <summary>No-op kept for callers that refreshed the old procedural brushes.</summary>
        public void RefreshBrushes()
        {
        }

        private void UpdatePlayback()
        {
            bool enabled = App.Settings?.Prop?.EnableAurora ?? true;
            bool shouldPlay = enabled && IsActive && !_paused && VisualRoot is not null;

            if (shouldPlay)
                StartAnimation();
            else
                StopAnimation(showStill: enabled);
        }

        private void StartAnimation()
        {
            if (_animating)
            {
                ImageBehavior.SetSpeedRatio(_image, 1d);
                return;
            }

            _image.Source = null;
            ImageBehavior.SetAnimatedSource(_image, _animatedSource);
            ImageBehavior.SetSpeedRatio(_image, 1d);
            _animating = true;
        }

        private void StopAnimation(bool showStill)
        {
            if (_animating)
            {
                // Freeze first so decoding work stops, then tear down the animator.
                ImageBehavior.SetSpeedRatio(_image, 0d);
                _image.ClearValue(ImageBehavior.AnimatedSourceProperty);
                _animating = false;
            }

            if (showStill)
            {
                EnsureStill();
                if (_stillBitmap is not null)
                    _image.Source = _stillBitmap;
            }
            else
            {
                _image.Source = null;
            }
        }

        private void EnsureStill()
        {
            if (_stillBitmap is not null)
                return;

            try
            {
                using var stream = Avalonia.Platform.AssetLoader.Open(StillUri);
                _stillBitmap = new Bitmap(stream);
            }
            catch
            {
                // Still frame is optional; GIF cover frame is used when available.
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == IsActiveProperty)
                UpdatePlayback();
        }
    }
}
