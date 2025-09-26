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

            if (string.IsNullOrEmpty(model.NIC))
                errors.Add(UserValidationError.NICInvalid);
            else if (model.NIC.Length < EVOwnerConstants.MinNICLength)
                errors.Add(UserValidationError.NICTooShort);
            else if (model.NIC.Length > EVOwnerConstants.MaxNICLength)
                errors.Add(UserValidationError.NICTooLong);
            else if (!IsValidNIC(model.NIC))
                errors.Add(UserValidationError.NICInvalid);

            return errors.Any() ? UserValidationResult.Failed(errors) : UserValidationResult.Success();
        }

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

        public static bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            var phoneRegex = new Regex(@"^[0-9+\-\s()]+$");
            return phoneRegex.IsMatch(phone.Trim());
        }

        public static bool IsValidNIC(string nic)
        {
            if (string.IsNullOrWhiteSpace(nic))
                return false;

            var nicRegex = new Regex(@"^[0-9]{9}[vVxX]$|^[0-9]{12}$");
            return nicRegex.IsMatch(nic.Trim());
        }

        public static string SanitizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            return input.Trim();
        }

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
    }
}