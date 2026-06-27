using System;
using System.Security.Cryptography;
using System.Text;

namespace DynLock.Core
{
    /// <summary>
    /// Mã hóa/giải mã file .dynx: AES-256-CBC + HMAC-SHA256 (encrypt-then-MAC).
    /// Dinh dang blob: MAGIC(5) | IV(16) | HMAC(32) | CIPHERTEXT(n)
    /// HMAC tinh tren MAGIC | IV | CIPHERTEXT.
    /// </summary>
    public static class DynxCrypto
    {
        // "DYNX" + version 1
        private static readonly byte[] Magic = { 0x44, 0x59, 0x4E, 0x58, 0x01 };
        private const int IvSize = 16;
        private const int MacSize = 32;

        public static byte[] Encrypt(byte[] plain, byte[] masterKey)
        {
            byte[] encKey = DeriveKey(masterKey, "enc");
            byte[] macKey = DeriveKey(masterKey, "mac");

            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Key = encKey;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();

                byte[] cipher;
                using (var enc = aes.CreateEncryptor())
                    cipher = enc.TransformFinalBlock(plain, 0, plain.Length);

                byte[] mac = ComputeMac(macKey, Magic, aes.IV, cipher);

                var blob = new byte[Magic.Length + IvSize + MacSize + cipher.Length];
                Buffer.BlockCopy(Magic, 0, blob, 0, Magic.Length);
                Buffer.BlockCopy(aes.IV, 0, blob, Magic.Length, IvSize);
                Buffer.BlockCopy(mac, 0, blob, Magic.Length + IvSize, MacSize);
                Buffer.BlockCopy(cipher, 0, blob, Magic.Length + IvSize + MacSize, cipher.Length);
                return blob;
            }
        }

        public static byte[] Decrypt(byte[] blob, byte[] masterKey)
        {
            int headerSize = Magic.Length + IvSize + MacSize;
            if (blob == null || blob.Length <= headerSize)
                throw new InvalidOperationException("File .dynx không hợp lệ (quá ngắn).");

            for (int i = 0; i < Magic.Length; i++)
                if (blob[i] != Magic[i])
                    throw new InvalidOperationException("File không phải định dạng .dynx hoặc sai phiên bản.");

            byte[] iv = new byte[IvSize];
            byte[] mac = new byte[MacSize];
            byte[] cipher = new byte[blob.Length - headerSize];
            Buffer.BlockCopy(blob, Magic.Length, iv, 0, IvSize);
            Buffer.BlockCopy(blob, Magic.Length + IvSize, mac, 0, MacSize);
            Buffer.BlockCopy(blob, headerSize, cipher, 0, cipher.Length);

            byte[] macKey = DeriveKey(masterKey, "mac");
            byte[] expected = ComputeMac(macKey, Magic, iv, cipher);
            if (!FixedTimeEquals(mac, expected))
                throw new InvalidOperationException(
                    "File .dynx bị hỏng hoặc mã hóa bằng key khác với key trong add-in.");

            byte[] encKey = DeriveKey(masterKey, "enc");
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Key = encKey;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using (var dec = aes.CreateDecryptor())
                    return dec.TransformFinalBlock(cipher, 0, cipher.Length);
            }
        }

        private static byte[] DeriveKey(byte[] masterKey, string purpose)
        {
            using (var sha = SHA256.Create())
            {
                byte[] label = Encoding.UTF8.GetBytes("DynLock:" + purpose);
                var input = new byte[masterKey.Length + label.Length];
                Buffer.BlockCopy(masterKey, 0, input, 0, masterKey.Length);
                Buffer.BlockCopy(label, 0, input, masterKey.Length, label.Length);
                return sha.ComputeHash(input);
            }
        }

        private static byte[] ComputeMac(byte[] macKey, byte[] magic, byte[] iv, byte[] cipher)
        {
            using (var hmac = new HMACSHA256(macKey))
            {
                var input = new byte[magic.Length + iv.Length + cipher.Length];
                Buffer.BlockCopy(magic, 0, input, 0, magic.Length);
                Buffer.BlockCopy(iv, 0, input, magic.Length, iv.Length);
                Buffer.BlockCopy(cipher, 0, input, magic.Length + iv.Length, cipher.Length);
                return hmac.ComputeHash(input);
            }
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
