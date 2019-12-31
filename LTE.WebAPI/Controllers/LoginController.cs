using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LTE.WebAPI.Models;

namespace LTE.WebAPI.Controllers
{
    [AllowAnonymous]
    public class LoginController : ApiController
    {
        /// <summary>
        /// 验证登录：验证用户名，密码是否正确
        /// </summary>
        /// <param name="user">用户信息</param>
        /// <returns>失败时返回错误提示</returns>
        [HttpPost]
        public Result PostLogin([FromBody]LoginModel user)
        {
            Result res = LoginModel.CheckUser(user.userName, user.userPwd);
            return res;
        }
    }
}
