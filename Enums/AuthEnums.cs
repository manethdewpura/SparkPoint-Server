namespace SparkPoint_Server.Enums
{
    /// <summary>
    /// Authentication result status enumeration
    /// </summary>
    public enum AuthenticationStatus
    {
        /// <summary>
        /// Authentication successful
        /// </summary>
        Success,
        
        /// <summary>
        /// Invalid username or password
        /// </summary>
        InvalidCredentials,
        
        /// <summary>
        /// User account is inactive
        /// </summary>
        UserInactive,
        
        /// <summary>
        /// EV Owner account is deactivated
        /// </summary>
        EVOwnerDeactivated,
        
        /// <summary>
        /// User not found
        /// </summary>
        UserNotFound,
        
        /// <summary>
        /// General authentication failure
        /// </summary>
        Failed
    }

    /// <summary>
    /// Token refresh result status enumeration
    /// </summary>
    public enum TokenRefreshStatus
    {
        /// <summary>
        /// Token refresh successful
        /// </summary>
        Success,
        
        /// <summary>
        /// User not found
        /// </summary>
        UserNotFound,
        
        /// <summary>
        /// Invalid refresh token
        /// </summary>
        InvalidRefreshToken,
        
        /// <summary>
        /// User account is inactive
        /// </summary>
        UserInactive,
        
        /// <summary>
        /// General token refresh failure
        /// </summary>
        Failed
    }

    /// <summary>
    /// Token type enumeration
    /// </summary>
    public enum TokenType
    {
        /// <summary>
        /// Access token for API authentication
        /// </summary>
        AccessToken,
        
        /// <summary>
        /// Refresh token for getting new access tokens
        /// </summary>
        RefreshToken
    }
}