/*
 * CommonEnums.cs
 * 
 * This file contains common enumerations used throughout the system.
 * It includes user roles, booking statuses, sort orders, and filter operations
 * that are shared across multiple modules.
 * 
 */

namespace SparkPoint_Server.Enums
{
    public enum UserRole
    {
        Admin = 1,
        StationUser = 2,
        EVOwner = 3
    }

    public enum BookingStatus
    {
        Pending,
        Confirmed,
        InProgress,
        Completed,
        Cancelled,
        NoShow
    }

    public enum SortOrder
    {
        Ascending,
        Descending
    }

    public enum FilterOperation
    {
        Equals,
        NotEquals,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        Contains,
        In,
        NotIn
    }
}