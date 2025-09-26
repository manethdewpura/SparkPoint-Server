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
        private readonly IMongoCollection<Booking> _bookingsCollection;
        private readonly IMongoCollection<ChargingStation> _stationsCollection;
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<EVOwner> _evOwnersCollection;
        private readonly BookingService _bookingService;

        public BookingsController()
        {
            var dbContext = new MongoDbContext();
            _bookingsCollection = dbContext.GetCollection<Booking>("Bookings");
            _stationsCollection = dbContext.GetCollection<ChargingStation>("ChargingStations");
            _usersCollection = dbContext.GetCollection<User>("Users");
            _evOwnersCollection = dbContext.GetCollection<EVOwner>("EVOwners");
            _bookingService = new BookingService();
        }

        // Create new reservation (within 7 days from booking date)
        [HttpPost]
        [Route("")]
        [RoleAuthorizeMiddleware("1", "3")] // Admin and EV Owner can create bookings
        public IHttpActionResult CreateBooking(BookingCreateModel model)
        {
            if (model == null)
                return BadRequest("Booking data is required.");

            if (string.IsNullOrEmpty(model.StationId))
                return BadRequest("Station ID is required.");

            if (model.ReservationTime == DateTime.MinValue)
                return BadRequest("Reservation time is required.");

            var identity = User.Identity as ClaimsIdentity;
            var userIdClaim = identity?.FindFirst(ClaimTypes.NameIdentifier);
            var userRoleClaim = identity?.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userRoleClaim == null)
                return Unauthorized();

            var currentUserId = userIdClaim.Value;
            var currentUserRole = int.Parse(userRoleClaim.Value);
            var ownerNIC = model.OwnerNIC;

            if (currentUserRole == 3)
            {
                var currentEvOwner = _evOwnersCollection.Find(ev => ev.UserId == currentUserId).FirstOrDefault();
                if (currentEvOwner == null)
                    return BadRequest("EV Owner profile not found.");
                
                ownerNIC = currentEvOwner.NIC;
            }
            else if (currentUserRole == 1 && string.IsNullOrEmpty(ownerNIC))
            {
                return BadRequest("Owner NIC is required for admin bookings.");
            }

            // Validate owner NIC exists
            var evOwner = _evOwnersCollection.Find(ev => ev.NIC == ownerNIC).FirstOrDefault();
            if (evOwner == null)
                return BadRequest("EV Owner not found.");

            // Validate reservation time is within 7 days
            var daysFromNow = (model.ReservationTime.Date - DateTime.Now.Date).Days;
            if (daysFromNow < 0)
                return BadRequest("Cannot make reservations for past dates.");
            
            if (daysFromNow > 7)
                return BadRequest("Reservations can only be made up to 7 days in advance.");

            // Validate station exists and is active
            var station = _stationsCollection.Find(s => s.Id == model.StationId && s.IsActive).FirstOrDefault();
            if (station == null)
                return BadRequest("Station not found or inactive.");

            // Check if slot is available at the requested time using the service
            if (!_bookingService.CheckSlotAvailability(model.StationId, model.ReservationTime))
                return BadRequest("No available slots at the requested time.");

            var booking = new Booking
            {
                OwnerNIC = ownerNIC,
                StationId = model.StationId,
                ReservationTime = model.ReservationTime,
                Status = BookingStatusConstants.Pending, // Use constant
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _bookingsCollection.InsertOne(booking);

            var availableSlots = _bookingService.GetAvailableSlotsAtTime(model.StationId, model.ReservationTime);

            return Ok(new 
            { 
                Message = "Booking created successfully.", 
                BookingId = booking.Id,
                AvailableSlotsAtTime = availableSlots
            });
        }

        // Get bookings based on user role
        [HttpGet]
        [Route("")]
        [RoleAuthorizeMiddleware("1", "2", "3")] // All authenticated users
        public IHttpActionResult GetBookings([FromUri] BookingFilterModel filter = null)
        {
            var identity = User.Identity as ClaimsIdentity;
            var userIdClaim = identity?.FindFirst(ClaimTypes.NameIdentifier);
            var userRoleClaim = identity?.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userRoleClaim == null)
                return Unauthorized();

            var currentUserId = userIdClaim.Value;
            var currentUserRole = int.Parse(userRoleClaim.Value);

            var filterBuilder = Builders<Booking>.Filter.Empty;

            // Role-based filtering
            if (currentUserRole == 3) // EV Owner - only their bookings
            {
                var currentEvOwner = _evOwnersCollection.Find(ev => ev.UserId == currentUserId).FirstOrDefault();
                if (currentEvOwner == null)
                    return BadRequest("EV Owner profile not found.");
                
                filterBuilder = Builders<Booking>.Filter.Eq(b => b.OwnerNIC, currentEvOwner.NIC);
            }
            else if (currentUserRole == 2) // Station User - only bookings for their station
            {
                var currentUser = _usersCollection.Find(u => u.Id == currentUserId).FirstOrDefault();
                if (currentUser == null || string.IsNullOrEmpty(currentUser.ChargingStationId))
                    return BadRequest("Station user not properly configured.");
                
                filterBuilder = Builders<Booking>.Filter.Eq(b => b.StationId, currentUser.ChargingStationId);
            }
            // Admin (role 1) can see all bookings - no additional filter needed

            // Apply additional filters if provided
            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Status))
                {
                    var statusFilter = Builders<Booking>.Filter.Eq(b => b.Status, filter.Status);
                    filterBuilder = Builders<Booking>.Filter.And(filterBuilder, statusFilter);
                }

                if (!string.IsNullOrEmpty(filter.StationId))
                {
                    var stationFilter = Builders<Booking>.Filter.Eq(b => b.StationId, filter.StationId);
                    filterBuilder = Builders<Booking>.Filter.And(filterBuilder, stationFilter);
                }

                if (filter.FromDate.HasValue)
                {
                    var fromDateFilter = Builders<Booking>.Filter.Gte(b => b.ReservationTime, filter.FromDate.Value);
                    filterBuilder = Builders<Booking>.Filter.And(filterBuilder, fromDateFilter);
                }

                if (filter.ToDate.HasValue)
                {
                    var toDateFilter = Builders<Booking>.Filter.Lte(b => b.ReservationTime, filter.ToDate.Value);
                    filterBuilder = Builders<Booking>.Filter.And(filterBuilder, toDateFilter);
                }
            }

            var bookings = _bookingsCollection.Find(filterBuilder)
                .SortByDescending(b => b.CreatedAt)
                .ToList();

            return Ok(bookings);
        }

        // Get specific booking
        [HttpGet]
        [Route("{bookingId}")]
        [RoleAuthorizeMiddleware("1", "2", "3")]
        public IHttpActionResult GetBooking(string bookingId)
        {
            var booking = _bookingsCollection.Find(b => b.Id == bookingId).FirstOrDefault();
            if (booking == null)
                return BadRequest("Booking not found.");

            var identity = User.Identity as ClaimsIdentity;
            var userIdClaim = identity?.FindFirst(ClaimTypes.NameIdentifier);
            var userRoleClaim = identity?.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userRoleClaim == null)
                return Unauthorized();

            var currentUserId = userIdClaim.Value;
            var currentUserRole = int.Parse(userRoleClaim.Value);

            // Check access permissions
            if (currentUserRole == 3) // EV Owner - only their bookings
            {
                var currentEvOwner = _evOwnersCollection.Find(ev => ev.UserId == currentUserId).FirstOrDefault();
                if (currentEvOwner == null || booking.OwnerNIC != currentEvOwner.NIC)
                    return BadRequest("Access denied.");
            }
            else if (currentUserRole == 2) // Station User - only bookings for their station
            {
                var currentUser = _usersCollection.Find(u => u.Id == currentUserId).FirstOrDefault();
                if (currentUser == null || booking.StationId != currentUser.ChargingStationId)
                    return BadRequest("Access denied.");
            }

            return Ok(booking);
        }

        [HttpPut]
        [Route("{bookingId}")]
        [RoleAuthorizeMiddleware("1", "3")] // Admin and EV Owner can update bookings
        public IHttpActionResult UpdateBooking(string bookingId, BookingUpdateModel model)
        {
            if (model == null)
                return BadRequest("Update data is required.");

            var booking = _bookingsCollection.Find(b => b.Id == bookingId).FirstOrDefault();
            if (booking == null)
                return BadRequest("Booking not found.");

            var identity = User.Identity as ClaimsIdentity;
            var userIdClaim = identity?.FindFirst(ClaimTypes.NameIdentifier);
            var userRoleClaim = identity?.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userRoleClaim == null)
                return Unauthorized();

            var currentUserId = userIdClaim.Value;
            var currentUserRole = int.Parse(userRoleClaim.Value);

            // Check access permissions
            if (currentUserRole == 3) // EV Owner - only their bookings
            {
                var currentEvOwner = _evOwnersCollection.Find(ev => ev.UserId == currentUserId).FirstOrDefault();
                if (currentEvOwner == null || booking.OwnerNIC != currentEvOwner.NIC)
                    return BadRequest("Access denied.");
            }

            // Check if booking can be modified (at least 12 hours before reservation)
            var hoursUntilReservation = (booking.ReservationTime - DateTime.Now).TotalHours;
            if (hoursUntilReservation < 12)
                return BadRequest("Cannot modify booking less than 12 hours before reservation time.");

            // Check if booking is in a modifiable state
            if (booking.Status == BookingStatusConstants.Cancelled || booking.Status == BookingStatusConstants.Completed)
                return BadRequest("Cannot modify cancelled or completed bookings.");

            var updateBuilder = Builders<Booking>.Update.Set(b => b.UpdatedAt, DateTime.UtcNow);

            if (model.ReservationTime.HasValue)
            {
                // Validate new reservation time is within 7 days
                var daysFromNow = (model.ReservationTime.Value.Date - DateTime.Now.Date).Days;
                if (daysFromNow < 0)
                    return BadRequest("Cannot reschedule to past dates.");
                
                if (daysFromNow > 7)
                    return BadRequest("Reservations can only be made up to 7 days in advance.");

                // Check availability at new time using the service
                if (!_bookingService.CheckSlotAvailability(booking.StationId, model.ReservationTime.Value, bookingId))
                    return BadRequest("No available slots at the requested time.");

                updateBuilder = updateBuilder.Set(b => b.ReservationTime, model.ReservationTime.Value);
            }

            if (!string.IsNullOrEmpty(model.StationId) && model.StationId != booking.StationId)
            {
                // Validate new station exists and is active
                var newStation = _stationsCollection.Find(s => s.Id == model.StationId && s.IsActive).FirstOrDefault();
                if (newStation == null)
                    return BadRequest("New station not found or inactive.");

                var reservationTime = model.ReservationTime ?? booking.ReservationTime;
                
                // Check availability at new station using the service
                if (!_bookingService.CheckSlotAvailability(model.StationId, reservationTime))
                    return BadRequest("No available slots at the new station for the requested time.");

                updateBuilder = updateBuilder.Set(b => b.StationId, model.StationId);
            }

            _bookingsCollection.UpdateOne(b => b.Id == bookingId, updateBuilder);

            return Ok("Booking updated successfully.");
        }

        // Cancel reservation (at least 12 hours before reservation)
        [HttpPut]
        [Route("cancel/{bookingId}")]
        [RoleAuthorizeMiddleware("1", "3")] // Admin and EV Owner can cancel bookings
        public IHttpActionResult CancelBooking(string bookingId)
        {
            var booking = _bookingsCollection.Find(b => b.Id == bookingId).FirstOrDefault();
            if (booking == null)
                return BadRequest("Booking not found.");

            var identity = User.Identity as ClaimsIdentity;
            var userIdClaim = identity?.FindFirst(ClaimTypes.NameIdentifier);
            var userRoleClaim = identity?.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userRoleClaim == null)
                return Unauthorized();

            var currentUserId = userIdClaim.Value;
            var currentUserRole = int.Parse(userRoleClaim.Value);

            // Check access permissions
            if (currentUserRole == 3) // EV Owner - only their bookings
            {
                var currentEvOwner = _evOwnersCollection.Find(ev => ev.UserId == currentUserId).FirstOrDefault();
                if (currentEvOwner == null || booking.OwnerNIC != currentEvOwner.NIC)
                    return BadRequest("Access denied.");
            }

            // Check if booking is already cancelled or completed
            if (booking.Status == BookingStatusConstants.Cancelled)
                return BadRequest("Booking is already cancelled.");
            
            if (booking.Status == BookingStatusConstants.Completed)
                return BadRequest("Cannot cancel completed booking.");

            // Check if booking can be cancelled (at least 12 hours before reservation)
            var hoursUntilReservation = (booking.ReservationTime - DateTime.Now).TotalHours;
            if (hoursUntilReservation < 12)
                return BadRequest("Cannot cancel booking less than 12 hours before reservation time.");

            // Use the service to update status and manage slots
            var result = _bookingService.UpdateBookingStatus(bookingId, BookingStatusConstants.Cancelled, booking.Status);
            
            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }

        // Update booking status (for station users and admins) - WITH SLOT MANAGEMENT
        [HttpPut]
        [Route("status/{bookingId}")]
        [RoleAuthorizeMiddleware("1", "2")] // Admin and Station User can update status
        public IHttpActionResult UpdateBookingStatus(string bookingId, BookingStatusUpdateModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Status))
                return BadRequest("Status is required.");

            var booking = _bookingsCollection.Find(b => b.Id == bookingId).FirstOrDefault();
            if (booking == null)
                return BadRequest("Booking not found.");

            var identity = User.Identity as ClaimsIdentity;
            var userIdClaim = identity?.FindFirst(ClaimTypes.NameIdentifier);
            var userRoleClaim = identity?.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userRoleClaim == null)
                return Unauthorized();

            var currentUserId = userIdClaim.Value;
            var currentUserRole = int.Parse(userRoleClaim.Value);

            // Check access permissions for station users
            if (currentUserRole == 2) // Station User - only bookings for their station
            {
                var currentUser = _usersCollection.Find(u => u.Id == currentUserId).FirstOrDefault();
                if (currentUser == null || booking.StationId != currentUser.ChargingStationId)
                    return BadRequest("Access denied.");
            }

            // Validate status transitions using constants
            if (!BookingStatusConstants.IsValidStatus(model.Status))
                return BadRequest("Invalid status value.");

            var hoursUntilReservation = (booking.ReservationTime - DateTime.Now).TotalHours;
            
            // If less than 12 hours before reservation, only allow certain status updates
            if (hoursUntilReservation < 12)
            {
                if (!BookingStatusConstants.IsNearTimeAllowedStatus(model.Status))
                    return BadRequest("Only 'In Progress', 'Completed', and 'No Show' status updates are allowed within 12 hours of reservation time.");
            }

            // Prevent certain status changes
            if (booking.Status == BookingStatusConstants.Completed && model.Status != BookingStatusConstants.Completed)
                return BadRequest("Cannot change status of completed booking.");
            
            if (booking.Status == BookingStatusConstants.Cancelled && model.Status != BookingStatusConstants.Cancelled)
                return BadRequest("Cannot change status of cancelled booking.");

            // Use the service to update status and manage slots
            var result = _bookingService.UpdateBookingStatus(bookingId, model.Status, booking.Status);
            
            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }

        // GET endpoint to check available slots at a specific time for a station
        [HttpGet]
        [Route("availability/{stationId}")]
        [RoleAuthorizeMiddleware("1", "2", "3")] // All authenticated users can check availability
        public IHttpActionResult CheckSlotAvailability(string stationId, [FromUri] DateTime reservationTime)
        {
            var station = _stationsCollection.Find(s => s.Id == stationId).FirstOrDefault();
            if (station == null)
                return BadRequest("Station not found.");

            var availableSlots = _bookingService.GetAvailableSlotsAtTime(stationId, reservationTime);
            var isAvailable = availableSlots > 0;

            return Ok(new 
            { 
                StationId = stationId,
                ReservationTime = reservationTime,
                TotalSlots = station.TotalSlots,
                AvailableSlots = availableSlots,
                IsAvailable = isAvailable
            });
        }
    }
}
