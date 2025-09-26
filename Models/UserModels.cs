using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SparkPoint_Server.Constants;
using SparkPoint_Server.Enums;
using System;
using System.Collections.Generic;

namespace SparkPoint_Server.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("username")]
        public string Username { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("firstName")]
        public string FirstName { get; set; }

        [BsonElement("lastName")]
        public string LastName { get; set; }

        [BsonElement("roleId")]
        public int RoleId { get; set; }

        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; }

        [BsonElement("chargingStationId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ChargingStationId { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = false;

        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class RegisterModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }
    public class UserUpdateModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Password { get; set; }
    }

    public class UserListFilterModel
    {
        public int? RoleId { get; set; }
        public bool? IsActive { get; set; }
        public string SearchTerm { get; set; }
    }

    public class CreateStationUserModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Password { get; set; }
        public string ChargingStationId { get; set; }
    }
    public class UserOperationResult
    {
        public UserOperationStatus Status { get; private set; }
        public bool IsSuccess => Status == UserOperationStatus.Success;
        public string Message { get; private set; }
        public object Data { get; private set; }

        private UserOperationResult() { }

        public static UserOperationResult Success(string message, object data = null)
        {
            return new UserOperationResult
            {
                Status = UserOperationStatus.Success,
                Message = message,
                Data = data
            };
        }

        public static UserOperationResult Failed(UserOperationStatus status, string customMessage = null)
        {
            var message = customMessage ?? GetDefaultErrorMessage(status);
            return new UserOperationResult
            {
                Status = status,
                Message = message
            };
        }

        private static string GetDefaultErrorMessage(UserOperationStatus status)
        {
            switch (status)
            {
                case UserOperationStatus.UserNotFound:
                    return UserConstants.UserNotFound;
                case UserOperationStatus.UsernameExists:
                    return UserConstants.UsernameExists;
                case UserOperationStatus.EmailExists:
                    return UserConstants.EmailExists;
                case UserOperationStatus.ValidationFailed:
                    return "Validation failed";
                case UserOperationStatus.AccountDeactivated:
                    return UserConstants.AccountDeactivated;
                default:
                    return "Operation failed";
            }
        }
    }

    public class EVOwnerOperationResult
    {
        public EVOwnerOperationStatus Status { get; private set; }
        public bool IsSuccess => Status == EVOwnerOperationStatus.Success;
        public string Message { get; private set; }
        public object Data { get; private set; }

        private EVOwnerOperationResult() { }

        public static EVOwnerOperationResult Success(string message, object data = null)
        {
            return new EVOwnerOperationResult
            {
                Status = EVOwnerOperationStatus.Success,
                Message = message,
                Data = data
            };
        }

        public static EVOwnerOperationResult Failed(EVOwnerOperationStatus status, string customMessage = null)
        {
            var message = customMessage ?? GetDefaultErrorMessage(status);
            return new EVOwnerOperationResult
            {
                Status = status,
                Message = message
            };
        }

        private static string GetDefaultErrorMessage(EVOwnerOperationStatus status)
        {
            switch (status)
            {
                case EVOwnerOperationStatus.UserNotFound:
                    return EVOwnerConstants.UserNotFound;
                case EVOwnerOperationStatus.EVOwnerNotFound:
                    return EVOwnerConstants.EVOwnerNotFound;
                case EVOwnerOperationStatus.UsernameExists:
                    return EVOwnerConstants.UsernameExists;
                case EVOwnerOperationStatus.EmailExists:
                    return EVOwnerConstants.EmailExists;
                case EVOwnerOperationStatus.PhoneExists:
                    return EVOwnerConstants.PhoneExists;
                case EVOwnerOperationStatus.NICExists:
                    return EVOwnerConstants.NICExists;
                case EVOwnerOperationStatus.AccountDeactivated:
                    return EVOwnerConstants.AccountDeactivated;
                case EVOwnerOperationStatus.AccountAlreadyDeactivated:
                    return EVOwnerConstants.AccountAlreadyDeactivated;
                case EVOwnerOperationStatus.AccountAlreadyActive:
                    return EVOwnerConstants.AccountAlreadyActive;
                default:
                    return "Operation failed";
            }
        }
    }

    public class UserValidationResult
    {
        public bool IsValid { get; private set; }
        public List<UserValidationError> Errors { get; private set; }
        public string ErrorMessage { get; private set; }

        private UserValidationResult()
        {
            Errors = new List<UserValidationError>();
        }

        public static UserValidationResult Success()
        {
            return new UserValidationResult
            {
                IsValid = true
            };
        }

        public static UserValidationResult Failed(UserValidationError error, string message = null)
        {
            return new UserValidationResult
            {
                IsValid = false,
                Errors = new List<UserValidationError> { error },
                ErrorMessage = message ?? GetDefaultErrorMessage(error)
            };
        }

        public static UserValidationResult Failed(List<UserValidationError> errors, string message = null)
        {
            return new UserValidationResult
            {
                IsValid = false,
                Errors = errors,
                ErrorMessage = message ?? "Multiple validation errors occurred"
            };
        }

        private static string GetDefaultErrorMessage(UserValidationError error)
        {
            switch (error)
            {
                case UserValidationError.UsernameRequired:
                    return UserConstants.UsernameRequired;
                case UserValidationError.EmailRequired:
                    return UserConstants.EmailRequired;
                case UserValidationError.PasswordRequired:
                    return UserConstants.PasswordRequired;
                default:
                    return "Validation error";
            }
        }
    }

    public class UserRetrievalResult
    {
        public bool IsSuccess { get; private set; }
        public object UserProfile { get; private set; }
        public string ErrorMessage { get; private set; }

        private UserRetrievalResult() { }

        public static UserRetrievalResult Success(object userProfile)
        {
            return new UserRetrievalResult
            {
                IsSuccess = true,
                UserProfile = userProfile
            };
        }

        public static UserRetrievalResult Failed(string errorMessage)
        {
            return new UserRetrievalResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }
}