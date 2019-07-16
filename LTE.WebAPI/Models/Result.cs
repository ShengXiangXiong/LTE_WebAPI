using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

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
    }
}