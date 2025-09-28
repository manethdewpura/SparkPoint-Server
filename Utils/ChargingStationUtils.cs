using System;
using System.Collections.Generic;
using System.Linq;
using SparkPoint_Server.Constants;
using SparkPoint_Server.Enums;
using SparkPoint_Server.Models;

namespace SparkPoint_Server.Utils
{
    public static class ChargingStationUtils
    {
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

        public static bool IsValidStationType(string type)
        {
            return ChargingStationConstants.ValidStationTypes.Contains(type, StringComparer.OrdinalIgnoreCase);
        }

        public static int CalculateNewAvailableSlots(int currentAvailable, int currentTotal, int newTotal)
        {
            var usedSlots = currentTotal - currentAvailable;
            var newAvailable = newTotal - Math.Min(usedSlots, newTotal);
            return Math.Max(0, Math.Min(newTotal, newAvailable));
        }

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

        public static string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return name.Trim();
        }

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

        public static string SanitizeType(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return null;

            return type.Trim();
        }

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

        public static object CreateDetailedStationResponse(ChargingStation station, List<User> stationUsers)
        {
            var userProfiles = stationUsers?.Select(CreateStationUserProfile).ToList() ?? new List<object>();
            
            return new
            {
                Station = station,
                StationUsers = userProfiles
            };
        }

        public static string[] GetValidStationTypes()
        {
            return ChargingStationConstants.ValidStationTypes;
        }

        // Helper method to calculate distance between two coordinates (optional, for future use)
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

        private static double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }
    }
}