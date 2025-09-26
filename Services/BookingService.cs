using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using SparkPoint_Server.Models;
using SparkPoint_Server.Helpers;
using SparkPoint_Server.Constants;

namespace SparkPoint_Server.Services
{ 
    public class BookingService
    {
        private readonly IMongoCollection<Booking> _bookingsCollection;
        private readonly IMongoCollection<ChargingStation> _stationsCollection;
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<EVOwner> _evOwnersCollection;
        private readonly BookingAuthorizationHelper _authorizationHelper;

        public BookingService()
        {
            var dbContext = new MongoDbContext();
            _bookingsCollection = dbContext.GetCollection<Booking>("Bookings");
            _stationsCollection = dbContext.GetCollection<ChargingStation>("ChargingStations");
            _usersCollection = dbContext.GetCollection<User>("Users");
            _evOwnersCollection = dbContext.GetCollection<EVOwner>("EVOwners");
            _authorizationHelper = new BookingAuthorizationHelper();
        }

        public BookingOperationResult CreateBooking(BookingCreateModel model, UserContext userContext)
        {
            // Validate request
            var validationResult = BookingValidationHelper.ValidateCreateBooking(model);
            if (!validationResult.IsValid)
                return BookingOperationResult.Failed(validationResult.ErrorMessage);

            // Check authorization
            var authResult = _authorizationHelper.CanCreateBooking(userContext);
            if (!authResult.IsAuthorized)
                return BookingOperationResult.Failed(authResult.ErrorMessage);

            // Resolve owner NIC
            var ownerResult = _authorizationHelper.ResolveOwnerNIC(userContext, model.OwnerNIC);
            if (!ownerResult.IsSuccess)
                return BookingOperationResult.Failed(ownerResult.ErrorMessage);

            // Validate owner exists
            var evOwner = _evOwnersCollection.Find(ev => ev.NIC == ownerResult.OwnerNIC).FirstOrDefault();
            if (evOwner == null)
                return BookingOperationResult.Failed("EV Owner not found.");

            // Validate station
            var stationResult = ValidateStation(model.StationId);
            if (!stationResult.IsSuccess)
                return BookingOperationResult.Failed(stationResult.ErrorMessage);

            // Check slot availability
            if (!CheckSlotAvailability(model.StationId, model.ReservationTime))
                return BookingOperationResult.Failed(ValidationMessages.NoAvailableSlots);

            // Create booking
            var booking = new Booking
            {
                OwnerNIC = ownerResult.OwnerNIC,
                StationId = model.StationId,
                ReservationTime = model.ReservationTime,
                Status = BookingStatusConstants.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _bookingsCollection.InsertOne(booking);

            var availableSlots = GetAvailableSlotsAtTime(model.StationId, model.ReservationTime);

            return BookingOperationResult.Success(
                $"Booking created successfully.",
                new { BookingId = booking.Id, AvailableSlotsAtTime = availableSlots }
            );
        }

        public BookingsQueryResult GetBookings(BookingFilterModel filter, UserContext userContext)
        {
            // Get role-based filter
            var filterResult = _authorizationHelper.GetBookingsFilter(userContext);
            if (!filterResult.IsSuccess)
                return BookingsQueryResult.Failed(filterResult.ErrorMessage);

            var filterBuilder = filterResult.Filter;

            // Apply additional filters
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

            return BookingsQueryResult.Success(bookings);
        }

        public BookingRetrievalResult GetBooking(string bookingId, UserContext userContext)
        {
            var booking = _bookingsCollection.Find(b => b.Id == bookingId).FirstOrDefault();
            if (booking == null)
                return BookingRetrievalResult.Failed(ValidationMessages.BookingNotFound);

            var authResult = _authorizationHelper.CanAccessBooking(userContext, booking);
            if (!authResult.IsAuthorized)
                return BookingRetrievalResult.Failed(authResult.ErrorMessage);

            return BookingRetrievalResult.Success(booking);
        }

        public BookingOperationResult UpdateBooking(string bookingId, BookingUpdateModel model, UserContext userContext)
        {
            var booking = _bookingsCollection.Find(b => b.Id == bookingId).FirstOrDefault();
            if (booking == null)
                return BookingOperationResult.Failed(ValidationMessages.BookingNotFound);

            // Check authorization
            var authResult = _authorizationHelper.CanUpdateBooking(userContext, booking);
            if (!authResult.IsAuthorized)
                return BookingOperationResult.Failed(authResult.ErrorMessage);

            // Validate update request
            var validationResult = BookingValidationHelper.ValidateUpdateBooking(model, booking);
            if (!validationResult.IsValid)
                return BookingOperationResult.Failed(validationResult.ErrorMessage);

            var updateBuilder = Builders<Booking>.Update.Set(b => b.UpdatedAt, DateTime.UtcNow);

            // Handle reservation time update
            if (model.ReservationTime.HasValue)
            {
                if (!CheckSlotAvailability(booking.StationId, model.ReservationTime.Value, bookingId))
                    return BookingOperationResult.Failed(ValidationMessages.NoAvailableSlots);

                updateBuilder = updateBuilder.Set(b => b.ReservationTime, model.ReservationTime.Value);
            }

            // Handle station change
            if (!string.IsNullOrEmpty(model.StationId) && model.StationId != booking.StationId)
            {
                var stationResult = ValidateStation(model.StationId);
                if (!stationResult.IsSuccess)
                    return BookingOperationResult.Failed(stationResult.ErrorMessage);

                var reservationTime = model.ReservationTime ?? booking.ReservationTime;
                if (!CheckSlotAvailability(model.StationId, reservationTime))
                    return BookingOperationResult.Failed("No available slots at the new station for the requested time.");

                updateBuilder = updateBuilder.Set(b => b.StationId, model.StationId);
            }

            _bookingsCollection.UpdateOne(b => b.Id == bookingId, updateBuilder);

            return BookingOperationResult.Success("Booking updated successfully.");
        }

        public BookingOperationResult CancelBooking(string bookingId, UserContext userContext)
        {
            var booking = _bookingsCollection.Find(b => b.Id == bookingId).FirstOrDefault();
            if (booking == null)
                return BookingOperationResult.Failed(ValidationMessages.BookingNotFound);

            // Check authorization
            var authResult = _authorizationHelper.CanCancelBooking(userContext, booking);
            if (!authResult.IsAuthorized)
                return BookingOperationResult.Failed(authResult.ErrorMessage);

            // Validate cancellation
            var validationResult = BookingValidationHelper.ValidateCancelBooking(booking);
            if (!validationResult.IsValid)
                return BookingOperationResult.Failed(validationResult.ErrorMessage);

            // Update status with slot management
            var result = UpdateBookingStatusWithSlotManagement(bookingId, BookingStatusConstants.Cancelled, booking.Status);
            
            if (!result.IsSuccess)
                return BookingOperationResult.Failed(result.Message);

            return BookingOperationResult.Success(result.Message);
        }

        public BookingOperationResult UpdateBookingStatus(string bookingId, BookingStatusUpdateModel model, UserContext userContext)
        {
            var booking = _bookingsCollection.Find(b => b.Id == bookingId).FirstOrDefault();
            if (booking == null)
                return BookingOperationResult.Failed(ValidationMessages.BookingNotFound);

            // Check authorization
            var authResult = _authorizationHelper.CanUpdateBookingStatus(userContext, booking);
            if (!authResult.IsAuthorized)
                return BookingOperationResult.Failed(authResult.ErrorMessage);

            // Validate status update
            var validationResult = BookingValidationHelper.ValidateStatusUpdate(model, booking);
            if (!validationResult.IsValid)
                return BookingOperationResult.Failed(validationResult.ErrorMessage);

            // Update status with slot management
            var result = UpdateBookingStatusWithSlotManagement(bookingId, model.Status, booking.Status);
            
            if (!result.IsSuccess)
                return BookingOperationResult.Failed(result.Message);

            return BookingOperationResult.Success(result.Message);
        }

        public SlotAvailabilityResult CheckSlotAvailabilityForResult(string stationId, DateTime reservationTime)
        {
            var station = _stationsCollection.Find(s => s.Id == stationId).FirstOrDefault();
            if (station == null)
                return SlotAvailabilityResult.Failed(ValidationMessages.StationNotFound);

            var availableSlots = GetAvailableSlotsAtTime(stationId, reservationTime);
            var isAvailable = availableSlots > 0;

            return SlotAvailabilityResult.Success(new
            {
                StationId = stationId,
                ReservationTime = reservationTime,
                TotalSlots = station.TotalSlots,
                AvailableSlots = availableSlots,
                IsAvailable = isAvailable
            });
        }

        public BookingStatusUpdateResult UpdateBookingStatusWithSlotManagement(string bookingId, string newStatus, string oldStatus)
        {
            var booking = _bookingsCollection.Find(b => b.Id == bookingId).FirstOrDefault();
            if (booking == null)
                return BookingStatusUpdateResult.Failed(ValidationMessages.BookingNotFound);

            var station = _stationsCollection.Find(s => s.Id == booking.StationId).FirstOrDefault();
            if (station == null)
                return BookingStatusUpdateResult.Failed(ValidationMessages.StationNotFound);

            // Calculate slot changes
            var slotChange = CalculateSlotChange(oldStatus, newStatus);
            
            // Update available slots if there's a change
            if (slotChange != 0)
            {
                var newAvailableSlots = station.AvailableSlots + slotChange;
                
                // Ensure available slots don't go below 0 or above total slots
                newAvailableSlots = Math.Max(0, Math.Min(station.TotalSlots, newAvailableSlots));

                var stationUpdate = Builders<ChargingStation>.Update
                    .Set(s => s.AvailableSlots, newAvailableSlots)
                    .Set(s => s.UpdatedAt, DateTime.UtcNow);

                _stationsCollection.UpdateOne(s => s.Id == booking.StationId, stationUpdate);
            }

            // Update booking status
            var bookingUpdate = Builders<Booking>.Update
                .Set(b => b.Status, newStatus)
                .Set(b => b.UpdatedAt, DateTime.UtcNow);

            _bookingsCollection.UpdateOne(b => b.Id == bookingId, bookingUpdate);

            var slotMessage = slotChange == 0 ? "No slot change required." 
                : slotChange > 0 ? $"Freed {slotChange} slot(s)." 
                : $"Reserved {Math.Abs(slotChange)} slot(s).";

            return BookingStatusUpdateResult.Success($"Booking status updated to {newStatus}. {slotMessage}");
        }

        private int CalculateSlotChange(string oldStatus, string newStatus)
        {
            var oldReservesSlot = BookingStatusConstants.IsSlotReservingStatus(oldStatus);
            var newReservesSlot = BookingStatusConstants.IsSlotReservingStatus(newStatus);
            var oldFreesSlot = BookingStatusConstants.IsSlotFreeingStatus(oldStatus);
            var newFreesSlot = BookingStatusConstants.IsSlotFreeingStatus(newStatus);

            // If moving from a slot-reserving status to a slot-freeing status
            if (oldReservesSlot && newFreesSlot)
                return 1; // Free one slot

            // If moving from a non-reserving status to a slot-reserving status
            if (!oldReservesSlot && !oldFreesSlot && newReservesSlot)
                return -1; // Reserve one slot

            // If moving from a slot-freeing status to a slot-reserving status (rare case)
            if (oldFreesSlot && newReservesSlot)
                return -1; // Reserve one slot

            // If moving from "Pending" to "Confirmed" or "In Progress"
            if (oldStatus == BookingStatusConstants.Pending && BookingStatusConstants.IsSlotReservingStatus(newStatus))
                return -1; // Reserve one slot

            return 0; // No change in slot allocation
        }

        public bool CheckSlotAvailability(string stationId, DateTime reservationTime, string excludeBookingId = null)
        {
            var station = _stationsCollection.Find(s => s.Id == stationId).FirstOrDefault();
            if (station == null)
                return false;

            var filterBuilder = Builders<Booking>.Filter.And(
                Builders<Booking>.Filter.Eq(b => b.StationId, stationId),
                Builders<Booking>.Filter.Eq(b => b.ReservationTime, reservationTime),
                Builders<Booking>.Filter.Not(
                    Builders<Booking>.Filter.In(b => b.Status, BookingStatusConstants.SlotFreeingStatuses)
                )
            );

            // Exclude specific booking if provided (for updates)
            if (!string.IsNullOrEmpty(excludeBookingId))
            {
                filterBuilder = Builders<Booking>.Filter.And(
                    filterBuilder,
                    Builders<Booking>.Filter.Ne(b => b.Id, excludeBookingId)
                );
            }

            var conflictingBookingsCount = _bookingsCollection.CountDocuments(filterBuilder);

            return conflictingBookingsCount < station.TotalSlots;
        }

        public int GetAvailableSlotsAtTime(string stationId, DateTime reservationTime)
        {
            var station = _stationsCollection.Find(s => s.Id == stationId).FirstOrDefault();
            if (station == null)
                return 0;

            var bookedSlotsAtTime = _bookingsCollection.CountDocuments(b =>
                b.StationId == stationId &&
                b.ReservationTime == reservationTime &&
                !BookingStatusConstants.IsSlotFreeingStatus(b.Status)
            );

            return station.TotalSlots - (int)bookedSlotsAtTime;
        }

        public BookingStatusUpdateResult RecalculateStationAvailableSlots(string stationId)
        {
            var station = _stationsCollection.Find(s => s.Id == stationId).FirstOrDefault();
            if (station == null)
                return BookingStatusUpdateResult.Failed(ValidationMessages.StationNotFound);

            // Get all active bookings (confirmed or in progress) for this station
            var activeBookingsCount = _bookingsCollection.CountDocuments(b =>
                b.StationId == stationId &&
                BookingStatusConstants.IsSlotReservingStatus(b.Status)
            );

            var calculatedAvailableSlots = station.TotalSlots - (int)activeBookingsCount;
            calculatedAvailableSlots = Math.Max(0, Math.Min(station.TotalSlots, calculatedAvailableSlots));

            var stationUpdate = Builders<ChargingStation>.Update
                .Set(s => s.AvailableSlots, calculatedAvailableSlots)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            _stationsCollection.UpdateOne(s => s.Id == stationId, stationUpdate);

            return BookingStatusUpdateResult.Success($"Station available slots recalculated. Available: {calculatedAvailableSlots}/{station.TotalSlots}");
        }

        private StationValidationResult ValidateStation(string stationId)
        {
            var station = _stationsCollection.Find(s => s.Id == stationId && s.IsActive).FirstOrDefault();
            if (station == null)
                return StationValidationResult.Failed(ValidationMessages.StationNotFound);

            return StationValidationResult.Success(station);
        }
    }

    // Result classes for service operations
    public class BookingOperationResult
    {
        public bool IsSuccess { get; private set; }
        public string Message { get; private set; }
        public object Data { get; private set; }

        private BookingOperationResult() { }

        public static BookingOperationResult Success(string message, object data = null)
        {
            return new BookingOperationResult
            {
                IsSuccess = true,
                Message = message,
                Data = data
            };
        }

        public static BookingOperationResult Failed(string message)
        {
            return new BookingOperationResult
            {
                IsSuccess = false,
                Message = message
            };
        }
    }

    public class BookingsQueryResult
    {
        public bool IsSuccess { get; private set; }
        public List<Booking> Bookings { get; private set; }
        public string ErrorMessage { get; private set; }

        private BookingsQueryResult() { }

        public static BookingsQueryResult Success(List<Booking> bookings)
        {
            return new BookingsQueryResult
            {
                IsSuccess = true,
                Bookings = bookings
            };
        }

        public static BookingsQueryResult Failed(string errorMessage)
        {
            return new BookingsQueryResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }

    public class BookingRetrievalResult
    {
        public bool IsSuccess { get; private set; }
        public Booking Booking { get; private set; }
        public string ErrorMessage { get; private set; }

        private BookingRetrievalResult() { }

        public static BookingRetrievalResult Success(Booking booking)
        {
            return new BookingRetrievalResult
            {
                IsSuccess = true,
                Booking = booking
            };
        }

        public static BookingRetrievalResult Failed(string errorMessage)
        {
            return new BookingRetrievalResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }

    public class SlotAvailabilityResult
    {
        public bool IsSuccess { get; private set; }
        public object AvailabilityData { get; private set; }
        public string ErrorMessage { get; private set; }

        private SlotAvailabilityResult() { }

        public static SlotAvailabilityResult Success(object availabilityData)
        {
            return new SlotAvailabilityResult
            {
                IsSuccess = true,
                AvailabilityData = availabilityData
            };
        }

        public static SlotAvailabilityResult Failed(string errorMessage)
        {
            return new SlotAvailabilityResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }

    public class StationValidationResult
    {
        public bool IsSuccess { get; private set; }
        public ChargingStation Station { get; private set; }
        public string ErrorMessage { get; private set; }

        private StationValidationResult() { }

        public static StationValidationResult Success(ChargingStation station)
        {
            return new StationValidationResult
            {
                IsSuccess = true,
                Station = station
            };
        }

        public static StationValidationResult Failed(string errorMessage)
        {
            return new StationValidationResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }

    public class BookingStatusUpdateResult
    {
        public bool IsSuccess { get; private set; }
        public string Message { get; private set; }

        private BookingStatusUpdateResult() { }

        public static BookingStatusUpdateResult Success(string message)
        {
            return new BookingStatusUpdateResult
            {
                IsSuccess = true,
                Message = message
            };
        }

        public static BookingStatusUpdateResult Failed(string message)
        {
            return new BookingStatusUpdateResult
            {
                IsSuccess = false,
                Message = message
            };
        }
    }
}