using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace Froststrap.Utility.Accounts
{
    /// <summary>
    /// Stores the AES-256 master key in Windows Credential Manager when available;
    /// falls back to a DPAPI-protected key file on Windows, or a user-scoped encrypted
    /// key file on other platforms.
    /// </summary>
    public static class AccountMasterKeyStore
    {
        private const string LOG_IDENT = "AccountMasterKeyStore";
        private const string CredTarget = "Eclipse/AccountsMasterKey";
        private const string FallbackFileName = "accounts.key";

        public static byte[] GetOrCreateMasterKey()
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    if (TryReadCredential(out var fromCred) && fromCred.Length == AesGcmCrypto.KeySizeBytes)
                        return fromCred;
                }
                catch (Exception ex)
                {
                    App.Logger.WriteException(LOG_IDENT + "::CredRead", ex);
                }
            }

            string fallbackPath = Path.Combine(Paths.Base, "AltMan", FallbackFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(fallbackPath)!);

            if (File.Exists(fallbackPath))
            {
                try
                {
                    byte[] wrapped = File.ReadAllBytes(fallbackPath);
                    byte[] key = UnwrapKey(wrapped);
                    if (key.Length == AesGcmCrypto.KeySizeBytes)
                    {
                        if (OperatingSystem.IsWindows())
                            TryWriteCredential(key);
                        return key;
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.WriteException(LOG_IDENT + "::FallbackRead", ex);
                }
            }

            byte[] fresh = AesGcmCrypto.GenerateKey();
            Persist(fresh, fallbackPath);
            return fresh;
        }

        private static void Persist(byte[] key, string fallbackPath)
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    if (TryWriteCredential(key))
                    {
                        // Also write fallback for migration / recovery
                        File.WriteAllBytes(fallbackPath, WrapKey(key));
                        return;
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.WriteException(LOG_IDENT + "::CredWrite", ex);
                }
            }

            File.WriteAllBytes(fallbackPath, WrapKey(key));
        }

        private static byte[] WrapKey(byte[] key)
        {
            if (OperatingSystem.IsWindows())
            {
                return ProtectedData.Protect(key, Encoding.UTF8.GetBytes("Eclipse.AltMan.v1"), DataProtectionScope.CurrentUser);
            }

            // Non-Windows: XOR with a machine-derived pad (best-effort; still AES-GCM for payloads)
            byte[] pad = SHA256.HashData(Encoding.UTF8.GetBytes(Environment.UserName + "|" + Environment.MachineName + "|Eclipse"));
            byte[] wrapped = new byte[key.Length];
            for (int i = 0; i < key.Length; i++)
                wrapped[i] = (byte)(key[i] ^ pad[i % pad.Length]);
            return wrapped;
        }

        private static byte[] UnwrapKey(byte[] wrapped)
        {
            if (OperatingSystem.IsWindows())
            {
                return ProtectedData.Unprotect(wrapped, Encoding.UTF8.GetBytes("Eclipse.AltMan.v1"), DataProtectionScope.CurrentUser);
            }

            byte[] pad = SHA256.HashData(Encoding.UTF8.GetBytes(Environment.UserName + "|" + Environment.MachineName + "|Eclipse"));
            byte[] key = new byte[wrapped.Length];
            for (int i = 0; i < wrapped.Length; i++)
                key[i] = (byte)(wrapped[i] ^ pad[i % pad.Length]);
            return key;
        }

        [SupportedOSPlatform("windows")]
        private static bool TryReadCredential(out byte[] key)
        {
            key = [];
            if (!CredRead(CredTarget, CRED_TYPE_GENERIC, 0, out IntPtr credPtr))
                return false;

            try
            {
                var cred = Marshal.PtrToStructure<CREDENTIAL>(credPtr);
                if (cred.CredentialBlobSize <= 0 || cred.CredentialBlob == IntPtr.Zero)
                    return false;

                key = new byte[cred.CredentialBlobSize];
                Marshal.Copy(cred.CredentialBlob, key, 0, key.Length);
                return true;
            }
            finally
            {
                CredFree(credPtr);
            }
        }

        [SupportedOSPlatform("windows")]
        private static bool TryWriteCredential(byte[] key)
        {
            var blob = Marshal.AllocHGlobal(key.Length);
            try
            {
                Marshal.Copy(key, 0, blob, key.Length);
                var cred = new CREDENTIAL
                {
                    Type = CRED_TYPE_GENERIC,
                    TargetName = CredTarget,
                    Comment = "Eclipse AltMan AES-256 master key",
                    CredentialBlobSize = key.Length,
                    CredentialBlob = blob,
                    Persist = CRED_PERSIST_LOCAL_MACHINE,
                    AttributeCount = 0,
                    Attributes = IntPtr.Zero,
                    TargetAlias = null,
                    UserName = Environment.UserName
                };

                return CredWrite(ref cred, 0);
            }
            finally
            {
                Marshal.FreeHGlobal(blob);
            }
        }

        private const int CRED_TYPE_GENERIC = 1;
        private const int CRED_PERSIST_LOCAL_MACHINE = 2;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDENTIAL
        {
            public int Flags;
            public int Type;
            public string TargetName;
            public string? Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public int CredentialBlobSize;
            public IntPtr CredentialBlob;
            public int Persist;
            public int AttributeCount;
            public IntPtr Attributes;
            public string? TargetAlias;
            public string? UserName;
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredWrite([In] ref CREDENTIAL userCredential, [In] uint flags);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credentialPtr);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern void CredFree(IntPtr buffer);
    }
}
