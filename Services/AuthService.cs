using System;
using System.Linq;
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

        public AuthService()
        {
            var dbContext = new MongoDbContext();
            _usersCollection = dbContext.GetCollection<User>(AuthConstants.UsersCollection);
            _refreshTokensCollection = dbContext.GetCollection<RefreshTokenEntry>(AuthConstants.RefreshTokensCollection);
            _evOwnersCollection = dbContext.GetCollection<EVOwner>(AuthConstants.EVOwnersCollection);
        }

        public AuthenticationResult AuthenticateUser(string username, string password)
        {
            // Find user by username
            var user = _usersCollection.Find(u => u.Username == username).FirstOrDefault();
            if (user == null || !PasswordUtils.VerifyPassword(password, user.PasswordHash))
            {
                return AuthenticationResult.Failed(AuthenticationStatus.InvalidCredentials);
            }

            // Check if user is active
            if (!user.IsActive)
            {
                var status = GetInactiveUserStatus(user);
                return AuthenticationResult.Failed(status);
            }

            // Generate tokens
            var accessToken = JwtHelper.GenerateAccessToken(user);
            var refreshToken = JwtHelper.GenerateRefreshToken();

            // Store refresh token
            StoreRefreshToken(user.Id, refreshToken);

            // Get user info with additional details if needed
            var userInfo = GetUserInfoForResponse(user);

            return AuthenticationResult.Success(accessToken, refreshToken, userInfo);
        }

        public TokenRefreshResult RefreshToken(string userId, string refreshToken)
        {
            // Find user
            var user = _usersCollection.Find(u => u.Id == userId).FirstOrDefault();
            if (user == null)
            {
                return TokenRefreshResult.Failed(TokenRefreshStatus.UserNotFound);
            }

            // Validate refresh token
            var refreshEntry = _refreshTokensCollection
                .Find(x => x.UserId == userId && x.Token == refreshToken)
                .FirstOrDefault();

            if (refreshEntry == null)
            {
                return TokenRefreshResult.Failed(TokenRefreshStatus.InvalidRefreshToken);
            }

            // Check if user is still active
            if (!user.IsActive)
            {
                return TokenRefreshResult.Failed(TokenRefreshStatus.UserInactive);
            }

            // Generate new tokens
            var newAccessToken = JwtHelper.GenerateAccessToken(user);
            var newRefreshToken = JwtHelper.GenerateRefreshToken();

            // Update refresh token in database
            refreshEntry.Token = newRefreshToken;
            _refreshTokensCollection.ReplaceOne(x => x.UserId == userId, refreshEntry);

            return TokenRefreshResult.Success(newAccessToken, newRefreshToken);
        }

        private void StoreRefreshToken(string userId, string refreshToken)
        {
            var refreshEntry = new RefreshTokenEntry 
            { 
                UserId = userId, 
                Token = refreshToken 
            };

            _refreshTokensCollection.ReplaceOne(
                x => x.UserId == userId,
                refreshEntry,
                new ReplaceOptions { IsUpsert = true }
            );
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
                        RoleId = user.RoleId,
                        RoleName = AuthUtils.GetRoleName(user.RoleId),
                        NIC = evOwner.NIC 
                    };
                }
            }

            return new 
            { 
                user.Id, 
                user.Username, 
                user.Email, 
                RoleId = user.RoleId,
                RoleName = AuthUtils.GetRoleName(user.RoleId)
            };
        }
    }
}