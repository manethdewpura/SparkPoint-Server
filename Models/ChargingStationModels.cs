using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

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

    public class StationCreateModel
    {
        [Required(ErrorMessage = "Location is required")]
        [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        public string Location { get; set; }

        [Required(ErrorMessage = "Type is required")]
        [StringLength(50, ErrorMessage = "Type cannot exceed 50 characters")]
        public string Type { get; set; }

        [Range(1, 100, ErrorMessage = "Total slots must be between 1 and 100")]
        public int TotalSlots { get; set; }
    }

    public class StationUpdateModel
    {
        [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        public string Location { get; set; }

        [StringLength(50, ErrorMessage = "Type cannot exceed 50 characters")]
        public string Type { get; set; }

        [Range(1, 100, ErrorMessage = "Total slots must be between 1 and 100")]
        public int? TotalSlots { get; set; }
    }

    public class StationFilterModel
    {
        public bool? IsActive { get; set; }
        
        [StringLength(100, ErrorMessage = "Search term cannot exceed 100 characters")]
        public string SearchTerm { get; set; }
    }

    public class StationQueryModel : StationFilterModel
    {
        public string SortBy { get; set; } = "CreatedAt";
        public string SortOrder { get; set; } = "Descending";
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }
}
