/*
 * TokenCleanupService.cs
 * 
 * This service provides background token cleanup functionality.
 * It runs periodically to clean up expired and revoked tokens,
 * ensuring database performance and security.
 * 
 */

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

        // Constructor: Registers the service with the hosting environment
        public TokenCleanupService()
        {
            HostingEnvironment.RegisterObject(this);
        }

        // Starts the background token cleanup timer
        // Starts the background token cleanup timer
        public void Start()
        {
            lock (_lock)
            {
                if (_timer != null) return;

                var interval = TimeSpan.FromHours(Constants.AuthConstants.TokenCleanupIntervalHours);
                _timer = new System.Threading.Timer(DoWork, null, interval, interval);
            }
        }

        // Stops the cleanup service and unregisters from hosting environment
        // Stops the cleanup service and unregisters from hosting environment
        public void Stop(bool immediate)
        {
            lock (_lock)
            {
                _shuttingDown = true;
            }

            HostingEnvironment.UnregisterObject(this);
        }

        // Performs the actual token cleanup work
        // Performs the actual token cleanup work
        private void DoWork(object state)
        {
            lock (_lock)
            {
                if (_shuttingDown) return;

                try
                {
                    var authService = new AuthService();
                    authService.CleanupExpiredTokens();
                    
                    System.Diagnostics.Debug.WriteLine("Token cleanup completed successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Token cleanup error: {ex.Message}");
                }
            }
        }
    }
}