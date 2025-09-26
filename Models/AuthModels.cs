using SparkPoint_Server.Constants;
using SparkPoint_Server.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SparkPoint_Server.Models
{
    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RefreshModel
    {
        public string UserId { get; set; }
        public string RefreshToken { get; set; }
    }

    public class RefreshTokenEntry
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; }

        [BsonElement("tokenId")]
        public string TokenId { get; set; }

        [BsonElement("tokenHash")]
        public string TokenHash { get; set; }

        [BsonElement("salt")]
        public string Salt { get; set; }

        [BsonElement("familyId")]
        public string FamilyId { get; set; }

        [BsonElement("deviceInfo")]
        public string DeviceInfo { get; set; }

        [BsonElement("userAgent")]
        public string UserAgent { get; set; }

        [BsonElement("ipAddress")]
        public string IpAddress { get; set; }

        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("expiresAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime ExpiresAt { get; set; }

        [BsonElement("lastUsedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? LastUsedAt { get; set; }

        [BsonElement("isRevoked")]
        public bool IsRevoked { get; set; } = false;

        [BsonElement("revokedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? RevokedAt { get; set; }

        [BsonElement("revokedReason")]
        public string RevokedReason { get; set; }

        [BsonElement("parentTokenId")]
        public string ParentTokenId { get; set; }

        [BsonElement("isUsed")]
        public bool IsUsed { get; set; } = false;

        [BsonElement("usedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? UsedAt { get; set; }

        [BsonIgnore]
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        [BsonIgnore]
        public bool IsValid => !IsRevoked && !IsExpired && !IsUsed;
    }

    public class LegacyRefreshTokenEntry
    {
        public string UserId { get; set; }
        public string Token { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; } = false;
        public string RevokedReason { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
    
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
                case TokenRefreshStatus.ExpiredRefreshToken:
                    return AuthConstants.ExpiredRefreshToken;
                case TokenRefreshStatus.RevokedRefreshToken:
                    return AuthConstants.RevokedRefreshToken;
                case TokenRefreshStatus.UserInactive:
                    return AuthConstants.UserAccountInactive;
                case TokenRefreshStatus.TokenFamilyRevoked:
                    return AuthConstants.TokenFamilyRevoked;
                default:
                    return "Token refresh failed";
            }
        }
    }

    public class RefreshTokenContext
    {
        public string UserAgent { get; set; }
        public string IpAddress { get; set; }
        public DateTime RequestTime { get; set; } = DateTime.UtcNow;
    }
}