using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Driver;
using SparkPoint_Server.Models;
using SparkPoint_Server.Helpers;
using SparkPoint_Server.Utils;
using SparkPoint_Server.Constants;
using SparkPoint_Server.Enums;

namespace SparkPoint_Server.Services
{
    public class AuthService
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<RefreshTokenEntry> _refreshTokensCollection;
        private readonly IMongoCollection<EVOwner> _evOwnersCollection;
        private readonly RefreshTokenService _refreshTokenService;

        public AuthService()
        {
            var dbContext = new MongoDbContext();
            _usersCollection = dbContext.GetCollection<User>(AuthConstants.UsersCollection);
            _refreshTokensCollection = dbContext.GetCollection<RefreshTokenEntry>(AuthConstants.RefreshTokensCollection);
            _evOwnersCollection = dbContext.GetCollection<EVOwner>(AuthConstants.EVOwnersCollection);
            _refreshTokenService = new RefreshTokenService();
        }

        public Models.AuthenticationResult AuthenticateUser(string username, string password, RefreshTokenContext context = null)
        {
            var user = _usersCollection.Find(u => u.Username == username).FirstOrDefault();
            if (user == null)
            {
                return Models.AuthenticationResult.Failed(AuthenticationStatus.UserNotFound);
            }

            if (!PasswordUtils.VerifyPassword(password, user.PasswordHash))
            {
                return Models.AuthenticationResult.Failed(AuthenticationStatus.InvalidCredentials);
            }

            if (!user.IsActive)
            {
                var status = GetInactiveUserStatus(user);
                return Models.AuthenticationResult.Failed(status);
            }

            var accessToken = JwtHelper.GenerateAccessToken(user);

            var refreshTokenContext = context ?? CreateDefaultContext();
            var refreshTokenEntry = _refreshTokenService.CreateRefreshToken(user.Id, refreshTokenContext);
            var refreshToken = refreshTokenEntry.TokenHash;

            var userInfo = GetUserInfoForResponse(user);

            return Models.AuthenticationResult.Success(accessToken, refreshToken, userInfo);
        }

        public TokenRefreshResult RefreshToken(string userId, string refreshToken, RefreshTokenContext context = null)
        {
            var user = _usersCollection.Find(u => u.Id == userId).FirstOrDefault();
            if (user == null)
            {
                return TokenRefreshResult.Failed(TokenRefreshStatus.UserNotFound);
            }

            if (!user.IsActive)
            {
                return TokenRefreshResult.Failed(TokenRefreshStatus.UserInactive);
            }

            var refreshContext = context ?? CreateDefaultContext();

            var validationResult = _refreshTokenService.ValidateRefreshToken(userId, refreshToken, refreshContext);
            
            if (!validationResult.IsSuccess)
            {
                return TokenRefreshResult.Failed(validationResult.Status, validationResult.ErrorMessage);
            }

            var newAccessToken = JwtHelper.GenerateAccessToken(user);
            
            // Check if refresh token is expired or close to expiring
            var currentToken = validationResult.TokenEntry;
            var timeUntilExpiry = currentToken.ExpiresAt - refreshContext.RequestTime;
            var shouldRefreshToken = timeUntilExpiry.TotalDays <= AuthConstants.RefreshTokenRenewalThresholdDays;
            
            string newRefreshToken;
            
            if (shouldRefreshToken)
            {
                // Create new refresh token and mark old one as used
                _refreshTokenService.MarkTokenAsUsed(currentToken.TokenId, refreshContext.RequestTime);
                var newTokenEntry = _refreshTokenService.CreateRefreshToken(userId, refreshContext, currentToken.TokenId);
                newRefreshToken = newTokenEntry.TokenHash;
            }
            else
            {
                // Keep existing refresh token and update last used time
                _refreshTokenService.UpdateTokenLastUsed(currentToken.TokenId, refreshContext.RequestTime);
                newRefreshToken = refreshToken;
            }

            return TokenRefreshResult.Success(newAccessToken, newRefreshToken);
        }

        public void LogoutUser(string userId, string refreshToken = null)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                _refreshTokenService.RevokeAllUserTokens(userId, TokenRevocationReason.UserLogout);
            }
            else
            {
                var userTokens = _refreshTokensCollection
                    .Find(t => t.UserId == userId && !t.IsRevoked)
                    .ToList();

                foreach (var token in userTokens)
                {
                    if (TokenSecurityUtils.VerifyToken(refreshToken, token.TokenHash, token.Salt))
                    {
                        _refreshTokenService.RevokeToken(token.TokenId, TokenRevocationReason.UserLogout);
                        break;
                    }
                }
            }
        }

        public List<object> GetActiveUserSessions(string userId)
        {
            var activeTokens = _refreshTokenService.GetActiveUserTokens(userId);
            
            return activeTokens.Select(token => new
            {
                TokenId = token.TokenId,
                DeviceInfo = token.DeviceInfo,
                IpAddress = token.IpAddress,
                CreatedAt = token.CreatedAt,
                LastUsedAt = token.LastUsedAt,
                ExpiresAt = token.ExpiresAt
            }).Cast<object>().ToList();
        }

        public void RevokeUserSession(string userId, string tokenId)
        {
            var token = _refreshTokensCollection
                .Find(t => t.TokenId == tokenId && t.UserId == userId)
                .FirstOrDefault();

            if (token != null)
            {
                _refreshTokenService.RevokeToken(tokenId, TokenRevocationReason.UserLogout);
            }
        }

        public void CleanupExpiredTokens()
        {
            _refreshTokenService.CleanupExpiredTokens();
        }

        private RefreshTokenContext CreateDefaultContext()
        {
            var context = HttpContext.Current;
            return new RefreshTokenContext
            {
                UserAgent = context?.Request?.UserAgent ?? "Unknown",
                IpAddress = GetClientIpAddress(context),
                RequestTime = DateTime.UtcNow
            };
        }

        private string GetClientIpAddress(HttpContext context)
        {
            if (context?.Request == null) return "Unknown";

            var forwardedFor = context.Request.Headers["X-Forwarded-For"];
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',');
                if (ips.Length > 0)
                {
                    return ips[0].Trim();
                }
            }

            var realIp = context.Request.Headers["X-Real-IP"];
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp.Trim();
            }

            return context.Request.UserHostAddress ?? "Unknown";
        }

        private AuthenticationStatus GetInactiveUserStatus(User user)
        {
            if (AuthUtils.IsEVOwner(user.RoleId))
            {
                var evOwner = _evOwnersCollection.Find(o => o.UserId == user.Id).FirstOrDefault();
                if (evOwner != null)
                {
                    return AuthenticationStatus.EVOwnerDeactivated;
                }
            }
            return AuthenticationStatus.UserInactive;
        }

        private object GetUserInfoForResponse(User user)
        {
            if (AuthUtils.IsEVOwner(user.RoleId))
            {
                var evOwner = _evOwnersCollection.Find(o => o.UserId == user.Id).FirstOrDefault();
                if (evOwner != null)
                {
                    return new 
                    { 
                        user.Id, 
                        user.Username, 
                        user.Email,
                        user.FirstName,
                        user.LastName,
                        RoleId = user.RoleId,
                        RoleName = AuthUtils.GetRoleName(user.RoleId),
                        NIC = evOwner.NIC,
                        Phone = evOwner.Phone
                    };
                }
            }

            // For Admin and Station Users, include FirstName and LastName
            return new 
            { 
                user.Id, 
                user.Username, 
                user.Email,
                user.FirstName,
                user.LastName,
                RoleId = user.RoleId,
                RoleName = AuthUtils.GetRoleName(user.RoleId)
            };
        }
    }
}