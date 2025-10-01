using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Security.Claims;
using SparkPoint_Server.Models;
using SparkPoint_Server.Helpers;
using SparkPoint_Server.Services;
using SparkPoint_Server.Utils;
using SparkPoint_Server.Attributes;
using SparkPoint_Server.Constants;
using MongoDB.Driver;

namespace SparkPoint_Server.Controllers
{
    [RoutePrefix("api/bookings")]
    public class BookingsController : ApiController
    {
        private readonly BookingService _bookingService;

        public BookingsController()
        {
            _bookingService = new BookingService();
        }

        [HttpPost]
        [Route("")]
        [AdminAndEVOwner]
        public IHttpActionResult CreateBooking(BookingCreateModel model)
        {
            var userContext = UserContextHelper.GetUserContext(this);
            if (userContext == null)
                return Unauthorized();

            var result = _bookingService.CreateBooking(model, userContext);
            
            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(new
            {
                Message = result.Message,
                Data = result.Data
            });
        }

        [HttpGet]
        [Route("")]
        [AllRoles]
        public IHttpActionResult GetBookings([FromUri] BookingFilterModel filter = null)
        {
            var userContext = UserContextHelper.GetUserContext(this);
            if (userContext == null)
                return Unauthorized();

            var result = _bookingService.GetBookings(filter, userContext);
            
            if (!result.IsSuccess)
                return BadRequest(result.ErrorMessage);

            return Ok(result.Bookings);
        }

        [HttpGet]
        [Route("{bookingId}")]
        [AllRoles]
        public IHttpActionResult GetBooking(string bookingId)
        {
            var userContext = UserContextHelper.GetUserContext(this);
            if (userContext == null)
                return Unauthorized();

            var result = _bookingService.GetBooking(bookingId, userContext);
            
            if (!result.IsSuccess)
                return BadRequest(result.ErrorMessage);

            return Ok(result.Booking);
        }

        [HttpPatch]
        [Route("{bookingId}")]
        [AdminAndEVOwner]
        public IHttpActionResult UpdateBooking(string bookingId, BookingUpdateModel model)
        {
            var userContext = UserContextHelper.GetUserContext(this);
            if (userContext == null)
                return Unauthorized();

            var result = _bookingService.UpdateBooking(bookingId, model, userContext);
            
            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }

        [HttpPatch]
        [Route("cancel/{bookingId}")]
        [AdminAndEVOwner]
        public IHttpActionResult CancelBooking(string bookingId)
        {
            var userContext = UserContextHelper.GetUserContext(this);
            if (userContext == null)
                return Unauthorized();

            var result = _bookingService.CancelBooking(bookingId, userContext);
            
            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }

        [HttpPatch]
        [Route("status/{bookingId}")]
        [AdminAndStationUser]
        public IHttpActionResult UpdateBookingStatus(string bookingId, BookingStatusUpdateModel model)
        {
            var userContext = UserContextHelper.GetUserContext(this);
            if (userContext == null)
                return Unauthorized();

            var result = _bookingService.UpdateBookingStatus(bookingId, model, userContext);
            
            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }

        [HttpGet]
        [Route("availability/{stationId}/date/{date}")]
        [AllRoles]
        public IHttpActionResult GetStationAvailabilityForDate(string stationId, DateTime date)
        {
            try
            {
                var timeSlots = TimeSlotConstants.GetAvailableTimeSlotsForDate(date);
                var availabilityInfo = new List<object>();

                foreach (var slot in timeSlots)
                {
                    if (!TimeSlotConstants.IsWithinOperatingHours(slot))
                        continue;

                    var availableSlots = _bookingService.GetAvailableSlotsAtTime(stationId, slot);
                    var isAvailable = availableSlots > 0;

                    availabilityInfo.Add(new
                    {
                        StartTime = slot,
                        EndTime = TimeSlotConstants.GetSlotEndTime(slot),
                        DisplayName = TimeSlotConstants.GetSlotDisplayName(slot),
                        AvailableSlots = availableSlots,
                        IsAvailable = isAvailable
                    });
                }

                return Ok(new
                {
                    StationId = stationId,
                    Date = date.Date,
                    AvailabilityInfo = availabilityInfo
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving station availability: {ex.Message}");
            }
        }
    }
}
