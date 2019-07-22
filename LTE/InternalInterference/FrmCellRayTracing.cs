using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Management;

using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Analyst3D;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;

using System.Runtime.InteropServices;
using System.Diagnostics;

using LTE.GIS;
using LTE.Geometric;
using LTE.InternalInterference;
using LTE.InternalInterference.Grid;
using LTE.DB;
//using LTE.CalcProcess;


namespace LTE.InternalInterference
{
    public partial class FrmCellRayTracing : Form
    {
        private string cellName;
        private int eNodeB;
        private int CI;
        private string cellType;

        private double incrementAngle;
        private double fromAngle;
        private double toAngle;
        private double distance;

        private int reflectionNum;
        private int diffractionNum;
        private float sideSplitUnit;
        private bool computeVSide;
        private bool computeIndoor;

        private float directCoefficient;
        private float reflectCoefficient;
        private float diffractCoefficient;
        private float diffractCoefficient2;
        private float jiezhiCoefficient;
        private int threadNum;

        private bool reRay = false;
        private bool multimes = false;
        private int mulNum;
        private int curNum;

        private static List<ProcessArgs> paList;

        /// <summary>
        /// 室内覆盖建筑物ids
        /// </summary>
        private List<int> bids;

        public FrmCellRayTracing()
        {
            InitializeComponent();
            if (paList == null)
            {
                paList = new List<ProcessArgs>();
            }

            this.threadNum = 0;
        }

        public FrmCellRayTracing(string cellName, int lac, int ci)
        {
            InitializeComponent();
            if (paList == null)
            {
                paList = new List<ProcessArgs>();
            }
            this.textBox_CellName.Text = cellName;
            this.eNodeB = lac;
            this.CI = ci;
            this.threadNum = 0;
        }

        /// <summary>
        /// 小区正在计算
        /// </summary>
        /// <param name="lac"></param>
        /// <param name="ci"></param>
        /// <returns></returns>
        public bool ExistProcess(int lac, int ci)
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

        private void textBox_RealNumber_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
            if (e.KeyChar >= '0' && e.KeyChar <= '9' || e.KeyChar == '\b' || e.KeyChar == '.' || e.KeyChar == '-')
                e.Handled = false;
        }

        private void textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
            if (e.KeyChar >= '0' && e.KeyChar <= '9' || e.KeyChar == '\b' || e.KeyChar == '.')
                e.Handled = false;
        }

        private void textBox_IntNumber_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
            if (e.KeyChar >= '0' && e.KeyChar <= '9' || e.KeyChar == '\b')
                e.Handled = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
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
        // p：并行进程数
        // d：分区域数
        // flag: 1--同时算，每次加载全部内存; 2--分批算，每次仅加载一部分内存，最后进行二次投射
        bool howCalc(ref int p, ref int d, ref int flag)
        {
            double capacity = this.MemoryInfo() / 6.0;  // 获取系统物理内存
            p = Math.Max(3, this.threadNum);
            d = 2;

            double theta = ((this.toAngle - this.fromAngle + 360)%360) / 360;
            double F = memF(this.distance);
            double R = memR(this.distance, theta);
            if (this.threadNum * F + R < capacity)
                return true;
            else
            {
                // 寻找可并行计算的进程数
                while(p * F + R > capacity  &&  p >= 0)
                    --p;

                if (p > 0)  // 分p个子区域，同时算，每次加载全部内存;
                {
                    flag = 1;
                    if (p <= this.threadNum)
                        return true;
                    else
                        return false;
                }
                else  // 无法一次性计算整个覆盖区域
                {
                    p = 3;

                    while ((F + R) / d > capacity)
                        ++d;

                    // 分d个子区域，分批算，每次仅加载一部分内存，最后进行二次投射
                    flag = 2;
                    return false;
                }
            }
        }

        private void drawCoverageSectors(LTE.Geometric.Point s, double distance, double fromAngle, double toAngle, int sectors)
        {
            double sectorAngle = (toAngle - fromAngle + 360) % 360;
            double angle = sectorAngle / sectors;
            for (int r = 0; r < sectors; r++)
            {
                double from = (fromAngle + r * angle);
                double to = (fromAngle + (r + 1) * angle);
                InterferenceFeatureLayerAnalysis.drawSector(s, from, to, distance);
            }
        }

        // 如果数据库表 tbAdjCoefficient 中有校正系数时，则界面中的校正系数仅仅被传入，而不会在计算场强中用到
        private void button1_Click(object sender, EventArgs e)
        {
            if (!validateCell())
                return;
            if (!validateInput())
                return;

            this.switchControls(false);

            DateTime startTime = DateTime.Now;

            CellInfo cellInfo = new CellInfo(this.cellName, this.eNodeB, this.CI, this.directCoefficient, this.reflectCoefficient, this.diffractCoefficient, this.diffractCoefficient2);

            //IbatisHelper.ExecuteNonQuery("setSQLServerMemory", null);  //设置sql内存边界值

            this.fromAngle = cellInfo.Azimuth - this.incrementAngle;
            this.toAngle = cellInfo.Azimuth + this.incrementAngle;

            //this.fromAngle = 295;
            //this.toAngle = 65;

            if (this.checkBox4.Checked) // 手动指定角度
            {
                this.fromAngle = float.Parse(this.textBox2.Text);
                this.toAngle = float.Parse(this.textBox4.Text);
            }

            if (this.multimes)  // 分批计算
            {
                double delta = (this.toAngle - this.fromAngle + 360) % 360 / this.mulNum;
                this.fromAngle = (this.fromAngle + (this.curNum-1) * delta + 360) % 360;
                this.toAngle = (fromAngle + delta + 360) % 360;
            }
            else
            {
                int p = 0, d = 0, flag = 0;
                bool ok = howCalc(ref p, ref d, ref flag);
                if (!ok)
                {
                    if (flag == 2)
                    {
                        MessageBox.Show(string.Format("建议计算方案：\n线程数：{0}\n批数：{1}\n", p, d));
                    }
                    else
                    {
                        MessageBox.Show(string.Format("建议计算方案：\n线程数：{0}\n", p));
                    }
                    this.switchControls(true);
                    return;
                }
            }
            //this.fromAngle = 141;
            //this.toAngle = 185;

            //drawCoverageSectors(cellInfo.SourcePoint, this.distance, this.fromAngle, this.toAngle, this.threadNum);

            ////统计加速结构的内存大小
            //testMemorySize(mingxid, mingyid, maxgxid, maxgyid);

            if (this.bids.Count > 0)
            {
                coverageAnalysis(cellInfo, startTime);
            }
            else 
            {
                this.reRay = false;  // 是否需要进行二次投射，即读取前一批覆盖计算中出界的射线，并对其进行射线跟踪
                if (this.multimes && this.curNum > 1)  // 如果是分批计算，且当前不是最后一批，就需要进行二次投射
                    this.reRay = true;

                Hashtable ht = new Hashtable();
                ht["CI"] = this.CI;
                ht["eNodeB"] = eNodeB;

                if (!multimes || (multimes && curNum == 1))  // 不是分批计算或第一批
                {
                    // 删除旧的接收功率数据
                    GridCover gc = GridCover.getInstance();
                    gc.deleteBuildingCover(ht);
                    gc.deleteGroundCover(ht);
                }

                if(multimes || curNum == 1)  // 是分批计算，且是第一批
                    IbatisHelper.ExecuteDelete("deleteSpecifiedReRay", ht);

                parallelComputing(cellInfo, fromAngle, toAngle);
            }

            if(this.multimes && this.curNum == this.mulNum)
                MessageBox.Show("当前是分批计算的最后一批，请在计算结束后进行分批计算合并！");

            this.switchControls(true);
            //GISLocate.Instance.LocateToPoint(cellInfo.SourcePoint);
            return;
        }


        private void beforeCalc(CellInfo cellInfo)
        {
            //生成加速结构
            Grid3D accgrid = new Grid3D(), ggrid = new Grid3D();

            if (!GridHelper.getInstance().PointXYZToAccGrid(cellInfo.SourcePoint, ref accgrid))
            {
                MessageBox.Show("无法获取小区所在加速网格坐标，计算结束！");
                return;
            }

            int mingxid = -1, maxgxid = -1, mingyid = -1, maxgyid = -1;
            int gridlength = GridHelper.getInstance().getAGridSize();
            int deltagrid = (int)Math.Ceiling(this.distance * 1.5 / gridlength);
            maxgyid = accgrid.gyid + deltagrid;
            mingyid = accgrid.gyid - deltagrid;
            maxgxid = accgrid.gxid + deltagrid;
            mingxid = accgrid.gxid - deltagrid;
            if (mingxid < 0)
            {
                maxgxid -= mingxid;
                mingxid = 0;
            }
            if (mingyid < 0)
            {
                maxgyid -= mingyid;
                mingyid = 0;
            }

            AccelerateStruct.clearAccelerateStruct();
            BuildingGrid3D.clearBuildingData();
            BuildingGrid3D.clearBuildingVertexOriginal();
            BuildingGrid3D.clearGrid3D();

            AccelerateStruct.setAccGridRange(mingxid, mingyid, maxgxid, maxgyid);
            AccelerateStruct.constructAccelerateStruct();

            //建筑物信息加速
            if (!GridHelper.getInstance().PointXYZToGrid3D(cellInfo.SourcePoint, ref ggrid))
            {
                MessageBox.Show("无法获取小区所在地面网格坐标，计算结束！");
                return;
            }

            gridlength = GridHelper.getInstance().getGGridSize();
            deltagrid = (int)Math.Ceiling((this.distance < 800 ? 1500 : this.distance * 2) / gridlength);
            maxgyid = ggrid.gyid + deltagrid;
            mingyid = ggrid.gyid - deltagrid;
            maxgxid = ggrid.gxid + deltagrid;
            mingxid = ggrid.gxid - deltagrid;
            if (mingxid < 0)
            {
                maxgxid -= mingxid;
                mingxid = 0;
            }
            if (mingyid < 0)
            {
                maxgyid -= mingyid;
                mingyid = 0;
            }

            BuildingGrid3D.setGGridRange(mingxid, mingyid, maxgxid, maxgyid);
            BuildingGrid3D.constructBuildingData();
            BuildingGrid3D.constructBuildingVertexOriginal();

            if (this.computeIndoor)
            {
                BuildingGrid3D.constructGrid3D();
            }

            GroundGrid.setBound(mingxid, mingyid, maxgxid, maxgyid);
            int cnt = GroundGrid.constructGGrids();
        }

        private void parallelComputing(CellInfo cellInfo, double fromAngle, double toAngle)
        {
            string bidstext = "-1";
            this.bids.ForEach(delegate(int bid)
            {
                bidstext += "," + bid;
            });

            bool recordReRay = false;  // 是否需要记录当前批的出界射线

            // 如果是分批计算，且当前批不是最后一批，则需记录当前批的出界射线，供下批二次投射
            if (this.multimes && this.curNum < this.mulNum) 
                recordReRay = true;

            if (!this.ExistProcess(this.eNodeB, this.CI))
            {
                //CtrlForm multiCtrl = new CtrlForm();
                //multiCtrl.Show();
                //multiCtrl.init(cellInfo,
                //    distance, fromAngle, toAngle, reflectionNum, diffractionNum, computeIndoor, sideSplitUnit,
                //    threadNum, bids, computeVSide, reRay);

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
                    this.threadNum, bidstext, this.sideSplitUnit, this.computeVSide, this.reRay, recordReRay, false, false);

                try
                {
                    //MessageBox.Show("preStart");
                    pa.pro = Process.Start(psi);
                    paList.Add(pa);
                    pa.pro.WaitForExit();
                    //MessageBox.Show("endStart");
                    
                }
                catch (InvalidOperationException exception)
                {
                    MessageBox.Show("多进程计算启动失败，原因： " + exception.Message);
                }
                catch (Exception ee)
                {
                    MessageBox.Show("多进程计算启动失败，原因： " + ee.Message);
                }
            }
            else
            {
                MessageBox.Show("该小区正在计算");
            }
        }

        private void coverageAnalysis(CellInfo cellInfo, DateTime startTime)
        {
            DateTime d0, d1, d2, d3, d4, d5;
            d0 = DateTime.Now;

            this.beforeCalc(cellInfo);

            d1 = DateTime.Now;

            // 网格立体覆盖类，用于记录射线跟踪结果，与数据库交互
            GridCover gc = GridCover.getInstance();
            // 根据覆盖扇形获取建筑物id
            List<int> bids = BuildingGrid3D.getBuildingIDBySector(cellInfo.SourcePoint, this.distance, this.fromAngle, this.toAngle);
            Console.WriteLine("building num = {0}", bids.Count);
            // 求比基准高度高的建筑物遮挡距离和角度范围
            List<TriangleBound> disAngle = BuildingGrid3D.getShelterDisAndAngle(cellInfo.SourcePoint, bids, 0);
            Console.WriteLine("disAngle num = {0}", disAngle.Count);
            //<LTE.Geometric.Point> points = GroundGrid.getGGridCenterBySector(cellInfo.SourcePoint, this.distance, this.fromAngle, this.toAngle, null);
            // 获取扇区与地面栅格交集内的栅格中心点
            List<LTE.Geometric.Point> points2 = GroundGrid.getGGridCenterBySector(cellInfo.SourcePoint, this.distance, this.fromAngle, this.toAngle, disAngle);
            //Console.WriteLine("ground grid num = {0}, after filter num = {1}", points.Count, points2.Count);
            // 返回所有建筑物相对于原点可见的侧面点集合
            List<LTE.Geometric.Point> vPoints = VerticalPlaneGrid.GetAllVerticalGrid(cellInfo.SourcePoint, bids, 3.0);
            double mergeAngle = 5.0 / 2000;//弧度制
            // 合并射线终点，射线的角度小于angle的合并
            List<LTE.Geometric.Point> vmPoints = GeometricUtilities.mergePointsByAngle(cellInfo.SourcePoint, vPoints, mergeAngle);
            Console.WriteLine("building points num = {0}, after filter num = {1}", vPoints.Count, vmPoints.Count);
            // 根据遮挡关系获取所有建筑物棱边绕射点
            List<LTE.Geometric.Point> diffPoints = BuildingGrid3D.getBuildingsEdgePointsByShelter(cellInfo.SourcePoint.Z, disAngle, 5);
            Console.WriteLine("building edge points num = {0}", diffPoints.Count);

            return;  //  ??????
            d2 = DateTime.Now;

            RayTracing interAnalysis = new RayTracing(cellInfo, this.reflectionNum, this.diffractionNum, this.computeIndoor);
            //菲尼尔绕射
            //interAnalysis.fei_Analysis(this.fromAngle, this.toAngle);
            //网格式扇形计算
            List<int> num1 = null, num2 = null, num3 = null;
            DateTime s1 = DateTime.Now;

            //interAnalysis.rayTracing();
            //num1 = interAnalysis.rayTracing(this.fromAngle, this.toAngle, this.distance, false);
            if (num1 == null)
            {
                num1 = new List<int>();
                num1.Add(0);
                num1.Add(0);
                num1.Add(0);
            }

            DateTime s2 = DateTime.Now;
            this.bids = BuildingGrid3D.getBuildingIDBySector(cellInfo.SourcePoint, this.distance, this.fromAngle, this.toAngle);
            //num2 = interAnalysis.buildingAnalysis(this.bids);
            if (num2 == null)
            {
                num2 = new List<int>();
                num2.Add(0);
                num2.Add(0);
                num2.Add(0);
            }

            DateTime s3 = DateTime.Now;
            //DiffractedRayAnalysis diffAnalysis = new DiffractedRayAnalysis(cellInfo, this.reflectionNum, this.diffractionNum, this.sideSplitUnit, this.computeIndoor);
            //num3 = diffAnalysis.diffractedRayAnalysis(this.bids, this.computeVSide);
            if (num3 == null)
            {
                num3 = new List<int>();
                num3.Add(0);
                num3.Add(0);
                num3.Add(0);
            }

            DateTime endTime = DateTime.Now;

            Hashtable ht1 = new Hashtable();
            ht1["eNodeB"] = this.eNodeB;
            ht1["CI"] = this.CI;

            gc.clearBuilding();
            gc.clearGround();

            CalcGridStrength calc = new CalcGridStrength(cellInfo, null);
            Dictionary<string, GridStrength> tstrength = calc.MergeMultipleTaskStrength(interAnalysis.getGridStrengths());
            gc.convertToDt(tstrength);

            gc.wirteGroundCover(ht1);
            gc.clearGround();

            if (this.computeIndoor)
            {
                gc.writeBuildingCover(ht1);
                gc.clearBuilding();
            }

            //GISLocate.Instance.LocateToPoint(cellInfo.SourcePoint);

            Hashtable ht = new Hashtable();
            ht[0] = "GIS窗体";
            ht[1] = "小区功率计算完毕";

            AccelerateStruct.clearAccelerateStruct();
            BuildingGrid3D.clearBuildingData();
            BuildingGrid3D.clearBuildingVertexOriginal();
            BuildingGrid3D.clearGrid3D();

            MessageBox.Show(this, String.Format("小区功率计算完毕\n\n开始时间：{0}\n地面网格数/初始射线数：{1}，接收射线数：{2}，用时：{3:0.0}分钟\n室内覆盖建筑物数：{4}，初始射线数：{5}，接收射线数：{6}，用时{7:0.0}分钟\n绕射建筑物数：{8}，绕射初始线数：{9}，接收射线数：{10}，用时：{11:0.0}分钟\n\n总初始射线数：{12}，总接收射线数：{13}，总计算时间：{14:0.0}分钟\n", startTime.ToString(), num1[0], num1[1], (s2 - s1).TotalMinutes, num2[0], num2[1], num2[2], (s3 - s2).TotalMinutes, num3[0], num3[1], num3[2], (endTime - s3).TotalMinutes, num1[0] + num2[1] + num3[1], num1[1] + num2[2] + num3[2], (endTime - startTime).TotalMinutes), "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        

        private void testMemorySize(int mingxid, int mingyid, int maxgxid, int maxgyid)
        {
            int nlz, times = 1000000, minID = 0, maxID = 0;

            byte[] b1 = new byte[times];
            byte[] b2 = new byte[times];
            Random rand = new Random();
            rand.NextBytes(b1);
            rand.NextBytes(b2);
            BuildingGrid3D.getBuildingIDRange(out minID, out maxID);

            DateTime dt1 = DateTime.Now;
            for (nlz = 0; nlz < times; nlz++)
            {
                int x = Convert.ToInt32(b1[nlz] * (maxgxid - mingxid) / 255) + mingxid;
                int y = Convert.ToInt32(b2[nlz] * (maxgyid - mingyid) / 255) + mingyid;
                int z = Convert.ToInt32(b1[nlz] % 4);
                List<int> buildingids = AccelerateStruct.getAccelerateStruct(x, y, z);
            }
            DateTime dt2 = DateTime.Now;

            for (nlz = 0; nlz < times; nlz++)
            {
                int bid = rand.Next(minID, maxID);
                BuildingGrid3D.getBuildingVertex(bid);
            }
            DateTime dt3 = DateTime.Now;

            int liti = AccelerateStruct.getDataMemory();
            int build = BuildingGrid3D.getDataMemory();

            AccelerateStruct.clearAccelerateStruct();
            BuildingGrid3D.clearBuildingData();
            BuildingGrid3D.clearBuildingVertexOriginal();
            BuildingGrid3D.clearGrid3D();

            this.switchControls(true);

            MessageBox.Show(this, "立体加速网格内存结构大小：" + (liti >> 10) + "K\n建筑物与地面网格结构的大小：" + (build >> 10) + "K\n立体加速查询" + times + "次，用时" + (dt2.Subtract(dt1).TotalMilliseconds) + "豪秒\n建筑物与地面网格结构查询" + times + "次，用时" + (dt3.Subtract(dt2).TotalMilliseconds) + "豪秒");
        }

        void async_AsyncFinished(object o)
        {
            Hashtable ht = new Hashtable();
            ht[0] = "GIS窗体";
            ht[1] = "小区功率计算完毕";
            this.switchControls(true);
        }

        object CellAnalysis(object[] args)
        {
            CellInfo cellInfo = new CellInfo(this.cellName, this.eNodeB, this.CI, this.directCoefficient, this.reflectCoefficient, this.diffractCoefficient, this.diffractCoefficient2);
            Hashtable ht1 = new Hashtable();
            ht1["eNodeB"] = this.eNodeB;
            ht1["CI"] = this.CI;
            IbatisHelper.ExecuteDelete("deleteSpecifiedCelltbGrids", ht1);


            RayTracing interAnalysis = new RayTracing(cellInfo, this.reflectionNum, this.diffractionNum, false);
            //interAnalysis.fei_Analysis(this.fromAngle, this.toAngle);
            //interAnalysis.rayTracing(this.fromAngle, this.toAngle, this.distance);
            //DiffractedRayAnalysis diffAnalysis = new DiffractedRayAnalysis(cellInfo, this.reflectionNum, this.diffractionNum, this.sideSplitUnit, false, this.fromAngle, this.toAngle, this.distance, this.computeVSide);
            //diffAnalysis.diffractedRayAnalysis();
            List<int> bids = new List<int>();
            //diffAnalysis.diffractedRayAnalysis(bids, this.computeVSide);

            //AnalysisEntry.DisplayAnalysis(cellInfo);
            //GISLocate.Instance.LocateToPoint(GeometryUtilities.ConstructPoint3D(cellInfo.SourcePoint, cellInfo.SourcePoint.Z));

            return null;
        }

        private Boolean validateInput()
        {
            //频点
            //if (this.textBox_Frenquncy.Text == string.Empty)
            //{
            //    MessageBox.Show(this, "请输入频点", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return false;
            //}
            //try { this.frequncy = int.Parse(this.textBox_Frenquncy.Text); }
            //catch
            //{
            //    MessageBox.Show(this, "您输入的频点格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    this.textBox_Frenquncy.Focus();
            //    return false;
            //}
            //if (this.cellType == "GSM900" && (this.frequncy < 1 || this.frequncy > 124))
            //{
            //    MessageBox.Show(this, "您输入的小区为" + this.cellType + "小区，频点范围为：1-124，请重新输入频点！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return false;
            //}
            //else if (this.cellType == "GSM1800" && (this.frequncy < 512 || this.frequncy > 885))
            //{
            //    MessageBox.Show(this, "您输入的小区为" + this.cellType + "小区，频点范围为：512-815，请重新输入频点！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return false;
            //}
            if (this.textBox_increment.Text == string.Empty)
            {
                MessageBox.Show(this, "请输入覆盖角度", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            try { this.incrementAngle = double.Parse(this.textBox_increment.Text); }
            catch
            {
                MessageBox.Show(this, "您输入的覆盖角度格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox_increment.Focus();
                return false;
            }
            if (this.incrementAngle > 90)
            {
                MessageBox.Show(this, "覆盖角度应小于90度，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            ////起始角度
            //if (this.textBox2.Text == string.Empty)
            //{
            //    MessageBox.Show(this, "请输入中心角起始角度", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return false;
            //}
            //try { this.fromAngle = double.Parse(this.textBox2.Text); }
            //catch
            //{
            //    MessageBox.Show(this, "您输入的中心角起始角度格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    this.textBox2.Focus();
            //    return false;
            //}
            ////终止角度
            //if (this.textBox4.Text == string.Empty)
            //{
            //    MessageBox.Show(this, "请输入中心角终止角度", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return false;
            //}
            //try { this.toAngle = double.Parse(this.textBox4.Text); }
            //catch
            //{
            //    MessageBox.Show(this, "您输入的中心角终止角度格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    this.textBox2.Focus();
            //    return false;
            //}
            //if (this.fromAngle > this.toAngle)
            //{
            //    MessageBox.Show(this, "方位角起始角应小于终止角，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return false;
            //}

            //最大距离
            if (this.textBox3.Text == string.Empty)
            {
                MessageBox.Show(this, "请输入最大距离", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            try { this.distance = double.Parse(this.textBox3.Text); }
            catch
            {
                MessageBox.Show(this, "您输入的最大距离格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox3.Focus();
                return false;
            }

            this.computeIndoor = this.checkBox2.Checked;
            this.computeVSide = this.checkBox1.Checked;
            
            string bidstext = this.textBox_bid.Text.Trim();
            bids = new List<int>();
            if (bidstext.Length > 0)
            {
                bidstext.Split(';').ToList().ForEach(delegate(string s)
                {
                    try
                    {
                        bids.Add(Convert.ToInt32(s));
                    }
                    catch (Exception e)
                    {
                    }
                });
            }

            try
            {
                this.threadNum = int.Parse(this.textBox1.Text);
                if (this.threadNum < -1)
                {
                    throw new Exception("线程个数太多");
                }
                if (this.threadNum > 10)
                {
                    MessageBox.Show(this, "您输入的线程个数太多，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }
            }
            catch
            {
                MessageBox.Show(this, "您输入的线程个数格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox1.Focus();
                return false;
            }

            this.multimes = this.checkBox5.Checked;
            if (this.multimes)
            {
                int.TryParse(this.textBox14.Text, out this.mulNum);
                int.TryParse(this.textBox11.Text, out this.curNum);
            }

            try { this.directCoefficient = float.Parse(this.textBox5.Text); }
            catch
            {
                MessageBox.Show(this, "您输入的直射系数格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox5.Focus();
                return false;
            }
            if (this.directCoefficient < 0.1 || this.directCoefficient > 3.0)
            {
                MessageBox.Show(this, "直射系数取值范围为 （0.1-3.0），请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox5.Focus();
                return false;
            }
            try { this.reflectCoefficient = float.Parse(this.textBox6.Text); }
            catch
            {
                MessageBox.Show(this, "您输入的反射系数格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox6.Focus();
                return false;
            }
            if (this.reflectCoefficient < 0.3 || this.reflectCoefficient > 1.5)
            {
                MessageBox.Show(this, "反射系数取值范围为 （0.3-1.5），请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox6.Focus();
                return false;
            }
            try { this.diffractCoefficient = float.Parse(this.textBox7.Text); }
            catch
            {
                MessageBox.Show(this, "您输入的绕射系数格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox7.Focus();
                return false;
            }
            if (this.diffractCoefficient < 0.3 || this.diffractCoefficient > 1.5)
            {
                MessageBox.Show(this, "绕射系数取值范围为 （0.3-1.5），请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox7.Focus();
                return false;
            }
            try { this.diffractCoefficient2 = float.Parse(this.textBox8.Text); }
            catch
            {
                MessageBox.Show(this, "您输入的绕射系数格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox8.Focus();
                return false;
            }

            try { this.reflectionNum = int.Parse(this.textBox_ReflectionNum.Text); }
            catch
            {
                MessageBox.Show(this, "您输入的反射次数格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox_ReflectionNum.Focus();
                return false;
            }
            try { this.diffractionNum = int.Parse(this.textBox_DiffractionNum.Text); }
            catch
            {
                MessageBox.Show(this, "您输入的绕射次数格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox_DiffractionNum.Focus();
                return false;
            }
            try { this.sideSplitUnit = float.Parse(this.textBox_SideSplitUnit.Text); }
            catch
            {
                MessageBox.Show(this, "您输入的建筑物棱边绕射点间隔格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox_SideSplitUnit.Focus();
                return false;
            }

            //try { float.TryParse(this.textBox9.Text, out this.jiezhiCoefficient); }
            //catch
            //{
            //    MessageBox.Show(this, "您输入的介质系数格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    this.textBox9.Focus();
            //    return false;
            //}
            //try { float.TryParse(this.textBox10.Text, out this.jiezhiCoefficient); }
            //catch
            //{
            //    MessageBox.Show(this, "您输入的介质系数格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    this.textBox10.Focus();
            //    return false;
            //}

            return true;
        }

        private bool validateCell()
        {
            if (this.textBox_CellName.Text == string.Empty)
            {
                MessageBox.Show(this, "请输入小区名称", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            this.cellName = textBox_CellName.Text.Trim();
            DataTable dt = IbatisHelper.ExecuteQueryForDataTable("SingleGetCellType", this.cellName);
            if (dt.Rows.Count == 0)
            {
                MessageBox.Show(this, "您输入的小区名称有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            this.eNodeB = Convert.ToInt32(dt.Rows[0]["eNodeB"]);
            this.CI = Convert.ToInt32(dt.Rows[0]["CI"]);
            this.cellType = Convert.ToString(dt.Rows[0]["NetType"]);
            return true;
        }
        private void setFrenquncyToolTip()
        {
            object o = IbatisHelper.ExecuteScalar("SingleGetCellType", this.textBox_CellName.Text);
            if (o == null)
                return;

            if (o.ToString() == "GSM900")
                this.toolTip1.SetToolTip(this.textBox_Frenquncy, "(范围：1-124)");
            else if (o.ToString() == "GSM1800")
                this.toolTip1.SetToolTip(this.textBox_Frenquncy, "(范围：512-885)");
        }

        private void switchControls(bool s)
        {
            if (this.textBox_CellName.InvokeRequired)
            {
                switchControlsHandler switchHandler = new switchControlsHandler(switchControls);
                this.textBox_CellName.BeginInvoke(switchHandler);
                return;
            }
            //this.textBox_CellName.Enabled = s;
            //this.textBox2.Enabled = s;
            //this.textBox3.Enabled = s;
            //this.textBox4.Enabled = s;
            //this.textBox5.Enabled = s;
            //this.textBox6.Enabled = s;
            //this.textBox7.Enabled = s;
            //this.textBox8.Enabled = s;
            //this.textBox9.Enabled = s;
            //this.textBox10.Enabled = s;
            this.button1.Enabled = s;
            this.button2.Enabled = s;
            this.button4.Enabled = s;
        }
        private delegate void switchControlsHandler(bool s);

        private void textBox_Frenquncy_Enter(object sender, EventArgs e)
        {
            this.setFrenquncyToolTip();
        }

        private void FrmCellRayTracing_Load(object sender, EventArgs e)
        {
            System.Windows.Forms.Form.CheckForIllegalCrossThreadCalls = false;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            
        }

        private void groupBox4_Enter(object sender, EventArgs e)
        {

        }

        public double convertw2dbm(double w)
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
        private void mergePwr(double EIRP, string getTb, bool flag)
        {
            Hashtable ht = new Hashtable();
            ht["CI"] = this.CI;
            ht["eNodeB"] = eNodeB;
            DataTable tb = IbatisHelper.ExecuteQueryForDataTable(getTb, ht);

            GridCover gc = GridCover.getInstance();

            // 删除旧数据
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
                if(flag)
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

            # region 计算最后一批栅格最终功率
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

        // 不是一次性计算一个小区的话，需要对相同小区、相同栅格的功率进行合并
        private void button4_Click(object sender, EventArgs e)
        {
            if (!validateCell())
                return;
            if (!validateInput())
                return;

            this.switchControls(false);
            CellInfo cellInfo = new CellInfo(this.cellName, this.eNodeB, this.CI, this.directCoefficient, this.reflectCoefficient, this.diffractCoefficient, this.diffractCoefficient2);

            DateTime t0, t1, t2;
            t0 = DateTime.Now;
            mergePwr(cellInfo.EIRP, "getPwrGround", false);
            t1 = DateTime.Now;

            //MessageBo       x.Show(string.Format("地面用时：{0}s", (t1 - t0).TotalMilliseconds / 1000));

            t1 = DateTime.Now;
            mergePwr(cellInfo.EIRP, "getPwrBuilding", true);
            t2 = DateTime.Now;
            //MessageBox.Show(string.Format("建筑物用时：{0}s", (t2 - t1).TotalMilliseconds / 1000));
            MessageBox.Show("栅格场强合并结束！");
            //MessageBox.Show(string.Format("耗时：{0} min", (t2 - t1).Minutes));
            //MessageBox.Show(string.Format("耗时：{0} min", (t2 - t1).TotalMilliseconds / 60000));
            this.switchControls(true);
        }

        private void groupBox10_Enter(object sender, EventArgs e)
        {

        }
    }

    class ProcessArgs
    {
        public Process pro;
        public string cellName;
        public int eNodeB;
        public int CI;
    }
}
