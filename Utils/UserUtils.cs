/*
 * UserUtils.cs
 * 
 * This utility class provides validation and helper methods for user-related operations.
 * It includes validation for various user models, data sanitization, and profile creation
 * functionality for different user types (regular users, EV owners, station users).
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
    public static class UserUtils
    {
        // Validates user registration model data
        public static UserValidationResult ValidateRegisterModel(RegisterModel model)
        {
            if (model == null)
                return UserValidationResult.Failed(UserValidationError.None, UserConstants.UserDataRequired);

            var errors = new List<UserValidationError>();

            if (string.IsNullOrEmpty(model.Username))
                errors.Add(UserValidationError.UsernameRequired);
            else if (model.Username.Length < UserConstants.MinUsernameLength)
                errors.Add(UserValidationError.UsernameTooShort);
            else if (model.Username.Length > UserConstants.MaxUsernameLength)
                errors.Add(UserValidationError.UsernameTooLong);

            if (string.IsNullOrEmpty(model.Email))
                errors.Add(UserValidationError.EmailRequired);
            else if (!IsValidEmail(model.Email))
                errors.Add(UserValidationError.EmailInvalid);
            else if (model.Email.Length > UserConstants.MaxEmailLength)
                errors.Add(UserValidationError.EmailTooLong);

            if (string.IsNullOrEmpty(model.Password))
                errors.Add(UserValidationError.PasswordRequired);
            else if (model.Password.Length < UserConstants.MinPasswordLength)
                errors.Add(UserValidationError.PasswordTooShort);
            else if (model.Password.Length > UserConstants.MaxPasswordLength)
                errors.Add(UserValidationError.PasswordTooLong);

            if (!string.IsNullOrEmpty(model.FirstName) && model.FirstName.Length > UserConstants.MaxFirstNameLength)
                errors.Add(UserValidationError.FirstNameTooLong);

            if (!string.IsNullOrEmpty(model.LastName) && model.LastName.Length > UserConstants.MaxLastNameLength)
                errors.Add(UserValidationError.LastNameTooLong);

            return errors.Any() ? UserValidationResult.Failed(errors) : UserValidationResult.Success();
        }

        // Validates EV Owner registration model data including NIC and phone
        public static UserValidationResult ValidateEVOwnerRegisterModel(EVOwnerRegisterModel model)
        {
            if (model == null)
                return UserValidationResult.Failed(UserValidationError.None, UserConstants.UserDataRequired);

            var errors = new List<UserValidationError>();

            if (string.IsNullOrEmpty(model.Username))
                errors.Add(UserValidationError.UsernameRequired);
            else if (model.Username.Length < UserConstants.MinUsernameLength)
                errors.Add(UserValidationError.UsernameTooShort);
            else if (model.Username.Length > UserConstants.MaxUsernameLength)
                errors.Add(UserValidationError.UsernameTooLong);

            if (string.IsNullOrEmpty(model.Email))
                errors.Add(UserValidationError.EmailRequired);
            else if (!IsValidEmail(model.Email))
                errors.Add(UserValidationError.EmailInvalid);
            else if (model.Email.Length > UserConstants.MaxEmailLength)
                errors.Add(UserValidationError.EmailTooLong);

            if (string.IsNullOrEmpty(model.Password))
                errors.Add(UserValidationError.PasswordRequired);
            else if (model.Password.Length < UserConstants.MinPasswordLength)
                errors.Add(UserValidationError.PasswordTooShort);
            else if (model.Password.Length > UserConstants.MaxPasswordLength)
                errors.Add(UserValidationError.PasswordTooLong);

            if (!string.IsNullOrEmpty(model.FirstName) && model.FirstName.Length > UserConstants.MaxFirstNameLength)
                errors.Add(UserValidationError.FirstNameTooLong);

            if (!string.IsNullOrEmpty(model.LastName) && model.LastName.Length > UserConstants.MaxLastNameLength)
                errors.Add(UserValidationError.LastNameTooLong);

            if (string.IsNullOrEmpty(model.Phone))
                errors.Add(UserValidationError.PhoneInvalid);
            else if (model.Phone.Length < EVOwnerConstants.MinPhoneLength)
                errors.Add(UserValidationError.PhoneTooShort);
            else if (model.Phone.Length > EVOwnerConstants.MaxPhoneLength)
                errors.Add(UserValidationError.PhoneTooLong);
            else if (!IsValidPhone(model.Phone))
                errors.Add(UserValidationError.PhoneInvalid);

            // Fixed NIC validation logic
            if (string.IsNullOrWhiteSpace(model.NIC))
            {
                errors.Add(UserValidationError.NICRequired);
            }
            else
            {
                var trimmedNIC = model.NIC.Trim();
                if (trimmedNIC.Length < EVOwnerConstants.MinNICLength)
                    errors.Add(UserValidationError.NICTooShort);
                else if (trimmedNIC.Length > EVOwnerConstants.MaxNICLength)
                    errors.Add(UserValidationError.NICTooLong);
                else if (!IsValidNIC(trimmedNIC))
                    errors.Add(UserValidationError.NICInvalid);
            }

            return errors.Any() ? UserValidationResult.Failed(errors) : UserValidationResult.Success();
        }

        // Validates station user creation model data
        public static UserValidationResult ValidateCreateStationUserModel(CreateStationUserModel model)
        {
            if (model == null)
                return UserValidationResult.Failed(UserValidationError.None, UserConstants.UserDataRequired);

            var errors = new List<UserValidationError>();

            if (string.IsNullOrEmpty(model.Username))
                errors.Add(UserValidationError.UsernameRequired);
            else if (model.Username.Length < UserConstants.MinUsernameLength)
                errors.Add(UserValidationError.UsernameTooShort);
            else if (model.Username.Length > UserConstants.MaxUsernameLength)
                errors.Add(UserValidationError.UsernameTooLong);

            if (string.IsNullOrEmpty(model.Email))
                errors.Add(UserValidationError.EmailRequired);
            else if (!IsValidEmail(model.Email))
                errors.Add(UserValidationError.EmailInvalid);
            else if (model.Email.Length > UserConstants.MaxEmailLength)
                errors.Add(UserValidationError.EmailTooLong);

            if (string.IsNullOrEmpty(model.Password))
                errors.Add(UserValidationError.PasswordRequired);
            else if (model.Password.Length < UserConstants.MinPasswordLength)
                errors.Add(UserValidationError.PasswordTooShort);
            else if (model.Password.Length > UserConstants.MaxPasswordLength)
                errors.Add(UserValidationError.PasswordTooLong);

            if (string.IsNullOrEmpty(model.ChargingStationId))
                errors.Add(UserValidationError.ChargingStationIdRequired);

            if (!string.IsNullOrEmpty(model.FirstName) && model.FirstName.Length > UserConstants.MaxFirstNameLength)
                errors.Add(UserValidationError.FirstNameTooLong);

            if (!string.IsNullOrEmpty(model.LastName) && model.LastName.Length > UserConstants.MaxLastNameLength)
                errors.Add(UserValidationError.LastNameTooLong);

            return errors.Any() ? UserValidationResult.Failed(errors) : UserValidationResult.Success();
        }

        // Validates user update model data
        public static UserValidationResult ValidateUserUpdateModel(UserUpdateModel model)
        {
            if (model == null)
                return UserValidationResult.Success();

            var errors = new List<UserValidationError>();

            if (!string.IsNullOrEmpty(model.Username))
            {
                if (model.Username.Length < UserConstants.MinUsernameLength)
                    errors.Add(UserValidationError.UsernameTooShort);
                else if (model.Username.Length > UserConstants.MaxUsernameLength)
                    errors.Add(UserValidationError.UsernameTooLong);
            }

            if (!string.IsNullOrEmpty(model.Email))
            {
                if (!IsValidEmail(model.Email))
                    errors.Add(UserValidationError.EmailInvalid);
                else if (model.Email.Length > UserConstants.MaxEmailLength)
                    errors.Add(UserValidationError.EmailTooLong);
            }

            if (!string.IsNullOrEmpty(model.Password))
            {
                if (model.Password.Length < UserConstants.MinPasswordLength)
                    errors.Add(UserValidationError.PasswordTooShort);
                else if (model.Password.Length > UserConstants.MaxPasswordLength)
                    errors.Add(UserValidationError.PasswordTooLong);
            }

            if (!string.IsNullOrEmpty(model.FirstName) && model.FirstName.Length > UserConstants.MaxFirstNameLength)
                errors.Add(UserValidationError.FirstNameTooLong);

            if (!string.IsNullOrEmpty(model.LastName) && model.LastName.Length > UserConstants.MaxLastNameLength)
                errors.Add(UserValidationError.LastNameTooLong);

            return errors.Any() ? UserValidationResult.Failed(errors) : UserValidationResult.Success();
        }

        // Validates EV Owner update model data
        public static UserValidationResult ValidateEVOwnerUpdateModel(EVOwnerUpdateModel model)
        {
            if (model == null)
                return UserValidationResult.Success();

            var errors = new List<UserValidationError>();

            if (!string.IsNullOrEmpty(model.Email))
            {
                if (!IsValidEmail(model.Email))
                    errors.Add(UserValidationError.EmailInvalid);
                else if (model.Email.Length > UserConstants.MaxEmailLength)
                    errors.Add(UserValidationError.EmailTooLong);
            }

            if (!string.IsNullOrEmpty(model.Password))
            {
                if (model.Password.Length < UserConstants.MinPasswordLength)
                    errors.Add(UserValidationError.PasswordTooShort);
                else if (model.Password.Length > UserConstants.MaxPasswordLength)
                    errors.Add(UserValidationError.PasswordTooLong);
            }

            if (!string.IsNullOrEmpty(model.FirstName) && model.FirstName.Length > UserConstants.MaxFirstNameLength)
                errors.Add(UserValidationError.FirstNameTooLong);

            if (!string.IsNullOrEmpty(model.LastName) && model.LastName.Length > UserConstants.MaxLastNameLength)
                errors.Add(UserValidationError.LastNameTooLong);

            if (!string.IsNullOrEmpty(model.Phone))
            {
                if (model.Phone.Length < EVOwnerConstants.MinPhoneLength)
                    errors.Add(UserValidationError.PhoneTooShort);
                else if (model.Phone.Length > EVOwnerConstants.MaxPhoneLength)
                    errors.Add(UserValidationError.PhoneTooLong);
                else if (!IsValidPhone(model.Phone))
                    errors.Add(UserValidationError.PhoneInvalid);
            }

            return errors.Any() ? UserValidationResult.Failed(errors) : UserValidationResult.Success();
        }

        // Validates EV Owner admin update model data
        public static UserValidationResult ValidateEVOwnerAdminUpdateModel(EVOwnerAdminUpdateModel model)
        {
            if (model == null)
                return UserValidationResult.Success();

            var errors = new List<UserValidationError>();

            if (!string.IsNullOrEmpty(model.Email))
            {
                if (!IsValidEmail(model.Email))
                    errors.Add(UserValidationError.EmailInvalid);
                else if (model.Email.Length > UserConstants.MaxEmailLength)
                    errors.Add(UserValidationError.EmailTooLong);
            }

            if (!string.IsNullOrEmpty(model.Password))
            {
                if (model.Password.Length < UserConstants.MinPasswordLength)
                    errors.Add(UserValidationError.PasswordTooShort);
                else if (model.Password.Length > UserConstants.MaxPasswordLength)
                    errors.Add(UserValidationError.PasswordTooLong);
            }

            if (!string.IsNullOrEmpty(model.FirstName) && model.FirstName.Length > UserConstants.MaxFirstNameLength)
                errors.Add(UserValidationError.FirstNameTooLong);

            if (!string.IsNullOrEmpty(model.LastName) && model.LastName.Length > UserConstants.MaxLastNameLength)
                errors.Add(UserValidationError.LastNameTooLong);

            if (!string.IsNullOrEmpty(model.Phone))
            {
                if (model.Phone.Length < EVOwnerConstants.MinPhoneLength)
                    errors.Add(UserValidationError.PhoneTooShort);
                else if (model.Phone.Length > EVOwnerConstants.MaxPhoneLength)
                    errors.Add(UserValidationError.PhoneTooLong);
                else if (!IsValidPhone(model.Phone))
                    errors.Add(UserValidationError.PhoneInvalid);
            }

            return errors.Any() ? UserValidationResult.Failed(errors) : UserValidationResult.Success();
        }

        // Validates email format using regex pattern
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return emailRegex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        // Validates phone number format using regex pattern
        public static bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            var phoneRegex = new Regex(@"^[0-9+\-\s()]+$");
            return phoneRegex.IsMatch(phone.Trim());
        }

        // Validates NIC format using regex pattern
        public static bool IsValidNIC(string nic)
        {
            if (string.IsNullOrWhiteSpace(nic))
                return false;

            var trimmedNIC = nic.Trim();
            
            var nicRegex = new Regex(@"^(?:[0-9]{9}[vVxX]|[0-9]{12})$");
            return nicRegex.IsMatch(trimmedNIC);
        }

        // Sanitizes input string by trimming whitespace
        public static string SanitizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            return input.Trim();
        }

        // Creates user profile object with optional EV Owner data
        public static object CreateUserProfile(User user, EVOwner evOwner = null)
        {
            var profile = new
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

            if (evOwner != null)
            {
                return new
                {
                    profile.Id,
                    profile.Username,
                    profile.Email,
                    profile.FirstName,
                    profile.LastName,
                    profile.RoleId,
                    profile.RoleName,
                    profile.IsActive,
                    evOwner.NIC,
                    evOwner.Phone,
                    profile.CreatedAt,
                    profile.UpdatedAt
                };
            }

            return profile;
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
                user.ChargingStationId,
                user.IsActive,
                user.CreatedAt,
                user.UpdatedAt
            };
        }
        // Creates station user profile object with charging station details
        public static object CreateStationUserProfileWithStation(User user, ChargingStation station)
        {
            var userProfile = new
            {
                user.Id,
                user.Username,
                user.Email,
                user.FirstName,
                user.LastName,
                user.RoleId,
                RoleName = AuthUtils.GetRoleName(user.RoleId),
                user.ChargingStationId,
                user.IsActive,
                user.CreatedAt,
                user.UpdatedAt
            };

            if (station != null)
            {
                return new
                {
                    userProfile.Id,
                    userProfile.Username,
                    userProfile.Email,
                    userProfile.FirstName,
                    userProfile.LastName,
                    userProfile.RoleId,
                    userProfile.RoleName,
                    userProfile.ChargingStationId,
                    userProfile.IsActive,
                    userProfile.CreatedAt,
                    userProfile.UpdatedAt,
                    ChargingStation = new
                    {
                        station.Id,
                        station.Name,
                        station.Location,
                        station.Address,
                        station.City,
                        station.Province,
                        station.ContactPhone,
                        station.ContactEmail,
                        station.Type,
                        station.TotalSlots,
                        station.AvailableSlots,
                        station.IsActive,
                        station.CreatedAt,
                        station.UpdatedAt
                    }
                };
            }

            return userProfile;
        }
    }
}