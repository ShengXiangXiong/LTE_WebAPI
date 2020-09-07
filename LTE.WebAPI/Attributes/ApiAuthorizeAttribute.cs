using JWT;
using JWT.Serializers;
using LTE.Model;
using LTE.WebAPI.Models;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace LTE.WebAPI.Attributes
{
    public class ApiAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            var authHeader = from t in actionContext.Request.Headers where t.Key == "auth" select t.Value.FirstOrDefault();
            //Todo: Test valid, production annotations
            //return true;

            if (authHeader != null)
            {
                string token = authHeader.FirstOrDefault();
                if (!string.IsNullOrEmpty(token))
                {
                    try
                    {
                        string secret = ConfigurationManager.AppSettings["Secret"].ToString();
                        //secret需要加密
                        IJsonSerializer serializer = new JsonNetSerializer();
                        IDateTimeProvider provider = new UtcDateTimeProvider();
                        IJwtValidator validator = new JwtValidator(serializer, provider);
                        IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
                        IJwtDecoder decoder = new JwtDecoder(serializer, validator, urlEncoder);

                        var json = decoder.DecodeToObject<AuthInfo>(token, secret, verify: true);
                        LoginModel user = (LoginModel)json.userInfo;
                        LoadInfo.UserId.Value = user.userId;
                        if (json != null)
                        {
                            if (Roles.Length != 0)
                            {
                                //role check
                                string[] vs = Roles.Trim().Split(new char[] { ',', ' ', '/', ';', '\\' });
                                foreach (var item in vs)
                                {
                                    if (item.Equals(user.userRole.Trim()))
                                    {
                                        actionContext.RequestContext.RouteData.Values.Add("auth", json);
                                        return true;
                                    }
                                }
                                return false;
                            }
                            return true;
                        }
                        return false;
                    }
                    catch (Exception ex)
                    {
                        return false;
                    }
                }
            }
            //清空描述字段
            Roles = "";
            return false;
        }

        protected override void HandleUnauthorizedRequest(HttpActionContext filterContext)
        {
            base.HandleUnauthorizedRequest(filterContext);

            var response = filterContext.Response = filterContext.Response ?? new HttpResponseMessage();
            //response.StatusCode = HttpStatusCode.Forbidden;
            var content = new Result
            {
                ok = false,
                msg = "服务端拒绝访问：你没有权限，或者掉线了" ,
                code = "3"
            };
            response.Content = new StringContent(Json.Encode(content), Encoding.UTF8, "application/json");
        }
    
    }
}