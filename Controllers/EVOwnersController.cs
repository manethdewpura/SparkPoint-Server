using System;
using System.Security.Claims;
using System.Web.Http;
using SparkPoint_Server.Models;
using SparkPoint_Server.Services;
using SparkPoint_Server.Attributes;
using SparkPoint_Server.Helpers;
using SparkPoint_Server.Enums;

namespace SparkPoint_Server.Controllers
{
    [RoutePrefix("api/evowners")]
    public class EVOwnersController : ApiController
    {
        private readonly EVOwnerService _evOwnerService;

        public EVOwnersController()
        {
            _evOwnerService = new EVOwnerService();
        }

        [HttpPost]
        [Route("register")]
        public IHttpActionResult Register(EVOwnerRegisterModel model)
        {
            var result = _evOwnerService.Register(model);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.Message);
            }

            return Ok(result.Message);
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

            var result = _evOwnerService.UpdateProfile(currentUserId, model);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.Message);
            }

            return Ok(result.Message);
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

            var result = _evOwnerService.DeactivateAccount(currentUserId);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.Message);
            }

            return Ok(result.Message);
        }

        [HttpPut]
        [Route("reactivate/{nic}")]
        [RoleAuthorizeMiddleware("1")]
        [OwnAccountMiddleware("nic")]
        public IHttpActionResult ReactivateAccount(string nic)
        {
            var result = _evOwnerService.ReactivateAccount(nic);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.Message);
            }

            return Ok(result.Message);
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

            var result = _evOwnerService.GetProfile(currentUserId);
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.UserProfile);
        }

        [HttpGet]
        [Route("profile/{nic}")]
        [RoleAuthorizeMiddleware("1", "2")]
        [OwnAccountMiddleware("nic")]
        public IHttpActionResult GetProfileByNic(string nic)
        {
            var result = _evOwnerService.GetProfileByNIC(nic);
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.UserProfile);
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

        private IHttpActionResult GetErrorResponse(EVOwnerOperationStatus status, string errorMessage)
        {
            switch (status)
            {
                case EVOwnerOperationStatus.UserNotFound:
                case EVOwnerOperationStatus.EVOwnerNotFound:
                    return BadRequest(errorMessage);
                case EVOwnerOperationStatus.UsernameExists:
                case EVOwnerOperationStatus.EmailExists:
                case EVOwnerOperationStatus.PhoneExists:
                case EVOwnerOperationStatus.NICExists:
                    return BadRequest(errorMessage);
                case EVOwnerOperationStatus.ValidationFailed:
                    return BadRequest(errorMessage);
                case EVOwnerOperationStatus.AccountDeactivated:
                case EVOwnerOperationStatus.AccountAlreadyDeactivated:
                case EVOwnerOperationStatus.AccountAlreadyActive:
                    return BadRequest(errorMessage);
                case EVOwnerOperationStatus.NotAuthorized:
                    return Unauthorized();
                default:
                    return BadRequest(errorMessage);
            }
        }
    }
}
