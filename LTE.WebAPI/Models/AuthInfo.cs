using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LTE.WebAPI.Models
{

    public class AuthInfo
    {
        /// <summary>
        /// jwt需要传递给payload的负载信息
        /// </summary>
        public AuthInfo()
        {
            iss = "server";
            aud = "client";
            sub = "panda";
            jti = DateTime.Now.ToString("yyyyMMddhhmmss");
            userInfo = null;
            //userId = 1;
            //userName = "panda";
            //userPwd = "panda";
            //string[] userRoles = { "user" };
        }
        //
        public string iss { get; set; }
        public string aud { get; set; }
        public string sub { get; set; }
        public string jti { get; set; }

        public LoginModel userInfo { get; set; }
        //public int userId { get; set; }
        //public string userName { get; set; }
        //public string userPwd { get; set; }
        //public string[] userRoles { get; set; }
    }
    
}