using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;
using SparkPoint_Server.Helpers;

namespace SparkPoint_Server.Attributes
{
    public class RoleAuthorizeMiddleware : AuthorizeAttribute
    {
        private readonly string[] roles;
        public RoleAuthorizeMiddleware(params string[] roles)
        {
            this.roles = roles;
        }

        protected override bool IsAuthorized(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            var authHeader = actionContext.Request.Headers.Authorization;
            if (authHeader == null || authHeader.Scheme != "Bearer")
                return false;
            var principal = JwtHelper.ValidateToken(authHeader.Parameter);
            if (principal == null)
                return false;
            if (roles.Length == 0)
                return true;
            return roles.Any(role => principal.IsInRole(role));
        }

        protected override void HandleUnauthorizedRequest(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Forbidden, "You are not authorized to access this resource.");
        }
    }
}
