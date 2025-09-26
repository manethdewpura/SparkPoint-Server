using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using SparkPoint_Server.Models;
using SparkPoint_Server.Helpers;
using SparkPoint_Server.Utils;
using SparkPoint_Server.Constants;
using SparkPoint_Server.Enums;

namespace SparkPoint_Server.Services
{
    public class RefreshTokenService
    {
        private readonly IMongoCollection<RefreshTokenEntry> _refreshTokensCollection;
        private readonly IMongoCollection<User> _usersCollection;

        public RefreshTokenService()
        {
            var dbContext = new MongoDbContext();
            _refreshTokensCollection = dbContext.GetCollection<RefreshTokenEntry>(AuthConstants.RefreshTokensCollection);
            _usersCollection = dbContext.GetCollection<User>(AuthConstants.UsersCollection);
            
            EnsureIndexes();
        }

        public RefreshTokenEntry CreateRefreshToken(string userId, RefreshTokenContext context, string parentTokenId = null)
        {
            var token = TokenSecurityUtils.GenerateRefreshToken();
            var salt = TokenSecurityUtils.GenerateSalt();
            var tokenId = TokenSecurityUtils.GenerateTokenId();
            var familyId = parentTokenId != null 
                ? GetTokenFamilyId(parentTokenId) ?? TokenSecurityUtils.GenerateTokenId()
                : TokenSecurityUtils.GenerateTokenId();

            var tokenEntry = new RefreshTokenEntry
            {
                UserId = userId,
                TokenId = tokenId,
                TokenHash = TokenSecurityUtils.HashToken(token, salt),
                Salt = salt,
                FamilyId = familyId,
                DeviceInfo = TokenSecurityUtils.ExtractDeviceInfo(context.UserAgent),
                UserAgent = context.UserAgent,
                IpAddress = context.IpAddress,
                CreatedAt = context.RequestTime,
                ExpiresAt = context.RequestTime.AddDays(AuthConstants.RefreshTokenExpiryDays),
                ParentTokenId = parentTokenId
            };

            _refreshTokensCollection.InsertOne(tokenEntry);

            CleanupOldTokensForUser(userId);

            tokenEntry.TokenHash = token;
            return tokenEntry;
        }

        public RefreshTokenValidationResult ValidateAndConsumeToken(string userId, string token, RefreshTokenContext context)
        {
            var userTokens = _refreshTokensCollection
                .Find(t => t.UserId == userId && !t.IsRevoked)
                .ToList();

            RefreshTokenEntry matchingToken = null;

            foreach (var tokenEntry in userTokens)
            {
                if (TokenSecurityUtils.VerifyToken(token, tokenEntry.TokenHash, tokenEntry.Salt))
                {
                    matchingToken = tokenEntry;
                    break;
                }
            }

            if (matchingToken == null)
            {
                return RefreshTokenValidationResult.Failed(TokenRefreshStatus.InvalidRefreshToken);
            }

            if (matchingToken.IsExpired)
            {
                RevokeToken(matchingToken.TokenId, TokenRevocationReason.ExpiredCleanup);
                return RefreshTokenValidationResult.Failed(TokenRefreshStatus.ExpiredRefreshToken);
            }

            if (matchingToken.IsUsed)
            {
                RevokeFamilyTokens(matchingToken.FamilyId, TokenRevocationReason.ReuseDetected);
                return RefreshTokenValidationResult.Failed(TokenRefreshStatus.TokenFamilyRevoked);
            }

            MarkTokenAsUsed(matchingToken.TokenId, context.RequestTime);

            return RefreshTokenValidationResult.Success(matchingToken);
        }

        public void RevokeToken(string tokenId, TokenRevocationReason reason)
        {
            var update = Builders<RefreshTokenEntry>.Update
                .Set(t => t.IsRevoked, true)
                .Set(t => t.RevokedAt, DateTime.UtcNow)
                .Set(t => t.RevokedReason, reason.ToString());

            _refreshTokensCollection.UpdateOne(t => t.TokenId == tokenId, update);
        }

        public void RevokeFamilyTokens(string familyId, TokenRevocationReason reason)
        {
            var update = Builders<RefreshTokenEntry>.Update
                .Set(t => t.IsRevoked, true)
                .Set(t => t.RevokedAt, DateTime.UtcNow)
                .Set(t => t.RevokedReason, reason.ToString());

            _refreshTokensCollection.UpdateMany(t => t.FamilyId == familyId, update);
        }

        public void RevokeAllUserTokens(string userId, TokenRevocationReason reason)
        {
            var update = Builders<RefreshTokenEntry>.Update
                .Set(t => t.IsRevoked, true)
                .Set(t => t.RevokedAt, DateTime.UtcNow)
                .Set(t => t.RevokedReason, reason.ToString());

            _refreshTokensCollection.UpdateMany(t => t.UserId == userId, update);
        }

        public List<RefreshTokenEntry> GetActiveUserTokens(string userId)
        {
			var now = DateTime.UtcNow;
			return _refreshTokensCollection
				.Find(t => t.UserId == userId && !t.IsRevoked && !t.IsUsed && t.ExpiresAt > now)
                .SortByDescending(t => t.CreatedAt)
                .ToList();
        }

        public void CleanupExpiredTokens()
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-AuthConstants.RevokedTokenRetentionDays);
            
            _refreshTokensCollection.DeleteMany(t => t.ExpiresAt < DateTime.UtcNow);
            
            _refreshTokensCollection.DeleteMany(t => t.IsRevoked && t.RevokedAt < cutoffDate);
        }

        private void MarkTokenAsUsed(string tokenId, DateTime usedAt)
        {
            var update = Builders<RefreshTokenEntry>.Update
                .Set(t => t.IsUsed, true)
                .Set(t => t.UsedAt, usedAt)
                .Set(t => t.LastUsedAt, usedAt);

            _refreshTokensCollection.UpdateOne(t => t.TokenId == tokenId, update);
        }

        private string GetTokenFamilyId(string tokenId)
        {
            var token = _refreshTokensCollection
                .Find(t => t.TokenId == tokenId)
                .FirstOrDefault();
            
            return token?.FamilyId;
        }

        private void CleanupOldTokensForUser(string userId)
        {
            var activeTokens = GetActiveUserTokens(userId);
            
            if (activeTokens.Count > AuthConstants.MaxRefreshTokensPerUser)
            {
                var tokensToRevoke = activeTokens
                    .Skip(AuthConstants.MaxRefreshTokensPerUser)
                    .ToList();

                foreach (var token in tokensToRevoke)
                {
                    RevokeToken(token.TokenId, TokenRevocationReason.ExpiredCleanup);
                }
            }
        }

        private void EnsureIndexes()
        {
            var indexKeys = Builders<RefreshTokenEntry>.IndexKeys;

            var tokenHashIndex = new CreateIndexModel<RefreshTokenEntry>(
                indexKeys.Ascending(x => x.TokenHash),
                new CreateIndexOptions { Unique = true, Name = "tokenHash_unique" }
            );

            var userTokenIndex = new CreateIndexModel<RefreshTokenEntry>(
                indexKeys.Ascending(x => x.UserId).Ascending(x => x.TokenHash),
                new CreateIndexOptions { Name = "userId_tokenHash" }
            );

            var familyIndex = new CreateIndexModel<RefreshTokenEntry>(
                indexKeys.Ascending(x => x.FamilyId),
                new CreateIndexOptions { Name = "familyId" }
            );

            var expiryIndex = new CreateIndexModel<RefreshTokenEntry>(
                indexKeys.Ascending(x => x.ExpiresAt),
                new CreateIndexOptions { Name = "expiresAt" }
            );

            var tokenIdIndex = new CreateIndexModel<RefreshTokenEntry>(
                indexKeys.Ascending(x => x.TokenId),
                new CreateIndexOptions { Unique = true, Name = "tokenId_unique" }
            );

            _refreshTokensCollection.Indexes.CreateMany(new[]
            {
                tokenHashIndex,
                userTokenIndex,
                familyIndex,
                expiryIndex,
                tokenIdIndex
            });
        }
    }

    public class RefreshTokenValidationResult
    {
        public TokenRefreshStatus Status { get; private set; }
        public bool IsSuccess => Status == TokenRefreshStatus.Success;
        public string ErrorMessage { get; private set; }
        public RefreshTokenEntry TokenEntry { get; private set; }

        private RefreshTokenValidationResult() { }

        public static RefreshTokenValidationResult Success(RefreshTokenEntry tokenEntry)
        {
            return new RefreshTokenValidationResult
            {
                Status = TokenRefreshStatus.Success,
                TokenEntry = tokenEntry
            };
        }

        public static RefreshTokenValidationResult Failed(TokenRefreshStatus status, string customMessage = null)
        {
            return new RefreshTokenValidationResult
            {
                Status = status,
                ErrorMessage = customMessage ?? GetDefaultErrorMessage(status)
            };
        }

        private static string GetDefaultErrorMessage(TokenRefreshStatus status)
        {
            switch (status)
            {
                case TokenRefreshStatus.InvalidRefreshToken:
                    return AuthConstants.InvalidRefreshToken;
                case TokenRefreshStatus.ExpiredRefreshToken:
                    return AuthConstants.ExpiredRefreshToken;
                case TokenRefreshStatus.RevokedRefreshToken:
                    return AuthConstants.RevokedRefreshToken;
                case TokenRefreshStatus.TokenFamilyRevoked:
                    return AuthConstants.TokenFamilyRevoked;
                default:
                    return "Token validation failed";
            }
        }
    }
}