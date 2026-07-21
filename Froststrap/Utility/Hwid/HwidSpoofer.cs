using System.Security;
using System.Security.Cryptography;
using System.Text;
using Froststrap.Utility.BanAsync;
using Microsoft.Win32;

namespace Froststrap.Utility.Hwid
{
    public static class HwidIdentifierKeys
    {
        public const string MachineGuid = "MachineGuid";
        public const string HwProfileGuid = "HwProfileGuid";
        public const string MachineId = "MachineId";
        public const string ProductId = "ProductId";
        public const string ComputerName = "ComputerName";
        public const string ActiveComputerName = "ActiveComputerName";
        public const string SusClientId = "SusClientId";
        public const string MacPrefix = "MAC:";
    }

    public sealed class HwidIdentifierInfo
    {
        public required string Key { get; init; }
        public required string DisplayName { get; init; }
        public string CurrentValue { get; init; } = "";
        public string BackupValue { get; init; } = "";
        public bool IsPresent { get; init; } = true;
    }

    public sealed class HwidPreviewValue
    {
        public required string Key { get; init; }
        public required string DisplayName { get; init; }
        public required string PreviewValue { get; init; }
    }

    /// <summary>
    /// Dedicated machine-wide HWID spoofing. Reuses BanAsync MAC / MachineGuid helpers where possible.
    /// </summary>
    public static class HwidSpoofer
    {
        private const string LOG_IDENT = "HwidSpoofer";

        public static IReadOnlyList<HwidIdentifierInfo> ReadAll()
        {
            var list = new List<HwidIdentifierInfo>
            {
                ReadOne(HwidIdentifierKeys.MachineGuid, "MachineGuid",
                    () => MachineGuidSpoofer.ReadCurrent() ?? "",
                    () => FirstNonEmpty(App.Settings.Prop.HwidOriginalMachineGuid, App.Settings.Prop.BanAsyncOriginalMachineGuid)),
                ReadOne(HwidIdentifierKeys.HwProfileGuid, "HwProfileGuid",
                    () => ReadString(@"SYSTEM\CurrentControlSet\Control\IDConfigDB\Hardware Profiles\0001", "HwProfileGuid") ?? "",
                    () => App.Settings.Prop.HwidOriginalHwProfileGuid),
                ReadOne(HwidIdentifierKeys.MachineId, "SQMClient MachineId",
                    () => ReadString(@"SOFTWARE\Microsoft\SQMClient", "MachineId") ?? "",
                    () => App.Settings.Prop.HwidOriginalMachineId),
                ReadOne(HwidIdentifierKeys.ProductId, "ProductId",
                    () => ReadString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductId") ?? "",
                    () => App.Settings.Prop.HwidOriginalProductId),
                ReadOne(HwidIdentifierKeys.ComputerName, "ComputerName",
                    () => ReadString(@"SYSTEM\CurrentControlSet\Control\ComputerName\ComputerName", "ComputerName") ?? "",
                    () => App.Settings.Prop.HwidOriginalComputerName),
                ReadOne(HwidIdentifierKeys.ActiveComputerName, "ActiveComputerName",
                    () => ReadString(@"SYSTEM\CurrentControlSet\Control\ComputerName\ActiveComputerName", "ComputerName") ?? "",
                    () => FirstNonEmpty(App.Settings.Prop.HwidOriginalActiveComputerName, App.Settings.Prop.HwidOriginalComputerName)),
                ReadOne(HwidIdentifierKeys.SusClientId, "SusClientId",
                    () => ReadString(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate", "SusClientId") ?? "",
                    () => App.Settings.Prop.HwidOriginalSusClientId),
            };

            foreach (var adapter in MacSpoofer.EnumeratePhysicalAdapters())
            {
                App.Settings.Prop.HwidOriginalMacByGuid.TryGetValue(adapter.Id, out string? backup);
                if (string.IsNullOrEmpty(backup))
                    App.Settings.Prop.BanAsyncOriginalMacByGuid.TryGetValue(adapter.Id, out backup);

                list.Add(new HwidIdentifierInfo
                {
                    Key = HwidIdentifierKeys.MacPrefix + adapter.Id,
                    DisplayName = $"MAC · {adapter.Name}",
                    CurrentValue = NetworkAdapter.FormatMac(adapter.PhysicalAddress),
                    BackupValue = string.IsNullOrEmpty(backup) ? "" : NetworkAdapter.FormatMac(backup),
                    IsPresent = true
                });
            }

            return list;
        }

        private static HwidIdentifierInfo ReadOne(string key, string display, Func<string> current, Func<string> backup)
        {
            string cur = current();
            return new HwidIdentifierInfo
            {
                Key = key,
                DisplayName = display,
                CurrentValue = cur,
                BackupValue = backup() ?? "",
                IsPresent = !string.IsNullOrEmpty(cur) || key is HwidIdentifierKeys.SusClientId or HwidIdentifierKeys.MachineId
            };
        }

        public static IReadOnlyList<HwidPreviewValue> GeneratePreview()
        {
            string computerName = GenerateComputerName();
            var list = new List<HwidPreviewValue>
            {
                new() { Key = HwidIdentifierKeys.MachineGuid, DisplayName = "MachineGuid", PreviewValue = MachineGuidSpoofer.GenerateRandom() },
                new() { Key = HwidIdentifierKeys.HwProfileGuid, DisplayName = "HwProfileGuid", PreviewValue = GenerateGuidBrace() },
                new() { Key = HwidIdentifierKeys.MachineId, DisplayName = "SQMClient MachineId", PreviewValue = GenerateGuidBrace() },
                new() { Key = HwidIdentifierKeys.ProductId, DisplayName = "ProductId", PreviewValue = GenerateProductId() },
                new() { Key = HwidIdentifierKeys.ComputerName, DisplayName = "ComputerName", PreviewValue = computerName },
                new() { Key = HwidIdentifierKeys.ActiveComputerName, DisplayName = "ActiveComputerName", PreviewValue = computerName },
                new() { Key = HwidIdentifierKeys.SusClientId, DisplayName = "SusClientId", PreviewValue = Guid.NewGuid().ToString() },
            };

            foreach (var adapter in MacSpoofer.EnumeratePhysicalAdapters())
            {
                string mac = MacSpoofer.GenerateRandomMac(
                    App.Settings.Prop.BanAsyncOuiMirror ? adapter.PhysicalAddress : null);
                list.Add(new HwidPreviewValue
                {
                    Key = HwidIdentifierKeys.MacPrefix + adapter.Id,
                    DisplayName = $"MAC · {adapter.Name}",
                    PreviewValue = NetworkAdapter.FormatMac(mac)
                });
            }

            return list;
        }

        public static int SpoofAll(Action<string> log)
        {
            int ok = 0;
            string sharedName = GenerateComputerName();
            foreach (var info in ReadAll().Where(i => i.IsPresent))
            {
                string? ov = info.Key is HwidIdentifierKeys.ComputerName or HwidIdentifierKeys.ActiveComputerName
                    ? sharedName
                    : null;
                if (Spoof(info.Key, log, ov))
                    ok++;
            }
            App.Settings.Save();
            return ok;
        }

        public static int RevertAll(Action<string> log)
        {
            int ok = 0;
            foreach (var info in ReadAll())
            {
                if (Revert(info.Key, log))
                    ok++;
            }
            App.Settings.Save();
            return ok;
        }

        public static bool Spoof(string key, Action<string> log, string? overrideValue = null)
        {
            if (key.StartsWith(HwidIdentifierKeys.MacPrefix, StringComparison.Ordinal))
                return SpoofMac(key[HwidIdentifierKeys.MacPrefix.Length..], log, overrideValue);

            return key switch
            {
                HwidIdentifierKeys.MachineGuid => SpoofMachineGuid(log, overrideValue),
                HwidIdentifierKeys.HwProfileGuid => SpoofString(
                    @"SYSTEM\CurrentControlSet\Control\IDConfigDB\Hardware Profiles\0001", "HwProfileGuid",
                    overrideValue ?? GenerateGuidBrace(), log,
                    cur => { if (string.IsNullOrEmpty(App.Settings.Prop.HwidOriginalHwProfileGuid)) App.Settings.Prop.HwidOriginalHwProfileGuid = cur; }),
                HwidIdentifierKeys.MachineId => SpoofString(
                    @"SOFTWARE\Microsoft\SQMClient", "MachineId",
                    overrideValue ?? GenerateGuidBrace(), log,
                    cur => { if (string.IsNullOrEmpty(App.Settings.Prop.HwidOriginalMachineId)) App.Settings.Prop.HwidOriginalMachineId = cur; }),
                HwidIdentifierKeys.ProductId => SpoofString(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductId",
                    overrideValue ?? GenerateProductId(), log,
                    cur => { if (string.IsNullOrEmpty(App.Settings.Prop.HwidOriginalProductId)) App.Settings.Prop.HwidOriginalProductId = cur; }),
                HwidIdentifierKeys.ComputerName => SpoofComputerName(log, overrideValue, activeOnly: false),
                HwidIdentifierKeys.ActiveComputerName => SpoofComputerName(log, overrideValue, activeOnly: true),
                HwidIdentifierKeys.SusClientId => SpoofString(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate", "SusClientId",
                    overrideValue ?? Guid.NewGuid().ToString(), log,
                    cur => { if (string.IsNullOrEmpty(App.Settings.Prop.HwidOriginalSusClientId)) App.Settings.Prop.HwidOriginalSusClientId = cur; }),
                _ => false
            };
        }

        public static bool Revert(string key, Action<string> log)
        {
            if (key.StartsWith(HwidIdentifierKeys.MacPrefix, StringComparison.Ordinal))
                return RevertMac(key[HwidIdentifierKeys.MacPrefix.Length..], log);

            return key switch
            {
                HwidIdentifierKeys.MachineGuid => RevertMachineGuid(log),
                HwidIdentifierKeys.HwProfileGuid => RevertString(
                    @"SYSTEM\CurrentControlSet\Control\IDConfigDB\Hardware Profiles\0001", "HwProfileGuid",
                    App.Settings.Prop.HwidOriginalHwProfileGuid, log,
                    () => App.Settings.Prop.HwidOriginalHwProfileGuid = ""),
                HwidIdentifierKeys.MachineId => RevertString(
                    @"SOFTWARE\Microsoft\SQMClient", "MachineId",
                    App.Settings.Prop.HwidOriginalMachineId, log,
                    () => App.Settings.Prop.HwidOriginalMachineId = ""),
                HwidIdentifierKeys.ProductId => RevertString(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductId",
                    App.Settings.Prop.HwidOriginalProductId, log,
                    () => App.Settings.Prop.HwidOriginalProductId = ""),
                HwidIdentifierKeys.ComputerName => RevertComputerName(log, activeOnly: false),
                HwidIdentifierKeys.ActiveComputerName => RevertComputerName(log, activeOnly: true),
                HwidIdentifierKeys.SusClientId => RevertString(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate", "SusClientId",
                    App.Settings.Prop.HwidOriginalSusClientId, log,
                    () => App.Settings.Prop.HwidOriginalSusClientId = ""),
                _ => false
            };
        }

        private static bool SpoofMachineGuid(Action<string> log, string? overrideValue)
        {
            string? current = MachineGuidSpoofer.ReadCurrent();
            if (!string.IsNullOrEmpty(current))
            {
                if (string.IsNullOrEmpty(App.Settings.Prop.HwidOriginalMachineGuid))
                    App.Settings.Prop.HwidOriginalMachineGuid = current!;
                if (string.IsNullOrEmpty(App.Settings.Prop.BanAsyncOriginalMachineGuid))
                    App.Settings.Prop.BanAsyncOriginalMachineGuid = current!;
            }

            string neu = string.IsNullOrEmpty(overrideValue) ? MachineGuidSpoofer.GenerateRandom() : overrideValue!;
            bool ok = MachineGuidSpoofer.Apply(neu, log);
            if (ok) App.Settings.Save();
            return ok;
        }

        private static bool RevertMachineGuid(Action<string> log)
        {
            string backup = FirstNonEmpty(App.Settings.Prop.HwidOriginalMachineGuid, App.Settings.Prop.BanAsyncOriginalMachineGuid);
            if (string.IsNullOrEmpty(backup))
            {
                log("No MachineGuid backup.");
                return false;
            }

            bool ok = MachineGuidSpoofer.Apply(backup, log);
            if (ok)
            {
                App.Settings.Prop.HwidOriginalMachineGuid = "";
                App.Settings.Save();
            }
            return ok;
        }

        private static bool SpoofComputerName(Action<string> log, string? overrideValue, bool activeOnly)
        {
            string path = activeOnly
                ? @"SYSTEM\CurrentControlSet\Control\ComputerName\ActiveComputerName"
                : @"SYSTEM\CurrentControlSet\Control\ComputerName\ComputerName";

            string? current = ReadString(path, "ComputerName");
            if (!string.IsNullOrEmpty(current))
            {
                if (activeOnly)
                {
                    if (string.IsNullOrEmpty(App.Settings.Prop.HwidOriginalActiveComputerName))
                        App.Settings.Prop.HwidOriginalActiveComputerName = current!;
                }
                else if (string.IsNullOrEmpty(App.Settings.Prop.HwidOriginalComputerName))
                {
                    App.Settings.Prop.HwidOriginalComputerName = current!;
                }
            }

            string neu = string.IsNullOrEmpty(overrideValue) ? GenerateComputerName() : overrideValue!;
            bool ok = WriteString(path, "ComputerName", neu, log);
            if (ok) App.Settings.Save();
            return ok;
        }

        private static bool RevertComputerName(Action<string> log, bool activeOnly)
        {
            string backup = activeOnly
                ? FirstNonEmpty(App.Settings.Prop.HwidOriginalActiveComputerName, App.Settings.Prop.HwidOriginalComputerName)
                : App.Settings.Prop.HwidOriginalComputerName;

            if (string.IsNullOrEmpty(backup))
            {
                log("No ComputerName backup.");
                return false;
            }

            string path = activeOnly
                ? @"SYSTEM\CurrentControlSet\Control\ComputerName\ActiveComputerName"
                : @"SYSTEM\CurrentControlSet\Control\ComputerName\ComputerName";

            bool ok = WriteString(path, "ComputerName", backup, log);
            if (ok)
            {
                if (activeOnly)
                    App.Settings.Prop.HwidOriginalActiveComputerName = "";
                else
                    App.Settings.Prop.HwidOriginalComputerName = "";
                App.Settings.Save();
            }
            return ok;
        }

        private static bool SpoofMac(string adapterId, Action<string> log, string? overrideValue)
        {
            var adapter = MacSpoofer.EnumeratePhysicalAdapters().FirstOrDefault(a => a.Id == adapterId);
            if (adapter is null)
            {
                log($"Adapter not found: {adapterId}");
                return false;
            }

            if (!string.IsNullOrEmpty(adapter.PhysicalAddress)
                && !App.Settings.Prop.HwidOriginalMacByGuid.ContainsKey(adapter.Id))
            {
                App.Settings.Prop.HwidOriginalMacByGuid[adapter.Id] = adapter.PhysicalAddress;
            }

            string mac = string.IsNullOrEmpty(overrideValue)
                ? MacSpoofer.GenerateRandomMac(App.Settings.Prop.BanAsyncOuiMirror ? adapter.PhysicalAddress : null)
                : overrideValue!.Replace(":", "").Replace("-", "").ToUpperInvariant();

            bool ok = MacSpoofer.SpoofAdapter(adapter, mac, log);
            if (ok)
            {
                if (!App.Settings.Prop.HwidSpoofedAdapterGuids.Contains(adapter.Id))
                    App.Settings.Prop.HwidSpoofedAdapterGuids.Add(adapter.Id);
                App.Settings.Save();
            }
            return ok;
        }

        private static bool RevertMac(string adapterId, Action<string> log)
        {
            var adapter = MacSpoofer.EnumeratePhysicalAdapters().FirstOrDefault(a => a.Id == adapterId);
            if (adapter is null)
            {
                log($"Adapter not found: {adapterId}");
                return false;
            }

            // Prefer clearing NetworkAddress (hardware MAC) if we have a backup OR were spoofed
            bool ok = MacSpoofer.RevertAdapter(adapter, log);
            if (ok)
            {
                App.Settings.Prop.HwidSpoofedAdapterGuids.Remove(adapter.Id);
                App.Settings.Prop.HwidOriginalMacByGuid.Remove(adapter.Id);
                App.Settings.Save();
            }
            return ok;
        }

        private static bool SpoofString(string path, string valueName, string newValue, Action<string> log, Action<string> saveBackup)
        {
            string? current = ReadString(path, valueName);
            if (!string.IsNullOrEmpty(current))
                saveBackup(current!);

            bool ok = WriteString(path, valueName, newValue, log);
            if (ok) App.Settings.Save();
            return ok;
        }

        private static bool RevertString(string path, string valueName, string backup, Action<string> log, Action clearBackup)
        {
            if (string.IsNullOrEmpty(backup))
            {
                log($"No backup for {valueName}.");
                return false;
            }

            bool ok = WriteString(path, valueName, backup, log);
            if (ok)
            {
                clearBackup();
                App.Settings.Save();
            }
            return ok;
        }

        public static string? ReadString(string subKey, string valueName)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(subKey, writable: false);
                return key?.GetValue(valueName)?.ToString();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT + "::Read", ex);
                return null;
            }
        }

        public static bool WriteString(string subKey, string valueName, string value, Action<string> log)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(subKey, writable: true)
                    ?? Registry.LocalMachine.CreateSubKey(subKey, writable: true);
                if (key is null)
                {
                    log($"Couldn't open HKLM\\{subKey}. Run as administrator.");
                    return false;
                }

                key.SetValue(valueName, value, RegistryValueKind.String);
                log($"Wrote {valueName} = {value}");
                return true;
            }
            catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException)
            {
                log("Access denied. Run as administrator.");
                return false;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT + "::Write", ex);
                log($"Write failed: {ex.Message}");
                return false;
            }
        }

        public static string GenerateGuidBrace() => "{" + Guid.NewGuid().ToString().ToUpperInvariant() + "}";

        public static string GenerateProductId()
        {
            static string Digits(int n)
            {
                var sb = new StringBuilder(n);
                for (int i = 0; i < n; i++)
                    sb.Append(RandomNumberGenerator.GetInt32(0, 10));
                return sb.ToString();
            }
            return $"{Digits(5)}-{Digits(3)}-{Digits(7)}-{Digits(5)}";
        }

        public static string GenerateComputerName()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var sb = new StringBuilder("DESKTOP-");
            for (int i = 0; i < 7; i++)
                sb.Append(chars[RandomNumberGenerator.GetInt32(0, chars.Length)]);
            return sb.ToString();
        }

        private static string FirstNonEmpty(params string?[] values)
            => values.FirstOrDefault(v => !string.IsNullOrEmpty(v)) ?? "";
    }
}
