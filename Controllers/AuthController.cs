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

            var result = _authService.AuthenticateUser(model.Username, model.Password);
            
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

            var result = _authService.RefreshToken(model.UserId, model.RefreshToken);
            
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
                case TokenRefreshStatus.UserInactive:
                    return BadRequest(errorMessage);
                default:
                    return BadRequest(errorMessage);
            }
        }
    }
}
