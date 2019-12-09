using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LTE.Geometric;
using LTE.DB;
using System.Data;

namespace LTE.InternalInterference
{
    // 用于系数校正
    public class RayNode
    {
        public int cellid;               // 小区ID
        public double startPwrW;         // 初始发射功率，单位w
        public double recePwrW;          // 接收功率，单位w
        public List<NodeInfo> rayList;   // 射线列表
    }

    // 用于系数校正
    public class RayHelper
    {
        public static HashSet<string> tbDTgrids;
        private static RayHelper instance = null;
        private static object syncRoot = new object();

        public static RayHelper getInstance()
        {
            if (instance == null)
            {
                lock (syncRoot)
                {
                    if (instance == null)
                    {
                        instance = new RayHelper();

                        tbDTgrids = new HashSet<string>();
                        getGrids1();
                    }
                }
            }
            return instance;
        }

        public bool ok(string key) // 栅格是否位于路测路径中
        {
            if (tbDTgrids.Count == 0)
                return false;
            return tbDTgrids.Contains(key);
        }

        private static void getGrids1()
        {
            DataTable tb = IbatisHelper.ExecuteQueryForDataTable("GetDTgrids", null);
            foreach (DataRow dataRow in tb.Rows)
            {
                int gxid = int.Parse(dataRow["gxid"].ToString());
                int gyid = int.Parse(dataRow["gyid"].ToString());
                string id = string.Format("{0},{1},{2}", gxid, gyid, 0);
                tbDTgrids.Add(id);
            }
        }
    }

    // 用于定位 2018.12.18
    public class RaysNode
    {
        public double emitPwrDbm;
        public double recvPwrDbm;
        public List<NodeInfo> rayList;   // 射线列表
    }
}
