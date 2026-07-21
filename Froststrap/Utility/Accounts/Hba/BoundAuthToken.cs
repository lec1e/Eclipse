using System.Security.Cryptography;
using System.Text;

namespace Froststrap.Utility.Accounts.Hba
{
    /// <summary>ECDSA P-256 Bound Auth Token generation (AltMan Crypto::generateBoundAuthToken).</summary>
    public static class BoundAuthToken
    {
        public const string HeaderName = "x-bound-auth-token";

        public static string? Generate(string privateKeyPem, string url, string method, string body = "")
        {
            if (string.IsNullOrWhiteSpace(privateKeyPem))
                return null;

            try
            {
                using var ecdsa = ECDsa.Create();
                ecdsa.ImportFromPem(privateKeyPem);

                string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                string hashedBody = Sha256Base64(body);
                string payload1 = $"{hashedBody}|{timestamp}|{url}|{method}";
                string payload2 = $"|{timestamp}|{url}|{method}";

                string? sig1 = Sign(ecdsa, payload1);
                string? sig2 = Sign(ecdsa, payload2);
                if (sig1 is null || sig2 is null)
                    return null;

                return $"v1|{hashedBody}|{timestamp}|{sig1}|{sig2}";
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("BoundAuthToken::Generate", ex);
                return null;
            }
        }

        private static string Sha256Base64(string data)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(data ?? ""));
            return Convert.ToBase64String(hash);
        }

        private static string? Sign(ECDsa ecdsa, string payload)
        {
            try
            {
                byte[] sig = ecdsa.SignData(
                    Encoding.UTF8.GetBytes(payload),
                    HashAlgorithmName.SHA256,
                    DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
                return Convert.ToBase64String(sig);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("BoundAuthToken::Sign", ex);
                return null;
            }
        }
    }
}
