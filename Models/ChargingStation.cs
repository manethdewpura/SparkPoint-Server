using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace SparkPoint_Server.Models
{
    public class ChargingStation
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("location")]
        public string Location { get; set; }

        [BsonElement("type")]
        public string Type { get; set; }

        [BsonElement("totalSlots")]
        public int TotalSlots { get; set; }

        [BsonElement("availableSlots")]
        public int AvailableSlots { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
