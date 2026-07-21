namespace Froststrap.Models
{
    public class ThemePalette
    {
        public string Accent { get; set; } = "#A855F7";
        public string GradientStart { get; set; } = "#A855F7";
        public string GradientEnd { get; set; } = "#7C3AED";
        public string Purple { get; set; } = "#C084FC";
        public string Background { get; set; } = "#0A0A0F";
        public string Surface { get; set; } = "#14121C";
        public string Hairline { get; set; } = "#2A2438";
        public string Glow { get; set; } = "#A855F7";

        public ThemePalette Clone() => (ThemePalette)MemberwiseClone();
    }
}
