/*
 * JwtHelper.cs
 * 
 * This helper class provides JWT token generation and validation functionality.
 * It handles access token creation with proper claims and expiration settings,
 * and includes token validation methods for secure authentication.
 * 
 */

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SparkPoint_Server.Models;
using SparkPoint_Server.Constants;

namespace SparkPoint_Server.Helpers
{
    public static class JwtHelper
    {
        // Generates JWT access token for authenticated user
        public static string GenerateAccessToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(AuthConstants.SecretKey);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.RoleId.ToString())
            };
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(AuthConstants.AccessTokenExpiryMinutes),
                Issuer = AuthConstants.Issuer,
                Audience = AuthConstants.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // Generates a cryptographically secure refresh token
        public static string GenerateRefreshToken()
        {
            const int tokenLength = 32;
            
            using (var rng = RandomNumberGenerator.Create())
            {
                var randomBytes = new byte[tokenLength];
                rng.GetBytes(randomBytes);
                return Convert.ToBase64String(randomBytes);
            }
        }

        // Generates refresh token with expiry and metadata
        public static RefreshTokenData GenerateRefreshTokenWithExpiry()
        {
            return new RefreshTokenData
            {
                Token = GenerateRefreshToken(),
                ExpiresAt = DateTime.UtcNow.AddDays(AuthConstants.RefreshTokenExpiryDays),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };
        }

        // Validates JWT token and returns claims principal
        public static ClaimsPrincipal ValidateToken(string token, bool validateLifetime = true)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(AuthConstants.SecretKey);
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = AuthConstants.Issuer,
                ValidateAudience = true,
                ValidAudience = AuthConstants.Audience,
                ValidateLifetime = validateLifetime,
                ClockSkew = TimeSpan.Zero
            };
            try
            {
                var principal = tokenHandler.ValidateToken(token, parameters, out _);
                return principal;
            }
            catch
            {
                return null;
            }
        }
    }

    public class RefreshTokenData
    {
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRevoked { get; set; }
        public string RevokedReason { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
}
