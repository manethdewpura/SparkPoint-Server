using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Web.Http;
using SparkPoint_Server.Models;
using SparkPoint_Server.Helpers;
using SparkPoint_Server.Attributes;
using MongoDB.Driver;

namespace SparkPoint_Server.Controllers
{
    [RoutePrefix("api/stations")]
    public class StationsController : ApiController
    {
        private readonly IMongoCollection<ChargingStation> _stationsCollection;
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<Booking> _bookingsCollection;

        public StationsController()
        {
            var dbContext = new MongoDbContext();
            _stationsCollection = dbContext.GetCollection<ChargingStation>("ChargingStations");
            _usersCollection = dbContext.GetCollection<User>("Users");
            _bookingsCollection = dbContext.GetCollection<Booking>("Bookings");
        }

        [HttpPost]
        [Route("")]
        [AdminOnly]
        public IHttpActionResult CreateStation(StationCreateModel model)
        {
            if (model == null)
                return BadRequest("Station data is required.");

            if (string.IsNullOrEmpty(model.Location))
                return BadRequest("Location is required.");

            if (string.IsNullOrEmpty(model.Type))
                return BadRequest("Type is required.");

            if (model.TotalSlots <= 0)
                return BadRequest("Total slots must be greater than 0.");

            var station = new ChargingStation
            {
                Location = model.Location,
                Type = model.Type,
                TotalSlots = model.TotalSlots,
                AvailableSlots = model.TotalSlots,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _stationsCollection.InsertOne(station);

            return Ok(new { Message = "Charging station created successfully.", StationId = station.Id });
        }

        [HttpGet]
        [Route("")]
        [AdminOnly]
        public IHttpActionResult GetStations([FromUri] StationFilterModel filter = null)
        {
            var filterBuilder = Builders<ChargingStation>.Filter.Empty;

            if (filter != null)
            {
                if (filter.IsActive.HasValue)
                {
                    var activeFilter = Builders<ChargingStation>.Filter.Eq(s => s.IsActive, filter.IsActive.Value);
                    filterBuilder = Builders<ChargingStation>.Filter.And(filterBuilder, activeFilter);
                }

                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    var searchFilter = Builders<ChargingStation>.Filter.Or(
                        Builders<ChargingStation>.Filter.Regex(s => s.Location, new MongoDB.Bson.BsonRegularExpression(filter.SearchTerm, "i")),
                        Builders<ChargingStation>.Filter.Regex(s => s.Type, new MongoDB.Bson.BsonRegularExpression(filter.SearchTerm, "i"))
                    );
                    filterBuilder = Builders<ChargingStation>.Filter.And(filterBuilder, searchFilter);
                }
            }

            var stations = _stationsCollection.Find(filterBuilder).ToList();

            return Ok(stations);
        }

        [HttpGet]
        [Route("{stationId}")]
        [AdminOnly]
        public IHttpActionResult GetStation(string stationId)
        {
            var station = _stationsCollection.Find(s => s.Id == stationId).FirstOrDefault();
            if (station == null)
                return BadRequest("Charging station not found.");

            var stationUsers = _usersCollection.Find(u => u.ChargingStationId == stationId && u.RoleId == 2).ToList();

            var userProfiles = stationUsers.Select(user => new
            {
                user.Id,
                user.Username,
                user.Email,
                user.FirstName,
                user.LastName,
                user.RoleId,
                RoleName = "Station User",
                user.IsActive,
                user.CreatedAt,
                user.UpdatedAt
            }).ToList();

            var response = new
            {
                Station = station,
                StationUsers = userProfiles
            };

            return Ok(response);
        }

        [HttpPut]
        [Route("{stationId}")]
        [AdminOnly]
        public IHttpActionResult UpdateStation(string stationId, StationUpdateModel model)
        {
            if (model == null)
                return BadRequest("Update data is required.");

            var station = _stationsCollection.Find(s => s.Id == stationId).FirstOrDefault();
            if (station == null)
                return BadRequest("Charging station not found.");

            var updateBuilder = Builders<ChargingStation>.Update.Set(s => s.UpdatedAt, DateTime.UtcNow);

            if (!string.IsNullOrEmpty(model.Location))
                updateBuilder = updateBuilder.Set(s => s.Location, model.Location);

            if (!string.IsNullOrEmpty(model.Type))
                updateBuilder = updateBuilder.Set(s => s.Type, model.Type);

            if (model.TotalSlots.HasValue && model.TotalSlots.Value > 0)
            {
                var currentAvailable = station.AvailableSlots;
                var currentTotal = station.TotalSlots;
                var newTotal = model.TotalSlots.Value;
                var newAvailable = Math.Max(0, Math.Min(newTotal, currentAvailable + (newTotal - currentTotal)));

                updateBuilder = updateBuilder.Set(s => s.TotalSlots, newTotal);
                updateBuilder = updateBuilder.Set(s => s.AvailableSlots, newAvailable);
            }

            _stationsCollection.UpdateOne(s => s.Id == stationId, updateBuilder);

            return Ok("Charging station updated successfully.");
        }

        [HttpPut]
        [Route("activate/{stationId}")]
        [AdminOnly]
        public IHttpActionResult ActivateStation(string stationId)
        {
            var station = _stationsCollection.Find(s => s.Id == stationId).FirstOrDefault();
            if (station == null)
                return BadRequest("Charging station not found.");

            if (station.IsActive)
                return BadRequest("Charging station is already active.");

            var update = Builders<ChargingStation>.Update
                .Set(s => s.IsActive, true)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            _stationsCollection.UpdateOne(s => s.Id == stationId, update);

            return Ok("Charging station activated successfully.");
        }

        [HttpPut]
        [Route("deactivate/{stationId}")]
        [AdminOnly]
        public IHttpActionResult DeactivateStation(string stationId)
        {
            var station = _stationsCollection.Find(s => s.Id == stationId).FirstOrDefault();
            if (station == null)
                return BadRequest("Charging station not found.");

            if (!station.IsActive)
                return BadRequest("Charging station is already deactivated.");

            var activeBookingsFilter = Builders<Booking>.Filter.And(
                Builders<Booking>.Filter.Eq(b => b.StationId, stationId),
                Builders<Booking>.Filter.Not(
                    Builders<Booking>.Filter.In(b => b.Status, new[] { "Cancelled", "Completed" })
                )
            );

            var activeBookingsCount = _bookingsCollection.CountDocuments(activeBookingsFilter);
            if (activeBookingsCount > 0)
            {
                return BadRequest("Cannot deactivate station. There are active bookings for this station.");
            }

            var update = Builders<ChargingStation>.Update
                .Set(s => s.IsActive, false)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            _stationsCollection.UpdateOne(s => s.Id == stationId, update);

            return Ok("Charging station deactivated successfully.");
        }
    }

    public class StationCreateModel
    {
        public string Location { get; set; }
        public string Type { get; set; }
        public int TotalSlots { get; set; }
    }

    public class StationUpdateModel
    {
        public string Location { get; set; }
        public string Type { get; set; }
        public int? TotalSlots { get; set; }
    }

    public class StationFilterModel
    {
        public bool? IsActive { get; set; }
        public string SearchTerm { get; set; }
    }
}
