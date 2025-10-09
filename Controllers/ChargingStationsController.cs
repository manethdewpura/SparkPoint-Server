/*
 * ChargingStationsController.cs
 * 
 * This controller handles all HTTP requests related to charging station operations.
 * It provides endpoints for creating, retrieving, updating, and managing charging stations.
 * All operations are secured with appropriate role-based authorization (admin-only for creation,
 * all roles for viewing).
 */

using System;
using System.Web.Http;
using SparkPoint_Server.Models;
using SparkPoint_Server.Services;
using SparkPoint_Server.Attributes;
using SparkPoint_Server.Constants;
using SparkPoint_Server.Enums;
using SparkPoint_Server.Utils;
using SparkPoint_Server.Helpers;

namespace SparkPoint_Server.Controllers
{
    [RoutePrefix("api/stations")]
    public class ChargingStationsController : ApiController
    {
        private readonly ChargingStationService _chargingStationService;

        // Constructor: Initializes the ChargingStationService dependency
        public ChargingStationsController()
        {
            _chargingStationService = new ChargingStationService();
        }

        // Creates a new charging station (admin only)
        [HttpPost]
        [Route("")]
        [AdminOnly]
        public IHttpActionResult CreateStation(StationCreateModel model)
        {
            // Validate the model first
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = _chargingStationService.CreateStation(model);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.Message);
            }

            return Ok(new { Message = result.Message, StationId = ((dynamic)result.Data)?.StationId });
        }

        // Retrieves charging stations with optional filtering
        [HttpGet]
        [Route("")]
        [AllRoles]
        public IHttpActionResult GetStations([FromUri] StationFilterModel filter = null)
        {
            var result = _chargingStationService.GetStations(filter);
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.Stations);
        }

        // Retrieves a specific charging station by ID
        [HttpGet]
        [Route("{stationId}")]
        [AllRoles]
        public IHttpActionResult GetStation(string stationId)
        {
            var result = _chargingStationService.GetStation(stationId);
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }

            var response = ChargingStationUtils.CreateDetailedStationResponse(result.Station, null);
            var responseWithUsers = new
            {
                Station = result.Station,
                StationUsers = result.StationUsers
            };

            return Ok(responseWithUsers);
        }

        // Updates an existing charging station (admin only)
        [HttpPatch]
        [Route("{stationId}")]
        [AdminOnly]
        public IHttpActionResult UpdateStation(string stationId, StationUpdateModel model)
        {
            // Validate the model first
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = _chargingStationService.UpdateStation(stationId, model);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.Message);
            }

            return Ok(result.Message);
        }

        // Activates a charging station (admin only)
        [HttpPatch]
        [Route("activate/{stationId}")]
        [AdminOnly]
        public IHttpActionResult ActivateStation(string stationId)
        {
            var result = _chargingStationService.ActivateStation(stationId);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.Message);
            }

            return Ok(result.Message);
        }

        // Deactivates a charging station (admin only)
        [HttpPatch]
        [Route("deactivate/{stationId}")]
        [AdminOnly]
        public IHttpActionResult DeactivateStation(string stationId)
        {
            var result = _chargingStationService.DeactivateStation(stationId);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.Message);
            }

            return Ok(result.Message);
        }

        // Updates total slots for a charging station (station users for their own station)
        [HttpPatch]
        [Route("{stationId}/slots")]
        [StationUserOnly]
        public IHttpActionResult UpdateStationSlots(string stationId, StationSlotsUpdateModel model)
        {
            // Validate the model first
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get current user ID for authorization
            var currentUserId = UserContextHelper.GetCurrentUserId(this);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            var result = _chargingStationService.UpdateStationSlots(stationId, model, currentUserId);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.Message);
            }

            return Ok(result.Message);
        }

        // Gets station statistics (admin only)
        [HttpGet]
        [Route("{stationId}/statistics")]
        [AdminOnly]
        public IHttpActionResult GetStationStatistics(string stationId)
        {
            var statistics = _chargingStationService.GetStationStatistics(stationId);
            
            if (statistics == null)
            {
                return BadRequest(ChargingStationConstants.StationNotFound);
            }

            return Ok(statistics);
        }

        // Gets valid station types
        [HttpGet]
        [Route("types")]
        public IHttpActionResult GetValidStationTypes()
        {
            var types = ChargingStationUtils.GetValidStationTypes();
            return Ok(types);
        }

        // Converts station operation status to appropriate HTTP response
        private IHttpActionResult GetErrorResponse(StationOperationStatus status, string errorMessage)
        {
            switch (status)
            {
                case StationOperationStatus.StationNotFound:
                    return NotFound();
                case StationOperationStatus.ValidationFailed:
                    return BadRequest(errorMessage);
                case StationOperationStatus.AlreadyInState:
                    return BadRequest(errorMessage);
                case StationOperationStatus.HasActiveBookings:
                    return Conflict();
                default:
                    return BadRequest(errorMessage);
            }
        }
    }
}
