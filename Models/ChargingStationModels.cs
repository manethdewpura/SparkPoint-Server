using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SparkPoint_Server.Constants;
using SparkPoint_Server.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SparkPoint_Server.Models
{
    public class ChargingStation
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("location")]
        public LocationCoordinates Location { get; set; }

        [BsonElement("address")]
        public string Address { get; set; }

        [BsonElement("city")]
        public string City { get; set; }

        [BsonElement("province")]
        public string Province { get; set; }

        [BsonElement("contactPhone")]
        public string ContactPhone { get; set; }

        [BsonElement("contactEmail")]
        public string ContactEmail { get; set; }

        [BsonElement("type")]
        public string Type { get; set; }

        [BsonElement("totalSlots")]
        public int TotalSlots { get; set; }

        [BsonElement("availableSlots")]
        public int AvailableSlots { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class LocationCoordinates
    {
        [BsonElement("longitude")]
        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180")]
        public double Longitude { get; set; }

        [BsonElement("latitude")]
        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90")]
        public double Latitude { get; set; }

        public LocationCoordinates() { }

        public LocationCoordinates(double longitude, double latitude)
        {
            Longitude = longitude;
            Latitude = latitude;
        }
    }

    public class StationCreateModel
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Location coordinates are required")]
        public LocationCoordinates Location { get; set; }

        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string Address { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string City { get; set; }

        [StringLength(100, ErrorMessage = "Province cannot exceed 100 characters")]
        public string Province { get; set; }

        [StringLength(15, ErrorMessage = "Contact phone cannot exceed 15 characters")]
        public string ContactPhone { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Contact email cannot exceed 100 characters")]
        public string ContactEmail { get; set; }

        [Required(ErrorMessage = "Type is required")]
        [StringLength(50, ErrorMessage = "Type cannot exceed 50 characters")]
        public string Type { get; set; }

        [Range(1, 100, ErrorMessage = "Total slots must be between 1 and 100")]
        public int TotalSlots { get; set; }
    }

    public class StationUpdateModel
    {
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; }

        public LocationCoordinates Location { get; set; }

        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string Address { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string City { get; set; }

        [StringLength(100, ErrorMessage = "Province cannot exceed 100 characters")]
        public string Province { get; set; }

        [StringLength(15, ErrorMessage = "Contact phone cannot exceed 15 characters")]
        public string ContactPhone { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Contact email cannot exceed 100 characters")]
        public string ContactEmail { get; set; }

        [StringLength(50, ErrorMessage = "Type cannot exceed 50 characters")]
        public string Type { get; set; }

        [Range(1, 100, ErrorMessage = "Total slots must be between 1 and 100")]
        public int? TotalSlots { get; set; }
    }

    public class StationFilterModel
    {
        public bool? IsActive { get; set; }
        
        [StringLength(100, ErrorMessage = "Search term cannot exceed 100 characters")]
        public string SearchTerm { get; set; }

        public LocationCoordinates NearLocation { get; set; }
        public double? MaxDistanceKm { get; set; }
    }

    public class StationQueryModel : StationFilterModel
    {
        public string SortBy { get; set; } = "CreatedAt";
        public string SortOrder { get; set; } = "Descending";
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }
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
                case StationValidationError.NameRequired:
                    return ChargingStationConstants.NameRequired;
                case StationValidationError.NameTooLong:
                    return $"Station name cannot exceed {ChargingStationConstants.MaxNameLength} characters";
                case StationValidationError.LocationRequired:
                    return ChargingStationConstants.LocationRequired;
                case StationValidationError.LongitudeRequired:
                    return ChargingStationConstants.LongitudeRequired;
                case StationValidationError.LatitudeRequired:
                    return ChargingStationConstants.LatitudeRequired;
                case StationValidationError.InvalidLongitude:
                    return ChargingStationConstants.InvalidLongitude;
                case StationValidationError.InvalidLatitude:
                    return ChargingStationConstants.InvalidLatitude;
                case StationValidationError.TypeRequired:
                    return ChargingStationConstants.TypeRequired;
                case StationValidationError.InvalidType:
                    return "Invalid station type";
                case StationValidationError.TotalSlotsMustBePositive:
                    return ChargingStationConstants.TotalSlotsMustBePositive;
                case StationValidationError.TotalSlotsExceedsMaximum:
                    return $"Total slots cannot exceed {ChargingStationConstants.MaxTotalSlots}";
                case StationValidationError.AddressTooLong:
                    return ChargingStationConstants.AddressTooLong;
                case StationValidationError.CityTooLong:
                    return ChargingStationConstants.CityTooLong;
                case StationValidationError.ProvinceTooLong:
                    return ChargingStationConstants.ProvinceTooLong;
                case StationValidationError.ContactPhoneTooLong:
                    return ChargingStationConstants.ContactPhoneTooLong;
                case StationValidationError.ContactEmailTooLong:
                    return ChargingStationConstants.ContactEmailTooLong;
                case StationValidationError.InvalidContactEmail:
                    return ChargingStationConstants.InvalidContactEmail;
                default:
                    return "Validation error";
            }
        }
    }
}
