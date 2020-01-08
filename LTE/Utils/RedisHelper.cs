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
                var config = new ConfigurationOptions
                {
                    AbortOnConnectFail = false,
                    AllowAdmin = true,
                    ConnectTimeout = 15000,
                    SyncTimeout = 5000,
                    ResponseTimeout = 15000,
                    EndPoints = { redis.addr }
                };
                redis.conn = ConnectionMultiplexer.Connect(config);
                redis.db = redis.conn.GetDatabase();
            }
            return redis;
        }
    }
}