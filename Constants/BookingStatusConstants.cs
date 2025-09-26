namespace SparkPoint_Server.Constants
{
    public static class BookingStatusConstants
    {
        // Standard booking statuses
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string InProgress = "In Progress";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
        public const string NoShow = "No Show";

        // Status arrays for slot management
        public static readonly string[] SlotReservingStatuses = { Confirmed, InProgress };
        public static readonly string[] SlotFreeingStatuses = { Completed, Cancelled, NoShow };
        public static readonly string[] ValidStatuses = { Pending, Confirmed, InProgress, Completed, Cancelled, NoShow };
        public static readonly string[] NearTimeAllowedStatuses = { InProgress, Completed, NoShow };

        public static bool IsSlotReservingStatus(string status)
        {
            return System.Array.IndexOf(SlotReservingStatuses, status) >= 0;
        }

        public static bool IsSlotFreeingStatus(string status)
        {
            return System.Array.IndexOf(SlotFreeingStatuses, status) >= 0;
        }

        public static bool IsValidStatus(string status)
        {
            return System.Array.IndexOf(ValidStatuses, status) >= 0;
        }

        public static bool IsNearTimeAllowedStatus(string status)
        {
            return System.Array.IndexOf(NearTimeAllowedStatuses, status) >= 0;
        }
    }
}