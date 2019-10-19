using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Management;
using LTE.InternalInterference;
using System.Text;
using System.Data;
using LTE.Calibration;

namespace LTE.WebAPI.Models
{
    // 系数校正
    public class CalibrationModel
    {
        /// <summary>
        /// 路测开始时间
        /// </summary>
        public string startDateTime{ get; set; }  

        /// <summary>
        /// 路测结束时间
        /// </summary>
        public string endDateTime{ get; set; } 

        /// <summary>
        /// 场景数量
        /// </summary>
        public int sceneNum { get; set; }  

        /// <summary>
        /// 遗传算法中的种群大小
        /// </summary>
        public int popSize{ get; set; } 

        /// <summary>
        /// 遗传算法中的进化代数
        /// </summary>
        public int gen{ get; set; }  

        /// <summary>
        /// 多场景系数校正
        /// </summary>
        /// <returns></returns>
        public Result calilbrate()
        {
            #region 防止内存不够

            double capacity = MemoryInfo() / 20;

            // 不要超过的射线条数
            int size = 350; // 一条射线约占350字节;
            int maxRayNum = (int)(capacity * (1073741824 / size));

            // 不要超过的路测栅格数量
            int maxGridNum = maxRayNum / 200;  // 假设一个栅格收到来自 40 个小区的信号，每个信号有 5 条射线

            Hashtable para = new Hashtable();
            para["startDateTime"] = startDateTime;
            para["endDateTime"] = endDateTime;
            // 由于没有真实路测，先假设 tbDT 中是真实路测
            DataTable tb1 = DB.IbatisHelper.ExecuteQueryForDataTable("getGridsNum", para);
            if (tb1.Rows.Count < 1)
            {
                return new Result(false, "不存在位于以上时间段的路测数据！");
            }

            DataTable tb;

            // 内存控制，这里仅对射线计算结果进行了内存控制，如果有真实路测，同样需要对真实路测进行内存控制
            int cnt = Convert.ToInt32(tb1.Rows[0][0]);
            if (cnt > maxGridNum)
            {
                int b = (int)Math.Ceiling((double)cnt / maxGridNum);
                if (b == 1)
                    b = 2;
                Hashtable para1 = new Hashtable();
                para1["mod"] = b;
                tb = DB.IbatisHelper.ExecuteQueryForDataTable("getRays1", para1);
            }
            else
            {
                tb = DB.IbatisHelper.ExecuteQueryForDataTable("getRays", null);
            }
            #endregion

            // EARFCN，绝对频率号，与小区工参表 cell 中的数据保持一致
            int frequence = 63;  

            // 真实路测，这里是根据射线轨迹得到的路测加上随机扰动的结果，为模拟路测
            Dictionary<string, double> meaPwr = new Dictionary<string, double>();

            // 射线计算结果
            Dictionary<string, TrajInfo> rayDic = new Dictionary<string, TrajInfo>();

            filter(ref tb, ref meaPwr, ref rayDic); // 选择射线跟踪计算结果和真实路测的公共部分

            // 遗传算法
            EA ea = new EA();
            EA.initEA(this.popSize, this.gen, this.sceneNum, ref meaPwr, ref rayDic, frequence);
            ea.GaMain();

            return new Result(true);
        }

        // 选择射线跟踪计算结果和真实路测的公共部分
        private void filter(ref DataTable tb, ref Dictionary<string, double> meaPwr, ref Dictionary<string, TrajInfo> rayDic)
        {
            // 筛选之前
            Dictionary<string, TrajInfo> rayDicOri = CalRays.buildingGrids(ref tb);  // 根据射线跟踪结果算出的路测

            // 本应为真实路测，这里为模拟路测，如果用实际路测，同样需要进行内存控制
            Dictionary<string, double> meaPwrOri = CalRays.getMeaPwr(ref rayDicOri, this.sceneNum);

            #region 转换  key: 栅格, value: 小区ID
            Dictionary<string, List<int>> rayID = new Dictionary<string, List<int>>();
            Dictionary<string, List<int>> meaID = new Dictionary<string, List<int>>();

            foreach (string key in rayDicOri.Keys)
            {
                string[] k = key.Split(',');
                string id = k[1] + "," + k[2];
                if (rayID.Keys.Contains(id))
                {
                    rayID[id].Add(Convert.ToInt32(k[0]));
                }
                else
                {
                    rayID[id] = new List<int>();
                    rayID[id].Add(Convert.ToInt32(k[0]));
                }
            }

            foreach (string key in meaPwrOri.Keys)
            {
                string[] k = key.Split(',');
                string id = k[1] + "," + k[2];
                if (meaID.Keys.Contains(id))
                {
                    meaID[id].Add(Convert.ToInt32(k[0]));
                }
                else
                {
                    meaID[id] = new List<int>();
                    meaID[id].Add(Convert.ToInt32(k[0]));
                }
            }
            #endregion

            #region 筛选
            HashSet<string> keys = new HashSet<string>();
            foreach (string key in meaID.Keys)
            {
                if (!rayID.Keys.Contains(key))
                    continue;

                List<int> list = meaID[key].Intersect(rayID[key]).ToList();  // 得到两者的公共小区

                // 一个栅格会收到来自多个小区的信号
                // 如果两者的公共小区数<3，都选；否则，选一个真实路测最强的，一个射线计算最强的，一个两者差值最大的
                if (list.Count <= 3)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        string k = string.Format("{0},{1}", list[i], key);
                        keys.Add(k);
                    }
                }
                else
                {
                    double max = Double.MinValue;
                    string maxK = "";
                    double max1 = Double.MinValue;
                    string maxK1 = "";
                    double max2 = Double.MinValue;
                    string maxK2 = "";
                    for (int i = 0; i < list.Count; i++)
                    {
                        string k = string.Format("{0},{1}", list[i], key);

                        // 真实路测最强的
                        if (meaPwrOri[k] > max)
                        {
                            max = meaPwrOri[k];
                            maxK = k;
                        }

                        // 射线计算最强的
                        if (rayDicOri[k].sumPwrDbm > max1)
                        {
                            max1 = rayDicOri[k].sumPwrDbm;
                            maxK1 = k;
                        }

                        // 两者差值最大的
                        double tmp = Math.Abs(rayDicOri[k].sumPwrDbm - meaPwrOri[k]);
                        if (tmp > max2)
                        {
                            max2 = tmp;
                            maxK2 = k;
                        }
                    }
                    keys.Add(maxK);
                    keys.Add(maxK1);
                    keys.Add(maxK2);
                }
            }
            #endregion

            // 筛选后
            rayDic = new Dictionary<string, TrajInfo>();
            meaPwr = new Dictionary<string, double>();
            foreach (string key in keys)
            {
                rayDic[key] = rayDicOri[key];
                meaPwr[key] = meaPwrOri[key];  // 模拟路测
            }
        }
        internal enum WmiType
        {
            Win32_Processor,
            Win32_PerfFormattedData_PerfOS_Memory,
            Win32_PhysicalMemory,
            Win32_NetworkAdapterConfiguration,
            Win32_LogicalDisk
        }

        /// <summary>
        /// 获取内存信息
        /// </summary>
        /// <returns></returns>
        public double MemoryInfo()
        {
            StringBuilder sr = new StringBuilder();
            long capacity = 0;
            Dictionary<string, ManagementObjectCollection> WmiDict =
                new Dictionary<string, ManagementObjectCollection>();

            var names = Enum.GetNames(typeof(WmiType));
            foreach (string name in names)
            {
                WmiDict.Add(name, new ManagementObjectSearcher("SELECT * FROM " + name).Get());
            }

            var query = WmiDict[WmiType.Win32_PhysicalMemory.ToString()];
            int index = 1;
            foreach (var obj in query)
            {
                sr.Append("内存" + index + "频率:" + obj["ConfiguredClockSpeed"] + ";");
                capacity += Convert.ToInt64(obj["Capacity"]);
                index++;
            }
            sr.Append("总物理内存:");
            capacity /= 1073741824;
            sr.Append(capacity + "G;");
            Console.WriteLine(sr);
            return capacity;
        }
    }
}