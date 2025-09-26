using System;

namespace SparkPoint_Server.Constants
{
    public static class AuthConstants
    {
        // JWT Configuration
        public static readonly string SecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
            ?? throw new InvalidOperationException("JWT_SECRET_KEY environment variable is required");
        
        public static readonly string Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "SparkPoint_Server";
        public static readonly string Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "SparkPoint_Client";
        
        public const int AccessTokenExpiryMinutes = 30;
        public const int RefreshTokenExpiryDays = 30; // Extended for better UX, but tokens can be rotated

        // Authentication Messages
        public const string InvalidCredentials = "Invalid username or password";
        public const string UserNotFound = "User not found";
        public const string InvalidRefreshToken = "Invalid refresh token";
        public const string UserAccountInactive = "User account is inactive";
        public const string EVOwnerAccountDeactivated = "Your EV Owner account has been deactivated. Please contact a back-office officer for reactivation.";
        
        // Validation Messages
        public const string UsernamePasswordRequired = "Username and password are required";
        public const string UserIdRefreshTokenRequired = "UserId and RefreshToken are required";
        
        // Database Collection Names
        public const string UsersCollection = "Users";
        public const string RefreshTokensCollection = "RefreshTokens";
        public const string EVOwnersCollection = "EVOwners";

        // Rate Limiting (requests per minute)
        public const int AuthRateLimitPerMinute = 10;
        public const int MutationRateLimitPerMinute = 60;
        public const int ReadRateLimitPerMinute = 100;
    }
}