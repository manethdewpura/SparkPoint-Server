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
        [RoleAuthorizeMiddleware("1", "3")]
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
        [RoleAuthorizeMiddleware("1", "2", "3")]
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
        [RoleAuthorizeMiddleware("1", "2", "3")]
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

        [HttpPut]
        [Route("{bookingId}")]
        [RoleAuthorizeMiddleware("1", "3")]
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

        [HttpPut]
        [Route("cancel/{bookingId}")]
        [RoleAuthorizeMiddleware("1", "3")]
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

        [HttpPut]
        [Route("status/{bookingId}")]
        [RoleAuthorizeMiddleware("1", "2")]
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
        [Route("availability/{stationId}")]
        [RoleAuthorizeMiddleware("1", "2", "3")]
        public IHttpActionResult CheckSlotAvailability(string stationId, [FromUri] DateTime reservationTime)
        {
            var result = _bookingService.CheckSlotAvailabilityForResult(stationId, reservationTime);
            
            if (!result.IsSuccess)
                return BadRequest(result.ErrorMessage);

            return Ok(result.AvailabilityData);
        }
    }
}
