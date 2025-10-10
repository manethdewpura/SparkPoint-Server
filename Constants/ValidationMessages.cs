/*
 * ValidationMessages.cs
 * 
 * This file contains validation error messages used throughout the system.
 * It includes messages for booking validation, user validation, station validation,
 * authorization, and status validation operations.
 */

namespace SparkPoint_Server.Constants
{

    public static class ValidationMessages
    {
        // General validation messages
        public const string RequiredField = "This field is required.";
        public const string InvalidFormat = "Invalid format.";
        public const string InvalidValue = "Invalid value.";

        // Booking validation messages
        public const string BookingDataRequired = "Booking data is required.";
        public const string StationIdRequired = "Station ID is required.";
        public const string ReservationTimeRequired = "Reservation time is required.";
        public const string OwnerNICRequired = "Owner NIC is required for admin bookings.";
        public const string PastDateReservation = "Cannot make reservations for past dates.";
        public const string MaxAdvanceReservation = "Reservations can only be made up to 7 days in advance.";
        public const string MinCancellationTime = "Cannot cancel booking less than 12 hours before reservation time.";
        public const string MinModificationTime = "Cannot modify booking less than 12 hours before reservation time.";
        public const string NoAvailableSlots = "No available slots at the requested time.";
        public const string BookingNotFound = "Booking not found.";
        public const string BookingAlreadyCancelled = "Booking is already cancelled.";
        public const string CannotCancelCompleted = "Cannot cancel completed booking.";
        public const string CannotModifyCompleted = "Cannot modify cancelled or completed bookings.";

        // User validation messages
        public const string UserNotFound = "User not found.";
        public const string UsernameRequired = "Username is required.";
        public const string EmailRequired = "Email is required.";
        public const string PasswordRequired = "Password is required.";
        public const string InvalidEmail = "Invalid email format.";
        public const string WeakPassword = "Password must be at least 6 characters long.";
        public const string UserInactive = "User account is inactive.";

        // Station validation messages
        public const string StationNotFound = "Station not found or inactive.";
        public const string StationInactive = "Station is inactive.";

        // Authorization messages
        public const string AccessDenied = "Access denied.";
        public const string Unauthorized = "Unauthorized access.";
        public const string InsufficientPermissions = "Insufficient permissions.";

        // Status validation messages
        public const string InvalidStatus = "Invalid status value.";
        public const string StatusRequired = "Status is required.";
        public const string InvalidStatusTransition = "Invalid status transition.";
        public const string NearTimeStatusRestriction = "Only 'In Progress', 'Completed', and 'No Show' status updates are allowed within 12 hours of reservation time.";
    }
}