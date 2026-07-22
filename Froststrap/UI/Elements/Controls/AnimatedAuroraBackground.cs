using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;

namespace Froststrap.UI.Elements.Controls
{
    /// <summary>
    /// Visible Midnight Rail aurora: bright purple/cyan nebula that slowly drifts.
    /// Fills its parent; pauses when the host window is inactive.
    /// </summary>
    public class AnimatedAuroraBackground : Canvas
    {
        private readonly Ellipse _wash = CreateOrb();
        private readonly Ellipse _core = CreateOrb();
        private readonly Ellipse _bloomPurple = CreateOrb();
        private readonly Ellipse _bloomCyan = CreateOrb();
        private readonly Ellipse _bloomMagenta = CreateOrb();
        private readonly Ellipse _streakA = CreateOrb();
        private readonly Ellipse _streakB = CreateOrb();
        private readonly Ellipse _streakC = CreateOrb();
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
            // Near-black base so nebula reads clearly
            Background = new SolidColorBrush(Color.FromRgb(0x07, 0x06, 0x10));

            Children.Add(_wash);
            Children.Add(_streakA);
            Children.Add(_streakB);
            Children.Add(_streakC);
            Children.Add(_bloomMagenta);
            Children.Add(_bloomCyan);
            Children.Add(_bloomPurple);
            Children.Add(_core);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            _timer.Tick += (_, _) => Tick();

            AttachedToVisualTree += OnAttached;
            DetachedFromVisualTree += OnDetached;
            SizeChanged += (_, e) => LayoutOrbs(e.NewSize);
        }

        /// <summary>
        /// Fill the slot we're given. Return 0 when size is unconstrained so
        /// SizeToContent windows are not inflated.
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            double w = double.IsInfinity(availableSize.Width) || availableSize.Width < 0
                ? 0
                : availableSize.Width;
            double h = double.IsInfinity(availableSize.Height) || availableSize.Height < 0
                ? 0
                : availableSize.Height;
            return new Size(w, h);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            LayoutOrbs(finalSize);
            return base.ArrangeOverride(finalSize);
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

            // Slow but clearly moving
            _t += 0.0045;
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
            if (size.Width <= 1 || size.Height <= 1)
                return;

            double scale = Math.Max(size.Width, size.Height);

            SizeOrb(_wash, scale * 1.35);
            SizeOrb(_core, scale * 0.95);
            SizeOrb(_bloomPurple, scale * 1.1);
            SizeOrb(_bloomCyan, scale * 0.9);
            SizeOrb(_bloomMagenta, scale * 0.85);
            SizeStreak(_streakA, scale * 1.55, scale * 0.32);
            SizeStreak(_streakB, scale * 1.35, scale * 0.26);
            SizeStreak(_streakC, scale * 1.2, scale * 0.22);

            // Upper-middle nebula — mockup placement, visible drift
            Place(_wash, size, 0.55 + Math.Sin(_t * 0.05) * 0.04, 0.22);
            Place(_core, size, 0.58 + Math.Sin(_t * 0.07) * 0.05, 0.16 + Math.Cos(_t * 0.055) * 0.035);
            Place(_bloomPurple, size, 0.42 + Math.Cos(_t * 0.06) * 0.05, 0.24 + Math.Sin(_t * 0.08) * 0.04);
            Place(_bloomCyan, size, 0.78 + Math.Sin(_t * 0.075) * 0.05, 0.18 + Math.Cos(_t * 0.065) * 0.04);
            Place(_bloomMagenta, size, 0.30 + Math.Cos(_t * 0.055) * 0.04, 0.32 + Math.Sin(_t * 0.07) * 0.035);

            Place(_streakA, size, 0.55, 0.14 + Math.Sin(_t * 0.05) * 0.03);
            Place(_streakB, size, 0.72, 0.28 + Math.Cos(_t * 0.055) * 0.03);
            Place(_streakC, size, 0.40, 0.10 + Math.Sin(_t * 0.045) * 0.025);

            _streakA.RenderTransform = new RotateTransform(-24 + Math.Sin(_t * 0.04) * 4);
            _streakB.RenderTransform = new RotateTransform(-32 + Math.Cos(_t * 0.045) * 4);
            _streakC.RenderTransform = new RotateTransform(-16 + Math.Sin(_t * 0.035) * 3.5);
        }

        private static void SizeOrb(Ellipse orb, double diameter)
        {
            orb.Width = diameter;
            orb.Height = diameter;
        }

        private static void SizeStreak(Ellipse orb, double width, double height)
        {
            orb.Width = width;
            orb.Height = height;
        }

        private static void Place(Ellipse orb, Size size, double nx, double ny)
        {
            SetLeft(orb, nx * size.Width - orb.Width / 2);
            SetTop(orb, ny * size.Height - orb.Height / 2);
        }

        public void RefreshBrushes()
        {
            Color purple = Color.FromRgb(0xA7, 0x8B, 0xFA);
            Color violet = Color.FromRgb(0xE9, 0xD5, 0xFF);
            Color deep = Color.FromRgb(0x7C, 0x3A, 0xED);
            Color magenta = Color.FromRgb(0xF0, 0xAB, 0xFC);
            Color cyan = Color.FromRgb(0x67, 0xE8, 0xF9);
            Color teal = Color.FromRgb(0x5E, 0xEA, 0xD4);

            _wash.Fill = Radial(deep, 0xB0);
            _wash.Opacity = 0.75;
            _core.Fill = Radial(violet, 0xFF);
            _core.Opacity = 0.95;
            _bloomPurple.Fill = Radial(purple, 0xF0);
            _bloomPurple.Opacity = 0.9;
            _bloomCyan.Fill = Radial(cyan, 0xD8);
            _bloomCyan.Opacity = 0.85;
            _bloomMagenta.Fill = Radial(magenta, 0xC0);
            _bloomMagenta.Opacity = 0.75;
            _streakA.Fill = Streak(purple, cyan, 0xF0);
            _streakA.Opacity = 0.8;
            _streakB.Fill = Streak(cyan, teal, 0xC8);
            _streakB.Opacity = 0.7;
            _streakC.Fill = Streak(magenta, purple, 0xB0);
            _streakC.Opacity = 0.65;
        }

        private static Ellipse CreateOrb() => new()
        {
            IsHitTestVisible = false
        };

        private static IBrush Radial(Color color, byte alpha) => new RadialGradientBrush
        {
            GradientOrigin = new RelativePoint(0.5, 0.4, RelativeUnit.Relative),
            Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            RadiusX = new RelativeScalar(0.65, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(0.5, RelativeUnit.Relative),
            GradientStops =
            [
                new Avalonia.Media.GradientStop(Color.FromArgb(alpha, color.R, color.G, color.B), 0),
                new Avalonia.Media.GradientStop(Color.FromArgb((byte)(alpha * 0.65), color.R, color.G, color.B), 0.35),
                new Avalonia.Media.GradientStop(Color.FromArgb((byte)(alpha * 0.22), color.R, color.G, color.B), 0.7),
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
                new Avalonia.Media.GradientStop(Color.FromArgb(alpha, a.R, a.G, a.B), 0.3),
                new Avalonia.Media.GradientStop(Color.FromArgb(alpha, b.R, b.G, b.B), 0.7),
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
