using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;
using System.Management;
using LTE.InternalInterference;
using LTE.InternalInterference.Grid;
using LTE.DB;
using LTE.GIS;
using System.Data;
using System.Diagnostics;


namespace LTE.WebAPI.Models
{
    // 射线跟踪公共部分
    public class CellRayTracing
    {
        /// <summary>
        /// 小区名称
        /// </summary>
        public string cellName { get; set; }  

        /// <summary>
        /// 小区覆盖半径
        /// </summary>
        public double distance { get; set; }   

        /// <summary>
        /// 以方位角为中心，两边扩展的角度
        /// </summary>
        public double incrementAngle { get; set; }    

        /// <summary>
        /// 线程个数
        /// </summary>
        public int threadNum { get; set; }  

        /// <summary>
        /// 反射次数
        /// </summary>
        public int reflectionNum { get; set; }    

        /// <summary>
        /// 绕射次数
        /// </summary>
        public int diffractionNum { get; set; }    

        /// <summary>
        /// 建筑物棱边绕射点间隔
        /// </summary>
        public double diffPointsMargin { get; set; }   

        /// <summary>
        /// 计算立体覆盖
        /// </summary>
        public bool computeIndoor { get; set; }  

        /// <summary>
        /// 计算棱边绕射
        /// </summary>
        public bool computeDiffrac { get; set; } 

        /// <summary>
        /// 直射校正系数
        /// </summary>
        public float directCoeff { get; set; }   

        /// <summary>
        /// 反射校正系数
        /// </summary>
        public float reflectCoeff { get; set; }  

        /// <summary>
        /// 绕射校正系数
        /// </summary>
        public float diffractCoeff { get; set; }   

        /// <summary>
        /// 菲涅尔绕射校正系数
        /// </summary>
        public float diffractCoeff2 { get; set; }    

        /// <summary>
        /// 验证小区合法性
        /// </summary>
        /// <param name="eNodeB">基站标识</param>
        /// <param name="CI">小区标识</param>
        /// <param name="cellType">小区类型</param>
        /// <returns></returns>
        public Result validateCell(ref int eNodeB, ref int CI, ref string cellType)
        {
            if (this.cellName == string.Empty)
            {
                return new Result(false, "请输入小区名称");
            }
            DataTable dt = IbatisHelper.ExecuteQueryForDataTable("SingleGetCellType", this.cellName);
            if (dt.Rows.Count == 0)
            {
                return new Result(false, "您输入的小区名称有误，请重新输入！");
            }
            eNodeB = Convert.ToInt32(dt.Rows[0]["eNodeB"]);
            CI = Convert.ToInt32(dt.Rows[0]["CI"]);
            cellType = Convert.ToString(dt.Rows[0]["NetType"]);
            return new Result(true);
        }

        /// <summary>
        /// 小区正在计算
        /// </summary>
        /// <param name="lac">基站标识</param>
        /// <param name="ci">小区标识</param>
        /// <returns></returns>
        private bool ExistProcess(int lac, int ci, ref List<ProcessArgs> paList)
        {
            for (int i = 0; i < paList.Count; )
            {
                ProcessArgs pa = paList[i];
                if (pa.pro.HasExited)
                {
                    paList.RemoveAt(i);
                    continue;
                }
                if (pa.eNodeB == lac && pa.CI == ci)
                {
                    return true;
                }
                i++;
            }
            return false;
        }
        public Result parallelComputing(ref CellInfo cellInfo, double fromAngle, double toAngle, int eNodeB, int CI,
            ref List<ProcessArgs> paList, bool isReRay, bool isRecordReRay, bool isRayLoc, bool isRayAdj)
        {
            string bidstext = "-1";

            if (!this.ExistProcess(eNodeB, CI, ref paList))
            {
                LTE.Geometric.Point p = cellInfo.SourcePoint;
                ProcessArgs pa = new ProcessArgs();
                ProcessStartInfo psi = new ProcessStartInfo();

                psi.UseShellExecute = true;
                psi.ErrorDialog = true;
                psi.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                psi.FileName = "LTE.MultiProcessController.exe";
                psi.Arguments = string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15} {16} {17} {18} {19} {20} {21} {22} {23} {24} {25} {26} {27} {28} {29} {30}",
                    cellInfo.SourceName, p.X, p.Y, 0, 0, p.Z, cellInfo.eNodeB, cellInfo.CI,
                    cellInfo.Azimuth, cellInfo.Inclination, cellInfo.cellType, cellInfo.frequncy, cellInfo.EIRP,
                    cellInfo.directCoefficient, cellInfo.reflectCoefficient, cellInfo.diffracteCoefficient, cellInfo.diffracteCoefficient,
                    fromAngle, toAngle, this.distance, this.reflectionNum, this.diffractionNum, this.computeIndoor,
                    this.threadNum, bidstext, this.diffPointsMargin, this.computeDiffrac, isReRay, isRecordReRay, isRayLoc, isRayAdj);

                try
                {
                    pa.pro = Process.Start(psi);
                    paList.Add(pa);
                    pa.pro.WaitForExit();
                    //子进程异常处理，防止父进程无限阻塞，Controller不能及时返回消息
                    if (pa.pro.ExitCode != 0)
                    {
                        return new Result(false, "多进程计算失败，请重试，错误代码：{0}",pa.pro.ExitCode );
                    }
                }
                catch (InvalidOperationException exception)
                {
                    return new Result(false, "多进程计算启动失败，原因： " + exception.Message);
                }
                catch (Exception ee)
                {
                    return new Result(false, "多进程计算启动失败，原因： " + ee.Message);
                }
            }
            else
            {
                return new Result(false, "该小区正在计算");
            }

            return new Result { ok=true, msg="小区覆盖计算结束", code="1"};
        }
    }

    public class ProcessArgs
    {
        public Process pro;
        public string cellName;
        public int eNodeB;
        public int CI;
    }

    // 基于射线跟踪进行小区覆盖计算
    public class CellRayTracingModel : CellRayTracing
    {
        /// <summary>
        /// 如果数据库表 tbAdjCoefficient 中有校正系数时，则界面中的校正系数仅仅被传入，而不会在计算场强中用到
        /// </summary>
        /// <returns></returns>
        public Result calc()
        {
            int eNodeB = 0, CI = 0;
            string cellType = "";

            Result rt = validateCell(ref eNodeB, ref CI, ref cellType);
            if (!rt.ok)
                return rt;

            CellInfo cellInfo = new CellInfo(this.cellName, eNodeB, CI, this.directCoeff, this.reflectCoeff, this.diffractCoeff, this.diffractCoeff2);

            double fromAngle = cellInfo.Azimuth - this.incrementAngle;
            double toAngle = cellInfo.Azimuth + this.incrementAngle;

            //指定最大覆盖半径
            int maxCoverageRadius = 15000;
            this.distance = Math.Min(this.distance, maxCoverageRadius);

            // 删除旧的接收功率数据
            Hashtable ht = new Hashtable();
            ht["CI"] = CI;
            ht["eNodeB"] = eNodeB;
            GridCover gc = GridCover.getInstance();
            gc.deleteBuildingCover(ht);
            gc.deleteGroundCover(ht);

            // 计算方案
            int threadCnt = 0, batchNum = 0, flag = 0;
            bool ok = howCalc(fromAngle, toAngle, ref threadCnt, ref batchNum, ref flag);

            // 不需要分批计算
            if (ok)  
            {
                List<ProcessArgs> paList = new List<ProcessArgs>();
                bool reRay = false;  // 是否需要进行二次投射，即读取前一批覆盖计算中出界的射线，并对其进行射线跟踪
                bool recordReRay = false;  // 是否需要记录当前批的出界射线

                // 小区覆盖计算
                return parallelComputing(ref cellInfo, fromAngle, toAngle, eNodeB, CI, ref paList, reRay, recordReRay, false, false);
            }
            // 需要分批计算
            else  
            {
                IbatisHelper.ExecuteDelete("deleteSpecifiedReRay", ht);

                // 建议计算方案：线程数：threadCnt, 批数 batchNum
                if (flag == 2)
                {
                    this.threadNum = threadCnt;
                    double delta = (toAngle - fromAngle + 360) % 360 / batchNum;
                    double startAngle = fromAngle;
                    
                    for (int currBatch = 1; currBatch <= batchNum; currBatch++)
                    {
                        fromAngle = (startAngle + (currBatch - 1) * delta + 360) % 360;
                        toAngle = (fromAngle + delta + 360) % 360;

                        List<ProcessArgs> paList = new List<ProcessArgs>();

                        bool reRay = false;  // 是否需要进行二次投射，即读取前一批覆盖计算中出界的射线，并对其进行射线跟踪
                        if (currBatch > 1)  // 当前不是第一批，就需要进行二次投射
                            reRay = true;

                        bool recordReRay = false;  // 是否需要记录当前批的出界射线
                        if (currBatch < batchNum)  // 前批不是最后一批，则需记录当前批的出界射线，供下批二次投射
                            recordReRay = true;

                        // 小区覆盖计算
                        Result result = parallelComputing(ref cellInfo, fromAngle, toAngle, eNodeB, CI, ref paList, reRay, recordReRay, false, false);
                        if (!result.ok)
                            return result;

                        // 分批计算的最后一批，需要对相同小区、相同栅格的功率进行合并
                        if (currBatch == batchNum)
                        {
                            mergePwr1(cellInfo.EIRP, "getPwrGround", false, eNodeB, CI);
                            mergePwr1(cellInfo.EIRP, "getPwrBuilding", true, eNodeB, CI);
                        }
                    }
                    return new Result(true);
                }
                // 建议计算方案：线程数：threadCnt
                else
                {
                    this.threadNum = threadCnt;
                    List<ProcessArgs> paList = new List<ProcessArgs>();
                    bool reRay = false;  // 是否需要进行二次投射，即读取前一批覆盖计算中出界的射线，并对其进行射线跟踪
                    bool recordReRay = false;  // 是否需要记录当前批的出界射线

                    return parallelComputing(ref cellInfo, fromAngle, toAngle, eNodeB, CI, ref paList, reRay, recordReRay, false, false);
                }
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
            //Console.WriteLine(sr);
            return capacity;
        }

        // 栅格所占内存 G
        double memF(double r)
        {
            return 0.0248915 * r * r / 1000000;
        }

        // 结果所占内存 G
        double memR(double r, double theta)
        {
            return 0.0001852 * theta * r * r / 1000000;
        }

        // 获取计算方案
        // threadCnt：并行进程数
        // batchNum：分区域数
        // flag: 1--同时算，每次加载全部内存; 2--分批算，每次仅加载一部分内存，最后进行二次投射
        bool howCalc(double fromAngle, double toAngle, ref int threadCnt, ref int batchNum, ref int flag)
        {
            double capacity = this.MemoryInfo() / 6.0;  // 获取系统物理内存
            threadCnt = Math.Max(3, this.threadNum);
            batchNum = 2;

            double theta = ((toAngle - fromAngle + 360) % 360) / 360;
            double F = memF(this.distance);
            double R = memR(this.distance, theta);
            if (this.threadNum * F + R < capacity)
                return true;
            else
            {
                // 寻找可并行计算的进程数
                while (threadCnt * F + R > capacity && threadCnt >= 0)
                    --threadCnt;

                if (threadCnt > 0)  // 分 threadCnt 个子区域，同时算，每次加载全部内存;
                {
                    flag = 1;
                    if (threadCnt <= this.threadNum)
                        return true;
                    else
                        return false;
                }
                else  // 无法一次性计算整个覆盖区域
                {
                    threadCnt = 3;
                    ////test
                    //threadCnt = 1;


                    while ((F + R) / batchNum > capacity)
                        ++batchNum;

                    // 分 batchNum 个子区域，分批算，每次仅加载一部分内存，最后进行二次投射
                    flag = 2;
                    return false;
                }
            }
        }

        private double convertw2dbm(double w)
        {
            return 10 * (Math.Log10(w) + 3);
        }

        public string DistinctStringArray(string[] strArr)
        {
            return String.Join(";", strArr.Distinct().ToArray());
        }

        // getTb：读数据库的sql
        // flag：false--操作tbGridPathloss， true--操作tbBuildingGridPathloss
        // 只有半径比较大的时候才会用到，因为一次算不完
        private void mergePwr1(double EIRP, string getTb, bool flag, int eNodeB, int CI)
        {
            Hashtable ht = new Hashtable();
            ht["CI"] = CI;
            ht["eNodeB"] = eNodeB;
            DataTable tb = IbatisHelper.ExecuteQueryForDataTable(getTb, ht);

            GridCover gc = GridCover.getInstance();
            if (flag)
                gc.deleteBuildingCover(ht);
            else
                gc.deleteGroundCover(ht);

            Dictionary<string, GridStrength> gridStrengths = new Dictionary<string, GridStrength>();

            foreach (DataRow dataRow in tb.Rows)
            {
                #region 读入数据，按栅格分组，合并
                int GXID = int.Parse(dataRow["GXID"].ToString());
                int GYID = int.Parse(dataRow["GYID"].ToString());
                int GZID = 0;
                if (flag)
                    GZID = int.Parse(dataRow["Level"].ToString());

                string key = String.Format("{0},{1},{2}", GXID, GYID, GZID);

                if (!gridStrengths.ContainsKey(key))
                {
                    GridStrength gs = new GridStrength();
                    gs.GXID = GXID;
                    gs.GYID = GYID;
                    gs.Level = GZID;
                    gs.eNodeB = int.Parse(dataRow["eNodeB"].ToString());
                    gs.CI = int.Parse(dataRow["CI"].ToString());
                    if (dataRow["FieldIntensity"].ToString() != "")
                        gs.FieldIntensity = double.Parse(dataRow["FieldIntensity"].ToString());
                    gs.DirectNum = int.Parse(dataRow["DirectPwrNum"].ToString());
                    gs.DirectPwrW = double.Parse(dataRow["DirectPwrW"].ToString());
                    gs.MaxDirectPwrW = double.Parse(dataRow["MaxDirectPwrW"].ToString());
                    gs.RefNum = int.Parse(dataRow["RefPwrNum"].ToString());
                    gs.RefPwrW = double.Parse(dataRow["RefPwrW"].ToString());
                    gs.MaxRefPwrW = double.Parse(dataRow["MaxRefPwrW"].ToString());
                    gs.RefBuildingID = dataRow["RefBuildingID"].ToString();
                    gs.DiffNum = int.Parse(dataRow["DiffNum"].ToString());
                    gs.DiffPwrW = double.Parse(dataRow["DiffPwrW"].ToString());
                    gs.MaxDiffPwrW = double.Parse(dataRow["MaxDiffPwrW"].ToString());
                    gs.DiffBuildingID = dataRow["DiffBuildingID"].ToString();
                    gs.BTSGridDistance = double.Parse(dataRow["BTSGridDistance"].ToString());
                    //gs.ReceivedPowerW = double.Parse(dataRow["ReceivedPowerW"].ToString());
                    //gs.ReceivedPowerdbm = double.Parse(dataRow["ReceivedPowerdbm"].ToString());
                    //gs.PathLoss = double.Parse(dataRow["PathLoss"].ToString());
                    if (flag)
                    {
                        gs.TransNum = int.Parse(dataRow["TransNum"].ToString());
                        gs.TransPwrW = double.Parse(dataRow["TransPwrW"].ToString());
                        gs.MaxTransPwrW = double.Parse(dataRow["MaxTransPwrW"].ToString());
                        gs.TransmitBuildingID = dataRow["TransmitBuildingID"].ToString();
                    }
                    else
                    {
                        gs.TransNum = 0;
                        gs.TransPwrW = 0;
                        gs.MaxTransPwrW = 0;
                        gs.TransmitBuildingID = "";
                    }
                    gridStrengths.Add(key, gs);
                }
                else
                {
                    GridStrength ogs = gridStrengths[key];

                    ogs.DirectNum += int.Parse(dataRow["DirectPwrNum"].ToString());
                    double pwr = double.Parse(dataRow["DirectPwrW"].ToString());
                    ogs.DirectPwrW += double.Parse(dataRow["DirectPwrW"].ToString());
                    if (ogs.MaxDirectPwrW < double.Parse(dataRow["MaxDirectPwrW"].ToString()))
                    {
                        ogs.MaxDirectPwrW = double.Parse(dataRow["MaxDirectPwrW"].ToString());
                    }

                    ogs.RefBuildingID += dataRow["RefBuildingID"].ToString();
                    ogs.RefNum += int.Parse(dataRow["RefPwrNum"].ToString());
                    ogs.RefPwrW += double.Parse(dataRow["RefPwrW"].ToString());
                    if (ogs.MaxRefPwrW < double.Parse(dataRow["MaxRefPwrW"].ToString()))
                    {
                        ogs.MaxRefPwrW = double.Parse(dataRow["MaxRefPwrW"].ToString());
                    }

                    ogs.DiffNum += int.Parse(dataRow["DiffNum"].ToString());
                    ogs.DiffPwrW += double.Parse(dataRow["DiffPwrW"].ToString());
                    ogs.DiffBuildingID += dataRow["DiffBuildingID"].ToString();
                    if (ogs.MaxDiffPwrW < double.Parse(dataRow["MaxDiffPwrW"].ToString()))
                    {
                        ogs.MaxDiffPwrW = double.Parse(dataRow["MaxDiffPwrW"].ToString());
                    }

                    if (flag)
                    {
                        ogs.TransNum += int.Parse(dataRow["TransNum"].ToString());
                        ogs.TransPwrW += double.Parse(dataRow["TransPwrW"].ToString());
                        ogs.TransmitBuildingID += dataRow["TransmitBuildingID"].ToString();
                        if (ogs.MaxTransPwrW < double.Parse(dataRow["MaxTransPwrW"].ToString()))
                        {
                            ogs.MaxTransPwrW = double.Parse(dataRow["MaxTransPwrW"].ToString());
                        }
                    }

                    //dictionary不能自动更新
                    gridStrengths[key] = ogs;
                }
                #endregion

                if (gridStrengths.Count > 30000)
                {
                    # region 计算栅格最终功率
                    foreach (var k in gridStrengths.Keys.ToArray())
                    {
                        GridStrength ogs = gridStrengths[k];
                        double p = ogs.DirectPwrW + ogs.DiffPwrW + ogs.RefPwrW + ogs.TransPwrW;
                        if (p > 0)
                            ogs.ReceivedPowerW = p;

                        ogs.ReceivedPowerdbm = convertw2dbm(ogs.ReceivedPowerW);
                        ogs.PathLoss = EIRP - ogs.ReceivedPowerdbm;

                        //反射、绕射建筑物id去重
                        ogs.RefBuildingID = DistinctStringArray(ogs.RefBuildingID.Split(';'));
                        ogs.DiffBuildingID = DistinctStringArray(ogs.DiffBuildingID.Split(';'));
                        ogs.TransmitBuildingID = DistinctStringArray(ogs.TransmitBuildingID.Split(';'));

                        //dictionary 不能自动更新
                        gridStrengths[k] = ogs;
                    }
                    #endregion

                    #region   写入数据库
                    gc.convertToDt(gridStrengths);

                    if (flag)
                    {
                        gc.writeBuildingCover(ht);
                        gc.clearBuilding();
                    }
                    else
                    {
                        gc.wirteGroundCover(ht);
                        gc.clearGround();
                    }
                    #endregion

                    gridStrengths.Clear();
                }
            }

            # region 计算栅格最终功率
            foreach (var k in gridStrengths.Keys.ToArray())
            {
                GridStrength ogs = gridStrengths[k];
                double p = ogs.DirectPwrW + ogs.DiffPwrW + ogs.RefPwrW + ogs.TransPwrW;
                if (p > 0)
                    ogs.ReceivedPowerW = p;

                ogs.ReceivedPowerdbm = convertw2dbm(ogs.ReceivedPowerW);
                ogs.PathLoss = EIRP - ogs.ReceivedPowerdbm;

                //反射、绕射建筑物id去重
                ogs.RefBuildingID = DistinctStringArray(ogs.RefBuildingID.Split(';'));
                ogs.DiffBuildingID = DistinctStringArray(ogs.DiffBuildingID.Split(';'));
                ogs.TransmitBuildingID = DistinctStringArray(ogs.TransmitBuildingID.Split(';'));

                //dictionary 不能自动更新
                gridStrengths[k] = ogs;
            }
            #endregion

            #region   写入数据库
            gc.convertToDt(gridStrengths);

            if (flag)
            {
                gc.writeBuildingCover(ht);
                gc.clearBuilding();
            }
            else
            {
                gc.wirteGroundCover(ht);
                gc.clearGround();
            }
            #endregion
            gridStrengths.Clear();
            tb.Clear();
        }
    }
}

