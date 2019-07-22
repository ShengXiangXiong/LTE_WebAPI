using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using ESRI.ArcGIS.Geometry;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Management;
using LTE.InternalInterference;

namespace LTE.Calibration
{
    public partial class CalibrationForm : Form
    {
        private int popSize;
        private int gen;
        private string startDateTime, endDateTime;
        private int scenNum;
        // 真实路测，这里是根据射线轨迹得到的路测加上随机扰动的结果，为模拟路测
        private Dictionary<string, double> meaPwr;
        // 射线计算结果
        private Dictionary<string, TrajInfo> rayDic;

        public CalibrationForm()
        {
            InitializeComponent();
            dateTimePicker1.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            dateTimePicker2.CustomFormat = "yyyy-MM-dd HH:mm:ss";
        }

        private Boolean validateInput()
        {
            this.startDateTime = this.dateTimePicker1.Value.ToString("yyyy-MM-dd HH:mm:ss");
            this.endDateTime = this.dateTimePicker2.Value.ToString("yyyy-MM-dd HH:mm:ss");

            if (this.textBox1.Text == string.Empty)
            {
                MessageBox.Show(this, "请输入场景个数", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (this.textPopSize.Text == string.Empty)
            {
                MessageBox.Show(this, "请输入种群大小", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (this.textGen.Text == string.Empty)
            {
                MessageBox.Show(this, "请输入遗传代数", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            try { int.TryParse(this.textBox1.Text, out this.scenNum); }
            catch
            {
                MessageBox.Show(this, "您输入的场景数目格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox1.Focus();
                return false;
            }
            try { int.TryParse(this.textPopSize.Text, out this.popSize); }
            catch
            {
                MessageBox.Show(this, "您输入的种群大小格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textPopSize.Focus();
                return false;
            }
            try { int.TryParse(this.textGen.Text, out this.gen); }
            catch
            {
                MessageBox.Show(this, "您输入的遗传代数格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textGen.Focus();
                return false;
            }

            return true;
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

        private void switchControls(bool s)
        {
            this.button1.Enabled = s;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!validateInput())
                return;

            this.switchControls(false);

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
                MessageBox.Show("不存在位于以上时间段的路测数据！");
                this.switchControls(true);
                return;
            }

            DataTable tb;

            // 内存控制，这里仅对射线计算结果进行了内存控制，如果有真实路测，同样需要对真实路测进行内存控制
            int cnt = Convert.ToInt32(tb1.Rows[0][0]);
            if (cnt  > maxGridNum)
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

            if (tb.Rows.Count < 1)
            {
                MessageBox.Show("不存在射线数据！");
                this.switchControls(true);
                return;
            }

            filter(ref tb); // 选择射线跟踪计算结果和真实路测的公共部分

            int frequence = 63;  // EARFCN，绝对频率号
            int popSize = Convert.ToInt32(this.textPopSize.Text);
            int gen = Convert.ToInt32(this.textGen.Text);
            EA ea = new EA();
            EA.initEA(popSize, gen, scenNum, ref meaPwr, ref rayDic, frequence);
            ea.GaMain();
            MessageBox.Show("校正完成");
            this.switchControls(true);
        }

        // 选择射线跟踪计算结果和真实路测的公共部分
        private void filter(ref DataTable tb)
        {
            // 筛选之前
            Dictionary<string, TrajInfo> rayDicOri = CalRays.buildingGrids(ref tb);  // 根据射线跟踪结果算出的路测

            // 本应为真实路测，这里为模拟路测，如果用实际路测，同样需要进行内存控制
            Dictionary<string, double> meaPwrOri = CalRays.getMeaPwr(ref rayDicOri, scenNum);  

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


    }
}
