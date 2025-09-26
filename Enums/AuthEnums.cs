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
        ExpiredRefreshToken,
        RevokedRefreshToken,
        TokenFamilyRevoked,
        UserInactive,
        Failed
    }

    public enum TokenType
    {
        AccessToken,
        RefreshToken
    }

    public enum TokenRevocationReason
    {
        UserLogout,
        TokenRotation,
        SecurityBreach,
        ReuseDetected,
        FamilyCompromised,
        ExpiredCleanup,
        AdminRevoke,
        UserDeactivated
    }
}