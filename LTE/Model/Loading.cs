using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LTE.Model
{
    public class LoadInfo
    {
        public int UserId { get; set; }
        public string taskName { get; set; }
        public int cnt { get; set; }
        public int count { get; set; }
        public Dictionary<string,int> rayCount { get; set; }
    }
    public class Loading
    {
        private static Loading _instance = null;
        private ConcurrentDictionary<int, ConcurrentDictionary<string,LoadInfo>> loadMap;

        private Loading()
        {
        }
        public static Loading getInstance()
        {
            if (_instance == null)
            {
                _instance = new Loading();
                _instance.loadMap = new ConcurrentDictionary<int, ConcurrentDictionary<string, LoadInfo>>();
            }
            return _instance;
        }
        public ConcurrentDictionary<int, ConcurrentDictionary<string, LoadInfo>> getLoadInfo()
        {
            return loadMap;
        }

        /// <summary>
        /// count 统计（并行操作）
        /// </summary>
        /// <param name="loadInfo"></param>
        public void addCount(LoadInfo loadInfo)
        {
            if (loadMap != null && loadMap.ContainsKey(loadInfo.UserId) && loadMap[loadInfo.UserId].ContainsKey(loadInfo.taskName))
            {
                loadMap[loadInfo.UserId][loadInfo.taskName].count += loadInfo.count;
            }
        }

        /// <summary>
        /// 只用于更新cnt，不能更新count，count由setCount确定
        /// </summary>
        /// <param name="loadInfo"></param>
        public void updateLoading(LoadInfo loadInfo)
        {
            if (!loadMap.ContainsKey(loadInfo.UserId))
            {
                var tmp = new ConcurrentDictionary<string, LoadInfo>();
                tmp[loadInfo.taskName] = loadInfo;
                loadMap[loadInfo.UserId] = tmp;
            }
            else
            {
                if (!loadMap[loadInfo.UserId].ContainsKey(loadInfo.taskName))
                {
                    loadMap[loadInfo.UserId][loadInfo.taskName] = loadInfo;
                }
                else
                {
                    loadMap[loadInfo.UserId][loadInfo.taskName].cnt = loadInfo.cnt;
                }
            }

        }

        //public void updateLoading(int userId,string taskName,int cnt,int count)
        //{
        //    if (!loadMap.ContainsKey(userId))
        //    {
        //        var tmp = new List<LoadInfo>();
        //        tmp.Add(new LoadInfo { UserId = userId, taskName = taskName, cnt = cnt, count = count });
        //        loadMap[userId] = tmp;

        //    }
        //    else
        //    {
        //        var tmp = loadMap[userId];
        //        foreach (var item in tmp)
        //        {
        //            if (item != null && item.taskName.Equals(taskName))
        //            {
        //                item.cnt = cnt;
        //                item.count = count;
        //                break;
        //            }
        //        }
        //        loadMap[userId] = tmp;
        //    }

        //}
        //public void updateLoading(int userId, string taskName, int cnt)
        //{
        //    var tmp = loadMap[userId];
        //    foreach (var item in tmp)
        //    {
        //        if (item != null && item.taskName.Equals(taskName))
        //        {
        //            item.cnt = cnt;
        //            break;
        //        }
        //    }
        //    loadMap[userId] = tmp;
        //}

    }
}