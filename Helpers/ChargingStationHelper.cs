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

            return filterBuilder;
        }
        private static FilterDefinition<ChargingStation> BuildSearchFilter(string searchTerm)
        {
            var locationFilter = Builders<ChargingStation>.Filter.Regex(
                s => s.Location, 
                new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")
            );

            var typeFilter = Builders<ChargingStation>.Filter.Regex(
                s => s.Type, 
                new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")
            );

            return Builders<ChargingStation>.Filter.Or(locationFilter, typeFilter);
        }

        public static SortDefinition<ChargingStation> BuildStationSort(StationSortField sortField, SortOrder sortOrder)
        {
            var sortBuilder = Builders<ChargingStation>.Sort;

            SortDefinition<ChargingStation> sortDefinition;

            switch (sortField)
            {
                case StationSortField.Location:
                    sortDefinition = sortOrder == SortOrder.Ascending 
                        ? sortBuilder.Ascending(s => s.Location)
                        : sortBuilder.Descending(s => s.Location);
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
                Builders<Booking>.Filter.Not(
                    Builders<Booking>.Filter.In(b => b.Status, new[] { "Cancelled", "Completed" })
                )
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
        public static UpdateDefinition<ChargingStation> BuildStationUpdate(StationUpdateModel model, ChargingStation currentStation)
        {
            var updateBuilder = Builders<ChargingStation>.Update.Set(s => s.UpdatedAt, System.DateTime.UtcNow);

            if (!string.IsNullOrEmpty(model.Location))
            {
                var sanitizedLocation = Utils.ChargingStationUtils.SanitizeLocation(model.Location);
                updateBuilder = updateBuilder.Set(s => s.Location, sanitizedLocation);
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

        public static UpdateDefinition<ChargingStation> BuildActivationUpdate()
        {
            return Builders<ChargingStation>.Update
                .Set(s => s.IsActive, true)
                .Set(s => s.UpdatedAt, System.DateTime.UtcNow);
        }

        public static UpdateDefinition<ChargingStation> BuildDeactivationUpdate()
        {
            return Builders<ChargingStation>.Update
                .Set(s => s.IsActive, false)
                .Set(s => s.UpdatedAt, System.DateTime.UtcNow);
        }
    }
}