using System;
using System.Security.Cryptography;

namespace LibraryMS.Common
{
    /// <summary>
    /// 密码哈希工具：PBKDF2-SHA256 / 10 万次迭代 / 每用户 16 字节随机盐 / 32 字节输出。
    /// 对应 docs/04-login-security-review.md 的密码存储规范。
    /// </summary>
    public static class CryptoUtil
    {
        public const int Iterations = 100000;
        public const int SaltSize = 16;
        public const int HashSize = 32;

        public static void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
        {
            salt = new byte[SaltSize];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            hash = Derive(password, salt);
        }

        public static byte[] Derive(string password, byte[] salt)
        {
            using (var kdf = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                return kdf.GetBytes(HashSize);
            }
        }

        public static bool Verify(string password, byte[] expectedHash, byte[] salt)
        {
            if (expectedHash == null || salt == null) return false;
            byte[] actual = Derive(password, salt);
            return FixedTimeEquals(actual, expectedHash);
        }

        /// <summary>常量时间比较，避免时序侧信道（.NET Framework 无 CryptographicOperations）。</summary>
        public static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }
    }
}
