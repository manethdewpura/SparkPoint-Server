using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;
using System.Web.Http.Controllers;
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
}
