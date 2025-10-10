/*
 * Global.asax.cs
 * 
 * This file contains the global application configuration and startup logic.
 * It handles application initialization, environment variable loading,
 * Web API configuration, and token cleanup service management.
 * 
 * Author: SparkPoint Development Team
 * Created: 2024
 * Last Modified: 2024
 */

using System.Web.Http;
using SparkPoint_Server.Services;
using dotenv.net;

namespace SparkPoint_Server
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        private static TokenCleanupService _cleanupService;

        // Initializes the application and starts background services
        protected void Application_Start()
        {
            DotEnv.Load(options: new DotEnvOptions(envFilePaths: new[] { Server.MapPath("~/") + ".env" }, overwriteExistingVars: false));
            GlobalConfiguration.Configure(WebApiConfig.Register);
            
            _cleanupService = new TokenCleanupService();
            _cleanupService.Start();
        }

        // Cleans up resources when the application shuts down
        protected void Application_End()
        {
            _cleanupService?.Stop(false);
        }
    }
}
