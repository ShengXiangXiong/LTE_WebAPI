using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Collections;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using LTE.Win32Lib;
using LTE.Geometric;
using System.Threading;

using LTE.DB;
using LTE.InternalInterference;
using LTE.InternalInterference.Grid;

// 更新：
// 2019.04.12 程序运行结束后关闭窗体

namespace LTE.MultiProcessController
{
    public partial class CtrlForm : Form
    {
        private string basePath = AppDomain.CurrentDomain.BaseDirectory;
        private List<ProcessArgs> paList;
        private string baseMMFName;
        private CellInfo cellInfo;

        private double fromAngle;
        private double toAngle;
        private double distance;
        private int reflectionNum;
        private int diffractionNum;
        private bool computeIndoor;

        private double diffPointsMargin;
        private bool computeVSide;

        int totalRay = 0;  // 射线总数
                    
        /// <summary>
        /// 需要计算的建筑物，过去的逻辑，现已不起作用
        /// </summary>
        private List<int> bids;
        /// <summary>
        /// 各子任务计算结果，用于合并
        /// </summary>
        private List<GridStrength> MultiTasksGridStrengths;
        private Dictionary<string, GridStrength> GridStrengths;
        private List<MMFReRayStruct> MultiTasksReRay; // 2019.5.22

        /// <summary>
        /// 子进程数
        /// </summary>
        private int processNum;

        private int maxProcNum;
        /// <summary>
        /// 即将开始计算的子进程列表
        /// </summary>
        private List<System.Windows.Forms.Message> msgList;

        /// <summary>
        /// 本批已经计算完成的子进程数
        /// </summary>
        private int procDoneNum;

        /// <summary>
        /// 已经计算完成的子进程数
        /// </summary>
        private int procDoneNum1;

        private ConsoleShow cs;

        private bool reRay;  // 是否为二次投射
        private bool rayLoc; // 生成的射线用于定位
        private bool rayAdj; // 生成的射线用于系数校正
        private bool isRecordReray;  // 是否记录当前批出界射线，以供下批二次投射

        private DateTime start, end, t1, t2, read, merge;
        private IntPtr parentHandle;
        public CtrlForm()
        {
            start = DateTime.Now;

            //MessageBox.Show("4");
            InitializeComponent();

            this.paList = new List<ProcessArgs>();
            this.procDoneNum = 0;
            this.procDoneNum1 = 0;
            maxProcNum = 2;
            //this.procStartNum = 0;
            //this.mutex = new Mutex();
            msgList = new List<Message>();
            this.MultiTasksGridStrengths = new List<GridStrength>();
            this.MultiTasksReRay = new List<MMFReRayStruct>(); // 2019.5.22

            string[] args = System.Environment.GetCommandLineArgs();
            if (dealParams(args))
            {
                //MessageBox.Show("5");
                cs = new ConsoleShow();
                this.startChildPro();
                //MessageBox.Show("6");
            }
            else
            {
                System.Environment.Exit(0);
            }
        }

        private bool dealParams(string[] args)
        {
            if (args.Length == 32)
            {
                try
                {
                    cellInfo = new CellInfo();
                    cellInfo.SourceName = args[1];
                    cellInfo.SourcePoint = new LTE.Geometric.Point(Convert.ToDouble(args[2]), Convert.ToDouble(args[3]), Convert.ToDouble(args[6]));
                    cellInfo.eNodeB = Convert.ToInt32(args[7]);
                    cellInfo.CI = Convert.ToInt32(args[8]);
                    cellInfo.Azimuth = Convert.ToDouble(args[9]);
                    cellInfo.Inclination = Convert.ToDouble(args[10]);
                    cellInfo.RayAzimuth = 0;
                    cellInfo.RayInclination = 0;
                    cellInfo.cellType = args[11] == "GSM900" ? CellType.GSM900 : CellType.GSM1800;
                    cellInfo.frequncy = Convert.ToInt32(args[12]);
                    cellInfo.EIRP = Convert.ToDouble(args[13]);

                    cellInfo.directCoefficient = Convert.ToSingle(args[14]);
                    cellInfo.reflectCoefficient = Convert.ToSingle(args[15]);
                    cellInfo.diffracteCoefficient = Convert.ToSingle(args[16]);
                    cellInfo.diffracteCoefficient2 = Convert.ToSingle(args[17]);

                    this.fromAngle = Convert.ToDouble(args[18]);
                    this.toAngle = Convert.ToDouble(args[19]);
                    this.distance = Convert.ToDouble(args[20]);
                    this.reflectionNum = Convert.ToInt32(args[21]);
                    this.diffractionNum = Convert.ToInt32(args[22]);
                    this.computeIndoor = Convert.ToBoolean(args[23]);

                    this.processNum = Convert.ToInt32(args[24]);
                    //this.processNum = processNum > 0 && processNum < 5 ? processNum : 2;

                    this.bids = new List<int>();
                    string[] bidstext = args[25].Split(',');
                    for (int i = 0, cnt = bidstext.Length; i < cnt; i++)
                    {
                        if (i > 0)
                        {
                            this.bids.Add(Convert.ToInt32(bidstext[i]));
                        }
                    }

                    this.diffPointsMargin = Convert.ToDouble(args[26]);
                    this.computeVSide = Convert.ToBoolean(args[27]);
                    this.reRay = Convert.ToBoolean(args[28]);
                    this.isRecordReray = this.rayLoc = Convert.ToBoolean(args[29]);
                    this.rayLoc = Convert.ToBoolean(args[30]);
                    this.rayAdj = Convert.ToBoolean(args[31]);
                    this.baseMMFName = string.Format("MMF_{0}_{1}", cellInfo.eNodeB, cellInfo.CI);
                    this.Text = string.Format("计算小区{0}({1}-{2}覆盖), 共{3}个计算进程", cellInfo.SourceName, cellInfo.eNodeB, cellInfo.CI, processNum);
                    
                    return true;
                }
                catch (Exception ee)
                {
                    MessageBox.Show("参数转换出错，出错原因：" + ee.Message);
                }
            }
            else
            {
                MessageBox.Show("CtrlForm 参数个数不匹配，程序退出:" + args.Length);
            }
            return false;
        }

        private void startChildPro()
        {
            Random r = new Random(DateTime.Now.Second);
            LTE.Geometric.Point p = cellInfo.SourcePoint;
            double deltaA = (this.toAngle - this.fromAngle + 360) % 360 / processNum;
            double from, to;
            string calcBids = "";
            int bidsNum = Convert.ToInt32(Math.Round(this.bids.Count * 1.0 / processNum));
            try
            {
                for (int i = 0; i < this.processNum; i++)
                {
                    from = (fromAngle + i * deltaA + 360) % 360;
                    to = (fromAngle + (i + 1) * deltaA + 360) % 360;


                    if (to > this.toAngle)
                        to = this.toAngle;


                    calcBids = "-1";
                    for (int j = i * bidsNum, k = 0, bound = i == this.processNum - 1 ? this.bids.Count - j : bidsNum; k < bound; k++)
                    {
                        calcBids += "," + this.bids[j + k];
                    }

                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = "LTE.CalcProcess.exe";
                    psi.Arguments = string.Format("{0} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15} {16} {17} {18} {19} {20} {21} {22} {23} {24} {25} {26} {27} {28} {29} {30} {31} {32} {33} {34}",
                        this.baseMMFName, 0, i + 1, cellInfo.SourceName, p.X, p.Y, 0, 0, p.Z, cellInfo.eNodeB, cellInfo.CI,
                        cellInfo.Azimuth, cellInfo.Inclination, cellInfo.cellType, cellInfo.frequncy, cellInfo.EIRP,
                        cellInfo.directCoefficient, cellInfo.reflectCoefficient, cellInfo.diffracteCoefficient, cellInfo.diffracteCoefficient2,
                        from, to, this.distance, this.reflectionNum, this.diffractionNum, this.computeIndoor, this.Handle.ToInt32(), calcBids,
                        this.diffPointsMargin, this.computeVSide, deltaA, this.reRay, this.isRecordReray, this.rayLoc, this.rayAdj);
                    psi.UseShellExecute = true;
                    //当前exe与子进程在同一级目录下
                    psi.WorkingDirectory = this.basePath;
                    psi.ErrorDialog = true;
                    Process pro = Process.Start(psi);
                    //pro.WaitForInputIdle();
                    //pro.Refresh();
                    //this.paList.Add(new ProcessArgs(pro));
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(string.Format("启动子程序异常：路径 = {0}\n, 原因：", this.basePath, ee.Message));
                System.Environment.Exit(0);
            }
        }

        private void startCalc(IntPtr handle)
        {
            ProcessArgs pa = new ProcessArgs();
            pa.handle = handle;
            pa.MMFName = string.Format("{0}_{1}", this.baseMMFName, handle);
            this.paList.Add(pa);
            
            IPC.PostMessage(handle, IPC.WM_POST_NOTIFY, this.Handle, 0);
            Console.WriteLine(string.Format("子进程{0} ready, mmf name: {1}, 启动计算...", handle, pa.MMFName));
        }

        protected override void DefWndProc(ref System.Windows.Forms.Message m)
        {
            switch (m.Msg)
            {
                case IPC.WM_POST_READY:
                    this.startCalc(m.WParam);
                    break;
                case IPC.WM_POST_CALCDONE:
                    if (this.rayLoc || this.rayAdj)
                        Console.WriteLine("完成！");
                    else
                        readCalcResult(m.WParam, m.LParam.ToInt32());
                    break;
                case IPC.WM_POST_ReRayDONE:  // 2019.5.22
                    readReRayResult(m.WParam, m.LParam.ToInt32());
                    break;                                                 
                default:
                    base.DefWndProc(ref m);
                    break;
            }
        }

        // 2019.5.22
        private void readReRayResult(IntPtr Chandle, int dataSize)
        {
            string shareName = this.getMMFName(Chandle) + "_ReRay";

            IntPtr mmf = IntPtr.Zero;
            try
            {
                mmf = MMF.OpenFileMapping(MMF.FileMapAccess.FileMapRead, false, shareName);
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("open mmf {0} failed, error : {1}", shareName, e.Message));
            }
            if (IntPtr.Zero == mmf)
            {
                Console.WriteLine(string.Format("共享内存<{0}>打开失败，错误信息编号：{1}", shareName, MMF.GetLastError()));
                return;
            }
            IntPtr reader = IntPtr.Zero;
            try
            {
                reader = MMF.MapViewOfFile(mmf, MMF.FileMapAccess.FileMapRead, 0, 0, (uint)dataSize);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            if (reader == IntPtr.Zero)
            {
                Console.WriteLine(string.Format("共享内存<{0}>映射失败，错误信息编号：{1}", shareName, MMF.GetLastError()));
                return;
            }

            IntPtr tp = IntPtr.Zero;
            Type type = typeof(MMFReRayStruct);
            int ssize = Marshal.SizeOf(type);

            tp = new IntPtr(reader.ToInt32());
            MMFReRayStruct data = new MMFReRayStruct();

            for (int dsize = 0, sp = reader.ToInt32(); dsize < dataSize; dsize += ssize)
            {
                try
                {
                    tp = new IntPtr(sp + dsize);
                    data = (MMFReRayStruct)Marshal.PtrToStructure(tp, typeof(MMFReRayStruct));
                    this.MultiTasksReRay.Add(data);
                }
                catch (Exception e)
                {
                    Console.WriteLine(string.Format("read error : tp = {0}, dsize = {1}, msg = {2}", tp.ToInt32(), dsize, e.Message));
                }
            }
            MMF.UnmapViewOfFile(reader);
            MMF.CloseHandle(mmf);
            mmf = reader = IntPtr.Zero;

            if (++this.procDoneNum1 == this.processNum)
            {
                // 删除旧的reRay
                Hashtable ht = new Hashtable();
                ht["CI"] = this.cellInfo.CI;
                ht["eNodeB"] = this.cellInfo.eNodeB;
                IbatisHelper.ExecuteDelete("deleteSpecifiedReRay", ht);

                System.Data.DataTable dtable = new System.Data.DataTable();
                dtable.Columns.Add("ci");
                dtable.Columns.Add("emitX");
                dtable.Columns.Add("emitY");
                dtable.Columns.Add("emitZ");
                dtable.Columns.Add("pwrDbm");
                dtable.Columns.Add("dirX");
                dtable.Columns.Add("dirY");
                dtable.Columns.Add("dirZ");
                dtable.Columns.Add("type");

                for (int i = 0; i < this.MultiTasksReRay.Count; i++)
                {
                    System.Data.DataRow thisrow = dtable.NewRow();
                    thisrow["ci"] = this.cellInfo.CI;
                    thisrow["emitX"] = Math.Round(this.MultiTasksReRay[i].emitX, 3);
                    thisrow["emitY"] = Math.Round(this.MultiTasksReRay[i].emitY, 3);
                    thisrow["emitZ"] = Math.Round(this.MultiTasksReRay[i].emitZ, 3);
                    thisrow["pwrDbm"] = Math.Round(this.MultiTasksReRay[i].pwrDbm, 3);
                    thisrow["dirX"] = Math.Round(this.MultiTasksReRay[i].dirX, 4);
                    thisrow["dirY"] = Math.Round(this.MultiTasksReRay[i].dirY, 4);
                    thisrow["dirZ"] = Math.Round(this.MultiTasksReRay[i].dirZ, 4);
                    thisrow["type"] = this.MultiTasksReRay[i].type;
                    dtable.Rows.Add(thisrow);
                }

                using (System.Data.SqlClient.SqlBulkCopy bcp = new System.Data.SqlClient.SqlBulkCopy(DataUtil.ConnectionString))
                {
                    bcp.BatchSize = dtable.Rows.Count;
                    bcp.BulkCopyTimeout = 1000;
                    bcp.DestinationTableName = "tbReRay";
                    bcp.WriteToServer(dtable);
                    bcp.Close();
                }
                dtable.Clear();
                Console.WriteLine("tbReRay 写入结束！");
            }
        }

        private void readCalcResult(IntPtr Chandle, int dataSize)
        {
            DateTime t1, t2;
            t1 = DateTime.Now;
            Console.WriteLine(string.Format("read params :child handle = {0}, data size = {1}", Chandle, dataSize));

            string shareName = this.getMMFName(Chandle);

            Console.WriteLine(string.Format("read child outcome :{0}", shareName));
            IntPtr mmf = IntPtr.Zero;
            try
            {
                mmf = MMF.OpenFileMapping(MMF.FileMapAccess.FileMapRead, false, shareName);
                Console.WriteLine("open file ...");
            }
            catch (Exception e)
            {
                //MessageBox.Show(string.Format("open mmf {0} failed, error : {1}", shareName, e.Message));
                Console.WriteLine(string.Format("open mmf {0} failed, error : {1}", shareName, e.Message));
            }
            if (IntPtr.Zero == mmf)
            {
                //MessageBox.Show(string.Format("共享内存<{0}>打开失败，错误信息编号：{1}", shareName, MMF.GetLastError()));
                Console.WriteLine(string.Format("共享内存<{0}>打开失败，错误信息编号：{1}", shareName, MMF.GetLastError()));
                return;
            }
            IntPtr reader = IntPtr.Zero;
            try
            {
                reader = MMF.MapViewOfFile(mmf, MMF.FileMapAccess.FileMapRead, 0, 0, (uint)dataSize);
                Console.WriteLine("map view ...");
            }
            catch (Exception e)
            {
            }
            if (reader == IntPtr.Zero)
            {
                //MessageBox.Show(string.Format("共享内存<{0}>映射失败，错误信息编号：{1}", shareName, MMF.GetLastError()));
                Console.WriteLine(string.Format("共享内存<{0}>映射失败，错误信息编号：{1}", shareName, MMF.GetLastError()));
                return;
            }

            IntPtr tp = IntPtr.Zero;
            Type type = typeof(MMFGSStruct);
            int ssize = Marshal.SizeOf(type);
            Console.WriteLine("ssize = " + ssize);

            tp = new IntPtr(reader.ToInt32());
            MMFGSStruct data = new MMFGSStruct();

            for (int dsize = 0, sp = reader.ToInt32(); dsize < dataSize; dsize += ssize)
            {
                try
                {
                    tp = new IntPtr(sp + dsize);
                    data = (MMFGSStruct)Marshal.PtrToStructure(tp, typeof(MMFGSStruct));

                    // convertFromMMFGSStruct 将共享内存传递的计算结果转换为GridStrength
                    this.MultiTasksGridStrengths.Add(convertFromMMFGSStruct(data));
                }
                catch (Exception e)
                {
                    Console.WriteLine(string.Format("read error : tp = {0}, dsize = {1}, msg = {2}", tp.ToInt32(), dsize, e.Message));
                    //MessageBox.Show(string.Format("read error : tp = {0}, dsize = {1}, msg = {2}", tp.ToInt32(), dsize, e.Message));
                }
            }
            Console.WriteLine("after read....");
            MMF.UnmapViewOfFile(reader);
            MMF.CloseHandle(mmf);
            mmf = reader = IntPtr.Zero;
            t2 = DateTime.Now;
            Console.WriteLine(string.Format("read {0} done. now total : {1}, using time: {2}", shareName, this.MultiTasksGridStrengths.Count, (t2 - t1).TotalMilliseconds));

            //this.outGSs();

            //if (++this.procDoneNum == Math.Min(this.maxProcNum, this.processNum - this.procDoneNum1))
            if (++this.procDoneNum == this.processNum)
            {
                //Console.WriteLine(string.Format("merge outcome..., init num : {0}", this.GridStrengths.Count));
                //Console.ReadKey();
                DateTime d1, d2;
                d1 = DateTime.Now;
                CalcGridStrength calc = new CalcGridStrength(this.cellInfo, null);
                this.GridStrengths = calc.MergeMultipleTaskStrength(this.MultiTasksGridStrengths);
                d2 = DateTime.Now;
                Console.WriteLine(string.Format("merge done. now num : {0}, using time: {1}ms", this.GridStrengths.Count, (d2 - d1).TotalMilliseconds));

                // 2017.6.14
                Console.WriteLine();
                Console.WriteLine("射线所能达到的最远地面距离: {0} m，该点功率：{1} dbm", calc.maxDistGround, calc.dbm);
                Console.WriteLine("小区平面坐标：({0}, {1})", calc.cellInfo.SourcePoint.X, calc.cellInfo.SourcePoint.Y);
                Console.WriteLine("射线所能达到的最远地面坐标：({0}, {1})", calc.gx, calc.gy);
                Console.WriteLine("覆盖栅格总数: {0}", this.GridStrengths.Count);
                Console.WriteLine();

                //Console.WriteLine("connection = " + DB.DataUtil.ConnectionString);

                Console.WriteLine("writing to database ...");

                GridCover gc = GridCover.getInstance();
                gc.convertToDt(this.GridStrengths);
                Hashtable ht = new Hashtable();
                ht["eNodeB"] = this.cellInfo.eNodeB;
                ht["CI"] = this.cellInfo.CI;
                //gc.deleteGroundCover(ht);
                gc.wirteGroundCover(ht);
                gc.clearGround();
                //if (this.computeIndoor)
                {
                    //gc.deleteBuildingCover(ht);
                    gc.writeBuildingCover(ht);
                    gc.clearBuilding();
                }
                Console.WriteLine("地面栅格总数: {0}", gc.ng);
                Console.WriteLine("立体栅格总数: {0}", gc.nb);
                end = DateTime.Now;
                string info = string.Format("总运行时间：{0}毫秒\n", (end - start).TotalMilliseconds);
                Console.WriteLine(info);
                Console.WriteLine("write done");

                this.GridStrengths.Clear();
                Console.ReadKey(); // 2019.04.12
                this.cs.free();
                this.Close();  // 2019.04.12
                //this.procDoneNum = 0;
            }
        }

        /// <summary>
        /// 将共享内存传递的计算结果转换为GridStrength
        /// </summary>
        /// <param name="ret"></param>
        /// <returns></returns>
        private GridStrength convertFromMMFGSStruct(MMFGSStruct ret)
        {
            GridStrength gs = new GridStrength();
            gs.GXID = ret.GXID;
            gs.GYID = ret.GYID;
            gs.Level = ret.Level;
            gs.GCenter = new LTE.Geometric.Point(ret.x, ret.y, ret.z);
            gs.eNodeB = ret.eNodeB;
            gs.CI = ret.CI;

            gs.FieldIntensity = ret.FieldIntensity;
            gs.DirectNum = ret.DirectNum;
            gs.DirectPwrW = ret.DirectPwrW;
            gs.MaxDirectPwrW = ret.MaxDirectPwrW;

            gs.RefNum = ret.RefNum;
            gs.RefPwrW = ret.RefPwrW;
            gs.MaxRefPwrW = ret.MaxRefPwrW;
            gs.RefBuildingID = ret.RefBuildingID;

            gs.DiffNum = ret.DiffNum;
            gs.DiffPwrW = ret.DiffPwrW;
            gs.MaxDiffPwrW = ret.MaxDiffPwrW;
            gs.DiffBuildingID = ret.DiffBuildingID;

            gs.TransNum = ret.TransNum;
            gs.TransPwrW = ret.TransPwrW;
            gs.MaxTransPwrW = ret.MaxTransPwrW;
            gs.TransmitBuildingID = ret.TransmitBuildingID;

            gs.BTSGridDistance = ret.BTSGridDistance;
            gs.ReceivedPowerW = ret.ReceivedPowerW;
            gs.ReceivedPowerdbm = ret.ReceivedPowerdbm;
            gs.PathLoss = ret.PathLoss;

            return gs;
        }

        public void outGS(GridStrength gs)
        {
            string s = string.Format("gxid={0}, gyid={1}, level={2}, lac={3}, ci={4}", gs.GXID, gs.GYID, gs.Level, gs.eNodeB, gs.CI);
            Console.WriteLine(s);
        }

        public void outGSs()
        {
            Console.WriteLine("count = " + this.MultiTasksGridStrengths.Count);
            GridStrength gs;
            for (int i = this.MultiTasksGridStrengths.Count - 1, j = i; i > -1 && i > j - 10; i--)
            {
                gs = this.MultiTasksGridStrengths[i];
                Console.WriteLine(i);
                if (gs.GCenter == null)
                    continue;
                string s = string.Format("gxid={0}, gyid={1}, level={2}, lac={3}, ci={4}, px = {5}, rxlev = {6}", gs.GXID, gs.GYID, gs.Level, gs.eNodeB, gs.CI, gs.GCenter.X, gs.ReceivedPowerW);
                Console.WriteLine(s);
            }
            Console.WriteLine("out done");
        }

        private GridStrength convertFromBytes(byte[] b)
        {
            return new GridStrength();
        }

        private object BytesToStruct(byte[] buf, int len, Type type)
        {
            object rtn;
            IntPtr buffer = Marshal.AllocHGlobal(len);
            Marshal.Copy(buf, 0, buffer, len);
            rtn = Marshal.PtrToStructure(buffer, type);
            Marshal.FreeHGlobal(buffer);
            return rtn;
        }

        /// <summary>
        /// 根据子进程句柄获取共享内存名字
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        private string getMMFName(IntPtr handle)
        {
            foreach (var pa in this.paList)
            {
                if (pa.handle == handle)
                {
                    return pa.MMFName;
                }
            }
            return string.Empty;
        }

        private void CtrlForm_Load(object sender, EventArgs e)
        {
            //MessageBox.Show(AppDomain.CurrentDomain.BaseDirectory);
            //MessageBox.Show(AppDomain.CurrentDomain.BaseDirectory);
        }
    }

    internal class ProcessArgs
    {
        //public Process pro;
        /// <summary>
        /// Process.MainWindowHandle有时候为空，且该字段只读，所以特殊设置保存handle
        /// </summary>
        public IntPtr handle;
        public string MMFName;
        public ProcessArgs()
        {
            this.handle = IntPtr.Zero;
        }

        public ProcessArgs(IntPtr handle, string mmf)
        {
            this.handle = handle;
            this.MMFName = mmf;
        }
    }
}
