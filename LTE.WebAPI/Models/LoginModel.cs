using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Collections;
using System.Data;

namespace LTE.WebAPI.Models
{
    public class LoginModel
    {
        /// <summary> 
        /// 用户名 
        /// </summary>   
        public string userName { get; set; }

        /// <summary> 
        /// 密码 
        /// </summary> 
        public string userPwd { get; set; }

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
                    return new Result(true);
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