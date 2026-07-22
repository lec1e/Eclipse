using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Froststrap.UI.Elements.Controls
{
    /// <summary>
    /// Soft drifting aurora orbs — Eclipse brand motion background.
    /// </summary>
    public class AnimatedAuroraBackground : Canvas
    {
        private readonly Ellipse _orbA = CreateOrb(520, 0.22);
        private readonly Ellipse _orbB = CreateOrb(420, 0.16);
        private readonly Ellipse _orbC = CreateOrb(360, 0.12);
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
            Children.Add(_orbA);
            Children.Add(_orbB);
            Children.Add(_orbC);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            _timer.Tick += (_, _) =>
            {
                if (!IsActive || !IsEffectivelyVisible)
                    return;
                if (App.Settings?.Prop is { EnableAurora: false })
                {
                    Opacity = 0;
                    return;
                }
                Opacity = 1;
                _t += 0.016;
                if (((int)(_t * 10)) % 30 == 0)
                    RefreshBrushes();
                LayoutOrbs(Bounds.Size);
            };

            AttachedToVisualTree += (_, _) =>
            {
                RefreshBrushes();
                if (IsActive)
                    _timer.Start();
            };
            DetachedFromVisualTree += (_, _) => _timer.Stop();
            SizeChanged += (_, _) => LayoutOrbs(Bounds.Size);
        }

        private void LayoutOrbs(Size size)
        {
            if (size.Width <= 0 || size.Height <= 0)
                return;

            Place(_orbA, size, 0.18 + Math.Sin(_t * 0.35) * 0.08, 0.22 + Math.Cos(_t * 0.28) * 0.10);
            Place(_orbB, size, 0.72 + Math.Cos(_t * 0.25) * 0.10, 0.30 + Math.Sin(_t * 0.32) * 0.12);
            Place(_orbC, size, 0.48 + Math.Sin(_t * 0.22) * 0.14, 0.78 + Math.Cos(_t * 0.30) * 0.08);
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

            _orbA.Fill = Radial(accent, 0x55);
            _orbB.Fill = Radial(purple, 0x40);
            _orbC.Fill = Radial(glow, 0x35);
        }

        private static Ellipse CreateOrb(double size, double opacity) => new()
        {
            Width = size,
            Height = size,
            Opacity = opacity,
            IsHitTestVisible = false
        };

        private static IBrush Radial(Color color, byte alpha) => new RadialGradientBrush
        {
            GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            RadiusX = new RelativeScalar(0.5, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(0.5, RelativeUnit.Relative),
            GradientStops =
            [
                new Avalonia.Media.GradientStop(Color.FromArgb(alpha, color.R, color.G, color.B), 0),
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
                    _timer.Start();
                else
                    _timer.Stop();
            }
        }
    }
}
