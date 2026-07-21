using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Froststrap.Utility
{
    public static class WindowTiler
    {
        private const string LOG_IDENT = "WindowTiler";
        private const string RobloxProcessName = "RobloxPlayerBeta";

        public static void ScheduleTilePass(WindowTilingLayout layout)
        {
            if (!OperatingSystem.IsWindows())
                return;

            _ = Task.Run(() =>
            {
                try
                {
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                    TileNow(layout);
                }
                catch (Exception ex)
                {
                    App.Logger.WriteException(LOG_IDENT + "::Schedule", ex);
                }
            });
        }

        public static void TileNow(WindowTilingLayout layout)
        {
            if (!OperatingSystem.IsWindows())
                return;

            var windows = FindRobloxWindows();
            App.Logger.WriteLine(LOG_IDENT, $"Found {windows.Count} Roblox window(s) to tile.");

            if (windows.Count == 0)
                return;

            var (rows, cols) = ResolveGrid(layout, windows.Count);
            App.Logger.WriteLine(LOG_IDENT, $"Layout: {rows}x{cols} for {windows.Count} window(s).");

            GetPrimaryWorkArea(out int areaX, out int areaY, out int areaW, out int areaH);
            int cellW = areaW / cols;
            int cellH = areaH / rows;

            for (int i = 0; i < windows.Count; i++)
            {
                int row = i / cols;
                int col = i % cols;
                if (row >= rows) row = rows - 1;

                int x = areaX + col * cellW;
                int y = areaY + row * cellH;

                ShowWindow(windows[i], SW_RESTORE);

                if (!SetWindowPos(windows[i], IntPtr.Zero, x, y, cellW, cellH, SWP_NOZORDER | SWP_NOACTIVATE))
                {
                    int err = Marshal.GetLastWin32Error();
                    App.Logger.WriteLine(LOG_IDENT, $"SetWindowPos failed for hWnd {windows[i]}: {err}");
                }
            }
        }

        private static void GetPrimaryWorkArea(out int x, out int y, out int w, out int h)
        {
            var rc = new RECT();
            if (SystemParametersInfo(SPI_GETWORKAREA, 0, ref rc, 0))
            {
                x = rc.Left;
                y = rc.Top;
                w = rc.Right - rc.Left;
                h = rc.Bottom - rc.Top;
                return;
            }

            x = 0; y = 0; w = 1920; h = 1080;
        }

        private static (int rows, int cols) ResolveGrid(WindowTilingLayout layout, int count)
        {
            switch (layout)
            {
                case WindowTilingLayout.Grid2x2: return (2, 2);
                case WindowTilingLayout.Grid3x3: return (3, 3);
                case WindowTilingLayout.Grid1x2: return (1, 2);
                case WindowTilingLayout.Grid2x1: return (2, 1);
                case WindowTilingLayout.Grid1x3: return (1, 3);
                case WindowTilingLayout.Grid3x1: return (3, 1);
                case WindowTilingLayout.Auto:
                default:
                    int cols = (int)Math.Ceiling(Math.Sqrt(count));
                    int rows = (int)Math.Ceiling((double)count / cols);
                    return (Math.Max(rows, 1), Math.Max(cols, 1));
            }
        }

        private static List<IntPtr> FindRobloxWindows()
        {
            var robloxPids = GetRobloxPids();
            var result = new List<IntPtr>();
            if (robloxPids.Count == 0)
                return result;

            EnumWindows((hWnd, _) =>
            {
                if (!IsWindowVisible(hWnd)) return true;
                GetWindowThreadProcessId(hWnd, out uint pid);
                if (!robloxPids.Contains((int)pid)) return true;

                int len = GetWindowTextLength(hWnd);
                if (len == 0) return true;

                var sb = new StringBuilder(len + 1);
                GetWindowText(hWnd, sb, sb.Capacity);
                string title = sb.ToString();

                var classBuf = new StringBuilder(128);
                GetClassName(hWnd, classBuf, classBuf.Capacity);
                string className = classBuf.ToString();

                if (className.StartsWith("WINDOWSCLIENT", StringComparison.OrdinalIgnoreCase) ||
                    className.Equals("ROBLOX", StringComparison.OrdinalIgnoreCase) ||
                    title.StartsWith("Roblox", StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(hWnd);
                }

                return true;
            }, IntPtr.Zero);

            return result;
        }

        private static HashSet<int> GetRobloxPids()
        {
            var pids = new HashSet<int>();
            try
            {
                foreach (var p in Process.GetProcessesByName(RobloxProcessName))
                {
                    pids.Add(p.Id);
                    p.Dispose();
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT + "::GetPids", ex);
            }
            return pids;
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const int SW_RESTORE = 9;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SPI_GETWORKAREA = 0x0030;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
