using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using StackExchange.Redis;

namespace LTE.Utils
{
    public class RedisHelper
    {
        private string addr;
        private ConnectionMultiplexer conn;
        private static RedisHelper redis;
        public IDatabase db;

        private RedisHelper() { }

        public static RedisHelper getInstance(string url = "localhost", string port = "6379")
        {
            if(redis is null)
            {
                redis = new RedisHelper();
                redis.addr = url + ":" + port;
                redis.conn = ConnectionMultiplexer.Connect(redis.addr);
                redis.db = redis.conn.GetDatabase();
            }
            return redis;
        }
    }
}