using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LTE.WebAPI.Models
{
    public class Result
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool ok{get; set;}

        /// <summary>
        /// 结果信息
        /// </summary>
        public string msg{get; set;}

        /// <summary>
        /// 返回的对象信息
        /// </summary>

        public object obj{ get; set; }
        /// <summary>
        /// 返回具体业务的状态码
        /// </summary>
        public string code { get; set; }
        /// <summary>
        /// token验证字符串
        /// </summary>

        public string token { get; set; }

        public Result() { }

        public Result(bool ok1)
        {
            ok = ok1;
            msg = "";
        }
        public Result(bool ok1, string msg1)
        {
            ok = ok1;
            msg = msg1;
        }

        public Result(bool ok1, string msg1, Object obj1)
        {
            ok = ok1;
            msg = msg1;
            obj = obj1;
        }

        public Result(bool ok1, string msg1, Object obj1, string code1)
        {
            ok = ok1;
            msg = msg1;
            obj = obj1;
            code = code1;
        }
        public Result(bool ok1, string msg1, Object obj1, string code1,string token1)
        {
            ok = ok1;
            msg = msg1;
            obj = obj1;
            code = code1;
            token = token1;
        }
    }
}