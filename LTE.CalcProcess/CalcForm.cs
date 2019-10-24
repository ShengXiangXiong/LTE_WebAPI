using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LTE.Win32Lib;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;
using System.Data.SqlClient;

using LTE.InternalInterference;
using LTE.InternalInterference.Grid;
using LTE.Geometric;
using LTE.DB;
using LTE.Beam;
using LTE.Model;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;

namespace LTE.CalcProcess
{
    public partial class CalcForm : Form
    {
        private string basePath = AppDomain.CurrentDomain.BaseDirectory;
        /// <summary>
        /// 共享内存名字
        /// </summary>
        private string MMFName;
        private string MMFName1;
        private CellInfo cellInfo;
        private RayTracing interAnalysis;

        /// <summary>
        /// 是否正在计算
        /// </summary>
        private bool computing;
        /// <summary>
        /// 用于进度条显示
        /// </summary>
        private int totalRay;

        //loading control
        private int userId;
        private string taskName;
        private LoadInfo loadInfo = new LoadInfo();

        //Http
        private HttpClient httpClient = new HttpClient();

        private double fromAngle;
        private double toAngle;
        private double distance;
        private int reflectionNum;
        private int diffractionNum;
        private bool computeIndoor;
        private double diffPointsMargin;
        private bool computeVSide;
        private double deltaA;  // 计算大区域覆盖分析时用到

        private bool reRay;  // 是否为二次投射
        private bool isRayLoc; // 生成的射线用于定位
        private bool isRayAdj; // 生成的射线用于系数校正
        private bool isRecordReray;  // 是否记录当前批出界射线，以供下批二次投射 2019.5.22
        private RayLocAdj rayLocate;

        private Geometric.Point p1, p2, p3, p4;  // 如果进行分批计算，当前批的计算矩形范围的四个顶点 2019.5.22

        private List<int> bids;

        private PathSolution solution;  // beam

        private IntPtr parentHandle;

        private ConsoleShow cs;

        public void LoadApi() { }


        /// <summary>
        /// HttpClient实现Post请求
        /// </summary>
        public async void doPostLoading(LoadInfo loadInfo,string action)
        {
            string url = "http://localhost:3298/api/Loading/"+action;

            HttpContent httpContent = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(loadInfo));
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            httpContent.Headers.ContentType.CharSet = "utf-8";
            try
            {
                //httpClient = new HttpClient();
                //AuthenticationHeaderValue authValue = new AuthenticationHeaderValue("Basic", token);
                //httpClient.DefaultRequestHeaders.Authorization = authValue;
                HttpResponseMessage response = await httpClient.PostAsync(url, httpContent);
            }
            catch (Exception)
            {
                throw;
            }

        }



        public CalcForm()
        {
            InitializeComponent();

            reRay = false;   // 不进行二次投射

            string[] args = System.Environment.GetCommandLineArgs();
            if (dealParams(args))
            {
                // 当前批计算范围
                LTE.Geometric.Point source = this.cellInfo.SourcePoint;
                if (this.isRecordReray || this.reRay)  // 如果是分批计算
                {
                    if ((toAngle - fromAngle + 360) % 360 > deltaA)
                        toAngle = fromAngle + deltaA;
                    double from = fromAngle * Math.PI / 180;
                    double to = toAngle * Math.PI / 180;
                    double alpha = (from + to) / 2.0;
                    double theta = Math.Abs(to - from) / 2;
                    double r = Math.Min(1.5 * distance, distance / Math.Cos(theta));  // 半对角线长度
                    double r1 = 0.5 * distance;
                    p1 = new Geometric.Point(source.X + r * Math.Sin(alpha - theta), source.Y + r * Math.Cos(alpha - theta), 10);
                    p2 = new Geometric.Point(source.X - r * Math.Sin(theta) * Math.Cos(alpha) - r1 * Math.Sin(alpha), source.Y + r * Math.Sin(theta) * Math.Sin(alpha) - r1 * Math.Cos(alpha), 10);
                    p3 = new Geometric.Point(source.X + r * Math.Sin(theta) * Math.Cos(alpha) - r1 * Math.Sin(alpha), source.Y - r * Math.Sin(theta) * Math.Sin(alpha) - r1 * Math.Cos(alpha), 10);
                    p4 = new Geometric.Point(source.X + r * Math.Sin(alpha + theta), source.Y + r * Math.Cos(alpha + theta), 10);
                }

                //MessageBox.Show("我是计算进程{0}({1:0.0}~{2:0.0}), 不能关闭");
                this.Text = string.Format("我是计算进程{0}({1:0.0}~{2:0.0}), 不能关闭", args[2], this.fromAngle, this.toAngle);
                if (this.isRayLoc || this.isRayAdj)
                {
                    this.rayLocate = new RayLocAdj(this.cellInfo, this.reflectionNum, this.diffractionNum, this.computeIndoor,
                        this.distance, this.fromAngle, this.toAngle, this.deltaA, isRayLoc, isRayAdj);
                }
                else
                {
                    this.interAnalysis = new RayTracing(this.cellInfo, this.reflectionNum, this.diffractionNum,
                        this.computeIndoor, this.distance, this.isRecordReray, ref p1, ref p2, ref p3, ref p4);
                }
                Console.WriteLine(this.Text);
                this.computing = false;
                this.sendReady();
                //MessageBox.Show("this.sendReady()");
            }
            else
            {
                System.Environment.Exit(0);
            }
            loadInfo.UserId = userId;
            loadInfo.taskName = taskName;
        }

        private bool dealParams(string[] args)
        {
            if (args.Length == 37)
            {
                try
                {
                    cs = new ConsoleShow();
                    //MessageBox.Show("读取参数 ...");
                    this.label1.Text = "读取参数 ...";
                    this.MMFName = string.Format("{0}_{1}", args[1], this.Handle);
                    this.MMFName1 = this.MMFName + "_ReRay";
                    Console.WriteLine("memory name : {0}", this.MMFName);
                    Console.WriteLine("child {0}", args[2]);

                    cellInfo = new CellInfo();
                    cellInfo.SourceName = args[3];
                    cellInfo.SourcePoint = new LTE.Geometric.Point(Convert.ToDouble(args[4]), Convert.ToDouble(args[5]), Convert.ToDouble(args[8]));
                    cellInfo.eNodeB = Convert.ToInt32(args[9]);
                    cellInfo.CI = Convert.ToInt32(args[10]);
                    cellInfo.Azimuth = Convert.ToDouble(args[11]);
                    cellInfo.Inclination = Convert.ToDouble(args[12]);
                    cellInfo.RayAzimuth = 0;
                    cellInfo.RayInclination = 0;
                    cellInfo.cellType = args[13] == "GSM900" ? CellType.GSM900 : CellType.GSM1800;
                    cellInfo.frequncy = Convert.ToInt32(args[14]);
                    cellInfo.EIRP = Convert.ToDouble(args[15]);

                    cellInfo.directCoefficient = Convert.ToSingle(args[16]);
                    cellInfo.reflectCoefficient = Convert.ToSingle(args[17]);
                    cellInfo.diffracteCoefficient = Convert.ToSingle(args[18]);
                    cellInfo.diffracteCoefficient2 = Convert.ToSingle(args[19]);

                    this.fromAngle = Convert.ToDouble(args[20]);
                    this.toAngle = Convert.ToDouble(args[21]);
                    this.distance = Convert.ToDouble(args[22]);
                    this.reflectionNum = Convert.ToInt32(args[23]);
                    this.diffractionNum = Convert.ToInt32(args[24]);
                    this.computeIndoor = Convert.ToBoolean(args[25]);
                    //主进程handle
                    this.parentHandle = new IntPtr(Convert.ToInt32(args[26]));

                    //MessageBox.Show(args[26]);
                    string[] bidstext = args[27].Split(',');

                    this.bids = new List<int>();
                    for (int i = 0, cnt = bidstext.Length; i < cnt; i++)
                    {
                        if (i > 0)
                        {
                            this.bids.Add(Convert.ToInt32(bidstext[i]));
                        }
                    }

                    this.diffPointsMargin = Convert.ToDouble(args[28]);
                    this.computeVSide = Convert.ToBoolean(args[29]);
                    this.deltaA = Convert.ToDouble(args[30]);
                    this.reRay = Convert.ToBoolean(args[31]);
                    this.isRecordReray = Convert.ToBoolean(args[32]);
                    this.isRayLoc = Convert.ToBoolean(args[33]);
                    this.isRayAdj = Convert.ToBoolean(args[34]);
                    //MessageBox.Show(args[31] + " " + args[32] + " " + args[33] + " " + args[34] + " ");

                    //执行此程序的用户id以及taskName
                    this.userId = Convert.ToInt32(args[35]);
                    this.taskName = args[36];

                    return true;
                }
                catch (Exception ee)
                {
                    MessageBox.Show("参数转换出错，出错原因：" + ee.Message);
                }
            }
            else
            {
                MessageBox.Show("CalcForm 参数个数不匹配，程序退出" + args.Length);
            }
            return false;
        }

        public void sendReady()
        {
            IPC.PostMessage(this.parentHandle, IPC.WM_POST_READY, this.Handle, 0);
        }

        protected override void DefWndProc(ref System.Windows.Forms.Message m)
        {
            switch (m.Msg)
            {
                case IPC.WM_POST_NOTIFY:
                    if (!this.computing)
                    {
                        //MessageBox.Show("子进程接收到启动计算信号...");
                        Console.WriteLine("子进程接收到启动计算信号...");
                        if (this.isRayLoc)
                        {
                            //MessageBox.Show("1");
                            this.startCalcLoc();
                        }
                        else if (this.isRayAdj)
                            this.startCalcAdj();
                        else
                        {
                            if (true)
                                this.startCalc();
                            else
                            {
                                // 2019.1.12 尹静萍
                                // 通过beam跟踪进行覆盖分析
                                // 没有用到，仅仅是为了验证正确性
                                // 相对于射线跟踪，没有处理覆盖空洞
                                this.startCalcBeam();
                            }
                        }
                    }
                    break;
                case IPC.WM_POST_Kill:
                    System.Environment.Exit(0);
                    break;
                default:
                    base.DefWndProc(ref m);
                    break;
            }
        }

        # region 小区覆盖计算
        /// <summary>
        /// 构建加速结构和内存数据等
        /// </summary>
        private void beforeCalc()
        {
            DateTime t0, t1, t2, t3, t4;
            t0 = DateTime.Now;

            //生成加速结构
            Grid3D accgrid = new Grid3D(), ggrid = new Grid3D();

            // 返回空间点(大地坐标)所在的加速网格坐标
            if (!GridHelper.getInstance().PointXYZToAccGrid(cellInfo.SourcePoint, ref accgrid))
            {
                MessageBox.Show("无法获取小区所在加速网格坐标，计算结束！");
                return;
            }

            //建筑物信息加速
            // 空间点（大地坐标）所在的立体网格
            if (!GridHelper.getInstance().PointXYZToGrid3D(cellInfo.SourcePoint, ref ggrid))
            {
                MessageBox.Show("无法获取小区所在地面网格坐标，计算结束！");
                return;
            }

            int mingxid = -1, maxgxid = -1, mingyid = -1, maxgyid = -1;
            int gridlength = GridHelper.getInstance().getAGridSize();
            int deltagrid = (int)Math.Ceiling(this.distance * 1.5 / gridlength);

            int maxAGXID = 0, maxAGYID = 0, minAGXID = 0, minAGYID = 0;
            GridHelper.getInstance().getMaxAccGridXY(ref maxAGXID, ref maxAGYID);
            GridHelper.getInstance().getMinAccGridXY(ref minAGXID, ref minAGYID);
            mingxid = accgrid.gxid - deltagrid;
            mingyid = accgrid.gyid - deltagrid;
            maxgxid = accgrid.gxid + deltagrid;
            maxgyid = accgrid.gyid + deltagrid;

            AccelerateStruct.clearAccelerateStruct();
            BuildingGrid3D.clearBuildingData();
            BuildingGrid3D.clearBuildingVertexOriginal();
            BuildingGrid3D.clearGrid3D();
            AccelerateStruct.setAccGridRange(mingxid, mingyid, maxgxid, maxgyid);

            double extend = 1.5;
            //Console.ReadKey();
            // 从数据库表tbAccelerateGridBuilding中取出所有符合条件的数据,并以GXID,GYID,GZID排序, 构造结果集的哈希表
            AccelerateStruct.constructAccelerateStruct();
            t1 = DateTime.Now;

            //MessageBox.Show("AccelerateStruct.constructAccelerateStruct()");
            gridlength = GridHelper.getInstance().getGGridSize();
            deltagrid = (int)Math.Ceiling(this.distance * extend / gridlength);
            int maxGXID = 0, maxGYID = 0, minGXID = 0, minGYID = 0;
            GridHelper.getInstance().getMaxGGridXY(ref maxGXID, ref maxGYID);
            GridHelper.getInstance().getMinGGridXY(ref minGXID, ref minGYID);
            mingxid = ggrid.gxid - deltagrid;
            mingyid = ggrid.gyid - deltagrid;
            maxgxid = ggrid.gxid + deltagrid;
            maxgyid = ggrid.gyid + deltagrid;

            BuildingGrid3D.setGGridRange(mingxid, mingyid, maxgxid, maxgyid);

            LTE.Geometric.Point source = this.cellInfo.SourcePoint;

            // 构建建筑物底面中心点、高度数据、所有点
            if (this.isRecordReray || this.reRay)  // 如果是分批计算
                BuildingGrid3D.constructBuildingData(ref p1, ref p2, ref p3, ref p4);
            else
                BuildingGrid3D.constructBuildingData();

            t2 = DateTime.Now;

            if (this.computeIndoor)
            {
                if (this.isRecordReray || this.reRay)  // 如果是分批计算
                    BuildingGrid3D.constructGrid3D(ref p1, ref p2, ref p3, ref p4);
                else
                    BuildingGrid3D.constructGrid3D();
            }

            t3 = DateTime.Now;

            deltagrid = (int)Math.Ceiling(deltagrid / extend);
            mingxid = ggrid.gxid - deltagrid;
            mingyid = ggrid.gyid - deltagrid;
            maxgxid = ggrid.gxid + deltagrid;
            maxgyid = ggrid.gyid + deltagrid;
            GroundGrid.setBound(mingxid, mingyid, maxgxid, maxgyid);

            // 获取中心点在范围内的地面栅格中心点
            if (this.isRecordReray || this.reRay)  // 如果是分批计算
            {
                if (GroundGrid.constructGGrids(ref p1, ref p2, ref p3, ref p4) == 0)
                {
                    //当前区域没有数据则返回父进程，而不是直接退出，若直接退出会造成父进程假死
                    IPC.PostMessage(this.parentHandle, IPC.WM_POST_CALCDONE, this.Handle, 0);
                    System.Environment.Exit(0);
                }
            }
            else
            {
                if (GroundGrid.constructGGrids() == 0)
                {
                    //当前区域没有数据则返回父进程，而不是直接退出，若直接退出会造成父进程假死
                    IPC.PostMessage(this.parentHandle, IPC.WM_POST_CALCDONE, this.Handle, 0);
                    System.Environment.Exit(0);
                }
            }

            t4 = DateTime.Now;

            // 2019.5.30 地形
            double range = this.distance * extend;
            //Geometric.Point source = cellInfo.SourcePoint;
            TINInfo.setBound(source.X - range, source.Y - range, source.X + range, source.Y + range);
            int nTIN = TINInfo.constructTINVertex();

            int nA = AccelerateStruct.accgrids.Count;
            int nB = BuildingGrid3D.buildingCenter.Count;
            int nG = GroundGrid.ggrids.Count;
            Console.WriteLine("加速网格数量: {0}", nA);
            Console.WriteLine("立体网格数量: {0}", nB);
            Console.WriteLine("地面网格数量: {0}", nG);
            int n = nA + nB + nG;
            Console.WriteLine("网格总数量: {0}", n);
            Console.WriteLine("TIN 数量: {0}", nTIN);

            /*
             * 地面栅格：34 byte   10 + 24 = 34
               加速栅格：20 byte   平均一个加速栅格2个建筑物 12 + 4*2 = 20
               建筑物栅格： 512 byte  4 + 12 = 16   平均一个建筑物320个栅格 16 * 32 = 512
               建筑物底面中心：28 byte 4 + 24 = 28
               建筑物高度：12 byte 4 + 8 = 12
               建筑物底面顶点：168 byte  4 + 24 = 28 平均一个建筑物6个顶点 28 * 6 = 168
               建筑物顶面顶点：1400 byte  4 + 24 = 28 平均一个建筑物50个顶面栅格 28 * 50 = 1400
             */
            Console.WriteLine("约占内存: {0} M", (nG * 34 + nA * 20 + nB * (28 + 12 + 168 + 1400)) / 1048576);

            Console.WriteLine(string.Format("加速栅格：{0}秒", (t1 - t0).TotalMilliseconds / 1000));
            Console.WriteLine(string.Format("建筑物：{0}秒", (t2 - t1).TotalMilliseconds / 1000));
            Console.WriteLine(string.Format("建筑物栅格：{0}", (t3 - t2).TotalMilliseconds / 1000));
            Console.WriteLine(string.Format("地面栅格：{0}", (t4 - t3).TotalMilliseconds / 1000));
        }

        private void startCalc()
        {
            DateTime t0, t1, t2, t3, t4, t5, t6;
            t0 = DateTime.Now;

            this.computing = true;
            this.label1.Text = "正在构建加速结构，即将开始计算 ...";
            this.label1.Refresh();
            this.beforeCalc();

            this.label1.Text = "生成射线 ...";
            this.label1.Refresh();

            int gray, bray = 0, vray;
            LTE.Geometric.Point source = this.cellInfo.SourcePoint;

            t1 = DateTime.Now;

            //获取覆盖扇形范围内的建筑物id，从BuildingGrid3D.buildingCenter中筛
            this.bids = BuildingGrid3D.getBuildingIDBySector(source, this.distance, this.fromAngle, this.toAngle);

            //MessageBox.Show("1");

            //将位于扇区覆盖范围内的地面栅格加进来
            List<LTE.Geometric.Point> gfPoints = GroundGrid.getGGridCenterBySector(source, this.distance, this.fromAngle, this.toAngle, null);

            //建筑物顶面栅格
            List<LTE.Geometric.Point> topPoints = TopPlaneGrid.GetAllTopGrid(source, this.bids);

            //MessageBox.Show("3");

            //建筑物立面栅格  2019.6.11 地形
            List<LTE.Geometric.Point> vPoints = VerticalPlaneGrid.GetAllVerticalGrid(source, this.bids, diffPointsMargin);
            double mergeAngle = 5.0 / 2000;//弧度制
            // 合并射线终点，射线的角度小于mergeAngle的合并
            List<LTE.Geometric.Point> vmPoints = GeometricUtilities.mergePointsByAngle(source, vPoints, mergeAngle);

            // 2019.6.11 地形
            //将建筑物按照到小区的距离排序，得到每个建筑物相对于小区的最小、最大角度，然后去掉被遮挡的建筑，并更新被遮挡建筑物可见部分
            List<TriangleBound> disAngle = BuildingGrid3D.getShelterDisAndAngle(source, this.bids, 0);

            //建筑物棱边栅格
            List<LTE.Geometric.Point> diffPoints = new List<LTE.Geometric.Point>();
            if (computeVSide)
                diffPoints = BuildingGrid3D.getBuildingsEdgePointsByShelter(source.Z, disAngle, diffPointsMargin);

            // 立体网格数量，只计算了建筑内部的，不包括表面
            int litiCnt = 0;
            for (int i = 0; i < this.bids.Count; i++)
            {
                if (BuildingGrid3D.bgrid3d.ContainsKey(this.bids[i]))
                    litiCnt += BuildingGrid3D.bgrid3d[this.bids[i]].Count;
            }

            // 高中低建筑物占比
            int h1 = 0;  // <10
            int h2 = 0;  // 10 ~ 20
            int h3 = 0;  // >20
            double h = 0;
            for (int i = 0; i < this.bids.Count; i++)
            {

                double ht = BuildingGrid3D.getBuildingHeight(this.bids[i]);
                h += ht;
                if (ht < 10)
                    h1++;
                else if (ht < 30)
                    h2++;
                else
                    h3++;
            }
            Console.WriteLine("disAngle num = {0}", disAngle.Count);
            Console.WriteLine("建筑物总数 = {0}", BuildingGrid3D.buildingCenter.Count);
            Console.WriteLine("扇区内建筑物数量 = {0}", this.bids.Count);
            Console.WriteLine("扇区内地面栅格数量 = {0}", gfPoints.Count);
            Console.WriteLine("扇区内立体栅格数量 = {0}", litiCnt);
            Console.WriteLine("扇区内建筑物顶面接收点数量 = {0}", topPoints.Count);
            Console.WriteLine("扇区内建筑物立面接收点数量 = {0}", vmPoints.Count);
            Console.WriteLine("扇区内建筑物棱边接收点数量 = {0}", diffPoints.Count);
            Console.WriteLine("扇区内<10m建筑物数量 = {0}, 占比 = {1} ", h1, (double)h1 / (double)this.bids.Count);
            Console.WriteLine("扇区内10~30m建筑物数量 = {0}, 占比 = {1} ", h2, (double)h2 / (double)this.bids.Count);
            Console.WriteLine("扇区内>30m建筑物数量 = {0}, 占比 = {1} ", h3, (double)h3 / (double)this.bids.Count);
            Console.WriteLine("扇区内建筑物占比 = {0}", (double)topPoints.Count / (double)gfPoints.Count);


            gray = gfPoints.Count;
            gray += topPoints.Count;
            bray = vmPoints.Count;
            vray = diffPoints.Count;

            //init load
            int count = gray + bray + vray;

            loadInfo.cnt = 0;
            loadInfo.count = count;
            loadInfo.UserId = userId;
            loadInfo.taskName = taskName;
            doPostLoading(loadInfo, "addCountByMulti");


            t2 = DateTime.Now;
            this.label1.Text = "直射分析 ...";
            this.label1.Refresh();
            this.analysis(gfPoints, 1);
            gray = gfPoints.Count;

            Console.WriteLine("直射分析完成");

            this.analysis(topPoints, 2);
            gray += topPoints.Count;

            //t5 = DateTime.Now;
            this.label1.Text = "反射、室内分析...";
            this.label1.Refresh();
            this.analysis(vmPoints, 3);
            bray = vmPoints.Count;

            Console.WriteLine("反射、室内分析完成");

            t3 = DateTime.Now;

            this.label1.Text = "绕射分析...";
            this.label1.Refresh();
            this.analysis(diffPoints, 4);
            vray = diffPoints.Count;

            Console.WriteLine("绕射线分析完成");

            this.label1.Text = "分析完成...";
            this.label1.Refresh();

            t4 = DateTime.Now;

            if (this.reRay)  // 需要二次投射
            {
                this.label1.Refresh();
                SourceInfo sourceInfo = this.cellInfo.clone();
                Hashtable ht = new Hashtable();
                ht["CI"] = this.cellInfo.CI;
                DataTable tbReRay = IbatisHelper.ExecuteQueryForDataTable("getReRay", ht);

                List<ReRay> rays = new List<ReRay>();

                foreach (DataRow dataRow in tbReRay.Rows)
                {
                    ReRay ray = new ReRay();
                    ray.emitPtX = double.Parse(dataRow["emitX"].ToString());
                    ray.emitPtY = double.Parse(dataRow["emitY"].ToString());
                    ray.emitPtZ = double.Parse(dataRow["emitZ"].ToString());
                    Geometric.Point emitPt = new Geometric.Point(ray.emitPtX, ray.emitPtY, ray.emitPtZ);

                    // 判断发射点是否位于当前批计算范围内
                    bool ok1 = (p1.X - emitPt.X) * (p2.Y - emitPt.Y) - (p2.X - emitPt.X) * (p1.Y - emitPt.Y) > 0;
                    bool ok2 = (p2.X - emitPt.X) * (p3.Y - emitPt.Y) - (p3.X - emitPt.X) * (p2.Y - emitPt.Y) > 0;
                    bool ok3 = (p3.X - emitPt.X) * (p4.Y - emitPt.Y) - (p4.X - emitPt.X) * (p3.Y - emitPt.Y) > 0;
                    bool ok4 = (p4.X - emitPt.X) * (p1.Y - emitPt.Y) - (p1.X - emitPt.X) * (p4.Y - emitPt.Y) > 0;
                    if (!(ok1 && ok2 && ok3 && ok4))
                        continue;

                    ray.pwrDbm = double.Parse(dataRow["pwrDbm"].ToString());
                    ray.dirX = double.Parse(dataRow["dirX"].ToString());
                    ray.dirY = double.Parse(dataRow["dirY"].ToString());
                    ray.dirZ = double.Parse(dataRow["dirZ"].ToString());
                    rays.Add(ray);
                }

                this.analysis(ref rays);
            }

            t5 = DateTime.Now;

            this.afterCalc();

            t6 = DateTime.Now;

            Console.WriteLine(string.Format("网格载入：{0}秒", (t1 - t0).TotalMilliseconds / 1000));
            Console.WriteLine(string.Format("生成筛选：{0}秒", (t2 - t1).TotalMilliseconds / 1000));
            Console.WriteLine(string.Format("绕射用时：{0}秒", (t4 - t3).TotalMilliseconds / 1000));
            Console.WriteLine(string.Format("射线跟踪：{0}秒", (t4 - t2).TotalMilliseconds / 1000));
            if (reRay)
                Console.WriteLine(string.Format("二次投射：{0}秒", (t5 - t4).TotalMilliseconds / 1000));
            Console.WriteLine(string.Format("写入mmf：{0}秒", (t6 - t5).TotalMilliseconds / 1000));
            Console.WriteLine(string.Format("总运行时间：{0}秒", (t6 - t0).TotalMilliseconds / 1000));

            Console.WriteLine();
            Console.WriteLine("射线数量 = {0}", this.interAnalysis.rayCount);
            Console.WriteLine("直射线数量 = {0}", this.interAnalysis.rayCountDir);
            Console.WriteLine("绕射线数量 = {0}", this.interAnalysis.rayCountDif);
            Console.WriteLine("反射线数量 = {0}", this.interAnalysis.rayCountRef);
            Console.WriteLine("透射线数量 = {0}", this.interAnalysis.rayCountTra);
            Console.WriteLine("地面有效直射线数量 = {0}", this.interAnalysis.rayCountDirG);
            Console.WriteLine("楼顶有效直射线数量 = {0}", this.interAnalysis.rayCountDirB);
            Console.WriteLine("初级反射线数量 = {0}", this.interAnalysis.rayCountRef1);
            Console.WriteLine("次级反射线数量 = {0}", this.interAnalysis.rayCountRef2);
            Console.WriteLine("三级反射线数量 = {0}", this.interAnalysis.rayCountRef3);
            Console.WriteLine("初级绕射线数量 = {0}", this.interAnalysis.rayCountDif1);
            Console.WriteLine("次级绕射线数量 = {0}", this.interAnalysis.rayCountDif2);
            // 一条射线约占350字节
            Console.WriteLine("射线占约内存数：{0} M", this.interAnalysis.rayCount * 350 / 1048576);

            //Console.ReadKey();
            this.cs.free();
            this.Close();
        }
        #endregion

        #region 生成用于定位的射线
        /// <summary>
        /// 构建加速结构和内存数据等
        /// </summary>
        private void beforeCalcLoc()
        {
            DateTime t0, t1;
            t0 = DateTime.Now;

            //生成加速结构
            Grid3D accgrid = new Grid3D();

            // 返回空间点(大地坐标)所在的加速网格坐标
            if (!GridHelper.getInstance().PointXYZToAccGrid(cellInfo.SourcePoint, ref accgrid))
            {
                MessageBox.Show("无法获取小区所在加速网格坐标，计算结束！");
                return;
            }

            //建筑物信息加速
            // 空间点（大地坐标）所在的立体网格
            Grid3D ggrid = new Grid3D();
            if (!GridHelper.getInstance().PointXYZToGrid3D(cellInfo.SourcePoint, ref ggrid))
            {
                MessageBox.Show("无法获取小区所在地面网格坐标，计算结束！");
                return;
            }

            double extend = 1.3;

            int mingxid = -1, maxgxid = -1, mingyid = -1, maxgyid = -1;
            int gridlength = GridHelper.getInstance().getAGridSize();
            int deltagrid = (int)Math.Ceiling(this.distance * extend / gridlength);

            int maxAGXID = 0, maxAGYID = 0, minAGXID = 0, minAGYID = 0;
            GridHelper.getInstance().getMaxAccGridXY(ref maxAGXID, ref maxAGYID);
            GridHelper.getInstance().getMinAccGridXY(ref minAGXID, ref minAGYID);
            mingxid = accgrid.gxid - deltagrid;
            mingyid = accgrid.gyid - deltagrid;
            maxgxid = accgrid.gxid + deltagrid;
            maxgyid = accgrid.gyid + deltagrid;

            AccelerateStruct.clearAccelerateStruct();
            BuildingGrid3D.clearBuildingData();
            BuildingGrid3D.clearBuildingVertexOriginal();
            BuildingGrid3D.clearGrid3D();
            AccelerateStruct.setAccGridRange(mingxid, mingyid, maxgxid, maxgyid);

            // 地形
            // 从数据库表tbAccelerateGridBuilding中取出所有符合条件的数据,并以GXID,GYID,GZID排序, 构造结果集的哈希表
            AccelerateStruct.constructAccelerateStruct();
            t1 = DateTime.Now;

            gridlength = GridHelper.getInstance().getGGridSize();
            deltagrid = (int)Math.Ceiling(this.distance * extend / gridlength);
            int maxGXID = 0, maxGYID = 0, minGXID = 0, minGYID = 0;
            GridHelper.getInstance().getMaxGGridXY(ref maxGXID, ref maxGYID);
            GridHelper.getInstance().getMinGGridXY(ref minGXID, ref minGYID);
            mingxid = ggrid.gxid - deltagrid;
            mingyid = ggrid.gyid - deltagrid;
            maxgxid = ggrid.gxid + deltagrid;
            maxgyid = ggrid.gyid + deltagrid;

            BuildingGrid3D.setGGridRange(mingxid, mingyid, maxgxid, maxgyid);

            LTE.Geometric.Point source = this.cellInfo.SourcePoint;
            // 构建建筑物底面中心点、高度数据、所有点
            BuildingGrid3D.constructBuildingData();

            // 2019.5.30 地形
            double range = this.distance * extend;
            TINInfo.setBound(source.X - range, source.Y - range, source.X + range, source.Y + range);
            int nTIN = TINInfo.constructTINVertex();

            int nA = AccelerateStruct.accgrids.Count;
            Console.WriteLine("加速网格数量: {0}", nA);

            Console.WriteLine(string.Format("加速栅格：{0}秒", (t1 - t0).TotalMilliseconds / 1000));
        }

        // 生成用于定位的射线
        private void startCalcLoc()
        {
            DateTime t0 = DateTime.Now;

            this.computing = true;
            this.label1.Text = "正在构建加速结构，即将开始计算 ...";
            this.label1.Refresh();
            this.beforeCalcLoc();

            this.label1.Text = "射线生成 ...";
            this.label1.Refresh();

            Geometric.Point source = this.cellInfo.SourcePoint;

            //获取覆盖扇形范围内的建筑物id，从 BuildingGrid3D.buildingCenter 中筛
            this.bids = BuildingGrid3D.getBuildingIDBySector(source, this.distance, this.fromAngle, this.toAngle);
            Console.WriteLine("建筑物数：{0}", this.bids.Count);

            //将建筑物按照到小区的距离排序，得到每个建筑物相对于小区的最小、最大角度，然后去掉被遮挡的建筑
            Dictionary<int, TriangleBound> disAngle = BuildingGrid3D.getShelterDisAndAngleBeam(source, this.bids, 0);
            Console.WriteLine("没有被遮挡的建筑物数：{0}", disAngle.Count);

            DateTime t1 = DateTime.Now;

            double diffPointsMargin = 10;

            #region  反射，采用 beam 或射线跟踪

            if (false)
            {
                Console.WriteLine("射线跟踪");
                #region 采用射线跟踪生成反射线
                //建筑物立面栅格
                List<LTE.Geometric.Point> vPoints = VerticalPlaneGrid.GetAllVerticalGrid(source, this.bids, diffPointsMargin);
                double mergeAngle = 5.0 / 2000;//弧度制
                // 合并射线终点，射线的角度小于mergeAngle的合并
                List<LTE.Geometric.Point> vmPoints = GeometricUtilities.mergePointsByAngle(source, vPoints, mergeAngle);

                for (int i = 0; i < vmPoints.Count; i++)
                {
                    List<InternalInterference.NodeInfo> rayList = new List<InternalInterference.NodeInfo>();
                    this.rayLocate.rayTracingFirstLoc(source, vmPoints[i], rayList, cellInfo, InternalInterference.RayType.Direction, 3);
                }
                #endregion
            }
            else
            {
                Console.WriteLine("beam 跟踪");
                #region 采用beam生成反射线
                beamPath(ref disAngle);
                #endregion
            }
            #endregion

            DateTime t2 = DateTime.Now;

            #region 绕射

            List<Geometric.Point> diffPoints = BuildingGrid3D.getBuildingsEdgePointsByShelter(source.Z, disAngle.Values.ToList(), diffPointsMargin);

            for (int i = 0; i < diffPoints.Count; i++)
            {
                List<InternalInterference.NodeInfo> rayList = new List<InternalInterference.NodeInfo>();
                this.rayLocate.rayTracingFirstLoc(source, diffPoints[i], rayList, cellInfo, InternalInterference.RayType.Direction, 4);
            }
            #endregion

            #region 直射
            double interval = 10;
            List<Geometric.Point> pts = GroundGrid.getPointBySector(source, this.distance, this.fromAngle, this.toAngle, interval);

            // 向地面发射射线
            if (source.Z > 1)
            {
                for (int i = 0; i < pts.Count; i++)
                {
                    List<InternalInterference.NodeInfo> rayList = new List<InternalInterference.NodeInfo>();
                    this.rayLocate.rayTracingFirstLoc(source, pts[i], rayList, cellInfo, InternalInterference.RayType.Direction, 1);
                }
            }

            // 向天空发射射线
            if (source.Z < 90)
            {
                for (int i = 0; i < pts.Count; i++)
                {
                    pts[i].Z = 90;
                    List<InternalInterference.NodeInfo> rayList = new List<InternalInterference.NodeInfo>();
                    this.rayLocate.rayTracingFirstLoc(source, pts[i], rayList, cellInfo, InternalInterference.RayType.Direction, 1);
                }
            }
            #endregion

            DateTime t3 = DateTime.Now;

            this.afterCalc();

            DateTime t4 = DateTime.Now;

            Console.WriteLine("射线数量 = {0}", this.rayLocate.rayCount);
            Console.WriteLine("直射线数量 = {0}", this.rayLocate.rayCountDir);
            Console.WriteLine("绕射线数量 = {0}", this.rayLocate.rayCountDif);
            Console.WriteLine("反射线数量 = {0}", this.rayLocate.rayCountRef);
            Console.WriteLine("地面有效直射线数量 = {0}", this.rayLocate.rayCountDirG);
            Console.WriteLine("初级反射线数量 = {0}", this.rayLocate.rayCountRef1);
            Console.WriteLine("次级反射线数量 = {0}", this.rayLocate.rayCountRef2);
            Console.WriteLine("三级反射线数量 = {0}", this.rayLocate.rayCountRef3);
            Console.WriteLine("初级绕射线数量 = {0}", this.rayLocate.rayCountDif1);
            Console.WriteLine("次级绕射线数量 = {0}", this.rayLocate.rayCountDif2);
            Console.WriteLine("初次绕射点数 = {0}", diffPoints.Count);
            // 一条射线约占350字节
            Console.WriteLine("射线约占内存数：{0} M", this.rayLocate.rayCount * 350 / 1048576);

            Console.WriteLine(string.Format("直射、绕射：{0}秒", (t3 - t2).TotalMilliseconds / 1000));
            Console.WriteLine(string.Format("写入mmf：{0}秒", (t4 - t3).TotalMilliseconds / 1000));
            Console.WriteLine(string.Format("总运行时间：{0}秒", (t4 - t0).TotalMilliseconds / 1000));
        }

        #endregion

        #region beam

        // 通过beam生成反射线
        private void beamPath(ref Dictionary<int, TriangleBound> disAngle)
        {
            DateTime t1 = DateTime.Now;

            Geometric.Point source = this.cellInfo.SourcePoint;

            #region 构造 room 面的顶点应该是逆时针
            Room room = new Room();
            int k = 0;
            uint cntV = 0;

            for (int b = 0; b < bids.Count; b++)
            {
                int id = bids[b];
                float height = (float)BuildingGrid3D.buildingHeight[id];
                List<Vector3> topPlane = new List<Vector3>();
                List<LTE.Geometric.Point> vertex = BuildingGrid3D.buildingVertex[id];

                if (!disAngle.Keys.Contains(id)) // 被遮挡，不构成初级 beam
                {
                    for (int i = vertex.Count - 1, j = 0; i >= 0; j = i, i--)
                    {
                        Vector3[] v = { new Vector3((float)vertex[j].X, (float)vertex[j].Y, 0), 
                                    new Vector3((float)vertex[i].X, (float)vertex[i].Y, 0), 
                                    new Vector3((float)vertex[i].X, (float)vertex[i].Y, height), 
                                    new Vector3((float)vertex[j].X, (float)vertex[j].Y, height) };

                        Polygon polygon = new Polygon(v, 4, k++, id, false, false);
                        room.addPolygon(ref polygon);

                        topPlane.Add(new Vector3((float)vertex[i].X, (float)vertex[i].Y, height));
                        cntV += 5;
                    }
                    Polygon topFace = new Polygon(ref topPlane, k++, id, false, false);
                    room.addPolygon(ref topFace);
                }
                else
                {
                    TriangleBound tmp = disAngle[id];
                    for (int i = vertex.Count - 1, j = 0; i >= 0; j = i, i--)
                    {
                        Vector3[] v = { new Vector3((float)vertex[j].X, (float)vertex[j].Y, 0), 
                                    new Vector3((float)vertex[i].X, (float)vertex[i].Y, 0), 
                                    new Vector3((float)vertex[i].X, (float)vertex[i].Y, height), 
                                    new Vector3((float)vertex[j].X, (float)vertex[j].Y, height) };

                        Polygon polygon;
                        if (i >= tmp.minIndex && j >= tmp.minIndex && i <= tmp.maxIndex && j <= tmp.maxIndex)  // 相对小区可见
                            polygon = new Polygon(v, 4, k++, id, true, false);
                        else
                            polygon = new Polygon(v, 4, k++, id, false, false);
                        room.addPolygon(ref polygon);

                        cntV += 5;
                        topPlane.Add(new Vector3((float)vertex[i].X, (float)vertex[i].Y, height));
                    }

                    Polygon topFace;
                    if (height <= source.Z)
                        topFace = new Polygon(ref topPlane, k++, id, true, false);   // 相对小区可见
                    else
                        topFace = new Polygon(ref topPlane, k++, id, false, false);
                    room.addPolygon(ref topFace);
                }
            }

            // 2019.6.17 地形
            foreach (KeyValuePair<int, List<Geometric.Point>> item in TINInfo.TINVertex)
            {
                if (item.Value.Count < 3)
                    continue;

                // 顶点必须是逆时针排列
                Vector3[] v = { new Vector3((float)item.Value[2].X, (float)item.Value[2].Y, (float)item.Value[2].Z), 
                                new Vector3((float)item.Value[1].X, (float)item.Value[1].Y, (float)item.Value[1].Z), 
                                new Vector3((float)item.Value[0].X, (float)item.Value[0].Y, (float)item.Value[0].Z)};

                Polygon polygon = new Polygon(v, 3, k++, -1, false, true);
                room.addPolygon(ref polygon);
            }
            #endregion

            DateTime t2 = DateTime.Now;

            room.constructKD();

            DateTime t3 = DateTime.Now;

            Console.WriteLine("面的个数: {0}", room.numElements());

            // Vector3：3 * 4 = 12 个字节
            // Polygon 除了顶点：3 * 4 + 4 * 4 + 1 = 29 个字节
            Console.WriteLine("面约占内存: {0}K", cntV / 1024 * 12 + room.numElements() / 1024 * 29);
            Console.WriteLine("面约占内存: {0}M", cntV / 1048576F * 12 + room.numElements() / 1048576F * 29);

            LTE.Beam.Point src = new LTE.Beam.Point((float)source.X, (float)source.Y, (float)source.Z);
            room.addSource(ref src);  // 发射源
            room.addListener(ref src);  // 接收点，无用，为了兼容

            DateTime t4 = DateTime.Now;

            int maximumOrder = this.reflectionNum;

            solution = new PathSolution(ref room, maximumOrder, ref this.cellInfo);
            solution.m_source = room.getSource(0);
            solution.m_listener = room.getListener(0);
            solution.beamTracingPath();  // 构建 beam 树、得到路径

            DateTime t5 = DateTime.Now;

            Console.WriteLine();
            Console.WriteLine(string.Format("构造 room：{0}秒", (t2 - t1).TotalMilliseconds / 1000));
            Console.WriteLine(string.Format("构建 KD 树：{0}秒", (t3 - t2).TotalMilliseconds / 1000));
            Console.WriteLine(string.Format("设置接收点：{0}秒", (t4 - t3).TotalMilliseconds / 1000));
            Console.WriteLine(string.Format("beam 跟踪：{0}秒", (t5 - t4).TotalMilliseconds / 1000));
            Console.WriteLine(string.Format("beam 射线数量 = {0}", solution.rayCount));
            // 一条射线约占350字节
            Console.WriteLine("beam射线约占内存数：{0} M", solution.rayCount * 350 / 1048576);
            Console.WriteLine();
        }

        /// <summary>
        /// 构建加速结构和内存数据等
        /// </summary>
        private void beforeCalcBeam()
        {
            DateTime t0, t1, t2, t3, t4;
            t0 = DateTime.Now;

            //建筑物信息
            // 空间点（大地坐标）所在的立体网格
            Grid3D ggrid = new Grid3D();
            if (!GridHelper.getInstance().PointXYZToGrid3D(cellInfo.SourcePoint, ref ggrid))
            {
                MessageBox.Show("无法获取小区所在地面网格坐标，计算结束！");
                return;
            }

            double r = 1;

            int mingxid = -1, maxgxid = -1, mingyid = -1, maxgyid = -1;
            int gridlength = GridHelper.getInstance().getAGridSize();
            int deltagrid = (int)Math.Ceiling(this.distance * r / gridlength);

            AccelerateStruct.clearAccelerateStruct();
            BuildingGrid3D.clearBuildingData();
            BuildingGrid3D.clearBuildingVertexOriginal();
            BuildingGrid3D.clearGrid3D();
            AccelerateStruct.setAccGridRange(mingxid, mingyid, maxgxid, maxgyid);

            t1 = DateTime.Now;

            gridlength = GridHelper.getInstance().getGGridSize();
            deltagrid = (int)Math.Ceiling(this.distance * r / gridlength);
            int maxGXID = 0, maxGYID = 0, minGXID = 0, minGYID = 0;
            GridHelper.getInstance().getMaxGGridXY(ref maxGXID, ref maxGYID);
            GridHelper.getInstance().getMinGGridXY(ref minGXID, ref minGYID);
            mingxid = ggrid.gxid - deltagrid;
            mingyid = ggrid.gyid - deltagrid;
            maxgxid = ggrid.gxid + deltagrid;
            maxgyid = ggrid.gyid + deltagrid;

            BuildingGrid3D.setGGridRange(mingxid, mingyid, maxgxid, maxgyid);

            Geometric.Point source = this.cellInfo.SourcePoint;
            // 构建建筑物底面中心点、高度数据、所有点
            if (this.isRecordReray || this.reRay)  // 如果是分批计算
                BuildingGrid3D.constructBuildingData(ref p1, ref p2, ref p3, ref p4);
            else
                BuildingGrid3D.constructBuildingData();

            t2 = DateTime.Now;

            if (this.computeIndoor)
            {
                if (this.isRecordReray || this.reRay)  // 如果是分批计算
                    BuildingGrid3D.constructGrid3D(ref p1, ref p2, ref p3, ref p4);
                else
                    BuildingGrid3D.constructGrid3D();
            }

            t3 = DateTime.Now;

            deltagrid = (int)Math.Ceiling(deltagrid / r);
            mingxid = ggrid.gxid - deltagrid;
            mingyid = ggrid.gyid - deltagrid;
            maxgxid = ggrid.gxid + deltagrid;
            maxgyid = ggrid.gyid + deltagrid;
            GroundGrid.setBound(mingxid, mingyid, maxgxid, maxgyid);

            // 获取中心点在范围内的地面栅格中心点
            if (this.isRecordReray || this.reRay)  // 如果是分批计算
            {
                if (GroundGrid.constructGGrids(ref p1, ref p2, ref p3, ref p4) == 0)
                {
                    System.Environment.Exit(0);
                }
            }
            else
            {
                if (GroundGrid.constructGGrids() == 0)
                {
                    System.Environment.Exit(0);
                }
            }

            t4 = DateTime.Now;
            //MessageBox.Show("GroundGrid.constructGGrids()");

            int nA = AccelerateStruct.accgrids.Count;
            int nB = BuildingGrid3D.buildingCenter.Count;
            int nG = GroundGrid.ggrids.Count;
            //Console.WriteLine("加速网格数量: {0}", nA);
            //Console.WriteLine("立体网格数量: {0}", nB);
            //Console.WriteLine("地面网格数量: {0}", nG);
            //int n = nA + nB + nG;
            //Console.WriteLine("网格总数量: {0}", n);

            /*
             * 地面栅格：34 byte   10 + 24 = 34
               加速栅格：20 byte   平均一个加速栅格2个建筑物 12 + 4*2 = 20
               建筑物栅格： 512 byte  4 + 12 = 16   平均一个建筑物320个栅格 16 * 32 = 512
               建筑物底面中心：28 byte 4 + 24 = 28
               建筑物高度：12 byte 4 + 8 = 12
               建筑物底面顶点：168 byte  4 + 24 = 28 平均一个建筑物6个顶点 28 * 6 = 168
               建筑物顶面顶点：1400 byte  4 + 24 = 28 平均一个建筑物50个顶面栅格 28 * 50 = 1400
             */
            //Console.WriteLine("约占内存: {0} M", (nG * 34 + nA * 20 + nB * (28 + 12 + 168 + 1400)) / 1048576);

            //Console.WriteLine(string.Format("加速栅格：{0} s", (t1 - t0).TotalMilliseconds / 1000));
            //Console.WriteLine(string.Format("建筑物：{0} s", (t2 - t1).TotalMilliseconds / 1000));
            //Console.WriteLine(string.Format("建筑物栅格数：{0}", (t3 - t2).TotalMilliseconds / 1000));
            //Console.WriteLine(string.Format("地面栅格数：{0}", (t4 - t3).TotalMilliseconds / 1000));
        }

        // beam 覆盖分析
        private void startCalcBeam()
        {
            this.computing = true;
            this.label1.Text = "beam 跟踪即将开始计算 ...";
            this.label1.Refresh();

            DateTime t0 = DateTime.Now;

            beforeCalcBeam();

            Geometric.Point source = this.cellInfo.SourcePoint;

            // 将位于扇区覆盖范围内的地面栅格加进来
            List<Geometric.Point> gfPoints = GroundGrid.getGGridCenterBySector(source, this.distance, this.fromAngle, this.toAngle, null);

            //获取覆盖扇形范围内的建筑物id，从BuildingGrid3D.buildingCenter中筛
            this.bids = BuildingGrid3D.getBuildingIDBySector(source, this.distance, this.fromAngle, this.toAngle);

            //将建筑物按照到小区的距离排序，得到每个建筑物相对于小区的最小、最大角度，然后去掉被遮挡的建筑
            Dictionary<int, TriangleBound> disAngle = BuildingGrid3D.getShelterDisAndAngleBeam(source, this.bids, 0);

            DateTime t1 = DateTime.Now;

            #region 构造 room  面的顶点应该是逆时针
            Room room = new Room();
            int k = 0;

            for (int b = 0; b < bids.Count; b++)
            {
                int id = bids[b];
                float height = (float)BuildingGrid3D.buildingHeight[id];
                List<Vector3> topPlane = new List<Vector3>();
                List<Geometric.Point> vertex = BuildingGrid3D.buildingVertex[id];

                if (!disAngle.Keys.Contains(id)) // 被遮挡，不构成初级 beam
                {
                    for (int i = vertex.Count - 1, j = 0; i >= 0; j = i, i--)
                    {
                        Vector3[] v = { new Vector3((float)vertex[j].X, (float)vertex[j].Y, 0), 
                                    new Vector3((float)vertex[i].X, (float)vertex[i].Y, 0), 
                                    new Vector3((float)vertex[i].X, (float)vertex[i].Y, height), 
                                    new Vector3((float)vertex[j].X, (float)vertex[j].Y, height) };

                        Polygon polygon = new Polygon(v, 4, k++, id, false, false);
                        room.addPolygon(ref polygon);

                        topPlane.Add(new Vector3((float)vertex[i].X, (float)vertex[i].Y, height));
                    }
                    Polygon topFace = new Polygon(ref topPlane, k++, id, false, false);
                    room.addPolygon(ref topFace);
                }
                else
                {
                    TriangleBound tmp = disAngle[id];
                    for (int i = vertex.Count - 1, j = 0; i >= 0; j = i, i--)
                    {
                        Vector3[] v = { new Vector3((float)vertex[j].X, (float)vertex[j].Y, 0), 
                                    new Vector3((float)vertex[i].X, (float)vertex[i].Y, 0), 
                                    new Vector3((float)vertex[i].X, (float)vertex[i].Y, height), 
                                    new Vector3((float)vertex[j].X, (float)vertex[j].Y, height) };

                        Polygon polygon;
                        if (i >= tmp.minIndex && j >= tmp.minIndex && i <= tmp.maxIndex && j <= tmp.maxIndex)  // 相对小区可见
                            polygon = new Polygon(v, 4, k++, id, true, false);
                        else
                            polygon = new Polygon(v, 4, k++, id, false, false);
                        room.addPolygon(ref polygon);

                        topPlane.Add(new Vector3((float)vertex[i].X, (float)vertex[i].Y, height));
                    }

                    Polygon topFace;
                    if (height <= source.Z)
                        topFace = new Polygon(ref topPlane, k++, id, true, false);   // 相对小区可见
                    else
                        topFace = new Polygon(ref topPlane, k++, id, false, false);
                    room.addPolygon(ref topFace);
                }

            }
            #endregion

            DateTime t2 = DateTime.Now;

            Console.WriteLine(string.Format("\n加载场景：{0} s", (t2 - t0).TotalMilliseconds / 1000));

            room.constructKD();

            DateTime t3 = DateTime.Now;

            Console.WriteLine("KD 树中面的个数: {0}", room.numElements());

            Beam.Point src = new Beam.Point((float)source.X, (float)source.Y, (float)source.Z);
            room.addSource(ref src);

            // 将地面栅格转换成 beam 中的 Listener
            List<Beam.Point> listeners = new List<Beam.Point>();
            for (int i = 0; i < gfPoints.Count; i++)
            {
                room.addListener(new Beam.Point((float)gfPoints[i].X, (float)gfPoints[i].Y, (float)gfPoints[i].Z));
            }
            //Console.WriteLine("接收点个数: {0}", room.numListeners());

            DateTime t4 = DateTime.Now;


            Console.WriteLine(string.Format("构建 KD 树：{0} s", (t3 - t2).TotalMilliseconds / 1000));
            Console.WriteLine(string.Format("设置接收点：{0} s", (t4 - t3).TotalMilliseconds / 1000));

            #region  跟踪

            Dictionary<string, GridStrength> gridStrengths = new Dictionary<string, GridStrength>();
            SourceInfo source1 = this.cellInfo.clone();

            int totalRays = 0;
            int directRays = 0;
            int reflectionRays = 0;
            int totalRaySegs = 0;
            int maximumOrder = this.reflectionNum;
            solution = new PathSolution(ref room, maximumOrder, ref this.cellInfo);

            solution.m_source = room.getSource(0);

            for (int s = 0; s < room.numSources(); s++)
            {
                //for (int l = 0; l < 1; l++)
                for (int l = 0; l < room.numListeners(); l++)
                {
                    solution.m_listener = room.getListener(l);
                    solution.beamTracing();  // 构建 beam 树，得到射线
                    totalRays += solution.numPaths();

                    // 分析路径，计算场强
                    for (int i = 0; i < solution.m_rays.Count; i++)
                    {
                        // 2018.12.04
                        Beam.Rays rays = solution.m_rays[i];
                        totalRaySegs += rays.m_rays.Count;

                        // 计算场强
                        double rayAzimuth = 0;
                        double rayIncination = 0;
                        int cnt = rays.m_rays.Count;
                        Geometric.Point SourcePoint = new Geometric.Point(rays.m_rays[0].PointOfIncidence.m_position.x, rays.m_rays[0].PointOfIncidence.m_position.y, rays.m_rays[0].PointOfIncidence.m_position.z);
                        Geometric.Point endp = new Geometric.Point(rays.m_rays[0].CrossPoint.m_position.x, rays.m_rays[0].CrossPoint.m_position.y, rays.m_rays[0].CrossPoint.m_position.z);
                        GeometricUtilities.getAzimuth_Inclination(SourcePoint, endp, out rayAzimuth, out rayIncination);
                        this.interAnalysis.CalcOutDoorRayStrengthBeam(ref rays.m_rays, rayAzimuth, rayIncination);

                        for (int j = 0; j < rays.m_rays.Count; j++)
                        {
                            if (rays.m_rays[j].rayType == Beam.RayType.Direction)
                                directRays++;
                            else if (rays.m_rays[j].rayType == Beam.RayType.HReflection || rays.m_rays[j].rayType == Beam.RayType.VReflection)
                                reflectionRays++;
                        }
                    }
                    solution.m_rays.Clear();   // 2018.12.04
                }
            }
            #endregion

            DateTime t5 = DateTime.Now;

            Console.WriteLine(string.Format("射线收集：{0} s", (t5 - t4).TotalMilliseconds / 1000));
            Console.WriteLine(string.Format("路径总数：{0}", totalRays));
            Console.WriteLine(string.Format("射线总数：{0}", totalRaySegs));
            Console.WriteLine(string.Format("直射线总数：{0}", directRays));
            Console.WriteLine(string.Format("反射线总数：{0}", reflectionRays));
            // 一条射线约占350字节
            Console.WriteLine("射线约占内存数：{0} M", totalRaySegs * 350 / 1048576);

            this.afterCalc();
            this.computing = false;

            DateTime t6 = DateTime.Now;

            Console.WriteLine(string.Format("写入 mmf：{0} s", (t6 - t5).TotalMilliseconds / 1000));
            Console.WriteLine(string.Format("总运行时间：{0} s", (t6 - t0).TotalMilliseconds / 1000));

            //Console.WriteLine(string.Format("\n射线总数：{0}", 103388032));
            //Console.WriteLine(string.Format("直射线总数：{0}", 22562));
            //Console.WriteLine(string.Format("反射线总数：{0}", 103365470));
            //Console.WriteLine("总运行时间：2.1374535 min");
        }

        #endregion

        #region 生成用于系数校正的射线
        public void startCalcAdj()
        {
            DateTime t0, t1, t2, t3, t4, t5, t6;
            t0 = DateTime.Now;

            this.beforeCalc();

            int gray, bray = 0, vray;
            Geometric.Point source = this.cellInfo.SourcePoint;

            t1 = DateTime.Now;

            //string s1 = string.Format("{0} {1}", this.fromAngle, this.toAngle);
            //MessageBox.Show(s1);

            //获取覆盖扇形范围内的建筑物id，从BuildingGrid3D.buildingCenter中筛
            this.bids = BuildingGrid3D.getBuildingIDBySector(source, this.distance, this.fromAngle, this.toAngle);
            Console.WriteLine("building num = {0}", this.bids.Count);

            //将位于扇区覆盖范围内的地面栅格加进来
            List<Geometric.Point> gfPoints = GroundGrid.getGGridCenterBySector(source, this.distance, this.fromAngle, this.toAngle, null);
            Console.WriteLine("ground grid num = {0}", gfPoints.Count);

            //建筑物顶面栅格
            List<Geometric.Point> topPoints = TopPlaneGrid.GetAllTopGrid(source, this.bids);
            Console.WriteLine("top grid num = {0}", topPoints.Count);

            //建筑物立面栅格
            List<Geometric.Point> vPoints = VerticalPlaneGrid.GetAllVerticalGrid(source, this.bids, diffPointsMargin);
            double mergeAngle = 5.0 / 2000;//弧度制
            // 合并射线终点，射线的角度小于mergeAngle的合并
            List<Geometric.Point> vmPoints = GeometricUtilities.mergePointsByAngle(source, vPoints, mergeAngle);
            Console.WriteLine("vertical grid num = {0}", vmPoints.Count);

            //将建筑物按照到小区的距离排序，得到每个建筑物相对于小区的最小、最大角度，然后去掉被遮挡的建筑，并更新被遮挡建筑物可见部分
            List<TriangleBound> disAngle = BuildingGrid3D.getShelterDisAndAngle(source, this.bids, 0);
            Console.WriteLine("disAngle num = {0}", disAngle.Count);

            //MessageBox.Show("3");

            //建筑物棱边栅格
            List<Geometric.Point> diffPoints = BuildingGrid3D.getBuildingsEdgePointsByShelter(source.Z, disAngle, diffPointsMargin);
            Console.WriteLine("building edge points num = {0}", diffPoints.Count);

            //string s = string.Format("{0} {1} {2}", gfPoints[gfPoints.Count - 1].X, gfPoints[gfPoints.Count - 1].Y, gfPoints[gfPoints.Count - 1].Z);
            //MessageBox.Show(s);

            t4 = DateTime.Now;
            Console.WriteLine("直射分析 ...");
            for (int i = 0; i < gfPoints.Count; i++)
            {
                List<InternalInterference.NodeInfo> rayList = new List<InternalInterference.NodeInfo>();
                this.rayLocate.rayTracingFirstAdj(source, gfPoints[i], rayList, cellInfo, InternalInterference.RayType.Direction, 1);
            }
            gray = gfPoints.Count;

            for (int i = 0; i < topPoints.Count; i++)
            {
                List<InternalInterference.NodeInfo> rayList = new List<InternalInterference.NodeInfo>();
                this.rayLocate.rayTracingFirstAdj(source, topPoints[i], rayList, cellInfo, InternalInterference.RayType.Direction, 2);
            }
            gray += topPoints.Count;
            t2 = DateTime.Now;

            //t5 = DateTime.Now;
            Console.WriteLine("反射、室内分析...");
            //this.label1.Refresh();
            for (int i = 0; i < vmPoints.Count; i++)
            {
                List<InternalInterference.NodeInfo> rayList = new List<InternalInterference.NodeInfo>();
                this.rayLocate.rayTracingFirstAdj(source, vmPoints[i], rayList, cellInfo, InternalInterference.RayType.Direction, 3);
            }
            bray = vmPoints.Count;

            //Console.WriteLine("绕射分析...");
            for (int i = 0; i < diffPoints.Count; i++)
            {
                List<InternalInterference.NodeInfo> rayList = new List<InternalInterference.NodeInfo>();
                this.rayLocate.rayTracingFirstAdj(source, diffPoints[i], rayList, cellInfo, InternalInterference.RayType.Direction, 4);
            }
            vray = diffPoints.Count;
            t3 = DateTime.Now;

            Console.WriteLine("分析完成...");

            t6 = DateTime.Now;

            // 高中低建筑物占比
            int h1 = 0;  // <10
            int h2 = 0;  // 10 ~ 30
            int h3 = 0;  // >30
            for (int i = 0; i < this.bids.Count; i++)
            {
                double ht = BuildingGrid3D.getBuildingHeight(this.bids[i]);
                if (ht < 10)
                    h1++;
                else if (ht < 30)
                    h2++;
                else
                    h3++;
            }

            // 立体网格数量
            int litiCnt = 0;
            for (int i = 0; i < this.bids.Count; i++)
            {
                if (BuildingGrid3D.bgrid3d.ContainsKey(this.bids[i]))
                    litiCnt += BuildingGrid3D.bgrid3d[this.bids[i]].Count;
            }

            Console.WriteLine("disAngle num = {0}", disAngle.Count);
            Console.WriteLine("扇区内建筑物数量 = {0}", this.bids.Count);
            Console.WriteLine("扇区内地面栅格数量 = {0}", gfPoints.Count);
            Console.WriteLine("扇区内立体栅格数量 = {0}", litiCnt);
            Console.WriteLine("扇区内建筑物顶面栅格数量 = {0}", topPoints.Count);
            Console.WriteLine("扇区内建筑物立面栅格数量 = {0}", vmPoints.Count);
            Console.WriteLine("扇区内建筑物棱边栅格数量 = {0}", diffPoints.Count);
            Console.WriteLine("扇区内<6m建筑物数量 = {0}, 占比 = {1} ", h1, h1 / this.bids.Count);
            Console.WriteLine("扇区内6~20m建筑物数量 = {0}, 占比 = {1} ", h2, h2 / this.bids.Count);
            Console.WriteLine("扇区内>20m建筑物数量 = {0}, 占比 = {1} ", h3, h3 / this.bids.Count);
            Console.WriteLine("扇区内建筑物占比 = {0}", topPoints.Count / gfPoints.Count);
            Console.WriteLine("射线数量 = {0}", this.rayLocate.rayCount);
            Console.WriteLine("直射线数量 = {0}", this.rayLocate.rayCountDir);
            Console.WriteLine("绕射线数量 = {0}", this.rayLocate.rayCountDif);
            Console.WriteLine("反射线数量 = {0}", this.rayLocate.rayCountRef);
            Console.WriteLine("透射线数量 = {0}", this.rayLocate.rayCountTra);
            Console.WriteLine("地面有效直射线数量 = {0}", this.rayLocate.rayCountDirG);
            Console.WriteLine("楼顶有效直射线数量 = {0}", this.rayLocate.rayCountDirB);
            // 一条射线约占50字节
            Console.WriteLine("射线占约内存数：{0} M", this.rayLocate.rayCount * 50 / 1048576);
            string info = string.Format("数据库：{0}毫秒\n生成筛选：{1}毫秒\n计算：{2}毫秒\n写入mmf：{3}毫秒\n射线跟踪时间：{4}毫秒\n总运行时间：{5}毫秒\n", (t1 - t0).TotalMilliseconds, (t4 - t1).TotalMilliseconds, (t3 - t4).TotalMilliseconds, (t6 - t3).TotalMilliseconds, (t6 - t1).TotalMilliseconds, (t6 - t0).TotalMilliseconds);
            Console.WriteLine(info);

            this.afterCalc();

            //Console.ReadKey();
            //this.cs.free();
        }
        #endregion

        /// <summary>
        /// 发出射线
        /// </summary>
        /// <param name="points"></param>
        /// <param name="type">初级直射线类型，1：连向地面;；2：连向楼顶；3：连向可见侧面；4：连向可见棱边</param>
        /// <returns></returns>
        private void analysis(List<LTE.Geometric.Point> points, int type)
        {
            this.resetProgress(points.Count);

            int rayCounter = 0;

            double varAzimuth = 0, varInclination = 0;
            List<LTE.InternalInterference.NodeInfo> rayList = new List<InternalInterference.NodeInfo>();
            foreach (var endp in points)
            {
                if (++rayCounter % 1000 == 1)
                {
                    this.updateProgress(rayCounter);
                }

                //if (rayCounter < 2117)
                //    continue;

                //MessageBox.Show(string.Format("{0}", rayCounter));

                SourceInfo s = this.cellInfo.clone();
                GeometricUtilities.getAzimuth_Inclination(s.SourcePoint, endp, out varAzimuth, out varInclination);

                //MessageBox.Show(string.Format("SourcePoint {0},{1},{2}", s.SourcePoint.X, s.SourcePoint.Y, s.SourcePoint.Z));
                //MessageBox.Show(string.Format("endp {0},{1},{2}", endp.X, endp.Y, endp.Z));

                s.RayAzimuth = varAzimuth;
                s.RayInclination = varInclination;

                double coverageRadius = (s.SourcePoint.Z / Math.Tan(s.RayInclination * (Math.PI / 180)));  // 根据下倾角方向到地面连线确定的非弱信号区域半径
                coverageRadius = Math.Min(coverageRadius, 500);

                // 跟踪某种类型的射线传播
                rayList.Clear();
                this.interAnalysis.rayTracingFirst(s.SourcePoint, endp, rayList, s, LTE.InternalInterference.RayType.Direction, type, coverageRadius);
            }
            //MessageBox.Show("finish");
            this.updateProgress(points.Count);
        }

        /// <summary>
        /// 发出射线  用于二次投射
        /// </summary>
        private void analysis(ref List<ReRay> points)
        {
            this.resetProgress(points.Count);

            int rayCounter = 0;

            List<LTE.InternalInterference.NodeInfo> rayList = new List<InternalInterference.NodeInfo>();
            foreach (var sp in points)
            {
                if (++rayCounter % 1000 == 1)
                {
                    this.updateProgress(rayCounter);
                }

                // 跟踪某种类型的射线传播
                rayList.Clear();
                Geometric.Point source = new Geometric.Point(sp.emitPtX, sp.emitPtY, sp.emitPtZ);
                Vector3D dir = new Vector3D(sp.dirX, sp.dirY, sp.dirZ);
                this.interAnalysis.rayTracing(source, dir, rayList, (InternalInterference.RayType)sp.type, sp.pwrDbm);
            }
            //MessageBox.Show("finish");
            this.updateProgress(points.Count);
        }

        // 2018.12.11  用于定位
        public void writeRayLoc()
        {
            System.Data.DataTable tb = new System.Data.DataTable();
            tb.Columns.Add("cellID");
            tb.Columns.Add("trajID");
            tb.Columns.Add("rayLevel");
            tb.Columns.Add("rayType");
            tb.Columns.Add("emitPwrDbm");
            tb.Columns.Add("rayStartPointX");
            tb.Columns.Add("rayStartPointY");
            tb.Columns.Add("rayStartPointZ");
            tb.Columns.Add("rayEndPointX");
            tb.Columns.Add("rayEndPointY");
            tb.Columns.Add("rayEndPointZ");
            tb.Columns.Add("distance");
            tb.Columns.Add("buildingID");
            tb.Columns.Add("angle");
            tb.Columns.Add("attenuation");  // 损耗系数
            tb.Columns.Add("recePwrDbm");

            System.Data.DataTable tb1 = IbatisHelper.ExecuteQueryForDataTable("GetMaxRayLocID", null);
            long maxid = 0;
            if (tb1.Rows.Count > 0 && tb1.Rows[0][0] != null && tb1.Rows[0][0].ToString() != "")
            {
                maxid = long.Parse(tb1.Rows[0][0].ToString()) + 1;
            }

            #region beam 的反射线结果
            if (solution != null && solution.m_rays != null)
            {
                for (int i = 0; i < this.solution.m_rays.Count; ++i)
                {
                    for (int j = 0; j < this.solution.m_rays[i].m_rays.Count; ++j)
                    {
                        System.Data.DataRow thisrow = tb.NewRow();

                        thisrow["cellID"] = this.cellInfo.CI; //MessageBox.Show("1");
                        thisrow["trajID"] = maxid; //MessageBox.Show("5");
                        thisrow["rayLevel"] = Convert.ToByte(j); //MessageBox.Show("6");
                        thisrow["rayType"] = Convert.ToByte(solution.m_rays[i].m_rays[j].rayType); //MessageBox.Show("7");
                        if (j == 0)
                            thisrow["emitPwrDbm"] = solution.m_rays[i].emitPwrDbm;
                        else
                            thisrow["emitPwrDbm"] = 0;
                        if (solution.m_rays[i].m_rays[j].PointOfIncidence != null)
                        {
                            thisrow["rayStartPointX"] = solution.m_rays[i].m_rays[j].PointOfIncidence.m_position.x; //MessageBox.Show("8");
                            thisrow["rayStartPointY"] = solution.m_rays[i].m_rays[j].PointOfIncidence.m_position.y; //MessageBox.Show("9");
                            thisrow["rayStartPointZ"] = solution.m_rays[i].m_rays[j].PointOfIncidence.m_position.z; //MessageBox.Show("10");
                        }
                        else
                        {
                            thisrow["rayStartPointX"] = -1; ////MessageBox.Show("8");
                            thisrow["rayStartPointY"] = -1; //MessageBox.Show("9");
                            thisrow["rayStartPointZ"] = -1;
                        }
                        if (solution.m_rays[i].m_rays[j].CrossPoint != null)
                        {
                            thisrow["rayEndPointX"] = solution.m_rays[i].m_rays[j].CrossPoint.m_position.x; //MessageBox.Show("11");
                            thisrow["rayEndPointY"] = solution.m_rays[i].m_rays[j].CrossPoint.m_position.y; //MessageBox.Show("12");
                            thisrow["rayEndPointZ"] = solution.m_rays[i].m_rays[j].CrossPoint.m_position.z; //MessageBox.Show("13");
                        }
                        else
                        {
                            thisrow["rayEndPointX"] = -1; //MessageBox.Show("11");
                            thisrow["rayEndPointY"] = -1; ////MessageBox.Show("12");
                            thisrow["rayEndPointZ"] = -1; //MessageBox.Show("13");
                        }
                        thisrow["distance"] = solution.m_rays[i].m_rays[j].Distance; ////MessageBox.Show("14");
                        thisrow["buildingID"] = solution.m_rays[i].m_rays[j].buildingID; //MessageBox.Show("15");
                        thisrow["angle"] = solution.m_rays[i].m_rays[j].Angle; //MessageBox.Show("16");
                        thisrow["attenuation"] = solution.m_rays[i].m_rays[j].attenuation; //MessageBox.Show("21");
                        if (j == solution.m_rays[i].m_rays.Count - 1)
                            thisrow["recePwrDbm"] = solution.m_rays[i].recvPwrDbm;
                        else
                            thisrow["recePwrDbm"] = 0; //MessageBox.Show("22");

                        tb.Rows.Add(thisrow);

                        //MessageBox.Show(string.Format("rayId {0}，distance {1}， angle {2}，type {3}, count{4}", j, kvp.Value[i].rayList[j].Distance,
                        //    kvp.Value[i].rayList[j].Angle, (int)kvp.Value[i].rayList[j].rayType, kvp.Value[i].rayList.Count));

                    }
                    ++maxid;

                    if (tb.Rows.Count > 50000)
                    {
                        using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                        {
                            bcp.BatchSize = tb.Rows.Count;
                            bcp.BulkCopyTimeout = 1000;
                            bcp.DestinationTableName = "tbRayLoc";
                            bcp.WriteToServer(tb);
                            bcp.Close();
                        }
                        tb.Clear();
                    }
                }
            }
            #endregion

            #region 直射、绕射、没有 beam 时的反射结果
            for (int i = 0; i < rayLocate.rayLoc.Count; ++i)
            {
                for (int j = 0; j < rayLocate.rayLoc[i].rayList.Count; ++j)
                {
                    System.Data.DataRow thisrow = tb.NewRow();

                    thisrow["cellID"] = this.cellInfo.CI; //MessageBox.Show("1");
                    thisrow["trajID"] = maxid; //MessageBox.Show("5");
                    thisrow["rayLevel"] = Convert.ToByte(j); //MessageBox.Show("6");

                    thisrow["rayType"] = Convert.ToByte(rayLocate.rayLoc[i].rayList[j].rayType); //MessageBox.Show("7");
                    if (j == 0)
                        thisrow["emitPwrDbm"] = rayLocate.rayLoc[i].emitPwrDbm;
                    else
                        thisrow["emitPwrDbm"] = 0;
                    if (rayLocate.rayLoc[i].rayList[j].PointOfIncidence != null)
                    {
                        thisrow["rayStartPointX"] = rayLocate.rayLoc[i].rayList[j].PointOfIncidence.X; //MessageBox.Show("8");
                        thisrow["rayStartPointY"] = rayLocate.rayLoc[i].rayList[j].PointOfIncidence.Y; //MessageBox.Show("9");
                        thisrow["rayStartPointZ"] = rayLocate.rayLoc[i].rayList[j].PointOfIncidence.Z; //MessageBox.Show("10");
                    }
                    else
                    {
                        thisrow["rayStartPointX"] = -1; ////MessageBox.Show("8");
                        thisrow["rayStartPointY"] = -1; //MessageBox.Show("9");
                        thisrow["rayStartPointZ"] = -1;
                    }
                    if (rayLocate.rayLoc[i].rayList[j].CrossPoint != null)
                    {
                        thisrow["rayEndPointX"] = rayLocate.rayLoc[i].rayList[j].CrossPoint.X; //MessageBox.Show("11");
                        thisrow["rayEndPointY"] = rayLocate.rayLoc[i].rayList[j].CrossPoint.Y; //MessageBox.Show("12");
                        thisrow["rayEndPointZ"] = rayLocate.rayLoc[i].rayList[j].CrossPoint.Z; //MessageBox.Show("13");
                    }
                    else
                    {
                        thisrow["rayEndPointX"] = -1; //MessageBox.Show("11");
                        thisrow["rayEndPointY"] = -1; ////MessageBox.Show("12");
                        thisrow["rayEndPointZ"] = -1; //MessageBox.Show("13");
                    }
                    thisrow["distance"] = rayLocate.rayLoc[i].rayList[j].Distance; ////MessageBox.Show("14");
                    thisrow["buildingID"] = rayLocate.rayLoc[i].rayList[j].buildingID; //MessageBox.Show("15");
                    thisrow["angle"] = rayLocate.rayLoc[i].rayList[j].Angle; //MessageBox.Show("16");
                    thisrow["attenuation"] = rayLocate.rayLoc[i].rayList[j].attenuation; //MessageBox.Show("21");
                    if (j == rayLocate.rayLoc[i].rayList.Count - 1)
                        thisrow["recePwrDbm"] = rayLocate.rayLoc[i].recvPwrDbm;
                    else
                        thisrow["recePwrDbm"] = 0; //MessageBox.Show("22");

                    tb.Rows.Add(thisrow);

                    //MessageBox.Show(string.Format("rayId {0}，distance {1}， angle {2}，type {3}, count{4}", j, kvp.Value[i].rayList[j].Distance,
                    //    kvp.Value[i].rayList[j].Angle, (int)kvp.Value[i].rayList[j].rayType, kvp.Value[i].rayList.Count));

                }
                ++maxid;

                if (tb.Rows.Count > 50000)
                {
                    using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                    {
                        bcp.BatchSize = tb.Rows.Count;
                        bcp.BulkCopyTimeout = 1000;
                        bcp.DestinationTableName = "tbRayLoc";
                        bcp.WriteToServer(tb);
                        bcp.Close();
                    }
                    tb.Clear();
                }
            }
            #endregion

            using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
            {
                bcp.BatchSize = tb.Rows.Count;
                bcp.BulkCopyTimeout = 1000;
                bcp.DestinationTableName = "tbRayLoc";
                bcp.WriteToServer(tb);
                bcp.Close();

            }
            tb.Clear();

            Console.WriteLine("定位数据入库");
        }

        // 2019.1.11  用于系数校正
        void writeToRayAdj()
        {
            System.Data.DataTable tb = new System.Data.DataTable();
            tb.Columns.Add("cellID");
            tb.Columns.Add("gxid");
            tb.Columns.Add("gyid");
            tb.Columns.Add("gzid");
            tb.Columns.Add("trajID");
            tb.Columns.Add("rayLevel");
            tb.Columns.Add("rayType");
            tb.Columns.Add("emitPwrW");
            tb.Columns.Add("startPointScen");
            tb.Columns.Add("endPointScen");
            tb.Columns.Add("rayStartPointX");
            tb.Columns.Add("rayStartPointY");
            tb.Columns.Add("rayStartPointZ");
            tb.Columns.Add("rayEndPointX");
            tb.Columns.Add("rayEndPointY");
            tb.Columns.Add("rayEndPointZ");
            tb.Columns.Add("distance");
            tb.Columns.Add("buildingID");
            tb.Columns.Add("angle");
            tb.Columns.Add("edgeStartPtX");
            tb.Columns.Add("edgeStartPtY");
            tb.Columns.Add("edgeEndPtX");
            tb.Columns.Add("edgeEndPtY");
            tb.Columns.Add("attenuation");
            tb.Columns.Add("recePwrW");
            Dictionary<string, List<RayNode>> dic = this.rayLocate.getRayList();

            //MessageBox.Show(string.Format("dic.Count {0}", dic.Count));

            System.Data.DataTable tb1 = IbatisHelper.ExecuteQueryForDataTable("GetMaxRayID", null);
            long maxid = 0;
            if (tb1.Rows.Count > 0 && tb1.Rows[0][0] != null && tb1.Rows[0][0].ToString() != "")
            {
                //MessageBox.Show(tb1.Rows[0][0].ToString());
                Console.WriteLine("id {0}", tb1.Rows[0][0].ToString());
                maxid = long.Parse(tb1.Rows[0][0].ToString()) + 1;
            }

            int cnt1 = 0;
            foreach (KeyValuePair<string, List<RayNode>> kvp in dic)
            {
                string[] id = kvp.Key.Split(',');
                //MessageBox.Show(kvp.Key);

                int xid = Convert.ToInt32(id[0]);
                int yid = Convert.ToInt32(id[1]);
                int zid = Convert.ToInt32(id[2]);

                for (int i = 0; i < kvp.Value.Count; ++i)
                {
                    for (int j = 0; j < kvp.Value[i].rayList.Count; ++j)
                    {
                        System.Data.DataRow thisrow = tb.NewRow();

                        thisrow["cellID"] = kvp.Value[i].cellid; //MessageBox.Show("1");
                        thisrow["gxid"] = xid; //MessageBox.Show("2");
                        thisrow["gyid"] = yid; //MessageBox.Show("3");
                        thisrow["gzid"] = zid; //MessageBox.Show("4");
                        thisrow["trajID"] = maxid; //MessageBox.Show("5");
                        thisrow["rayLevel"] = Convert.ToByte(j); //MessageBox.Show("6");
                        thisrow["rayType"] = Convert.ToByte(kvp.Value[i].rayList[j].rayType); //MessageBox.Show("7");
                        if (j == 0)
                            thisrow["emitPwrW"] = kvp.Value[i].startPwrW;
                        else
                            thisrow["emitPwrW"] = 0;
                        thisrow["startPointScen"] = 0;
                        thisrow["endPointScen"] = 0;
                        if (kvp.Value[i].rayList[j].PointOfIncidence != null)
                        {
                            thisrow["rayStartPointX"] = kvp.Value[i].rayList[j].PointOfIncidence.X; //MessageBox.Show("8");
                            thisrow["rayStartPointY"] = kvp.Value[i].rayList[j].PointOfIncidence.Y; //MessageBox.Show("9");
                            thisrow["rayStartPointZ"] = kvp.Value[i].rayList[j].PointOfIncidence.Z; //MessageBox.Show("10");
                        }
                        else
                        {
                            thisrow["rayStartPointX"] = -1; ////MessageBox.Show("8");
                            thisrow["rayStartPointY"] = -1; //MessageBox.Show("9");
                            thisrow["rayStartPointZ"] = -1;
                        }
                        if (kvp.Value[i].rayList[j].CrossPoint != null)
                        {
                            thisrow["rayEndPointX"] = kvp.Value[i].rayList[j].CrossPoint.X; //MessageBox.Show("11");
                            thisrow["rayEndPointY"] = kvp.Value[i].rayList[j].CrossPoint.Y; //MessageBox.Show("12");
                            thisrow["rayEndPointZ"] = kvp.Value[i].rayList[j].CrossPoint.Z; //MessageBox.Show("13");
                        }
                        else
                        {
                            thisrow["rayEndPointX"] = -1; //MessageBox.Show("11");
                            thisrow["rayEndPointY"] = -1; ////MessageBox.Show("12");
                            thisrow["rayEndPointZ"] = -1; //MessageBox.Show("13");
                        }
                        thisrow["distance"] = kvp.Value[i].rayList[j].Distance; ////MessageBox.Show("14");
                        thisrow["buildingID"] = kvp.Value[i].rayList[j].buildingID; //MessageBox.Show("15");
                        thisrow["angle"] = kvp.Value[i].rayList[j].Angle; //MessageBox.Show("16");
                        if (kvp.Value[i].rayList[j].SideFromPoint != null)
                        {
                            thisrow["edgeStartPtX"] = kvp.Value[i].rayList[j].SideFromPoint.X; //MessageBox.Show("17");
                            thisrow["edgeStartPtY"] = kvp.Value[i].rayList[j].SideFromPoint.Y; //MessageBox.Show("18");
                        }
                        else
                        {
                            thisrow["edgeStartPtX"] = -1; //MessageBox.Show("17");
                            thisrow["edgeStartPtY"] = -1; //MessageBox.Show("18");
                        }
                        if (kvp.Value[i].rayList[j].SideToPoint != null)
                        {
                            thisrow["edgeEndPtX"] = kvp.Value[i].rayList[j].SideToPoint.X; //MessageBox.Show("19");
                            thisrow["edgeEndPtY"] = kvp.Value[i].rayList[j].SideToPoint.Y; //MessageBox.Show("20");
                        }
                        else
                        {
                            thisrow["edgeEndPtX"] = -1; //MessageBox.Show("19");
                            thisrow["edgeEndPtY"] = -1; //MessageBox.Show("20");
                        }
                        thisrow["attenuation"] = kvp.Value[i].rayList[j].attenuation; //MessageBox.Show("21");
                        if (j == kvp.Value[i].rayList.Count - 1)
                            thisrow["recePwrW"] = kvp.Value[i].recePwrW;
                        else
                            thisrow["recePwrW"] = 0; //MessageBox.Show("22");

                        tb.Rows.Add(thisrow);
                        ++cnt1;

                        //MessageBox.Show(string.Format("rayId {0}，distance {1}， angle {2}，type {3}, count{4}", j, kvp.Value[i].rayList[j].Distance,
                        //    kvp.Value[i].rayList[j].Angle, (int)kvp.Value[i].rayList[j].rayType, kvp.Value[i].rayList.Count));

                    }
                    ++maxid;
                }
            }

            using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
            {
                bcp.BatchSize = cnt1;
                bcp.BulkCopyTimeout = 1000;
                bcp.DestinationTableName = "tbRayAdj";
                bcp.WriteToServer(tb);
                bcp.Close();

            }
            tb1.Clear();
            Console.WriteLine("系数校正数据入库");
        }

        // 2019.1.11  用于系数校正
        void writeRayAdj()
        {
            System.Data.DataTable tb = new System.Data.DataTable();
            tb.Columns.Add("cellID");
            tb.Columns.Add("gxid");
            tb.Columns.Add("gyid");
            tb.Columns.Add("gzid");
            tb.Columns.Add("trajID");
            tb.Columns.Add("rayLevel");
            tb.Columns.Add("rayType");
            tb.Columns.Add("emitPwrW");
            tb.Columns.Add("startPointScen");
            tb.Columns.Add("endPointScen");
            tb.Columns.Add("rayStartPointX");
            tb.Columns.Add("rayStartPointY");
            tb.Columns.Add("rayStartPointZ");
            tb.Columns.Add("rayEndPointX");
            tb.Columns.Add("rayEndPointY");
            tb.Columns.Add("rayEndPointZ");
            tb.Columns.Add("distance");
            tb.Columns.Add("buildingID");
            tb.Columns.Add("angle");
            tb.Columns.Add("proportion");
            tb.Columns.Add("edgeStartPtX");
            tb.Columns.Add("edgeStartPtY");
            tb.Columns.Add("edgeEndPtX");
            tb.Columns.Add("edgeEndPtY");
            tb.Columns.Add("attenuation");
            tb.Columns.Add("recePwrW");


            Dictionary<string, List<RayNode>> dic = this.rayLocate.getRayList();

            //MessageBox.Show(string.Format("dic.Count {0}", dic.Count));

            System.Data.DataTable tb1 = IbatisHelper.ExecuteQueryForDataTable("GetMaxRayID", null);
            long maxid = 0;
            if (tb1.Rows.Count > 0 && tb1.Rows[0][0] != null && tb1.Rows[0][0].ToString() != "")
            {
                //MessageBox.Show(tb1.Rows[0][0].ToString());
                Console.WriteLine("id {0}", tb1.Rows[0][0].ToString());
                maxid = long.Parse(tb1.Rows[0][0].ToString()) + 1;
            }

            foreach (KeyValuePair<string, List<RayNode>> kvp in dic)
            {
                string[] id = kvp.Key.Split(',');
                //MessageBox.Show(kvp.Key);

                int xid = Convert.ToInt32(id[0]);
                int yid = Convert.ToInt32(id[1]);
                int zid = Convert.ToInt32(id[2]);

                for (int i = 0; i < kvp.Value.Count; ++i)
                {
                    for (int j = 0; j < kvp.Value[i].rayList.Count; ++j)
                    {
                        System.Data.DataRow thisrow = tb.NewRow();

                        thisrow["cellID"] = kvp.Value[i].cellid; //MessageBox.Show("1");
                        thisrow["gxid"] = xid; //MessageBox.Show("2");
                        thisrow["gyid"] = yid; //MessageBox.Show("3");
                        thisrow["gzid"] = zid; //MessageBox.Show("4");
                        thisrow["trajID"] = maxid; //MessageBox.Show("5");
                        thisrow["rayLevel"] = Convert.ToByte(j); //MessageBox.Show("6");
                        thisrow["rayType"] = Convert.ToByte(kvp.Value[i].rayList[j].rayType); //MessageBox.Show("7");
                        if (j == 0)
                            thisrow["emitPwrW"] = kvp.Value[i].startPwrW;
                        else
                            thisrow["emitPwrW"] = 0;
                        thisrow["startPointScen"] = kvp.Value[i].rayList[j].startPointScen;
                        thisrow["endPointScen"] = kvp.Value[i].rayList[j].endPointScen;
                        if (kvp.Value[i].rayList[j].PointOfIncidence != null)
                        {
                            thisrow["rayStartPointX"] = kvp.Value[i].rayList[j].PointOfIncidence.X; //MessageBox.Show("8");
                            thisrow["rayStartPointY"] = kvp.Value[i].rayList[j].PointOfIncidence.Y; //MessageBox.Show("9");
                            thisrow["rayStartPointZ"] = kvp.Value[i].rayList[j].PointOfIncidence.Z; //MessageBox.Show("10");
                        }
                        else
                        {
                            thisrow["rayStartPointX"] = -1; ////MessageBox.Show("8");
                            thisrow["rayStartPointY"] = -1; //MessageBox.Show("9");
                            thisrow["rayStartPointZ"] = -1;
                        }
                        if (kvp.Value[i].rayList[j].CrossPoint != null)
                        {
                            thisrow["rayEndPointX"] = kvp.Value[i].rayList[j].CrossPoint.X; //MessageBox.Show("11");
                            thisrow["rayEndPointY"] = kvp.Value[i].rayList[j].CrossPoint.Y; //MessageBox.Show("12");
                            thisrow["rayEndPointZ"] = kvp.Value[i].rayList[j].CrossPoint.Z; //MessageBox.Show("13");
                        }
                        else
                        {
                            thisrow["rayEndPointX"] = -1; //MessageBox.Show("11");
                            thisrow["rayEndPointY"] = -1; ////MessageBox.Show("12");
                            thisrow["rayEndPointZ"] = -1; //MessageBox.Show("13");
                        }
                        thisrow["distance"] = kvp.Value[i].rayList[j].Distance; ////MessageBox.Show("14");
                        thisrow["buildingID"] = kvp.Value[i].rayList[j].buildingID; //MessageBox.Show("15");
                        thisrow["angle"] = kvp.Value[i].rayList[j].Angle; //MessageBox.Show("16");
                        thisrow["proportion"] = kvp.Value[i].rayList[j].proportion; //MessageBox.Show(kvp.Value[i].rayList[j].proportion);
                        if (kvp.Value[i].rayList[j].SideFromPoint != null)
                        {
                            thisrow["edgeStartPtX"] = kvp.Value[i].rayList[j].SideFromPoint.X; //MessageBox.Show("17");
                            thisrow["edgeStartPtY"] = kvp.Value[i].rayList[j].SideFromPoint.Y; //MessageBox.Show("18");
                        }
                        else
                        {
                            thisrow["edgeStartPtX"] = -1; //MessageBox.Show("17");
                            thisrow["edgeStartPtY"] = -1; //MessageBox.Show("18");
                        }
                        if (kvp.Value[i].rayList[j].SideToPoint != null)
                        {
                            thisrow["edgeEndPtX"] = kvp.Value[i].rayList[j].SideToPoint.X; //MessageBox.Show("19");
                            thisrow["edgeEndPtY"] = kvp.Value[i].rayList[j].SideToPoint.Y; //MessageBox.Show("20");
                        }
                        else
                        {
                            thisrow["edgeEndPtX"] = -1; //MessageBox.Show("19");
                            thisrow["edgeEndPtY"] = -1; //MessageBox.Show("20");
                        }
                        thisrow["attenuation"] = kvp.Value[i].rayList[j].attenuation; //MessageBox.Show("21");
                        if (j == kvp.Value[i].rayList.Count - 1)
                            thisrow["recePwrW"] = kvp.Value[i].recePwrW;
                        else
                            thisrow["recePwrW"] = 0; //MessageBox.Show("22");

                        tb.Rows.Add(thisrow);

                        //MessageBox.Show(string.Format("rayId {0}，distance {1}， angle {2}，type {3}, count{4}", j, kvp.Value[i].rayList[j].Distance,
                        //    kvp.Value[i].rayList[j].Angle, (int)kvp.Value[i].rayList[j].rayType, kvp.Value[i].rayList.Count));

                    }
                    ++maxid;
                }

                if (tb.Rows.Count > 5000)
                {
                    using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                    {
                        bcp.BatchSize = tb.Rows.Count;
                        bcp.BulkCopyTimeout = 1000;
                        bcp.DestinationTableName = "tbRayAdj";
                        bcp.WriteToServer(tb);
                        bcp.Close();

                    }
                    tb.Clear();
                }
            }

            using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
            {
                bcp.BatchSize = tb.Rows.Count;
                bcp.BulkCopyTimeout = 1000;
                bcp.DestinationTableName = "tbRayAdj";
                bcp.WriteToServer(tb);
                bcp.Close();

            }
            tb.Clear();
            Console.WriteLine("系数校正数据入库");
        }

        /// <summary>
        /// 计算结束清理内存，与主进程通信
        /// </summary>
        private void afterCalc()
        {
            AccelerateStruct.clearAccelerateStruct();
            //send message to notify parent process to read rxlev
            if (this.isRayLoc)
            {
                writeRayLoc();
                IPC.PostMessage(this.parentHandle, IPC.WM_POST_CALCDONE, this.Handle, 0);
            }
            else if (this.isRayAdj)
            {
                writeRayAdj();
                IPC.PostMessage(this.parentHandle, IPC.WM_POST_CALCDONE, this.Handle, 0);
            }
            else
            {
                BuildingGrid3D.clearBuildingData();
                BuildingGrid3D.clearBuildingVertexOriginal();
                BuildingGrid3D.clearGrid3D();
                GroundGrid.ggrids.Clear();

                //int dataSize1 = this.writeReRayToMMF(this.MMFName1);

                if (isRecordReray && this.interAnalysis.reRays.Count > 0)  // 2019.5.22
                {
                    int dataSize1 = this.writeReRayToMMF(this.MMFName1);
                    IPC.PostMessage(this.parentHandle, IPC.WM_POST_ReRayDONE, this.Handle, dataSize1);
                    Console.WriteLine("post reray to parent process succees................");
                }
                int dataSize = this.writeDataToMMF(this.MMFName, false);
                IPC.PostMessage(this.parentHandle, IPC.WM_POST_CALCDONE, this.Handle, dataSize);
                Console.WriteLine("post calcDone to parent process succees................");
            }
        }

        /// <summary>
        /// 将覆盖分析结果写入共享内存，返回数据大小
        /// </summary>
        /// <param name="sharename"></param>
        /// <returns></returns>
        public int writeDataToMMF(string sharename, bool isRay)
        {
            List<GridStrength> gridStrengths = this.interAnalysis.getGridStrengths();

            int ssize = Marshal.SizeOf(typeof(MMFGSStruct)), cnt = gridStrengths.Count, dataSize = ssize * cnt, ret = dataSize;
            //Console.WriteLine("ssize = {0}", ssize);
            Console.WriteLine("共享内存大小约为：{0} M", dataSize / 1048576);
            IntPtr mmf, hmap, tp;
            mmf = MMF.CreateOrOpenMMF(sharename, dataSize);
            hmap = MMF.MapViewOfFile(mmf, MMF.FileMapAccess.FileMapWrite, 0, 0, (uint)dataSize);
            if (hmap == IntPtr.Zero)
            {
                MMF.CloseHandle(mmf);
                Console.WriteLine("文件写入映射失败！");
                return 0;
            }
            dataSize = 0;
            MMFGSStruct mmfgs;
            long sp = hmap.ToInt64();
            for (int i = 0; i < cnt; i++, dataSize += ssize)
            {
                GridStrength gs = gridStrengths[i];
                //if (Object.ReferenceEquals(gs, null))
                //{
                //    Console.WriteLine("null");
                //    Console.ReadKey();
                //    continue;
                //}
                mmfgs = convertFromGridStrength(gs);
                //outSTR(i, mmfgs);
                try
                {
                    tp = new IntPtr(sp + dataSize);
                    Marshal.StructureToPtr(mmfgs, tp, false);
                }
                catch (Exception e)
                {
                    Console.WriteLine("出错：{0}", i);
                    //MessageBox.Show(string.Format("mmf={0}, dataSize={1}, sizeof={2}, content = {5}, num = {3}, error = {4}", mmf.ToInt32(), ret, ssize, i, e.Message, gridStrengths[i].RefBuildingID + "," + gridStrengths[i].DiffBuildingID + "," + gridStrengths[i].TransmitBuildingID));
                    outGS(gridStrengths[i]);

                    //string s = string.Format("gxid={0}, gyid={1}, level={2}, lac={3}, ci={4}, px = {5}, rxlev = {6}", gs.GXID, gs.GYID, gs.Level, gs.eNodeB, gs.CI, gs.GCenter.X, gs.ReceivedPowerW);
                    //Console.WriteLine(s);
                    //MessageBox.Show(s);
                    //MessageBox.Show(string.Format("mmf={0}, dataSize={1}, sizeof={2}, content = {5}, num = {3}, error = {4}", mmf.ToInt32(), ret, ssize, i, e.Message, gridStrengths[i].RefBuildingID + "," + gridStrengths[i].DiffBuildingID + "," + gridStrengths[i].TransmitBuildingID));
                }
            }
            MMF.UnmapViewOfFile(hmap);

            gridStrengths.Clear();

            return ret;
        }

        /// <summary>
        /// 将出界射线写入共享内存，返回数据大小
        /// 创建共享内存文件(CreateFileMapping)---》映射文件视图到调用进程的地址空间（MapViewOfFile）---》
        ///     写数据到共享内存(Marshal.StructureToPtr)----》发送消息通知需要读共享内存的窗口（PostMessage）
        /// </summary>
        /// <param name="sharename"></param>
        /// <returns></returns>
        public int writeReRayToMMF(string sharename)
        {
            List<ReRay> reRays = this.interAnalysis.reRays;

            int ssize = Marshal.SizeOf(typeof(MMFReRayStruct));
            int cnt = reRays.Count;
            int dataSize = ssize * cnt;
            int ret = dataSize;
            IntPtr mmf, hmap, tp;
            
            mmf = MMF.CreateOrOpenMMF(sharename, dataSize);
            hmap = MMF.MapViewOfFile(mmf, MMF.FileMapAccess.FileMapWrite, 0, 0, (uint)dataSize);
            if (hmap == IntPtr.Zero)
            {
                MMF.CloseHandle(mmf);
                MessageBox.Show("文件写入映射失败！");
                return 0;
            }
            dataSize = 0;
            MMFReRayStruct mmfgs;
            long sp = hmap.ToInt64();
            for (int i = 0; i < cnt; i++, dataSize += ssize)
            {
                ReRay ray = reRays[i];
                mmfgs = convertFromReRay(ray);
                try
                {
                    tp = new IntPtr(sp + dataSize);
                    Marshal.StructureToPtr(mmfgs, tp, false);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            MMF.UnmapViewOfFile(hmap);

            reRays.Clear();

            return ret;
        }

        public void outGS(GridStrength gs)
        {
            string s = string.Format("catch, gxid={0}, gyid={1}, level={2}, lac={3}, ci={4}, px = {5}, rxlev = {6}", gs.GXID, gs.GYID, gs.Level, gs.eNodeB, gs.CI, gs.GCenter.X, gs.ReceivedPowerW);
            Console.WriteLine(s);
        }

        public void outSTR(int num, MMFGSStruct gs)
        {
            string s = string.Format("num = {7}, gxid={0}, gyid={1}, level={2}, lac={3}, ci={4}, rxlev = {6}", gs.GXID, gs.GYID, gs.Level, gs.eNodeB, gs.CI, gs.ReceivedPowerW, num);
            Console.WriteLine(s);
        }

        private MMFGSStruct convertFromGridStrength(GridStrength gs)
        {
            MMFGSStruct ret = new MMFGSStruct();
            try
            {
                ret.GXID = gs.GXID;
                ret.GYID = gs.GYID;
                ret.Level = gs.Level;
                ret.x = gs.GCenter.X;
                ret.y = gs.GCenter.Y;
                ret.z = gs.GCenter.Z;
                ret.eNodeB = gs.eNodeB;
                ret.CI = gs.CI;

                ret.FieldIntensity = gs.FieldIntensity;
                ret.DirectNum = gs.DirectNum;
                ret.DirectPwrW = gs.DirectPwrW;
                ret.MaxDirectPwrW = gs.MaxDirectPwrW;

                ret.RefNum = gs.RefNum;
                ret.RefPwrW = gs.RefPwrW;
                ret.MaxRefPwrW = gs.MaxRefPwrW;
                ret.RefBuildingID = gs.RefBuildingID;

                ret.DiffNum = gs.DiffNum;
                ret.DiffPwrW = gs.DiffPwrW;
                ret.MaxDiffPwrW = gs.MaxDiffPwrW;
                ret.DiffBuildingID = gs.DiffBuildingID;

                ret.TransNum = gs.TransNum;
                ret.TransPwrW = gs.TransPwrW;
                ret.MaxTransPwrW = gs.MaxTransPwrW;
                ret.TransmitBuildingID = gs.TransmitBuildingID;

                ret.BTSGridDistance = gs.BTSGridDistance;
                ret.ReceivedPowerW = gs.ReceivedPowerW;
                ret.ReceivedPowerdbm = gs.ReceivedPowerdbm;
                ret.PathLoss = gs.PathLoss;
            }
            catch (Exception e)
            {
                MessageBox.Show("Error: " + e.Message);
            }

            return ret;
        }

        private MMFReRayStruct convertFromReRay(ReRay ray)
        {
            MMFReRayStruct ret = new MMFReRayStruct();
            try
            {
                ret.CI = this.cellInfo.CI;
                ret.emitX = ray.emitPtX;
                ret.emitY = ray.emitPtY;
                ret.emitZ = ray.emitPtZ;
                ret.dirX = ray.dirX;
                ret.dirY = ray.dirY;
                ret.dirZ = ray.dirZ;
                ret.pwrDbm = ray.pwrDbm;
                ret.type = (byte)ray.type;
            }
            catch (Exception e)
            {
                MessageBox.Show("Error: " + e.Message);
            }

            return ret;
        }

        /// <summary>
        /// 重置计算进度
        /// </summary>
        /// <param name="total"></param>
        private void resetProgress(int total)
        {
            this.totalRay = total;
            this.progressBar1.Maximum = total;
            this.progressBar1.Value = 0;
            this.progressBar1.Refresh();
        }

        /// <summary>
        /// 更新计算进度显示
        /// </summary>
        /// <param name="val"></param>
        private void updateProgress(int val)
        {
            loadInfo.cnt = (int)this.interAnalysis.rayCount;
            doPostLoading(loadInfo, "updateLoadingInfo");
            //loading.updateLoading(userId, taskName, (int)this.interAnalysis.rayCount);

            this.label1.Text = string.Format("已经计算{0}/{1}条射线", val, this.totalRay);
            this.label1.Refresh();
            this.progressBar1.Value = val;
            this.progressBar1.Refresh();
        }

        private void CalcForm_Load(object sender, EventArgs e)
        {

        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }
    }
}
