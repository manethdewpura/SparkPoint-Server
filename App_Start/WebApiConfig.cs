using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SparkPoint_Server
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Environment detection
            var isProduction = IsProductionEnvironment();

            // JSON Serialization Settings
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.JsonFormatter.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
            config.Formatters.JsonFormatter.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            config.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            
            // Production: Disable pretty JSON formatting for smaller response size
            config.Formatters.JsonFormatter.SerializerSettings.Formatting = 
                isProduction ? Formatting.None : Formatting.Indented;

            // Remove XML formatter to return only JSON
            config.Formatters.Remove(config.Formatters.XmlFormatter);

            // Add custom message handler for CORS with improved security
            config.MessageHandlers.Add(new SecureCorsMessageHandler());

            // Web API routes - Attribute routing first (enables [Route] attributes)
            config.MapHttpAttributeRoutes();

            // Convention-based routes for backward compatibility
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Additional route for actions
            config.Routes.MapHttpRoute(
                name: "ActionApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Production: Disable detailed error information for security
            config.IncludeErrorDetailPolicy = isProduction ? 
                IncludeErrorDetailPolicy.Never : 
                IncludeErrorDetailPolicy.LocalOnly;
        }

        private static bool IsProductionEnvironment()
        {
            var environment = ConfigurationManager.AppSettings["Environment"];
            return string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);
        }
    }

    public class SecureCorsMessageHandler : DelegatingHandler
    {
        private static readonly HashSet<string> AllowedOrigins = GetAllowedOrigins();
        private static readonly bool IsProductionEnvironment = IsProduction();

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Handle preflight OPTIONS requests
            if (request.Method == HttpMethod.Options)
            {
                var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                AddCorsHeaders(response, request);

                var tcs = new TaskCompletionSource<HttpResponseMessage>();
                tcs.SetResult(response);
                return tcs.Task;
            }

            // Continue with the request and add CORS headers to the response
            return base.SendAsync(request, cancellationToken).ContinueWith(task =>
            {
                var response = task.Result;
                AddCorsHeaders(response, request);
                return response;
            }, cancellationToken);
        }

        private void AddCorsHeaders(HttpResponseMessage response, HttpRequestMessage request)
        {
            var origin = request.Headers.Contains("Origin")
                ? request.Headers.GetValues("Origin")?.FirstOrDefault()
                : null;
            
            // Production: Use specific allowed origins
            if (IsProductionEnvironment)
            {
                if (!string.IsNullOrEmpty(origin) && AllowedOrigins.Contains(origin.ToLowerInvariant()))
                {
                    response.Headers.Add("Access-Control-Allow-Origin", origin);
                    response.Headers.Add("Access-Control-Allow-Credentials", "true");
                }
                // If origin is not allowed, don't add CORS headers (will cause CORS error on frontend)
            }
            else
            {
                // Development: Allow all origins but without credentials for security
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                // Note: Cannot use credentials with wildcard origin
            }

            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With, X-Api-Key");
            response.Headers.Add("Access-Control-Max-Age", "86400"); // 24 hours

            // Security headers
            response.Headers.Add("X-Content-Type-Options", "nosniff");
            response.Headers.Add("X-Frame-Options", "DENY");
            response.Headers.Add("X-XSS-Protection", "1; mode=block");
            
            if (IsProductionEnvironment)
            {
                response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
            }
        }

        private static HashSet<string> GetAllowedOrigins()
        {
            var allowedOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Try to get from environment variable first
            var originsEnv = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
            if (!string.IsNullOrEmpty(originsEnv))
            {
                var origins = originsEnv.Split(',', ';').Select(o => o.Trim().ToLowerInvariant());
                foreach (var origin in origins)
                {
                    if (!string.IsNullOrEmpty(origin))
                        allowedOrigins.Add(origin);
                }
            }

            // Fallback to app settings
            if (allowedOrigins.Count == 0)
            {
                var originsConfig = ConfigurationManager.AppSettings["CORS:AllowedOrigins"];
                if (!string.IsNullOrEmpty(originsConfig))
                {
                    var origins = originsConfig.Split(',', ';').Select(o => o.Trim().ToLowerInvariant());
                    foreach (var origin in origins)
                    {
                        if (!string.IsNullOrEmpty(origin))
                            allowedOrigins.Add(origin);
                    }
                }
            }

            // Default allowed origins for production
            if (allowedOrigins.Count == 0)
            {
                allowedOrigins.Add("*");
            }

            return allowedOrigins;
        }

        private static bool IsProduction()
        {
            var environment = ConfigurationManager.AppSettings["Environment"];
            return string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);
        }
    }
}
