using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Web.Http;
using SparkPoint_Server.Models;
using SparkPoint_Server.Helpers;
using SparkPoint_Server.Attributes;
using MongoDB.Driver;

namespace SparkPoint_Server.Controllers
{
    [RoutePrefix("api/users")]
    public class UsersController : ApiController
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<ChargingStation> _stationsCollection;

        public UsersController()
        {
            var dbContext = new MongoDbContext();
            _usersCollection = dbContext.GetCollection<User>("Users");
            _stationsCollection = dbContext.GetCollection<ChargingStation>("ChargingStations");
        }

        [HttpPost]
        [Route("admin/register")]
        public IHttpActionResult RegisterAdmin(RegisterModel model)
        {
            if (_usersCollection.Find(u => u.Username == model.Username || u.Email == model.Email).Any())
                return BadRequest("Username or email already exists.");

            if (string.IsNullOrEmpty(model.Username))
                return BadRequest("Username is required.");

            if (string.IsNullOrEmpty(model.Email))
                return BadRequest("Email is required.");

            if (string.IsNullOrEmpty(model.Password))
                return BadRequest("Password is required.");

            var adminUser = new User
            {
                Username = model.Username,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PasswordHash = HashPassword(model.Password),
                RoleId = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _usersCollection.InsertOne(adminUser);

            return Ok(new { Message = "Admin registration successful.", UserId = adminUser.Id });
        }

        [HttpPut]
        [Route("profile")]
        [RoleAuthorizeMiddleware("1")]
        [OwnAccountMiddleware]
        public IHttpActionResult UpdateProfile(UserUpdateModel model)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            var currentUser = _usersCollection.Find(u => u.Id == currentUserId).FirstOrDefault();
            if (currentUser == null)
                return BadRequest("User not found.");

            if (!currentUser.IsActive)
                return BadRequest("Account is deactivated and cannot be updated.");

            if (currentUser.RoleId != 1)
                return BadRequest("This endpoint is only for Admin users.");

            if (!string.IsNullOrEmpty(model.Email) && model.Email != currentUser.Email)
            {
                if (_usersCollection.Find(u => u.Email == model.Email && u.Id != currentUserId).Any())
                    return BadRequest("Email already exists.");
            }

            if (!string.IsNullOrEmpty(model.Username) && model.Username != currentUser.Username)
            {
                if (_usersCollection.Find(u => u.Username == model.Username && u.Id != currentUserId).Any())
                    return BadRequest("Username already exists.");
            }

            var updateBuilder = Builders<User>.Update.Set(u => u.UpdatedAt, DateTime.UtcNow);

            if (!string.IsNullOrEmpty(model.FirstName))
                updateBuilder = updateBuilder.Set(u => u.FirstName, model.FirstName);

            if (!string.IsNullOrEmpty(model.LastName))
                updateBuilder = updateBuilder.Set(u => u.LastName, model.LastName);

            if (!string.IsNullOrEmpty(model.Email))
                updateBuilder = updateBuilder.Set(u => u.Email, model.Email);

            if (!string.IsNullOrEmpty(model.Username))
                updateBuilder = updateBuilder.Set(u => u.Username, model.Username);

            if (!string.IsNullOrEmpty(model.Password))
                updateBuilder = updateBuilder.Set(u => u.PasswordHash, HashPassword(model.Password));

            _usersCollection.UpdateOne(u => u.Id == currentUserId, updateBuilder);

            return Ok("Profile updated successfully.");
        }

        [HttpGet]
        [Route("profile")]
        [RoleAuthorizeMiddleware("1")]
        [OwnAccountMiddleware]
        public IHttpActionResult GetProfile()
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            var currentUser = _usersCollection.Find(u => u.Id == currentUserId).FirstOrDefault();
            if (currentUser == null)
                return BadRequest("User not found.");

            if (currentUser.RoleId != 1)
                return BadRequest("This endpoint is only for Admin users.");

            var profile = new
            {
                currentUser.Id,
                currentUser.Username,
                currentUser.Email,
                currentUser.FirstName,
                currentUser.LastName,
                currentUser.RoleId,
                RoleName = GetRoleName(currentUser.RoleId),
                currentUser.IsActive,
                currentUser.CreatedAt,
                currentUser.UpdatedAt
            };

            return Ok(profile);
        }

        [HttpPost]
        [Route("station-user")]
        [RoleAuthorizeMiddleware("1")]
        public IHttpActionResult CreateStationUser(CreateStationUserModel model)
        {
            if (model == null)
                return BadRequest("User data is required.");

            if (string.IsNullOrEmpty(model.Username))
                return BadRequest("Username is required.");

            if (string.IsNullOrEmpty(model.Email))
                return BadRequest("Email is required.");

            if (string.IsNullOrEmpty(model.Password))
                return BadRequest("Password is required.");

            if (string.IsNullOrEmpty(model.ChargingStationId))
                return BadRequest("Charging station ID is required.");

            if (_usersCollection.Find(u => u.Username == model.Username).Any())
                return BadRequest("Username already exists.");

            if (_usersCollection.Find(u => u.Email == model.Email).Any())
                return BadRequest("Email already exists.");

            var station = _stationsCollection.Find(s => s.Id == model.ChargingStationId).FirstOrDefault();
            if (station == null)
                return BadRequest("Charging station not found.");

            var stationUser = new User
            {
                Username = model.Username,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                RoleId = 2,
                PasswordHash = HashPassword(model.Password),
                ChargingStationId = model.ChargingStationId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _usersCollection.InsertOne(stationUser);

            return Ok(new { Message = "Station user created successfully.", UserId = stationUser.Id });
        }

        [HttpGet]
        [Route("station-user/{userId}")]
        [RoleAuthorizeMiddleware("1")]
        public IHttpActionResult GetStationUserProfile(string userId)
        {
            var stationUser = _usersCollection.Find(u => u.Id == userId).FirstOrDefault();
            if (stationUser == null)
                return BadRequest("User not found.");
            
            if (stationUser.RoleId != 2)
                return BadRequest("User is not a station user.");

            var profile = new
            {
                stationUser.Id,
                stationUser.Username,
                stationUser.Email,
                stationUser.FirstName,
                stationUser.LastName,
                stationUser.RoleId,
                RoleName = GetRoleName(stationUser.RoleId),
                stationUser.ChargingStationId,
                stationUser.IsActive,
                stationUser.CreatedAt,
                stationUser.UpdatedAt
            };

            return Ok(profile);
        }

        [HttpPut]
        [Route("station-user/{userId}")]
        [RoleAuthorizeMiddleware("1")]
        public IHttpActionResult UpdateStationUser(string userId, UserUpdateModel model)
        {
            var stationUser = _usersCollection.Find(u => u.Id == userId).FirstOrDefault();
            if (stationUser == null)
                return BadRequest("User not found.");

            if (stationUser.RoleId != 2)
                return BadRequest("User is not a station user.");

            if (!string.IsNullOrEmpty(model.Email) && model.Email != stationUser.Email)
            {
                if (_usersCollection.Find(u => u.Email == model.Email && u.Id != userId).Any())
                    return BadRequest("Email already exists.");
            }

            if (!string.IsNullOrEmpty(model.Username) && model.Username != stationUser.Username)
            {
                if (_usersCollection.Find(u => u.Username == model.Username && u.Id != userId).Any())
                    return BadRequest("Username already exists.");
            }

            var updateBuilder = Builders<User>.Update.Set(u => u.UpdatedAt, DateTime.UtcNow);

            if (!string.IsNullOrEmpty(model.FirstName))
                updateBuilder = updateBuilder.Set(u => u.FirstName, model.FirstName);

            if (!string.IsNullOrEmpty(model.LastName))
                updateBuilder = updateBuilder.Set(u => u.LastName, model.LastName);

            if (!string.IsNullOrEmpty(model.Email))
                updateBuilder = updateBuilder.Set(u => u.Email, model.Email);

            if (!string.IsNullOrEmpty(model.Username))
                updateBuilder = updateBuilder.Set(u => u.Username, model.Username);

            if (!string.IsNullOrEmpty(model.Password))
                updateBuilder = updateBuilder.Set(u => u.PasswordHash, HashPassword(model.Password));

            _usersCollection.UpdateOne(u => u.Id == userId, updateBuilder);

            return Ok("Station user updated successfully.");
        }

        private string GetCurrentUserId()
        {
            var authHeader = Request.Headers.Authorization;
            if (authHeader == null || authHeader.Scheme != "Bearer")
                return null;

            var principal = JwtHelper.ValidateToken(authHeader.Parameter);
            if (principal == null)
                return null;

            return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }

        private string GetRoleName(int roleId)
        {
            switch (roleId)
            {
                case 1: return "Admin";
                case 2: return "Station User";
                case 3: return "EV Owner";
                default: return "Unknown";
            }
        }
    }
}
