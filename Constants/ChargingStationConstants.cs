namespace SparkPoint_Server.Constants
{
    public static class ChargingStationConstants
    {
        // Validation Messages
        public const string StationDataRequired = "Station data is required.";
        public const string NameRequired = "Station name is required.";
        public const string LocationRequired = "Location coordinates are required.";
        public const string LongitudeRequired = "Longitude is required.";
        public const string LatitudeRequired = "Latitude is required.";
        public const string InvalidLongitude = "Longitude must be between -180 and 180.";
        public const string InvalidLatitude = "Latitude must be between -90 and 90.";
        public const string TypeRequired = "Type is required.";
        public const string TotalSlotsMustBePositive = "Total slots must be greater than 0.";
        public const string UpdateDataRequired = "Update data is required.";
        public const string StationNotFound = "Charging station not found.";
        public const string StationAlreadyActive = "Charging station is already active.";
        public const string StationAlreadyDeactivated = "Charging station is already deactivated.";
        public const string CannotDeactivateWithActiveBookings = "Cannot deactivate station. There are active bookings for this station.";
        public const string AddressTooLong = "Address cannot exceed 200 characters.";
        public const string CityTooLong = "City cannot exceed 100 characters.";
        public const string ProvinceTooLong = "Province cannot exceed 100 characters.";
        public const string ContactPhoneTooLong = "Contact phone cannot exceed 15 characters.";
        public const string ContactEmailTooLong = "Contact email cannot exceed 100 characters.";
        public const string InvalidContactEmail = "Invalid contact email format.";
        
        // Success Messages
        public const string StationCreatedSuccessfully = "Charging station created successfully.";
        public const string StationUpdatedSuccessfully = "Charging station updated successfully.";
        public const string StationActivatedSuccessfully = "Charging station activated successfully.";
        public const string StationDeactivatedSuccessfully = "Charging station deactivated successfully.";
        
        // Database Collection Names
        public const string ChargingStationsCollection = "ChargingStations";
        public const string BookingsCollection = "Bookings";
        
        // Validation Constants
        public const int MinTotalSlots = 1;
        public const int MaxTotalSlots = 100;
        public const int MaxNameLength = 100;
        public const int MaxTypeLength = 50;
        public const int MaxAddressLength = 200;
        public const int MaxCityLength = 100;
        public const int MaxProvinceLength = 100;
        public const int MaxContactPhoneLength = 15;
        public const int MaxContactEmailLength = 100;
        public const double MinLongitude = -180.0;
        public const double MaxLongitude = 180.0;
        public const double MinLatitude = -90.0;
        public const double MaxLatitude = 90.0;
        
        // Station Types (if needed for validation)
        public static readonly string[] ValidStationTypes = { "AC", "DC"};
        
        // Station Search Constants
        public const string NameField = "name";
        public const string LocationField = "location";
        public const string TypeField = "type";
        public const string AddressField = "address";
        public const string CityField = "city";
        public const string ProvinceField = "province";
        
        // Default Values
        public const bool DefaultIsActiveStatus = true;
    }
}