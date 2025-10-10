/*
 * UserService.cs
 * 
 * This service handles all business logic related to user management operations.
 * It manages user registration, profile updates, user listing, and account
 * activation/deactivation with proper validation and authorization checks.
 * All operations interact with MongoDB collections for data persistence.
 * 
 */

using System;
using System.Linq;
using System.Collections.Generic;
using MongoDB.Driver;
using SparkPoint_Server.Models;
using SparkPoint_Server.Helpers;
using SparkPoint_Server.Utils;
using SparkPoint_Server.Constants;
using SparkPoint_Server.Enums;

namespace SparkPoint_Server.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<ChargingStation> _stationsCollection;

        // Constructor: Initializes MongoDB collections
        public UserService()
        {
            var dbContext = new MongoDbContext();
            _usersCollection = dbContext.GetCollection<User>(AuthConstants.UsersCollection);
            _stationsCollection = dbContext.GetCollection<ChargingStation>(ChargingStationConstants.ChargingStationsCollection);
        }

        // Registers a new admin user
        public UserOperationResult RegisterAdmin(RegisterModel model)
        {
            var validationResult = UserUtils.ValidateRegisterModel(model);
            if (!validationResult.IsValid)
            {
                return UserOperationResult.Failed(UserOperationStatus.ValidationFailed, validationResult.ErrorMessage);
            }

            try
            {
                if (_usersCollection.Find(u => u.Username == model.Username || u.Email == model.Email).Any())
                {
                    return UserOperationResult.Failed(UserOperationStatus.UsernameExists, UserConstants.UsernameOrEmailExists);
                }

                var adminUser = new User
                {
                    Username = UserUtils.SanitizeString(model.Username),
                    Email = UserUtils.SanitizeString(model.Email),
                    FirstName = UserUtils.SanitizeString(model.FirstName),
                    LastName = UserUtils.SanitizeString(model.LastName),
                    PasswordHash = PasswordUtils.HashPassword(model.Password),
                    RoleId = ApplicationConstants.AdminRoleId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _usersCollection.InsertOne(adminUser);

                return UserOperationResult.Success(
                    UserConstants.AdminRegistrationSuccessful,
                    new { UserId = adminUser.Id }
                );
            }
            catch (Exception ex)
            {
                return UserOperationResult.Failed(UserOperationStatus.Failed, ex.Message);
            }
        }

        // Updates user profile
        public UserOperationResult UpdateProfile(string userId, UserUpdateModel model)
        {
            var validationResult = UserUtils.ValidateUserUpdateModel(model);
            if (!validationResult.IsValid)
            {
                return UserOperationResult.Failed(UserOperationStatus.ValidationFailed, validationResult.ErrorMessage);
            }

            try
            {
                var currentUser = _usersCollection.Find(u => u.Id == userId).FirstOrDefault();
                if (currentUser == null)
                {
                    return UserOperationResult.Failed(UserOperationStatus.UserNotFound);
                }

                if (!currentUser.IsActive)
                {
                    return UserOperationResult.Failed(UserOperationStatus.AccountDeactivated);
                }

                if (!string.IsNullOrEmpty(model.Email) && model.Email != currentUser.Email)
                {
                    if (_usersCollection.Find(u => u.Email == model.Email && u.Id != userId).Any())
                    {
                        return UserOperationResult.Failed(UserOperationStatus.EmailExists);
                    }
                }

                if (!string.IsNullOrEmpty(model.Username) && model.Username != currentUser.Username)
                {
                    if (_usersCollection.Find(u => u.Username == model.Username && u.Id != userId).Any())
                    {
                        return UserOperationResult.Failed(UserOperationStatus.UsernameExists);
                    }
                }

                var updateDefinition = UserUpdateHelper.BuildUserUpdate(model);
                _usersCollection.UpdateOne(u => u.Id == userId, updateDefinition);

                return UserOperationResult.Success(UserConstants.ProfileUpdatedSuccessfully);
            }
            catch (Exception ex)
            {
                return UserOperationResult.Failed(UserOperationStatus.Failed, ex.Message);
            }
        }

        // Gets user profile by user ID
        public UserRetrievalResult GetProfile(string userId)
        {
            try
            {
                var currentUser = _usersCollection.Find(u => u.Id == userId).FirstOrDefault();
                if (currentUser == null)
                {
                    return UserRetrievalResult.Failed(UserConstants.UserNotFound);
                }

                var profile = UserUtils.CreateUserProfile(currentUser);
                return UserRetrievalResult.Success(profile);
            }
            catch (Exception ex)
            {
                return UserRetrievalResult.Failed(ex.Message);
            }
        }

        // Creates a new station user
        public UserOperationResult CreateStationUser(CreateStationUserModel model)
        {
            var validationResult = UserUtils.ValidateCreateStationUserModel(model);
            if (!validationResult.IsValid)
            {
                return UserOperationResult.Failed(UserOperationStatus.ValidationFailed, validationResult.ErrorMessage);
            }

            try
            {
                if (_usersCollection.Find(u => u.Username == model.Username).Any())
                {
                    return UserOperationResult.Failed(UserOperationStatus.UsernameExists);
                }

                if (_usersCollection.Find(u => u.Email == model.Email).Any())
                {
                    return UserOperationResult.Failed(UserOperationStatus.EmailExists);
                }

                var station = _stationsCollection.Find(s => s.Id == model.ChargingStationId).FirstOrDefault();
                if (station == null)
                {
                    return UserOperationResult.Failed(UserOperationStatus.ChargingStationNotFound);
                }

                var stationUser = new User
                {
                    Username = UserUtils.SanitizeString(model.Username),
                    Email = UserUtils.SanitizeString(model.Email),
                    FirstName = UserUtils.SanitizeString(model.FirstName),
                    LastName = UserUtils.SanitizeString(model.LastName),
                    RoleId = ApplicationConstants.StationUserRoleId,
                    PasswordHash = PasswordUtils.HashPassword(model.Password),
                    ChargingStationId = model.ChargingStationId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _usersCollection.InsertOne(stationUser);

                return UserOperationResult.Success(
                    UserConstants.StationUserCreatedSuccessfully,
                    new { UserId = stationUser.Id }
                );
            }
            catch (Exception ex)
            {
                return UserOperationResult.Failed(UserOperationStatus.Failed, ex.Message);
            }
        }

        // Gets station user profile by user ID
        public UserRetrievalResult GetStationUserProfile(string userId)
        {
            try
            {
                var stationUser = _usersCollection.Find(u => u.Id == userId).FirstOrDefault();
                if (stationUser == null)
                {
                    return UserRetrievalResult.Failed(UserConstants.UserNotFound);
                }

                if (stationUser.RoleId != ApplicationConstants.StationUserRoleId)
                {
                    return UserRetrievalResult.Failed(UserConstants.NotStationUser);
                }

                ChargingStation stationDetails = null;
                if (!string.IsNullOrEmpty(stationUser.ChargingStationId))
                {
                    stationDetails = _stationsCollection.Find(s => s.Id == stationUser.ChargingStationId).FirstOrDefault();
                }

                var profile = UserUtils.CreateStationUserProfileWithStation(stationUser, stationDetails);

                return UserRetrievalResult.Success(profile);
            }
            catch (Exception ex)
            {
                return UserRetrievalResult.Failed(ex.Message);
            }
        }

        // Updates station user profile
        public UserOperationResult UpdateStationUser(string userId, UserUpdateModel model)
        {
            var validationResult = UserUtils.ValidateUserUpdateModel(model);
            if (!validationResult.IsValid)
            {
                return UserOperationResult.Failed(UserOperationStatus.ValidationFailed, validationResult.ErrorMessage);
            }

            try
            {
                var stationUser = _usersCollection.Find(u => u.Id == userId).FirstOrDefault();
                if (stationUser == null)
                {
                    return UserOperationResult.Failed(UserOperationStatus.UserNotFound);
                }

                if (stationUser.RoleId != ApplicationConstants.StationUserRoleId)
                {
                    return UserOperationResult.Failed(UserOperationStatus.NotStationUser, UserConstants.NotStationUser);
                }

                if (!string.IsNullOrEmpty(model.Email) && model.Email != stationUser.Email)
                {
                    if (_usersCollection.Find(u => u.Email == model.Email && u.Id != userId).Any())
                    {
                        return UserOperationResult.Failed(UserOperationStatus.EmailExists);
                    }
                }

                if (!string.IsNullOrEmpty(model.Username) && model.Username != stationUser.Username)
                {
                    if (_usersCollection.Find(u => u.Username == model.Username && u.Id != userId).Any())
                    {
                        return UserOperationResult.Failed(UserOperationStatus.UsernameExists);
                    }
                }

                var updateDefinition = UserUpdateHelper.BuildUserUpdate(model);
                _usersCollection.UpdateOne(u => u.Id == userId, updateDefinition);

                return UserOperationResult.Success(UserConstants.StationUserUpdatedSuccessfully);
            }
            catch (Exception ex)
            {
                return UserOperationResult.Failed(UserOperationStatus.Failed, ex.Message);
            }
        }

        // Checks if user account is active
        public bool IsUserActive(string userId)
        {
            try
            {
                var user = _usersCollection.Find(u => u.Id == userId && u.IsActive).FirstOrDefault();
                return user != null;
            }
            catch
            {
                return false;
            }
        }

        // Checks if user has specific role
        public bool CheckUserRole(string userId, int expectedRoleId)
        {
            try
            {
                var user = _usersCollection.Find(u => u.Id == userId && u.RoleId == expectedRoleId).FirstOrDefault();
                return user != null;
            }
            catch
            {
                return false;
            }
        }

        // Gets all station users with optional filtering
        public UserRetrievalResult GetAllStationUsers(UserListFilterModel filter = null)
        {
            try
            {
                // Build the base filter for station users only
                var stationUserFilter = Builders<User>.Filter.Eq(u => u.RoleId, ApplicationConstants.StationUserRoleId);
                
                // Apply additional filters if provided
                var combinedFilter = stationUserFilter;
                if (filter != null)
                {
                    var additionalFilter = UserFilterHelper.BuildUserFilter(filter);
                    combinedFilter = Builders<User>.Filter.And(stationUserFilter, additionalFilter);
                }

                var stationUsers = _usersCollection.Find(combinedFilter).ToList();

                var userProfiles = stationUsers.Select(UserUtils.CreateStationUserProfile).ToList();
                
                return UserRetrievalResult.Success(userProfiles);
            }
            catch (Exception ex)
            {
                return UserRetrievalResult.Failed(ex.Message);
            }
        }
    }
}