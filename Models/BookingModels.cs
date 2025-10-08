using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace SparkPoint_Server.Models
{
    public class Booking
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("ownerNIC")]
        public string OwnerNIC { get; set; }

        [BsonElement("stationId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string StationId { get; set; }

        [BsonElement("reservationTime")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime ReservationTime { get; set; }

        [BsonElement("slotsRequested")]
        public int SlotsRequested { get; set; } = 1;

        [BsonElement("status")]
        public string Status { get; set; }

        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class BookingWithStationInfo
    {
        public string Id { get; set; }
        public string OwnerNIC { get; set; }
        public string StationId { get; set; }
        public DateTime ReservationTime { get; set; }
        public int SlotsRequested { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Station information - using basic info without slot details
        public ChargingStationBasicInfo Station { get; set; }
        
        // Additional slot information
        public string TimeSlotDisplay { get; set; }
        public DateTime SlotEndTime { get; set; }
    }

    public class ChargingStationBasicInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public LocationCoordinates Location { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }
        public string Type { get; set; }
    }

    public class ChargingStationInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public LocationCoordinates Location { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }
        public string Type { get; set; }
        public int TotalSlots { get; set; }
        public int AvailableSlots { get; set; }
        public bool IsActive { get; set; }
    }

    public class BookingCreateModel
    {
        public string OwnerNIC { get; set; }
        public string StationId { get; set; }
        public DateTime ReservationTime { get; set; }
        public int SlotsRequested { get; set; } = 1;
    }

    public class BookingUpdateModel
    {
        public string StationId { get; set; }
        public DateTime? ReservationTime { get; set; }
        public int? SlotsRequested { get; set; }
    }

    public class BookingStatusUpdateModel
    {
        public string Status { get; set; }
    }

    public class BookingFilterModel
    {
        public string Status { get; set; }
        public string StationId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
