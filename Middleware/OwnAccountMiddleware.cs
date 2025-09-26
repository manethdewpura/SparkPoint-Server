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
                        new { error = "Authentication required", message = "Unable to identify current user" }
                    );
                    return;
                }

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
                            new { error = "Access denied", message = "You can only access your own account" }
                        );
                        return;
                    }
                }
                else
                {
                    actionContext.Response = actionContext.Request.CreateResponse(
                        HttpStatusCode.Forbidden,
                        new { error = "Access denied", message = "You are not authorized to perform this operation" }
                    );
                    return;
                }

                base.OnActionExecuting(actionContext);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OwnAccountMiddleware error: {ex.Message}");
                
                actionContext.Response = actionContext.Request.CreateResponse(
                    HttpStatusCode.InternalServerError,
                    new { error = "Internal error", message = "Error validating account ownership" }
                );
            }
        }

        private bool ValidateEVOwnerOwnership(HttpActionContext actionContext, string currentUserId)
        {
            var dbContext = new MongoDbContext();
            var evOwnersCollection = dbContext.GetCollection<EVOwner>("EVOwners");

            if (!string.IsNullOrEmpty(_nicParameterName))
            {
                var nicValue = GetParameterValue(actionContext, _nicParameterName);

                if (string.IsNullOrEmpty(nicValue))
                {
                    var currentUserEvOwner = evOwnersCollection.Find(o => o.UserId == currentUserId).FirstOrDefault();
                    return currentUserEvOwner != null;
                }

                var evOwner = evOwnersCollection.Find(o => o.NIC == nicValue).FirstOrDefault();
                return evOwner != null && evOwner.UserId == currentUserId;
            }

            if (!string.IsNullOrEmpty(_userIdParameterName))
            {
                var userIdValue = GetParameterValue(actionContext, _userIdParameterName);
                return userIdValue == currentUserId;
            }

            var evOwnerRecord = evOwnersCollection.Find(o => o.UserId == currentUserId).FirstOrDefault();
            return evOwnerRecord != null;
        }

        private string GetParameterValue(HttpActionContext actionContext, string parameterName)
        {
            if (actionContext.ActionArguments.ContainsKey(parameterName))
            {
                return actionContext.ActionArguments[parameterName]?.ToString();
            }

            if (actionContext.ControllerContext.RouteData.Values.ContainsKey(parameterName))
            {
                return actionContext.ControllerContext.RouteData.Values[parameterName]?.ToString();
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