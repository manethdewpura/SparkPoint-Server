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
    /// <summary>
    /// Service class for charging station operations
    /// </summary>
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

        /// <summary>
        /// Creates a new charging station
        /// </summary>
        /// <param name="model">The station creation model</param>
        /// <returns>Result of the creation operation</returns>
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

        /// <summary>
        /// Gets all charging stations with optional filtering
        /// </summary>
        /// <param name="filter">Optional filter criteria</param>
        /// <returns>Result of the query operation</returns>
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

        /// <summary>
        /// Gets a single charging station with its users
        /// </summary>
        /// <param name="stationId">The station ID</param>
        /// <returns>Result of the retrieval operation</returns>
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

        /// <summary>
        /// Updates a charging station
        /// </summary>
        /// <param name="stationId">The station ID</param>
        /// <param name="model">The update model</param>
        /// <returns>Result of the update operation</returns>
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

        /// <summary>
        /// Activates a charging station
        /// </summary>
        /// <param name="stationId">The station ID</param>
        /// <returns>Result of the activation operation</returns>
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

        /// <summary>
        /// Deactivates a charging station
        /// </summary>
        /// <param name="stationId">The station ID</param>
        /// <returns>Result of the deactivation operation</returns>
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

        /// <summary>
        /// Gets stations with sorting
        /// </summary>
        /// <param name="filter">Optional filter criteria</param>
        /// <param name="sortField">Field to sort by</param>
        /// <param name="sortOrder">Sort order</param>
        /// <returns>Result of the query operation</returns>
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

        /// <summary>
        /// Gets stations with pagination
        /// </summary>
        /// <param name="filter">Optional filter criteria</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Result of the query operation with pagination info</returns>
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

        /// <summary>
        /// Checks if a station exists and is active
        /// </summary>
        /// <param name="stationId">The station ID</param>
        /// <returns>True if exists and is active, false otherwise</returns>
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

        /// <summary>
        /// Gets station statistics
        /// </summary>
        /// <param name="stationId">The station ID</param>
        /// <returns>Station statistics object</returns>
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