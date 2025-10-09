/*
 * EVOwnerService.cs
 * 
 * This service handles all business logic related to EV Owner operations.
 * It manages EV Owner registration, profile updates, account activation/deactivation,
 * and data retrieval. All operations interact with MongoDB collections for data persistence.
 * 
 */

using System;
using System.Linq;
using MongoDB.Driver;
using SparkPoint_Server.Models;
using SparkPoint_Server.Helpers;
using SparkPoint_Server.Utils;
using SparkPoint_Server.Constants;
using SparkPoint_Server.Enums;

namespace SparkPoint_Server.Services
{
    public class EVOwnerService
    {
        private readonly IMongoCollection<EVOwner> _evOwnersCollection;
        private readonly IMongoCollection<User> _usersCollection;

        // Constructor: Initializes MongoDB collections for EV Owners and Users
        public EVOwnerService()
        {
            var dbContext = new MongoDbContext();
            _evOwnersCollection = dbContext.GetCollection<EVOwner>(EVOwnerConstants.EVOwnersCollection);
            _usersCollection = dbContext.GetCollection<User>(AuthConstants.UsersCollection);
        }

        // Registers a new EV Owner by creating both User and EVOwner records
        public EVOwnerOperationResult Register(EVOwnerRegisterModel model)
        {
            var validationResult = UserUtils.ValidateEVOwnerRegisterModel(model);
            if (!validationResult.IsValid)
            {
                return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.ValidationFailed, validationResult.ErrorMessage);
            }

            try
            {
                if (_usersCollection.Find(u => u.Username == model.Username).Any())
                {
                    return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.UsernameExists);
                }

                if (_usersCollection.Find(u => u.Email == model.Email).Any())
                {
                    return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.EmailExists);
                }

                if (_evOwnersCollection.Find(o => o.Phone == model.Phone).Any())
                {
                    return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.PhoneExists);
                }

                if (_evOwnersCollection.Find(o => o.NIC == model.NIC).Any())
                {
                    return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.NICExists);
                }

                var user = new User
                {
                    Username = UserUtils.SanitizeString(model.Username),
                    Email = UserUtils.SanitizeString(model.Email),
                    FirstName = UserUtils.SanitizeString(model.FirstName),
                    LastName = UserUtils.SanitizeString(model.LastName),
                    PasswordHash = PasswordUtils.HashPassword(model.Password),
                    RoleId = ApplicationConstants.EVOwnerRoleId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _usersCollection.InsertOne(user);

                var evOwner = new EVOwner
                {
                    NIC = UserUtils.SanitizeString(model.NIC),
                    Phone = UserUtils.SanitizeString(model.Phone),
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _evOwnersCollection.InsertOne(evOwner);

                return EVOwnerOperationResult.Success(EVOwnerConstants.RegistrationSuccessful);
            }
            catch (Exception ex)
            {
                return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.Failed, ex.Message);
            }
        }

        // Updates the profile of an EV Owner by user ID
        public EVOwnerOperationResult UpdateProfile(string userId, EVOwnerUpdateModel model)
        {
            var validationResult = UserUtils.ValidateEVOwnerUpdateModel(model);
            if (!validationResult.IsValid)
            {
                return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.ValidationFailed, validationResult.ErrorMessage);
            }

            try
            {
                var currentUser = _usersCollection.Find(u => u.Id == userId).FirstOrDefault();
                var currentEVOwner = _evOwnersCollection.Find(o => o.UserId == userId).FirstOrDefault();

                if (currentUser == null || currentEVOwner == null)
                {
                    return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.UserNotFound);
                }

                if (!currentUser.IsActive)
                {
                    return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.AccountDeactivated);
                }

                if (!string.IsNullOrEmpty(model.Email) && model.Email != currentUser.Email)
                {
                    if (_usersCollection.Find(u => u.Email == model.Email && u.Id != userId).Any())
                    {
                        return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.EmailExists);
                    }
                }

                if (!string.IsNullOrEmpty(model.Phone) && model.Phone != currentEVOwner.Phone)
                {
                    if (_evOwnersCollection.Find(o => o.Phone == model.Phone && o.NIC != currentEVOwner.NIC).Any())
                    {
                        return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.PhoneExists);
                    }
                }

                var userUpdateBuilder = UserUpdateHelper.BuildUserUpdate(new UserUpdateModel
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    Password = model.Password
                });

                _usersCollection.UpdateOne(u => u.Id == userId, userUpdateBuilder);

                var evOwnerUpdateBuilder = UserUpdateHelper.BuildEVOwnerUpdate(model);
                _evOwnersCollection.UpdateOne(o => o.UserId == userId, evOwnerUpdateBuilder);

                return EVOwnerOperationResult.Success(EVOwnerConstants.ProfileUpdatedSuccessfully);
            }
            catch (Exception ex)
            {
                return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.Failed, ex.Message);
            }
        }

        // Deactivates an EV Owner account by user ID
        public EVOwnerOperationResult DeactivateAccount(string userId)
        {
            try
            {
                var currentUser = _usersCollection.Find(u => u.Id == userId).FirstOrDefault();
                if (currentUser == null)
                {
                    return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.UserNotFound);
                }

                if (!currentUser.IsActive)
                {
                    return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.AccountAlreadyDeactivated);
                }

                var update = UserUpdateHelper.BuildActivationUpdate(false);
                _usersCollection.UpdateOne(u => u.Id == userId, update);

                return EVOwnerOperationResult.Success(EVOwnerConstants.AccountDeactivatedSuccessfully);
            }
            catch (Exception ex)
            {
                return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.Failed, ex.Message);
            }
        }

        // Allows admin to deactivate any EV Owner account by NIC
        public EVOwnerOperationResult AdminDeactivateAccount(string nic)
        {
            try
            {
                var evOwner = _evOwnersCollection.Find(o => o.NIC == nic).FirstOrDefault();
                if (evOwner == null)
                {
                    return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.EVOwnerNotFound);
                }

                var user = _usersCollection.Find(u => u.Id == evOwner.UserId).FirstOrDefault();
                if (user == null)
                {
                    return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.UserNotFound);
                }

                if (!user.IsActive)
                {
                    return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.AccountAlreadyDeactivated);
                }

                var update = UserUpdateHelper.BuildActivationUpdate(false);
                _usersCollection.UpdateOne(u => u.Id == evOwner.UserId, update);

                return EVOwnerOperationResult.Success(EVOwnerConstants.AccountDeactivatedSuccessfully);
            }
            catch (Exception ex)
            {
                return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.Failed, ex.Message);
            }
        }

        // Reactivates a deactivated EV Owner account by NIC
        public EVOwnerOperationResult ReactivateAccount(string nic)
        {
            try
            {
                var evOwner = _evOwnersCollection.Find(o => o.NIC == nic).FirstOrDefault();
                if (evOwner == null)
                {
                    return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.EVOwnerNotFound);
                }

                var user = _usersCollection.Find(u => u.Id == evOwner.UserId).FirstOrDefault();
                if (user == null)
                {
                    return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.UserNotFound);
                }

                if (user.IsActive)
                {
                    return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.AccountAlreadyActive);
                }

                var update = UserUpdateHelper.BuildActivationUpdate(true);
                _usersCollection.UpdateOne(u => u.Id == evOwner.UserId, update);

                return EVOwnerOperationResult.Success(EVOwnerConstants.AccountReactivatedSuccessfully);
            }
            catch (Exception ex)
            {
                return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.Failed, ex.Message);
            }
        }

        // Retrieves EV Owner profile by user ID
        public UserRetrievalResult GetProfile(string userId)
        {
            try
            {
                var currentUser = _usersCollection.Find(u => u.Id == userId).FirstOrDefault();
                var currentEVOwner = _evOwnersCollection.Find(o => o.UserId == userId).FirstOrDefault();

                if (currentUser == null || currentEVOwner == null)
                {
                    return UserRetrievalResult.Failed(EVOwnerConstants.UserNotFound);
                }

                var profile = UserUtils.CreateUserProfile(currentUser, currentEVOwner);
                return UserRetrievalResult.Success(profile);
            }
            catch (Exception ex)
            {
                return UserRetrievalResult.Failed(ex.Message);
            }
        }

        // Retrieves EV Owner profile by NIC
        public UserRetrievalResult GetProfileByNIC(string nic)
        {
            try
            {
                var evOwner = _evOwnersCollection.Find(o => o.NIC == nic).FirstOrDefault();
                if (evOwner == null)
                {
                    return UserRetrievalResult.Failed(EVOwnerConstants.EVOwnerNotFound);
                }

                var user = _usersCollection.Find(u => u.Id == evOwner.UserId).FirstOrDefault();
                if (user == null)
                {
                    return UserRetrievalResult.Failed(EVOwnerConstants.UserNotFound);
                }

                var profile = UserUtils.CreateUserProfile(user, evOwner);
                return UserRetrievalResult.Success(profile);
            }
            catch (Exception ex)
            {
                return UserRetrievalResult.Failed(ex.Message);
            }
        }

        // Checks if an EV Owner account is active by NIC
        public bool IsEVOwnerActive(string nic)
        {
            try
            {
                var evOwner = _evOwnersCollection.Find(o => o.NIC == nic).FirstOrDefault();
                if (evOwner == null)
                    return false;

                var user = _usersCollection.Find(u => u.Id == evOwner.UserId && u.IsActive).FirstOrDefault();
                return user != null;
            }
            catch
            {
                return false;
            }
        }

        // Retrieves EV Owner record by user ID
        // Retrieves EV Owner record by user ID
        public EVOwner GetEVOwnerByUserId(string userId)
        {
            try
            {
                return _evOwnersCollection.Find(o => o.UserId == userId).FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        // Retrieves EV Owner record by NIC
        // Retrieves EV Owner record by NIC
        public EVOwner GetEVOwnerByNIC(string nic)
        {
            try
            {
                return _evOwnersCollection.Find(o => o.NIC == nic).FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        // Updates EV Owner profile by NIC (internal method)
        // Updates EV Owner profile by NIC (internal method)
        public EVOwnerOperationResult UpdateProfileByNIC(string nic, EVOwnerUpdateModel model)
        {
            var validationResult = UserUtils.ValidateEVOwnerUpdateModel(model);
            if (!validationResult.IsValid)
            {
                return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.ValidationFailed, validationResult.ErrorMessage);
            }

            try
            {
                var evOwner = _evOwnersCollection.Find(o => o.NIC == nic).FirstOrDefault();
                if (evOwner == null)
                {
                    return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.EVOwnerNotFound);
                }

                var currentUser = _usersCollection.Find(u => u.Id == evOwner.UserId).FirstOrDefault();
                if (currentUser == null)
                {
                    return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.UserNotFound);
                }

                if (!string.IsNullOrEmpty(model.Email) && model.Email != currentUser.Email)
                {
                    if (_usersCollection.Find(u => u.Email == model.Email && u.Id != currentUser.Id).Any())
                    {
                        return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.EmailExists);
                    }
                }

                if (!string.IsNullOrEmpty(model.Phone) && model.Phone != evOwner.Phone)
                {
                    if (_evOwnersCollection.Find(o => o.Phone == model.Phone && o.NIC != nic).Any())
                    {
                        return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.PhoneExists);
                    }
                }

                var userUpdateBuilder = UserUpdateHelper.BuildUserUpdate(new UserUpdateModel
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    Password = model.Password
                });

                _usersCollection.UpdateOne(u => u.Id == evOwner.UserId, userUpdateBuilder);

                var evOwnerUpdateBuilder = UserUpdateHelper.BuildEVOwnerUpdate(model);
                _evOwnersCollection.UpdateOne(o => o.NIC == nic, evOwnerUpdateBuilder);

                return EVOwnerOperationResult.Success(EVOwnerConstants.ProfileUpdatedSuccessfully);
            }
            catch (Exception ex)
            {
                return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.Failed, ex.Message);
            }
        }

        // Updates EV Owner profile by NIC with admin privileges
        // Updates EV Owner profile by NIC with admin privileges
        public EVOwnerOperationResult AdminUpdateProfile(string nic, EVOwnerAdminUpdateModel model)
        {
            var validationResult = UserUtils.ValidateEVOwnerAdminUpdateModel(model);
            if (!validationResult.IsValid)
            {
                return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.ValidationFailed, validationResult.ErrorMessage);
            }

            try
            {
                var evOwner = _evOwnersCollection.Find(o => o.NIC == nic).FirstOrDefault();
                if (evOwner == null)
                {
                    return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.EVOwnerNotFound);
                }

                var currentUser = _usersCollection.Find(u => u.Id == evOwner.UserId).FirstOrDefault();
                if (currentUser == null)
                {
                    return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.UserNotFound);
                }

                if (!string.IsNullOrEmpty(model.Email) && model.Email != currentUser.Email)
                {
                    if (_usersCollection.Find(u => u.Email == model.Email && u.Id != currentUser.Id).Any())
                    {
                        return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.EmailExists);
                    }
                }

                if (!string.IsNullOrEmpty(model.Phone) && model.Phone != evOwner.Phone)
                {
                    if (_evOwnersCollection.Find(o => o.Phone == model.Phone && o.NIC != nic).Any())
                    {
                        return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.PhoneExists);
                    }
                }

                var userUpdateBuilder = UserUpdateHelper.BuildUserUpdate(new UserUpdateModel
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    Password = model.Password
                });

                _usersCollection.UpdateOne(u => u.Id == evOwner.UserId, userUpdateBuilder);

                var evOwnerUpdateBuilder = UserUpdateHelper.BuildEVOwnerAdminUpdate(model);
                _evOwnersCollection.UpdateOne(o => o.NIC == nic, evOwnerUpdateBuilder);

                return EVOwnerOperationResult.Success(EVOwnerConstants.ProfileUpdatedSuccessfully);
            }
            catch (Exception ex)
            {
                return EVOwnerOperationResult.Failed(EVOwnerOperationStatus.Failed, ex.Message);
            }
        }

        // Retrieves all EV Owners with optional filtering
        public UserRetrievalResult GetAllEVOwners(EVOwnerListFilterModel filter = null)
        {
            try
            {
                // Get all EV owners
                var allEVOwners = _evOwnersCollection.Find(_ => true).ToList();

                // Get all users with EV Owner role
                var evOwnerUserIds = allEVOwners.Select(o => o.UserId).ToList();
                var userFilter = Builders<User>.Filter.In(u => u.Id, evOwnerUserIds);
                
                // Apply IsActive filter if provided
                if (filter?.IsActive.HasValue == true)
                {
                    var activeFilter = Builders<User>.Filter.Eq(u => u.IsActive, filter.IsActive.Value);
                    userFilter = Builders<User>.Filter.And(userFilter, activeFilter);
                }

                var users = _usersCollection.Find(userFilter).ToList();

                // Build user profiles - create strongly typed objects for filtering
                var evOwnerProfiles = users.Select(user =>
                {
                    var evOwner = allEVOwners.FirstOrDefault(o => o.UserId == user.Id);
                    return new
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        RoleId = user.RoleId,
                        RoleName = AuthUtils.GetRoleName(user.RoleId),
                        IsActive = user.IsActive,
                        NIC = evOwner?.NIC,
                        Phone = evOwner?.Phone,
                        CreatedAt = user.CreatedAt,
                        UpdatedAt = user.UpdatedAt
                    };
                }).ToList();

                // Apply search term filtering if provided
                if (!string.IsNullOrEmpty(filter?.SearchTerm))
                {
                    var searchTerm = filter.SearchTerm.ToLower();
                    evOwnerProfiles = evOwnerProfiles.Where(p =>
                        (p.Username?.ToLower().Contains(searchTerm) ?? false) ||
                        (p.Email?.ToLower().Contains(searchTerm) ?? false) ||
                        (p.FirstName?.ToLower().Contains(searchTerm) ?? false) ||
                        (p.LastName?.ToLower().Contains(searchTerm) ?? false) ||
                        (p.NIC?.ToLower().Contains(searchTerm) ?? false) ||
                        (p.Phone?.ToLower().Contains(searchTerm) ?? false)
                    ).ToList();
                }

                return UserRetrievalResult.Success(evOwnerProfiles);
            }
            catch (Exception ex)
            {
                return UserRetrievalResult.Failed(ex.Message);
            }
        }
    }
}