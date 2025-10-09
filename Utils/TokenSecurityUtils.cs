/*
 * TokenSecurityUtils.cs
 * 
 * This utility class provides token security and cryptographic operations.
 * It handles token generation, hashing, verification, salt generation,
 * and device information extraction for secure token management.
 * 
 */

using System;
using System.Security.Cryptography;
using System.Text;

namespace SparkPoint_Server.Utils
{
    public static class TokenSecurityUtils
    {
        private const int SaltSize = 32;
        private const int TokenSize = 32;

        // Generates a cryptographically secure salt
        // Generates a cryptographically secure salt
        public static string GenerateSalt()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var saltBytes = new byte[SaltSize];
                rng.GetBytes(saltBytes);
                return Convert.ToBase64String(saltBytes);
            }
        }

        // Generates a cryptographically secure refresh token
        // Generates a cryptographically secure refresh token
        public static string GenerateRefreshToken()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var tokenBytes = new byte[TokenSize];
                rng.GetBytes(tokenBytes);
                return Convert.ToBase64String(tokenBytes);
            }
        }

        // Hashes a token with salt using SHA256
        // Hashes a token with salt using SHA256
        public static string HashToken(string token, string salt)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(salt))
                throw new ArgumentException("Token and salt cannot be null or empty");

            using (var sha256 = SHA256.Create())
            {
                var saltBytes = Convert.FromBase64String(salt);
                var tokenBytes = Encoding.UTF8.GetBytes(token);
                
                var combined = new byte[tokenBytes.Length + saltBytes.Length];
                Array.Copy(tokenBytes, 0, combined, 0, tokenBytes.Length);
                Array.Copy(saltBytes, 0, combined, tokenBytes.Length, saltBytes.Length);
                
                var hashBytes = sha256.ComputeHash(combined);
                return Convert.ToBase64String(hashBytes);
            }
        }

        // Verifies a token against its stored hash and salt
        // Verifies a token against its stored hash and salt
        public static bool VerifyToken(string token, string storedHash, string salt)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(salt))
                return false;

            try
            {
                var computedHash = HashToken(token, salt);
                return string.Equals(computedHash, storedHash, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        // Generates a unique token ID
        // Generates a unique token ID
        public static string GenerateTokenId()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var idBytes = new byte[16];
                rng.GetBytes(idBytes);
                return Convert.ToBase64String(idBytes);
            }
        }

        // Extracts device information from user agent string
        // Extracts device information from user agent string
        public static string ExtractDeviceInfo(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "Unknown";

            userAgent = userAgent.ToLowerInvariant();
            
            if (userAgent.Contains("mobile") || userAgent.Contains("android") || userAgent.Contains("iphone"))
                return "Mobile";
            if (userAgent.Contains("tablet") || userAgent.Contains("ipad"))
                return "Tablet";
            if (userAgent.Contains("electron") || userAgent.Contains("desktop"))
                return "Desktop App";
            
            return "Web Browser";
        }

        // Validates if a token family ID is properly formatted
        // Validates if a token family ID is properly formatted
        public static bool IsValidTokenFamilyId(string familyId)
        {
            if (string.IsNullOrEmpty(familyId))
                return false;

            try
            {
                var bytes = Convert.FromBase64String(familyId);
                return bytes.Length == 16;
            }
            catch
            {
                return false;
            }
        }
    }
}