using System;
using System.Linq;
using MongoDB.Driver;
using SparkPoint_Server.Models;
using SparkPoint_Server.Helpers;
using SparkPoint_Server.Utils;

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
            _usersCollection = dbContext.GetCollection<User>("Users");
            _refreshTokensCollection = dbContext.GetCollection<RefreshTokenEntry>("RefreshTokens");
            _evOwnersCollection = dbContext.GetCollection<EVOwner>("EVOwners");
        }

        public AuthenticationResult AuthenticateUser(string username, string password)
        {
            // Find user by username
            var user = _usersCollection.Find(u => u.Username == username).FirstOrDefault();
            if (user == null || !PasswordUtils.VerifyPassword(password, user.PasswordHash))
            {
                return AuthenticationResult.Failed("Invalid username or password");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                var errorMessage = GetInactiveUserMessage(user);
                return AuthenticationResult.Failed(errorMessage);
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
                return TokenRefreshResult.Failed("User not found");
            }

            // Validate refresh token
            var refreshEntry = _refreshTokensCollection
                .Find(x => x.UserId == userId && x.Token == refreshToken)
                .FirstOrDefault();

            if (refreshEntry == null)
            {
                return TokenRefreshResult.Failed("Invalid refresh token");
            }

            // Check if user is still active
            if (!user.IsActive)
            {
                return TokenRefreshResult.Failed("User account is inactive");
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

        private string GetInactiveUserMessage(User user)
        {
            var evOwner = _evOwnersCollection.Find(o => o.UserId == user.Id).FirstOrDefault();
            if (evOwner != null)
            {
                return "Your EV Owner account has been deactivated. Please contact a back-office officer for reactivation.";
            }
            return "User account is inactive.";
        }

        private object GetUserInfoForResponse(User user)
        {
            if (user.RoleId == 3)
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
                        NIC = evOwner.NIC 
                    };
                }
            }

            return new 
            { 
                user.Id, 
                user.Username, 
                user.Email, 
                RoleId = user.RoleId 
            };
        }
    }

    public class AuthenticationResult
    {
        public bool IsSuccess { get; private set; }
        public string ErrorMessage { get; private set; }
        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }
        public object UserInfo { get; private set; }

        private AuthenticationResult() { }

        public static AuthenticationResult Success(string accessToken, string refreshToken, object userInfo)
        {
            return new AuthenticationResult
            {
                IsSuccess = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserInfo = userInfo
            };
        }

        public static AuthenticationResult Failed(string errorMessage)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }

    public class TokenRefreshResult
    {
        public bool IsSuccess { get; private set; }
        public string ErrorMessage { get; private set; }
        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }

        private TokenRefreshResult() { }

        public static TokenRefreshResult Success(string accessToken, string refreshToken)
        {
            return new TokenRefreshResult
            {
                IsSuccess = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        public static TokenRefreshResult Failed(string errorMessage)
        {
            return new TokenRefreshResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }
}