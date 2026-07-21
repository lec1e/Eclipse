using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Froststrap.Utility.Accounts
{
    /// <summary>
    /// AES-256-GCM helpers. Ciphertext layout: nonce (12) || tag (16) || ciphertext.
    /// </summary>
    public static class AesGcmCrypto
    {
        public const int KeySizeBytes = 32;
        public const int NonceSizeBytes = 12;
        public const int TagSizeBytes = 16;

        public static byte[] GenerateKey()
        {
            var key = new byte[KeySizeBytes];
            RandomNumberGenerator.Fill(key);
            return key;
        }

        public static string EncryptToBase64(string plaintext, byte[] key)
        {
            if (string.IsNullOrEmpty(plaintext))
                return "";

            byte[] plain = Encoding.UTF8.GetBytes(plaintext);
            byte[] nonce = new byte[NonceSizeBytes];
            RandomNumberGenerator.Fill(nonce);
            byte[] ciphertext = new byte[plain.Length];
            byte[] tag = new byte[TagSizeBytes];

            using var aes = new AesGcm(key, TagSizeBytes);
            aes.Encrypt(nonce, plain, ciphertext, tag);

            var packed = new byte[NonceSizeBytes + TagSizeBytes + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, packed, 0, NonceSizeBytes);
            Buffer.BlockCopy(tag, 0, packed, NonceSizeBytes, TagSizeBytes);
            Buffer.BlockCopy(ciphertext, 0, packed, NonceSizeBytes + TagSizeBytes, ciphertext.Length);
            return Convert.ToBase64String(packed);
        }

        public static string DecryptFromBase64(string packedBase64, byte[] key)
        {
            if (string.IsNullOrEmpty(packedBase64))
                return "";

            byte[] packed = Convert.FromBase64String(packedBase64);
            if (packed.Length < NonceSizeBytes + TagSizeBytes)
                throw new CryptographicException("Ciphertext too short.");

            byte[] nonce = packed.AsSpan(0, NonceSizeBytes).ToArray();
            byte[] tag = packed.AsSpan(NonceSizeBytes, TagSizeBytes).ToArray();
            byte[] ciphertext = packed.AsSpan(NonceSizeBytes + TagSizeBytes).ToArray();
            byte[] plain = new byte[ciphertext.Length];

            using var aes = new AesGcm(key, TagSizeBytes);
            aes.Decrypt(nonce, ciphertext, tag, plain);
            return Encoding.UTF8.GetString(plain);
        }
    }
}
