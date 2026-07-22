using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;

namespace Froststrap.UI.Elements.Controls
{
    /// <summary>
    /// Midnight Rail mockup aurora: vivid purple → cyan nebula across the upper stage,
    /// soft star field, continuous drift. Drawn behind translucent chrome.
    /// </summary>
    public class AnimatedAuroraBackground : Canvas
    {
        private readonly Ellipse _core = CreateOrb(980, 0.95);
        private readonly Ellipse _bloomPurple = CreateOrb(1200, 0.88);
        private readonly Ellipse _bloomCyan = CreateOrb(1050, 0.82);
        private readonly Ellipse _bloomMagenta = CreateOrb(860, 0.70);
        private readonly Ellipse _streakA = CreateStreak(1700, 320, 0.78);
        private readonly Ellipse _streakB = CreateStreak(1500, 260, 0.68);
        private readonly Ellipse _streakC = CreateStreak(1300, 200, 0.55);
        private readonly Ellipse _hazeLeft = CreateOrb(900, 0.45);
        private readonly List<Ellipse> _stars = [];
        private readonly DispatcherTimer _timer;
        private double _t;
        private bool _starsBuilt;

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
            // Near-black base like the mockup
            Background = new SolidColorBrush(Color.FromRgb(0x05, 0x05, 0x0A));

            Children.Add(_hazeLeft);
            Children.Add(_streakA);
            Children.Add(_streakB);
            Children.Add(_streakC);
            Children.Add(_bloomMagenta);
            Children.Add(_bloomCyan);
            Children.Add(_bloomPurple);
            Children.Add(_core);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            _timer.Tick += (_, _) => Tick();

            AttachedToVisualTree += (_, _) =>
            {
                EnsureStars();
                SyncFromSettings();
                RefreshBrushes();
                LayoutOrbs(Bounds.Size);
                if (IsActive)
                    _timer.Start();
            };
            DetachedFromVisualTree += (_, _) => _timer.Stop();
            SizeChanged += (_, e) =>
            {
                EnsureStars();
                LayoutOrbs(e.NewSize);
            };
        }

        private void Tick()
        {
            if (!IsActive || !IsEffectivelyVisible)
                return;

            // Keep aurora forced on while the control is live (setting can still hide it).
            SyncFromSettings();
            if (Opacity <= 0.01)
                return;

            _t += 0.016;
            LayoutOrbs(Bounds.Size);
            TwinkleStars();
        }

        public void SyncFromSettings()
        {
            bool enabled = App.Settings?.Prop?.EnableAurora ?? true;
            Opacity = enabled ? 1 : 0;
            IsVisible = enabled;
            if (enabled && IsActive && VisualRoot is not null && !_timer.IsEnabled)
                _timer.Start();
            else if (!enabled)
                _timer.Stop();
        }

        private void EnsureStars()
        {
            if (_starsBuilt || Bounds.Width <= 0)
                return;

            _starsBuilt = true;
            var rng = new Random(42);
            for (int i = 0; i < 48; i++)
            {
                double s = rng.NextDouble() * 2.2 + 0.8;
                var star = new Ellipse
                {
                    Width = s,
                    Height = s,
                    Fill = new SolidColorBrush(Color.FromArgb(
                        (byte)rng.Next(90, 200), 220, 230, 255)),
                    Opacity = rng.NextDouble() * 0.55 + 0.25,
                    IsHitTestVisible = false
                };
                Children.Insert(0, star);
                _stars.Add(star);
                SetLeft(star, rng.NextDouble() * Math.Max(Bounds.Width, 800));
                SetTop(star, rng.NextDouble() * Math.Max(Bounds.Height, 600) * 0.72);
            }
        }

        private void TwinkleStars()
        {
            for (int i = 0; i < _stars.Count; i++)
            {
                double wave = 0.35 + 0.45 * (0.5 + 0.5 * Math.Sin(_t * 1.7 + i * 0.37));
                _stars[i].Opacity = wave;
            }
        }

        private void LayoutOrbs(Size size)
        {
            if (size.Width <= 0 || size.Height <= 0)
                return;

            // Mockup: aurora concentrated upper-center / upper-right over the content stage
            Place(_core, size, 0.62 + Math.Sin(_t * 0.11) * 0.025, 0.18 + Math.Cos(_t * 0.09) * 0.02);
            Place(_bloomPurple, size, 0.55 + Math.Cos(_t * 0.10) * 0.03, 0.22 + Math.Sin(_t * 0.13) * 0.025);
            Place(_bloomCyan, size, 0.78 + Math.Sin(_t * 0.12) * 0.03, 0.20 + Math.Cos(_t * 0.14) * 0.03);
            Place(_bloomMagenta, size, 0.42 + Math.Cos(_t * 0.09) * 0.025, 0.30 + Math.Sin(_t * 0.11) * 0.02);
            Place(_hazeLeft, size, 0.22 + Math.Sin(_t * 0.07) * 0.02, 0.28 + Math.Cos(_t * 0.08) * 0.02);

            Place(_streakA, size, 0.60, 0.16 + Math.Sin(_t * 0.08) * 0.02);
            Place(_streakB, size, 0.72, 0.26 + Math.Cos(_t * 0.09) * 0.02);
            Place(_streakC, size, 0.48, 0.12 + Math.Sin(_t * 0.07) * 0.015);

            _streakA.RenderTransform = new RotateTransform(-18 + Math.Sin(_t * 0.06) * 3);
            _streakB.RenderTransform = new RotateTransform(-26 + Math.Cos(_t * 0.07) * 3);
            _streakC.RenderTransform = new RotateTransform(-12 + Math.Sin(_t * 0.05) * 2.5);
        }

        private static void Place(Ellipse orb, Size size, double nx, double ny)
        {
            SetLeft(orb, nx * size.Width - orb.Width / 2);
            SetTop(orb, ny * size.Height - orb.Height / 2);
        }

        public void RefreshBrushes()
        {
            Color purple = Color.FromRgb(0x8B, 0x5C, 0xF6);
            Color violet = Color.FromRgb(0xC4, 0xB5, 0xFD);
            Color magenta = Color.FromRgb(0xE8, 0x79, 0xF9);
            Color cyan = Color.FromRgb(0x22, 0xD3, 0xEE);
            Color teal = Color.FromRgb(0x2D, 0xD4, 0xBF);
            Color blue = Color.FromRgb(0x38, 0xBD, 0xF8);

            _core.Fill = Radial(violet, 0xEE);
            _bloomPurple.Fill = Radial(purple, 0xD0);
            _bloomCyan.Fill = Radial(cyan, 0xC0);
            _bloomMagenta.Fill = Radial(magenta, 0xA8);
            _hazeLeft.Fill = Radial(blue, 0x70);
            _streakA.Fill = Streak(purple, cyan, 0xD8);
            _streakB.Fill = Streak(cyan, teal, 0xC0);
            _streakC.Fill = Streak(magenta, purple, 0xA0);
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
            GradientOrigin = new RelativePoint(0.5, 0.45, RelativeUnit.Relative),
            Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            RadiusX = new RelativeScalar(0.58, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(0.42, RelativeUnit.Relative),
            GradientStops =
            [
                new Avalonia.Media.GradientStop(Color.FromArgb(alpha, color.R, color.G, color.B), 0),
                new Avalonia.Media.GradientStop(Color.FromArgb((byte)(alpha * 0.55), color.R, color.G, color.B), 0.42),
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
                new Avalonia.Media.GradientStop(Color.FromArgb(alpha, a.R, a.G, a.B), 0.35),
                new Avalonia.Media.GradientStop(Color.FromArgb(alpha, b.R, b.G, b.B), 0.65),
                new Avalonia.Media.GradientStop(Color.FromArgb(0, b.R, b.G, b.B), 1)
            ]
        };

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == IsActiveProperty)
            {
                if (IsActive && VisualRoot is not null)
                {
                    SyncFromSettings();
                    if (Opacity > 0)
                        _timer.Start();
                }
                else
                    _timer.Stop();
            }
        }
    }
}
