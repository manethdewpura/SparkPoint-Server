/*
 * RoleModels.cs
 * 
 * This file contains role-related data models used throughout the system.
 * It includes the Role entity class for role management operations
 * with proper MongoDB serialization attributes.
 * 
 */

using MongoDB.Bson.Serialization.Attributes;
using System;

namespace SparkPoint_Server.Models
{
    public class Role
    {
        [BsonId]
        public int Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}