using System;
using SparkPoint_Server.Models;
using SparkPoint_Server.Constants;

namespace SparkPoint_Server.Helpers
{
    public static class BookingValidationHelper
    {

        public static ValidationResult ValidateCreateBooking(BookingCreateModel model)
        {
            if (model == null)
                return ValidationResult.Failed(ValidationMessages.BookingDataRequired);

            if (string.IsNullOrEmpty(model.StationId))
                return ValidationResult.Failed(ValidationMessages.StationIdRequired);

            if (model.ReservationTime == DateTime.MinValue)
                return ValidationResult.Failed(ValidationMessages.ReservationTimeRequired);

            var reservationTimeResult = ValidateReservationTime(model.ReservationTime);
            if (!reservationTimeResult.IsValid)
                return reservationTimeResult;

            return ValidationResult.Success();
        }

        public static ValidationResult ValidateUpdateBooking(BookingUpdateModel model, Booking currentBooking)
        {
            if (model == null)
                return ValidationResult.Failed("Update data is required.");

            if (currentBooking == null)
                return ValidationResult.Failed(ValidationMessages.BookingNotFound);

            // Check if booking can be modified (at least 12 hours before reservation)
            var hoursUntilReservation = (currentBooking.ReservationTime - DateTime.Now).TotalHours;
            if (hoursUntilReservation < ApplicationConstants.MinModificationHours)
                return ValidationResult.Failed(ValidationMessages.MinModificationTime);

            // Check if booking is in a modifiable state
            if (currentBooking.Status == BookingStatusConstants.Cancelled || 
                currentBooking.Status == BookingStatusConstants.Completed)
                return ValidationResult.Failed(ValidationMessages.CannotModifyCompleted);

            if (model.ReservationTime.HasValue)
            {
                var reservationTimeResult = ValidateReservationTime(model.ReservationTime.Value);
                if (!reservationTimeResult.IsValid)
                    return reservationTimeResult;
            }

            return ValidationResult.Success();
        }

        public static ValidationResult ValidateReservationTime(DateTime reservationTime)
        {
            var daysFromNow = (reservationTime.Date - DateTime.Now.Date).Days;
            
            if (daysFromNow < 0)
                return ValidationResult.Failed(ValidationMessages.PastDateReservation);
            
            if (daysFromNow > ApplicationConstants.MaxAdvanceReservationDays)
                return ValidationResult.Failed(ValidationMessages.MaxAdvanceReservation);

            return ValidationResult.Success();
        }

        public static ValidationResult ValidateCancelBooking(Booking booking)
        {
            if (booking == null)
                return ValidationResult.Failed(ValidationMessages.BookingNotFound);

            if (booking.Status == BookingStatusConstants.Cancelled)
                return ValidationResult.Failed(ValidationMessages.BookingAlreadyCancelled);
            
            if (booking.Status == BookingStatusConstants.Completed)
                return ValidationResult.Failed(ValidationMessages.CannotCancelCompleted);

            var hoursUntilReservation = (booking.ReservationTime - DateTime.Now).TotalHours;
            if (hoursUntilReservation < ApplicationConstants.MinCancellationHours)
                return ValidationResult.Failed(ValidationMessages.MinCancellationTime);

            return ValidationResult.Success();
        }

        public static ValidationResult ValidateStatusUpdate(BookingStatusUpdateModel model, Booking currentBooking)
        {
            if (model == null || string.IsNullOrEmpty(model.Status))
                return ValidationResult.Failed(ValidationMessages.StatusRequired);

            if (currentBooking == null)
                return ValidationResult.Failed(ValidationMessages.BookingNotFound);

            if (!BookingStatusConstants.IsValidStatus(model.Status))
                return ValidationResult.Failed(ValidationMessages.InvalidStatus);

            var hoursUntilReservation = (currentBooking.ReservationTime - DateTime.Now).TotalHours;
            
            // If less than 12 hours before reservation, only allow certain status updates
            if (hoursUntilReservation < ApplicationConstants.MinModificationHours)
            {
                if (!BookingStatusConstants.IsNearTimeAllowedStatus(model.Status))
                    return ValidationResult.Failed(ValidationMessages.NearTimeStatusRestriction);
            }

            // Prevent certain status changes
            if (currentBooking.Status == BookingStatusConstants.Completed && model.Status != BookingStatusConstants.Completed)
                return ValidationResult.Failed("Cannot change status of completed booking.");
            
            if (currentBooking.Status == BookingStatusConstants.Cancelled && model.Status != BookingStatusConstants.Cancelled)
                return ValidationResult.Failed("Cannot change status of cancelled booking.");

            return ValidationResult.Success();
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; private set; }
        public string ErrorMessage { get; private set; }

        private ValidationResult() { }

        public static ValidationResult Success()
        {
            return new ValidationResult { IsValid = true };
        }

        public static ValidationResult Failed(string errorMessage)
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = errorMessage
            };
        }
    }
}