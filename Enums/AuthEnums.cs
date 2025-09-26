namespace SparkPoint_Server.Enums
{
    public enum AuthenticationStatus
    {
        Success,
        InvalidCredentials,
        UserInactive,
        EVOwnerDeactivated,
        UserNotFound,
        Failed
    }

    public enum TokenRefreshStatus
    {
        Success,
        UserNotFound,
        InvalidRefreshToken,
        UserInactive,
        Failed
    }

    public enum TokenType
    {
        AccessToken,
        RefreshToken
    }
}