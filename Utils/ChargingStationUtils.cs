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

            if (string.IsNullOrEmpty(model.Location))
                errors.Add(StationValidationError.LocationRequired);
            else if (model.Location.Length > ChargingStationConstants.MaxLocationLength)
                errors.Add(StationValidationError.LocationTooLong);

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

            if (!string.IsNullOrEmpty(model.Location))
            {
                if (model.Location.Length > ChargingStationConstants.MaxLocationLength)
                    errors.Add(StationValidationError.LocationTooLong);
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

        public static string SanitizeLocation(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
                return null;

            return location.Trim();
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
    }
}