/*
 * ChargingStationEnums.cs
 * 
 * This file contains charging station-related enumerations used throughout the system.
 * It includes operation status, filter types, sort fields, station types,
 * and validation errors for charging station operations.
 * 
 */

namespace SparkPoint_Server.Enums
{
    public enum StationOperationStatus
    {
        Success,
        StationNotFound,
        AlreadyInState,
        HasActiveBookings,
        ValidationFailed,
        Failed
    }

    public enum StationFilterType
    {
        ActiveStatus,
        Name,
        Location,
        Type,
        SearchTerm
    }

    public enum StationSortField
    {
        Name,
        Location,
        Type,
        TotalSlots,
        AvailableSlots,
        IsActive,
        CreatedAt,
        UpdatedAt
    }

    public enum StationType
    {
        AC,
        DC
    }

    public enum StationValidationError
    {
        None,
        NameRequired,
        NameTooLong,
        LocationRequired,
        LongitudeRequired,
        LatitudeRequired,
        InvalidLongitude,
        InvalidLatitude,
        TypeRequired,
        InvalidType,
        TotalSlotsMustBePositive,
        TotalSlotsExceedsMaximum,
        AddressTooLong,
        CityTooLong,
        ProvinceTooLong,
        ContactPhoneTooLong,
        ContactEmailTooLong,
        InvalidContactEmail
    }
}