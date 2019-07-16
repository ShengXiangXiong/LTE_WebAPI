using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LTE.DB;
using System.Data;
using System.Data.SqlClient;
using LTE.InternalInterference.Grid;
using ESRI.ArcGIS.Geometry;
using LTE.GIS;
using System.Collections;
using LTE.InternalInterference;

namespace LTE.WebAPI.Models
{
    // 网外干扰定位，基于启发式规则
    public class InterferenceLocateModel
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

        // 确定干扰区域
        static double leftBound = 666918, rightBound = 670080, downBound = 3542960, upBound = 3548826; // 南京 第一次压缩
        private void initRange()
        {
            #region 经纬度 转 xy
            IPoint pt = GeometryUtilities.ConstructPoint3D(this.minLongitude, this.minLatitude, 0);
            IPoint pt1 = GeometryUtilities.ConstructPoint3D(this.maxLongitude, this.maxLatitude, 0);
            pt = PointConvert.Instance.GetProjectPoint(pt);
            pt1 = PointConvert.Instance.GetProjectPoint(pt1);
            #endregion

            leftBound = pt.X;
            downBound = pt.Y;
            rightBound = pt1.X;
            upBound = pt1.Y;
        }

        static bool first = true;  // 第一次压缩干扰源区域

        // 基于启发式规则压缩干扰区域
        public Result rules()
        {
            System.Text.StringBuilder msg = new System.Text.StringBuilder();
            if (first) // 第一次压缩干扰源区域
            {
                initRange();
                msg.Append(string.Format("初始干扰区域范围：{0}m * {1}m\n", Math.Round(rightBound - leftBound, 0), Math.Round(upBound - downBound, 0)));
            }

            Hashtable ht = new Hashtable();
            ht["RoadID1"] = 0;
            ht["RoadID2"] = 6;

            readData("getDT", ht);  // 读取路测
            strongWeakPt();     // 强弱相间点、强点用到
            strongWeakPt2();    // 射线跟踪用到

            DateTime t0 = DateTime.Now;

            #region 强点
            strongLoc();
            #endregion

            DateTime t1 = DateTime.Now;

            #region 单调性
            if (!first)  // 只利用新区域内的路测
            {
                ht["RoadID1"] = 7;
                ht["RoadID2"] = 10;
                readData("getDT", ht);
            }
            monotoneLoc();
            #endregion

            DateTime t2 = DateTime.Now;

            #region 强弱
            if (!first)  // 只利用新区域内的路测
                strongWeakPt();

            strongWeakLoc();
            #endregion

            DateTime t3 = DateTime.Now;

            first = false;   // 准备第二次压缩

            #region 压缩后的区域
            double left = double.MaxValue, down = double.MaxValue, right = 0, up = 0;
            newBound(ref StrongResult, ref left, ref down, ref right, ref up);
            newBound(ref MonotoneResult, ref left, ref down, ref right, ref up);
            newBound(ref StrongWeakResult, ref left, ref down, ref right, ref up);

            if (left > leftBound)
                leftBound = left;
            if (right < rightBound)
                rightBound = right;
            if (down > downBound)
                downBound = down;
            if (up < upBound)
                upBound = up;

            msg.Append(string.Format("新的干扰区域范围：{0}m * {1}m\n", Math.Round(rightBound - leftBound, 0),
                                                                        Math.Round(upBound - downBound, 0)));
            msg.Append(string.Format("\n路测单调性定位所用时间：{0} s\n", (t1 - t0).TotalMilliseconds / 1000));
            msg.Append("路测单调性干扰源定位结果：\n");
            addResult(ref msg, ref MonotoneResult);

            msg.Append(string.Format("\n\n路测相邻强弱信号点对定位所用时间：{0} s\n", (t2 - t1).TotalMilliseconds / 1000));
            msg.Append("路测相邻强弱信号点对定位结果：\n");
            addResult(ref msg, ref StrongWeakResult);

            msg.Append(string.Format("\n\n路测强信号点定位所用时间：{0} s\n", (t3 - t2).TotalMilliseconds / 1000));
            msg.Append("路测强信号点定位结果：\n");
            addResult(ref msg, ref StrongResult);

            MonotoneResult.Clear();
            StrongWeakResult.Clear();
            StrongResult.Clear();

            IbatisHelper.ExecuteDelete("DeleteInfArea", null);

            // 写入数据库
            System.Data.DataTable dtable = new System.Data.DataTable();
            dtable.Columns.Add("leftBound");
            dtable.Columns.Add("downBound");
            dtable.Columns.Add("rightBound");
            dtable.Columns.Add("upBound");
            System.Data.DataRow thisrow = dtable.NewRow();
            thisrow["leftBound"] = Math.Round(leftBound, 3);
            thisrow["downBound"] = Math.Round(downBound, 3);
            thisrow["rightBound"] = Math.Round(rightBound, 3);
            thisrow["upBound"] = Math.Round(upBound, 3);
            dtable.Rows.Add(thisrow);
            using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
            {
                bcp.BatchSize = dtable.Rows.Count;
                bcp.BulkCopyTimeout = 1000;
                bcp.DestinationTableName = "tbInfArea";
                bcp.WriteToServer(dtable);
                bcp.Close();
            }
            dtable.Clear();
            #endregion
            return new Result(true, msg.ToString());
        }

        // 路测读取
        double centX = 0, centY = 0;  // 路测中心点
        List<int> roadID = new List<int>();
        List<int> roadDivide = new List<int>();  // 道路分割点下标
        List<double> longtitude = new List<double>();
        List<double> latitude = new List<double>();
        List<double> gx = new List<double>();
        List<double> gy = new List<double>();
        List<double> gxid = new List<double>();
        List<double> gyid = new List<double>();
        List<double> pwrDbm = new List<double>();
        List<double> y = new List<double>();
        int n = 0;
        private void readData(string tbName, Hashtable ht)
        {
            roadID.Clear();
            roadDivide.Clear();
            longtitude.Clear();
            latitude.Clear();
            gx.Clear();
            gy.Clear();
            gxid.Clear();
            gyid.Clear();
            pwrDbm.Clear();
            y.Clear();
            n = 0;

            DataTable tb = IbatisHelper.ExecuteQueryForDataTable(tbName, ht);
            n = tb.Rows.Count;

            roadID.Add(Convert.ToInt32(tb.Rows[0]["roadID"].ToString()));
            longtitude.Add(Convert.ToDouble(tb.Rows[0]["longtitude"].ToString()));
            latitude.Add(Convert.ToDouble(tb.Rows[0]["latitude"].ToString()));
            gx.Add(Convert.ToDouble(tb.Rows[0]["x"].ToString()));
            gy.Add(Convert.ToDouble(tb.Rows[0]["y"].ToString()));
            gxid.Add(Convert.ToInt32(tb.Rows[0]["gxid"].ToString()));
            gyid.Add(Convert.ToInt32(tb.Rows[0]["gyid"].ToString()));
            pwrDbm.Add(Convert.ToDouble(tb.Rows[0]["RecePowerDbm"].ToString()));

            centX += gx[0];
            centY += gy[0];

            int k = 1;
            for (int i = 1; i < n; i++)
            {
                int xid = Convert.ToInt32(tb.Rows[i]["gxid"].ToString());
                int yid = Convert.ToInt32(tb.Rows[i]["gyid"].ToString());
                if (xid == gxid[k - 1] && yid == gyid[k - 1])
                    continue;

                roadID.Add(Convert.ToInt32(tb.Rows[i]["roadID"].ToString()));
                longtitude.Add(Convert.ToDouble(tb.Rows[i]["longtitude"].ToString()));
                latitude.Add(Convert.ToDouble(tb.Rows[i]["latitude"].ToString()));
                gx.Add(Convert.ToDouble(tb.Rows[i]["x"].ToString()));
                gy.Add(Convert.ToDouble(tb.Rows[i]["y"].ToString()));
                gxid.Add(xid);
                gyid.Add(yid);

                centX += gx[k];
                centY += gy[k];

                if (gx[k] == gx[k - 1])
                    y.Add(1000000000);
                else
                    y.Add((gy[k] - gy[k - 1]) / (gx[k] - gx[k - 1]));
                pwrDbm.Add(Convert.ToDouble(tb.Rows[i]["RecePowerDbm"].ToString()));

                ++k;

            }
            centX /= k;
            centY /= n;

            #region 提取道路
            Divide divide1 = new Divide(50, 0.75, ref y);
            divide1.run();

            for (int i = 1; i < divide1.posV.Count; i++)
                if (divide1.posV[i] - divide1.posV[i - 1] > 5)
                    roadDivide.Add(divide1.posV[i - 1]);
            roadDivide.Add(k - 1);
            #endregion

            n = k;

        }

        List<StrongWeakPt> Pt = new List<StrongWeakPt>();

        // 强弱相间点、强点用到
        private void strongWeakPt()
        {
            Pt.Clear();
            for (int j = 1; j < n; j++)
            {
                if (gx[j] < leftBound || gx[j] > rightBound || gy[j] < downBound || gy[j] > upBound)
                    continue;
                if (gx[j - 1] < leftBound || gx[j - 1] > rightBound || gy[j - 1] < downBound || gy[j - 1] > upBound)
                    continue;

                if (pwrDbm[j] - pwrDbm[j - 1] > 15 && pwrDbm[j] - pwrDbm[j - 1] < 25 && (Math.Abs(gx[j - 1] - gx[j]) < 50 && Math.Abs(gy[j - 1] - gy[j]) < 50))
                {
                    // 拉大距离
                    int k = j;

                    LTE.Geometric.Point pt = new LTE.Geometric.Point(gx[k], gy[k], 0);
                    LTE.Geometric.Point pt1 = new LTE.Geometric.Point(gx[j - 1], gy[j - 1], 0);
                    StrongWeakPt p = new StrongWeakPt(ref pt1, ref pt, Math.Abs(pwrDbm[k] - pwrDbm[j - 1]));

                    p.strongPower = pwrDbm[j];
                    Pt.Add(p);
                }
                else if (pwrDbm[j - 1] - pwrDbm[j] > 15 && pwrDbm[j - 1] - pwrDbm[j] < 25 && (Math.Abs(gx[j - 1] - gx[j]) < 100 && Math.Abs(gy[j - 1] - gy[j]) < 100))
                {
                    int k = j;

                    LTE.Geometric.Point pt = new LTE.Geometric.Point(gx[j - 1], gy[j - 1], 0);
                    LTE.Geometric.Point pt1 = new LTE.Geometric.Point(gx[k], gy[k], 0);
                    StrongWeakPt p = new StrongWeakPt(ref pt1, ref pt, Math.Abs(pwrDbm[k] - pwrDbm[j - 1]));
                    p.strongPower = pwrDbm[j - 1];
                    Pt.Add(p);
                }
            }

        }

        // 射线跟踪用到
        static List<StrongWeakPt> Pt2 = new List<StrongWeakPt>();
        private void strongWeakPt2()
        {
            Pt2.Clear();
            double d = -49;
            while (Pt2.Count < 20)
            {
                for (int j = 1; j < n; j++)
                {
                    if (pwrDbm[j] > d)
                    {
                        // 拉大距离
                        int k = j;

                        LTE.Geometric.Point pt = new LTE.Geometric.Point(gx[k], gy[k], 0);
                        LTE.Geometric.Point pt1 = new LTE.Geometric.Point(gx[j - 1], gy[j - 1], 0);
                        StrongWeakPt p = new StrongWeakPt(ref pt1, ref pt, Math.Abs(pwrDbm[k] - pwrDbm[j - 1]));
                        p.strongPower = pwrDbm[j];
                        Pt2.Add(p);
                    }
                }
                d--;
            }
        }

        List<Pt3D> StrongResult = new List<Pt3D>();

        // 根据强信号点推理干扰源，多点定位
        private void strongLoc()
        {
            List<List<int>> id = getStrongPtMult();  // 获得多点
            MulPtLoc solve = new MulPtLoc((leftBound + rightBound) / 2, (downBound + upBound) / 2, 6);

            for (int i = 0; i < id.Count; i++)
            {
                List<List<double>> X = new List<List<double>>();  // 测量点位置
                List<double> P = new List<double>();   // 测量点功率

                for (int j = 0; j < id[i].Count; j++)
                {
                    int k = id[i][j];
                    X.Add(new List<double> { gx[k], gy[k] });
                    P.Add(pwrDbm[k]);
                }
                double[] loc = solve.solve(ref X, ref P);

                Pt3D pt = new Pt3D(loc[0], loc[1], 0);
                if (inBound(ref pt))
                {
                    StrongResult.Add(pt);
                }
            }
        }

        // 根据旋转直线法获得一对强信号点
        List<List<int>> getStrongPtMult()
        {
            List<List<int>> ptID = new List<List<int>>();

            int n = Pt.Count;
            if (n == 0)
                return ptID;

            double cx = 0, cy = 0;
            double pwrMax = -10000;
            for (int i = 0; i < n; i++)
            {
                cx += Pt[i].strong.X;
                cy += Pt[i].strong.Y;
                if (Pt[i].strongPower > pwrMax)
                    pwrMax = Pt[i].strongPower;
            }
            cx /= n;
            cy /= n;

            int[] id = new int[1000];

            int id1 = 0, id2 = 0;
            int k = 0;
            HashSet<int> hs = new HashSet<int>();

            float deltaA = 25;
            int cnt = (int)(180.0 / deltaA) - 1;
            double startA = 0;
            bool ok = farthest(ref id1, ref id2, 0, cx, cy, 2, ref hs);
            if (ok)
            {
                id[k++] = id1; id[k++] = id2;
                hs.Add(id1); hs.Add(id2);
            }

            for (int i = 0; i < cnt; i++)
            {
                startA += deltaA;
                double K = Math.Tan(startA / 180.0 * Math.PI);
                ok = farthest(ref id1, ref id2, K, cx, cy, 3, ref hs);
                if (ok)
                {
                    id[k++] = id1; id[k++] = id2;
                    hs.Add(id1); hs.Add(id2);
                }
                if (k > 1000)
                    break;
            }

            for (int i = 0; i < k; i += 6)
            {
                List<int> ptid = new List<int>();
                for (int j = i; j < i + 6; j++)
                    ptid.Add(id[i * 6 + j]);
                ptID.Add(ptid);
            }
            return ptID;
        }

        // 位于直线 y - cy = k(x - cx) 附近的最远点对
        // flag = 1: x = cx   平行于y轴
        // flag = 2: y = cy   平行于x轴
        // flag = 3: 其他直线
        bool farthest(ref int id1, ref int id2, double k, double cx, double cy, int flag, ref HashSet<int> hs)
        {
            double thld = 100;
            List<int> id = new List<int>();
            if (flag == 1)
            {
                for (int i = 0; i < Pt.Count; i++)
                {
                    if (hs.Contains(i))
                        continue;
                    if (Math.Abs(Pt[i].strong.X - cx) < thld)
                        id.Add(i);
                }
            }
            else if (flag == 2)
            {
                for (int i = 0; i < Pt.Count; i++)
                {
                    if (hs.Contains(i))
                        continue;
                    if (Math.Abs(Pt[i].strong.Y - cy) < thld)
                        id.Add(i);
                }
            }
            else
            {
                double den = Math.Sqrt(k * k + 1);
                for (int i = 0; i < Pt.Count; i++)
                {
                    if (hs.Contains(i))
                        continue;
                    if (Math.Abs(k * Pt[i].strong.X - Pt[i].strong.Y - k * cx + cy) / den < thld)
                        id.Add(i);
                }
            }

            int n = id.Count;
            if (n < 2)
                return false;

            List<TmpP> list = new List<TmpP>();
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    // 如果不是位于中心的两侧
                    if ((Pt[id[i]].strong.X < cx && Pt[id[j]].strong.X < cx) || (Pt[id[i]].strong.X > cx && Pt[id[j]].strong.X > cx))
                        continue;

                    // 点到直线的距离
                    list.Add(new TmpP(id[i], id[j], Math.Sqrt(Math.Pow(Pt[id[i]].strong.X - Pt[id[j]].strong.X, 2) + Math.Pow(Pt[id[i]].strong.Y - Pt[id[j]].strong.Y, 2))));
                }
            }
            list.Sort(new TmpPCompare());

            if (list.Count == 0)
                return false;

            id1 = list[0].id1;
            id2 = list[0].id2;
            return true;
        }

        List<Pt3D> MonotoneResult = new List<Pt3D>();

        // 根据单调性对栅格打分
        private Result monotoneLoc()
        {

            int agMaxX = 0, agMaxY = 0, agMinX = 0, agMinY = 0, agZ = 0;
            GridHelper.getInstance().XYZToAccGrid(leftBound, downBound, 0, ref agMinX, ref agMinY, ref agZ);
            GridHelper.getInstance().XYZToAccGrid(rightBound, upBound, 0, ref agMaxX, ref agMaxY, ref agZ);

            Dictionary<string, int> dicMon = new Dictionary<string, int>();
            Regress regress1 = new Regress();

            for (int div = 0; div < roadDivide.Count - 1; div++)
            {
                List<double> pwrDbmSub = new List<double>();
                List<double> x = new List<double>();

                x.Add(0);
                for (int j = roadDivide[div]; j < roadDivide[div + 1]; j++)
                {
                    pwrDbmSub.Add(pwrDbm[j]);
                    x.Add(j - roadDivide[div] + 1);
                }

                #region 提取信号强度突变点
                Divide divide = new Divide(100, 0.8, ref pwrDbmSub);
                divide.run();
                #endregion

                #region 对每段进行线性拟合

                for (int i = 0; i < divide.posV.Count - 1; i++)
                {
                    int id1 = divide.posV[i];
                    int id2 = divide.posV[i + 1] - 1;
                    if (id2 < 0)
                        id2 = 0;

                    double a2 = 0, b2 = 0, err2 = 0;
                    regress1.CalcRegress(ref x, divide.m, id1, id2, out a2, out b2, out err2);   // y = ax + b  

                    double x1, y1, x2, y2;  // (x1, y1) 单调上升的尽头
                    if (a2 < 0)
                    {
                        x1 = gx[id1 + roadDivide[div]];
                        y1 = gy[id1 + roadDivide[div]];
                        x2 = gx[id2 + roadDivide[div]];
                        y2 = gy[id2 + roadDivide[div]];
                    }
                    else
                    {
                        x1 = gx[id2 + roadDivide[div]];
                        y1 = gy[id2 + roadDivide[div]];
                        x2 = gx[id1 + roadDivide[div]];
                        y2 = gy[id1 + roadDivide[div]];
                    }

                    double k = (x1 - x2) / (y2 - y1);
                    double b = y1 - (x1 - x2) / (y2 - y1) * x1;
                    double y2Tmp = k * x2 + b;

                    if (y2 < y2Tmp)
                    {
                        for (int tx = agMinX + 1; tx < agMaxX - 1; tx++)
                        {
                            for (int ty = agMinY + 1; ty < agMaxY - 1; ty++)
                            {
                                double cx = 0, cy = 0, cz = 0;
                                GridHelper.getInstance().AccGridToXYZ(tx, ty, 0, ref cx, ref cy, ref cz);
                                if (cy > k * cx + b)
                                {
                                    string key = string.Format("{0},{1}", tx, ty);
                                    if (dicMon.Keys.Contains(key))
                                        dicMon[key]++;
                                    else
                                        dicMon[key] = 0;
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int tx = agMinX + 1; tx < agMaxX - 1; tx++)
                        {
                            for (int ty = agMinY + 1; ty < agMaxY - 1; ty++)
                            {
                                double cx = 0, cy = 0, cz = 0;
                                GridHelper.getInstance().AccGridToXYZ(tx, ty, 0, ref cx, ref cy, ref cz);
                                if (cy < k * cx + b)
                                {
                                    string key = string.Format("{0},{1}", tx, ty);
                                    if (dicMon.Keys.Contains(key))
                                        dicMon[key]++;
                                    else
                                        dicMon[key] = 0;
                                }
                            }
                        }
                    }
                }
                #endregion
            }

            // 挑选分数最高的格子
            if (dicMon.Count == 0)
            {
                return new Result(false, "单调性未推出合理结果");
            }
            var maxKeyMon = (from d in dicMon orderby d.Value descending select d.Key).First();
            var allMaxKeyMon = (from d in dicMon where d.Value == dicMon[maxKeyMon] select d.Key).ToArray();
            double X = 0, Y = 0, Z = 0;

            int cnt = 0;
            if (allMaxKeyMon.Count() > 5)
            {
                foreach (string a in allMaxKeyMon)  // 有多个分数最高的格子
                {
                    string[] num = a.Split(',');
                    GridHelper.getInstance().AccGridToXYZ(Convert.ToInt32(num[0]), Convert.ToInt32(num[1]), 0, ref X, ref Y, ref Z);

                    Pt3D pt = new Pt3D(X, Y, 0);
                    if (inBound(ref pt))
                        MonotoneResult.Add(pt);
                }
            }
            else
            {
                var dicSort = from objDic in dicMon orderby objDic.Value descending select objDic;
                foreach (KeyValuePair<string, int> kvp in dicSort)
                {
                    string[] num = kvp.Key.Split(',');
                    GridHelper.getInstance().AccGridToXYZ(Convert.ToInt32(num[0]), Convert.ToInt32(num[1]), 0, ref X, ref Y, ref Z);

                    ++cnt;
                    MonotoneResult.Add(new Pt3D(X, Y, 0));

                    if (cnt > 5)
                        break;
                }
            }
            return new Result(true);
        }

        List<Pt3D> StrongWeakResult = new List<Pt3D>();

        // 根据强弱相间点对栅格打分
        private Result strongWeakLoc()
        {
            Pt.Sort(new StrongWeakPtCmp());

            // 加载加速结构
            RayTracing inter = new RayTracing();
            inter.SinglePrepare(leftBound, rightBound, downBound, upBound);

            // 从每个点发出360*360条射线，经过的格子分值加1，分数最高的格子为可能位置
            Dictionary<string, int> dic = new Dictionary<string, int>();
            for (int i = 0; i < Pt.Count && i < 20; i++)
            {
                for (int j = 0; j < 360; j++)
                {
                    for (int h = 3; h < 100; h += 3)
                    {
                        double angle = (double)(j) / 180.0 * Math.PI;
                        LTE.Geometric.Point endPoint = new Geometric.Point(Pt[i].strong.X + Math.Cos(angle), Pt[i].strong.Y + Math.Sin(angle), (double)h);
                        LTE.Geometric.Vector3D dir = LTE.Geometric.Vector3D.constructVector(Pt[i].strong, endPoint);
                        inter.SingleRayJudge(Pt[i].strong, dir, ref dic, true);

                        LTE.Geometric.Point endPoint1 = new Geometric.Point(Pt[i].weak.X + Math.Cos(angle), Pt[i].weak.Y + Math.Sin(angle), (double)h);
                        LTE.Geometric.Vector3D dir1 = LTE.Geometric.Vector3D.constructVector(Pt[i].weak, endPoint1);
                        inter.SingleRayJudge(Pt[i].weak, dir1, ref dic, false);
                    }
                }
            }

            if (dic.Count == 0)
            {
                return new Result(false, "强弱点对未推出合理结果");
            }

            // 挑选分数最高的格子
            var maxKey = (from d in dic orderby d.Value descending select d.Key).First();
            var allMaxKey = (from d in dic where d.Value == dic[maxKey] select d.Key).ToArray();

            int cnt = 0;
            double X = 0, Y = 0, Z = 0;

            if (allMaxKey.Count() > 5)
            {
                foreach (string a in allMaxKey)  // 有多个分数最高的格子
                {
                    string[] num = a.Split(',');
                    GridHelper.getInstance().AccGridToXYZ(Convert.ToInt32(num[0]), Convert.ToInt32(num[1]), Convert.ToInt32(num[2]), ref X, ref Y, ref Z);

                    Pt3D pt = new Pt3D(X, Y, Z);
                    if (inBound(ref pt))
                    {
                        StrongWeakResult.Add(pt);
                    }
                }
            }
            else
            {
                var dicSort = from objDic in dic orderby objDic.Value descending select objDic;
                foreach (KeyValuePair<string, int> kvp in dicSort)
                {
                    string[] num = kvp.Key.Split(',');
                    GridHelper.getInstance().AccGridToXYZ(Convert.ToInt32(num[0]), Convert.ToInt32(num[1]), 0, ref X, ref Y, ref Z);

                    ++cnt;

                    Pt3D pt = new Pt3D(X, Y, Z);
                    if (inBound(ref pt))
                    {
                        StrongWeakResult.Add(pt);
                    }

                    if (cnt > 5)
                        break;
                }
            }
            return new Result(true);
        }

        bool inBound(ref Pt3D pt)
        {
            if (pt.x < rightBound && pt.y < upBound && pt.x > leftBound && pt.y > downBound)
                return true;
            return false;
        }

        void newBound(ref List<Pt3D> result, ref double left, ref double down, ref double right, ref double up)
        {
            if (result.Count == 0)
                return;

            for (int i = 0; i < result.Count; i++)
            {
                left = Math.Min(left, result[i].x);
                down = Math.Min(down, result[i].y);
                right = Math.Max(right, result[i].x);
                up = Math.Max(up, result[i].y);
            }
        }

        void addResult(ref System.Text.StringBuilder msg, ref List<Pt3D> result)
        {
            for (int i = 0; i < result.Count; i++)
            {
                msg.Append(string.Format("{0}\t{1}\t{2}\n", Math.Round(result[i].x, 3), Math.Round(result[i].y, 3), Math.Round(result[i].z, 3)));
            }
        }

        // 干扰源候选位置评估
        public Result candidate()
        {
            System.Text.StringBuilder msg = new System.Text.StringBuilder();

            DateTime t0 = DateTime.Now;

            filterLoc(ref Pt2);

            DateTime t1 = DateTime.Now;

            msg.Append(string.Format("候选干扰源评估所用时间：{0} s\n", (t1 - t0).TotalMilliseconds / 1000));
            msg.Append("候选干扰源评估结果：\n");
            addResult(ref msg, ref FinalResult);
            msg.Append("\n\n距离真实干扰源的位置：\n");
            for (int i = 0; i < FinalResult.Count; i++)
            {
                msg.Append(string.Format("{0}\n", Math.Sqrt(Math.Pow(FinalResult[i].x - 668400, 2) + Math.Pow(FinalResult[i].y - 3545720, 2))));
            }

            writeFinalResultToDB();

            return new Result(true, msg.ToString());
        }

        // 射线跟踪
        List<Pt3D> RayResult = new List<Pt3D>();
        List<Pt3D> FinalResult = new List<Pt3D>();
        private void filterLoc(ref List<StrongWeakPt> Pt)
        {
            // 寻找符合条件的强点
            Pt.Sort(new StrongPtCmp());
            List<List<int>> id = new List<List<int>>();

            double dis = 300;
            int numPt = Math.Min(25, Math.Max(0, Pt.Count() - 3));
            bool ok = false;
            while (id.Count < numPt && dis >= 0)
            {
                for (int i = 0; i < Pt.Count; i++)
                {
                    double x1 = Pt[i].strong.X;
                    double y1 = Pt[i].strong.Y;

                    for (int j = i + 1; j < Pt.Count; j++)
                    {
                        double x2 = Pt[j].strong.X;
                        double y2 = Pt[j].strong.Y;

                        if (Math.Abs(x1 - x2) > dis && Math.Abs(y1 - y2) > dis)
                        {
                            for (int k = j + 1; k < Pt.Count; k++)
                            {
                                double x3 = Pt[k].strong.X;
                                double y3 = Pt[k].strong.Y;

                                if (Math.Abs(x3 - x2) > dis && Math.Abs(y3 - y2) > dis && Math.Abs(x1 - x3) > dis && Math.Abs(y1 - y3) > dis)
                                {
                                    List<int> tmpId = new List<int>();
                                    tmpId.Add(i);
                                    tmpId.Add(j);
                                    tmpId.Add(k);
                                    id.Add(tmpId);
                                    if (id.Count >= numPt)
                                    {
                                        ok = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (ok)
                            break;
                    }
                    if (ok)
                        break;
                }
                dis -= 50;
            }

            // 评估
            List<Score> score = new List<Score>();
            CalcGridStrength cgs = new CalcGridStrength(null, null);

            // 实际的
            List<double> dif1 = new List<double>();
            for (int i = 0; i < id.Count; i++)
            {
                List<int> j = id[i];
                dif1.Add((Pt[j[0]].strongPower - Pt[j[1]].strongPower) / (Pt[j[0]].strongPower - Pt[j[2]].strongPower));
            }

            // 计算的
            score.Add(new Score(0, 0, 0, double.MaxValue));
            for (double x = leftBound; x < rightBound; x += 10)
            {
                for (double y = downBound; y < upBound; y += 10)
                {
                    for (double h = 3; h < 100; h += 10)
                    {
                        double diff = 0;
                        for (int i = 0; i < id.Count; i++)
                        {
                            List<int> j = id[i];
                            double p1 = cgs.calcDirectRayStrength(x, y, h, Pt[j[0]].strong.X, Pt[j[0]].strong.Y, 0, 50, 1800);
                            double p2 = cgs.calcDirectRayStrength(x, y, h, Pt[j[1]].strong.X, Pt[j[1]].strong.Y, 0, 50, 1800);
                            double p3 = cgs.calcDirectRayStrength(x, y, h, Pt[j[2]].strong.X, Pt[j[2]].strong.Y, 0, 50, 1800);
                            double dif2 = (p1 - p2) / (p1 - p3);
                            if (Math.Abs(p1 - p3) < 0.0001)
                            {
                                continue;
                            }
                            diff += Math.Pow(dif2 - dif1[i], 2);
                        }
                        score.Add(new Score(x, y, h, diff));
                    }
                }
            }

            score.Sort(new ScoreCmp());

            for (int i = 0; i < score.Count && i < 3; i++)
            {
                RayResult.Add(new Pt3D(score[i].x, score[i].y, score[i].z));

                int gx = 0, gy = 0, gz = 0;
                GridHelper.getInstance().XYZToAccGrid(score[i].x, score[i].y, score[i].z, ref gx, ref gy, ref gz);

                FinalResult.Add(new Pt3D(score[i].x, score[i].y, score[i].z));
            }
        }

        void writeFinalResultToDB()
        {
            System.Data.DataTable dtable = new System.Data.DataTable();
            dtable.Columns.Add("id");
            dtable.Columns.Add("x");
            dtable.Columns.Add("y");
            dtable.Columns.Add("z");
            dtable.Columns.Add("Longitude");
            dtable.Columns.Add("Latitude");

            for (int i = 0; i < FinalResult.Count; i++)
            {
                System.Data.DataRow thisrow = dtable.NewRow();
                thisrow["id"] = i;
                thisrow["x"] = Math.Round(FinalResult[i].x, 3);
                thisrow["y"] = Math.Round(FinalResult[i].y, 3);
                thisrow["z"] = Math.Round(FinalResult[i].z, 3);
                IPoint pt = GeometryUtilities.ConstructPoint3D(FinalResult[i].x, FinalResult[i].y, 0);
                PointConvert.Instance.GetGeoPoint(pt);
                thisrow["Longitude"] = Math.Round(pt.X, 6);
                thisrow["Latitude"] = Math.Round(pt.Y, 6);
                dtable.Rows.Add(thisrow);
            }

            IbatisHelper.ExecuteDelete("DeleteInfSource", null);
            using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
            {
                bcp.BatchSize = dtable.Rows.Count;
                bcp.BulkCopyTimeout = 1000;
                bcp.DestinationTableName = "tbInfSource";
                bcp.WriteToServer(dtable);
                bcp.Close();
            }
            dtable.Clear();
        }
    }

    class DT
    {
        public int id;
        public int ci;
        public int roadID;
        public double x;
        public double y;
    };

    class StrongWeakPt
    {
        public LTE.Geometric.Point weak, strong;
        public double diff;  // 功率差
        public double strongPower;

        public StrongWeakPt(ref LTE.Geometric.Point w, ref LTE.Geometric.Point s, double d)
        {
            this.weak = w;
            this.strong = s;
            this.diff = d;
        }
    };

    class StrongWeakPtCmp : IComparer<StrongWeakPt>
    {
        public int Compare(StrongWeakPt a, StrongWeakPt b)
        {
            return b.diff.CompareTo(a.diff);
        }
    };

    class Pt3D
    {
        public double x, y, z;
        public Pt3D(double x1, double y1, double z1)
        {
            x = x1;
            y = y1;
            z = z1;
        }
    };

    class TmpP
    {
        public int id1, id2;
        public double dis;

        public TmpP() { }

        public TmpP(int id11, int id22, double dis1)
        {
            id1 = id11;
            id2 = id22;
            dis = dis1;
        }
    };

    class TmpPCompare : IComparer<TmpP>
    {
        public int Compare(TmpP a, TmpP b)
        {
            return b.dis.CompareTo(a.dis);
        }
    };

    class StrongPtCmp : IComparer<StrongWeakPt>
    {
        public int Compare(StrongWeakPt a, StrongWeakPt b)
        {
            return b.strongPower.CompareTo(a.strongPower);
        }
    };

    class Score
    {
        public double x, y, z;
        public double score;

        public Score(double x1, double y1, double z1, double score1)
        {
            x = x1;
            y = y1;
            z = z1;
            score = score1;
        }
    };

    class ScoreCmp : IComparer<Score>
    {
        public int Compare(Score a, Score b)
        {
            return a.score.CompareTo(b.score);
        }
    };
}