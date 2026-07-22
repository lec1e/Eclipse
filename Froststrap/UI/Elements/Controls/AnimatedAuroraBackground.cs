using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Froststrap.UI.Elements.Controls
{
    /// <summary>
    /// Soft nebula wash — large purple/cyan blooms matching the Midnight Rail mockup.
    /// </summary>
    public class AnimatedAuroraBackground : Canvas
    {
        private readonly Ellipse _bloomL = CreateOrb(1100, 0.85);
        private readonly Ellipse _bloomR = CreateOrb(980, 0.75);
        private readonly Ellipse _bloomC = CreateOrb(720, 0.55);
        private readonly Ellipse _bloomB = CreateOrb(640, 0.45);
        private readonly Ellipse _hazeA = CreateStreak(1200, 360, 0.42);
        private readonly Ellipse _hazeB = CreateStreak(1000, 280, 0.35);
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
            Background = new SolidColorBrush(Color.FromRgb(0x0B, 0x0B, 0x10));
            Children.Add(_hazeA);
            Children.Add(_hazeB);
            Children.Add(_bloomL);
            Children.Add(_bloomR);
            Children.Add(_bloomC);
            Children.Add(_bloomB);

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

            // Mockup: purple wash left/center, cyan on the right edge
            Place(_bloomL, size, 0.22 + Math.Sin(_t * 0.18) * 0.04, 0.35 + Math.Cos(_t * 0.14) * 0.05);
            Place(_bloomR, size, 0.88 + Math.Cos(_t * 0.16) * 0.03, 0.28 + Math.Sin(_t * 0.20) * 0.06);
            Place(_bloomC, size, 0.55 + Math.Sin(_t * 0.12) * 0.05, 0.72 + Math.Cos(_t * 0.15) * 0.04);
            Place(_bloomB, size, 0.40 + Math.Cos(_t * 0.11) * 0.04, 0.18 + Math.Sin(_t * 0.13) * 0.03);

            Place(_hazeA, size, 0.48, 0.42 + Math.Sin(_t * 0.08) * 0.03);
            Place(_hazeB, size, 0.72, 0.58 + Math.Cos(_t * 0.09) * 0.03);
            _hazeA.RenderTransform = new RotateTransform(12 + Math.Sin(_t * 0.06) * 4);
            _hazeB.RenderTransform = new RotateTransform(-16 + Math.Cos(_t * 0.07) * 4);
        }

        private static void Place(Ellipse orb, Size size, double nx, double ny)
        {
            SetLeft(orb, nx * size.Width - orb.Width / 2);
            SetTop(orb, ny * size.Height - orb.Height / 2);
        }

        public void RefreshBrushes()
        {
            Color purple = Color.FromRgb(0x7C, 0x3A, 0xED);
            Color violet = Color.FromRgb(0xA8, 0x55, 0xF7);
            Color cyan = Color.FromRgb(0x22, 0xD3, 0xEE);
            Color indigo = Color.FromRgb(0x4F, 0x46, 0xE5);

            _bloomL.Fill = Radial(violet, 0xE0);
            _bloomR.Fill = Radial(cyan, 0xC0);
            _bloomC.Fill = Radial(purple, 0xA8);
            _bloomB.Fill = Radial(indigo, 0x90);
            _hazeA.Fill = Radial(violet, 0x88);
            _hazeB.Fill = Radial(cyan, 0x70);
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
            RadiusY = new RelativeScalar(0.55, RelativeUnit.Relative),
            GradientStops =
            [
                new Avalonia.Media.GradientStop(Color.FromArgb(alpha, color.R, color.G, color.B), 0),
                new Avalonia.Media.GradientStop(Color.FromArgb((byte)(alpha * 0.45), color.R, color.G, color.B), 0.45),
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
