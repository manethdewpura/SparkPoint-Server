/*
 * UsersController.cs
 * 
 * This controller handles all HTTP requests related to user management operations.
 * It provides endpoints for admin registration, user profile management, user listing,
 * and account activation/deactivation. All operations are secured with appropriate
 * role-based authorization.
 * 
 */

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

        // Constructor: Initializes the UserService dependency
        public UsersController()
        {
            _userService = new UserService();
        }

        // Registers a new admin user (admin only)
        [HttpPost]
        [Route("admin/register")]
        [AdminOnly]
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

        // Updates user profile (admin and station users only)
        [HttpPatch]
        [Route("profile")]
        [AdminAndStationUser]
        [OwnAccountMiddleware]
        public IHttpActionResult UpdateProfile(UserUpdateModel model)
        {
            var currentUserId = UserContextHelper.GetCurrentUserId(this);
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            var currentUserRoleId = UserContextHelper.GetCurrentUserRoleId(this);
            if (!currentUserRoleId.HasValue)
                return Unauthorized();

            if (!_userService.CheckUserRole(currentUserId, ApplicationConstants.AdminRoleId) && 
                !_userService.CheckUserRole(currentUserId, ApplicationConstants.StationUserRoleId))
                return BadRequest("Only Admin and Station User roles can update their profiles through this endpoint.");

            var result = _userService.UpdateProfile(currentUserId, model);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.Message);
            }

            return Ok(result.Message);
        }

        // Gets current user profile (admin and station users only)
        [HttpGet]
        [Route("profile")]
        [AdminAndStationUser]
        [OwnAccountMiddleware]
        public IHttpActionResult GetProfile()
        {
            var currentUserId = UserContextHelper.GetCurrentUserId(this);
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            var currentUserRoleId = UserContextHelper.GetCurrentUserRoleId(this);
            if (!currentUserRoleId.HasValue)
                return Unauthorized();


            if (!_userService.CheckUserRole(currentUserId, ApplicationConstants.AdminRoleId) && 
                !_userService.CheckUserRole(currentUserId, ApplicationConstants.StationUserRoleId))
                return BadRequest("Only Admin and Station User roles can access their profiles through this endpoint.");

            var result = _userService.GetProfile(currentUserId);
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.UserProfile);
        }

        // Creates a new station user (admin only)
        [HttpPost]
        [Route("station-user")]
        [AdminOnly]
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

        // Gets station user profile by user ID (admin only)
        [HttpGet]
        [Route("station-user/{userId}")]
        [AdminOnly]
        public IHttpActionResult GetStationUserProfile(string userId)
        {
            var result = _userService.GetStationUserProfile(userId);
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.UserProfile);
        }

        // Updates station user profile (admin only)
        [HttpPatch]
        [Route("station-user/{userId}")]
        [AdminOnly]
        public IHttpActionResult UpdateStationUser(string userId, UserUpdateModel model)
        {
            var result = _userService.UpdateStationUser(userId, model);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.Message);
            }

            return Ok(result.Message);
        }

        // Gets all station users with optional filtering (admin only)
        [HttpGet]
        [Route("station-users")]
        [AdminOnly]
        public IHttpActionResult GetAllStationUsers([FromUri] UserListFilterModel filter = null)
        {
            var result = _userService.GetAllStationUsers(filter);
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.UserProfile);
        }

        // Converts user operation status to appropriate HTTP response
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
