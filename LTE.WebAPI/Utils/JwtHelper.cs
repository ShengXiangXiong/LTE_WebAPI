using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using System.Configuration;
using LTE.WebAPI.Models;

namespace LTE.WebAPI.Utils
{
    public class JwtHelper
    {

        //私钥  web.config中配置
        //"panda";
        private static string secret = ConfigurationManager.AppSettings["Secret"].ToString();

        /// <summary>
        /// 生成JwtToken
        /// </summary>
        /// <param name="payload">不敏感的用户数据</param>
        /// <returns></returns>
        public static string SetJwtEncode(AuthInfo authInfo)
        {

            //格式如下
            //var payload = new Dictionary<string, object>
            //{
            //    { "username","admin" },
            //    { "pwd", "claim2-value" }
            //};

            IJwtAlgorithm algorithm = new HMACSHA256Algorithm();
            IJsonSerializer serializer = new JsonNetSerializer();
            IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
            IJwtEncoder encoder = new JwtEncoder(algorithm, serializer, urlEncoder);


            var jwtcreated = Math.Round((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds + 5);
            var jwtcreatedOver = Math.Round((DateTime.UtcNow.AddDays(7) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds + 5);//TOKEN声明周期一周
            var payload = new Dictionary<string, dynamic>
                {
                    {"iss", authInfo.iss},//非必须。issuer 请求实体，可以是发起请求的用户的信息，也可是jwt的签发者。
                    {"iat", jwtcreated},//非必须。issued at。 token创建时间，unix时间戳格式
                    {"exp", jwtcreatedOver},//非必须。expire 指定token的生命周期。unix时间戳格式
                    {"aud", authInfo.aud},//非必须。接收该JWT的一方。
                    {"sub", authInfo.sub},//非必须。该JWT所面向的用户
                    {"jti", authInfo.jti},//非必须。JWT ID。针对当前token的唯一标识
                    {"userInfo",authInfo.userInfo}//自定义字段 用于存放当前登录人账户信息
                    //{"userId", authInfo.userId},//自定义字段 用于存放当前登录人账户信息
                    //{"userName", authInfo.userName},//自定义字段 用于存放当前登录人账户信息
                    //{"userPwd", authInfo.userPwd},//自定义字段 用于存放当前登录人登录密码信息
                    //{"userRole", authInfo.userRoles},//自定义字段 用于存放当前登录人登录权限信息
                };

            var token = encoder.Encode(payload, secret);
            return token;
        }

        /// <summary>
        /// 根据jwtToken  获取实体
        /// </summary>
        /// <param name="token">jwtToken</param>
        /// <returns></returns>
        public static AuthInfo GetJwtDecode(string token)
        {
            IJsonSerializer serializer = new JsonNetSerializer();
            IDateTimeProvider provider = new UtcDateTimeProvider();
            IJwtValidator validator = new JwtValidator(serializer, provider);
            IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
            IJwtDecoder decoder = new JwtDecoder(serializer, validator, urlEncoder);
            var userInfo = decoder.DecodeToObject<AuthInfo>(token, secret, verify: true);
            return userInfo;
        }
    }
}