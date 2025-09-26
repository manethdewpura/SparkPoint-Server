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
        Location,
        Type,
        SearchTerm
    }

    public enum StationSortField
    {
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
        LocationRequired,
        LocationTooLong,
        TypeRequired,
        InvalidType,
        TotalSlotsMustBePositive,
        TotalSlotsExceedsMaximum
    }
}