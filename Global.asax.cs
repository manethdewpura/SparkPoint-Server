using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using SparkPoint_Server.Services;

namespace SparkPoint_Server
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        private static TokenCleanupService _cleanupService;

        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            
            _cleanupService = new TokenCleanupService();
            _cleanupService.Start();
        }

        protected void Application_End()
        {
            _cleanupService?.Stop(false);
        }
    }
}
