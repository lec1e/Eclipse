using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using FluentAvalonia.Styling;
using Froststrap.Models;

namespace Froststrap.Utility
{
    public static class ThemeManager
    {
        public static readonly Dictionary<string, ThemePalette> Presets = new()
        {
            ["Eclipse"] = new ThemePalette
            {
                Accent = "#C084FC",
                GradientStart = "#A855F7",
                GradientEnd = "#22D3EE",
                Purple = "#E879F9",
                Glow = "#A855F7",
                Background = "#07060C",
                Surface = "#12101A",
                Hairline = "#2E2640"
            },
            ["Purple Haze"] = new ThemePalette
            {
                Accent = "#A855F7",
                GradientStart = "#A855F7",
                GradientEnd = "#22D3EE",
                Purple = "#EC4899",
                Glow = "#A855F7"
            },
            ["Blood Red"] = new ThemePalette
            {
                Accent = "#FF4D4D",
                GradientStart = "#FF4D4D",
                GradientEnd = "#FF9838",
                Purple = "#FF4D4D",
                Glow = "#FF4D4D",
                Background = "#0F0A0A",
                Surface = "#1C1212"
            },
            ["Ocean"] = new ThemePalette
            {
                Accent = "#38BDF8",
                GradientStart = "#38BDF8",
                GradientEnd = "#818CF8",
                Purple = "#818CF8",
                Glow = "#38BDF8",
                Background = "#0A0F14",
                Surface = "#121821"
            },
            ["Emerald"] = new ThemePalette
            {
                Accent = "#34D399",
                GradientStart = "#34D399",
                GradientEnd = "#A3E635",
                Purple = "#22D3EE",
                Glow = "#34D399",
                Background = "#0A0F0C",
                Surface = "#121C16"
            },
            ["Sunset"] = new ThemePalette
            {
                Accent = "#FB923C",
                GradientStart = "#FB923C",
                GradientEnd = "#F472B6",
                Purple = "#F472B6",
                Glow = "#FB923C",
                Background = "#0F0A0C",
                Surface = "#1C1418"
            },
            ["Mono"] = new ThemePalette
            {
                Accent = "#E5E7EB",
                GradientStart = "#E5E7EB",
                GradientEnd = "#9CA3AF",
                Purple = "#9CA3AF",
                Glow = "#E5E7EB",
                Background = "#0B0B0B",
                Surface = "#161616"
            },
        };

        private static string _lastSignature = "";

        public static Color Parse(string? hex, Color fallback)
        {
            if (!string.IsNullOrWhiteSpace(hex) && Color.TryParse(hex.Trim(), out var color))
                return color;
            return fallback;
        }

        public static void ApplyFromSettings()
        {
            var settings = App.Settings?.Prop;
            if (settings is null)
                return;

            ThemePalette palette = settings.Palette ?? new ThemePalette();

            if (!string.IsNullOrEmpty(settings.SelectedThemePreset)
                && Presets.TryGetValue(settings.SelectedThemePreset, out var preset)
                && settings.SelectedThemePreset != "Custom")
            {
                palette = preset.Clone();
                settings.Palette = palette;
            }

            // Force re-apply when glass/aurora/glow toggles even if palette hex is unchanged
            string signature = Signature(palette);
            if (signature == _lastSignature)
                return;

            Apply(palette);
            _lastSignature = signature;
        }

        public static void ApplyPreset(string name)
        {
            if (!Presets.TryGetValue(name, out var preset))
                return;

            App.Settings.Prop.SelectedThemePreset = name;
            App.Settings.Prop.Palette = preset.Clone();
            Apply(App.Settings.Prop.Palette);
            _lastSignature = Signature(App.Settings.Prop.Palette);
        }

        public static void Apply(ThemePalette p)
        {
            if (Application.Current is null)
                return;

            var res = Application.Current.Resources;

            Color accent = Parse(p.Accent, Color.FromRgb(0xA8, 0x55, 0xF7));
            Color gStart = Parse(p.GradientStart, accent);
            Color gEnd = Parse(p.GradientEnd, Color.FromRgb(0x7C, 0x3A, 0xED));
            Color purple = Parse(p.Purple, Color.FromRgb(0xC0, 0x84, 0xFC));
            Color ink = Parse(p.Background, Color.FromRgb(0x0A, 0x0A, 0x0F));
            Color surface = Parse(p.Surface, Color.FromRgb(0x14, 0x12, 0x1C));
            Color hairline = Parse(p.Hairline, Color.FromRgb(0x2A, 0x24, 0x38));
            Color glow = Parse(p.Glow, accent);

            bool glass = App.Settings?.Prop?.EnableGlass ?? true;

            res["BrandAccentColor"] = accent;
            res["BrandPurpleColor"] = purple;
            res["BrandInkColor"] = ink;
            res["BrandSurfaceColor"] = surface;
            res["BrandHairlineColor"] = hairline;
            res["BrandGlowColor"] = glow;
            res["BrandGradientEnd"] = gEnd;

            res["BrandAccentBrush"] = new SolidColorBrush(accent);
            res["BrandPurpleBrush"] = new SolidColorBrush(purple);
            res["BrandInkBrush"] = new SolidColorBrush(ink);
            res["BrandSurfaceBrush"] = new SolidColorBrush(surface);
            res["BrandHairlineBrush"] = new SolidColorBrush(hairline);

            // Glass stage — translucent so aurora reads through Midnight Rail chrome
            res["GlassFillBrush"] = glass
                ? new SolidColorBrush(Color.FromArgb(0x66, surface.R, surface.G, surface.B))
                : new SolidColorBrush(surface);
            res["GlassRailBrush"] = glass
                ? new SolidColorBrush(Color.FromArgb(0x99, ink.R, ink.G, ink.B))
                : new SolidColorBrush(ink);
            res["GlassHeaderBrush"] = glass
                ? new SolidColorBrush(Color.FromArgb(0xAA, ink.R, ink.G, ink.B))
                : new SolidColorBrush(Color.FromArgb(0xEE, ink.R, ink.G, ink.B));
            res["GlassStageBrush"] = glass
                ? new SolidColorBrush(Color.FromArgb(0x55, surface.R, surface.G, surface.B))
                : new SolidColorBrush(surface);

            res["BrandGradientBrush"] = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
                GradientStops =
                [
                    new Avalonia.Media.GradientStop(gStart, 0),
                    new Avalonia.Media.GradientStop(gEnd, 1)
                ]
            };

            // Drive FluentAvalonia accent when available
            var faTheme = Application.Current.Styles.OfType<FluentAvaloniaTheme>().FirstOrDefault();
            if (faTheme is not null)
            {
                faTheme.CustomAccentColor = accent;
                faTheme.PreferUserAccentColor = false;
            }

            // When theme is Dark (not Custom), tint application background toward palette ink
            if (App.Settings.Prop.Theme.GetFinal() == Enums.Theme.Dark)
            {
                res["ApplicationBackgroundColor"] = new SolidColorBrush(ink);
                res["PrimaryBackgroundColor"] = new SolidColorBrush(Color.FromArgb(0xCC, ink.R, ink.G, ink.B));
            }
        }

        private static string Signature(ThemePalette p)
        {
            var s = App.Settings?.Prop;
            return string.Join("|", p.Accent, p.GradientStart, p.GradientEnd, p.Purple, p.Background,
                p.Surface, p.Hairline, p.Glow, s?.EnableAurora, s?.EnableGlass, s?.EnableGlow, s?.SelectedThemePreset);
        }
    }
}
