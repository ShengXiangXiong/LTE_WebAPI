using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LTE.GIS;
using System.Collections;
using LTE.InternalInterference.Grid;
using LTE.DB;

namespace LTE.InternalInterference
{
    public partial class AreaCoverDefect : Form
    {
        public AreaCoverDefect()
        {
            InitializeComponent();
        }


        private bool valideInput(ref int mingxid, ref int maxgxid, ref int mingyid, ref int maxgyid)
        {
            if (this.textBox1.Text != "" && this.textBox2.Text != "" && this.textBox3.Text != "" && this.textBox4.Text != "")
            {
                try
                {
                    mingxid = Convert.ToInt32(this.textBox1.Text);
                    maxgxid = Convert.ToInt32(this.textBox2.Text);
                    mingyid = Convert.ToInt32(this.textBox3.Text);
                    maxgyid = Convert.ToInt32(this.textBox4.Text);
                }
                catch (Exception e)
                {
                    return false;
                }
            }
            return true;
        }

        
        private void button1_Click(object sender, EventArgs e)
        {
            int mingxid, maxgxid, mingyid, maxgyid;
            mingxid = maxgxid = mingyid = maxgyid = -1;
            if ( ! valideInput(ref mingxid, ref maxgxid, ref mingyid, ref maxgyid) )
            {
                MessageBox.Show("边界网格输入有误！");
                return;
            }

            defectAnalysis(mingxid, mingyid, maxgxid, maxgyid);
        }

        enum DefectType
        {
            Weak,         // 弱覆盖点
            Excessive,    // 过覆盖点
            Overlapped,   // 重叠覆盖点
            PCIconflict,  // PCI 冲突点
            PCIconfusion, // PCI 混淆
            PCImod3       // PCI 模 3 冲突点
        };

        private void defectAnalysis(int minxid, int minyid, int maxxid, int maxyid)
        {
            double T = 6;  // 门限
            double T1 = 6;
            Hashtable ht = new Hashtable();
            ht["MinGXID"] = minxid;
            ht["MinGYID"] = minyid;
            ht["MaxGXID"] = maxxid;
            ht["MaxGYID"] = maxyid;

            System.Data.DataTable dtable = new System.Data.DataTable();
            dtable.Columns.Add("GXID");
            dtable.Columns.Add("GYID");
            dtable.Columns.Add("GZID");
            dtable.Columns.Add("type");
            dtable.Columns.Add("CI");
            dtable.Columns.Add("PCI");
            dtable.Columns.Add("ReceivedPowerdbm");
            dtable.Columns.Add("explain");

            double effective = -110;  // 有效信号阈值

            #region 地面
            DataTable tb = IbatisHelper.ExecuteQueryForDataTable("CoverAnalysis", ht);
            int n = tb.Rows.Count;

            Dictionary<string, List<Sub>> dic = new Dictionary<string, List<Sub>>();       // 栅格的其它信息
            Dictionary<string, List<double>> xy = new Dictionary<string, List<double>>();  // 栅格位置

            int weak = 0;  // 弱覆盖
            List<Analysis> exessive = new List<Analysis>(); // 过覆盖
            List<Analysis> overlap = new List<Analysis>();  // 重叠覆盖
            int pcim3 = 0; // pci mod 3 对打
            int pcic = 0; // pci 冲突
            int pcih = 0;  // pci 混淆

            for (int i = 0; i < n; i++)
            {
                int x = Convert.ToInt32(tb.Rows[i]["GXID"].ToString());
                int y = Convert.ToInt32(tb.Rows[i]["GYID"].ToString());
                double minX = Convert.ToDouble(tb.Rows[i]["minX"].ToString());
                double minY = Convert.ToDouble(tb.Rows[i]["minY"].ToString());
                double maxX = Convert.ToDouble(tb.Rows[i]["maxX"].ToString());
                double maxY = Convert.ToDouble(tb.Rows[i]["maxY"].ToString());
                double p = Convert.ToDouble(tb.Rows[i]["ReceivedPowerdbm"].ToString());
                int ci = Convert.ToInt32(tb.Rows[i]["ci"].ToString());
                int pci = Convert.ToInt32(tb.Rows[i]["pci"].ToString());

                string key = string.Format("{0},{1}", x, y);

                if (!xy.Keys.Contains(key))
                {
                    xy[key] = new List<double>();
                    xy[key].Add(minX);
                    xy[key].Add(minY);
                    xy[key].Add(maxX);
                    xy[key].Add(maxY);
                }

                if (dic.Keys.Contains(key))
                {
                    dic[key].Add(new Sub(p, ci, pci));
                }
                else
                {
                    dic[key] = new List<Sub>();
                    dic[key].Add(new Sub(p, ci, pci));
                }
            }

            foreach (string key in dic.Keys)
            {
                dic[key].Sort(new SubCompare());  // 按功率从大到小排序

                string[] id = key.Split(',');
                int xid = Convert.ToInt32(id[0]);
                int yid = Convert.ToInt32(id[1]);

                // 弱覆盖
                if (dic[key][0].p < -95)// && dic[key][m] > -110)
                {
                    weak++;

                    System.Data.DataRow thisrow = dtable.NewRow();
                    thisrow["GXID"] = xid;
                    thisrow["GYID"] = yid;
                    thisrow["GZID"] = 0;
                    thisrow["type"] = (short)DefectType.Weak;
                    thisrow["CI"] = dic[key][0].ci;
                    thisrow["PCI"] = dic[key][0].pci;
                    thisrow["ReceivedPowerdbm"] = dic[key][0].p;
                    thisrow["explain"] = "";
                    dtable.Rows.Add(thisrow);
                }

                // 当前栅格只接收到了1个信号，不存在pci模3对打、过覆盖、重叠覆盖、pci冲突、pci混淆
                if (dic[key].Count < 2) 
                    continue;

                // 过覆盖
                if (dic[key][0].p > effective && dic[key][1].p > effective && Math.Abs(dic[key][0].p - dic[key][1].p) < T1)
                {
                    Analysis A = new Analysis(xid, yid, xy[key][0], xy[key][1], xy[key][2], xy[key][3]);
                    exessive.Add(A);

                    System.Data.DataRow thisrow = dtable.NewRow();
                    thisrow["GXID"] = xid;
                    thisrow["GYID"] = yid;
                    thisrow["GZID"] = 0;
                    thisrow["type"] = (short)DefectType.Excessive;
                    thisrow["CI"] = dic[key][0].ci;
                    thisrow["PCI"] = dic[key][0].pci;
                    thisrow["ReceivedPowerdbm"] = dic[key][0].p;
                    thisrow["explain"] = string.Format("{0}:{1};{2}:{3}", dic[key][0].ci, dic[key][0].p, dic[key][1].ci, dic[key][1].p); ;
                    dtable.Rows.Add(thisrow);
                }

                // 当前栅格接收到了 2 个信号
                if (dic[key].Count == 2)
                {
                    // pci mod3 对打
                    if (dic[key][0].p > effective && dic[key][1].p > effective && dic[key][0].pci % 3 == dic[key][1].pci % 3)
                    {
                        ++pcim3;

                        System.Data.DataRow thisrow = dtable.NewRow();
                        thisrow["GXID"] = xid;
                        thisrow["GYID"] = yid;
                        thisrow["GZID"] = 0;
                        thisrow["type"] = (short)DefectType.PCImod3;
                        thisrow["CI"] = dic[key][0].ci;
                        thisrow["PCI"] = dic[key][0].pci;
                        thisrow["ReceivedPowerdbm"] = dic[key][0].p;
                        thisrow["explain"] = string.Format("{0}:{1},{2};{3}:{4},{5}", dic[key][0].ci, dic[key][0].pci, 
                            dic[key][0].p, dic[key][1].ci, dic[key][1].pci, dic[key][1].p); 
                        dtable.Rows.Add(thisrow);
                    }

                    // pci 冲突
                    if (dic[key][0].p > effective && dic[key][1].p > effective && dic[key][0].pci == dic[key][1].pci)
                    {
                        ++pcic;

                        System.Data.DataRow thisrow = dtable.NewRow();
                        thisrow["GXID"] = xid;
                        thisrow["GYID"] = yid;
                        thisrow["GZID"] = 0;
                        thisrow["type"] = (short)DefectType.PCIconflict;
                        thisrow["CI"] = dic[key][0].ci;
                        thisrow["PCI"] = dic[key][0].pci;
                        thisrow["ReceivedPowerdbm"] = dic[key][0].p;
                        thisrow["explain"] = string.Format("{0}:{1},{2};{3}:{4},{5}", dic[key][0].ci, dic[key][0].pci, 
                            dic[key][0].p, dic[key][1].ci, dic[key][1].pci, dic[key][1].p); 
                        dtable.Rows.Add(thisrow);
                    }
                }

                else if (dic[key].Count > 2)  // 当前栅格接收到了>2个信号
                {
                    // 重叠覆盖
                    if (dic[key][0].p > effective && dic[key][1].p > effective && dic[key][2].p > effective
                       && Math.Abs(dic[key][0].p - dic[key][1].p) < T && Math.Abs(dic[key][0].p - dic[key][2].p) < T
                       && Math.Abs(dic[key][1].p - dic[key][2].p) < T)
                    {
                        Analysis A = new Analysis(xid, yid, xy[key][0], xy[key][1], xy[key][2], xy[key][3]);
                        overlap.Add(A);

                        System.Data.DataRow thisrow = dtable.NewRow();
                        thisrow["GXID"] = xid;
                        thisrow["GYID"] = yid;
                        thisrow["GZID"] = 0;
                        thisrow["type"] = (short)DefectType.Overlapped;
                        thisrow["CI"] = dic[key][0].ci;
                        thisrow["PCI"] = dic[key][0].pci;
                        thisrow["ReceivedPowerdbm"] = dic[key][0].p;
                        thisrow["explain"] = string.Format("{0}:{1};{2}:{3};{4}:{5}", dic[key][0].ci, dic[key][0].p, dic[key][1].ci, dic[key][1].p, dic[key][2].ci, dic[key][2].p);
                        dtable.Rows.Add(thisrow);
                    }

                    // pci mod3 对打
                    if (dic[key][0].p > effective && dic[key][1].p > effective && dic[key][2].p > effective &&
                        (dic[key][0].pci % 3 == dic[key][1].pci % 3 || dic[key][0].pci % 3 == dic[key][2].pci % 3))
                    {
                        ++pcim3;

                        System.Data.DataRow thisrow = dtable.NewRow();
                        thisrow["GXID"] = xid;
                        thisrow["GYID"] = yid;
                        thisrow["GZID"] = 0;
                        thisrow["type"] = (short)DefectType.PCImod3;
                        thisrow["CI"] = dic[key][0].ci;
                        thisrow["PCI"] = dic[key][0].pci;
                        thisrow["ReceivedPowerdbm"] = dic[key][0].p;
                        thisrow["explain"] = string.Format("{0}:{1},{2};{3}:{4},{5};{6}:{7},{8}", dic[key][0].ci, 
                            dic[key][0].pci, dic[key][0].p, dic[key][1].ci, dic[key][1].pci, dic[key][1].p,
                            dic[key][2].ci, dic[key][2].pci, dic[key][2].p); 
                        dtable.Rows.Add(thisrow);
                    }

                    // pci 冲突
                    if (dic[key][0].p > effective && dic[key][1].p > effective && dic[key][2].p > effective && 
                        (dic[key][0].pci == dic[key][1].pci || dic[key][0].pci == dic[key][2].pci))
                    {
                        ++pcic;

                        System.Data.DataRow thisrow = dtable.NewRow();
                        thisrow["GXID"] = xid;
                        thisrow["GYID"] = yid;
                        thisrow["GZID"] = 0;
                        thisrow["type"] = (short)DefectType.PCIconflict;
                        thisrow["CI"] = dic[key][0].ci;
                        thisrow["PCI"] = dic[key][0].pci;
                        thisrow["ReceivedPowerdbm"] = dic[key][0].p;
                        thisrow["explain"] = string.Format("{0}:{1},{2};{3}:{4},{5};{6}:{7},{8}", dic[key][0].ci, 
                            dic[key][0].pci, dic[key][0].p, dic[key][1].ci, dic[key][1].pci, dic[key][1].p,
                            dic[key][2].ci, dic[key][2].pci, dic[key][2].p);
                        dtable.Rows.Add(thisrow);
                    }

                    // pci 混淆
                    if (dic[key][0].p > effective && dic[key][1].p > effective && dic[key][2].p > effective
                         && dic[key][1].pci  == dic[key][2].pci)
                    {
                        ++pcih;

                        System.Data.DataRow thisrow = dtable.NewRow();
                        thisrow["GXID"] = xid;
                        thisrow["GYID"] = yid;
                        thisrow["GZID"] = 0;
                        thisrow["type"] = (short)DefectType.PCIconfusion;
                        thisrow["CI"] = dic[key][0].ci;
                        thisrow["PCI"] = dic[key][0].pci;
                        thisrow["ReceivedPowerdbm"] = dic[key][0].p;
                        thisrow["explain"] = string.Format("{0}:{1},{2};{3}:{4},{5};{6}:{7},{8}", dic[key][0].ci, 
                            dic[key][0].pci, dic[key][0].p, dic[key][1].ci, dic[key][1].pci, dic[key][1].p,
                            dic[key][2].ci, dic[key][2].pci, dic[key][2].p); 
                        dtable.Rows.Add(thisrow);
                    }
                }
            }

            #endregion
            //-----------------------------------------------

            #region  立体

            int weak1 = 0;  // 弱覆盖点数
            int pcim31 = 0;  // pci mod3 对打
            int pcih1 = 0;  // pci 混淆数
            int pcic1 = 0;  // pci 冲突数
            List<Analysis> overlap1 = new List<Analysis>();  // 重叠覆盖
            List<Analysis> exessive1 = new List<Analysis>(); // 过覆盖

            tb = IbatisHelper.ExecuteQueryForDataTable("CoverAnalysis3D", ht);
            n = tb.Rows.Count;
            double h = GridHelper.getInstance().getGHeight();

            Dictionary<string, List<Sub>> dic1 = new Dictionary<string, List<Sub>>();
            Dictionary<string, List<double>> xy1 = new Dictionary<string, List<double>>();

            
            for (int i = 0; i < n; i++)
            {
                int x = Convert.ToInt32(tb.Rows[i]["GXID"].ToString());
                int y = Convert.ToInt32(tb.Rows[i]["GYID"].ToString());
                int z = Convert.ToInt32(tb.Rows[i]["level"].ToString());
                double minX = Convert.ToDouble(tb.Rows[i]["minX"].ToString());
                double minY = Convert.ToDouble(tb.Rows[i]["minY"].ToString());
                double maxX = Convert.ToDouble(tb.Rows[i]["maxX"].ToString());
                double maxY = Convert.ToDouble(tb.Rows[i]["maxY"].ToString());
                double p = Convert.ToDouble(tb.Rows[i]["ReceivedPowerdbm"].ToString());
                int ci = Convert.ToInt32(tb.Rows[i]["ci"].ToString());
                int pci = Convert.ToInt32(tb.Rows[i]["pci"].ToString());

                string key = string.Format("{0},{1},{2}", x, y, z);

                if (!dic1.Keys.Contains(key))
                {
                    xy1[key] = new List<double>();
                    xy1[key].Add(minX);
                    xy1[key].Add(minY);
                    xy1[key].Add(maxX);
                    xy1[key].Add(maxY);
                    xy1[key].Add(z * h);
                }

                if (dic1.Keys.Contains(key))
                {
                    dic1[key].Add(new Sub(p, ci, pci));
                }
                else
                {
                    dic1[key] = new List<Sub>();
                    dic1[key].Add(new Sub(p, ci, pci));
                }
            }

            foreach (string key in dic1.Keys)
            {
                dic1[key].Sort(new SubCompare());
                int m = dic1[key].Count - 1;

                string[] id = key.Split(',');
                int xid = Convert.ToInt32(id[0]);
                int yid = Convert.ToInt32(id[1]);
                int zid = Convert.ToInt32(id[2]);

                if (dic1[key][0].p < -95) // && dic1[key][m] > -110)
                {
                    ++weak1;

                    System.Data.DataRow thisrow = dtable.NewRow();
                    thisrow["GXID"] = xid;
                    thisrow["GYID"] = yid;
                    thisrow["GZID"] = zid;
                    thisrow["type"] = (short)DefectType.Weak;
                    thisrow["CI"] = dic1[key][0].ci;
                    thisrow["PCI"] = dic1[key][0].pci;
                    thisrow["ReceivedPowerdbm"] = dic1[key][0].p;
                    thisrow["explain"] = "";
                    dtable.Rows.Add(thisrow);
                }

                if (dic1[key][0].p < -110)
                    continue;

                // 当前栅格只接收到一个信号，不存在pci模3对打、过覆盖、重叠覆盖、PCI冲突、PCI混淆
                if (dic1[key].Count < 2)
                    continue;

                // 过覆盖
                if (dic1[key][0].p > effective && dic1[key][1].p > effective && Math.Abs(dic1[key][0].p - dic1[key][1].p) < T1)
                {
                    Analysis A = new Analysis(xid, yid, zid, xy1[key][0], xy1[key][1], xy1[key][2], xy1[key][3], zid * h);
                    exessive1.Add(A);

                    System.Data.DataRow thisrow = dtable.NewRow();
                    thisrow["GXID"] = xid;
                    thisrow["GYID"] = yid;
                    thisrow["GZID"] = zid;
                    thisrow["type"] = (short)DefectType.Excessive;
                    thisrow["CI"] = dic1[key][0].ci;
                    thisrow["PCI"] = dic1[key][0].pci;
                    thisrow["ReceivedPowerdbm"] = dic1[key][0].p;
                    thisrow["explain"] = string.Format("{0}:{1};{2}:{3}", dic1[key][0].ci, dic1[key][0].p, dic1[key][1].ci, dic1[key][1].p); ;
                    dtable.Rows.Add(thisrow);
                }

                // 当前栅格接收到了 2 个信号
                if (dic1[key].Count == 2)
                {
                    // pci mod3 对打
                    if (dic1[key][0].p > effective && dic1[key][1].p > effective && dic1[key][0].pci % 3 == dic1[key][1].pci % 3)
                    {
                        ++pcim31;

                        System.Data.DataRow thisrow = dtable.NewRow();
                        thisrow["GXID"] = xid;
                        thisrow["GYID"] = yid;
                        thisrow["GZID"] = zid;
                        thisrow["type"] = (short)DefectType.PCImod3;
                        thisrow["CI"] = dic1[key][0].ci;
                        thisrow["PCI"] = dic1[key][0].pci;
                        thisrow["ReceivedPowerdbm"] = dic1[key][0].p;
                        thisrow["explain"] = string.Format("{0}:{1},{2};{3}:{4},{5}", dic1[key][0].ci, dic1[key][0].pci,
                            dic1[key][0].p, dic1[key][1].ci, dic1[key][1].pci, dic1[key][1].p);
                        dtable.Rows.Add(thisrow);
                    }

                    // pci 冲突
                    if (dic1[key][0].p > effective && dic1[key][1].p > effective && dic1[key][0].pci == dic1[key][1].pci)
                    {
                        ++pcic1;

                        System.Data.DataRow thisrow = dtable.NewRow();
                        thisrow["GXID"] = xid;
                        thisrow["GYID"] = yid;
                        thisrow["GZID"] = zid;
                        thisrow["type"] = (short)DefectType.PCIconflict;
                        thisrow["CI"] = dic1[key][0].ci;
                        thisrow["PCI"] = dic1[key][0].pci;
                        thisrow["ReceivedPowerdbm"] = dic1[key][0].p;
                        thisrow["explain"] = string.Format("{0}:{1},{2};{3}:{4},{5}", dic1[key][0].ci, dic1[key][0].pci,
                            dic1[key][0].p, dic1[key][1].ci, dic1[key][1].pci, dic1[key][1].p);
                        dtable.Rows.Add(thisrow);
                    }
                }

                else if (dic1[key].Count > 2)  // 当前栅格接收到了>2个信号
                {
                    // 重叠覆盖
                    if (dic1[key][0].p > effective && dic1[key][1].p > effective && dic1[key][2].p > effective
                       && Math.Abs(dic1[key][0].p - dic1[key][1].p) < T && Math.Abs(dic1[key][0].p - dic1[key][2].p) < T
                       && Math.Abs(dic1[key][1].p - dic1[key][2].p) < T)
                    {
                        Analysis A = new Analysis(xid, yid, zid, xy1[key][0], xy1[key][1], xy1[key][2], xy1[key][3], zid * h);
                        overlap1.Add(A);

                        System.Data.DataRow thisrow = dtable.NewRow();
                        thisrow["GXID"] = xid;
                        thisrow["GYID"] = yid;
                        thisrow["GZID"] = zid;
                        thisrow["type"] = (short)DefectType.Overlapped;
                        thisrow["CI"] = dic1[key][0].ci;
                        thisrow["PCI"] = dic1[key][0].pci;
                        thisrow["ReceivedPowerdbm"] = dic1[key][0].p;
                        thisrow["explain"] = string.Format("{0}:{1};{2}:{3};{4}:{5}", dic1[key][0].ci, dic1[key][0].p, dic1[key][1].ci, dic1[key][1].p, dic1[key][2].ci, dic1[key][2].p);
                        dtable.Rows.Add(thisrow);
                    }

                    // pci mod3 对打
                    if (dic1[key][0].p > effective && dic1[key][1].p > effective && dic1[key][2].p > effective &&
                        (dic1[key][0].pci % 3 == dic1[key][1].pci % 3 || dic1[key][0].pci % 3 == dic1[key][2].pci % 3))
                    {
                        ++pcim31;

                        System.Data.DataRow thisrow = dtable.NewRow();
                        thisrow["GXID"] = xid;
                        thisrow["GYID"] = yid;
                        thisrow["GZID"] = zid;
                        thisrow["type"] = (short)DefectType.PCImod3;
                        thisrow["CI"] = dic1[key][0].ci;
                        thisrow["PCI"] = dic1[key][0].pci;
                        thisrow["ReceivedPowerdbm"] = dic1[key][0].p;
                        thisrow["explain"] = string.Format("{0}:{1},{2};{3}:{4},{5};{6}:{7},{8}", dic1[key][0].ci,
                            dic1[key][0].pci, dic1[key][0].p, dic1[key][1].ci, dic1[key][1].pci, dic1[key][1].p,
                            dic1[key][2].ci, dic1[key][2].pci, dic1[key][2].p);
                        dtable.Rows.Add(thisrow);
                    }

                    // pci 冲突
                    if (dic1[key][0].p > effective && dic1[key][1].p > effective && dic1[key][2].p > effective &&
                        (dic1[key][0].pci == dic1[key][1].pci || dic1[key][0].pci == dic1[key][2].pci))
                    {
                        ++pcic1;

                        System.Data.DataRow thisrow = dtable.NewRow();
                        thisrow["GXID"] = xid;
                        thisrow["GYID"] = yid;
                        thisrow["GZID"] = zid;
                        thisrow["type"] = (short)DefectType.PCIconflict;
                        thisrow["CI"] = dic1[key][0].ci;
                        thisrow["PCI"] = dic1[key][0].pci;
                        thisrow["ReceivedPowerdbm"] = dic1[key][0].p;
                        thisrow["explain"] = string.Format("{0}:{1},{2};{3}:{4},{5};{6}:{7},{8}", dic1[key][0].ci,
                            dic1[key][0].pci, dic1[key][0].p, dic1[key][1].ci, dic1[key][1].pci, dic1[key][1].p,
                            dic1[key][2].ci, dic1[key][2].pci, dic1[key][2].p);
                        dtable.Rows.Add(thisrow);
                    }

                    // pci 混淆
                    if (dic1[key][0].p > effective && dic1[key][1].p > effective && dic1[key][2].p > effective
                         && dic1[key][1].pci == dic1[key][2].pci)
                    {
                        ++pcih1;

                        System.Data.DataRow thisrow = dtable.NewRow();
                        thisrow["GXID"] = xid;
                        thisrow["GYID"] = yid;
                        thisrow["GZID"] = zid;
                        thisrow["type"] = (short)DefectType.PCIconfusion;
                        thisrow["CI"] = dic1[key][0].ci;
                        thisrow["PCI"] = dic1[key][0].pci;
                        thisrow["ReceivedPowerdbm"] = dic1[key][0].p;
                        thisrow["explain"] = string.Format("{0}:{1},{2};{3}:{4},{5};{6}:{7},{8}", dic1[key][0].ci,
                            dic1[key][0].pci, dic1[key][0].p, dic1[key][1].ci, dic1[key][1].pci, dic1[key][1].p,
                            dic1[key][2].ci, dic1[key][2].pci, dic1[key][2].p);
                        dtable.Rows.Add(thisrow);
                    }
                }
            }

            using (System.Data.SqlClient.SqlBulkCopy bcp = new System.Data.SqlClient.SqlBulkCopy(DataUtil.ConnectionString))
            {
                bcp.BatchSize = dtable.Rows.Count;
                bcp.BulkCopyTimeout = 1000;
                bcp.DestinationTableName = "tbDefect";
                bcp.WriteToServer(dtable);
                bcp.Close();
            }
            dtable.Clear();

            double total = dic.Count + dic1.Count;
            double totalG = dic.Count;
            double totalB = dic1.Count;

            string mess = string.Format(" 地面---总栅格数：{0}\n 弱覆盖点数：{1}, 占比：{2}\n 过覆盖点数：{3}, 占比：{4}\n 重叠覆盖点数：{5}, 占比：{6}\n PCI模3对打点数：{7}, 占比：{8}\n PCI冲突点数：{9}, 占比：{10}\n PCI混淆点数：{11}, 占比：{12}",
                                         dic.Count,
                                         weak, weak / totalG,
                                         exessive.Count, exessive.Count / totalG,
                                         overlap.Count, overlap.Count / totalG,
                                         pcim3, pcim3 / totalG,
                                         pcic, pcic/totalG,
                                         pcih, pcih/totalG);
            MessageBox.Show(mess);

            mess = string.Format(" 立体---总栅格数：{0}\n 弱覆盖点数：{1}, 占比：{2}\n 过覆盖点数：{3}, 占比：{4}\n 重叠覆盖点数：{5}, 占比：{6}\n PCI模3对打点数：{7}, 占比：{8}\n PCI冲突点数：{9}, 占比：{10}\n PCI混淆点数：{11}, 占比：{12}",
                                         dic1.Count,
                                         weak1, weak1 / totalB,
                                         exessive1.Count, exessive1.Count / totalB,
                                         overlap1.Count, overlap1.Count / totalB,
                                         pcim31, pcim31 / totalB,
                                         pcic1, pcic1 / totalB,
                                         pcih1, pcih1 / totalB);
            MessageBox.Show(mess);

            mess = string.Format(" 总计---总栅格数：{0}\n 弱覆盖点数：{1}, 占比：{2}\n 过覆盖点数：{3}, 占比：{4}\n 重叠覆盖点数：{5}, 占比：{6}\n PCI模3对打点数：{7}, 占比：{8}\n PCI冲突点数：{9}, 占比：{10}\n PCI混淆点数：{11}, 占比：{12}",
                                         total,
                                         weak+weak1, (weak+weak1) / total,
                                         (exessive.Count + exessive1.Count), (exessive.Count + exessive1.Count) / total,
                                         (overlap.Count + overlap1.Count), (overlap.Count + overlap1.Count) / total,
                                         (pcim3 + pcim31), (pcim3 + pcim31) / total,
                                         (pcic + pcic1), (pcic + pcic1) / total,
                                         (pcih + pcih1), (pcih + pcih1) / total);
            MessageBox.Show(mess);
            #endregion

        }

        private void AreaCoverDefect_Load(object sender, EventArgs e)
        {

        }

        private void button8_Click(object sender, EventArgs e)
        {
            int mingxid, maxgxid, mingyid, maxgyid;
            mingxid = maxgxid = mingyid = maxgyid = -1;
            if (!valideInput(ref mingxid, ref maxgxid, ref mingyid, ref maxgyid))
            {
                MessageBox.Show("边界网格输入有误！");
                return;
            }

            double minx = 0, miny = 0, maxx = 0, maxy = 0, z = 0;
            GridHelper.getInstance().GridToXYZ(mingxid, mingyid, 0, ref minx, ref miny, ref z);
            GridHelper.getInstance().GridToXYZ(maxgxid, maxgyid, 0, ref maxx, ref maxy, ref z);
            ESRI.ArcGIS.Geometry.IPoint pt = GeometryUtilities.ConstructPoint3D(minx, miny, 0);
            ESRI.ArcGIS.Geometry.IPoint pt1 = GeometryUtilities.ConstructPoint3D(maxx, maxy, 0);

            DrawUtilities.DrawRect(pt, pt1, 255, 0, 0);

            // 刷新图层
            ESRI.ArcGIS.Carto.IGraphicsLayer pLayer = (GISMapApplication.Instance.Scene as ESRI.ArcGIS.Carto.IBasicMap).BasicGraphicsLayer;
            GISMapApplication.Instance.RefreshLayer(pLayer);

            MessageBox.Show("完成！");
        }

        private void button9_Click(object sender, EventArgs e)
        {
            IbatisHelper.ExecuteDelete("DeleteDefect", null);
            MessageBox.Show("完成！");
        }

        void drawPoint(DefectType type, double size, ESRI.ArcGIS.Display.IColor color)
        {
            int mingxid, maxgxid, mingyid, maxgyid;
            mingxid = maxgxid = mingyid = maxgyid = -1;
            if (!valideInput(ref mingxid, ref maxgxid, ref mingyid, ref maxgyid))
            {
                MessageBox.Show("边界网格输入有误！");
                return;
            }

            Hashtable para = new Hashtable();
            para["minGXID"] = mingxid;
            para["maxGXID"] = maxgxid;
            para["minGYID"] = mingyid;
            para["maxGYID"] = maxgyid;
            para["type"] = (short)type;
            DataTable tb = new DataTable();
            tb = IbatisHelper.ExecuteQueryForDataTable("getDefect", para);

            ESRI.ArcGIS.Carto.IGraphicsLayer pLayer = (GISMapApplication.Instance.Scene as ESRI.ArcGIS.Carto.IBasicMap).BasicGraphicsLayer;
            ESRI.ArcGIS.Analyst3D.IGraphicsContainer3D pGC = pLayer as ESRI.ArcGIS.Analyst3D.IGraphicsContainer3D;

            for (int i = 0; i < tb.Rows.Count; i++)
            {
                double x = Convert.ToDouble(tb.Rows[i]["CX"].ToString());
                double y = Convert.ToDouble(tb.Rows[i]["CX"].ToString());
                DrawUtilities.DrawPoint(pGC, GeometryUtilities.ConstructPoint3D(x, y, 0));
            }

            GISMapApplication.Instance.RefreshLayer(pLayer);
            MessageBox.Show("完成");
        }

        void opLayer(string layerName, short type)
        {
            int mingxid, maxgxid, mingyid, maxgyid;
            mingxid = maxgxid = mingyid = maxgyid = -1;
            if (!valideInput(ref mingxid, ref maxgxid, ref mingyid, ref maxgyid))
            {
                MessageBox.Show("边界网格输入有误！");
                return;
            }

            OperateDefectLayer operateGrid3d = new OperateDefectLayer(layerName);
            operateGrid3d.ClearLayer();
            operateGrid3d.constuctGrid3Ds(mingxid, maxgxid, mingyid, maxgyid, type);
            MessageBox.Show("已呈现！");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            opLayer(LayerNames.Weak, (short)DefectType.Weak);
            //drawPoint(DefectType.Weak, 20, ColorUtilities.GetColor(0, 0, 255));
        }

        private void button3_Click(object sender, EventArgs e)
        {
            opLayer(LayerNames.Excessive, (short)DefectType.Excessive);
            //drawPoint(DefectType.Excessive, 20, ColorUtilities.GetColor(255, 0, 0));
        }

        private void button4_Click(object sender, EventArgs e)
        {
            opLayer(LayerNames.Overlapped, (short)DefectType.Overlapped);
            //drawPoint(DefectType.Overlapped, 20, ColorUtilities.GetColor(0, 255, 0));
        }
        
        private void button7_Click(object sender, EventArgs e)
        {
            opLayer(LayerNames.PCImod3, (short)DefectType.PCImod3);
            //drawPoint(DefectType.PCImod3, 20, ColorUtilities.GetColor(0, 255, 255));
        }

        private void button5_Click(object sender, EventArgs e)
        {
            opLayer(LayerNames.PCIconfusion, (short)DefectType.PCIconfusion);
            //drawPoint(DefectType.PCIconfusion, 20, ColorUtilities.GetColor(255, 0, 255));
        }

        private void button6_Click(object sender, EventArgs e)
        {
            opLayer(LayerNames.PCIconflict, (short)DefectType.PCIconflict);
            //drawPoint(DefectType.PCIconflict, 20, ColorUtilities.GetColor(255, 255, 0));
        }

        // 清空默认图层
        private void button10_Click(object sender, EventArgs e)
        {
            ESRI.ArcGIS.Carto.IGraphicsLayer pLayer = (GISMapApplication.Instance.Scene as ESRI.ArcGIS.Carto.IBasicMap).BasicGraphicsLayer;
            ESRI.ArcGIS.Analyst3D.IGraphicsContainer3D pGC = pLayer as ESRI.ArcGIS.Analyst3D.IGraphicsContainer3D;
            pGC.DeleteAllElements();
            GISMapApplication.Instance.RefreshLayer(pLayer);
        }
    }

    class Sub
    {
        public double p;  // 功率
        public int ci;    // 小区标识 CI
        public int pci;   // 小区物理标识 PCI

        public Sub(double p1, int ci1, int pci1)
        {
            p = p1;
            ci = ci1;
            pci = pci1;
        }
    };

    class SubCompare : IComparer<Sub>
    {
        public int Compare(Sub a, Sub b)
        {
            return b.p.CompareTo(a.p);
        }
    }

    class Analysis
    {
        public int xid, yid, zid;   
        double minX, minY, maxX, maxY, h;

        public Analysis(int x, int y, double minx, double miny, double maxx, double maxy)
        {
            xid = x;
            yid = y;
            minX = minx;
            minY = miny;
            maxX = maxx;
            maxY = maxy;
        }

        public Analysis(int x, int y, int z, double minx, double miny, double maxx, double maxy, double h1)
        {
            xid = x;
            yid = y;
            zid = z;
            minX = minx;
            minY = miny;
            maxX = maxx;
            maxY = maxy;
            h = h1;
        }
    };

    
}
