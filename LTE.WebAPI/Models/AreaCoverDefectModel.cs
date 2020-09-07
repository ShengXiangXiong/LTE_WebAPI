using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections;
using LTE.InternalInterference.Grid;
using LTE.DB;
using System.Data.SqlClient;
using System.Data;
using LTE.Model;

namespace LTE.WebAPI.Models
{
    // 网内干扰分析
    public class AreaCoverDefectModel
    {
        /// <summary>
        /// 最小经度
        /// </summary>
        public double minLongitude { get; set; }   
 
        /// <summary>
        /// 最小纬度
        /// </summary>
        public double minLatitude { get; set; }    

        /// <summary>
        /// 最大经度
        /// </summary>
        public double maxLongitude { get; set; }  

        /// <summary>
        /// 最大纬度
        /// </summary>
        public double maxLatitude { get; set; }    

        enum DefectType
        {
            Weak,         // 弱覆盖点
            Excessive,    // 过覆盖点
            Overlapped,   // 重叠覆盖点
            PCIconflict,  // PCI 冲突点
            PCIconfusion, // PCI 混淆
            PCImod3       // PCI 模 3 冲突点
        };

        /// <summary>
        /// 网内干扰分析
        /// </summary>
        /// <returns></returns>
        public Result defectAnalysis()
        {
            // 经纬度转换为栅格ID
            LTE.Geometric.Point pMin = new Geometric.Point();
            pMin.X = this.minLongitude;
            pMin.Y = this.minLatitude;
            pMin.Z = 0;
            pMin = LTE.Utils.PointConvertByProj.Instance.GetProjectPoint(pMin);

            LTE.Geometric.Point pMax = new Geometric.Point();
            pMax.X = this.maxLongitude;
            pMax.Y = this.maxLatitude;
            pMax.Z = 0;
            pMax = LTE.Utils.PointConvertByProj.Instance.GetProjectPoint(pMax);

            int minxid = 0, minyid = 0, maxxid = 0, maxyid = 0;
            GridHelper.getInstance().XYToGGrid(pMin.X, pMin.Y, ref minxid, ref minyid);
            GridHelper.getInstance().XYToGGrid(pMax.X, pMax.Y, ref maxxid, ref maxyid);
            //GridHelper.getInstance().LngLatToGGrid(pMin.X, pMin.Y, ref minxid, ref minyid);
            //GridHelper.getInstance().LngLatToGGrid(pMax.X, pMax.Y, ref maxxid, ref maxyid);

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

            DataTable tb1 = IbatisHelper.ExecuteQueryForDataTable("CoverAnalysis", ht);
            DataTable tb2 = IbatisHelper.ExecuteQueryForDataTable("CoverAnalysis3D", ht);
            int cnt = tb1.Rows.Count + tb2.Rows.Count;
            //if (cnt < 1)
            //{
            //    return new Result(false, "当前区域未进行覆盖计算");
            //}
            if (cnt < 0.85 * (maxxid - minxid + 1) * (maxyid - minyid + 1))
            {
                return new Result(false, "当前区域已计算覆盖率过小，请对此区域重新计算小区覆盖");
            }

            #region 地面初始化
            DataTable tb = tb1;
            int n = tb.Rows.Count;
            //if (n < 1)
            //    return new Result(false, "当前区域未进行覆盖计算");

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

            #endregion

            #region 立体初始化
            int weak1 = 0;  // 弱覆盖点数
            int pcim31 = 0;  // pci mod3 对打
            int pcih1 = 0;  // pci 混淆数
            int pcic1 = 0;  // pci 冲突数
            List<Analysis> overlap1 = new List<Analysis>();  // 重叠覆盖
            List<Analysis> exessive1 = new List<Analysis>(); // 过覆盖

            tb = tb2;
            n = tb.Rows.Count;
            //if (n < 1)
            //    return new Result(false, "当前区域未进行覆盖计算");

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
            #endregion

            LoadInfo loadInfo = new LoadInfo();
            int count = dic.Keys.Count + dic1.Keys.Count;
            int updateSize = (int)Math.Round(count * 0.02);
            loadInfo.loadCountAdd(count);
            int cnt1 = 0;
            #region 地面
            foreach (string key in dic.Keys)
            {
                if (updateSize==++cnt1)
                {
                    loadInfo.loadHashAdd(updateSize);
                    cnt1 = 0;
                }
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
                         && dic[key][1].pci == dic[key][2].pci)
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

            foreach (string key in dic1.Keys)
            {
                if (updateSize == ++cnt1)
                {
                    loadInfo.loadHashAdd(updateSize);
                    cnt1 = 0;
                }
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

            string mess = string.Format(" 地面---总栅格数：{0}\n 弱覆盖点数：{1}, 占比：{2}\n 过覆盖点数：{3}, 占比：{4}\n 重叠覆盖点数：{5}, 占比：{6}\n PCI模3对打点数：{7}, 占比：{8}\n PCI冲突点数：{9}, 占比：{10}\n PCI混淆点数：{11}, 占比：{12}\n",
                                         dic.Count,
                                         weak, weak / totalG,
                                         exessive.Count, exessive.Count / totalG,
                                         overlap.Count, overlap.Count / totalG,
                                         pcim3, pcim3 / totalG,
                                         pcic, pcic / totalG,
                                         pcih, pcih / totalG);

            string mess3D = string.Format(" 立体---总栅格数：{0}\n 弱覆盖点数：{1}, 占比：{2}\n 过覆盖点数：{3}, 占比：{4}\n 重叠覆盖点数：{5}, 占比：{6}\n PCI模3对打点数：{7}, 占比：{8}\n PCI冲突点数：{9}, 占比：{10}\n PCI混淆点数：{11}, 占比：{12}\n",
                                         dic1.Count,
                                         weak1, weak1 / totalB,
                                         exessive1.Count, exessive1.Count / totalB,
                                         overlap1.Count, overlap1.Count / totalB,
                                         pcim31, pcim31 / totalB,
                                         pcic1, pcic1 / totalB,
                                         pcih1, pcih1 / totalB);

            string messAll = string.Format(" 总计---总栅格数：{0}\n 弱覆盖点数：{1}, 占比：{2}\n 过覆盖点数：{3}, 占比：{4}\n 重叠覆盖点数：{5}, 占比：{6}\n PCI模3对打点数：{7}, 占比：{8}\n PCI冲突点数：{9}, 占比：{10}\n PCI混淆点数：{11}, 占比：{12}\n",
                                         total,
                                         weak + weak1, (weak + weak1) / total,
                                         (exessive.Count + exessive1.Count), (exessive.Count + exessive1.Count) / total,
                                         (overlap.Count + overlap1.Count), (overlap.Count + overlap1.Count) / total,
                                         (pcim3 + pcim31), (pcim3 + pcim31) / total,
                                         (pcic + pcic1), (pcic + pcic1) / total,
                                         (pcih + pcih1), (pcih + pcih1) / total);
            return new Result(true, mess + mess3D + messAll);
            #endregion

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