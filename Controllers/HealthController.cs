using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using MongoDB.Driver;
using SparkPoint_Server.Helpers;
using SparkPoint_Server.Models;

namespace SparkPoint_Server.Controllers
{
    [RoutePrefix("api/health")]
    public class HealthController : ApiController
    {
        private readonly MongoDbContext _dbContext;

        public HealthController()
        {
            _dbContext = new MongoDbContext();
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetHealth()
        {
            try
            {
                var healthStatus = new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    Version = "1.0.0",
                    Environment = System.Configuration.ConfigurationManager.AppSettings["Environment"] ?? "Development"
                };

                return Ok(healthStatus);
            }
            catch (Exception ex)
            {
                var errorStatus = new
                {
                    Status = "Unhealthy",
                    Timestamp = DateTime.UtcNow,
                    Error = "Health check failed",
                    Details = ex.Message
                };

                return Content(System.Net.HttpStatusCode.ServiceUnavailable, errorStatus);
            }
        }

        [HttpGet]
        [Route("detailed")]
        public async Task<IHttpActionResult> GetDetailedHealth()
        {
            var healthChecks = new System.Collections.Generic.Dictionary<string, object>();
            var overallStatus = "Healthy";

            try
            {
                healthChecks["Application"] = new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    Version = "1.0.0"
                };
            }
            catch (Exception ex)
            {
                overallStatus = "Unhealthy";
                healthChecks["Application"] = new
                {
                    Status = "Unhealthy",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                };
            }

            try
            {
                var testCollection = _dbContext.GetCollection<object>("HealthCheck");
                await testCollection.EstimatedDocumentCountAsync();
                
                healthChecks["Database"] = new
                {
                    Status = "Healthy",
                    Type = "MongoDB",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                overallStatus = "Degraded";
                healthChecks["Database"] = new
                {
                    Status = "Unhealthy",
                    Type = "MongoDB",
                    Error = "Database connectivity failed",
                    Details = ex.Message,
                    Timestamp = DateTime.UtcNow
                };
            }

            var result = new
            {
                Status = overallStatus,
                Timestamp = DateTime.UtcNow,
                Checks = healthChecks
            };

            var statusCode = overallStatus == "Healthy" ? 
                System.Net.HttpStatusCode.OK : 
                System.Net.HttpStatusCode.ServiceUnavailable;

            return Content(statusCode, result);
        }

        [HttpGet]
        [Route("ready")]
        public async Task<IHttpActionResult> GetReadiness()
        {
            try
            {
                var testCollection = _dbContext.GetCollection<object>("HealthCheck");
                await testCollection.EstimatedDocumentCountAsync();
                
                return Ok(new { Status = "Ready", Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.ServiceUnavailable, new 
                { 
                    Status = "Not Ready", 
                    Timestamp = DateTime.UtcNow,
                    Error = "Database not accessible"
                });
            }
        }

        [HttpGet]
        [Route("live")]
        public IHttpActionResult GetLiveness()
        {
            return Ok(new { Status = "Alive", Timestamp = DateTime.UtcNow });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }
    }
}