using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Http;
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

            return Ok(new
            {
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken,
                user = result.UserInfo
            });
        }

        [HttpPost]
        [Route("refresh")]
        public IHttpActionResult Refresh(RefreshModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.RefreshToken))
            {
                return BadRequest(AuthConstants.UserIdRefreshTokenRequired);
            }

            var context = CreateRefreshTokenContext();
            var result = _authService.RefreshToken(model.UserId, model.RefreshToken, context);
            
            if (!result.IsSuccess)
            {
                return GetErrorResponse(result.Status, result.ErrorMessage);
            }

            return Ok(new 
            { 
                accessToken = result.AccessToken, 
                refreshToken = result.RefreshToken 
            });
        }

        [HttpPost]
        [Route("logout")]
        public IHttpActionResult Logout(RefreshModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.UserId))
            {
                return BadRequest("UserId is required");
            }

            _authService.LogoutUser(model.UserId, model.RefreshToken);

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
    }
}
