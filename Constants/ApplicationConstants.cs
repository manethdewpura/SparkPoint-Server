namespace SparkPoint_Server.Constants
{

    public static class ApplicationConstants
    {
        // Time constraints
        public const int MaxAdvanceReservationDays = 7;
        public const int MinCancellationHours = 12;
        public const int MinModificationHours = 12;

        // User roles
        public const int AdminRoleId = 1;
        public const int StationUserRoleId = 2;
        public const int EVOwnerRoleId = 3;

        // Role names
        public const string AdminRoleName = "Admin";
        public const string StationUserRoleName = "Station User";
        public const string EVOwnerRoleName = "EV Owner";

        // Role strings for attributes and middleware
        public const string AdminRoleString = "1";
        public const string StationUserRoleString = "2";
        public const string EVOwnerRoleString = "3";

        // Default pagination
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;

        // Validation constants
        public const int MinPasswordLength = 6;
        public const int MaxPasswordLength = 100;
        public const int MinUsernameLength = 3;
        public const int MaxUsernameLength = 50;
    }
}