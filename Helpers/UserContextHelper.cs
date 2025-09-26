using System.Security.Claims;
using System.Web.Http;
using SparkPoint_Server.Constants;

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
    }
    public class UserContext
    {
        public string UserId { get; set; }
        public int RoleId { get; set; }
        public bool IsValid { get; set; }

        public bool IsAdmin => RoleId == ApplicationConstants.AdminRoleId;

        public bool IsStationUser => RoleId == ApplicationConstants.StationUserRoleId;

        public bool IsEVOwner => RoleId == ApplicationConstants.EVOwnerRoleId;
    }
}