using System;

namespace SparkPoint_Server.Constants
{
    public static class AuthConstants
    {
        public static readonly string SecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
            ?? throw new InvalidOperationException("JWT_SECRET_KEY environment variable is required");
        
        public static readonly string Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "SparkPoint_Server";
        public static readonly string Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "SparkPoint_Client";
        
        public const int AccessTokenExpiryMinutes = 30;
        public const int RefreshTokenExpiryDays = 30;

        public const string InvalidCredentials = "Invalid username or password";
        public const string UserNotFound = "User not found";
        public const string InvalidRefreshToken = "Invalid refresh token";
        public const string ExpiredRefreshToken = "Refresh token has expired";
        public const string RevokedRefreshToken = "Refresh token has been revoked";
        public const string TokenFamilyRevoked = "Token family has been revoked due to security concerns";
        public const string UserAccountInactive = "User account is inactive";
        public const string EVOwnerAccountDeactivated = "Your EV Owner account has been deactivated. Please contact a back-office officer for reactivation.";
        
        public const string UsernamePasswordRequired = "Username and password are required";
        public const string UserIdRefreshTokenRequired = "UserId and RefreshToken are required";
        
        public const string UsersCollection = "Users";
        public const string RefreshTokensCollection = "RefreshTokens";
        public const string EVOwnersCollection = "EVOwners";

        public const int AuthRateLimitPerMinute = 10;
        public const int MutationRateLimitPerMinute = 60;
        public const int ReadRateLimitPerMinute = 100;

        public const int MaxRefreshTokensPerUser = 10;
        public const int TokenCleanupIntervalHours = 24;
        public const int RevokedTokenRetentionDays = 7;
    }
}