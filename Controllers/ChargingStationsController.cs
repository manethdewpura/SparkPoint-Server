using System;
using System.Web.Http;
using SparkPoint_Server.Models;
using SparkPoint_Server.Services;
using SparkPoint_Server.Attributes;
using SparkPoint_Server.Constants;
using SparkPoint_Server.Enums;
using SparkPoint_Server.Utils;

namespace SparkPoint_Server.Controllers
{
    [RoutePrefix("api/stations")]
    public class ChargingStationsController : ApiController
    {
        private readonly ChargingStationService _chargingStationService;

        public ChargingStationsController()
        {
            _chargingStationService = new ChargingStationService();
        }

        [HttpPost]
        [Route("")]
        [AdminOnly]
        public IHttpActionResult CreateStation(StationCreateModel model)
        {
            var result = _chargingStationService.CreateStation(model);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.Message);
            }

            return Ok(new { Message = result.Message, StationId = ((dynamic)result.Data)?.StationId });
        }

        [HttpGet]
        [Route("")]
        [AdminOnly]
        public IHttpActionResult GetStations([FromUri] StationFilterModel filter = null)
        {
            var result = _chargingStationService.GetStations(filter);
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.Stations);
        }

        [HttpGet]
        [Route("{stationId}")]
        [AdminOnly]
        public IHttpActionResult GetStation(string stationId)
        {
            var result = _chargingStationService.GetStation(stationId);
            
            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }

            var response = ChargingStationUtils.CreateDetailedStationResponse(result.Station, null);
            // Replace null with actual station users from result
            var responseWithUsers = new
            {
                Station = result.Station,
                StationUsers = result.StationUsers
            };

            return Ok(responseWithUsers);
        }

        [HttpPut]
        [Route("{stationId}")]
        [AdminOnly]
        public IHttpActionResult UpdateStation(string stationId, StationUpdateModel model)
        {
            var result = _chargingStationService.UpdateStation(stationId, model);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.Message);
            }

            return Ok(result.Message);
        }

        [HttpPut]
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

        [HttpPut]
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

        [HttpGet]
        [Route("types")]
        public IHttpActionResult GetValidStationTypes()
        {
            var types = ChargingStationUtils.GetValidStationTypes();
            return Ok(types);
        }

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
