using LTE.Utils;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace LTE.Model
{
    public class LoadInfo
    {
        public static ThreadLocal<int> UserId = new ThreadLocal<int>();
        public static ThreadLocal<string> taskName = new ThreadLocal<string>();
        public int cnt { get; set; }
        public int count { get; set; }
        public Dictionary<string, int> rayCount { get; set; }
        //表示当前任务是否已经崩溃
        public bool breakdown { get; set; }
        public bool finish { get; set; }


        private static IDatabase db = RedisHelper.getInstance().db;

        public int getCnt()
        {
            return (int)db.HashGet(UserId.Value.ToString() + ":" + taskName.Value, "cnt");
        }

        public void loadCreate()
        {
            db.SetAdd("Task:" + UserId.Value, taskName.Value);
            HashEntry[] hashEntries = new HashEntry[6];
            hashEntries[0] = new HashEntry("UserId", UserId.Value);
            hashEntries[1] = new HashEntry("taskName", taskName.Value);
            hashEntries[2] = new HashEntry("cnt", 0);
            hashEntries[3] = new HashEntry("count", this.count);
            hashEntries[4] = new HashEntry("breakdown", false);
            hashEntries[5] = new HashEntry("finish", false);
            db.HashSet(UserId.Value.ToString() + ":" + taskName.Value, hashEntries);
            db.KeyExpire(UserId.Value.ToString() + ":" + taskName.Value, DateTime.Now.AddDays(7));
        }
        public void loadUpdate()
        {
            db.HashSet(UserId.Value.ToString() + ":" + taskName.Value, "cnt", this.cnt);
        }
        public void loadHashAdd(int cnt)
        {
            //db.HashSet(UserId.Value.ToString() + ":" + taskName.Value, "cnt", this.cnt);
            db.HashIncrement(UserId.Value.ToString() + ":" + taskName.Value, "cnt", cnt);
        }
        public void loadCountAdd(int count)
        {
            //int tmp = (int)db.HashGet(UserId.Value.ToString() + ":" + taskName.Value, "count");
            //db.HashSet(UserId.Value.ToString() + ":" + taskName.Value, "count", this.count+tmp);
            db.HashIncrement(UserId.Value.ToString() + ":" + taskName.Value, "count", count);
        }
        public void loadFinish()
        {
            db.HashSet(UserId.Value.ToString() + ":" + taskName.Value, "finish", true);
        }
        public void loadBreakDown()
        {
            db.HashSet(UserId.Value.ToString() + ":" + taskName.Value, "breakdown", true);
        }
        public List<HashEntry[]> GetLoadInfos()
        {
            RedisValue[] taskNames = db.SetMembers("Task:" + UserId.Value);
            List<HashEntry[]> res = new List<HashEntry[]>();
            foreach (var item in taskNames)
            {
                res.Add(db.HashGetAll(UserId.Value + ":" + item));
            }
            return res;
        }
    }
    //public class Loading
    //{
    //    private static Loading _instance = null;
    //    private ConcurrentDictionary<int, ConcurrentDictionary<string, LoadInfo>> loadMap;

    //    private Loading()
    //    {
    //    }
    //    public static Loading getInstance()
    //    {
    //        if (_instance == null)
    //        {
    //            _instance = new Loading();
    //            _instance.loadMap = new ConcurrentDictionary<int, ConcurrentDictionary<string, LoadInfo>>();
    //        }
    //        return _instance;
    //    }
    //    public ConcurrentDictionary<int, ConcurrentDictionary<string, LoadInfo>> getLoadInfo()
    //    {
    //        return loadMap;
    //    }

    //    /// <summary>
    //    /// count 统计（并行操作）
    //    /// </summary>
    //    /// <param name="loadInfo"></param>
    //    public void addCount(LoadInfo loadInfo)
    //    {
    //        if (loadMap != null && loadMap.ContainsKey(loadInfo.UserId) && loadMap[loadInfo.UserId].ContainsKey(loadInfo.taskName))
    //        {
    //            loadMap[loadInfo.UserId][loadInfo.taskName].count += loadInfo.count;
    //        }
    //    }

    //    /// <summary>
    //    /// 只用于更新cnt，不能更新count，count由setCount确定
    //    /// </summary>
    //    /// <param name="loadInfo"></param>
    //    public void updateLoading(LoadInfo loadInfo)
    //    {
    //        if (!loadMap.ContainsKey(loadInfo.UserId))
    //        {
    //            var tmp = new ConcurrentDictionary<string, LoadInfo>();
    //            tmp[loadInfo.taskName] = loadInfo;
    //            loadMap[loadInfo.UserId] = tmp;
    //        }
    //        else
    //        {
    //            if (!loadMap[loadInfo.UserId].ContainsKey(loadInfo.taskName))
    //            {
    //                loadMap[loadInfo.UserId][loadInfo.taskName] = loadInfo;
    //            }
    //            else
    //            {
    //                loadMap[loadInfo.UserId][loadInfo.taskName].cnt = loadInfo.cnt;
    //            }
    //        }

    //    }

    //    //public void updateLoading(int userId,string taskName,int cnt,int count)
    //    //{
    //    //    if (!loadMap.ContainsKey(userId))
    //    //    {
    //    //        var tmp = new List<LoadInfo>();
    //    //        tmp.Add(new LoadInfo { UserId = userId, taskName = taskName, cnt = cnt, count = count });
    //    //        loadMap[userId] = tmp;

    //    //    }
    //    //    else
    //    //    {
    //    //        var tmp = loadMap[userId];
    //    //        foreach (var item in tmp)
    //    //        {
    //    //            if (item != null && item.taskName.Equals(taskName))
    //    //            {
    //    //                item.cnt = cnt;
    //    //                item.count = count;
    //    //                break;
    //    //            }
    //    //        }
    //    //        loadMap[userId] = tmp;
    //    //    }

    //    //}
    //    //public void updateLoading(int userId, string taskName, int cnt)
    //    //{
    //    //    var tmp = loadMap[userId];
    //    //    foreach (var item in tmp)
    //    //    {
    //    //        if (item != null && item.taskName.Equals(taskName))
    //    //        {
    //    //            item.cnt = cnt;
    //    //            break;
    //    //        }
    //    //    }
    //    //    loadMap[userId] = tmp;
    //    //}

    //}
}