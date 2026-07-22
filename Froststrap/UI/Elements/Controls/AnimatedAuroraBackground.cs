using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;

namespace Froststrap.UI.Elements.Controls
{
    /// <summary>
    /// Soft Midnight Rail aurora — large blurred purple/cyan haze that drifts slowly.
    /// Does not contribute to layout size (safe for SizeToContent windows).
    /// Pauses when the host window is inactive.
    /// </summary>
    public class AnimatedAuroraBackground : Canvas
    {
        private readonly Ellipse _wash = CreateOrb(1800, 0.55);
        private readonly Ellipse _core = CreateOrb(1200, 0.85);
        private readonly Ellipse _bloomPurple = CreateOrb(1400, 0.78);
        private readonly Ellipse _bloomCyan = CreateOrb(1100, 0.62);
        private readonly Ellipse _bloomMagenta = CreateOrb(1000, 0.55);
        private readonly Ellipse _streakA = CreateStreak(2000, 420, 0.55);
        private readonly Ellipse _streakB = CreateStreak(1700, 340, 0.42);
        private readonly Ellipse _streakC = CreateStreak(1500, 280, 0.32);
        private readonly DispatcherTimer _timer;
        private double _t;
        private bool _paused;
        private Window? _hostWindow;

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
            Background = new SolidColorBrush(Color.FromRgb(0x06, 0x06, 0x0C));
            Effect = new BlurEffect { Radius = 56 };

            Children.Add(_wash);
            Children.Add(_streakA);
            Children.Add(_streakB);
            Children.Add(_streakC);
            Children.Add(_bloomMagenta);
            Children.Add(_bloomCyan);
            Children.Add(_bloomPurple);
            Children.Add(_core);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
            _timer.Tick += (_, _) => Tick();

            AttachedToVisualTree += OnAttached;
            DetachedFromVisualTree += OnDetached;
            SizeChanged += (_, e) => LayoutOrbs(e.NewSize);
        }

        /// <summary>Ignore intrinsic child sizes so SizeToContent parents stay compact.</summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(0, 0);
        }

        private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            SyncFromSettings();
            RefreshBrushes();
            LayoutOrbs(Bounds.Size);
            HookWindow(TopLevel.GetTopLevel(this) as Window);
            UpdateTimer();
        }

        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            UnhookWindow();
            _timer.Stop();
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
            UpdateTimer();
        }

        private void OnHostDeactivated(object? sender, EventArgs e)
        {
            _paused = true;
            _timer.Stop();
        }

        public void SetPaused(bool paused)
        {
            _paused = paused;
            UpdateTimer();
        }

        private void UpdateTimer()
        {
            bool enabled = App.Settings?.Prop?.EnableAurora ?? true;
            if (enabled && IsActive && !_paused && VisualRoot is not null)
            {
                if (!_timer.IsEnabled)
                    _timer.Start();
            }
            else
            {
                _timer.Stop();
            }
        }

        private void Tick()
        {
            if (_paused || !IsActive || !IsEffectivelyVisible || Opacity <= 0.01)
                return;

            // Gentle slow drift — clearly animated, never frantic
            _t += 0.0028;
            LayoutOrbs(Bounds.Size);
        }

        public void SyncFromSettings()
        {
            bool enabled = App.Settings?.Prop?.EnableAurora ?? true;
            Opacity = enabled ? 1 : 0;
            IsVisible = enabled;
            UpdateTimer();
        }

        private void LayoutOrbs(Size size)
        {
            if (size.Width <= 0 || size.Height <= 0)
                return;

            // Soft haze across the upper-middle stage
            Place(_wash, size, 0.58 + Math.Sin(_t * 0.028) * 0.02, 0.18);
            Place(_core, size, 0.60 + Math.Sin(_t * 0.04) * 0.025, 0.14 + Math.Cos(_t * 0.032) * 0.018);
            Place(_bloomPurple, size, 0.46 + Math.Cos(_t * 0.035) * 0.025, 0.20 + Math.Sin(_t * 0.045) * 0.018);
            Place(_bloomCyan, size, 0.78 + Math.Sin(_t * 0.042) * 0.025, 0.16 + Math.Cos(_t * 0.038) * 0.02);
            Place(_bloomMagenta, size, 0.34 + Math.Cos(_t * 0.03) * 0.022, 0.26 + Math.Sin(_t * 0.036) * 0.018);

            Place(_streakA, size, 0.58, 0.12 + Math.Sin(_t * 0.03) * 0.015);
            Place(_streakB, size, 0.72, 0.22 + Math.Cos(_t * 0.033) * 0.015);
            Place(_streakC, size, 0.44, 0.08 + Math.Sin(_t * 0.026) * 0.012);

            _streakA.RenderTransform = new RotateTransform(-22 + Math.Sin(_t * 0.022) * 2.2);
            _streakB.RenderTransform = new RotateTransform(-30 + Math.Cos(_t * 0.025) * 2.2);
            _streakC.RenderTransform = new RotateTransform(-16 + Math.Sin(_t * 0.02) * 1.8);
        }

        private static void Place(Ellipse orb, Size size, double nx, double ny)
        {
            SetLeft(orb, nx * size.Width - orb.Width / 2);
            SetTop(orb, ny * size.Height - orb.Height / 2);
        }

        public void RefreshBrushes()
        {
            Color purple = Color.FromRgb(0xA7, 0x8B, 0xFA);
            Color violet = Color.FromRgb(0xDD, 0xD6, 0xFE);
            Color deep = Color.FromRgb(0x7C, 0x3A, 0xED);
            Color magenta = Color.FromRgb(0xE8, 0x79, 0xF9);
            Color cyan = Color.FromRgb(0x67, 0xE8, 0xF9);
            Color teal = Color.FromRgb(0x5E, 0xEA, 0xD4);

            _wash.Fill = Radial(deep, 0x88);
            _core.Fill = Radial(violet, 0xE0);
            _bloomPurple.Fill = Radial(purple, 0xD0);
            _bloomCyan.Fill = Radial(cyan, 0xA8);
            _bloomMagenta.Fill = Radial(magenta, 0x98);
            _streakA.Fill = Streak(purple, cyan, 0xC0);
            _streakB.Fill = Streak(cyan, teal, 0x90);
            _streakC.Fill = Streak(magenta, purple, 0x80);
        }

        private static Ellipse CreateOrb(double size, double opacity) => new()
        {
            Width = size,
            Height = size,
            Opacity = opacity,
            IsHitTestVisible = false
        };

        private static Ellipse CreateStreak(double width, double height, double opacity) => new()
        {
            Width = width,
            Height = height,
            Opacity = opacity,
            IsHitTestVisible = false,
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative)
        };

        private static IBrush Radial(Color color, byte alpha) => new RadialGradientBrush
        {
            GradientOrigin = new RelativePoint(0.5, 0.42, RelativeUnit.Relative),
            Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            RadiusX = new RelativeScalar(0.68, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(0.52, RelativeUnit.Relative),
            GradientStops =
            [
                new Avalonia.Media.GradientStop(Color.FromArgb(alpha, color.R, color.G, color.B), 0),
                new Avalonia.Media.GradientStop(Color.FromArgb((byte)(alpha * 0.55), color.R, color.G, color.B), 0.42),
                new Avalonia.Media.GradientStop(Color.FromArgb((byte)(alpha * 0.14), color.R, color.G, color.B), 0.74),
                new Avalonia.Media.GradientStop(Color.FromArgb(0, color.R, color.G, color.B), 1)
            ]
        };

        private static IBrush Streak(Color a, Color b, byte alpha) => new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops =
            [
                new Avalonia.Media.GradientStop(Color.FromArgb(0, a.R, a.G, a.B), 0),
                new Avalonia.Media.GradientStop(Color.FromArgb(alpha, a.R, a.G, a.B), 0.34),
                new Avalonia.Media.GradientStop(Color.FromArgb(alpha, b.R, b.G, b.B), 0.66),
                new Avalonia.Media.GradientStop(Color.FromArgb(0, b.R, b.G, b.B), 1)
            ]
        };

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == IsActiveProperty)
                UpdateTimer();
        }
    }
}
