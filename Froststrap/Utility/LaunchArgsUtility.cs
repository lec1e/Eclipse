using System.Text.RegularExpressions;

namespace Froststrap.Utility
{
    public static class LaunchArgsUtility
    {
        private static readonly Regex PlaceIdRegex = new(
            @"placeid[^0-9]+(\d{4,19})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex PlaceLauncherUrlRegex = new(
            @"placelauncherurl[:=]([^+&\s""]+)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex AccessCodeRegex = new(
            @"accesscode[^0-9a-fA-F]+([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex RbxServersQuickLaunchRegex = new(
            @"^https?://(?:www\.)?rbxservers\.xyz/embedded/quicklaunch/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static long? TryExtractPlaceId(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
                return null;

            var launcherMatch = PlaceLauncherUrlRegex.Match(commandLine);
            if (launcherMatch.Success)
            {
                string launcherUrl;
                try { launcherUrl = Uri.UnescapeDataString(launcherMatch.Groups[1].Value); }
                catch { launcherUrl = launcherMatch.Groups[1].Value; }

                var inner = PlaceIdRegex.Match(launcherUrl);
                if (inner.Success && long.TryParse(inner.Groups[1].Value, out long innerId))
                    return innerId;
            }

            var match = PlaceIdRegex.Match(commandLine);
            if (!match.Success)
                return null;

            return long.TryParse(match.Groups[1].Value, out long placeId) ? placeId : null;
        }

        public static string? TryExtractAccessCode(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
                return null;

            var match = AccessCodeRegex.Match(commandLine);
            return match.Success ? match.Groups[1].Value : null;
        }

        public static string? TryExtractRbxServersQuickLaunchCode(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            var match = RbxServersQuickLaunchRegex.Match(url);
            return match.Success ? match.Groups[1].Value : null;
        }

        public static string AppendAccessCode(string commandLine, string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode))
                return commandLine;

            if (string.IsNullOrEmpty(commandLine))
                return $"accessCode={accessCode}";

            var existing = AccessCodeRegex.Match(commandLine);
            if (existing.Success)
            {
                int valueStart = existing.Groups[1].Index;
                int valueLength = existing.Groups[1].Length;
                return commandLine[..valueStart] + accessCode + commandLine[(valueStart + valueLength)..];
            }

            return commandLine + "&accessCode=" + accessCode;
        }
    }
}
