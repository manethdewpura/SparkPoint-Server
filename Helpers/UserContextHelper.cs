using System.Security.Claims;
using System.Web.Http;
using SparkPoint_Server.Constants;
using SparkPoint_Server.Helpers;

namespace SparkPoint_Server.Helpers
{
    public static class UserContextHelper
    {
        public static UserContext GetUserContext(ApiController controller)
        {
            var identity = controller.User.Identity as ClaimsIdentity;
            var userIdClaim = identity?.FindFirst(ClaimTypes.NameIdentifier);
            var userRoleClaim = identity?.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userRoleClaim == null)
                return null;

            return new UserContext
            {
                UserId = userIdClaim.Value,
                RoleId = int.Parse(userRoleClaim.Value),
                IsValid = true
            };
        }

        public static string GetCurrentUserId(ApiController controller)
        {
            var authHeader = controller.Request.Headers.Authorization;
            if (authHeader == null || authHeader.Scheme != "Bearer")
                return null;

            var principal = JwtHelper.ValidateToken(authHeader.Parameter);
            if (principal == null)
                return null;

            return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public static string GetCurrentUserRole(ApiController controller)
        {
            var authHeader = controller.Request.Headers.Authorization;
            if (authHeader == null || authHeader.Scheme != "Bearer")
                return null;

            var principal = JwtHelper.ValidateToken(authHeader.Parameter);
            if (principal == null)
                return null;

            return principal.FindFirst(ClaimTypes.Role)?.Value;
        }

        public static string GetCurrentUsername(ApiController controller)
        {
            var authHeader = controller.Request.Headers.Authorization;
            if (authHeader == null || authHeader.Scheme != "Bearer")
                return null;

            var principal = JwtHelper.ValidateToken(authHeader.Parameter);
            if (principal == null)
                return null;

            return principal.FindFirst(ClaimTypes.Name)?.Value;
        }

        public static bool IsUserAuthenticated(ApiController controller)
        {
            return !string.IsNullOrEmpty(GetCurrentUserId(controller));
        }

        public static int? GetCurrentUserRoleId(ApiController controller)
        {
            var roleString = GetCurrentUserRole(controller);
            if (int.TryParse(roleString, out int roleId))
                return roleId;
            return null;
        }

        public static bool IsCurrentUserAdmin(ApiController controller)
        {
            var roleString = GetCurrentUserRole(controller);
            return roleString == AuthorizationConstants.Roles.Admin;
        }

        public static bool IsCurrentUserStationUser(ApiController controller)
        {
            var roleString = GetCurrentUserRole(controller);
            return roleString == AuthorizationConstants.Roles.StationUser;
        }

        public static bool IsCurrentUserEVOwner(ApiController controller)
        {
            var roleString = GetCurrentUserRole(controller);
            return roleString == AuthorizationConstants.Roles.EVOwner;
        }

        public static bool HasRole(ApiController controller, string role)
        {
            var currentRole = GetCurrentUserRole(controller);
            return currentRole == role;
        }

        public static bool HasAnyRole(ApiController controller, params string[] roles)
        {
            var currentRole = GetCurrentUserRole(controller);
            foreach (var role in roles)
            {
                if (currentRole == role)
                    return true;
            }
            return false;
        }
    }

    public class UserContext
    {
        public string UserId { get; set; }
        public int RoleId { get; set; }
        public bool IsValid { get; set; }

        public bool IsAdmin => RoleId == ApplicationConstants.AdminRoleId;

        public bool IsStationUser => RoleId == ApplicationConstants.StationUserRoleId;

        public bool IsEVOwner => RoleId == ApplicationConstants.EVOwnerRoleId;

        public string RoleString
        {
            get
            {
                switch (RoleId)
                {
                    case ApplicationConstants.AdminRoleId:
                        return AuthorizationConstants.Roles.Admin;
                    case ApplicationConstants.StationUserRoleId:
                        return AuthorizationConstants.Roles.StationUser;
                    case ApplicationConstants.EVOwnerRoleId:
                        return AuthorizationConstants.Roles.EVOwner;
                    default:
                        return "Unknown";
                }
            }
        }
    }
}