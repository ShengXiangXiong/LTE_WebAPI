using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using LTE.Model;
using LTE.DB;
using System.Data;
using ESRI.ArcGIS.Geometry;

namespace LTE.InternalInterference.Grid
{
    /// <summary>
    /// 立体加速栅格类
    /// </summary>
    public class AccelerateStruct
    {
        //输入数据：覆盖范围的栅格编号
        private static int minGXID = -1;
        private static int maxGXID = -1;
        private static int minGYID = -1;
        private static int maxGYID = -1;
        //立体栅格加速结构
        public static Dictionary<string, List<int>> accgrids = new Dictionary<string, List<int>>();

        // 2019.3.25 立体栅格的所属场景
        public static Dictionary<string, int> gridScene = new Dictionary<string, int>(); 

        /// <summary>
        /// 设置加速栅格二维边界
        /// </summary>
        /// <param name="mingxid"></param>
        /// <param name="mingyid"></param>
        /// <param name="maxgxid"></param>
        /// <param name="maxgyid"></param>
        public static void setAccGridRange(int mingxid, int mingyid, int maxgxid, int maxgyid)
        {
            minGXID = mingxid;
            minGYID = mingyid;
            maxGXID = maxgxid;
            maxGYID = maxgyid;
        }

        /// <summary>
        /// 检查计算栅格是否在设置区域中
        /// </summary>
        /// <param name="gxid"></param>
        /// <param name="gyid"></param>
        /// <returns></returns>
        public static bool checkInRange(int gxid, int gyid)
        {
            return (gxid >= minGXID && gxid <= maxGXID && gyid >= minGYID && gyid <= maxGYID);
        }

        /// <summary>
        /// 清空加速结构
        /// </summary>
        public static void clearAccelerateStruct()
        {
            accgrids.Clear();
        }

        /// <summary>
        ///从数据库表tbAccelerateGridBuilding中取出所有符合条件的数据,并以GXID,GYID,GZID排序, 构造结果集的哈希表
        /// </summary>
        /// <returns></returns>
        public static void constructAccelerateStruct()
        {
            Hashtable ht = new Hashtable();
            ht["minGXID"] = minGXID;
            ht["maxGXID"] = maxGXID;
            ht["minGYID"] = minGYID;
            ht["maxGYID"] = maxGYID;

            DataTable dt = IbatisHelper.ExecuteQueryForDataTable("GetAccelerateStruct", ht);

            List<int> bid;//哈希表中每一个key值（栅格）对应的建筑物id列表
            for (int i = 0; i < dt.Rows.Count; i++)//按行遍历DataTable
            {
                string key = dt.Rows[i][0].ToString() + "," + dt.Rows[i][1].ToString() + "," + dt.Rows[i][2].ToString();

                if (!accgrids.ContainsKey(key))
                {//若key不存在，创建新的键值对
                    bid = new List<int>();
                    bid.Add(Convert.ToInt32(dt.Rows[i][3]));
                    accgrids.Add(key, bid);
                }
                else
                {//若key存在，更新键值对
                    accgrids[key].Add(Convert.ToInt32(dt.Rows[i][3]));//这样可以更新成功吗？
                }
            }

            // 2019.3.25  场景记录
            ht["minGXID"] = minGXID - 300;
            ht["maxGXID"] = maxGXID + 300;
            ht["minGYID"] = minGYID - 300;
            ht["maxGYID"] = maxGYID + 300;
            DataTable dt1 = IbatisHelper.ExecuteQueryForDataTable("GetAgridScene", ht);
            for (int i = 0; i < dt1.Rows.Count; i++)//按行遍历DataTable
            {
                string key = dt1.Rows[i][0].ToString() + "," + dt1.Rows[i][1].ToString() + "," + dt1.Rows[i][2].ToString();
                gridScene[key] = Convert.ToInt32(dt1.Rows[i]["Scene"]);
            }
        }

        public static int getDataMemory()
        {
            int ret = 0;
            IDictionaryEnumerator de = accgrids.GetEnumerator();
            while (de.MoveNext())
            {
                ret += de.Key.ToString().ToCharArray().Length * 2;
                ret += ((List<int>)de.Value).Count * 4;
            }
            return ret;
        }

        /// <summary>
        /// 获取加速栅格内的建筑物id列表，需先调用checkInRange
        /// </summary>
        /// <param name="gxid"></param>
        /// <param name="gyid"></param>
        /// <param name="gzid"></param>
        /// <returns></returns>
        public static List<int> getAccelerateStruct(int gxid, int gyid, int gzid)
        {
            string key = gxid + "," + gyid + "," + gzid;
            if (accgrids.ContainsKey(key))
            {
                return accgrids[key];
            }
            return null;
        }

    }

}
