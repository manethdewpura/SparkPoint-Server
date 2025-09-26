using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SparkPoint_Server.Models
{
    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RefreshModel
    {
        public string UserId { get; set; }
        public string RefreshToken { get; set; }
    }

    public class RefreshTokenEntry
    {
        public string UserId { get; set; }
        public string Token { get; set; }
    }
}