using System.Security.Claims;
using System.Web.Http;
using SparkPoint_Server.Helpers;

namespace SparkPoint_Server.Utils
{
    public static class ControllerUtils
    {
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
    }
}