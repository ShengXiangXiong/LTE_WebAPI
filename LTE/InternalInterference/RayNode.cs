using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LTE.Geometric;
using LTE.DB;
using System.Data;
using LTE.InternalInterference.Grid;
using System.Collections;

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

        public static RayHelper getInstance(int eNodeBID, double radius)
        {
            if (instance == null)
            {
                lock (syncRoot)
                {
                    if (instance == null)
                    {
                        instance = new RayHelper();

                        tbDTgrids = new HashSet<string>();

                        getGridsForACell(eNodeBID, radius);
                    }
                }
            }
            return instance;
        }

        //用于每次生成一个小区的路测数据后，清空路测栅格实例，jinhj
        public static void clearInstance() {
            instance = null;
            tbDTgrids.Clear();
        }

        public bool ok(string key) // 栅格是否位于路测路径中
        {
            if (tbDTgrids.Count == 0)
                return false;
            return tbDTgrids.Contains(key);
        }

        //旧版读取所有路测代码，目前不可用
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

        //从真实路测获取所有主小区为该小区的路测点，将经纬坐标转换为栅格id，jinhj
        private static void getGrids(int cellIndex) {
            Hashtable ht = new Hashtable();
            ht["cellIndex"] = cellIndex;
            DataTable tb = IbatisHelper.ExecuteQueryForDataTable("GetAllDTGridsOfACell", ht);
            foreach (DataRow dataRow in tb.Rows)
            {
                double x = double.Parse(dataRow["x"].ToString());
                double y = double.Parse(dataRow["y"].ToString());
                int gxid = -1;
                int gyid = -1;
                GridHelper.getInstance().XYToGGrid(x, y, ref gxid, ref gyid);
                string id = string.Format("{0},{1},{2}", gxid, gyid, 0);


                tbDTgrids.Add(id);
            }
        }


        //从真实路测获得某个小区某个半径范围内的路测点，将经纬坐标转换为栅格id，jinhj
        private static void getGridsForACell(int eNodeBID, double radius) {
            Hashtable ht = new Hashtable();
            ht["eNodeBID"] = eNodeBID;
            ht["radius"] = radius;
            DataTable tb = IbatisHelper.ExecuteQueryForDataTable("GetDTGridsOfACell", ht);
            foreach (DataRow dataRow in tb.Rows)
            {
                double x= double.Parse(dataRow["x"].ToString());
                double y= double.Parse(dataRow["y"].ToString());
                int gxid = -1;
                int gyid = -1;
                GridHelper.getInstance().XYToGGrid(x, y, ref gxid, ref gyid);
                string id = string.Format("{0},{1},{2}", gxid, gyid, 0);
                tbDTgrids.Add(id);
            }

            //测试，查看原始路测数目、及栅格数目，以查看是否有多个路测位于同一个栅格，jinhj
            int tbDTCnt = tb.Rows.Count;
            int tbDTgridsCnt = tbDTgrids.Count;
            int check = 1;
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
