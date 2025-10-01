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

        [HttpPatch]
        [Route("update")]
        [EVOwnerOnly]
        [OwnAccountMiddleware]
        public IHttpActionResult UpdateProfile(EVOwnerUpdateModel model)
        {
            var currentUserId = UserContextHelper.GetCurrentUserId(this);
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            var result = _evOwnerService.UpdateProfile(currentUserId, model);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.Message);
            }

            return Ok(result.Message);
        }

        [HttpPatch]
        [Route("update/{nic}")]
        [AdminAndStationUser]
        [OwnAccountMiddleware("nic")]
        public IHttpActionResult UpdateProfileByNic(string nic, EVOwnerUpdateModel model)
        {
            if (string.IsNullOrEmpty(nic))
                return BadRequest("NIC is required.");

            if (model == null)
                return BadRequest("Update data is required.");

            var result = _evOwnerService.UpdateProfileByNIC(nic, model);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.Message);
            }

            return Ok(result.Message);
        }

        [HttpPatch]
        [Route("deactivate")]
        [EVOwnerOnly]
        [OwnAccountMiddleware]
        public IHttpActionResult DeactivateAccount()
        {
            var currentUserId = UserContextHelper.GetCurrentUserId(this);
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            var result = _evOwnerService.DeactivateAccount(currentUserId);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.Message);
            }

            return Ok(result.Message);
        }

        [HttpPatch]
        [Route("reactivate/{nic}")]
        [AdminOnly]
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
        [EVOwnerOnly]
        [OwnAccountMiddleware]
        public IHttpActionResult GetProfile()
        {
            var currentUserId = UserContextHelper.GetCurrentUserId(this);
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
        [AdminAndStationUser]
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
