/*
 * BookingSortField.cs
 * 
 * This file contains booking sort field enumerations used for sorting booking queries.
 * It defines the available fields that can be used to sort booking results
 * in various booking operations.
 * 
 */

namespace SparkPoint_Server.Enums
{
    public enum BookingSortField
    {
        CreatedAt,
        UpdatedAt,
        ReservationTime,
        Status
    }
}