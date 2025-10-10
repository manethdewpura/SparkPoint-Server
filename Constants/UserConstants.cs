/*
 * UserConstants.cs
 * 
 * This file contains user-related constants used throughout the system.
 * It includes validation messages, success messages, field length limits,
 * and collection names for user management operations.
 */

namespace SparkPoint_Server.Constants
{
    public static class EVOwnerConstants
    {
        public const string UsernameExists = "Username already exists.";
        public const string EmailExists = "Email already exists.";
        public const string PhoneExists = "Phone already exists.";
        public const string NICExists = "NIC already exists.";
        public const string RegistrationSuccessful = "EV Owner registration successful.";
        public const string ProfileUpdatedSuccessfully = "Profile updated successfully.";
        public const string AccountDeactivatedSuccessfully = "Account deactivated successfully.";
        public const string AccountReactivatedSuccessfully = "Account reactivated successfully.";
        public const string UserNotFound = "User not found.";
        public const string EVOwnerNotFound = "EV Owner not found.";
        public const string AccountDeactivated = "Account is deactivated and cannot be updated.";
        public const string AccountAlreadyDeactivated = "Account is already deactivated.";
        public const string AccountAlreadyActive = "Account is already active.";
        
        public const string EVOwnersCollection = "EVOwners";
        
        public const int MaxPhoneLength = 10;
        public const int MaxNICLength = 12;
        public const int MinPhoneLength = 8;
        public const int MinNICLength = 10;
        
        // NIC validation error messages
        public const string NICRequired = "NIC is required.";
        public const string NICInvalidFormat = "Invalid NIC format. Please enter a valid Sri Lankan NIC (9 digits + V/X for old format or 12 digits for new format).";
        public const string NICTooShort = "NIC is too short. Please enter a valid NIC.";
        public const string NICTooLong = "NIC is too long. Please enter a valid NIC.";
    }

    public static class UserConstants
    {
        public const string UsernameRequired = "Username is required.";
        public const string EmailRequired = "Email is required.";
        public const string PasswordRequired = "Password is required.";
        public const string ChargingStationIdRequired = "Charging station ID is required.";
        public const string UserDataRequired = "User data is required.";
        public const string UsernameExists = "Username already exists.";
        public const string EmailExists = "Email already exists.";
        public const string UsernameOrEmailExists = "Username or email already exists.";
        public const string ChargingStationNotFound = "Charging station not found.";
        public const string UserNotFound = "User not found.";
        public const string NotStationUser = "User is not a station user.";
        public const string AdminOnlyEndpoint = "This endpoint is only for Admin users.";
        public const string AccountDeactivated = "Account is deactivated and cannot be updated.";
        
        public const string AdminRegistrationSuccessful = "Admin registration successful.";
        public const string ProfileUpdatedSuccessfully = "Profile updated successfully.";
        public const string StationUserCreatedSuccessfully = "Station user created successfully.";
        public const string StationUserUpdatedSuccessfully = "Station user updated successfully.";
        
        public const int MinUsernameLength = 3;
        public const int MaxUsernameLength = 50;
        public const int MinPasswordLength = 6;
        public const int MaxPasswordLength = 100;
        public const int MaxEmailLength = 100;
        public const int MaxFirstNameLength = 50;
        public const int MaxLastNameLength = 50;
    }
}