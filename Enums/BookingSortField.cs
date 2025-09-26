namespace SparkPoint_Server.Enums
{
    /// <summary>
    /// Enumeration for booking sort fields
    /// </summary>
    public enum BookingSortField
    {
        /// <summary>
        /// Sort by creation date
        /// </summary>
        CreatedAt,
        
        /// <summary>
        /// Sort by last update date
        /// </summary>
        UpdatedAt,
        
        /// <summary>
        /// Sort by reservation time
        /// </summary>
        ReservationTime,
        
        /// <summary>
        /// Sort by booking status
        /// </summary>
        Status
    }
}