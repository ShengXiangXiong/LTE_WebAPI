using LTE.Utils;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LTE.WebAPI.Utils
{
    public class UIDHelper
    {
        /// <summary>
        /// 按时间生成uid
        /// </summary>
        /// <returns></returns>
        public static int genUIdByTime()
        {
            return Int32.Parse(string.Format("{0:yyyyMMddHH}", DateTime.Now));
        }
        /// <summary>
        /// 从redis中生成uid：步长为batch设置下一次uid的起始值，返回的是本次的起始id
        /// </summary>
        /// <returns></returns>
        public static long GenUIdByRedis(String dbName,long batch)
        {
            String prefix = "UID_";
            IDatabase db = RedisHelper.getInstance().db;
            return db.StringIncrement(prefix+dbName, batch) -batch;
        }
    }
}