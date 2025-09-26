using System;
using System.Security.Cryptography;

namespace SparkPoint_Server.Utils
{
    public static class PasswordUtils
    {
        private const int WorkFactor = 12;

        public static byte[] GenerateSalt(int size = 32)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var salt = new byte[size];
                rng.GetBytes(salt);
                return salt;
            }
        }

        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        }

        public static bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        public static bool NeedsRehash(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return true;

            try
            {
                return BCrypt.Net.BCrypt.PasswordNeedsRehash(hash, WorkFactor);
            }
            catch
            {
                return true;
            }
        }
    }
}