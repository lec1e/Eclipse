using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;

namespace Froststrap.UI.Elements.Controls
{
    /// <summary>
    /// Near-black stage with diagonal purple/cyan streaks in the upper content area (mockup).
    /// </summary>
    public class AnimatedAuroraBackground : Canvas
    {
        private readonly Ellipse _core = CreateOrb(900, 0.55);
        private readonly Ellipse _right = CreateOrb(780, 0.45);
        private readonly Ellipse _soft = CreateOrb(620, 0.32);
        private readonly Ellipse _streakA = CreateStreak(1400, 220, 0.50);
        private readonly Ellipse _streakB = CreateStreak(1200, 180, 0.38);
        private readonly Ellipse _streakC = CreateStreak(1000, 140, 0.28);
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
            Background = new SolidColorBrush(Color.FromRgb(0x06, 0x06, 0x0A));
            Children.Add(_streakA);
            Children.Add(_streakB);
            Children.Add(_streakC);
            Children.Add(_core);
            Children.Add(_right);
            Children.Add(_soft);

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

            _t += 0.011;
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

            // Mockup: glow concentrated upper-middle / upper-right of content
            Place(_core, size, 0.48 + Math.Sin(_t * 0.14) * 0.03, 0.28 + Math.Cos(_t * 0.11) * 0.03);
            Place(_right, size, 0.78 + Math.Cos(_t * 0.12) * 0.03, 0.22 + Math.Sin(_t * 0.15) * 0.04);
            Place(_soft, size, 0.32 + Math.Sin(_t * 0.10) * 0.02, 0.40 + Math.Cos(_t * 0.09) * 0.02);

            // Diagonal streaks across upper half
            Place(_streakA, size, 0.55, 0.26 + Math.Sin(_t * 0.07) * 0.02);
            Place(_streakB, size, 0.68, 0.34 + Math.Cos(_t * 0.08) * 0.02);
            Place(_streakC, size, 0.42, 0.20 + Math.Sin(_t * 0.06) * 0.015);
            _streakA.RenderTransform = new RotateTransform(-18 + Math.Sin(_t * 0.05) * 2);
            _streakB.RenderTransform = new RotateTransform(-22 + Math.Cos(_t * 0.06) * 2);
            _streakC.RenderTransform = new RotateTransform(-14 + Math.Sin(_t * 0.04) * 2);
        }

        private static void Place(Ellipse orb, Size size, double nx, double ny)
        {
            SetLeft(orb, nx * size.Width - orb.Width / 2);
            SetTop(orb, ny * size.Height - orb.Height / 2);
        }

        public void RefreshBrushes()
        {
            Color purple = Color.FromRgb(0x8B, 0x5C, 0xF6);
            Color violet = Color.FromRgb(0xA8, 0x55, 0xF7);
            Color magenta = Color.FromRgb(0xC0, 0x26, 0xD3);
            Color cyan = Color.FromRgb(0x22, 0xD3, 0xEE);

            _core.Fill = Radial(violet, 0x90);
            _right.Fill = Radial(cyan, 0x70);
            _soft.Fill = Radial(magenta, 0x50);
            _streakA.Fill = Radial(purple, 0x78);
            _streakB.Fill = Radial(cyan, 0x55);
            _streakC.Fill = Radial(magenta, 0x48);
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
            RadiusY = new RelativeScalar(0.45, RelativeUnit.Relative),
            GradientStops =
            [
                new Avalonia.Media.GradientStop(Color.FromArgb(alpha, color.R, color.G, color.B), 0),
                new Avalonia.Media.GradientStop(Color.FromArgb((byte)(alpha * 0.35), color.R, color.G, color.B), 0.5),
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
