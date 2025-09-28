using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using SparkPoint_Server.Models;
using SparkPoint_Server.Helpers;
using SparkPoint_Server.Utils;
using SparkPoint_Server.Constants;
using SparkPoint_Server.Enums;

namespace SparkPoint_Server.Services
{

    public class ChargingStationService
    {
        private readonly IMongoCollection<ChargingStation> _stationsCollection;
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<Booking> _bookingsCollection;

        public ChargingStationService()
        {
            var dbContext = new MongoDbContext();
            _stationsCollection = dbContext.GetCollection<ChargingStation>(ChargingStationConstants.ChargingStationsCollection);
            _usersCollection = dbContext.GetCollection<User>(AuthConstants.UsersCollection);
            _bookingsCollection = dbContext.GetCollection<Booking>(ChargingStationConstants.BookingsCollection);
        }

        public StationOperationResult CreateStation(StationCreateModel model)
        {
            // Validate the model
            var validationResult = ChargingStationUtils.ValidateCreateModel(model);
            if (!validationResult.IsValid)
            {
                return StationOperationResult.Failed(StationOperationStatus.ValidationFailed, validationResult.ErrorMessage);
            }

            try
            {
                // Create the station
                var station = new ChargingStation
                {
                    Name = ChargingStationUtils.SanitizeName(model.Name),
                    Location = ChargingStationUtils.SanitizeLocation(model.Location),
                    Type = ChargingStationUtils.SanitizeType(model.Type),
                    TotalSlots = model.TotalSlots,
                    AvailableSlots = model.TotalSlots,
                    IsActive = ChargingStationConstants.DefaultIsActiveStatus,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _stationsCollection.InsertOne(station);

                return StationOperationResult.Success(
                    ChargingStationConstants.StationCreatedSuccessfully,
                    new { StationId = station.Id }
                );
            }
            catch (Exception ex)
            {
                return StationOperationResult.Failed(StationOperationStatus.Failed, ex.Message);
            }
        }

        public StationQueryResult GetStations(StationFilterModel filter = null)
        {
            try
            {
                var filterDefinition = ChargingStationFilterHelper.BuildStationFilter(filter);
                var stations = _stationsCollection.Find(filterDefinition).ToList();

                return StationQueryResult.Success(stations);
            }
            catch (Exception ex)
            {
                return StationQueryResult.Failed(ex.Message);
            }
        }

        public StationRetrievalResult GetStation(string stationId)
        {
            try
            {
                var station = _stationsCollection.Find(s => s.Id == stationId).FirstOrDefault();
                if (station == null)
                {
                    return StationRetrievalResult.Failed(ChargingStationConstants.StationNotFound);
                }

                var stationUsersFilter = ChargingStationFilterHelper.BuildStationUsersFilter(stationId);
                var stationUsers = _usersCollection.Find(stationUsersFilter).ToList();

                var userProfiles = stationUsers.Select(ChargingStationUtils.CreateStationUserProfile).ToList();
                
                return StationRetrievalResult.Success(station, userProfiles);
            }
            catch (Exception ex)
            {
                return StationRetrievalResult.Failed(ex.Message);
            }
        }

        public StationOperationResult UpdateStation(string stationId, StationUpdateModel model)
        {
            // Validate the model
            var validationResult = ChargingStationUtils.ValidateUpdateModel(model);
            if (!validationResult.IsValid)
            {
                return StationOperationResult.Failed(StationOperationStatus.ValidationFailed, validationResult.ErrorMessage);
            }

            try
            {
                var station = _stationsCollection.Find(s => s.Id == stationId).FirstOrDefault();
                if (station == null)
                {
                    return StationOperationResult.Failed(StationOperationStatus.StationNotFound);
                }

                var updateDefinition = ChargingStationUpdateHelper.BuildStationUpdate(model, station);
                _stationsCollection.UpdateOne(s => s.Id == stationId, updateDefinition);

                return StationOperationResult.Success(ChargingStationConstants.StationUpdatedSuccessfully);
            }
            catch (Exception ex)
            {
                return StationOperationResult.Failed(StationOperationStatus.Failed, ex.Message);
            }
        }

        public StationOperationResult ActivateStation(string stationId)
        {
            try
            {
                var station = _stationsCollection.Find(s => s.Id == stationId).FirstOrDefault();
                if (station == null)
                {
                    return StationOperationResult.Failed(StationOperationStatus.StationNotFound);
                }

                if (station.IsActive)
                {
                    return StationOperationResult.Failed(
                        StationOperationStatus.AlreadyInState,
                        ChargingStationConstants.StationAlreadyActive
                    );
                }

                var updateDefinition = ChargingStationUpdateHelper.BuildActivationUpdate();
                _stationsCollection.UpdateOne(s => s.Id == stationId, updateDefinition);

                return StationOperationResult.Success(ChargingStationConstants.StationActivatedSuccessfully);
            }
            catch (Exception ex)
            {
                return StationOperationResult.Failed(StationOperationStatus.Failed, ex.Message);
            }
        }

        public StationOperationResult DeactivateStation(string stationId)
        {
            try
            {
                var station = _stationsCollection.Find(s => s.Id == stationId).FirstOrDefault();
                if (station == null)
                {
                    return StationOperationResult.Failed(StationOperationStatus.StationNotFound);
                }

                if (!station.IsActive)
                {
                    return StationOperationResult.Failed(
                        StationOperationStatus.AlreadyInState,
                        ChargingStationConstants.StationAlreadyDeactivated
                    );
                }

                // Check for active bookings
                var activeBookingsFilter = ChargingStationFilterHelper.BuildActiveBookingsFilter(stationId);
                var activeBookingsCount = _bookingsCollection.CountDocuments(activeBookingsFilter);
                
                if (activeBookingsCount > 0)
                {
                    return StationOperationResult.Failed(
                        StationOperationStatus.HasActiveBookings,
                        ChargingStationConstants.CannotDeactivateWithActiveBookings
                    );
                }

                var updateDefinition = ChargingStationUpdateHelper.BuildDeactivationUpdate();
                _stationsCollection.UpdateOne(s => s.Id == stationId, updateDefinition);

                return StationOperationResult.Success(ChargingStationConstants.StationDeactivatedSuccessfully);
            }
            catch (Exception ex)
            {
                return StationOperationResult.Failed(StationOperationStatus.Failed, ex.Message);
            }
        }

        public StationQueryResult GetStationsSorted(StationFilterModel filter, StationSortField sortField, SortOrder sortOrder)
        {
            try
            {
                var filterDefinition = ChargingStationFilterHelper.BuildStationFilter(filter);
                var sortDefinition = ChargingStationFilterHelper.BuildStationSort(sortField, sortOrder);
                
                var stations = _stationsCollection
                    .Find(filterDefinition)
                    .Sort(sortDefinition)
                    .ToList();

                return StationQueryResult.Success(stations);
            }
            catch (Exception ex)
            {
                return StationQueryResult.Failed(ex.Message);
            }
        }

        public StationQueryResult GetStationsPaginated(StationFilterModel filter, int pageNumber, int pageSize)
        {
            try
            {
                var filterDefinition = ChargingStationFilterHelper.BuildStationFilter(filter);
                
                var totalCount = _stationsCollection.CountDocuments(filterDefinition);
                var skip = (pageNumber - 1) * pageSize;
                
                var stations = _stationsCollection
                    .Find(filterDefinition)
                    .Skip(skip)
                    .Limit(pageSize)
                    .ToList();

                var paginationInfo = new
                {
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return StationQueryResult.Success(stations);
            }
            catch (Exception ex)
            {
                return StationQueryResult.Failed(ex.Message);
            }
        }

        public bool IsStationActiveAndExists(string stationId)
        {
            try
            {
                var station = _stationsCollection.Find(s => s.Id == stationId && s.IsActive).FirstOrDefault();
                return station != null;
            }
            catch
            {
                return false;
            }
        }

        public object GetStationStatistics(string stationId)
        {
            try
            {
                var station = _stationsCollection.Find(s => s.Id == stationId).FirstOrDefault();
                if (station == null)
                    return null;

                var totalBookings = _bookingsCollection.CountDocuments(b => b.StationId == stationId);
                var activeBookings = _bookingsCollection.CountDocuments(b => 
                    b.StationId == stationId && 
                    BookingStatusConstants.IsSlotReservingStatus(b.Status));

                var utilization = station.TotalSlots > 0 
                    ? (double)(station.TotalSlots - station.AvailableSlots) / station.TotalSlots * 100
                    : 0;

                return new
                {
                    StationId = stationId,
                    TotalSlots = station.TotalSlots,
                    AvailableSlots = station.AvailableSlots,
                    UtilizationPercentage = Math.Round(utilization, 2),
                    TotalBookings = totalBookings,
                    ActiveBookings = activeBookings,
                    IsActive = station.IsActive
                };
            }
            catch
            {
                return null;
            }
        }
    }
}