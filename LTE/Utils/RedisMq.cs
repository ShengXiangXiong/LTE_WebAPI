using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTE.Utils
{
    public class RedisMq
    {
        static IDatabase db = RedisHelper.getInstance().db;
        public static ISubscriber subscriber = db.Multiplexer.GetSubscriber();
        public static long Pub(RedisChannel channel, RedisValue message)
        {
            return db.Publish(channel, message);
        }
    }
}
