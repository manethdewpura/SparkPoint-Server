/*
 * ChargingStationUtils.cs
 * 
 * This utility class provides charging station-related validation and helper methods.
 * It includes model validation, data sanitization, coordinate validation,
 * and various utility functions for charging station operations.
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SparkPoint_Server.Constants;
using SparkPoint_Server.Enums;
using SparkPoint_Server.Models;

namespace SparkPoint_Server.Utils
{
    public static class ChargingStationUtils
    {
        // Validates charging station creation model data
        public static StationValidationResult ValidateCreateModel(StationCreateModel model)
        {
            if (model == null)
                return StationValidationResult.Failed(StationValidationError.None, ChargingStationConstants.StationDataRequired);

            var errors = new List<StationValidationError>();

            // Validate name
            if (string.IsNullOrEmpty(model.Name))
                errors.Add(StationValidationError.NameRequired);
            else if (model.Name.Length > ChargingStationConstants.MaxNameLength)
                errors.Add(StationValidationError.NameTooLong);

            // Validate location coordinates
            if (model.Location == null)
                errors.Add(StationValidationError.LocationRequired);
            else
            {
                var coordinateErrors = ValidateLocationCoordinates(model.Location);
                errors.AddRange(coordinateErrors);
            }

            // Validate address (optional)
            if (!string.IsNullOrEmpty(model.Address) && model.Address.Length > ChargingStationConstants.MaxAddressLength)
                errors.Add(StationValidationError.AddressTooLong);

            // Validate city (optional)
            if (!string.IsNullOrEmpty(model.City) && model.City.Length > ChargingStationConstants.MaxCityLength)
                errors.Add(StationValidationError.CityTooLong);

            // Validate province (optional)
            if (!string.IsNullOrEmpty(model.Province) && model.Province.Length > ChargingStationConstants.MaxProvinceLength)
                errors.Add(StationValidationError.ProvinceTooLong);

            // Validate contact phone (optional)
            if (!string.IsNullOrEmpty(model.ContactPhone) && model.ContactPhone.Length > ChargingStationConstants.MaxContactPhoneLength)
                errors.Add(StationValidationError.ContactPhoneTooLong);

            // Validate contact email (optional)
            if (!string.IsNullOrEmpty(model.ContactEmail))
            {
                if (model.ContactEmail.Length > ChargingStationConstants.MaxContactEmailLength)
                    errors.Add(StationValidationError.ContactEmailTooLong);
                else if (!IsValidEmail(model.ContactEmail))
                    errors.Add(StationValidationError.InvalidContactEmail);
            }

            if (string.IsNullOrEmpty(model.Type))
                errors.Add(StationValidationError.TypeRequired);
            else if (!IsValidStationType(model.Type))
                errors.Add(StationValidationError.InvalidType);

            if (model.TotalSlots <= 0)
                errors.Add(StationValidationError.TotalSlotsMustBePositive);
            else if (model.TotalSlots > ChargingStationConstants.MaxTotalSlots)
                errors.Add(StationValidationError.TotalSlotsExceedsMaximum);

            return errors.Any() ? StationValidationResult.Failed(errors) : StationValidationResult.Success();
        }

        // Validates charging station update model data
        public static StationValidationResult ValidateUpdateModel(StationUpdateModel model)
        {
            if (model == null)
                return StationValidationResult.Failed(StationValidationError.None, ChargingStationConstants.UpdateDataRequired);

            var errors = new List<StationValidationError>();

            // Validate name if provided
            if (!string.IsNullOrEmpty(model.Name))
            {
                if (model.Name.Length > ChargingStationConstants.MaxNameLength)
                    errors.Add(StationValidationError.NameTooLong);
            }

            // Validate location coordinates if provided
            if (model.Location != null)
            {
                var coordinateErrors = ValidateLocationCoordinates(model.Location);
                errors.AddRange(coordinateErrors);
            }

            // Validate address if provided
            if (!string.IsNullOrEmpty(model.Address) && model.Address.Length > ChargingStationConstants.MaxAddressLength)
                errors.Add(StationValidationError.AddressTooLong);

            // Validate city if provided
            if (!string.IsNullOrEmpty(model.City) && model.City.Length > ChargingStationConstants.MaxCityLength)
                errors.Add(StationValidationError.CityTooLong);

            // Validate province if provided
            if (!string.IsNullOrEmpty(model.Province) && model.Province.Length > ChargingStationConstants.MaxProvinceLength)
                errors.Add(StationValidationError.ProvinceTooLong);

            // Validate contact phone if provided
            if (!string.IsNullOrEmpty(model.ContactPhone) && model.ContactPhone.Length > ChargingStationConstants.MaxContactPhoneLength)
                errors.Add(StationValidationError.ContactPhoneTooLong);

            // Validate contact email if provided
            if (!string.IsNullOrEmpty(model.ContactEmail))
            {
                if (model.ContactEmail.Length > ChargingStationConstants.MaxContactEmailLength)
                    errors.Add(StationValidationError.ContactEmailTooLong);
                else if (!IsValidEmail(model.ContactEmail))
                    errors.Add(StationValidationError.InvalidContactEmail);
            }

            if (!string.IsNullOrEmpty(model.Type))
            {
                if (!IsValidStationType(model.Type))
                    errors.Add(StationValidationError.InvalidType);
            }

            if (model.TotalSlots.HasValue)
            {
                if (model.TotalSlots.Value <= 0)
                    errors.Add(StationValidationError.TotalSlotsMustBePositive);
                else if (model.TotalSlots.Value > ChargingStationConstants.MaxTotalSlots)
                    errors.Add(StationValidationError.TotalSlotsExceedsMaximum);
            }

            return errors.Any() ? StationValidationResult.Failed(errors) : StationValidationResult.Success();
        }

        // Validates location coordinates for longitude and latitude ranges
        // Validates location coordinates for longitude and latitude ranges
        public static List<StationValidationError> ValidateLocationCoordinates(LocationCoordinates coordinates)
        {
            var errors = new List<StationValidationError>();

            if (coordinates.Longitude < ChargingStationConstants.MinLongitude || 
                coordinates.Longitude > ChargingStationConstants.MaxLongitude)
            {
                errors.Add(StationValidationError.InvalidLongitude);
            }

            if (coordinates.Latitude < ChargingStationConstants.MinLatitude || 
                coordinates.Latitude > ChargingStationConstants.MaxLatitude)
            {
                errors.Add(StationValidationError.InvalidLatitude);
            }

            return errors;
        }

        // Validates if the station type is supported
        // Validates if the station type is supported
        public static bool IsValidStationType(string type)
        {
            return ChargingStationConstants.ValidStationTypes.Contains(type, StringComparer.OrdinalIgnoreCase);
        }

        // Validates email format using regex pattern
        // Validates email format using regex pattern
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Use regex pattern for email validation
                var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                return Regex.IsMatch(email, emailPattern, RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        // Calculates new available slots when total slots change
        // Calculates new available slots when total slots change
        public static int CalculateNewAvailableSlots(int currentAvailable, int currentTotal, int newTotal)
        {
            var usedSlots = currentTotal - currentAvailable;
            var newAvailable = newTotal - Math.Min(usedSlots, newTotal);
            return Math.Max(0, Math.Min(newTotal, newAvailable));
        }

        // Converts string station type to enum
        // Converts string station type to enum
        public static StationType GetStationTypeEnum(string type)
        {
            switch (type?.ToUpper())
            {
                case "AC":
                    return StationType.AC;
                case "DC":
                    return StationType.DC;
                default:
                    return StationType.AC;
            }
        }

        // Converts station type enum to string
        // Converts station type enum to string
        public static string GetStationTypeString(StationType type)
        {
            switch (type)
            {
                case StationType.AC:
                    return "AC";
                case StationType.DC:
                    return "DC";
                default:
                    return "AC";
            }
        }

        // Sanitizes station name by trimming whitespace
        // Sanitizes station name by trimming whitespace
        public static string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return name.Trim();
        }

        // Sanitizes address by trimming whitespace
        public static string SanitizeAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return null;

            return address.Trim();
        }

        // Sanitizes city by trimming whitespace
        public static string SanitizeCity(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
                return null;

            return city.Trim();
        }

        // Sanitizes province by trimming whitespace
        public static string SanitizeProvince(string province)
        {
            if (string.IsNullOrWhiteSpace(province))
                return null;

            return province.Trim();
        }

        // Sanitizes contact phone by trimming whitespace
        public static string SanitizeContactPhone(string contactPhone)
        {
            if (string.IsNullOrWhiteSpace(contactPhone))
                return null;

            return contactPhone.Trim();
        }

        // Sanitizes contact email by trimming and converting to lowercase
        public static string SanitizeContactEmail(string contactEmail)
        {
            if (string.IsNullOrWhiteSpace(contactEmail))
                return null;

            return contactEmail.Trim().ToLowerInvariant();
        }

        // Sanitizes location coordinates and ensures they are within valid ranges
        // Sanitizes location coordinates and ensures they are within valid ranges
        public static LocationCoordinates SanitizeLocation(LocationCoordinates location)
        {
            if (location == null)
                return null;

            // Ensure coordinates are within valid ranges
            var longitude = Math.Max(ChargingStationConstants.MinLongitude, 
                             Math.Min(ChargingStationConstants.MaxLongitude, location.Longitude));
            var latitude = Math.Max(ChargingStationConstants.MinLatitude, 
                            Math.Min(ChargingStationConstants.MaxLatitude, location.Latitude));

            return new LocationCoordinates(longitude, latitude);
        }

        // Sanitizes station type by trimming whitespace
        public static string SanitizeType(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return null;

            return type.Trim();
        }

        // Creates station user profile object
        public static object CreateStationUserProfile(User user)
        {
            return new
            {
                user.Id,
                user.Username,
                user.Email,
                user.FirstName,
                user.LastName,
                user.RoleId,
                RoleName = AuthUtils.GetRoleName(user.RoleId),
                user.IsActive,
                user.CreatedAt,
                user.UpdatedAt
            };
        }

        // Creates detailed station response with station users
        public static object CreateDetailedStationResponse(ChargingStation station, List<User> stationUsers)
        {
            var userProfiles = stationUsers?.Select(CreateStationUserProfile).ToList() ?? new List<object>();
            
            return new
            {
                Station = station,
                StationUsers = userProfiles
            };
        }

        // Gets valid station types array
        public static string[] GetValidStationTypes()
        {
            return ChargingStationConstants.ValidStationTypes;
        }

        // Calculates distance between two coordinates using Haversine formula
        // Calculates distance between two coordinates using Haversine formula
        public static double CalculateDistance(LocationCoordinates coord1, LocationCoordinates coord2)
        {
            const double EarthRadius = 6371; // Earth's radius in kilometers

            var lat1Rad = ToRadians(coord1.Latitude);
            var lat2Rad = ToRadians(coord2.Latitude);
            var deltaLatRad = ToRadians(coord2.Latitude - coord1.Latitude);
            var deltaLonRad = ToRadians(coord2.Longitude - coord1.Longitude);

            var a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadius * c;
        }

        // Converts degrees to radians for distance calculations
        // Converts degrees to radians for distance calculations
        private static double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }
    }
}