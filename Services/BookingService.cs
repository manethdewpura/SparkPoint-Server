using System;
using System.Linq;
using MongoDB.Driver;
using SparkPoint_Server.Models;
using SparkPoint_Server.Helpers;
using SparkPoint_Server.Utils;

namespace SparkPoint_Server.Services
{
    public class BookingService
    {
        private readonly IMongoCollection<Booking> _bookingsCollection;
        private readonly IMongoCollection<ChargingStation> _stationsCollection;
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<EVOwner> _evOwnersCollection;

        public BookingService()
        {
            var dbContext = new MongoDbContext();
            _bookingsCollection = dbContext.GetCollection<Booking>("Bookings");
            _stationsCollection = dbContext.GetCollection<ChargingStation>("ChargingStations");
            _usersCollection = dbContext.GetCollection<User>("Users");
            _evOwnersCollection = dbContext.GetCollection<EVOwner>("EVOwners");
        }

        /// <summary>
        /// Updates the booking status and manages available slots accordingly
        /// </summary>
        /// <param name="bookingId">The booking ID</param>
        /// <param name="newStatus">The new status</param>
        /// <param name="oldStatus">The current/old status</param>
        /// <returns>Result of the operation</returns>
        public BookingStatusUpdateResult UpdateBookingStatus(string bookingId, string newStatus, string oldStatus)
        {
            var booking = _bookingsCollection.Find(b => b.Id == bookingId).FirstOrDefault();
            if (booking == null)
                return BookingStatusUpdateResult.Failed("Booking not found.");

            var station = _stationsCollection.Find(s => s.Id == booking.StationId).FirstOrDefault();
            if (station == null)
                return BookingStatusUpdateResult.Failed("Station not found.");

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

        /// <summary>
        /// Calculates how many slots should be freed (+) or reserved (-) based on status change
        /// </summary>
        /// <param name="oldStatus">Current status</param>
        /// <param name="newStatus">New status</param>
        /// <returns>Positive number means slots are freed, negative means slots are reserved</returns>
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

        /// <summary>
        /// Checks if there are available slots for a booking at a specific time
        /// </summary>
        /// <param name="stationId">Station ID</param>
        /// <param name="reservationTime">Reservation time</param>
        /// <param name="excludeBookingId">Booking ID to exclude from the check (for updates)</param>
        /// <returns>True if slots are available</returns>
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

        /// <summary>
        /// Gets current available slots for a station at a specific time
        /// </summary>
        /// <param name="stationId">Station ID</param>
        /// <param name="reservationTime">Reservation time</param>
        /// <returns>Number of available slots at that time</returns>
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

        /// <summary>
        /// Recalculates and updates the available slots for a station based on current bookings
        /// This method can be used for data consistency maintenance
        /// </summary>
        /// <param name="stationId">Station ID</param>
        /// <returns>Result of the recalculation</returns>
        public BookingStatusUpdateResult RecalculateStationAvailableSlots(string stationId)
        {
            var station = _stationsCollection.Find(s => s.Id == stationId).FirstOrDefault();
            if (station == null)
                return BookingStatusUpdateResult.Failed("Station not found.");

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
    }

    /// <summary>
    /// Result of booking status update operation
    /// </summary>
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