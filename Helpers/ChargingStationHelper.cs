/*
 * ChargingStationHelper.cs
 * 
 * This helper class provides charging station-related MongoDB operations.
 * It includes filter building for station queries, sorting definitions,
 * and update builders for charging station operations.
 * 
 */

using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using SparkPoint_Server.Models;
using SparkPoint_Server.Constants;
using SparkPoint_Server.Enums;

namespace SparkPoint_Server.Helpers
{
    public static class ChargingStationFilterHelper
    {
        // Builds MongoDB filter for charging station queries based on provided filter criteria
        public static FilterDefinition<ChargingStation> BuildStationFilter(StationFilterModel filter)
        {
            var filterBuilder = Builders<ChargingStation>.Filter.Empty;

            if (filter == null)
                return filterBuilder;

            if (filter.IsActive.HasValue)
            {
                var activeFilter = Builders<ChargingStation>.Filter.Eq(s => s.IsActive, filter.IsActive.Value);
                filterBuilder = Builders<ChargingStation>.Filter.And(filterBuilder, activeFilter);
            }

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchFilter = BuildSearchFilter(filter.SearchTerm);
                filterBuilder = Builders<ChargingStation>.Filter.And(filterBuilder, searchFilter);
            }

            // Add location-based filtering if specified
            if (filter.NearLocation != null && filter.MaxDistanceKm.HasValue)
            {
                var locationFilter = BuildLocationFilter(filter.NearLocation, filter.MaxDistanceKm.Value);
                filterBuilder = Builders<ChargingStation>.Filter.And(filterBuilder, locationFilter);
            }

            return filterBuilder;
        }

        // Builds search filter for station queries using regex pattern matching
        private static FilterDefinition<ChargingStation> BuildSearchFilter(string searchTerm)
        {
            // Search in station name, type, address, city, and province
            var nameFilter = Builders<ChargingStation>.Filter.Regex(
                s => s.Name, 
                new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")
            );

            var typeFilter = Builders<ChargingStation>.Filter.Regex(
                s => s.Type, 
                new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")
            );

            var addressFilter = Builders<ChargingStation>.Filter.Regex(
                s => s.Address, 
                new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")
            );

            var cityFilter = Builders<ChargingStation>.Filter.Regex(
                s => s.City, 
                new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")
            );

            var provinceFilter = Builders<ChargingStation>.Filter.Regex(
                s => s.Province, 
                new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")
            );

            return Builders<ChargingStation>.Filter.Or(nameFilter, typeFilter, addressFilter, cityFilter, provinceFilter);
        }

        // Builds location-based filter for nearby station searches
        private static FilterDefinition<ChargingStation> BuildLocationFilter(LocationCoordinates center, double maxDistanceKm)
        {
            // Simple bounding box filter for location-based search
            // This is a simplified version - for production, you'd want proper geospatial indexing
            var latDelta = maxDistanceKm / 111.0; // Rough approximation: 1 degree latitude ? 111 km
            var lonDelta = maxDistanceKm / (111.0 * System.Math.Cos(center.Latitude * System.Math.PI / 180.0));

            var minLat = center.Latitude - latDelta;
            var maxLat = center.Latitude + latDelta;
            var minLon = center.Longitude - lonDelta;
            var maxLon = center.Longitude + lonDelta;

            return Builders<ChargingStation>.Filter.And(
                Builders<ChargingStation>.Filter.Gte(s => s.Location.Latitude, minLat),
                Builders<ChargingStation>.Filter.Lte(s => s.Location.Latitude, maxLat),
                Builders<ChargingStation>.Filter.Gte(s => s.Location.Longitude, minLon),
                Builders<ChargingStation>.Filter.Lte(s => s.Location.Longitude, maxLon)
            );
        }

        // Builds MongoDB sort definition for charging station queries
        public static SortDefinition<ChargingStation> BuildStationSort(StationSortField sortField, SortOrder sortOrder)
        {
            var sortBuilder = Builders<ChargingStation>.Sort;

            SortDefinition<ChargingStation> sortDefinition;

            switch (sortField)
            {
                case StationSortField.Name:
                    sortDefinition = sortOrder == SortOrder.Ascending
                        ? sortBuilder.Ascending(s => s.Name)
                        : sortBuilder.Descending(s => s.Name);
                    break;
                case StationSortField.Type:
                    sortDefinition = sortOrder == SortOrder.Ascending
                        ? sortBuilder.Ascending(s => s.Type)
                        : sortBuilder.Descending(s => s.Type);
                    break;
                case StationSortField.TotalSlots:
                    sortDefinition = sortOrder == SortOrder.Ascending
                        ? sortBuilder.Ascending(s => s.TotalSlots)
                        : sortBuilder.Descending(s => s.TotalSlots);
                    break;
                case StationSortField.AvailableSlots:
                    sortDefinition = sortOrder == SortOrder.Ascending
                        ? sortBuilder.Ascending(s => s.AvailableSlots)
                        : sortBuilder.Descending(s => s.AvailableSlots);
                    break;
                case StationSortField.IsActive:
                    sortDefinition = sortOrder == SortOrder.Ascending
                        ? sortBuilder.Ascending(s => s.IsActive)
                        : sortBuilder.Descending(s => s.IsActive);
                    break;
                case StationSortField.CreatedAt:
                    sortDefinition = sortOrder == SortOrder.Ascending
                        ? sortBuilder.Ascending(s => s.CreatedAt)
                        : sortBuilder.Descending(s => s.CreatedAt);
                    break;
                case StationSortField.UpdatedAt:
                    sortDefinition = sortOrder == SortOrder.Ascending
                        ? sortBuilder.Ascending(s => s.UpdatedAt)
                        : sortBuilder.Descending(s => s.UpdatedAt);
                    break;
                case StationSortField.Location:
                    // For coordinate sorting, we'll default to created date
                    sortDefinition = sortBuilder.Descending(s => s.CreatedAt);
                    break;
                default:
                    sortDefinition = sortBuilder.Descending(s => s.CreatedAt);
                    break;
            }

            return sortDefinition;
        }

        public static FilterDefinition<Booking> BuildActiveBookingsFilter(string stationId)
        {
            return Builders<Booking>.Filter.And(
                Builders<Booking>.Filter.Eq(b => b.StationId, stationId),
                Builders<Booking>.Filter.In(b => b.Status, BookingStatusConstants.SlotReservingStatuses)
            );
        }

        public static FilterDefinition<User> BuildStationUsersFilter(string stationId)
        {
            return Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.ChargingStationId, stationId),
                Builders<User>.Filter.Eq(u => u.RoleId, ApplicationConstants.StationUserRoleId)
            );
        }
    }

    public static class ChargingStationUpdateHelper
    {
        // Builds MongoDB update definition for charging station updates
        public static UpdateDefinition<ChargingStation> BuildStationUpdate(StationUpdateModel model, ChargingStation currentStation)
        {
            var updateBuilder = Builders<ChargingStation>.Update.Set(s => s.UpdatedAt, System.DateTime.UtcNow);

            if (!string.IsNullOrEmpty(model.Name))
            {
                var sanitizedName = Utils.ChargingStationUtils.SanitizeName(model.Name);
                updateBuilder = updateBuilder.Set(s => s.Name, sanitizedName);
            }

            if (model.Location != null)
            {
                var sanitizedLocation = Utils.ChargingStationUtils.SanitizeLocation(model.Location);
                updateBuilder = updateBuilder.Set(s => s.Location, sanitizedLocation);
            }

            if (!string.IsNullOrEmpty(model.Address))
            {
                var sanitizedAddress = Utils.ChargingStationUtils.SanitizeAddress(model.Address);
                updateBuilder = updateBuilder.Set(s => s.Address, sanitizedAddress);
            }

            if (!string.IsNullOrEmpty(model.City))
            {
                var sanitizedCity = Utils.ChargingStationUtils.SanitizeCity(model.City);
                updateBuilder = updateBuilder.Set(s => s.City, sanitizedCity);
            }

            if (!string.IsNullOrEmpty(model.Province))
            {
                var sanitizedProvince = Utils.ChargingStationUtils.SanitizeProvince(model.Province);
                updateBuilder = updateBuilder.Set(s => s.Province, sanitizedProvince);
            }

            if (!string.IsNullOrEmpty(model.ContactPhone))
            {
                var sanitizedContactPhone = Utils.ChargingStationUtils.SanitizeContactPhone(model.ContactPhone);
                updateBuilder = updateBuilder.Set(s => s.ContactPhone, sanitizedContactPhone);
            }

            if (!string.IsNullOrEmpty(model.ContactEmail))
            {
                var sanitizedContactEmail = Utils.ChargingStationUtils.SanitizeContactEmail(model.ContactEmail);
                updateBuilder = updateBuilder.Set(s => s.ContactEmail, sanitizedContactEmail);
            }

            if (!string.IsNullOrEmpty(model.Type))
            {
                var sanitizedType = Utils.ChargingStationUtils.SanitizeType(model.Type);
                updateBuilder = updateBuilder.Set(s => s.Type, sanitizedType);
            }

            if (model.TotalSlots.HasValue && model.TotalSlots.Value > 0)
            {
                var newAvailable = Utils.ChargingStationUtils.CalculateNewAvailableSlots(
                    currentStation.AvailableSlots,
                    currentStation.TotalSlots,
                    model.TotalSlots.Value
                );

                updateBuilder = updateBuilder.Set(s => s.TotalSlots, model.TotalSlots.Value);
                updateBuilder = updateBuilder.Set(s => s.AvailableSlots, newAvailable);
            }

            return updateBuilder;
        }

        // Builds MongoDB update definition for station activation
        public static UpdateDefinition<ChargingStation> BuildActivationUpdate()
        {
            return Builders<ChargingStation>.Update
                .Set(s => s.IsActive, true)
                .Set(s => s.UpdatedAt, System.DateTime.UtcNow);
        }

        // Builds MongoDB update definition for station deactivation
        public static UpdateDefinition<ChargingStation> BuildDeactivationUpdate()
        {
            return Builders<ChargingStation>.Update
                .Set(s => s.IsActive, false)
                .Set(s => s.UpdatedAt, System.DateTime.UtcNow);
        }
    }
}