using System.Linq;
using MongoDB.Driver;
using SparkPoint_Server.Models;
using SparkPoint_Server.Helpers;
using SparkPoint_Server.Constants;

namespace SparkPoint_Server.Helpers
{

    public class BookingAuthorizationHelper
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<EVOwner> _evOwnersCollection;

        public BookingAuthorizationHelper()
        {
            var dbContext = new MongoDbContext();
            _usersCollection = dbContext.GetCollection<User>("Users");
            _evOwnersCollection = dbContext.GetCollection<EVOwner>("EVOwners");
        }

        public AuthorizationResult CanAccessBooking(UserContext userContext, Booking booking)
        {
            if (userContext?.IsAdmin == true)
                return AuthorizationResult.Success();

            if (userContext?.IsEVOwner == true)
            {
                var evOwner = _evOwnersCollection.Find(ev => ev.UserId == userContext.UserId).FirstOrDefault();
                if (evOwner == null)
                    return AuthorizationResult.Failed("EV Owner profile not found.");

                if (booking.OwnerNIC != evOwner.NIC)
                    return AuthorizationResult.Failed(ValidationMessages.AccessDenied);

                return AuthorizationResult.Success();
            }

            if (userContext?.IsStationUser == true)
            {
                var user = _usersCollection.Find(u => u.Id == userContext.UserId).FirstOrDefault();
                if (user == null || string.IsNullOrEmpty(user.ChargingStationId))
                    return AuthorizationResult.Failed("Station user not properly configured.");

                if (booking.StationId != user.ChargingStationId)
                    return AuthorizationResult.Failed(ValidationMessages.AccessDenied);

                return AuthorizationResult.Success();
            }

            return AuthorizationResult.Failed(ValidationMessages.AccessDenied);
        }

        public AuthorizationResult CanCreateBooking(UserContext userContext)
        {
            if (userContext?.IsAdmin == true || userContext?.IsEVOwner == true)
                return AuthorizationResult.Success();

            return AuthorizationResult.Failed("Only Admins and EV Owners can create bookings.");
        }

        public AuthorizationResult CanUpdateBooking(UserContext userContext, Booking booking)
        {
            if (userContext?.IsAdmin == true)
                return AuthorizationResult.Success();

            if (userContext?.IsEVOwner == true)
            {
                var evOwner = _evOwnersCollection.Find(ev => ev.UserId == userContext.UserId).FirstOrDefault();
                if (evOwner == null)
                    return AuthorizationResult.Failed("EV Owner profile not found.");

                if (booking.OwnerNIC != evOwner.NIC)
                    return AuthorizationResult.Failed(ValidationMessages.AccessDenied);

                return AuthorizationResult.Success();
            }

            return AuthorizationResult.Failed("Only Admins and booking owners can update bookings.");
        }

        public AuthorizationResult CanCancelBooking(UserContext userContext, Booking booking)
        {
            return CanUpdateBooking(userContext, booking); // Same rules as update
        }

        public AuthorizationResult CanUpdateBookingStatus(UserContext userContext, Booking booking)
        {
            if (userContext?.IsAdmin == true)
                return AuthorizationResult.Success();

            if (userContext?.IsStationUser == true)
            {
                var user = _usersCollection.Find(u => u.Id == userContext.UserId).FirstOrDefault();
                if (user == null || string.IsNullOrEmpty(user.ChargingStationId))
                    return AuthorizationResult.Failed("Station user not properly configured.");

                if (booking.StationId != user.ChargingStationId)
                    return AuthorizationResult.Failed(ValidationMessages.AccessDenied);

                return AuthorizationResult.Success();
            }

            return AuthorizationResult.Failed("Only Admins and Station Users can update booking status.");
        }

        public OwnerResolutionResult ResolveOwnerNIC(UserContext userContext, string requestedOwnerNIC)
        {
            if (userContext?.IsEVOwner == true)
            {
                var evOwner = _evOwnersCollection.Find(ev => ev.UserId == userContext.UserId).FirstOrDefault();
                if (evOwner == null)
                    return OwnerResolutionResult.Failed("EV Owner profile not found.");

                return OwnerResolutionResult.Success(evOwner.NIC);
            }
            
            if (userContext?.IsAdmin == true)
            {
                if (string.IsNullOrEmpty(requestedOwnerNIC))
                    return OwnerResolutionResult.Failed(ValidationMessages.OwnerNICRequired);

                return OwnerResolutionResult.Success(requestedOwnerNIC);
            }

            return OwnerResolutionResult.Failed("Cannot resolve owner NIC.");
        }

        public FilterDefinitionResult<Booking> GetBookingsFilter(UserContext userContext)
        {
            var emptyFilter = Builders<Booking>.Filter.Empty;

            if (userContext?.IsAdmin == true)
                return FilterDefinitionResult<Booking>.Success(emptyFilter); // Admins see all bookings

            if (userContext?.IsEVOwner == true)
            {
                var evOwner = _evOwnersCollection.Find(ev => ev.UserId == userContext.UserId).FirstOrDefault();
                if (evOwner == null)
                    return FilterDefinitionResult<Booking>.Failed("EV Owner profile not found.");

                var ownerFilter = Builders<Booking>.Filter.Eq(b => b.OwnerNIC, evOwner.NIC);
                return FilterDefinitionResult<Booking>.Success(ownerFilter);
            }

            if (userContext?.IsStationUser == true)
            {
                var user = _usersCollection.Find(u => u.Id == userContext.UserId).FirstOrDefault();
                if (user == null || string.IsNullOrEmpty(user.ChargingStationId))
                    return FilterDefinitionResult<Booking>.Failed("Station user not properly configured.");

                var stationFilter = Builders<Booking>.Filter.Eq(b => b.StationId, user.ChargingStationId);
                return FilterDefinitionResult<Booking>.Success(stationFilter);
            }

            return FilterDefinitionResult<Booking>.Failed(ValidationMessages.AccessDenied);
        }
    }

    public class AuthorizationResult
    {
        public bool IsAuthorized { get; private set; }
        public string ErrorMessage { get; private set; }

        private AuthorizationResult() { }

        public static AuthorizationResult Success()
        {
            return new AuthorizationResult { IsAuthorized = true };
        }

        public static AuthorizationResult Failed(string errorMessage)
        {
            return new AuthorizationResult
            {
                IsAuthorized = false,
                ErrorMessage = errorMessage
            };
        }
    }

    public class OwnerResolutionResult
    {
        public bool IsSuccess { get; private set; }
        public string OwnerNIC { get; private set; }
        public string ErrorMessage { get; private set; }

        private OwnerResolutionResult() { }

        public static OwnerResolutionResult Success(string ownerNIC)
        {
            return new OwnerResolutionResult
            {
                IsSuccess = true,
                OwnerNIC = ownerNIC
            };
        }

        public static OwnerResolutionResult Failed(string errorMessage)
        {
            return new OwnerResolutionResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }

    public class FilterDefinitionResult<T>
    {
        public bool IsSuccess { get; private set; }
        public FilterDefinition<T> Filter { get; private set; }
        public string ErrorMessage { get; private set; }

        private FilterDefinitionResult() { }

        public static FilterDefinitionResult<T> Success(FilterDefinition<T> filter)
        {
            return new FilterDefinitionResult<T>
            {
                IsSuccess = true,
                Filter = filter
            };
        }

        public static FilterDefinitionResult<T> Failed(string errorMessage)
        {
            return new FilterDefinitionResult<T>
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }
}