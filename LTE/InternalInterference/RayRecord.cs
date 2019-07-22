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
    public partial class RayRecord : Form
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

        public RayRecord()
        {
            InitializeComponent();
            if (paList == null)
            {
                paList = new List<ProcessArgs>();
            }
        }

        public RayRecord(string cellName, int lac, int ci)
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

        private void smoothBuildingPoints()
        {
            BuildingGrid3D.constructBuildingVertexOriginal();
            int minBid, maxBid;
            BuildingGrid3D.getAllBuildingIDRange(out minBid, out maxBid);

            DataTable dt = new DataTable();
            dt.Columns.Add("BuildingID", System.Type.GetType("System.Int32"));
            dt.Columns.Add("VertexLong", System.Type.GetType("System.Double"));
            dt.Columns.Add("VertexLat", System.Type.GetType("System.Double"));
            dt.Columns.Add("VertexX", System.Type.GetType("System.Double"));
            dt.Columns.Add("VertexY", System.Type.GetType("System.Double"));
            dt.Columns.Add("VIndex", System.Type.GetType("System.Int16"));

            IbatisHelper.ExecuteDelete("DeleteBuildingVertex", null);

            for (int i = minBid; i <= maxBid; i++)
            {
                List<LTE.Geometric.Point> bpoints = BuildingGrid3D.getBuildingVertexOriginal(i);
                
                List<LTE.Geometric.Point> ps = GeometryUtilities.SmoothBuildingPoints(ref bpoints);  // 2018-05-08
                if (ps.Count < 6)
                    ps = bpoints;

                for (int j = 0; j < ps.Count; j++)
                {
                    ESRI.ArcGIS.Geometry.IPoint p = GeometryUtilities.ConstructPoint2D(ps[j].X, ps[j].Y);
                    PointConvert.Instance.GetGeoPoint(p);
                    DataRow dr = dt.NewRow();
                    dr["BuildingID"] = i;
                    dr["VertexLong"] = p.X;
                    dr["VertexLat"] = p.Y;
                    dr["VertexX"] = ps[j].X;
                    dr["VertexY"] = ps[j].Y;
                    dr["VIndex"] = j;
                    dt.Rows.Add(dr);
                }
                if (dt.Rows.Count >= 5000)
                {
                    DataUtil.BCPDataTableImport(dt, "tbBuildingVertex");
                    dt.Clear();
                }
            }
            DataUtil.BCPDataTableImport(dt, "tbBuildingVertex");
            dt.Clear();
            BuildingGrid3D.clearBuildingVertexOriginal();

            MessageBox.Show("处理完成！");
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

            double theta = (this.toAngle - this.fromAngle + 360) / 360;
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
                    if (d <= this.mulNum)
                        return true;
                    else
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

        private void button1_Click(object sender, EventArgs e)
        {
            if (!validateInput())
                return;

            this.switchControls(false);

            DateTime startTime = DateTime.Now;

            //IbatisHelper.ExecuteNonQuery("setSQLServerMemory", null);  //设置sql内存边界值

            if (this.radioButton2.Checked) // 生成并记录用于定位的射线
            {
                CellInfo cellInfo;

                if (this.is_source.Checked)  // 手动指定发射源
                {
                    cellInfo = new CellInfo();
                    cellInfo.SourcePoint = new Geometric.Point();
                    cellInfo.SourcePoint.X = double.Parse(this.sourceX.Text);
                    cellInfo.SourcePoint.Y = double.Parse(this.sourceY.Text);
                    cellInfo.SourcePoint.Z = double.Parse(this.sourceZ.Text);
                    cellInfo.SourceName = this.textBoxCI.Text;
                    cellInfo.CI = int.Parse(this.textBoxCI.Text);
                    cellInfo.EIRP = 100;
                    cellInfo.Inclination = -7;
                    cellInfo.diffracteCoefficient = this.diffractCoefficient;
                    cellInfo.reflectCoefficient = this.reflectCoefficient;
                    cellInfo.directCoefficient = this.directCoefficient;
                    cellInfo.diffracteCoefficient2 = this.diffractCoefficient2;

                    if (this.checkBox3.Checked) // 手动指定方位角
                    {
                        Geometric.Point end = new Geometric.Point();
                        end.X = double.Parse(this.endX.Text);
                        end.Y = double.Parse(this.endY.Text);
                        cellInfo.Azimuth = LTE.Geometric.GeometricUtilities.getPolarCoord(end, cellInfo.SourcePoint).theta / Math.PI * 180;
                    }
                    else if (this.cellName != null && this.cellName != string.Empty)  // 方位角中心线末端为指定小区
                    {
                        if (!validateCell())
                        {
                            this.switchControls(true);
                            return;
                        }

                        CellInfo end = new CellInfo(this.cellName, this.eNodeB, this.CI, this.directCoefficient, this.reflectCoefficient, this.diffractCoefficient, this.diffractCoefficient2);
                        cellInfo.Azimuth = LTE.Geometric.GeometricUtilities.getPolarCoord(end.SourcePoint, cellInfo.SourcePoint).theta / Math.PI * 180;
                    }
                    else
                    {
                        MessageBox.Show("请填写小区名称或手动指定方位角");
                        this.switchControls(true);
                        return;
                    }

                    fromAngle = cellInfo.Azimuth - this.incrementAngle;
                    toAngle = cellInfo.Azimuth + this.incrementAngle;
                }
                else  // 发射源为指定的小区
                {
                    if (!validateCell())
                        return;

                    cellInfo = new CellInfo(this.cellName, this.eNodeB, this.CI, this.directCoefficient, this.reflectCoefficient, this.diffractCoefficient, this.diffractCoefficient2);
                    this.fromAngle = cellInfo.Azimuth - this.incrementAngle;
                    this.toAngle = cellInfo.Azimuth + this.incrementAngle;
                }

                parallelComputing(cellInfo, fromAngle, toAngle, true, false);
            }
            // 生成并记录用于系数校正的射线
            // 先生成虚拟路测，只会记录射线终点位于虚拟路测中的轨迹，而不是所有轨迹
            // 如果虚拟路测为空，则生成的射线轨迹不会记在数据库中
            else if (this.radioButton1.Checked)
            {
                if (!validateCell())
                    return;

                CellInfo cellInfo = new CellInfo(this.cellName, this.eNodeB, this.CI, this.directCoefficient, this.reflectCoefficient, this.diffractCoefficient, this.diffractCoefficient2);
                this.fromAngle = cellInfo.Azimuth - this.incrementAngle;
                this.toAngle = cellInfo.Azimuth + this.incrementAngle;

                parallelComputing(cellInfo, fromAngle, toAngle, false, true);
            }
            
            this.switchControls(true);
            return;
        }

        private void parallelComputing(CellInfo cellInfo, double fromAngle, double toAngle, bool loc, bool adj)
        {
            string bidstext = "-1";
            this.bids.ForEach(delegate(int bid)
            {
                bidstext += "," + bid;
            });

            if (!this.ExistProcess(this.eNodeB, this.CI))
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
                    this.threadNum, bidstext, this.sideSplitUnit, this.computeVSide, this.reRay, false, loc, adj);

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
            this.cellName = this.textBox_CellName.Text;
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
                //MessageBox.Show(this, "请输入小区名称", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        }
        private delegate void switchControlsHandler(bool s);

        private void FrmCellRayTracing_Load(object sender, EventArgs e)
        {
            System.Windows.Forms.Form.CheckForIllegalCrossThreadCalls = false;
        }

        private void groupBox4_Enter(object sender, EventArgs e)
        {

        }

        
        private void groupBox10_Enter(object sender, EventArgs e)
        {

        }

        private void sourceX_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
