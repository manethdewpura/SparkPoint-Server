/*
 * BookingService.cs
 * 
 * This service handles all business logic related to booking operations.
 * It manages booking creation, retrieval, updates, and cancellation with proper
 * authorization checks and time slot validation. All operations interact with
 * MongoDB collections for data persistence.
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using SparkPoint_Server.Models;
using SparkPoint_Server.Helpers;
using SparkPoint_Server.Constants;
using SparkPoint_Server.Enums;

namespace SparkPoint_Server.Services
{ 
    public class BookingService
    {
        private readonly IMongoCollection<Booking> _bookingsCollection;
        private readonly IMongoCollection<ChargingStation> _stationsCollection;
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<EVOwner> _evOwnersCollection;
        private readonly BookingAuthorizationHelper _authorizationHelper;

        // Constructor: Initializes MongoDB collections and authorization helper
        public BookingService()
        {
            var dbContext = new MongoDbContext();
            _bookingsCollection = dbContext.GetCollection<Booking>("Bookings");
            _stationsCollection = dbContext.GetCollection<ChargingStation>("ChargingStations");
            _usersCollection = dbContext.GetCollection<User>("Users");
            _evOwnersCollection = dbContext.GetCollection<EVOwner>("EVOwners");
            _authorizationHelper = new BookingAuthorizationHelper();
        }

        // Creates a new booking with validation and authorization
        public BookingOperationResult CreateBooking(BookingCreateModel model, UserContext userContext)
        {
            var validationResult = BookingValidationHelper.ValidateCreateBooking(model);
            if (!validationResult.IsValid)
                return BookingOperationResult.Failed(validationResult.ErrorMessage);

            var authResult = _authorizationHelper.CanCreateBooking(userContext);
            if (!authResult.IsAuthorized)
                return BookingOperationResult.Failed(authResult.ErrorMessage);

            var ownerResult = _authorizationHelper.ResolveOwnerNIC(userContext, model.OwnerNIC);
            if (!ownerResult.IsSuccess)
                return BookingOperationResult.Failed(ownerResult.ErrorMessage);

            var evOwner = _evOwnersCollection.Find(ev => ev.NIC == ownerResult.OwnerNIC).FirstOrDefault();
            if (evOwner == null)
                return BookingOperationResult.Failed("EV Owner not found.");

            var stationResult = ValidateStation(model.StationId);
            if (!stationResult.IsValid)
                return BookingOperationResult.Failed(stationResult.ErrorMessage);

            if (!CheckSlotAvailability(model.StationId, model.ReservationTime, model.SlotsRequested))
                return BookingOperationResult.Failed($"Only {GetAvailableSlotsAtTime(model.StationId, model.ReservationTime)} slots available for the requested time slot.");

            var booking = new Booking
            {
                OwnerNIC = ownerResult.OwnerNIC,
                StationId = model.StationId,
                ReservationTime = model.ReservationTime,
                SlotsRequested = model.SlotsRequested,
                Status = BookingStatusConstants.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _bookingsCollection.InsertOne(booking);

            var availableSlots = GetAvailableSlotsAtTime(model.StationId, model.ReservationTime);
            var slotDisplayName = TimeSlotConstants.GetSlotDisplayName(model.ReservationTime);

            return BookingOperationResult.Success(
                $"Booking created successfully for {slotDisplayName}.",
                new { 
                    BookingId = booking.Id, 
                    SlotsBooked = model.SlotsRequested,
                    TimeSlot = slotDisplayName,
                    AvailableSlotsRemaining = availableSlots 
                }
            );
        }

        // Retrieves bookings with filtering based on user role
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

            // Enrich bookings with station information
            var enrichedBookings = EnrichBookingsWithStationInfo(bookings);

            return BookingsQueryResult.Success(enrichedBookings);
        }

        // Retrieves a specific booking by ID with authorization
        public BookingRetrievalResult GetBooking(string bookingId, UserContext userContext)
        {
            var booking = _bookingsCollection.Find(b => b.Id == bookingId).FirstOrDefault();
            if (booking == null)
                return BookingRetrievalResult.Failed(ValidationMessages.BookingNotFound);

            var authResult = _authorizationHelper.CanAccessBooking(userContext, booking);
            if (!authResult.IsAuthorized)
                return BookingRetrievalResult.Failed(authResult.ErrorMessage);

            // Enrich booking with station information
            var enrichedBooking = EnrichBookingWithStationInfo(booking);

            return BookingRetrievalResult.Success(enrichedBooking);
        }

        // Updates an existing booking with validation
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
                var slotsToCheck = model.SlotsRequested ?? booking.SlotsRequested;
                if (!CheckSlotAvailability(booking.StationId, model.ReservationTime.Value, slotsToCheck, bookingId))
                    return BookingOperationResult.Failed($"Only {GetAvailableSlotsAtTime(booking.StationId, model.ReservationTime.Value)} slots available for the requested time slot.");

                updateBuilder = updateBuilder.Set(b => b.ReservationTime, model.ReservationTime.Value);
            }

            // Handle slots requested update
            if (model.SlotsRequested.HasValue)
            {
                var reservationTime = model.ReservationTime ?? booking.ReservationTime;
                if (!CheckSlotAvailability(booking.StationId, reservationTime, model.SlotsRequested.Value, bookingId))
                    return BookingOperationResult.Failed($"Only {GetAvailableSlotsAtTime(booking.StationId, reservationTime)} slots available for the requested time slot.");

                updateBuilder = updateBuilder.Set(b => b.SlotsRequested, model.SlotsRequested.Value);
            }

            // Handle station change
            if (!string.IsNullOrEmpty(model.StationId) && model.StationId != booking.StationId)
            {
                var stationResult = ValidateStation(model.StationId);
                if (!stationResult.IsValid)
                    return BookingOperationResult.Failed(stationResult.ErrorMessage);

                var reservationTime = model.ReservationTime ?? booking.ReservationTime;
                var slotsToCheck = model.SlotsRequested ?? booking.SlotsRequested;
                if (!CheckSlotAvailability(model.StationId, reservationTime, slotsToCheck))
                    return BookingOperationResult.Failed("Insufficient slots available at the new station for the requested time.");

                updateBuilder = updateBuilder.Set(b => b.StationId, model.StationId);
            }

            _bookingsCollection.UpdateOne(b => b.Id == bookingId, updateBuilder);

            return BookingOperationResult.Success("Booking updated successfully.");
        }

        // Cancels a booking with proper slot management
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

        // Updates booking status with slot management
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

        // Checks slot availability and returns detailed result
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

        // Updates booking status with automatic slot management
        public BookingStatusUpdateResult UpdateBookingStatusWithSlotManagement(string bookingId, string newStatus, string oldStatus)
        {
            var booking = _bookingsCollection.Find(b => b.Id == bookingId).FirstOrDefault();
            if (booking == null)
                return BookingStatusUpdateResult.Failed(ValidationMessages.BookingNotFound);

            var station = _stationsCollection.Find(s => s.Id == booking.StationId).FirstOrDefault();
            if (station == null)
                return BookingStatusUpdateResult.Failed(ValidationMessages.StationNotFound);

            // Calculate slot changes based on the number of slots in the booking
            var slotChange = CalculateSlotChange(oldStatus, newStatus, booking.SlotsRequested);
            
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

        // Calculates slot change based on status transitions
        private int CalculateSlotChange(string oldStatus, string newStatus, int slotsRequested)
        {
            var oldReservesSlot = BookingStatusConstants.IsSlotReservingStatus(oldStatus);
            var newReservesSlot = BookingStatusConstants.IsSlotReservingStatus(newStatus);
            var oldFreesSlot = BookingStatusConstants.IsSlotFreeingStatus(oldStatus);
            var newFreesSlot = BookingStatusConstants.IsSlotFreeingStatus(newStatus);

            // If moving from a slot-reserving status to a slot-freeing status
            if (oldReservesSlot && newFreesSlot)
                return slotsRequested; // Free the requested slots

            // If moving from a non-reserving status to a slot-reserving status
            if (!oldReservesSlot && !oldFreesSlot && newReservesSlot)
                return -slotsRequested; // Reserve the requested slots

            // If moving from a slot-freeing status to a slot-reserving status (rare case)
            if (oldFreesSlot && newReservesSlot)
                return -slotsRequested; // Reserve the requested slots

            // If moving from "Pending" to "Confirmed" or "In Progress"
            if (oldStatus == BookingStatusConstants.Pending && BookingStatusConstants.IsSlotReservingStatus(newStatus))
                return -slotsRequested; // Reserve the requested slots

            return 0; // No change in slot allocation
        }

        // Checks if slots are available for booking
        public bool CheckSlotAvailability(string stationId, DateTime reservationTime, int slotsRequested = 1, string excludeBookingId = null)
        {
            var station = _stationsCollection.Find(s => s.Id == stationId).FirstOrDefault();
            if (station == null)
                return false;

            var availableSlots = GetAvailableSlotsAtTime(stationId, reservationTime, excludeBookingId);
            return availableSlots >= slotsRequested;
        }

        // Gets number of available slots at specific time
        public int GetAvailableSlotsAtTime(string stationId, DateTime reservationTime, string excludeBookingId = null)
        {
            var station = _stationsCollection.Find(s => s.Id == stationId).FirstOrDefault();
            if (station == null)
                return 0;

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

            var bookedSlots = _bookingsCollection.Find(filterBuilder)
                .ToList()
                .Sum(b => b.SlotsRequested);

            return station.TotalSlots - bookedSlots;
        }

        // Recalculates station available slots based on active bookings
        public BookingStatusUpdateResult RecalculateStationAvailableSlots(string stationId)
        {
            var station = _stationsCollection.Find(s => s.Id == stationId).FirstOrDefault();
            if (station == null)
                return BookingStatusUpdateResult.Failed(ValidationMessages.StationNotFound);

            // Get all active bookings (confirmed or in progress) for this station
            var activeBookingsCount = _bookingsCollection.CountDocuments(b =>
                b.StationId == stationId &&
                BookingStatusConstants.SlotReservingStatuses.Contains(b.Status)
            );

            var calculatedAvailableSlots = station.TotalSlots - (int)activeBookingsCount;
            calculatedAvailableSlots = Math.Max(0, Math.Min(station.TotalSlots, calculatedAvailableSlots));

            var stationUpdate = Builders<ChargingStation>.Update
                .Set(s => s.AvailableSlots, calculatedAvailableSlots)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            _stationsCollection.UpdateOne(s => s.Id == stationId, stationUpdate);

            return BookingStatusUpdateResult.Success($"Station available slots recalculated. Available: {calculatedAvailableSlots}/{station.TotalSlots}");
        }

        // Validates station exists and is active
        private StationValidationResult ValidateStation(string stationId)
        {
            var station = _stationsCollection.Find(s => s.Id == stationId && s.IsActive).FirstOrDefault();
            if (station == null)
                return StationValidationResult.Failed(StationValidationError.None, ValidationMessages.StationNotFound);

            return StationValidationResult.Success();
        }

        // Enriches bookings with station information
        private List<BookingWithStationInfo> EnrichBookingsWithStationInfo(List<Booking> bookings)
        {
            if (bookings == null || !bookings.Any())
                return new List<BookingWithStationInfo>();

            // Get unique station IDs
            var stationIds = bookings.Select(b => b.StationId).Distinct().ToList();

            // Fetch all stations in one query
            var stations = _stationsCollection.Find(s => stationIds.Contains(s.Id)).ToList();
            var stationDict = stations.ToDictionary(s => s.Id, s => s);

            // Map bookings to enriched bookings
            return bookings.Select(booking =>
            {
                var enrichedBooking = new BookingWithStationInfo
                {
                    Id = booking.Id,
                    OwnerNIC = booking.OwnerNIC,
                    StationId = booking.StationId,
                    ReservationTime = booking.ReservationTime,
                    SlotsRequested = booking.SlotsRequested,
                    Status = booking.Status,
                    CreatedAt = booking.CreatedAt,
                    UpdatedAt = booking.UpdatedAt,
                    TimeSlotDisplay = TimeSlotConstants.GetSlotDisplayName(booking.ReservationTime),
                    SlotEndTime = TimeSlotConstants.GetSlotEndTime(booking.ReservationTime)
                };

                // Add basic station information if available (without slot details)
                if (stationDict.TryGetValue(booking.StationId, out var station))
                {
                    enrichedBooking.Station = new ChargingStationBasicInfo
                    {
                        Id = station.Id,
                        Name = station.Name,
                        Location = station.Location,
                        Address = station.Address,
                        City = station.City,
                        Province = station.Province,
                        ContactPhone = station.ContactPhone,
                        ContactEmail = station.ContactEmail,
                        Type = station.Type
                    };
                }

                return enrichedBooking;
            }).ToList();
        }

        // Enriches single booking with station information
        private BookingWithStationInfo EnrichBookingWithStationInfo(Booking booking)
        {
            if (booking == null)
                return null;

            var enrichedBooking = new BookingWithStationInfo
            {
                Id = booking.Id,
                OwnerNIC = booking.OwnerNIC,
                StationId = booking.StationId,
                ReservationTime = booking.ReservationTime,
                SlotsRequested = booking.SlotsRequested,
                Status = booking.Status,
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt,
                TimeSlotDisplay = TimeSlotConstants.GetSlotDisplayName(booking.ReservationTime),
                SlotEndTime = TimeSlotConstants.GetSlotEndTime(booking.ReservationTime)
            };

            // Get basic station information (without slot details)
            var station = _stationsCollection.Find(s => s.Id == booking.StationId).FirstOrDefault();
            if (station != null)
            {
                enrichedBooking.Station = new ChargingStationBasicInfo
                {
                    Id = station.Id,
                    Name = station.Name,
                    Location = station.Location,
                    Address = station.Address,
                    City = station.City,
                    Province = station.Province,
                    ContactPhone = station.ContactPhone,
                    ContactEmail = station.ContactEmail,
                    Type = station.Type
                };
            }

            return enrichedBooking;
        }

        // Gets nearby stations availability for a specific date
        public NearbyStationsAvailabilityResult GetNearbyStationsAvailability(DateTime date, double? latitude, double? longitude, double radiusKm)
        {
            try
            {
                var timeSlots = TimeSlotConstants.GetAvailableTimeSlotsForDate(date);
                var validTimeSlots = timeSlots.Where(TimeSlotConstants.IsWithinOperatingHours).ToList();

                // Get all active stations
                var stationsFilter = Builders<ChargingStation>.Filter.Eq(s => s.IsActive, true);
                
                // If location is provided, filter by distance
                if (latitude.HasValue && longitude.HasValue)
                {
                    // For MongoDB with location queries, you would typically use $near or $geoWithin
                    // For now, we'll get all stations and filter in memory (not optimal for large datasets)
                    var allStations = _stationsCollection.Find(stationsFilter).ToList();
                    
                    var nearbyStations = allStations.Where(station => 
                        CalculateDistance(latitude.Value, longitude.Value, 
                                        station.Location.Latitude, station.Location.Longitude) <= radiusKm)
                        .ToList();

                    var stationAvailability = nearbyStations.Select(station => 
                        CreateStationAvailabilityInfo(station, validTimeSlots)).ToList();

                    return NearbyStationsAvailabilityResult.Success(new
                    {
                        Date = date.Date,
                        UserLocation = new { Latitude = latitude.Value, Longitude = longitude.Value },
                        RadiusKm = radiusKm,
                        TimeSlots = validTimeSlots.Select(slot => new
                        {
                            StartTime = slot,
                            EndTime = TimeSlotConstants.GetSlotEndTime(slot),
                            DisplayName = TimeSlotConstants.GetSlotDisplayName(slot)
                        }),
                        Stations = stationAvailability,
                        OperatingHours = "6:00 AM - 12:00 AM",
                        SlotDuration = "2 hours"
                    });
                }
                else
                {
                    // Return all active stations if no location provided
                    var allStations = _stationsCollection.Find(stationsFilter).ToList();
                    var stationAvailability = allStations.Select(station => 
                        CreateStationAvailabilityInfo(station, validTimeSlots)).ToList();

                    return NearbyStationsAvailabilityResult.Success(new
                    {
                        Date = date.Date,
                        TimeSlots = validTimeSlots.Select(slot => new
                        {
                            StartTime = slot,
                            EndTime = TimeSlotConstants.GetSlotEndTime(slot),
                            DisplayName = TimeSlotConstants.GetSlotDisplayName(slot)
                        }),
                        Stations = stationAvailability,
                        OperatingHours = "6:00 AM - 12:00 AM",
                        SlotDuration = "2 hours"
                    });
                }
            }
            catch (Exception ex)
            {
                return NearbyStationsAvailabilityResult.Failed($"Error retrieving nearby stations availability: {ex.Message}");
            }
        }

        // Creates station availability info for time slots
        private object CreateStationAvailabilityInfo(ChargingStation station, List<DateTime> timeSlots)
        {
            var slotAvailability = timeSlots.Select(slot => new
            {
                StartTime = slot,
                EndTime = TimeSlotConstants.GetSlotEndTime(slot),
                DisplayName = TimeSlotConstants.GetSlotDisplayName(slot),
                AvailableSlots = GetAvailableSlotsAtTime(station.Id, slot),
                IsAvailable = GetAvailableSlotsAtTime(station.Id, slot) > 0
            }).ToList();

            return new
            {
                StationId = station.Id,
                StationName = station.Name,
                Location = station.Location,
                TotalSlots = station.TotalSlots,
                Type = station.Type,
                TimeSlotAvailability = slotAvailability
            };
        }

        // Calculates distance between two coordinates using Haversine formula
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula to calculate distance between two coordinates
            const double R = 6371; // Earth's radius in kilometers
            
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return R * c;
        }

        // Converts degrees to radians for distance calculation
        private double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        // ...existing other methods...
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
        public List<BookingWithStationInfo> Bookings { get; private set; }
        public string ErrorMessage { get; private set; }

        private BookingsQueryResult() { }

        public static BookingsQueryResult Success(List<BookingWithStationInfo> bookings)
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
        public BookingWithStationInfo Booking { get; private set; }
        public string ErrorMessage { get; private set; }

        private BookingRetrievalResult() { }

        public static BookingRetrievalResult Success(BookingWithStationInfo booking)
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

    public class NearbyStationsAvailabilityResult
    {
        public bool IsSuccess { get; private set; }
        public object Data { get; private set; }
        public string ErrorMessage { get; private set; }

        private NearbyStationsAvailabilityResult() { }

        public static NearbyStationsAvailabilityResult Success(object data)
        {
            return new NearbyStationsAvailabilityResult
            {
                IsSuccess = true,
                Data = data
            };
        }

        public static NearbyStationsAvailabilityResult Failed(string errorMessage)
        {
            return new NearbyStationsAvailabilityResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }
}