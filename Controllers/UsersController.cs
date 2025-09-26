using System;
using System.Security.Claims;
using System.Web.Http;
using SparkPoint_Server.Models;
using SparkPoint_Server.Services;
using SparkPoint_Server.Attributes;
using SparkPoint_Server.Helpers;
using SparkPoint_Server.Constants;
using SparkPoint_Server.Enums;

namespace SparkPoint_Server.Controllers
{
    [RoutePrefix("api/users")]
    public class UsersController : ApiController
    {
        private readonly UserService _userService;

        public UsersController()
        {
            _userService = new UserService();
        }

        [HttpPost]
        [Route("admin/register")]
        public IHttpActionResult RegisterAdmin(RegisterModel model)
        {
            if (model == null)
                return BadRequest(UserConstants.UserDataRequired);

            if (string.IsNullOrEmpty(model.Username))
                return BadRequest(UserConstants.UsernameRequired);

            if (string.IsNullOrEmpty(model.Email))
                return BadRequest(UserConstants.EmailRequired);

            if (string.IsNullOrEmpty(model.Password))
                return BadRequest(UserConstants.PasswordRequired);

            var result = _userService.RegisterAdmin(model);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.Message);
            }

            return Ok(new { Message = result.Message, UserId = ((dynamic)result.Data)?.UserId });
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

            if (!_userService.CheckUserRole(currentUserId, ApplicationConstants.AdminRoleId))
                return BadRequest(UserConstants.AdminOnlyEndpoint);

            var result = _userService.UpdateProfile(currentUserId, model);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.Message);
            }

            return Ok(result.Message);
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

            if (!_userService.CheckUserRole(currentUserId, ApplicationConstants.AdminRoleId))
                return BadRequest(UserConstants.AdminOnlyEndpoint);

            var result = _userService.GetProfile(currentUserId);
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.UserProfile);
        }

        [HttpPost]
        [Route("station-user")]
        [RoleAuthorizeMiddleware("1")]
        public IHttpActionResult CreateStationUser(CreateStationUserModel model)
        {
            if (model == null)
                return BadRequest(UserConstants.UserDataRequired);

            if (string.IsNullOrEmpty(model.Username))
                return BadRequest(UserConstants.UsernameRequired);

            if (string.IsNullOrEmpty(model.Email))
                return BadRequest(UserConstants.EmailRequired);

            if (string.IsNullOrEmpty(model.Password))
                return BadRequest(UserConstants.PasswordRequired);

            if (string.IsNullOrEmpty(model.ChargingStationId))
                return BadRequest(UserConstants.ChargingStationIdRequired);

            var result = _userService.CreateStationUser(model);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.Message);
            }

            return Ok(new { Message = result.Message, UserId = ((dynamic)result.Data)?.UserId });
        }

        [HttpGet]
        [Route("station-user/{userId}")]
        [RoleAuthorizeMiddleware("1")]
        public IHttpActionResult GetStationUserProfile(string userId)
        {
            var result = _userService.GetStationUserProfile(userId);
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.UserProfile);
        }

        [HttpPut]
        [Route("station-user/{userId}")]
        [RoleAuthorizeMiddleware("1")]
        public IHttpActionResult UpdateStationUser(string userId, UserUpdateModel model)
        {
            var result = _userService.UpdateStationUser(userId, model);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.Message);
            }

            return Ok(result.Message);
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

        private IHttpActionResult GetErrorResponse(UserOperationStatus status, string errorMessage)
        {
            switch (status)
            {
                case UserOperationStatus.UserNotFound:
                    return BadRequest(errorMessage);
                case UserOperationStatus.UsernameExists:
                case UserOperationStatus.EmailExists:
                    return BadRequest(errorMessage);
                case UserOperationStatus.ValidationFailed:
                    return BadRequest(errorMessage);
                case UserOperationStatus.AccountDeactivated:
                    return BadRequest(errorMessage);
                case UserOperationStatus.ChargingStationNotFound:
                    return BadRequest(errorMessage);
                case UserOperationStatus.NotStationUser:
                    return BadRequest(errorMessage);
                case UserOperationStatus.NotAuthorized:
                    return Unauthorized();
                default:
                    return BadRequest(errorMessage);
            }
        }
    }
}
