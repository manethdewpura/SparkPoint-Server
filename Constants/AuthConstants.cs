namespace SparkPoint_Server.Constants
{
    public static class AuthConstants
    {
        // JWT Configuration
        public static readonly string SecretKey = "3gAI5KtwwXRPK1vmM5A3j8W3oFy+aV7xIj2oc7um5zFQ24Au+SzZTNELjEx7busO";
        public static readonly string Issuer = "SparkPoint_Server";
        public static readonly string Audience = "SparkPoint_Client";
        public const int AccessTokenExpiryMinutes = 30;
        public const int RefreshTokenExpiryDays = 7;

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
    }
}