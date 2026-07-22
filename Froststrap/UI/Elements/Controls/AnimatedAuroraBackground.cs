using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;

namespace Froststrap.UI.Elements.Controls
{
    /// <summary>
    /// High-definition Midnight Rail aurora: bright purple/cyan nebula with soft
    /// geometric haze and stars. Animation pauses when the host window is inactive.
    /// </summary>
    public class AnimatedAuroraBackground : Canvas
    {
        private readonly Ellipse _wash = CreateOrb(1600, 0.55);
        private readonly Ellipse _core = CreateOrb(1100, 1.0);
        private readonly Ellipse _bloomPurple = CreateOrb(1400, 0.95);
        private readonly Ellipse _bloomCyan = CreateOrb(1200, 0.90);
        private readonly Ellipse _bloomMagenta = CreateOrb(1000, 0.78);
        private readonly Ellipse _bloomTeal = CreateOrb(900, 0.65);
        private readonly Ellipse _streakA = CreateStreak(1900, 380, 0.85);
        private readonly Ellipse _streakB = CreateStreak(1700, 300, 0.75);
        private readonly Ellipse _streakC = CreateStreak(1500, 240, 0.62);
        private readonly Ellipse _geoA = CreateStreak(900, 520, 0.35);
        private readonly Ellipse _geoB = CreateStreak(800, 480, 0.28);
        private readonly List<Ellipse> _stars = [];
        private readonly DispatcherTimer _timer;
        private double _t;
        private bool _starsBuilt;
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
            // Slightly lifted dark-indigo base so nebula reads bright (not crushed black)
            Background = new SolidColorBrush(Color.FromRgb(0x0A, 0x08, 0x14));

            Children.Add(_wash);
            Children.Add(_geoA);
            Children.Add(_geoB);
            Children.Add(_streakA);
            Children.Add(_streakB);
            Children.Add(_streakC);
            Children.Add(_bloomTeal);
            Children.Add(_bloomMagenta);
            Children.Add(_bloomCyan);
            Children.Add(_bloomPurple);
            Children.Add(_core);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            _timer.Tick += (_, _) => Tick();

            AttachedToVisualTree += OnAttached;
            DetachedFromVisualTree += OnDetached;
            SizeChanged += (_, e) =>
            {
                EnsureStars();
                LayoutOrbs(e.NewSize);
            };
        }

        private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            EnsureStars();
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

            _t += 0.014;
            LayoutOrbs(Bounds.Size);
            TwinkleStars();
        }

        public void SyncFromSettings()
        {
            bool enabled = App.Settings?.Prop?.EnableAurora ?? true;
            Opacity = enabled ? 1 : 0;
            IsVisible = enabled;
            UpdateTimer();
        }

        private void EnsureStars()
        {
            if (_starsBuilt || Bounds.Width <= 0)
                return;

            _starsBuilt = true;
            var rng = new Random(42);
            for (int i = 0; i < 64; i++)
            {
                double s = rng.NextDouble() * 2.4 + 0.7;
                var star = new Ellipse
                {
                    Width = s,
                    Height = s,
                    Fill = new SolidColorBrush(Color.FromArgb(
                        (byte)rng.Next(110, 230), 235, 240, 255)),
                    Opacity = rng.NextDouble() * 0.6 + 0.3,
                    IsHitTestVisible = false
                };
                Children.Insert(0, star);
                _stars.Add(star);
                SetLeft(star, rng.NextDouble() * Math.Max(Bounds.Width, 800));
                SetTop(star, rng.NextDouble() * Math.Max(Bounds.Height, 600) * 0.78);
            }
        }

        private void TwinkleStars()
        {
            for (int i = 0; i < _stars.Count; i++)
            {
                double wave = 0.4 + 0.5 * (0.5 + 0.5 * Math.Sin(_t * 1.4 + i * 0.41));
                _stars[i].Opacity = wave;
            }
        }

        private void LayoutOrbs(Size size)
        {
            if (size.Width <= 0 || size.Height <= 0)
                return;

            // Bright nebula across upper half — mockup-style, not crushed to black
            Place(_wash, size, 0.55 + Math.Sin(_t * 0.05) * 0.02, 0.22);
            Place(_core, size, 0.58 + Math.Sin(_t * 0.10) * 0.03, 0.16 + Math.Cos(_t * 0.08) * 0.02);
            Place(_bloomPurple, size, 0.48 + Math.Cos(_t * 0.09) * 0.03, 0.20 + Math.Sin(_t * 0.11) * 0.025);
            Place(_bloomCyan, size, 0.76 + Math.Sin(_t * 0.11) * 0.03, 0.18 + Math.Cos(_t * 0.12) * 0.03);
            Place(_bloomMagenta, size, 0.36 + Math.Cos(_t * 0.08) * 0.025, 0.28 + Math.Sin(_t * 0.10) * 0.02);
            Place(_bloomTeal, size, 0.68 + Math.Sin(_t * 0.07) * 0.02, 0.34 + Math.Cos(_t * 0.09) * 0.02);

            Place(_streakA, size, 0.58, 0.14 + Math.Sin(_t * 0.07) * 0.02);
            Place(_streakB, size, 0.70, 0.24 + Math.Cos(_t * 0.08) * 0.02);
            Place(_streakC, size, 0.46, 0.10 + Math.Sin(_t * 0.06) * 0.015);

            // Soft geometric planes (sidebar mockup vibe)
            Place(_geoA, size, 0.30 + Math.Sin(_t * 0.04) * 0.015, 0.35);
            Place(_geoB, size, 0.82 + Math.Cos(_t * 0.05) * 0.015, 0.40);

            _streakA.RenderTransform = new RotateTransform(-20 + Math.Sin(_t * 0.05) * 2.5);
            _streakB.RenderTransform = new RotateTransform(-28 + Math.Cos(_t * 0.06) * 2.5);
            _streakC.RenderTransform = new RotateTransform(-14 + Math.Sin(_t * 0.045) * 2);
            _geoA.RenderTransform = new RotateTransform(28 + Math.Sin(_t * 0.03) * 4);
            _geoB.RenderTransform = new RotateTransform(-35 + Math.Cos(_t * 0.035) * 4);
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
            Color magenta = Color.FromRgb(0xF0, 0xAB, 0xFC);
            Color cyan = Color.FromRgb(0x67, 0xE8, 0xF9);
            Color teal = Color.FromRgb(0x5E, 0xEA, 0xD4);
            Color blue = Color.FromRgb(0x7D, 0xD3, 0xFC);

            _wash.Fill = Radial(deep, 0x90);
            _core.Fill = Radial(violet, 0xFF);
            _bloomPurple.Fill = Radial(purple, 0xF0);
            _bloomCyan.Fill = Radial(cyan, 0xE0);
            _bloomMagenta.Fill = Radial(magenta, 0xC8);
            _bloomTeal.Fill = Radial(teal, 0xA0);
            _streakA.Fill = Streak(purple, cyan, 0xF0);
            _streakB.Fill = Streak(cyan, teal, 0xD8);
            _streakC.Fill = Streak(magenta, purple, 0xC0);
            _geoA.Fill = Streak(deep, purple, 0x70);
            _geoB.Fill = Streak(blue, cyan, 0x58);
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
            RadiusX = new RelativeScalar(0.62, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(0.48, RelativeUnit.Relative),
            GradientStops =
            [
                new Avalonia.Media.GradientStop(Color.FromArgb(alpha, color.R, color.G, color.B), 0),
                new Avalonia.Media.GradientStop(Color.FromArgb((byte)(alpha * 0.6), color.R, color.G, color.B), 0.38),
                new Avalonia.Media.GradientStop(Color.FromArgb((byte)(alpha * 0.18), color.R, color.G, color.B), 0.72),
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
                new Avalonia.Media.GradientStop(Color.FromArgb(alpha, a.R, a.G, a.B), 0.32),
                new Avalonia.Media.GradientStop(Color.FromArgb(alpha, b.R, b.G, b.B), 0.68),
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
