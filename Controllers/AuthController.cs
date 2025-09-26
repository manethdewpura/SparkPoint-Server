using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Http;
using SparkPoint_Server.Models;
using SparkPoint_Server.Helpers;
using MongoDB.Driver;

namespace SparkPoint_Server.Controllers
{
    [RoutePrefix("api/auth")]
    public class AuthController : ApiController
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<RefreshTokenEntry> _refreshTokensCollection;
        private readonly IMongoCollection<EVOwner> _evOwnersCollection;

        public AuthController()
        {
            var dbContext = new MongoDbContext();
            _usersCollection = dbContext.GetCollection<User>("Users");
            _refreshTokensCollection = dbContext.GetCollection<RefreshTokenEntry>("RefreshTokens");
            _evOwnersCollection = dbContext.GetCollection<EVOwner>("EVOwners");
        }

        [HttpPost]
        [Route("login")]
        public IHttpActionResult Login(LoginModel model)
        {
            var user = _usersCollection.Find(u => u.Username == model.Username).FirstOrDefault();
            if (user == null || !VerifyPassword(model.Password, user.PasswordHash))
                return Unauthorized();
            
            if (!user.IsActive)
            {
                var evOwner = _evOwnersCollection.Find(o => o.UserId == user.Id).FirstOrDefault();
                if (evOwner != null)
                    return BadRequest("Your EV Owner account has been deactivated. Please contact a back-office officer for reactivation.");
                else
                    return BadRequest("User account is inactive.");
            }

            var accessToken = JwtHelper.GenerateAccessToken(user);
            var refreshToken = JwtHelper.GenerateRefreshToken();
            var refreshEntry = new RefreshTokenEntry { UserId = user.Id, Token = refreshToken };
            _refreshTokensCollection.ReplaceOne(
                x => x.UserId == user.Id,
                refreshEntry,
                new ReplaceOptions { IsUpsert = true }
            );

            object userInfo;
            if (user.RoleId == 3)
            {
                var evOwner = _evOwnersCollection.Find(o => o.UserId == user.Id).FirstOrDefault();
                if (evOwner != null)
                {
                    userInfo = new { user.Id, user.Username, user.Email, RoleId = user.RoleId, NIC = evOwner.NIC };
                }
                else
                {
                    userInfo = new { user.Id, user.Username, user.Email, RoleId = user.RoleId };
                }
            }
            else
            {
                userInfo = new { user.Id, user.Username, user.Email, RoleId = user.RoleId };
            }

            return Ok(new
            {
                accessToken,
                refreshToken,
                user = userInfo
            });
        }

        [HttpPost]
        [Route("refresh")]
        public IHttpActionResult Refresh(RefreshModel model)
        {
            var user = _usersCollection.Find(u => u.Id == model.UserId).FirstOrDefault();
            var refreshEntry = _refreshTokensCollection.Find(x => x.UserId == model.UserId && x.Token == model.RefreshToken).FirstOrDefault();
            if (user == null || refreshEntry == null)
                return Unauthorized();
            
            if (!user.IsActive)
                return BadRequest("User account is inactive.");
                
            var accessToken = JwtHelper.GenerateAccessToken(user);
            var newRefreshToken = JwtHelper.GenerateRefreshToken();
            refreshEntry.Token = newRefreshToken;
            _refreshTokensCollection.ReplaceOne(x => x.UserId == user.Id, refreshEntry);
            return Ok(new { accessToken, refreshToken = newRefreshToken });
        }
        private string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }
        private bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }
    }

    public class RegisterModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }
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
        public string UserId { get; set; }
        public string Token { get; set; }
    }
}
