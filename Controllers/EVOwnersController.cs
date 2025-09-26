using System;
using System.Linq;
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
    [RoutePrefix("api/evowners")]
    public class EVOwnersController : ApiController
    {
        private readonly IMongoCollection<EVOwner> _evOwnersCollection;
        private readonly IMongoCollection<User> _usersCollection;

        public EVOwnersController()
        {
            var dbContext = new MongoDbContext();
            _evOwnersCollection = dbContext.GetCollection<EVOwner>("EVOwners");
            _usersCollection = dbContext.GetCollection<User>("Users");
        }

        [HttpPost]
        [Route("register")]
        public IHttpActionResult Register(EVOwnerRegisterModel model)
        {
            if (_usersCollection.Find(u => u.Username == model.Username).Any())
                return BadRequest("Username already exists.");

            if (_usersCollection.Find(u => u.Email == model.Email).Any())
                return BadRequest("Email already exists.");

            if (_evOwnersCollection.Find(o => o.Phone == model.Phone).Any())
                return BadRequest("Phone already exists.");

            if (_evOwnersCollection.Find(o => o.NIC == model.NIC).Any())
                return BadRequest("NIC already exists.");

            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PasswordHash = HashPassword(model.Password),
                RoleId = 3,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            _usersCollection.InsertOne(user);

            var evOwner = new EVOwner
            {
                NIC = model.NIC,
                Phone = model.Phone,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            _evOwnersCollection.InsertOne(evOwner);
            
            return Ok("EV Owner registration successful.");
        }

        [HttpPut]
        [Route("update")]
        [RoleAuthorizeMiddleware("3")]
        [OwnAccountMiddleware]
        public IHttpActionResult UpdateProfile(EVOwnerUpdateModel model)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            var currentUser = _usersCollection.Find(u => u.Id == currentUserId).FirstOrDefault();
            var currentEVOwner = _evOwnersCollection.Find(o => o.UserId == currentUserId).FirstOrDefault();

            if (currentUser == null || currentEVOwner == null)
                return BadRequest("User not found.");

            if (!currentUser.IsActive)
                return BadRequest("Account is deactivated and cannot be updated.");

            if (!string.IsNullOrEmpty(model.Email) && model.Email != currentUser.Email)
            {
                if (_usersCollection.Find(u => u.Email == model.Email && u.Id != currentUserId).Any())
                    return BadRequest("Email already exists.");
            }

            if (!string.IsNullOrEmpty(model.Phone) && model.Phone != currentEVOwner.Phone)
            {
                if (_evOwnersCollection.Find(o => o.Phone == model.Phone && o.NIC != currentEVOwner.NIC).Any())
                    return BadRequest("Phone already exists.");
            }

            var userUpdate = Builders<User>.Update
                .Set(u => u.UpdatedAt, DateTime.UtcNow);

            if (!string.IsNullOrEmpty(model.FirstName))
                userUpdate = userUpdate.Set(u => u.FirstName, model.FirstName);
            
            if (!string.IsNullOrEmpty(model.LastName))
                userUpdate = userUpdate.Set(u => u.LastName, model.LastName);
            
            if (!string.IsNullOrEmpty(model.Email))
                userUpdate = userUpdate.Set(u => u.Email, model.Email);

            if (!string.IsNullOrEmpty(model.Password))
                userUpdate = userUpdate.Set(u => u.PasswordHash, HashPassword(model.Password));

            _usersCollection.UpdateOne(u => u.Id == currentUserId, userUpdate);

            var evOwnerUpdate = Builders<EVOwner>.Update
                .Set(o => o.UpdatedAt, DateTime.UtcNow);

            if (!string.IsNullOrEmpty(model.Phone))
                evOwnerUpdate = evOwnerUpdate.Set(o => o.Phone, model.Phone);

            _evOwnersCollection.UpdateOne(o => o.UserId == currentUserId, evOwnerUpdate);

            return Ok("Profile updated successfully.");
        }

        [HttpPut]
        [Route("deactivate")]
        [RoleAuthorizeMiddleware("3")]
        [OwnAccountMiddleware]
        public IHttpActionResult DeactivateAccount()
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            var currentUser = _usersCollection.Find(u => u.Id == currentUserId).FirstOrDefault();
            if (currentUser == null)
                return BadRequest("User not found.");

            if (!currentUser.IsActive)
                return BadRequest("Account is already deactivated.");

            var update = Builders<User>.Update
                .Set(u => u.IsActive, false)
                .Set(u => u.UpdatedAt, DateTime.UtcNow);

            _usersCollection.UpdateOne(u => u.Id == currentUserId, update);

            return Ok("Account deactivated successfully.");
        }

        [HttpPut]
        [Route("reactivate/{nic}")]
        [RoleAuthorizeMiddleware("1")]
        [OwnAccountMiddleware("nic")]
        public IHttpActionResult ReactivateAccount(string nic)
        {
            var evOwner = _evOwnersCollection.Find(o => o.NIC == nic).FirstOrDefault();
            if (evOwner == null)
                return BadRequest("EV Owner not found.");

            var user = _usersCollection.Find(u => u.Id == evOwner.UserId).FirstOrDefault();
            if (user == null)
                return BadRequest("User not found.");

            if (user.IsActive)
                return BadRequest("Account is already active.");

            var update = Builders<User>.Update
                .Set(u => u.IsActive, true)
                .Set(u => u.UpdatedAt, DateTime.UtcNow);

            _usersCollection.UpdateOne(u => u.Id == evOwner.UserId, update);

            return Ok("Account reactivated successfully.");
        }

        [HttpGet]
        [Route("profile")]
        [RoleAuthorizeMiddleware("3")]
        [OwnAccountMiddleware]
        public IHttpActionResult GetProfile()
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            var currentUser = _usersCollection.Find(u => u.Id == currentUserId).FirstOrDefault();
            var currentEVOwner = _evOwnersCollection.Find(o => o.UserId == currentUserId).FirstOrDefault();

            if (currentUser == null || currentEVOwner == null)
                return BadRequest("User not found.");

            var profile = new
            {
                currentUser.Id,
                currentUser.Username,
                currentUser.Email,
                currentUser.FirstName,
                currentUser.LastName,
                currentUser.IsActive,
                currentEVOwner.NIC,
                currentEVOwner.Phone,
                currentUser.CreatedAt,
                currentUser.UpdatedAt
            };

            return Ok(profile);
        }

        [HttpGet]
        [Route("profile/{nic}")]
        [RoleAuthorizeMiddleware("1", "2")]
        [OwnAccountMiddleware("nic")]
        public IHttpActionResult GetProfileByNic(string nic)
        {
            var evOwner = _evOwnersCollection.Find(o => o.NIC == nic).FirstOrDefault();
            if (evOwner == null)
                return BadRequest("EV Owner not found.");

            var user = _usersCollection.Find(u => u.Id == evOwner.UserId).FirstOrDefault();
            if (user == null)
                return BadRequest("User not found.");

            var profile = new
            {
                user.Id,
                user.Username,
                user.Email,
                user.FirstName,
                user.LastName,
                user.IsActive,
                evOwner.NIC,
                evOwner.Phone,
                user.CreatedAt,
                user.UpdatedAt
            };

            return Ok(profile);
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
    }

    public class EVOwnerRegisterModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Password { get; set; }
        public string NIC { get; set; }
        public string Phone { get; set; }
    }

    public class EVOwnerUpdateModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Password { get; set; }
    }
}
