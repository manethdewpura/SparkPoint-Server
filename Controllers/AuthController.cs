using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Http;
using System.Web;
using SparkPoint_Server.Models;
using SparkPoint_Server.Helpers;
using SparkPoint_Server.Services;
using SparkPoint_Server.Constants;
using SparkPoint_Server.Enums;
using MongoDB.Driver;

namespace SparkPoint_Server.Controllers
{
    [RoutePrefix("api/auth")]
    public class AuthController : ApiController
    {
        private readonly AuthService _authService;

        public AuthController()
        {
            _authService = new AuthService();
        }

        [HttpPost]
        [Route("login")]
        public IHttpActionResult Login(LoginModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest(AuthConstants.UsernamePasswordRequired);
            }

            var context = CreateRefreshTokenContext();
            var result = _authService.AuthenticateUser(model.Username, model.Password, context);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.ErrorMessage);
            }

            if (IsWebClient())
            {
                SetRefreshTokenCookie(result.RefreshToken);
                SetAccessTokenCookie(result.AccessToken);
                
                return Ok(new
                {
                    user = result.UserInfo
                });
            }
            else
            {
                return Ok(new
                {
                    accessToken = result.AccessToken,
                    refreshToken = result.RefreshToken,
                    user = result.UserInfo
                });
            }
        }

        [HttpPost]
        [Route("refresh")]
        public IHttpActionResult Refresh(RefreshModel model)
        {
            string refreshToken = null;
            string userId = null;

            if (IsWebClient())
            {
                refreshToken = GetRefreshTokenFromCookie();
                userId = model?.UserId;
                
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return BadRequest("Refresh token cookie not found");
                }
            }
            else
            {
                if (model == null || string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.RefreshToken))
                {
                    return BadRequest(AuthConstants.UserIdRefreshTokenRequired);
                }
                
                refreshToken = model.RefreshToken;
                userId = model.UserId;
            }

            var context = CreateRefreshTokenContext();
            var result = _authService.RefreshToken(userId, refreshToken, context);
            
            if (!result.IsSuccess)
            {
                if (IsWebClient())
                {
                    ClearRefreshTokenCookie();
                    ClearAccessTokenCookie();
                }
                return GetErrorResponse(result.Status, result.ErrorMessage);
            }

            if (IsWebClient())
            {
                SetRefreshTokenCookie(result.RefreshToken);
                SetAccessTokenCookie(result.AccessToken);
                
                return Ok(new 
                { 
                    message = "Tokens refreshed successfully"
                });
            }
            else
            {
                return Ok(new 
                { 
                    accessToken = result.AccessToken, 
                    refreshToken = result.RefreshToken 
                });
            }
        }

        [HttpPost]
        [Route("logout")]
        public IHttpActionResult Logout(RefreshModel model)
        {
            string refreshToken = null;
            string userId = null;

            if (IsWebClient())
            {
                refreshToken = GetRefreshTokenFromCookie();
                userId = model?.UserId;
                
                ClearRefreshTokenCookie();
                ClearAccessTokenCookie();
            }
            else
            {
                if (model == null || string.IsNullOrEmpty(model.UserId))
                {
                    return BadRequest("UserId is required");
                }
                
                refreshToken = model.RefreshToken;
                userId = model.UserId;
            }

            if (!string.IsNullOrEmpty(userId))
            {
                _authService.LogoutUser(userId, refreshToken);
            }

            return Ok(new { message = "Logged out successfully" });
        }

        [HttpGet]
        [Route("sessions/{userId}")]
        public IHttpActionResult GetActiveSessions(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("UserId is required");
            }

            var sessions = _authService.GetActiveUserSessions(userId);

            return Ok(new { sessions = sessions });
        }

        [HttpDelete]
        [Route("sessions/{userId}/{tokenId}")]
        public IHttpActionResult RevokeSession(string userId, string tokenId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tokenId))
            {
                return BadRequest("UserId and TokenId are required");
            }

            _authService.RevokeUserSession(userId, tokenId);

            return Ok(new { message = "Session revoked successfully" });
        }

        private RefreshTokenContext CreateRefreshTokenContext()
        {
            return new RefreshTokenContext
            {
                UserAgent = Request.Headers.UserAgent?.ToString() ?? "Unknown",
                IpAddress = GetClientIpAddress(),
                RequestTime = DateTime.UtcNow
            };
        }

        private string GetClientIpAddress()
        {
            if (Request.Properties.ContainsKey("MS_HttpContext"))
            {
                var context = Request.Properties["MS_HttpContext"] as System.Web.HttpContextWrapper;
                if (context?.Request != null)
                {
                    var forwardedFor = context.Request.Headers["X-Forwarded-For"];
                    if (!string.IsNullOrEmpty(forwardedFor))
                    {
                        var ips = forwardedFor.Split(',');
                        if (ips.Length > 0)
                        {
                            return ips[0].Trim();
                        }
                    }

                    var realIp = context.Request.Headers["X-Real-IP"];
                    if (!string.IsNullOrEmpty(realIp))
                    {
                        return realIp.Trim();
                    }

                    return context.Request.UserHostAddress ?? "Unknown";
                }
            }

            return "Unknown";
        }

        private IHttpActionResult GetErrorResponse(AuthenticationStatus status, string errorMessage)
        {
            switch (status)
            {
                case AuthenticationStatus.InvalidCredentials:
                    return Unauthorized();
                case AuthenticationStatus.UserInactive:
                case AuthenticationStatus.EVOwnerDeactivated:
                    return BadRequest(errorMessage);
                case AuthenticationStatus.UserNotFound:
                    return Unauthorized();
                default:
                    return BadRequest(errorMessage);
            }
        }

        private IHttpActionResult GetErrorResponse(TokenRefreshStatus status, string errorMessage)
        {
            switch (status)
            {
                case TokenRefreshStatus.UserNotFound:
                case TokenRefreshStatus.InvalidRefreshToken:
                    return Unauthorized();
                case TokenRefreshStatus.ExpiredRefreshToken:
                case TokenRefreshStatus.RevokedRefreshToken:
                case TokenRefreshStatus.TokenFamilyRevoked:
                    return Unauthorized();
                case TokenRefreshStatus.UserInactive:
                    return BadRequest(errorMessage);
                default:
                    return BadRequest(errorMessage);
            }
        }

        private bool IsWebClient()
        {
            var userAgent = Request.Headers.UserAgent?.ToString() ?? "";
            
            try
            {
                // Check for common web browser user agents (case-insensitive)
                var hasWebUserAgent = AuthConstants.WebBrowserUserAgents.Any(indicator => 
                    userAgent.ToLowerInvariant().Contains(indicator.ToLowerInvariant()));
                
                // If wildcard is configured, only check user agent
                if (AuthConstants.AllowedWebOrigins.Contains("*"))
                {
                    return hasWebUserAgent;
                }
                
                // Otherwise, check origin/referer as before
                var origin = Request.Headers.GetValues("Origin")?.FirstOrDefault() ?? "";
                var referer = Request.Headers.Referrer?.ToString() ?? "";
                
                // Check if request comes from allowed web origins (case-insensitive)
                var isWebOrigin = !string.IsNullOrEmpty(origin) && 
                    AuthConstants.AllowedWebOrigins.Any(allowedOrigin => 
                        allowedOrigin != "*" && origin.ToLowerInvariant().Contains(allowedOrigin.ToLowerInvariant()));
                
                // Additional check for referer (case-insensitive)
                var isWebReferer = !string.IsNullOrEmpty(referer) && 
                    AuthConstants.AllowedWebOrigins.Any(allowedOrigin => 
                        allowedOrigin != "*" && referer.ToLowerInvariant().Contains(allowedOrigin.ToLowerInvariant()));
                
                return hasWebUserAgent && (isWebOrigin || isWebReferer);
            }
            catch
            {
                // If any error occurs in detection, default to non-web client
                return false;
            }
        }

        private void SetRefreshTokenCookie(string refreshToken)
        {
            if (HttpContext.Current?.Response != null)
            {
                var isSecure = IsSecureRequest();
                var cookie = new HttpCookie(AuthConstants.RefreshTokenCookieName, refreshToken)
                {
                    HttpOnly = true,
                    Secure = isSecure,
                    Expires = DateTime.UtcNow.AddDays(AuthConstants.RefreshTokenExpiryDays),
                    Path = AuthConstants.RefreshTokenCookiePath
                };
                
                if (isSecure)
                {
                    cookie.SameSite = SameSiteMode.None;
                }
                
                HttpContext.Current.Response.Cookies.Add(cookie);
            }
        }

        private void SetAccessTokenCookie(string accessToken)
        {
            if (HttpContext.Current?.Response != null)
            {
                var isSecure = IsSecureRequest();
                var cookie = new HttpCookie(AuthConstants.AccessTokenCookieName, accessToken)
                {
                    HttpOnly = true,
                    Secure = isSecure,
                    Expires = DateTime.UtcNow.AddMinutes(AuthConstants.AccessTokenExpiryMinutes),
                    Path = AuthConstants.AccessTokenCookiePath
                };
                
                // Only set SameSite for secure connections to avoid cross-site issues
                if (isSecure)
                {
                    cookie.SameSite = SameSiteMode.None;
                }
                // For non-secure connections (development), don't set SameSite attribute
                // This allows the cookie to work in cross-site scenarios
                
                HttpContext.Current.Response.Cookies.Add(cookie);
            }
        }

        private string GetRefreshTokenFromCookie()
        {
            if (HttpContext.Current?.Request?.Cookies != null)
            {
                var cookie = HttpContext.Current.Request.Cookies[AuthConstants.RefreshTokenCookieName];
                return cookie?.Value;
            }
            return null;
        }

        private string GetAccessTokenFromCookie()
        {
            if (HttpContext.Current?.Request?.Cookies != null)
            {
                var cookie = HttpContext.Current.Request.Cookies[AuthConstants.AccessTokenCookieName];
                return cookie?.Value;
            }
            return null;
        }

        private void ClearRefreshTokenCookie()
        {
            if (HttpContext.Current?.Response != null)
            {
                var isSecure = IsSecureRequest();
                var cookie = new HttpCookie(AuthConstants.RefreshTokenCookieName, "")
                {
                    HttpOnly = true,
                    Secure = isSecure,
                    Expires = DateTime.UtcNow.AddDays(-1), // Set expiry to past date
                    Path = AuthConstants.RefreshTokenCookiePath
                };
                
                // Only set SameSite for secure connections to avoid cross-site issues
                if (isSecure)
                {
                    cookie.SameSite = SameSiteMode.None;
                }
                // For non-secure connections (development), don't set SameSite attribute
                
                HttpContext.Current.Response.Cookies.Add(cookie);
            }
        }

        private void ClearAccessTokenCookie()
        {
            if (HttpContext.Current?.Response != null)
            {
                var isSecure = IsSecureRequest();
                var cookie = new HttpCookie(AuthConstants.AccessTokenCookieName, "")
                {
                    HttpOnly = true,
                    Secure = isSecure,
                    Expires = DateTime.UtcNow.AddDays(-1), // Set expiry to past date
                    Path = AuthConstants.AccessTokenCookiePath
                };
                
                // Only set SameSite for secure connections to avoid cross-site issues
                if (isSecure)
                {
                    cookie.SameSite = SameSiteMode.None;
                }
                // For non-secure connections (development), don't set SameSite attribute
                
                HttpContext.Current.Response.Cookies.Add(cookie);
            }
        }

        private bool IsSecureRequest()
        {
            try
            {
                // Check if the request is over HTTPS
                if (HttpContext.Current?.Request != null)
                {
                    return HttpContext.Current.Request.IsSecureConnection;
                }
                
                // Fallback: check the request URI scheme
                return Request.RequestUri?.Scheme?.Equals("https", StringComparison.OrdinalIgnoreCase) == true;
            }
            catch
            {
                // Default to false if we can't determine
                return false;
            }
        }
    }
}
