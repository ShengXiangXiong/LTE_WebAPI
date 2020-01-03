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
using LTE.DB;
using System.IO;
using LTE.Geometric;
using LTE.Model;
using LTE.Utils;

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
            //进度类实例
            LoadInfo loadInfo = new LoadInfo();

            try {
                //初始条件检查
                if (!DataCheck.checkInitFinished())
                {
                    loadInfo.breakdown = true;
                    loadInfo.loadBreakDown();
                    return new Result(false, "系数校正射线计算失败，请先完成场景建模。");
                }

                #region 得到轨迹数据（会进行内存控制）

                double capacity = MemoryInfo() / 20;

                // 不要超过的射线条数（待核实）
                int size = 350; // 一条射线约占350字节;
                int maxRayNum = (int)(capacity * (1073741824 / size));

                // 对轨迹数据进行内存控制：比较tbRayAdj射线数与控制的最大射线条数，若超过，则对读取tbRayAdj的数据量进行内存控制（取模）
                DataTable rayAdjGridsCntTb = DB.IbatisHelper.ExecuteQueryForDataTable("getRayAdjNum", null);
                if (rayAdjGridsCntTb.Rows.Count < 1)
                {
                    return new Result(false, "不存在轨迹数据！");
                }
                int rayAdjGridsCnt = Convert.ToInt32(rayAdjGridsCntTb.Rows[0][0]);

                DataTable rayAdjTb;
                if (rayAdjGridsCnt > maxRayNum)
                {
                    int mod = (int)Math.Ceiling((double)rayAdjGridsCnt / maxRayNum);
                    if (mod == 1)
                        mod = 2;
                    Hashtable para1 = new Hashtable();
                    para1["mod"] = mod;
                    rayAdjTb = DB.IbatisHelper.ExecuteQueryForDataTable("getSomeRaysByModInAdjRange", para1);
                }
                else
                {
                    rayAdjTb = DB.IbatisHelper.ExecuteQueryForDataTable("getRaysInAdjRange", null);
                }

                //对路测数据进行内存控制
                int maxGridNum = maxRayNum / 200;  // 不要超过的路测栅格数量。假设一个栅格收到来自 40 个小区的信号，每个信号有 5 条射线（待核实）
                int maxdTCnt = maxGridNum * 2; //假设路测点数是栅格数的二倍

                // 获得轨迹数据包含的小区集合对应的路测数据的条数
                List<String> cellIdList = new List<String>();
                DataTable rayAdjCells = DB.IbatisHelper.ExecuteQueryForDataTable("getRayAdjCells", null);
                for (int i = 0; i < rayAdjCells.Rows.Count; ++i)
                {
                    cellIdList.Add(rayAdjCells.Rows[i][0].ToString());
                }

                DataTable dTCntTb = DB.IbatisHelper.ExecuteQueryForDataTable("getDTNum", null);//todo 更改为查小区集合内的路测数据数量,根据cellIdList
                                                                                               //if (dTCntTb.Rows.Count < 1)
                                                                                               //{
                                                                                               //    return new Result(false, "不存在轨迹数据对应小区的路测数据！");
                                                                                               //}
                                                                                               //int tbDTCnt = Convert.ToInt32(dTCntTb.Rows[0][0]);

                DataTable tbDTTb;
                //if (tbDTCnt > maxdTCnt)
                //{
                //    int mod = (int)Math.Ceiling((double)tbDTCnt / maxdTCnt);
                //    if (mod == 1)
                //        mod = 2;
                //    Hashtable para = new Hashtable();
                //    para["mod"] = mod;
                //    tbDTTb = DB.IbatisHelper.ExecuteQueryForDataTable("getSomeCellDTByMod", para);// todo sql未写
                //}
                //else
                //{
                //tbDTTb = DB.IbatisHelper.ExecuteQueryForDataTable("getCellDT", null);//todo 更改为查小区集合内的路测数据

                //查询tbRayAdjRange中接收信号强度高于-100dbm的路测点
                tbDTTb = DB.IbatisHelper.ExecuteQueryForDataTable("getCellDTInAdjRangeOverNegative100", null);
                //}

                #endregion

                // 得到计算值和真实路测的公共部分
                Dictionary<string, List<DTInfo>> dtInfoDic = new Dictionary<string, List<DTInfo>>();// 路测数据
                Dictionary<string, TrajInfo> rayDic = new Dictionary<string, TrajInfo>();// 计算值
                #region //测试用,一个点
                Dictionary<string, List<DTInfo>> dtPwrDicTest = new Dictionary<string, List<DTInfo>>();//测试用的路测数据
                Boolean tag = true;
                foreach (string key in dtInfoDic.Keys)
                {
                    if (tag)
                    {
                        dtPwrDicTest[key] = new List<DTInfo>();
                        foreach (DTInfo point in dtInfoDic[key])
                        {
                            if (!dtPwrDicTest.ContainsKey(key))
                            {
                                dtPwrDicTest[key] = new List<DTInfo>();
                            }
                            dtPwrDicTest[key].Add(point);
                        }
                        tag = false;
                        break;
                    }
                }
                #endregion
                filterWithDTInfo(ref rayAdjTb, ref tbDTTb, ref dtInfoDic, ref rayDic);

                // 遗传算法
                int frequence = 63;// EARFCN，绝对频率号，与小区工参表 cell 中的数据保持一致
                                   //根据轨迹中射线信息计算每条线段对应的转角
                calcDefAngle(ref rayDic);
                // ---------------- start ---------------------
                bool isFilterGrids_withCondition = true;//进行筛选
                int gridsType = 0;//筛选出只含直射的栅格 

                if (isFilterGrids_withCondition)
                {
                    filterGrids_withCondition(ref rayDic, gridsType);
                    int test = 0;
                }
                else
                {
                    filterGrids_withCondition(ref rayDic, -1);
                }
                // ----------------- end -----------------------
                int type0LowerBound = 10;//200; // 400;//控制第一轮实际代数
                int type0HigherBound = 300;//250; // 550;
                double ratio = 0.5;//0.5;//第一轮代数占用户输入代数的比例
                int popSizeType;
                int genType;

                // 初始化进度信息
                int round1Cnt = Math.Min(type0HigherBound, Math.Max(type0LowerBound, (int)(this.gen * ratio)));
                int round2Cnt = (int)(this.gen * (1 - ratio));
                loadInfo.loadCountAdd(round1Cnt + round2Cnt);

                //分别跑两轮
                for (int runTypeTag = 0; runTypeTag <= 1; ++runTypeTag)//0:跑所有栅格 1：去除高误差栅格 2：修正高误差栅格路经
                {
                    if (runTypeTag == 0)
                    {
                        popSizeType = this.popSize;
                        genType = round1Cnt;

                    }
                    else
                    {
                        popSizeType = this.popSize;
                        genType = round2Cnt;
                    }

                    EA.initEA(popSizeType, genType, this.sceneNum, ref dtInfoDic, ref rayDic, frequence);
                    EA ea = new EA();
                    ea.GaMain(runTypeTag, loadInfo);
                }
                loadInfo.loadFinish();
                return new Result(true, "系数校正射线计算完成");
            }
            catch (Exception)
            {
                loadInfo.breakdown = true;
                loadInfo.loadBreakDown();
                return new Result(false, "系数校正射线计算失败");
            }
        }

        //对轨迹中所有射线段的转角赋值
        public void filterGrids_withCondition(ref Dictionary<string, TrajInfo> rayDic, int GridsType)
        {
            //
            //GridsType：
            //       -1 所有的多种混合
            //       0  只包含直射；
            //       1  只包含反射轨迹；
            //       2  只包含绕射轨迹；

            if (-1 == GridsType) {
                foreach (string key in rayDic.Keys)
                { //遍历每个栅格
                   EA.GridsOfSpecialType[key] = 0;
                    
                }
            }
            if (0 == GridsType) {
                foreach (string key in rayDic.Keys)
                { //遍历每个栅格
                    TrajInfo trajsOfGrid = rayDic[key];
                    Boolean isZhiShe = true;//判断只含直射

                    // 遍历每个栅格内轨迹 
                    foreach (int trajId in trajsOfGrid.traj.Keys)
                    {
                        RayInfo curTraj = trajsOfGrid.traj[trajId];
                        

                        // 遍历每个轨迹的射线段
                        for (int j = 0; j < curTraj.rayList.Count; ++j)  // 对于一条轨迹中的每条射线
                        {
                           
                            NodeInfo nodeCur = curTraj.rayList[j];//当前射线段


                            if (nodeCur.rayType == RayType.VReflection || nodeCur.rayType == RayType.HReflection)
                            {
                                isZhiShe = false;
                            }
                            else if (nodeCur.rayType == RayType.HDiffraction || nodeCur.rayType == RayType.VDiffraction)
                            {
                                isZhiShe = false;
                            }


                        }

                    }

                    if (isZhiShe)
                    {
                        EA.GridsOfSpecialType[key] = 0;
                    }
                }
            }        
        }

        //对轨迹中所有射线段的转角赋值
        public void calcDefAngle(ref Dictionary<string, TrajInfo> rayDic)
        {
            foreach (string key in rayDic.Keys)
            { //遍历每个栅格
                TrajInfo trajsOfGrid = rayDic[key];
                // 遍历每个栅格内轨迹 
                foreach (int trajId in trajsOfGrid.traj.Keys)
                {
                    RayInfo curTraj = trajsOfGrid.traj[trajId];
                    // 遍历每个轨迹的射线段
                    for (int j = 0; j < curTraj.rayList.Count; ++j)  // 对于一条轨迹中的每条射线
                    {
                        NodeInfo nodeCur = curTraj.rayList[j];//当前射线段
                        if (j == 0)
                        {
                            nodeCur.DefAngle = -1;//如果是轨迹的第一条射线段，那么肯定是直射线，转角赋默认值-1
                        }
                        else
                        {
                            NodeInfo nodeBef = curTraj.rayList[j - 1];//前一条射线段
                            NewVector3D vecBef = new NewVector3D(nodeBef.PointOfIncidence, nodeBef.CrossPoint);
                            NewVector3D vecCur = new NewVector3D(nodeCur.PointOfIncidence, nodeCur.CrossPoint);
                            double defAngle = vecBef.calcDefAngle(ref vecCur);
                            nodeCur.DefAngle = defAngle;

                        }
                    }
                }

            }
        }

        //过滤路测点，和计算点，路测点将含距离信息
        private void filterWithDTInfo(ref DataTable rayAdjTb, ref DataTable DTTb, ref Dictionary<string, List<DTInfo>> meaPwr, ref Dictionary<string, TrajInfo> rayDic)
        {
            //筛选前
            Dictionary<string, TrajInfo> rayDicOri = CalRays.buildingGrids(ref rayAdjTb);  //计算生成的轨迹数据
            Dictionary<string, List<DTInfo>> meaPwrOri = CalRays.getDTInfoFromtbDT(ref DTTb);//路测数据
            //Dictionary<string, List<double>> meaPwrOri = CalRays.getMeaPwr(ref rayDicOri, this.sceneNum); //模拟路测

            #region 将原始的计算数据及路测数据转换为rayID字典及meaID字典：key: (gxid,gyid), value: 该栅格对应的小区ID组成的list
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
            HashSet<string> keys = new HashSet<string>();//（cellID，gxid，gyid）
            foreach (string key in meaID.Keys)//遍历路测数据包含的栅格
            {
                if (!rayID.Keys.Contains(key))
                    continue;

                List<int> list = meaID[key].Intersect(rayID[key]).ToList();  // 求交集，得到两者的公共小区

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
                        for (int j = 0; j < meaPwrOri[k].Count; ++j)
                        {
                            double tmp = meaPwrOri[k][j].pwrDbm;
                            if (tmp > max2)
                            {
                                max2 = tmp;
                                maxK = k;
                            }
                        }

                        // 射线计算最强的
                        if (rayDicOri[k].sumPwrDbm > max1)
                        {
                            max1 = rayDicOri[k].sumPwrDbm;
                            maxK1 = k;
                        }

                        // 两者差值最大的
                        for (int j = 0; j < meaPwrOri[k].Count; ++j)
                        {
                            double tmp = Math.Abs(rayDicOri[k].sumPwrDbm - meaPwrOri[k][j].pwrDbm);
                            if (tmp > max2)
                            {
                                max2 = tmp;
                                maxK2 = k;
                            }
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
            meaPwr = new Dictionary<string, List<DTInfo>>();
            foreach (string key in keys)
            {
                rayDic[key] = rayDicOri[key];
                meaPwr[key] = meaPwrOri[key];
            }

            //一个小区一个栅格，只选择一个与计算值差的最大的路测点
            int BigErrorPointCnt = 0;
            foreach (string key in keys)
            {

                double recePwr = rayDic[key].sumPwrDbm;

                ////测试用 查找某一栅格的计算值
                //if (key == "35653633,4045,4427") {
                //    int testPoint = 1;
                //}

                DTInfo maxDiffDtInfo= meaPwr[key][0];//与实际值差值最大的路测点,初始化为第一个路测点
                double maxDiff = -1;//差值最大值

                foreach (DTInfo dtInfo in meaPwr[key])
                {
                    double diff = Math.Abs(recePwr - dtInfo.pwrDbm);
                    if (diff > maxDiff)
                    {
                        maxDiff = diff;
                        maxDiffDtInfo = dtInfo;
                    }
                }

                //更新路测数据为一个栅格一个（与真实值差的最大的）路测值
                meaPwr[key] = new List<DTInfo>();
                meaPwr[key].Add(maxDiffDtInfo);
            }

            //找到真实值与路测值差的最大的小区,写入文件
            double maxDiffVal = -1;//最大差值
            double maxDifRecePwr = -1;
            double maxDifDtPwr = -1;
            string maxDifKey = ""; //最大差值对应的小区key
            foreach (string key in keys)
            {
                double recePwr = rayDic[key].sumPwrDbm;

                foreach (DTInfo dtInfo in meaPwr[key])
                {
                    double diff = Math.Abs(recePwr - dtInfo.pwrDbm);
                    if (diff > maxDiffVal)
                    {
                        maxDiffVal = diff;
                        maxDifKey = key;
                        maxDifRecePwr = recePwr;
                        maxDifDtPwr = dtInfo.pwrDbm;
                    }
                }
            }

            StreamWriter maxDiffPointFile;
            string path1 = EA.basePath + @"maxDiffPoint.txt";
            maxDiffPointFile = File.CreateText(path1);
            maxDiffPointFile.WriteLine(maxDifKey + "\t" + "路测值：" + maxDifDtPwr.ToString("0.00") + "计算值：" + maxDifRecePwr.ToString("0.00"));
            maxDiffPointFile.Close();

            //测指定点用，测试完删掉
            //指定点
            //string testKey = "35653633,4045,4427";
            //double testPwr = meaPwr[testKey][0];

            ////指定点：最大误差点
            //string testKey = maxDifKey;
            //double testPwr = maxDifDtPwr;

            //meaPwr = new Dictionary<string, List<double>>();
            //meaPwr[testKey] = new List<double>();
            //meaPwr[testKey].Add(testPwr);
        }


        /// <summary>
        ///         选择射线跟踪计算结果和真实路测的公共部分
        /// </summary>
        /// <param name="rayAdjTb">计算值的轨迹数据表（经内存控制）</param>
        /// <param name="DTTb">路测数据表（经内存控制）</param>
        /// <param name="meaPwr">路测数据字典</param>
        /// <param name="rayDic">计算值字典</param>
        private void filter(ref DataTable rayAdjTb, ref DataTable DTTb, ref Dictionary<string, List<double>> meaPwr, ref Dictionary<string, TrajInfo> rayDic)
        {
            //筛选前
            Dictionary<string, TrajInfo> rayDicOri = CalRays.buildingGrids(ref rayAdjTb);  //计算生成的轨迹数据
            Dictionary<string, List<double>> meaPwrOri = CalRays.getMeaPwrFromtbDT(ref DTTb);//路测数据
            //Dictionary<string, List<double>> meaPwrOri = CalRays.getMeaPwr(ref rayDicOri, this.sceneNum); //模拟路测

            #region 将原始的计算数据及路测数据转换为rayID字典及meaID字典：key: (gxid,gyid), value: 该栅格对应的小区ID组成的list
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
            HashSet<string> keys = new HashSet<string>();//（cellID，gxid，gyid）
            foreach (string key in meaID.Keys)//遍历路测数据包含的栅格
            {
                if (!rayID.Keys.Contains(key))
                    continue;

                List<int> list = meaID[key].Intersect(rayID[key]).ToList();  // 求交集，得到两者的公共小区

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
                        if (meaPwrOri[k].Max() > max)
                        {
                            max = meaPwrOri[k].Max();
                            maxK = k;
                        }

                        // 射线计算最强的
                        if (rayDicOri[k].sumPwrDbm > max1)
                        {
                            max1 = rayDicOri[k].sumPwrDbm;
                            maxK1 = k;
                        }

                        // 两者差值最大的
                        for (int j = 0; j < meaPwrOri[k].Count; ++j)
                        {
                            double tmp = Math.Abs(rayDicOri[k].sumPwrDbm - meaPwrOri[k][j]);
                            if (tmp > max2)
                            {
                                max2 = tmp;
                                maxK2 = k;
                            }
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
            meaPwr = new Dictionary<string, List<double>>();
            foreach (string key in keys)
            {
                rayDic[key] = rayDicOri[key];
                meaPwr[key] = meaPwrOri[key];
            }

            //一个小区一个栅格，只选择一个与计算值差的最大的路测点
            int BigErrorPointCnt = 0;
            foreach (string key in keys)
            {

                double recePwr = rayDic[key].sumPwrDbm;

                ////测试用 查找某一栅格的计算值
                //if (key == "35653633,4045,4427") {
                //    int testPoint = 1;
                //}

                double maxDiffDtPwr = meaPwr[key][0];//与实际值差值最大的路测点,初始化为第一个路测点
                double maxDiff = -1;//差值最大值

                foreach (double dtPwr in meaPwr[key])
                {
                    double diff = Math.Abs(recePwr - dtPwr);
                    if (diff > maxDiff)
                    {
                        maxDiff = diff;
                        maxDiffDtPwr = dtPwr;
                    }
                }


                //if (maxDiff > 30)
                //{ //去掉路测值和计算值相差30dbm以上的点。
                //    BigErrorPointCnt++;
                //    continue;
                //}

                //更新路测数据为一个栅格一个（与真实值差的最大的）路测值
                meaPwr[key] = new List<double>();
                meaPwr[key].Add(maxDiffDtPwr);
            }

            //找到真实值与路测值差的最大的小区,写入文件
            double maxDiffVal = -1;//最大差值
            double maxDifRecePwr=-1;
            double maxDifDtPwr=-1;
            string maxDifKey = ""; //最大差值对应的小区key
            foreach (string key in keys)
            {
                double recePwr = rayDic[key].sumPwrDbm;

                foreach (double dtPwr in meaPwr[key])
                {
                    double diff = Math.Abs(recePwr - dtPwr);
                    if (diff > maxDiffVal)
                    {
                        maxDiffVal = diff;
                        maxDifKey = key;
                        maxDifRecePwr = recePwr;
                        maxDifDtPwr = dtPwr;
                    }
                }
            }

            StreamWriter maxDiffPointFile;
            string path1 = EA.basePath + @"maxDiffPoint.txt";
            maxDiffPointFile = File.CreateText(path1);
            maxDiffPointFile.WriteLine(maxDifKey + "\t" + "路测值："+maxDifDtPwr.ToString("0.00") + "计算值："+maxDifRecePwr.ToString("0.00"));
            maxDiffPointFile.Close();

            //测指定点用，测试完删掉
            //指定点
            //string testKey = "35653633,4045,4427";
            //double testPwr = meaPwr[testKey][0];

            ////指定点：最大误差点
            //string testKey = maxDifKey;
            //double testPwr = maxDifDtPwr;

            //meaPwr = new Dictionary<string, List<double>>();
            //meaPwr[testKey] = new List<double>();
            //meaPwr[testKey].Add(testPwr);
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