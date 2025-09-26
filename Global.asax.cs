using System.Web.Http;
using SparkPoint_Server.Services;
using dotenv.net;

namespace SparkPoint_Server
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        private static TokenCleanupService _cleanupService;

        protected void Application_Start()
        {
            DotEnv.Load(options: new DotEnvOptions(envFilePaths: new[] { Server.MapPath("~/") + ".env" }, overwriteExistingVars: false));
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
