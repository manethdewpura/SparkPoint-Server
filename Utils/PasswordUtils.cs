/*
 * PasswordUtils.cs
 * 
 * This utility class provides password hashing and verification functionality.
 * It uses BCrypt for secure password hashing with configurable work factors
 * and includes salt generation for enhanced security.
 * 
 */

using System;
using System.Security.Cryptography;

namespace SparkPoint_Server.Utils
{
    public static class PasswordUtils
    {
        private const int WorkFactor = 12;

        // Generates cryptographically secure salt
        public static byte[] GenerateSalt(int size = 32)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var salt = new byte[size];
                rng.GetBytes(salt);
                return salt;
            }
        }

        // Hashes password using BCrypt with work factor
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        }

        // Verifies password against hash using BCrypt
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

        // Checks if password hash needs rehashing
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