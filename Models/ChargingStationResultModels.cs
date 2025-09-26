using System.Collections.Generic;
using SparkPoint_Server.Enums;
using SparkPoint_Server.Constants;
using SparkPoint_Server.Models;

namespace SparkPoint_Server.Models
{
    /// <summary>
    /// Result of station operation
    /// </summary>
    public class StationOperationResult
    {
        public StationOperationStatus Status { get; private set; }
        public bool IsSuccess => Status == StationOperationStatus.Success;
        public string Message { get; private set; }
        public object Data { get; private set; }

        private StationOperationResult() { }

        public static StationOperationResult Success(string message, object data = null)
        {
            return new StationOperationResult
            {
                Status = StationOperationStatus.Success,
                Message = message,
                Data = data
            };
        }

        public static StationOperationResult Failed(StationOperationStatus status, string customMessage = null)
        {
            var message = customMessage ?? GetDefaultErrorMessage(status);
            return new StationOperationResult
            {
                Status = status,
                Message = message
            };
        }

        private static string GetDefaultErrorMessage(StationOperationStatus status)
        {
            switch (status)
            {
                case StationOperationStatus.StationNotFound:
                    return ChargingStationConstants.StationNotFound;
                case StationOperationStatus.AlreadyInState:
                    return "Station is already in the requested state";
                case StationOperationStatus.HasActiveBookings:
                    return ChargingStationConstants.CannotDeactivateWithActiveBookings;
                case StationOperationStatus.ValidationFailed:
                    return "Validation failed";
                default:
                    return "Operation failed";
            }
        }
    }

    /// <summary>
    /// Result of station query operation
    /// </summary>
    public class StationQueryResult
    {
        public bool IsSuccess { get; private set; }
        public List<ChargingStation> Stations { get; private set; }
        public string ErrorMessage { get; private set; }

        private StationQueryResult() { }

        public static StationQueryResult Success(List<ChargingStation> stations)
        {
            return new StationQueryResult
            {
                IsSuccess = true,
                Stations = stations
            };
        }

        public static StationQueryResult Failed(string errorMessage)
        {
            return new StationQueryResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Result of single station retrieval
    /// </summary>
    public class StationRetrievalResult
    {
        public bool IsSuccess { get; private set; }
        public ChargingStation Station { get; private set; }
        public List<object> StationUsers { get; private set; }
        public string ErrorMessage { get; private set; }

        private StationRetrievalResult() { }

        public static StationRetrievalResult Success(ChargingStation station, List<object> stationUsers = null)
        {
            return new StationRetrievalResult
            {
                IsSuccess = true,
                Station = station,
                StationUsers = stationUsers
            };
        }

        public static StationRetrievalResult Failed(string errorMessage)
        {
            return new StationRetrievalResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Station validation result
    /// </summary>
    public class StationValidationResult
    {
        public bool IsValid { get; private set; }
        public List<StationValidationError> Errors { get; private set; }
        public string ErrorMessage { get; private set; }

        private StationValidationResult()
        {
            Errors = new List<StationValidationError>();
        }

        public static StationValidationResult Success()
        {
            return new StationValidationResult
            {
                IsValid = true
            };
        }

        public static StationValidationResult Failed(StationValidationError error, string message = null)
        {
            return new StationValidationResult
            {
                IsValid = false,
                Errors = new List<StationValidationError> { error },
                ErrorMessage = message ?? GetDefaultErrorMessage(error)
            };
        }

        public static StationValidationResult Failed(List<StationValidationError> errors, string message = null)
        {
            return new StationValidationResult
            {
                IsValid = false,
                Errors = errors,
                ErrorMessage = message ?? "Multiple validation errors occurred"
            };
        }

        private static string GetDefaultErrorMessage(StationValidationError error)
        {
            switch (error)
            {
                case StationValidationError.LocationRequired:
                    return ChargingStationConstants.LocationRequired;
                case StationValidationError.LocationTooLong:
                    return $"Location cannot exceed {ChargingStationConstants.MaxLocationLength} characters";
                case StationValidationError.TypeRequired:
                    return ChargingStationConstants.TypeRequired;
                case StationValidationError.InvalidType:
                    return "Invalid station type";
                case StationValidationError.TotalSlotsMustBePositive:
                    return ChargingStationConstants.TotalSlotsMustBePositive;
                case StationValidationError.TotalSlotsExceedsMaximum:
                    return $"Total slots cannot exceed {ChargingStationConstants.MaxTotalSlots}";
                default:
                    return "Validation error";
            }
        }
    }
}