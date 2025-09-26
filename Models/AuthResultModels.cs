using SparkPoint_Server.Enums;
using SparkPoint_Server.Constants;

namespace SparkPoint_Server.Models
{
    public class AuthenticationResult
    {
        public AuthenticationStatus Status { get; private set; }
        public bool IsSuccess => Status == AuthenticationStatus.Success;
        public string ErrorMessage { get; private set; }
        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }
        public object UserInfo { get; private set; }

        private AuthenticationResult() { }

        public static AuthenticationResult Success(string accessToken, string refreshToken, object userInfo)
        {
            return new AuthenticationResult
            {
                Status = AuthenticationStatus.Success,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserInfo = userInfo
            };
        }

        public static AuthenticationResult Failed(AuthenticationStatus status, string customMessage = null)
        {
            var errorMessage = customMessage ?? GetDefaultErrorMessage(status);
            return new AuthenticationResult
            {
                Status = status,
                ErrorMessage = errorMessage
            };
        }

        private static string GetDefaultErrorMessage(AuthenticationStatus status)
        {
            switch (status)
            {
                case AuthenticationStatus.InvalidCredentials:
                    return AuthConstants.InvalidCredentials;
                case AuthenticationStatus.UserInactive:
                    return AuthConstants.UserAccountInactive;
                case AuthenticationStatus.EVOwnerDeactivated:
                    return AuthConstants.EVOwnerAccountDeactivated;
                case AuthenticationStatus.UserNotFound:
                    return AuthConstants.UserNotFound;
                default:
                    return "Authentication failed";
            }
        }
    }

    public class TokenRefreshResult
    {
        public TokenRefreshStatus Status { get; private set; }
        public bool IsSuccess => Status == TokenRefreshStatus.Success;
        public string ErrorMessage { get; private set; }
        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }

        private TokenRefreshResult() { }

        public static TokenRefreshResult Success(string accessToken, string refreshToken)
        {
            return new TokenRefreshResult
            {
                Status = TokenRefreshStatus.Success,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        public static TokenRefreshResult Failed(TokenRefreshStatus status, string customMessage = null)
        {
            var errorMessage = customMessage ?? GetDefaultErrorMessage(status);
            return new TokenRefreshResult
            {
                Status = status,
                ErrorMessage = errorMessage
            };
        }

        private static string GetDefaultErrorMessage(TokenRefreshStatus status)
        {
            switch (status)
            {
                case TokenRefreshStatus.UserNotFound:
                    return AuthConstants.UserNotFound;
                case TokenRefreshStatus.InvalidRefreshToken:
                    return AuthConstants.InvalidRefreshToken;
                case TokenRefreshStatus.UserInactive:
                    return AuthConstants.UserAccountInactive;
                default:
                    return "Token refresh failed";
            }
        }
    }
}