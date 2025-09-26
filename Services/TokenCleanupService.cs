using System;
using System.Threading.Tasks;
using System.Web.Hosting;
using SparkPoint_Server.Services;

namespace SparkPoint_Server.Services
{
    public class TokenCleanupService : IRegisteredObject
    {
        private readonly object _lock = new object();
        private bool _shuttingDown;
        private System.Threading.Timer _timer;

        public TokenCleanupService()
        {
            HostingEnvironment.RegisterObject(this);
        }

        public void Start()
        {
            lock (_lock)
            {
                if (_timer != null) return;

                var interval = TimeSpan.FromHours(Constants.AuthConstants.TokenCleanupIntervalHours);
                _timer = new System.Threading.Timer(DoWork, null, interval, interval);
            }
        }

        public void Stop(bool immediate)
        {
            lock (_lock)
            {
                _shuttingDown = true;
            }

            HostingEnvironment.UnregisterObject(this);
        }

        private void DoWork(object state)
        {
            lock (_lock)
            {
                if (_shuttingDown) return;

                try
                {
                    var authService = new AuthService();
                    authService.CleanupExpiredTokens();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Token cleanup error: {ex.Message}");
                }
            }
        }
    }
}