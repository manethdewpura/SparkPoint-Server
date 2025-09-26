using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Http;
using SparkPoint_Server.Models;
using SparkPoint_Server.Helpers;
using SparkPoint_Server.Services;
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
                return BadRequest("Username and password are required");
            }

            var result = _authService.AuthenticateUser(model.Username, model.Password);
            
            if (!result.IsSuccess)
            {
                if (result.ErrorMessage.Contains("Invalid username or password"))
                {
                    return Unauthorized();
                }
                return BadRequest(result.ErrorMessage);
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
                return BadRequest("UserId and RefreshToken are required");
            }

            var result = _authService.RefreshToken(model.UserId, model.RefreshToken);
            
            if (!result.IsSuccess)
            {
                if (result.ErrorMessage.Contains("not found") || result.ErrorMessage.Contains("Invalid"))
                {
                    return Unauthorized();
                }
                return BadRequest(result.ErrorMessage);
            }

            return Ok(new 
            { 
                accessToken = result.AccessToken, 
                refreshToken = result.RefreshToken 
            });
        }
    }
}
