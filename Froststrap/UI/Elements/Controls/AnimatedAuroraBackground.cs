using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;

namespace Froststrap.UI.Elements.Controls
{
    /// <summary>
    /// Mockup aurora: vivid diagonal purple/cyan streaks across upper content on near-black.
    /// </summary>
    public class AnimatedAuroraBackground : Canvas
    {
        private readonly Ellipse _bloomA = CreateOrb(1100, 0.72);
        private readonly Ellipse _bloomB = CreateOrb(920, 0.60);
        private readonly Ellipse _bloomC = CreateOrb(700, 0.42);
        private readonly Ellipse _streak1 = CreateStreak(1600, 260, 0.62);
        private readonly Ellipse _streak2 = CreateStreak(1400, 200, 0.50);
        private readonly Ellipse _streak3 = CreateStreak(1200, 160, 0.38);
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
            Background = new SolidColorBrush(Color.FromRgb(0x07, 0x07, 0x0C));
            Children.Add(_streak1);
            Children.Add(_streak2);
            Children.Add(_streak3);
            Children.Add(_bloomA);
            Children.Add(_bloomB);
            Children.Add(_bloomC);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
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

            _t += 0.012;
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

            // Concentrated in content upper half (right of rail)
            Place(_bloomA, size, 0.55 + Math.Sin(_t * 0.14) * 0.03, 0.28 + Math.Cos(_t * 0.11) * 0.03);
            Place(_bloomB, size, 0.78 + Math.Cos(_t * 0.12) * 0.03, 0.24 + Math.Sin(_t * 0.15) * 0.04);
            Place(_bloomC, size, 0.38 + Math.Sin(_t * 0.10) * 0.02, 0.36 + Math.Cos(_t * 0.09) * 0.02);

            Place(_streak1, size, 0.58, 0.26 + Math.Sin(_t * 0.07) * 0.015);
            Place(_streak2, size, 0.70, 0.34 + Math.Cos(_t * 0.08) * 0.015);
            Place(_streak3, size, 0.48, 0.20 + Math.Sin(_t * 0.06) * 0.012);
            _streak1.RenderTransform = new RotateTransform(-20 + Math.Sin(_t * 0.05) * 2);
            _streak2.RenderTransform = new RotateTransform(-24 + Math.Cos(_t * 0.06) * 2);
            _streak3.RenderTransform = new RotateTransform(-16 + Math.Sin(_t * 0.04) * 2);
        }

        private static void Place(Ellipse orb, Size size, double nx, double ny)
        {
            SetLeft(orb, nx * size.Width - orb.Width / 2);
            SetTop(orb, ny * size.Height - orb.Height / 2);
        }

        public void RefreshBrushes()
        {
            Color purple = Color.FromRgb(0x8B, 0x5C, 0xF6);
            Color violet = Color.FromRgb(0xC0, 0x84, 0xFC);
            Color magenta = Color.FromRgb(0xD9, 0x46, 0xEF);
            Color cyan = Color.FromRgb(0x22, 0xD3, 0xEE);
            Color blue = Color.FromRgb(0x38, 0xBD, 0xF8);

            _bloomA.Fill = Radial(violet, 0xB8);
            _bloomB.Fill = Radial(cyan, 0x98);
            _bloomC.Fill = Radial(magenta, 0x70);
            _streak1.Fill = Radial(purple, 0xA0);
            _streak2.Fill = Radial(blue, 0x80);
            _streak3.Fill = Radial(magenta, 0x68);
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
            RadiusX = new RelativeScalar(0.55, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(0.40, RelativeUnit.Relative),
            GradientStops =
            [
                new Avalonia.Media.GradientStop(Color.FromArgb(alpha, color.R, color.G, color.B), 0),
                new Avalonia.Media.GradientStop(Color.FromArgb((byte)(alpha * 0.4), color.R, color.G, color.B), 0.5),
                new Avalonia.Media.GradientStop(Color.FromArgb(0, color.R, color.G, color.B), 1)
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
