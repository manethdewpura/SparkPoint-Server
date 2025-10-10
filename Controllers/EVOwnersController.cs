/*
 * EVOwnersController.cs
 * 
 * This controller handles all HTTP requests related to EV Owner operations.
 * It provides endpoints for EV Owner registration, profile management, account activation/deactivation,
 * and profile retrieval. All operations are secured with appropriate authorization attributes.
 */

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

        // Constructor: Initializes the EVOwnerService dependency
        public EVOwnersController()
        {
            _evOwnerService = new EVOwnerService();
        }

        // Registers a new EV Owner with the system
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

        // Updates the profile of the currently authenticated EV Owner
        [HttpPatch]
        [Route("update")]
        [EVOwnerOnly]
        [OwnAccountMiddleware]
        public IHttpActionResult UpdateProfile(EVOwnerUpdateModel model)
        {
            var currentUserId = UserContextHelper.GetCurrentUserId(this);
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            if (model == null)
                return BadRequest("Update data is required.");

            var result = _evOwnerService.UpdateProfile(currentUserId, model);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.Message);
            }

            return Ok(result.Message);
        }

        // Allows admin to update any EV Owner profile by NIC
        [HttpPatch]
        [Route("admin/update/{nic}")]
        [AdminOnly]
        public IHttpActionResult AdminUpdateProfile(string nic, EVOwnerAdminUpdateModel model)
        {
            if (string.IsNullOrEmpty(nic))
                return BadRequest("NIC is required.");

            if (model == null)
                return BadRequest("Update data is required.");

            var result = _evOwnerService.AdminUpdateProfile(nic, model);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.Message);
            }

            return Ok(result.Message);
        }

        // Deactivates the currently authenticated EV Owner account
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

        // Allows admin to deactivate any EV Owner account by NIC
        [HttpPatch]
        [Route("admin/deactivate/{nic}")]
        [AdminOnly]
        [OwnAccountMiddleware("nic")]
        public IHttpActionResult AdminDeactivateAccount(string nic)
        {
            if (string.IsNullOrEmpty(nic))
                return BadRequest("NIC is required.");

            var result = _evOwnerService.AdminDeactivateAccount(nic);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.Message);
            }

            return Ok(result.Message);
        }

        // Allows admin to reactivate a deactivated EV Owner account by NIC
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

        // Retrieves the profile of the currently authenticated EV Owner
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

        // Retrieves EV Owner profile by NIC (admin and station user access)
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

        // Retrieves all EV Owners with optional filtering (admin only)
        [HttpGet]
        [Route("all")]
        [AdminOnly]
        public IHttpActionResult GetAllEVOwners([FromUri] EVOwnerListFilterModel filter = null)
        {
            var result = _evOwnerService.GetAllEVOwners(filter);
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.UserProfile);
        }

        // Maps EV Owner operation status to appropriate HTTP response
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
