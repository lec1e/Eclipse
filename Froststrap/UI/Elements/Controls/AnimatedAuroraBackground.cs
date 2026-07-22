using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Froststrap.UI.Elements.Controls
{
    /// <summary>
    /// Nebula-style drifting aurora — purple/teal streaks matching Eclipse launcher look.
    /// </summary>
    public class AnimatedAuroraBackground : Canvas
    {
        private readonly Ellipse _orbA = CreateOrb(820, 0.72);
        private readonly Ellipse _orbB = CreateOrb(680, 0.58);
        private readonly Ellipse _orbC = CreateOrb(560, 0.48);
        private readonly Ellipse _orbD = CreateOrb(480, 0.40);
        private readonly Ellipse _streakA = CreateStreak(980, 240, 0.38);
        private readonly Ellipse _streakB = CreateStreak(820, 200, 0.30);
        private readonly DispatcherTimer _timer;
        private double _t;

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
            Children.Add(_streakA);
            Children.Add(_streakB);
            Children.Add(_orbA);
            Children.Add(_orbB);
            Children.Add(_orbC);
            Children.Add(_orbD);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            _timer.Tick += (_, _) => Tick();

            AttachedToVisualTree += (_, _) =>
            {
                SyncFromSettings();
                RefreshBrushes();
                if (IsActive)
                    _timer.Start();
            };
            DetachedFromVisualTree += (_, _) => _timer.Stop();
            SizeChanged += (_, _) => LayoutOrbs(Bounds.Size);
        }

        private void Tick()
        {
            if (!IsActive || !IsEffectivelyVisible)
                return;

            SyncFromSettings();
            if (Opacity <= 0.01)
                return;

            _t += 0.015;
            if (((int)(_t * 10)) % 20 == 0)
                RefreshBrushes();
            LayoutOrbs(Bounds.Size);
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

        private void LayoutOrbs(Size size)
        {
            if (size.Width <= 0 || size.Height <= 0)
                return;

            Place(_orbA, size, 0.18 + Math.Sin(_t * 0.32) * 0.10, 0.22 + Math.Cos(_t * 0.26) * 0.12);
            Place(_orbB, size, 0.82 + Math.Cos(_t * 0.24) * 0.10, 0.30 + Math.Sin(_t * 0.30) * 0.14);
            Place(_orbC, size, 0.55 + Math.Sin(_t * 0.20) * 0.16, 0.70 + Math.Cos(_t * 0.28) * 0.10);
            Place(_orbD, size, 0.32 + Math.Cos(_t * 0.18) * 0.12, 0.58 + Math.Sin(_t * 0.22) * 0.14);

            Place(_streakA, size, 0.45 + Math.Sin(_t * 0.12) * 0.08, 0.35 + Math.Cos(_t * 0.10) * 0.06);
            Place(_streakB, size, 0.65 + Math.Cos(_t * 0.14) * 0.10, 0.62 + Math.Sin(_t * 0.11) * 0.08);
            _streakA.RenderTransform = new RotateTransform(18 + Math.Sin(_t * 0.08) * 6);
            _streakB.RenderTransform = new RotateTransform(-22 + Math.Cos(_t * 0.09) * 5);
        }

        private static void Place(Ellipse orb, Size size, double nx, double ny)
        {
            SetLeft(orb, nx * size.Width - orb.Width / 2);
            SetTop(orb, ny * size.Height - orb.Height / 2);
        }

        public void RefreshBrushes()
        {
            Color accent = GetColor("BrandAccentColor", Color.FromRgb(0xA8, 0x55, 0xF7));
            Color purple = GetColor("BrandPurpleColor", Color.FromRgb(0xC0, 0x84, 0xFC));
            Color glow = GetColor("BrandGlowColor", accent);
            Color cyan = GetColor("BrandGradientEnd", Color.FromRgb(0x22, 0xD3, 0xEE));

            _orbA.Fill = Radial(accent, 0xC8);
            _orbB.Fill = Radial(cyan, 0xA0);
            _orbC.Fill = Radial(purple, 0x98);
            _orbD.Fill = Radial(glow, 0x78);
            _streakA.Fill = Radial(accent, 0x70);
            _streakB.Fill = Radial(cyan, 0x60);
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
            GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            RadiusX = new RelativeScalar(0.6, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(0.6, RelativeUnit.Relative),
            GradientStops =
            [
                new Avalonia.Media.GradientStop(Color.FromArgb(alpha, color.R, color.G, color.B), 0),
                new Avalonia.Media.GradientStop(Color.FromArgb((byte)(alpha / 2), color.R, color.G, color.B), 0.4),
                new Avalonia.Media.GradientStop(Color.FromArgb(0, color.R, color.G, color.B), 1)
            ]
        };

        private static Color GetColor(string key, Color fallback)
        {
            if (Application.Current?.TryGetResource(key, ThemeVariant.Default, out var value) == true
                && value is Color c)
                return c;
            return fallback;
        }

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
