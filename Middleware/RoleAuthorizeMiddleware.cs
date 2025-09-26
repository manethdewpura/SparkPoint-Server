using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;
using System.Web.Http.Controllers;
using SparkPoint_Server.Helpers;
using SparkPoint_Server.Constants;

namespace SparkPoint_Server.Attributes
{
    public class RoleAuthorizeMiddleware : AuthorizeAttribute
    {
        private readonly string[] roles;

        public RoleAuthorizeMiddleware(params string[] roles)
        {
            this.roles = roles;
        }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var authHeader = actionContext.Request.Headers.Authorization;
            
            if (authHeader == null || authHeader.Scheme != "Bearer")
            {
                HandleUnauthorizedRequest(actionContext);
                return;
            }

            var principal = JwtHelper.ValidateToken(authHeader.Parameter);
            if (principal == null)
            {
                HandleUnauthorizedRequest(actionContext);
                return;
            }

            if (roles.Length > 0 && !roles.Any(role => principal.IsInRole(role)))
            {
                HandleForbiddenRequest(actionContext);
                return;
            }

            actionContext.RequestContext.Principal = principal;
        }

        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
        {
            actionContext.Response = actionContext.Request.CreateResponse(
                HttpStatusCode.Unauthorized, 
                new { error = "Authentication required", message = "Invalid or missing bearer token" });
        }

        private void HandleForbiddenRequest(HttpActionContext actionContext)
        {
            actionContext.Response = actionContext.Request.CreateResponse(
                HttpStatusCode.Forbidden, 
                new { error = "Access denied", message = "You are not authorized to access this resource" });
        }
    }

    // Specific role attributes for common use cases
    public class AdminOnlyAttribute : RoleAuthorizeMiddleware
    {
        public AdminOnlyAttribute() : base(AuthorizationConstants.Roles.Admin) { }
    }

    public class StationUserOnlyAttribute : RoleAuthorizeMiddleware
    {
        public StationUserOnlyAttribute() : base(AuthorizationConstants.Roles.StationUser) { }
    }

    public class EVOwnerOnlyAttribute : RoleAuthorizeMiddleware
    {
        public EVOwnerOnlyAttribute() : base(AuthorizationConstants.Roles.EVOwner) { }
    }

    public class AdminAndStationUserAttribute : RoleAuthorizeMiddleware
    {
        public AdminAndStationUserAttribute() : base(AuthorizationConstants.Roles.Admin, AuthorizationConstants.Roles.StationUser) { }
    }

    public class AdminAndEVOwnerAttribute : RoleAuthorizeMiddleware
    {
        public AdminAndEVOwnerAttribute() : base(AuthorizationConstants.Roles.Admin, AuthorizationConstants.Roles.EVOwner) { }
    }

    public class AllRolesAttribute : RoleAuthorizeMiddleware
    {
        public AllRolesAttribute() : base(AuthorizationConstants.Roles.Admin, AuthorizationConstants.Roles.StationUser, AuthorizationConstants.Roles.EVOwner) { }
    }

    // For backwards compatibility and explicit role specification
    public class RequireRolesAttribute : RoleAuthorizeMiddleware
    {
        public RequireRolesAttribute(params string[] roles) : base(roles) { }

        // Named constructors for common scenarios
        public static RequireRolesAttribute Admin()
            => new RequireRolesAttribute(AuthorizationConstants.Roles.Admin);

        public static RequireRolesAttribute StationUser()
            => new RequireRolesAttribute(AuthorizationConstants.Roles.StationUser);

        public static RequireRolesAttribute EVOwner()
            => new RequireRolesAttribute(AuthorizationConstants.Roles.EVOwner);

        public static RequireRolesAttribute AdminOrStationUser()
            => new RequireRolesAttribute(AuthorizationConstants.Roles.Admin, AuthorizationConstants.Roles.StationUser);

        public static RequireRolesAttribute AdminOrEVOwner()
            => new RequireRolesAttribute(AuthorizationConstants.Roles.Admin, AuthorizationConstants.Roles.EVOwner);
    }
}
