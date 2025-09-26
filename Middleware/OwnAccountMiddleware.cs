using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using SparkPoint_Server.Helpers;
using SparkPoint_Server.Models;
using MongoDB.Driver;

namespace SparkPoint_Server.Attributes
{
    public class OwnAccountMiddleware : ActionFilterAttribute
    {
        private readonly string _nicParameterName;
        private readonly string _userIdParameterName;

        public OwnAccountMiddleware(string nicParameterName = "nic")
        {
            _nicParameterName = nicParameterName;
        }

        public OwnAccountMiddleware(string userIdParameterName, bool isUserIdBased)
        {
            if (isUserIdBased)
            {
                _userIdParameterName = userIdParameterName;
            }
        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            try
            {
                var currentUserId = GetCurrentUserId(actionContext);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    actionContext.Response = actionContext.Request.CreateResponse(
                        HttpStatusCode.Unauthorized,
                        "Unable to identify current user."
                    );
                    return;

                    var currentUserRole = GetCurrentUserRole(actionContext);

                    if (currentUserRole == "1" || currentUserRole == "2")
                    {
                        base.OnActionExecuting(actionContext);
                        return;
                    }

                    if (currentUserRole == "3")
                    {
                        if (!ValidateEVOwnerOwnership(actionContext, currentUserId))
                        {
                            actionContext.Response = actionContext.Request.CreateResponse(
                                HttpStatusCode.Forbidden,
                                "You can only access your own account."
                            );
                            return;
                        }
                    }
                    else
                    {
                        actionContext.Response = actionContext.Request.CreateResponse(
                            HttpStatusCode.Forbidden,
                            "You are not authorized to perform this operation."
                        );
                        return;
                    }

                    base.OnActionExecuting(actionContext);
                }
            }
            catch (Exception)
            {
                actionContext.Response = actionContext.Request.CreateResponse(
                    HttpStatusCode.InternalServerError,
                    "Error validating account ownership."
                );
            }
        }

        private bool ValidateEVOwnerOwnership(HttpActionContext actionContext, string currentUserId)
        {
            var dbContext = new MongoDbContext();
            var evOwnersCollection = dbContext.GetCollection<EVOwner>("EVOwners");

            // If validating by NIC parameter
            if (!string.IsNullOrEmpty(_nicParameterName))
            {
                var nicValue = actionContext.ActionArguments.ContainsKey(_nicParameterName)
                    ? actionContext.ActionArguments[_nicParameterName]?.ToString()
                    : GetRouteValue(actionContext, _nicParameterName);

                if (string.IsNullOrEmpty(nicValue))
                {
                    // For operations without NIC parameter (like profile update), 
                    // just validate that the user is an EV Owner
                    var currentUserEvOwner = evOwnersCollection.Find(o => o.UserId == currentUserId).FirstOrDefault();
                    return currentUserEvOwner != null;
                }

                // Check if the requested NIC belongs to the current user
                var evOwner = evOwnersCollection.Find(o => o.NIC == nicValue).FirstOrDefault();
                return evOwner != null && evOwner.UserId == currentUserId;
            }

            // If validating by UserId parameter
            if (!string.IsNullOrEmpty(_userIdParameterName))
            {
                var userIdValue = actionContext.ActionArguments.ContainsKey(_userIdParameterName)
                    ? actionContext.ActionArguments[_userIdParameterName]?.ToString()
                    : GetRouteValue(actionContext, _userIdParameterName);

                return userIdValue == currentUserId;
            }

            // Default: just validate that the user is an EV Owner
            var evOwnerRecord = evOwnersCollection.Find(o => o.UserId == currentUserId).FirstOrDefault();
            return evOwnerRecord != null;
        }

        private string GetRouteValue(HttpActionContext actionContext, string parameterName)
        {
            // Try to get from route data through the controller context
            if (actionContext.ControllerContext.RouteData.Values.ContainsKey(parameterName))
            {
                return actionContext.ControllerContext.RouteData.Values[parameterName]?.ToString();
            }

            // Try to get from request URI
            var requestUri = actionContext.Request.RequestUri;
            if (requestUri != null)
            {
                var segments = requestUri.Segments;
                // This is a simple approach - in a real scenario, you might need more sophisticated route parsing
                if (segments.Length > 0)
                {
                    var lastSegment = segments.LastOrDefault()?.Trim('/');
                    return lastSegment;
                }
            }

            return null;
        }

        private string GetCurrentUserId(HttpActionContext actionContext)
        {
            var authHeader = actionContext.Request.Headers.Authorization;
            if (authHeader == null || authHeader.Scheme != "Bearer")
                return null;

            var principal = JwtHelper.ValidateToken(authHeader.Parameter);
            if (principal == null)
                return null;

            return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private string GetCurrentUserRole(HttpActionContext actionContext)
        {
            var authHeader = actionContext.Request.Headers.Authorization;
            if (authHeader == null || authHeader.Scheme != "Bearer")
                return null;

            var principal = JwtHelper.ValidateToken(authHeader.Parameter);
            if (principal == null)
                return null;

            return principal.FindFirst(ClaimTypes.Role)?.Value;
        }
    }
}