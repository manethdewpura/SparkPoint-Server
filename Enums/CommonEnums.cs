namespace SparkPoint_Server.Enums
{
    /// <summary>
    /// User role enumeration
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// System administrator
        /// </summary>
        Admin = 1,
        
        /// <summary>
        /// Charging station user
        /// </summary>
        StationUser = 2,
        
        /// <summary>
        /// Electric vehicle owner
        /// </summary>
        EVOwner = 3
    }

    /// <summary>
    /// Booking status enumeration
    /// </summary>
    public enum BookingStatus
    {
        /// <summary>
        /// Booking is pending confirmation
        /// </summary>
        Pending,
        
        /// <summary>
        /// Booking is confirmed
        /// </summary>
        Confirmed,
        
        /// <summary>
        /// Charging is in progress
        /// </summary>
        InProgress,
        
        /// <summary>
        /// Charging session completed
        /// </summary>
        Completed,
        
        /// <summary>
        /// Booking was cancelled
        /// </summary>
        Cancelled,
        
        /// <summary>
        /// Customer did not show up
        /// </summary>
        NoShow
    }

    /// <summary>
    /// Sort order enumeration
    /// </summary>
    public enum SortOrder
    {
        /// <summary>
        /// Ascending order
        /// </summary>
        Ascending,
        
        /// <summary>
        /// Descending order
        /// </summary>
        Descending
    }

    /// <summary>
    /// Filter operation enumeration
    /// </summary>
    public enum FilterOperation
    {
        /// <summary>
        /// Equal to
        /// </summary>
        Equals,
        
        /// <summary>
        /// Not equal to
        /// </summary>
        NotEquals,
        
        /// <summary>
        /// Greater than
        /// </summary>
        GreaterThan,
        
        /// <summary>
        /// Less than
        /// </summary>
        LessThan,
        
        /// <summary>
        /// Greater than or equal to
        /// </summary>
        GreaterThanOrEqual,
        
        /// <summary>
        /// Less than or equal to
        /// </summary>
        LessThanOrEqual,
        
        /// <summary>
        /// Contains text
        /// </summary>
        Contains,
        
        /// <summary>
        /// In array of values
        /// </summary>
        In,
        
        /// <summary>
        /// Not in array of values
        /// </summary>
        NotIn
    }
}