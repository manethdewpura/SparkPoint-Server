using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using SparkPoint_Server.Constants;

namespace SparkPoint_Server.Middleware
{

    public class RateLimitAttribute : ActionFilterAttribute
    {
        private readonly int _requestsPerMinute;
        private readonly string _rateLimitType;
        
        private static readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitStore = 
            new ConcurrentDictionary<string, RateLimitInfo>();

        public RateLimitAttribute(int requestsPerMinute = AuthConstants.ReadRateLimitPerMinute, string rateLimitType = "read")
        {
            _requestsPerMinute = requestsPerMinute;
            _rateLimitType = rateLimitType;
        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var identifier = GetClientIdentifier(actionContext);
            var now = DateTime.UtcNow;
            var windowStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc);
            
            var key = $"{identifier}:{_rateLimitType}:{windowStart.Ticks}";
            
            if (_rateLimitStore.Count > 10000)
            {
                CleanupOldEntries();
            }

            var rateLimitInfo = _rateLimitStore.AddOrUpdate(key, 
                new RateLimitInfo { Count = 1, WindowStart = windowStart },
                (k, existing) => 
                {
                    if (existing.WindowStart == windowStart)
                    {
                        existing.Count++;
                        return existing;
                    }
                    else
                    {
                        return new RateLimitInfo { Count = 1, WindowStart = windowStart };
                    }
                });

            if (rateLimitInfo.Count > _requestsPerMinute)
            {
                var response = actionContext.Request.CreateResponse((HttpStatusCode)429);
                response.Headers.Add("X-RateLimit-Limit", _requestsPerMinute.ToString());
                response.Headers.Add("X-RateLimit-Remaining", "0");
                response.Headers.Add("X-RateLimit-Reset", windowStart.AddMinutes(1).ToString("yyyy-MM-ddTHH:mm:ssZ"));
                response.Headers.Add("Retry-After", "60");
                
                response.Content = new StringContent(
                    $"{{\"error\":\"Rate limit exceeded\",\"limit\":{_requestsPerMinute},\"type\":\"{_rateLimitType}\"}}", 
                    System.Text.Encoding.UTF8, 
                    "application/json");

                actionContext.Response = response;
                return;
            }

            // Add rate limit headers to successful responses
            actionContext.Request.Properties["RateLimit-Limit"] = _requestsPerMinute;
            actionContext.Request.Properties["RateLimit-Remaining"] = Math.Max(0, _requestsPerMinute - rateLimitInfo.Count);
            actionContext.Request.Properties["RateLimit-Reset"] = windowStart.AddMinutes(1).ToString("yyyy-MM-ddTHH:mm:ssZ");

            base.OnActionExecuting(actionContext);
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            // Add rate limit headers to response
            if (actionExecutedContext.Response != null)
            {
                var request = actionExecutedContext.Request;
                if (request.Properties.ContainsKey("RateLimit-Limit"))
                {
                    actionExecutedContext.Response.Headers.Add("X-RateLimit-Limit", 
                        request.Properties["RateLimit-Limit"].ToString());
                }
                if (request.Properties.ContainsKey("RateLimit-Remaining"))
                {
                    actionExecutedContext.Response.Headers.Add("X-RateLimit-Remaining", 
                        request.Properties["RateLimit-Remaining"].ToString());
                }
                if (request.Properties.ContainsKey("RateLimit-Reset"))
                {
                    actionExecutedContext.Response.Headers.Add("X-RateLimit-Reset", 
                        request.Properties["RateLimit-Reset"].ToString());
                }
            }

            base.OnActionExecuted(actionExecutedContext);
        }

        private string GetClientIdentifier(HttpActionContext actionContext)
        {
            // Get client IP
            var clientIp = GetClientIpAddress(actionContext);
            
            // Try to get user ID from JWT token for user-based rate limiting
            var userId = GetUserIdFromToken(actionContext);
            
            if (!string.IsNullOrEmpty(userId))
            {
                return $"user:{userId}";
            }
            
            return $"ip:{clientIp}";
        }

        private string GetClientIpAddress(HttpActionContext actionContext)
        {
            // Check for forwarded IP (load balancer/proxy)
            var request = actionContext.Request;
            
            if (request.Headers.Contains("X-Forwarded-For"))
            {
                var forwarded = string.Join(",", request.Headers.GetValues("X-Forwarded-For"));
                var firstIp = forwarded.Split(',')[0].Trim();
                if (!string.IsNullOrEmpty(firstIp))
                    return firstIp;
            }
            
            if (request.Headers.Contains("X-Real-IP"))
            {
                var realIp = string.Join(",", request.Headers.GetValues("X-Real-IP"));
                if (!string.IsNullOrEmpty(realIp))
                    return realIp;
            }

            // Fallback to remote IP
            var context = request.Properties["MS_HttpContext"] as System.Web.HttpContext;
            if (context != null)
            {
                return context.Request.UserHostAddress ?? "unknown";
            }

            return "unknown";
        }

        private string GetUserIdFromToken(HttpActionContext actionContext)
        {
            try
            {
                var authHeader = actionContext.Request.Headers.Authorization;
                if (authHeader == null || authHeader.Scheme != "Bearer")
                    return null;

                var principal = Helpers.JwtHelper.ValidateToken(authHeader.Parameter, validateLifetime: false);
                if (principal == null)
                    return null;

                return principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            }
            catch
            {
                return null;
            }
        }

        private void CleanupOldEntries()
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddMinutes(-5);
                var keysToRemove = new System.Collections.Generic.List<string>();

                foreach (var kvp in _rateLimitStore)
                {
                    if (kvp.Value.WindowStart < cutoffTime)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    _rateLimitStore.TryRemove(key, out _);
                }
            }
            catch
            {
            }
        }
    }

    public class AuthRateLimitAttribute : RateLimitAttribute
    {
        public AuthRateLimitAttribute() : base(AuthConstants.AuthRateLimitPerMinute, "auth") { }
    }

    public class MutationRateLimitAttribute : RateLimitAttribute
    {
        public MutationRateLimitAttribute() : base(AuthConstants.MutationRateLimitPerMinute, "mutation") { }
    }

    public class ReadRateLimitAttribute : RateLimitAttribute
    {
        public ReadRateLimitAttribute() : base(AuthConstants.ReadRateLimitPerMinute, "read") { }
    }

    internal class RateLimitInfo
    {
        public int Count { get; set; }
        public DateTime WindowStart { get; set; }
    }
}