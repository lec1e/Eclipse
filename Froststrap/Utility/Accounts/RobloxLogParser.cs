using System.Text.RegularExpressions;

namespace Froststrap.Utility.Accounts
{
    public sealed class RobloxGameSession
    {
        public string Timestamp { get; set; } = "";
        public string JobId { get; set; } = "";
        public string PlaceId { get; set; } = "";
        public string UniverseId { get; set; } = "";
        public string ServerIp { get; set; } = "";
        public string ServerPort { get; set; } = "";
    }

    public sealed class RobloxLogInfo
    {
        public string FileName { get; set; } = "";
        public string FullPath { get; set; } = "";
        public string Timestamp { get; set; } = "";
        public string Version { get; set; } = "";
        public string Channel { get; set; } = "";
        public string UserId { get; set; } = "";
        public bool IsInstallerLog { get; set; }
        public List<RobloxGameSession> Sessions { get; } = new();
    }

    public static class RobloxLogParser
    {
        private static readonly Regex GuidRegex = new(
            @"[0-9a-fA-F]{8}-(?:[0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}",
            RegexOptions.Compiled);

        public static string LogsFolder
        {
            get
            {
                string? local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return string.IsNullOrEmpty(local) ? "" : Path.Combine(local, "Roblox", "logs");
            }
        }

        public static List<RobloxLogInfo> ScanLogs(string? search = null)
        {
            var results = new List<RobloxLogInfo>();
            string folder = LogsFolder;
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                return results;

            foreach (string path in Directory.EnumerateFiles(folder, "*.log").OrderByDescending(File.GetLastWriteTimeUtc))
            {
                var info = new RobloxLogInfo
                {
                    FileName = Path.GetFileName(path),
                    FullPath = path
                };
                ParseLogFile(info);
                if (info.IsInstallerLog) continue;
                if (!string.IsNullOrWhiteSpace(search))
                {
                    string q = search.Trim();
                    bool match = info.FileName.Contains(q, StringComparison.OrdinalIgnoreCase)
                        || info.UserId.Contains(q, StringComparison.OrdinalIgnoreCase)
                        || info.Sessions.Any(s =>
                            s.PlaceId.Contains(q, StringComparison.OrdinalIgnoreCase)
                            || s.JobId.Contains(q, StringComparison.OrdinalIgnoreCase)
                            || s.ServerIp.Contains(q, StringComparison.OrdinalIgnoreCase));
                    if (!match) continue;
                }
                results.Add(info);
            }
            return results;
        }

        public static void ParseLogFile(RobloxLogInfo logInfo)
        {
            if (logInfo.FileName.Contains("RobloxPlayerInstaller", StringComparison.OrdinalIgnoreCase))
            {
                logInfo.IsInstallerLog = true;
                return;
            }

            const int maxRead = 512 * 1024;
            byte[] bytes;
            try
            {
                using var fs = File.OpenRead(logInfo.FullPath);
                int toRead = (int)Math.Min(fs.Length, maxRead);
                bytes = new byte[toRead];
                _ = fs.Read(bytes, 0, toRead);
            }
            catch
            {
                return;
            }

            string text = System.Text.Encoding.UTF8.GetString(bytes);
            RobloxGameSession? current = null;
            string currentTimestamp = "";

            foreach (string rawLine in text.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');

                if (line.Length >= 20 && char.IsDigit(line[0]))
                {
                    int z = line.IndexOf('Z');
                    if (z > 0 && z < 30)
                    {
                        currentTimestamp = line[..(z + 1)];
                        if (string.IsNullOrEmpty(logInfo.Timestamp))
                            logInfo.Timestamp = currentTimestamp;
                    }
                }

                if (string.IsNullOrEmpty(logInfo.Channel))
                {
                    const string token = "The channel is ";
                    int i = line.IndexOf(token, StringComparison.Ordinal);
                    if (i >= 0)
                    {
                        int start = i + token.Length;
                        int end = line.IndexOfAny([' ', '\t'], start);
                        logInfo.Channel = end < 0 ? line[start..] : line[start..end];
                    }
                }

                if (string.IsNullOrEmpty(logInfo.Version))
                {
                    const string token = "\"version\":\"";
                    int i = line.IndexOf(token, StringComparison.Ordinal);
                    if (i >= 0)
                    {
                        int start = i + token.Length;
                        int end = line.IndexOf('"', start);
                        if (end > start) logInfo.Version = line[start..end];
                    }
                }

                if (string.IsNullOrEmpty(logInfo.UserId))
                {
                    const string token = "userid:";
                    int i = line.IndexOf(token, StringComparison.OrdinalIgnoreCase);
                    if (i < 0) i = line.IndexOf("UserId=", StringComparison.OrdinalIgnoreCase);
                    if (i >= 0)
                    {
                        int start = line.IndexOfAny([':', '='], i) + 1;
                        while (start < line.Length && !char.IsDigit(line[start])) start++;
                        int end = start;
                        while (end < line.Length && char.IsDigit(line[end])) end++;
                        if (end > start) logInfo.UserId = line[start..end];
                    }
                }

                const string jobToken = "Joining game '";
                int jobIdx = line.IndexOf(jobToken, StringComparison.Ordinal);
                if (jobIdx >= 0)
                {
                    int start = jobIdx + jobToken.Length;
                    int end = line.IndexOf('\'', start);
                    if (end > start)
                    {
                        string candidate = line[start..end];
                        if (GuidRegex.IsMatch(candidate))
                        {
                            current = new RobloxGameSession
                            {
                                Timestamp = currentTimestamp,
                                JobId = candidate
                            };
                            logInfo.Sessions.Add(current);
                        }
                    }
                }

                if (current is null) continue;

                const string placeToken = "place ";
                int placeIdx = line.IndexOf(placeToken, StringComparison.Ordinal);
                if (placeIdx >= 0 && string.IsNullOrEmpty(current.PlaceId))
                {
                    int start = placeIdx + placeToken.Length;
                    int end = start;
                    while (end < line.Length && char.IsDigit(line[end])) end++;
                    if (end > start) current.PlaceId = line[start..end];
                }

                const string uniToken = "universeid:";
                int uniIdx = line.IndexOf(uniToken, StringComparison.OrdinalIgnoreCase);
                if (uniIdx >= 0 && string.IsNullOrEmpty(current.UniverseId))
                {
                    int start = uniIdx + uniToken.Length;
                    int end = start;
                    while (end < line.Length && char.IsDigit(line[end])) end++;
                    if (end > start) current.UniverseId = line[start..end];
                }

                // server ip/port patterns like Connecting to 1.2.3.4:12345
                var ipMatch = Regex.Match(line, @"(\d{1,3}(?:\.\d{1,3}){3}):(\d{2,5})");
                if (ipMatch.Success && string.IsNullOrEmpty(current.ServerIp))
                {
                    current.ServerIp = ipMatch.Groups[1].Value;
                    current.ServerPort = ipMatch.Groups[2].Value;
                }
            }
        }

        public static void OpenLogsFolder()
        {
            string folder = LogsFolder;
            if (string.IsNullOrEmpty(folder)) return;
            Directory.CreateDirectory(folder);
            Utilities.ShellExecute(folder);
        }

        public static int ClearLogs()
        {
            string folder = LogsFolder;
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return 0;
            int n = 0;
            foreach (string path in Directory.EnumerateFiles(folder, "*.log"))
            {
                try { File.Delete(path); n++; } catch { /* locked */ }
            }
            return n;
        }
    }
}
