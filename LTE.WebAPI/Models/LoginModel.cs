using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Collections;
using System.Data;
using LTE.WebAPI.Utils;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using StackExchange.Redis;
using LTE.Utils;
using System.Web.Http.Controllers;

namespace LTE.WebAPI.Models
{
    public class LoginModel
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int userId { get; set; }
        /// <summary> 
        /// 用户名 
        /// </summary>   
        public string userName { get; set; }

        /// <summary> 
        /// 密码 
        /// </summary> 
        public string userPwd { get; set; }

        /// <summary>
        /// 角色
        /// </summary>
        public string userRole { get; set; }

        /// <summary>
        /// 用户头像
        /// </summary>
        public string userFace { get; set; }

        //public LoginModel(string username)
        //{
        //    this.userName = userName;
        //}
        //public LoginModel(string username, string pwd)
        //{
        //    this.userName = username;
        //    this.userPwd = pwd;
        //}
        public LoginModel(){}

        public LoginModel(string username, string pwd)
        {
            this.userName = username;
            this.userPwd = pwd;
        }
        public override string ToString()
        {
            return string.Format("role:{2} userName:{0} passWord:{1}", this.userName, this.userPwd, this.userRole);
        }
        /// <summary>
        /// 检查用户是否合法
        /// </summary>
        /// <param name="name">用户名</param>
        /// <param name="pwd">密码</param>
        /// <returns>成功：true, ""; 失败：false, 提示信息</returns>
        public static Result CheckUser(string name, string pwd)
        {
            //return true;
            //判断用户名密码是否匹配
            try
            {
                IDatabase db = RedisHelper.getInstance().db;
                Hashtable ht = new Hashtable();
                ht["name"] = name;
                ht["pwd"] = pwd;
                DataTable dt = DB.IbatisHelper.ExecuteQueryForDataTable("getUser", ht);  // Ibatis 数据访问
                if (dt.Rows.Count == 0)
                {
                    return new Result(false, "用户不存在！");
                }
                else
                {
                    DataRow userInfoDt = dt.Rows[0];
                    //Regex rgx = new Regex(@"[,./?:'\]");
                    //System.Diagnostics.Debug.Write(userInfo["Role"]);

                    //LoginModel userInfo = new LoginModel{userName=(string)userInfoDt["userName"],userId= (int)userInfoDt["ID"],
                    //    userRoles =((string)userInfoDt["Role"]).Trim().Split(new char[] { ',', ' ', '/', ';','\\' })};

                    LoginModel userInfo = new LoginModel
                    {
                        userName = (string)userInfoDt["userName"],
                        userId = (int)userInfoDt["ID"],
                        userRole = (string)userInfoDt["Role"]
                    };

                    AuthInfo auth = new AuthInfo {userInfo=userInfo};
                    string token = JwtHelper.SetJwtEncode(auth);

                    db.StringSet("login"+token, JsonConvert.SerializeObject(userInfo));
                    db.KeyExpire("login" + token, DateTime.Now.AddDays(7));

                    return new Result { ok = true, code = "1", obj = userInfo, msg = "登陆成功", token = token };
                }
            }
            catch (System.Data.SqlClient.SqlException err)
            {
                if (err.Message.IndexOf("连接超时") != -1)
                {
                    return new Result(false, "连接超时");
                }
                else if (err.Message.IndexOf("侦听") != -1)
                {
                    return new Result(false, "侦听");
                }
                else
                {
                    return new Result(false, err.ToString());
                }
            }
            catch (System.Exception err)
            {
                return new Result(false, err.ToString());
            }
        }
    }
}