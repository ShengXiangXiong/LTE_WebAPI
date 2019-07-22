using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LTE.GIS;
using LTE.DB;
using LTE.InternalInterference.Grid;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Analyst3D;
using System.Data.SqlClient;
using System.Collections;
using System.IO;


namespace LTE.InternalInterference
{
    public partial class InfLocate : Form
    {
        private ArrayList attrName = new ArrayList();//数据库属性列表
        LTE.Win32Lib.ConsoleShow cs; 

        public InfLocate()
        {
            InitializeComponent();
            cs = new Win32Lib.ConsoleShow();
        }


        // 确定干扰区域
        static double leftBound = 666918, rightBound = 670080, downBound = 3542960, upBound = 3548826;
        //double l = 666918 - 25, r = 670080 + 25, d = 3542960 - 25, u = 3548826 + 25; // 南京 一次
        //double l = 667479 - 25, r = 668741 + 25, d = 3544851 - 25, u = 3546146 + 25; // 南京 二次
        //double l = 273978 - 25, r = 276773 + 25, d = 3464897 - 25, u = 3467568 + 25; // 苏州
        private void button1_Click(object sender, EventArgs e)
        {
            #region xy 转 经纬度
            //IPoint pt = GeometryUtilities.ConstructPoint3D(666918, 3542960, 0);
            //IPoint pt1 = GeometryUtilities.ConstructPoint3D(670080, 3548826, 0);
            //PointConvert.Instance.GetGeoPoint(pt);
            //PointConvert.Instance.GetGeoPoint(pt1);
            #endregion

            initRange();
            MessageBox.Show("完成！");
        }

        // 确定干扰区域
        private void initRange()
        {
            double minLon, minLat, maxLon, maxLat;
            minLon = minLat = maxLon = maxLat = 0;
            if (!valideInput(ref minLon, ref minLat, ref maxLon, ref maxLat))
            {
                MessageBox.Show("干扰区域边界输入有误！");
                return;
            }

            #region 经纬度 转 xy
            IPoint pt = GeometryUtilities.ConstructPoint3D(minLon, minLat, 0);
            IPoint pt1 = GeometryUtilities.ConstructPoint3D(maxLon, maxLat, 0);
            PointConvert.Instance.GetProjectPoint(pt);
            PointConvert.Instance.GetProjectPoint(pt1);
            #endregion

            leftBound = pt.X;
            downBound = pt.Y;
            rightBound = pt1.X;
            upBound = pt1.Y;

            DrawUtilities.DrawRect(pt, pt1, 255, 0, 0);

            labRange.Text = string.Format("{0}m * {1}m", Math.Round(pt1.X - pt.X, 0), Math.Round(pt1.Y - pt.Y, 0));
        }

        private bool valideInput(ref double minLon, ref double minLat, ref double maxLon, ref double maxLat)
        {
            if ( this.txtMinLon.Text != "" && this.txtMinLat.Text != "" && this.txtMaxLon.Text != "" && this.txtMaxLat.Text != "")
            {
                try{
                    minLon = Convert.ToDouble(this.txtMinLon.Text);
                    minLat = Convert.ToDouble(this.txtMinLat.Text);
                    maxLon = Convert.ToDouble(this.txtMaxLon.Text);
                    maxLat = Convert.ToDouble(this.txtMaxLat.Text);
                }
                catch(Exception e)
                {
                    return false;
                }
            }
            return true;
        }

        private void groupBox4_Enter(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            // 删除旧的路测数据
            IbatisHelper.ExecuteDelete("DeleteDT", null);
            MessageBox.Show("删除成功");
        }

        private void button11_Click(object sender, EventArgs e)
        {
            // 绘制路测数据
            OperateDTLayer layer = new OperateDTLayer();
            layer.ClearLayer();
            layer.constuctDTGrids();
            MessageBox.Show("路测呈现！");
        }

        void printResult(string msg, ref List<Pt3D> result)
        {
            Console.WriteLine(msg);
            for (int i = 0; i < result.Count; i++)
            {
                Console.WriteLine("{0}, {1}, {2}", result[i].x, result[i].y, result[i].z);
            }
            //Console.ReadKey();
            //cs.free();
        }

        bool inBound(ref Pt3D pt)
        {
            if (pt.x < rightBound && pt.y < upBound && pt.x > leftBound && pt.y > downBound)
                return true;
            return false;
        }

        // 根据单调性对栅格打分
        private void button6_Click(object sender, EventArgs e)
        {
            if (roadID.Count == 0)
            {
                Hashtable ht = new Hashtable();
                ht["RoadID1"] = 0;
                ht["RoadID2"] = 6;
                readData("getDT", ht);
                initRange();
            }
            monotoneLoc();
            drawMonotone();
            printResult("路测单调性定位结果：", ref MonotoneResult);
            //Console.ReadKey();
        }

        List<Pt3D> MonotoneResult = new List<Pt3D>();

        // 根据单调性对栅格打分
        private void monotoneLoc()
        {

            int agMaxX = 0, agMaxY = 0, agMinX = 0, agMinY = 0, agZ = 0;
            GridHelper.getInstance().XYZToAccGrid(leftBound, downBound, 0, ref agMinX, ref agMinY, ref agZ);
            GridHelper.getInstance().XYZToAccGrid(rightBound, upBound, 0, ref agMaxX, ref agMaxY, ref agZ);
            //GridHelper.getInstance().getMaxAccGridXY(ref agMaxX, ref agMaxY);

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

                    #region 绘制单调情况
                    //IPoint p = GeometryUtilities.ConstructPoint3D(longtitude[id1 + roadDivide[div]], latitude[id1 + roadDivide[div]], 0);
                    //PointConvert.Instance.GetProjectPoint(p);
                    //IPoint p1 = GeometryUtilities.ConstructPoint3D(longtitude[id2 + roadDivide[div]], latitude[id2 + roadDivide[div]], 0);
                    //PointConvert.Instance.GetProjectPoint(p1);
                    //IGraphicsLayer pLayer = (GISMapApplication.Instance.Scene as IBasicMap).BasicGraphicsLayer;
                    //IRgbColor pColor = new RgbColorClass();  //颜色
                    //if (a2 > 0)
                    //{
                    //    pColor.RGB = System.Drawing.Color.FromArgb(255, 0, 0).ToArgb();  //(B,G,R)
                    //    DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, p, pColor, 8);
                    //    pColor.RGB = System.Drawing.Color.FromArgb(0, 0, 255).ToArgb();  //(B,G,R)
                    //    DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, p1, pColor, 8);
                    //}
                    //else
                    //{
                    //    pColor.RGB = System.Drawing.Color.FromArgb(0, 0, 255).ToArgb();  //(B,G,R)
                    //    DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, p, pColor, 8);
                    //    pColor.RGB = System.Drawing.Color.FromArgb(255, 0, 0).ToArgb();  //(B,G,R)
                    //    DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, p1, pColor, 8);
                    //}
                    //DrawLine(p, p1);

                    //if (id1 == 0)
                    //    continue;
                    //double azimuth = Math.Atan(a2) * 180 / Math.PI;
                    //IPoint p = GeometryUtilities.ConstructPoint3D(longtitude[id1], latitude[id1], 0);
                    //PointConvert.Instance.GetProjectPoint(p);
                    //DrawTriangle(azimuth, 30, p.X, p.Y);
                    #endregion
                }
                #endregion
            }

            // 挑选分数最高的格子
            if (dicMon.Count == 0)
            {
                MessageBox.Show("单调性未推出合理结果");
                return;
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
                    if(inBound(ref pt))
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
        }

        // 根据强弱相间对栅格打分
        private void button8_Click(object sender, EventArgs e)
        {
            if (roadID.Count == 0)
            {
                Hashtable ht = new Hashtable();
                ht["RoadID1"] = 0;
                ht["RoadID2"] = 6;
                readData("getDT", ht);
                initRange();
            }
            strongWeakPt();
            strongWeakLoc();
            drawStrongWeak();
            printResult("路测相邻强弱信号点对定位结果：", ref StrongWeakResult);
            //Console.ReadKey();
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
                    //while (k < n && pwrDbm[k] - pwrDbm[j - 1] > 15)
                    //    k++;
                    //k = (k + j) / 2;

                    LTE.Geometric.Point pt = new LTE.Geometric.Point(gx[k], gy[k], 0);
                    LTE.Geometric.Point pt1 = new LTE.Geometric.Point(gx[j - 1], gy[j - 1], 0);
                    StrongWeakPt p = new StrongWeakPt(ref pt1, ref pt, Math.Abs(pwrDbm[k] - pwrDbm[j - 1]));

                    p.strongPower = pwrDbm[j];
                    Pt.Add(p);
                }
                else if (pwrDbm[j - 1] - pwrDbm[j] > 15 && pwrDbm[j - 1] - pwrDbm[j] < 25 && (Math.Abs(gx[j - 1] - gx[j]) < 100 && Math.Abs(gy[j - 1] - gy[j]) < 100))
                {
                    int k = j;
                    //while (k < n && pwrDbm[j - 1] - pwrDbm[k] > 15)
                    //    k++;
                    //k = (k + j) / 2;

                    LTE.Geometric.Point pt = new LTE.Geometric.Point(gx[j - 1], gy[j - 1], 0);
                    LTE.Geometric.Point pt1 = new LTE.Geometric.Point(gx[k], gy[k], 0);
                    StrongWeakPt p = new StrongWeakPt(ref pt1, ref pt, Math.Abs(pwrDbm[k] - pwrDbm[j - 1]));
                    p.strongPower = pwrDbm[j - 1];
                    Pt.Add(p);
                }
            }

        }

        List<Pt3D> StrongWeakResult = new List<Pt3D>();

        // 根据强弱相间点对栅格打分
        private void strongWeakLoc()
        {
            Pt.Sort(new StrongWeakPtCmp());

            #region 绘制
            //for (int i = 0; i < Pt.Count && i < 20; i++)
            //{
            //    IPoint p = GeometryUtilities.ConstructPoint3D(Pt[i].strong.X, Pt[i].strong.Y, 0);
            //    IPoint p1 = GeometryUtilities.ConstructPoint3D(Pt[i].weak.X, Pt[i].weak.Y, 0);
            //    IGraphicsLayer pLayer = (GISMapApplication.Instance.Scene as IBasicMap).BasicGraphicsLayer;
            //    IRgbColor pColor = new RgbColorClass();  //颜色
            //    pColor.RGB = System.Drawing.Color.FromArgb(255, 0, 0).ToArgb();  //(B,G,R)
            //    DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, p, pColor);
            //    pColor.RGB = System.Drawing.Color.FromArgb(0, 0, 255).ToArgb();  //(B,G,R)
            //    DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, p1, pColor);
            //}
            #endregion

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
                MessageBox.Show("强弱点对未推出合理结果");
                return;
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

        }

        // 根据强信号点推理干扰源
        private void button9_Click(object sender, EventArgs e)
        {
            if (roadID.Count == 0)
            {
                Hashtable ht = new Hashtable();
                ht["RoadID1"] = 0;
                ht["RoadID2"] = 6;
                readData("getDT", ht);
                initRange();
            }
            strongWeakPt();
            // strong1();  // 三点定位
            strongLoc();   // 多点定位
            drawStrong();
            printResult("路测强信号点定位结果：", ref StrongResult);
            //Console.ReadKey();
        }

        List<Pt3D> StrongResult = new List<Pt3D>();

        // 根据强信号点推理干扰源，多点定位
        private void strongLoc()
        {
            List<List<int>> id = getStrongPtMult();  // 获得多点
            //List<List<int>> id = getStrongPtMul();   // 获得多点
            MulPtLoc solve = new MulPtLoc((leftBound+rightBound)/2, (downBound + upBound)/2, 6);

            for (int i = 0; i < id.Count; i++)
            {
                List<List<double>> X = new List<List<double>>();  // 测量点位置
                List<double> P = new List<double>();   // 测量点功率

                for(int j=0; j<id[i].Count; j++)
                {
                    int k = id[i][j];
                    X.Add(new List<double>{gx[k], gy[k]});
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

        // 根据强信号点推理干扰源，三点定位
        private void strong1()
        {
            Pt.Sort(new StrongPtCmp());

            // 寻找距离较远的强信号点
            List<List<int>> id = new List<List<int>>();

            // 寻找符合条件的强点
            double dis = 300;
            int numPt = 5;
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

            IRgbColor pColor = new RgbColorClass();  //颜色
            pColor.RGB = System.Drawing.Color.FromArgb(0, 255, 0).ToArgb();
            IGraphicsLayer pLayer = (GISMapApplication.Instance.Scene as IBasicMap).BasicGraphicsLayer;
            IPoint pt = null;

            // 三点定位
            int gx = 0, gy = 0, gz = 0;
            for (int i = 0; i < id.Count; i++)
            {
                List<int> j = id[i];
                Newton newton = new Newton(Pt[j[0]].strongPower, Pt[j[1]].strongPower, Pt[j[2]].strongPower,
                                           Pt[j[0]].strong.X, Pt[j[1]].strong.X, Pt[j[2]].strong.X,
                                           Pt[j[0]].strong.Y, Pt[j[1]].strong.Y, Pt[j[2]].strong.Y);
                newton.run();

                GridHelper.getInstance().XYZToAccGrid(newton.result[0], newton.result[1], 0, ref gx, ref gy, ref gz);

                if (newton.result[0] > leftBound && newton.result[0] < rightBound && newton.result[1] > downBound && newton.result[1] < upBound)
                //if (newton.result[0] >= l && newton.result[0] <= r && newton.result[1] >= d && newton.result[1] <= u)
                {
                    StrongResult.Add(new Pt3D(newton.result[0], newton.result[1], 0));
                }

                #region
                //pt = GeometryUtilities.ConstructPoint3D(Pt[j[0]].strong.X, Pt[j[0]].strong.Y, 0);
                //DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, pt, pColor, 20);
                //pt = GeometryUtilities.ConstructPoint3D(Pt[j[1]].strong.X, Pt[j[1]].strong.Y, 0);
                //DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, pt, pColor, 20);
                //pt = GeometryUtilities.ConstructPoint3D(Pt[j[2]].strong.X, Pt[j[2]].strong.Y, 0);
                //DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, pt, pColor, 20);
                #endregion
            }

            if (StrongResult.Count == 0)
            {
                MessageBox.Show("强信号点未推出合理结果");
                return;
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

            for (int i = 0; i < k; i+=6)
            {
                List<int> ptid = new List<int>();
                for (int j = i; j < i + 6; j++)
                    ptid.Add(id[i * 6 + j]);
                ptID.Add(ptid);
            }
            return ptID;

            #region
            //string path = @"c:\t22.txt";
            //StreamWriter sw = File.CreateText(path);
            //for (int i = 0; i < k; i++)
            //{
            //    sw.WriteLine(Pt[id[i]].strong.X + "\t" + Pt[id[i]].strong.Y + "\t" + Pt[id[i]].strongPower);
            //}
            //sw.Close();

            //string path1 = @"c:\tc.txt";
            //StreamWriter sw1 = File.CreateText(path1);
            //sw1.WriteLine(cx + "\t" + cy);
            //sw1.Close();

            //string path2 = @"c:\tall.txt";
            //StreamWriter sw2 = File.CreateText(path2);
            //for (int i = 0; i < Pt.Count; i++)
            //    sw2.WriteLine(Pt[i].strong.X + "\t" + Pt[i].strong.Y);
            //sw2.Close();
            #endregion
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

        // 获得多个强信号点
        private List<List<int>> getStrongPtMul()
        {
            Pt.Sort(new StrongPtCmp());

            // 寻找距离较远的强信号点
            List<List<int>> id = new List<List<int>>();

            // 寻找符合条件的强点
            double dis = 300;
            int numPt = 5;
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
                                    for (int l = k + 1; l < Pt.Count; l++)
                                    {
                                        double x4 = Pt[l].strong.X;
                                        double y4 = Pt[l].strong.Y;

                                        if (Math.Abs(x4 - x3) > dis && Math.Abs(y4 - y3) > dis && Math.Abs(x4 - x2) > dis && Math.Abs(y4 - y2) > dis
                                            && Math.Abs(x4 - x1) > dis && Math.Abs(y4 - y1) > dis)
                                        {
                                            for (int ii = l + 1; ii < Pt.Count; ii++)
                                            {
                                                double x5 = Pt[ii].strong.X;
                                                double y5 = Pt[ii].strong.Y;

                                                if (Math.Abs(x5 - x4) > dis && Math.Abs(y5 - y4) > dis && Math.Abs(x5 - x3) > dis && Math.Abs(y5 - y3) > dis
                                                    && Math.Abs(x5 - x2) > dis && Math.Abs(y5 - y2) > dis && Math.Abs(x5 - x1) > dis && Math.Abs(y5 - y1) > dis)
                                                {
                                                    for (int jj = ii + 1; jj < Pt.Count; jj++)
                                                    {
                                                        double x6 = Pt[jj].strong.X;
                                                        double y6 = Pt[jj].strong.Y;

                                                        if (Math.Abs(x6 - x5) > dis && Math.Abs(y6 - y5) > dis && Math.Abs(x6 - x4) > dis && Math.Abs(y6 - y4) > dis
                                                            && Math.Abs(x6 - x3) > dis && Math.Abs(y6 - y3) > dis && Math.Abs(x6 - x3) > dis
                                                            && Math.Abs(y6 - y3) > dis && Math.Abs(x6 - x2) > dis && Math.Abs(y6 - y1) > dis)
                                                        {
                                                            List<int> tmpId = new List<int>();
                                                            tmpId.Add(i);
                                                            tmpId.Add(j);
                                                            tmpId.Add(k);
                                                            tmpId.Add(l);
                                                            tmpId.Add(ii);
                                                            tmpId.Add(jj);
                                                            id.Add(tmpId);
                                                            if (id.Count >= numPt)
                                                            {
                                                                ok = true;
                                                                break;
                                                            }
                                                        }
                                                        if (ok)
                                                            break;
                                                    }
                                                }
                                                if (ok)
                                                    break;
                                            }
                                        }
                                        if (ok)
                                            break;
                                    }
                                }
                                if (ok)
                                    break;
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

            return id;
            //string path = @"c:\t1.txt";
            //StreamWriter sw = File.CreateText(path);
            //for (int i = 0; i < id.Count; i++)
            //{
            //    for (int j = 0; j < id[i].Count; j++)
            //    {
            //        sw.WriteLine(Pt[id[i][j]].strong.X + "\t" + Pt[id[i][j]].strong.Y + "\t" + Pt[id[i][j]].strongPower);
            //    }
            //}
            //sw.Close();
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

                //if (roadID[i] != roadID[i - 1])
                //{
                //    roadDivide.Add(i);
                //}
            }
            centX /= k;
            centY /= n;
            //roadDivide.Add(n - 1);

            #region 提取道路
            Divide divide1 = new Divide(50, 0.75, ref y);
            divide1.run();

            //roadDivide.Add(0);
            for (int i = 1; i < divide1.posV.Count; i++)
                if (divide1.posV[i] - divide1.posV[i - 1] > 5)
                    roadDivide.Add(divide1.posV[i - 1]);
            roadDivide.Add(k - 1);
            #endregion

            n = k;

            #region 分析路测  突变点
            //List<Circle> circles = new List<Circle>();   // 存放以突变点为中心的圆
            //for (int i = 0; i < pos.Count; i++)
            //{
            //    int k = pos[i];
            //    double dis = dis2(pwrDbm[k - 1], 53) * 1000;

            //    IPoint p = GeometryUtilities.ConstructPoint3D(longtitude[k - 1], latitude[k - 1], 0);
            //    PointConvert.Instance.GetProjectPoint(p);
            //    ////IGraphicsLayer pLayer = (GISMapApplication.Instance.Scene as IBasicMap).BasicGraphicsLayer;
            //    //DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, p);
            //    //IRgbColor pColor = new RgbColorClass();    //颜色
            //    //pColor.RGB = System.Drawing.Color.FromArgb(255, 0, 0).ToArgb();//(B,G,R)
            //    //DrawCircle_Graphics(p, dis, pColor);

            //    Circle C = new Circle(p.X, p.Y, dis);
            //    circles.Add(C);
            //}

            //// 存放圆的交点
            //List<LTE.Geometric.Point> crossPts = new List<LTE.Geometric.Point>();  
            //for (int i = 0; i < circles.Count-1; i++)
            //{
            //    for (int j = i + 1; j < circles.Count; j++)
            //    {
            //        LTE.Geometric.Point p1 = new BLL.Geometric.Point(), p2 = new BLL.Geometric.Point();
            //        bool ok = MyMath.calc(circles[i].Cent.X, circles[i].Cent.Y, circles[i].r, circles[j].Cent.X, circles[j].Cent.Y, circles[j].r, ref p1, ref p2);
            //        if (ok)
            //        {
            //            crossPts.Add(p1);
            //            crossPts.Add(p2);
            //        }
            //    }
            //}

            ////for (int i = 0; i < crossPts.Count; i++)
            ////{
            ////    IPoint p = GeometryUtilities.ConstructPoint3D(crossPts[i].X, crossPts[i].Y, 0);
            //////    IGraphicsLayer pLayer = (GISMapApplication.Instance.Scene as IBasicMap).BasicGraphicsLayer;
            ////    DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, p);
            ////}

            //// 对交点进行聚类
            //int K = 3;  // 聚类个数
            //double[,] clusterAssment = new double[crossPts.Count, 2];   // 第一列存储簇分配结果，第二列存储平方误差
            //List<LTE.Geometric.Point> centList = new List<LTE.Geometric.Point>();      // 存储所有质心
            //Cluster cluster = new Cluster();
            //cluster.biKeans(ref crossPts, K, ref clusterAssment, ref centList);

            //#region 绘制聚类结果
            ////for (int i = 0; i < K; i++)
            ////{
            ////    IPoint p6 = GeometryUtilities.ConstructPoint3D(centList[i].X, centList[i].Y, 0);
            //////    IGraphicsLayer pLayer6 = (GISMapApplication.Instance.Scene as IBasicMap).BasicGraphicsLayer;
            ////    DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, p6);
            ////    IRgbColor pColor6 = new RgbColorClass();    //颜色
            ////    pColor6.RGB = System.Drawing.Color.FromArgb(0, 255, 0).ToArgb();//(B,G,R)
            ////    DrawCircle_Graphics(p6, 300, pColor6);
            ////}
            //#endregion

            //// 得到点数最多的类
            //List<P> cnt = new List<P>();
            //for (int i = 0; i < K; i++)
            //{
            //    int num = 0;
            //    for (int j = 0; j < crossPts.Count; j++)
            //    {
            //        if (clusterAssment[j, 0] == i)
            //        {
            //            num++;
            //        }
            //    }
            //    P p = new P(i, num);
            //    cnt.Add(p);
            //}
            //cnt.Sort(new Pcomp());

            //// 绘制点数最多的类
            ////IPoint p5 = GeometryUtilities.ConstructPoint3D(centList[cnt[1].id].X, centList[cnt[1].id].Y, 0);
            //////IGraphicsLayer pLayer5 = (GISMapApplication.Instance.Scene as IBasicMap).BasicGraphicsLayer;
            ////DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, p5);
            ////IRgbColor pColor5 = new RgbColorClass();    //颜色
            ////pColor5.RGB = System.Drawing.Color.FromArgb(0, 0, 255).ToArgb();//(B,G,R)
            ////DrawCircle_Graphics(p5, 300, pColor5);

            #endregion

            #region 提取道路
            //Divide divide1 = new Divide(26, 0.75, ref y);
            //divide1.run();
            //List<int> pos1 = new List<int>();
            //for (int i = 1; i < divide1.posV.Count; i++)
            //    if (divide1.posV[i] - divide1.posV[i - 1] > 5)
            //        pos1.Add(divide1.posV[i - 1]);
            //pos1.Add(n);
            //Regress regress2 = new Regress();

            //// 对每条道路
            //for (int i = 0; i < pos1.Count - 1; i++)
            //{
            //    int id1 = pos1[i] + 2;
            //    if (id1 >= n)
            //        id1 = n-1;
            //    IPoint p = GeometryUtilities.ConstructPoint3D(longtitude[id1 - 1], latitude[id1 - 1], 0);
            //    PointConvert.Instance.GetProjectPoint(p);

            //    int id2 = pos1[i + 1] - 3;
            //    if (id2 < 0)
            //        id2 = 0;
            //    IPoint p1 = GeometryUtilities.ConstructPoint3D(longtitude[id2], latitude[id2], 0);
            //    PointConvert.Instance.GetProjectPoint(p1);

            //    IGraphicsLayer pLayer = (GISMapApplication.Instance.Scene as IBasicMap).BasicGraphicsLayer;
            //    IRgbColor pColor = new RgbColorClass();  //颜色
            //    pColor.RGB = System.Drawing.Color.FromArgb(0, 255, 0).ToArgb();//(B,G,R)
            //    //DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, p, pColor);
            //    //DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, p1, pColor);

            //    //DrawLine(p, p1);

            //    // 提取突变点
            //    List<double> pwrDbm1 = new List<double>();
            //    for(int j=id1-1; j<=id2; j++)
            //    {
            //        pwrDbm1.Add(pwrDbm[j]);
            //    }
            //    Divide divide2 = new Divide(26, 0.7, ref pwrDbm1);
            //    divide2.run();

            //    // 对突变点分析
            //    for (int j = 0; j < divide2.posV.Count - 1; j++)
            //    {
            //        //int iid = divide2.posV[j] - 1;

            //        int id3 = divide2.posV[j];
            //        int id4 = divide2.posV[j + 1] - 1;
            //        if (id4 < 0)
            //            id4 = 0;

            //        double a2 = 0, b2 = 0, err2 = 0;
            //        regress2.CalcRegress(ref x, divide2.m, id3, id4, out a2, out b2, out err2);   // y = ax + b  

            //        int id5 = id1 + id3;
            //        int id6 = id1 + id4;
            //        if (id5 >= n)
            //            id5 = n - 1;
            //        if (id6 >= n)
            //            id6 = n - 1;
            //        IPoint p3 = GeometryUtilities.ConstructPoint3D(longtitude[id5], latitude[id5], 0);
            //        PointConvert.Instance.GetProjectPoint(p3);
            //        IPoint p4 = GeometryUtilities.ConstructPoint3D(longtitude[id6], latitude[id6], 0);
            //        PointConvert.Instance.GetProjectPoint(p4);
            //        if (a2 > 0.1)
            //        {
            //            pColor.RGB = System.Drawing.Color.FromArgb(255, 0, 0).ToArgb();  //(B,G,R)
            //            DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, p3, pColor);
            //            pColor.RGB = System.Drawing.Color.FromArgb(0, 0, 255).ToArgb();  //(B,G,R)
            //            DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, p4, pColor);
            //            DrawLine(p3, p4);
            //        }
            //        else if (a2 < -0.1)
            //        {
            //            pColor.RGB = System.Drawing.Color.FromArgb(0, 0, 255).ToArgb();  //(B,G,R)
            //            DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, p3, pColor);
            //            pColor.RGB = System.Drawing.Color.FromArgb(255, 0, 0).ToArgb();  //(B,G,R)
            //            DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, p4, pColor);
            //            DrawLine(p3, p4);
            //        }
            //        else
            //        {
            //            pColor.RGB = System.Drawing.Color.FromArgb(0, 255, 0).ToArgb();  //(B,G,R)
            //            DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, p3, pColor);
            //            pColor.RGB = System.Drawing.Color.FromArgb(0, 255, 0).ToArgb();  //(B,G,R)
            //            DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, p4, pColor);
            //            DrawLine(p3, p4);
            //        }
            //    }
            //}
            #endregion

            #region 道路分析
            /*
            //IbatisHelper.ExecuteDelete("DeleteDTtmp", null);
            Regress regress3 = new Regress();

            // 找到每条路
            int road = 0;
            List<int> roadid = new List<int>();
            int k = 0;
            roadid.Add(0);
            while (k < roadID.Count)
            {
                if (roadID[k] == road)
                {
                    k++;
                }
                else
                {
                    roadid.Add(k);
                    ++road;
                }
            }
            roadid.Add(roadID.Count - 1);

            //System.Data.DataTable tb1 = new System.Data.DataTable();
            //tb1.Columns.Add("RoadID");
            //tb1.Columns.Add("PwrDbm");
            //tb1.Columns.Add("k");
            //tb1.Columns.Add("b");
            //tb1.Columns.Add("err");
            List<double> pwrtmp = new List<double>();
            k = 0;
            for (int i = 1; i < roadid.Count; i++)
            {
                // 每条路
                for (int j = roadid[i - 1]; j < roadid[i]; j++)
                {
                    pwrtmp.Add(pwrDbm[j]);
                }

                // 提取突变点
                Divide divide3 = new Divide(pwrtmp.Count / 2, 0.85, ref pwrtmp);
                divide3.run();

                int id0 = roadid[i - 1];

                // 对各段进行拟合
                for (int j = 0; j < divide3.posV.Count - 1; j++)
                {
                    int id1 = divide3.posV[j];
                    int id2 = divide3.posV[j + 1] - 1;

                    double a2 = 0, b2 = 0, err2 = 0;
                    regress3.CalcRegress(ref x, divide3.m, id1, id2, out a2, out b2, out err2);   // y = ax + b  

                    //if (err2 < 1 && id2 - id1 > 20)
                    if (Math.Abs(a2) > Math.Tan(3 / 180.0 * Math.PI) && id2 - id1 > 10)
                    {
                        // 绘制
                        int id5 = id0 + id1 - 1;
                        int id6 = id0 + id2 - 1;
                        IGraphicsLayer pLayer = (GISMapApplication.Instance.Scene as IBasicMap).BasicGraphicsLayer;
                        IRgbColor pColor = new RgbColorClass();  //颜色
                        pColor.RGB = System.Drawing.Color.FromArgb(0, 255, 0).ToArgb();//(B,G,R)
                        IPoint p3 = GeometryUtilities.ConstructPoint3D(longtitude[id5], latitude[id5], 0);
                        PointConvert.Instance.GetProjectPoint(p3);
                        IPoint p4 = GeometryUtilities.ConstructPoint3D(longtitude[id6], latitude[id6], 0);
                        PointConvert.Instance.GetProjectPoint(p4);
                        if (a2 > 0)
                        {
                            pColor.RGB = System.Drawing.Color.FromArgb(255, 0, 0).ToArgb();  //(B,G,R)
                            DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, p3, pColor);
                            pColor.RGB = System.Drawing.Color.FromArgb(0, 0, 255).ToArgb();  //(B,G,R)
                            DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, p4, pColor);
                            DrawLine(p3, p4);
                        }
                        else if (a2 < 0)
                        {
                            pColor.RGB = System.Drawing.Color.FromArgb(0, 0, 255).ToArgb();  //(B,G,R)
                            DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, p3, pColor);
                            pColor.RGB = System.Drawing.Color.FromArgb(255, 0, 0).ToArgb();  //(B,G,R)
                            DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, p4, pColor);
                            DrawLine(p3, p4);
                        }
                    }

                    // 写入数据库
                    int cnt = id2 - id1 + 1;
                    //for (int l = id1; l <= id2; l++)
                    //{
                    //    System.Data.DataRow thisrow = tb1.NewRow();
                    //    thisrow["RoadID"] = k;
                    //    thisrow["PwrDbm"] = a2 * x[l] + b2;
                    //    thisrow["k"] = a2;
                    //    thisrow["b"] = b2;
                    //    thisrow["err"] = err2;
                    //    tb1.Rows.Add(thisrow);
                    //}
                }
                k++;
                pwrtmp.Clear();
            }
            //using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
            //{
            //    bcp.BatchSize = roadID.Count;
            //    bcp.BulkCopyTimeout = 1000;
            //    bcp.DestinationTableName = "tbDTtmp";
            //    bcp.WriteToServer(tb1);
            //    bcp.Close();
            //}
            //tb1.Clear();
             */
            #endregion

            //MessageBox.Show("结束!");
        }

        // 绘制结果
        private void drawResult(List<Pt3D> result, double size, IRgbColor pColor, IRgbColor pColor1)
        {
            IGraphicsLayer pLayer = (GISMapApplication.Instance.Scene as IBasicMap).BasicGraphicsLayer;
            IPoint pt = null;

            for (int i = 0; i < result.Count; i++)
            {
                pt = GeometryUtilities.ConstructPoint3D(result[i].x, result[i].y, 0);
                DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, pt, pColor, size);
            }
        }

        // 绘制单调性结果
        private void drawMonotone()
        {
            IRgbColor pColor = new RgbColorClass();  //颜色
            pColor.RGB = System.Drawing.Color.FromArgb(0, 0, 255).ToArgb();  //(B,G,R)
            //pColor.RGB = System.Drawing.Color.FromArgb(0, 0, 0).ToArgb();  //(B,G,R)
            IRgbColor pColor1 = new RgbColorClass();
            pColor1.RGB = System.Drawing.Color.FromArgb(255, 0, 0).ToArgb();  //(B,G,R)

            drawResult(MonotoneResult, 25, pColor, pColor1);
        }

        // 绘制强弱相间结果
        private void drawStrongWeak()
        {
            IRgbColor pColor = new RgbColorClass();  //颜色
            pColor.RGB = System.Drawing.Color.FromArgb(0, 255, 0).ToArgb();  //(B,G,R)
            //pColor.RGB = System.Drawing.Color.FromArgb(0, 0, 0).ToArgb();  //(B,G,R)
            IRgbColor pColor1 = new RgbColorClass();
            pColor1.RGB = System.Drawing.Color.FromArgb(255, 0, 0).ToArgb();  //(B,G,R)

            drawResult(StrongWeakResult, 25, pColor, pColor1);
        }

        // 绘制强点结果
        private void drawStrong()
        {
            IRgbColor pColor = new RgbColorClass();  //颜色
            pColor.RGB = System.Drawing.Color.FromArgb(0, 255, 255).ToArgb();  //黄色
            //pColor.RGB = System.Drawing.Color.FromArgb(0, 0, 0).ToArgb();
            IRgbColor pColor1 = new RgbColorClass();
            pColor1.RGB = System.Drawing.Color.FromArgb(255, 0, 0).ToArgb();  //(B,G,R)

            drawResult(StrongResult, 25, pColor, pColor1);
        }

        // 绘制射线跟踪结果/候选位置评估结果
        private void drawRay()
        {
            IRgbColor pColor = new RgbColorClass();  //颜色
            IGraphicsLayer pLayer = (GISMapApplication.Instance.Scene as IBasicMap).BasicGraphicsLayer;
            IPoint pt = null;

            pColor.RGB = System.Drawing.Color.FromArgb(0, 0, 0).ToArgb();  //黑色
            //pColor.RGB = System.Drawing.Color.FromArgb(0, 0, 255).ToArgb(); 

            for (int i = 0; i < RayResult.Count; i++)
            {
                pt = GeometryUtilities.ConstructPoint3D(RayResult[i].x, RayResult[i].y, RayResult[i].z);
                DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, pt, pColor, 25);
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

        // 评估
        private void button12_Click(object sender, EventArgs e)
        {
            DateTime t0 = DateTime.Now;
            
            filterLoc(Pt2);
            drawRay();

            //FinalResult.Clear();
            //FinalResult.Add(new Pt3D(668336, 3545716, 20));
            //FinalResult.Add(new Pt3D(668336, 3545711, 20));
            //FinalResult.Add(new Pt3D(668331, 3545716, 20));
            DateTime t1 = DateTime.Now;

            Console.WriteLine();
            //Console.WriteLine("候选干扰源评估所用时间：5.3684649 min");
            Console.WriteLine("候选干扰源评估所用时间：{0} s", (t1 - t0).TotalMilliseconds / 1000);
            printResult("候选干扰源评估结果：", ref FinalResult);
            Console.WriteLine("距离真实干扰源的位置：");
            for (int i = 0; i < FinalResult.Count; i++)
            {
                Console.WriteLine("{0}", Math.Sqrt(Math.Pow(FinalResult[i].x - 668400, 2) + Math.Pow(FinalResult[i].y - 3545720, 2)));
            }

            writeFinalResultToDB();
            Console.WriteLine("写入数据库表：tbInfSource");
            Console.ReadKey();
        }

        // 射线跟踪用到
        List<StrongWeakPt> Pt2 = new List<StrongWeakPt>();
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
                        //while (k < n && pwrDbm[k] - pwrDbm[j - 1] > 15)
                        //    k++;
                        //k = (k + j) / 2;

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

        // 射线跟踪
        List<Pt3D> RayResult = new List<Pt3D>();
        List<Pt3D> FinalResult = new List<Pt3D>();
        private void filterLoc(List<StrongWeakPt> Pt)
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
                        //if(diff < score[score.Count-1].score)
                        score.Add(new Score(x, y, h, diff));
                    }
                }
            }

            #region 绘制
            //IRgbColor pColor = new RgbColorClass();  //颜色
            //pColor.RGB = System.Drawing.Color.FromArgb(0, 0, 255).ToArgb();  //(B,G,R)
            //IGraphicsLayer pLayer = (GISMapApplication.Instance.Scene as IBasicMap).BasicGraphicsLayer;

            //for (int i = 0; i < posX.Count; i++)
            //{
            //    IPoint pt = GeometryUtilities.ConstructPoint3D(posX[i], posY[i], 0);
            //    DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, pt, pColor, 20);
            //}

            //pColor.RGB = System.Drawing.Color.FromArgb(255, 0, 0).ToArgb();  //(B,G,R)

            //for (int i = 0; i < bestX.Count; i++)
            //{
            //    IPoint pt = GeometryUtilities.ConstructPoint3D(bestX[i], bestY[i], bestZ[i]);
            //    DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, pt, pColor, 20);
            //}
            #endregion

            score.Sort(new ScoreCmp());

            for (int i = 0; i < score.Count && i < 3; i++)
            {
                RayResult.Add(new Pt3D(score[i].x, score[i].y, score[i].z));

                int gx = 0, gy = 0, gz = 0;
                GridHelper.getInstance().XYZToAccGrid(score[i].x, score[i].y, score[i].z, ref gx, ref gy, ref gz);

                FinalResult.Add(new Pt3D(score[i].x, score[i].y, score[i].z));
            }
        }

        static bool first = true;  // 第一次迭代

        // 综合运用所有启发式规则
        private void button7_Click(object sender, EventArgs e)
        {
            //Console.WriteLine("第二次干扰源搜索区域压缩：");

            Hashtable ht = new Hashtable();
            ht["RoadID1"] = 0;
            ht["RoadID2"] = 6;

            readData("getDT", ht);
            strongWeakPt();     // 强弱相间点、强点用到
            strongWeakPt2();    // 射线跟踪用到

            DateTime t0 = DateTime.Now;

            #region 强点
            strongLoc();
            drawStrong();      // 黄色
            //MessageBox.Show("强点评估结束");
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
            drawMonotone();    // 红色
            //MessageBox.Show("单调性评估结束");
            #endregion

            DateTime t2 = DateTime.Now;

            #region 强弱
            if (!first)  // 只利用新区域内的路测
                strongWeakPt();

            strongWeakLoc();
            drawStrongWeak();  // 绿色
            //MessageBox.Show("强弱相间评估结束");
            #endregion

            DateTime t3 = DateTime.Now;

            first = false;   // 准备第二次压缩

            #region 临时

            //MonotoneResult.Clear();
            //StrongResult.Clear();
            //StrongWeakResult.Clear();

            //MonotoneResult.Add(new Pt3D(668651, 3545786, 0));
            //MonotoneResult.Add(new Pt3D(668651, 3545816, 0));
            //MonotoneResult.Add(new Pt3D(668651, 3545846, 0));
            //StrongWeakResult.Add(new Pt3D(668231, 3545696, 15));
            //StrongWeakResult.Add(new Pt3D(668261, 3545876, 15));
            //StrongWeakResult.Add(new Pt3D(668231, 3545696, 45));
            //StrongResult.Add(new Pt3D(668382, 3545759, 15));
            //StrongResult.Add(new Pt3D(668383, 3545759, 15));
            //StrongResult.Add(new Pt3D(668384, 3545757, 45));

            #endregion

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

            #region 界面
            IPoint pt = GeometryUtilities.ConstructPoint3D(leftBound, downBound, 0);
            IPoint pt1 = GeometryUtilities.ConstructPoint3D(rightBound, upBound, 0);
            PointConvert.Instance.GetGeoPoint(pt);
            PointConvert.Instance.GetGeoPoint(pt1);
            this.txtMinLon.Text = string.Format("{0}", Math.Round(pt.X, 6));
            this.txtMinLat.Text = string.Format("{0}", Math.Round(pt.Y, 6));
            this.txtMaxLon.Text = string.Format("{0}", Math.Round(pt1.X, 6));
            this.txtMaxLon.Text = string.Format("{0}", Math.Round(pt1.X, 6));
            this.labRange.Text = string.Format("{0}m * {1}m", Math.Round(rightBound - leftBound, 0), Math.Round(upBound - downBound, 0));
            #endregion

            DrawUtilities.DrawRect(leftBound, downBound, rightBound, upBound, 255, 0, 0);

            string msg = string.Format("\n新的干扰区域范围：{0}m * {1}m", Math.Round(rightBound - leftBound, 0), 
                                                                        Math.Round(upBound - downBound, 0));
            Console.WriteLine("\n路测单调性定位所用时间：{0} s", (t1 - t0).TotalMilliseconds / 1000);
            printResult("路测单调性干扰源定位结果：", ref MonotoneResult);

            Console.WriteLine("\n路测相邻强弱信号点对定位所用时间：{0} s", (t2 - t1).TotalMilliseconds / 1000);
            printResult("路测相邻强弱信号点对定位结果：", ref StrongWeakResult);

            Console.WriteLine("\n路测强信号点定位所用时间：{0} s", (t3 - t2).TotalMilliseconds / 1000);
            printResult("路测强信号点定位结果：", ref StrongResult);

            Console.WriteLine(msg);
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
        }

        void newBound(ref List<Pt3D> result, ref double left, ref double down, ref double right, ref double up)
        {
            if(result.Count == 0)
                return;

            for (int i = 0; i < result.Count; i++)
            {
                left = Math.Min(left, result[i].x);
                down = Math.Min(down, result[i].y);
                right = Math.Max(right, result[i].x);
                up = Math.Max(up, result[i].y);
            }
        }

        // 虚拟路测生成
        private void button4_Click(object sender, EventArgs e)
        {
            // 注意：确保已完成区域覆盖计算

            // 每条路径上的点序列，确保每两个点之间的路径接近于直线
            double[] xx1 = { 667987, 667596, 667172, 667001 };
            double[] yy1 = { 3545330, 3545454, 3545668, 3545775 };
            double[] xx2 = { 667001, 667063, 667090, 667260, 667264, 667296, 667338, 667299, 667192 };
            double[] yy2 = { 3545775, 3546033, 3546242, 3546968, 3547139, 3547208, 3547361, 3547476, 3547626 };
            double[] xx3 = { 667192, 667291, 667663, 667881, 668101, 668244, 669079 };
            double[] yy3 = { 3547626, 3547600, 3547590, 3547618, 3547609, 3547594, 3547436 };
            double[] xx4 = { 669079, 668983 };
            double[] yy4 = { 3547436, 3546849 };

            List<List<double>> vx = new List<List<double>>();
            List<List<double>> vy = new List<List<double>>();

            vx.Add(new List<double>(xx1));
            vx.Add(new List<double>(xx2));
            vx.Add(new List<double>(xx3));
            vx.Add(new List<double>(xx4));
            vy.Add(new List<double>(yy1));
            vy.Add(new List<double>(yy2));
            vy.Add(new List<double>(yy3));
            vy.Add(new List<double>(yy4));

            // 起始序号
            int id = 0;
            int roadid = 0;

            // 虚拟路测路径生成
            DTPathGen(ref vx, ref vy, ref id, ref roadid);

            // 虚拟路测场强填写
            DTStrength();

            MessageBox.Show("完成！");
        }

        // 虚拟路测路径生成
        // vx, vy 为每条虚拟路测路径上的点序列，每两点之间近乎直线
        public static void DTPathGen(ref List<List<double>> vx, ref List<List<double>> vy, ref int id, ref int roadid)
        {
            DataTable dtable = new DataTable();
            dtable.Columns.Add("id", Type.GetType("System.Int32"));
            dtable.Columns.Add("dateTime", Type.GetType("System.DateTime"));
            dtable.Columns.Add("RoadID", Type.GetType("System.Int32"));
            dtable.Columns.Add("x", Type.GetType("System.Decimal"));
            dtable.Columns.Add("y", Type.GetType("System.Decimal"));
            dtable.Columns.Add("longtitude", Type.GetType("System.Decimal"));
            dtable.Columns.Add("latitude", Type.GetType("System.Decimal"));
            dtable.Columns.Add("gxid", Type.GetType("System.Int32"));
            dtable.Columns.Add("gyid", Type.GetType("System.Int32"));
            dtable.Columns.Add("RecePowerDbm", Type.GetType("System.Double"));

            for (int k = 0; k < vx.Count; k++)
            {

                double x1 = vx[k][0], y1 = vy[k][0];
                double x2, y2;

                for (int i = 1; i < vx[k].Count; i++)
                {
                    x2 = vx[k][i];
                    y2 = vy[k][i];

                    gen(ref id, roadid, x1, y1, x2, y2, ref dtable);

                    x1 = x2;
                    y1 = y2;
                }
                roadid++;
            }

            try
            {
                using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                {
                    bcp.BatchSize = dtable.Rows.Count;
                    bcp.BulkCopyTimeout = 1000;
                    bcp.DestinationTableName = "tbDT";
                    bcp.WriteToServer(dtable);
                    bcp.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            dtable.Clear();
        }

        // 每一条虚拟路测路径生成
        static void gen(ref int id, int roadid, double x1, double y1, double x2, double y2, ref DataTable dtable)
        {
            double dx = Math.Abs(x2 - x1);
            double dy = Math.Abs(y2 - y1);

            double dis = 5;  // 间隔

            if (dy < 10)
            {
                double y = (y1 + y2) / 2.0;
                if (x2 > x1)
                {
                    for (double x = x1; x < x2; x += dis)
                    {
                        DateTime dt = DateTime.Now;
                        System.Data.DataRow thisrow = dtable.NewRow();
                        thisrow["id"] = id++;
                        thisrow["dateTime"] = System.Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                        thisrow["RoadID"] = roadid;
                        thisrow["x"] = Math.Round(x, 1);
                        thisrow["y"] = Math.Round(y, 1);
                        thisrow["longtitude"] = 0;
                        thisrow["latitude"] = 0;
                        thisrow["gxid"] = 0;
                        thisrow["gyid"] = 0;
                        thisrow["RecePowerDbm"] = 0;
                        dtable.Rows.Add(thisrow);
                    }
                }
                else
                {
                    for (double x = x1; x > x2; x -= dis)
                    {
                        DateTime dt = DateTime.Now;
                        System.Data.DataRow thisrow = dtable.NewRow();
                        thisrow["id"] = id++;
                        thisrow["dateTime"] = System.Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                        thisrow["RoadID"] = roadid;
                        thisrow["x"] = Math.Round(x, 1);
                        thisrow["y"] = Math.Round(y, 1);
                        thisrow["longtitude"] = 0;
                        thisrow["latitude"] = 0;
                        thisrow["gxid"] = 0;
                        thisrow["gyid"] = 0;
                        thisrow["RecePowerDbm"] = 0;
                        dtable.Rows.Add(thisrow);
                    }
                }
            }
            else if (dx < 10)
            {
                double x = (x1 + x2) / 2.0;
                if (y2 > y1)
                {
                    for (double y = y1; y < y2; y += dis)
                    {
                        DateTime dt = DateTime.Now;
                        System.Data.DataRow thisrow = dtable.NewRow();
                        thisrow["id"] = id++;
                        thisrow["dateTime"] = System.Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                        thisrow["RoadID"] = roadid;
                        thisrow["x"] = Math.Round(x, 1);
                        thisrow["y"] = Math.Round(y, 1);
                        thisrow["longtitude"] = 0;
                        thisrow["latitude"] = 0;
                        thisrow["gxid"] = 0;
                        thisrow["gyid"] = 0;
                        thisrow["RecePowerDbm"] = 0;
                        dtable.Rows.Add(thisrow);
                    }
                }
                else
                {
                    for (double y = y1; y > y2; y -= dis)
                    {
                        DateTime dt = DateTime.Now;
                        System.Data.DataRow thisrow = dtable.NewRow();
                        thisrow["id"] = id++;
                        thisrow["dateTime"] = System.Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                        thisrow["RoadID"] = roadid;
                        thisrow["x"] = Math.Round(x, 1);
                        thisrow["y"] = Math.Round(y, 1);
                        thisrow["longtitude"] = 0;
                        thisrow["latitude"] = 0;
                        thisrow["gxid"] = 0;
                        thisrow["gyid"] = 0;
                        thisrow["RecePowerDbm"] = 0;
                        dtable.Rows.Add(thisrow);
                    }
                }
            }
            else
            {
                if (dx > dy)
                {
                    double k = dy / dx;
                    double ddy = dis * k;
                    if (y1 > y2)
                        ddy = -ddy;

                    if (x1 < x2)
                    {
                        for (double x = x1, y = y1; x < x2; x += dis, y += ddy)
                        {
                            DateTime dt = DateTime.Now;
                            System.Data.DataRow thisrow = dtable.NewRow();
                            thisrow["id"] = id++;
                            thisrow["dateTime"] = System.Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                            thisrow["RoadID"] = roadid;
                            thisrow["x"] = Math.Round(x, 1);
                            thisrow["y"] = Math.Round(y, 1);
                            thisrow["longtitude"] = 0;
                            thisrow["latitude"] = 0;
                            thisrow["gxid"] = 0;
                            thisrow["gyid"] = 0;
                            thisrow["RecePowerDbm"] = 0;
                            dtable.Rows.Add(thisrow);
                        }
                    }
                    else
                    {
                        for (double x = x1, y = y1; x > x2; x -= dis, y += ddy)
                        {
                            DateTime dt = DateTime.Now;
                            System.Data.DataRow thisrow = dtable.NewRow();
                            thisrow["id"] = id++;
                            thisrow["dateTime"] = System.Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                            thisrow["RoadID"] = roadid;
                            thisrow["x"] = Math.Round(x, 1);
                            thisrow["y"] = Math.Round(y, 1);
                            thisrow["longtitude"] = 0;
                            thisrow["latitude"] = 0;
                            thisrow["gxid"] = 0;
                            thisrow["gyid"] = 0;
                            thisrow["RecePowerDbm"] = 0;
                            dtable.Rows.Add(thisrow);
                        }
                    }
                }
                else
                {
                    double k = dx / dy;
                    double ddx = dis * k;
                    if (x1 > x2)
                        ddx = -ddx;

                    if (y1 < y2)
                    {
                        for (double y = y1, x = x1; y < y2; y += dis, x += ddx)
                        {
                            DateTime dt = DateTime.Now;
                            System.Data.DataRow thisrow = dtable.NewRow();
                            thisrow["id"] = id++;
                            thisrow["dateTime"] = System.Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                            thisrow["RoadID"] = roadid;
                            thisrow["x"] = Math.Round(x, 1);
                            thisrow["y"] = Math.Round(y, 1);
                            thisrow["longtitude"] = 0;
                            thisrow["latitude"] = 0;
                            thisrow["gxid"] = 0;
                            thisrow["gyid"] = 0;
                            thisrow["RecePowerDbm"] = 0;
                            dtable.Rows.Add(thisrow);
                        }
                    }
                    else
                    {
                        for (double y = y1, x = x1; y > y2; y -= dis, x += ddx)
                        {
                            DateTime dt = DateTime.Now;
                            System.Data.DataRow thisrow = dtable.NewRow();
                            thisrow["id"] = id++;
                            thisrow["dateTime"] = System.Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                            thisrow["RoadID"] = roadid;
                            thisrow["x"] = Math.Round(x, 1);
                            thisrow["y"] = Math.Round(y, 1);
                            thisrow["longtitude"] = 0;
                            thisrow["latitude"] = 0;
                            thisrow["gxid"] = 0;
                            thisrow["gyid"] = 0;
                            thisrow["RecePowerDbm"] = 0;
                            dtable.Rows.Add(thisrow);
                        }
                    }
                }
            }
        }

        // 虚拟路测场强填写
        public static void DTStrength()
        {
            // 读取原始路测
            DataTable tb1 = IbatisHelper.ExecuteQueryForDataTable("getDT1", null);
            int n = tb1.Rows.Count;
            List<DT> tbDT = new List<DT>();
            for (int i = 0; i < tb1.Rows.Count; i++)
            {
                DT dt = new DT();
                dt.id = Convert.ToInt32(tb1.Rows[i]["id"].ToString());
                dt.roadID = Convert.ToInt32(tb1.Rows[i]["roadID"].ToString());
                dt.ci = Convert.ToInt32(tb1.Rows[i]["ci"].ToString());
                //dt.longitude = Convert.ToDouble(tb1.Rows[i]["longtitude"].ToString());
                //dt.latitude = Convert.ToDouble(tb1.Rows[i]["latitude"].ToString());
                dt.x = Convert.ToDouble(tb1.Rows[i]["x"].ToString());
                dt.y = Convert.ToDouble(tb1.Rows[i]["y"].ToString());
                tbDT.Add(dt);
            }

            // 得到网格编号，写入数据库
            System.Data.DataTable tb = new System.Data.DataTable();
            tb.Columns.Add("id");
            tb.Columns.Add("dateTime");
            tb.Columns.Add("GXID");
            tb.Columns.Add("GYID");
            tb.Columns.Add("ci");
            tb.Columns.Add("RoadID");
            tb.Columns.Add("x");
            tb.Columns.Add("y");
            tb.Columns.Add("longtitude");
            tb.Columns.Add("latitude");
            tb.Columns.Add("RecePowerDbm");


            int xid = 0, yid = 0;
            for (int i = 0; i < tbDT.Count; i++)
            {
                GridHelper.getInstance().XYToGGrid(tbDT[i].x, tbDT[i].y, ref xid, ref yid); // 网格编号

                IPoint p = new PointClass();
                p.X = tbDT[i].x;
                p.Y = tbDT[i].y;
                p.Z = 0;
                PointConvert.Instance.GetGeoPoint(p);

                DateTime dt = DateTime.Now;

                System.Data.DataRow thisrow = tb.NewRow();
                thisrow["id"] = tbDT[i].id;
                thisrow["dateTime"] = dt.ToLocalTime().ToString();
                thisrow["GXID"] = xid;
                thisrow["GYID"] = yid;
                thisrow["ci"] = tbDT[i].ci;
                thisrow["RoadID"] = tbDT[i].roadID;
                thisrow["x"] = tbDT[i].x;
                thisrow["y"] = tbDT[i].y;
                thisrow["longtitude"] = Math.Round(p.X, 6);
                thisrow["latitude"] = Math.Round(p.Y, 6);
                thisrow["RecePowerDbm"] = 0;
                tb.Rows.Add(thisrow);
            }

            // 删除原始路测
            IbatisHelper.ExecuteDelete("DeleteDT", null);

            using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
            {
                bcp.BatchSize = tb.Rows.Count;
                bcp.BulkCopyTimeout = 1000;
                bcp.DestinationTableName = "tbDT";
                bcp.WriteToServer(tb);
                bcp.Close();
            }
            tb.Clear();

            // 得到网格对应的路测数据
            IbatisHelper.ExecuteUpdate("UpdateDT1", null);
            IbatisHelper.ExecuteUpdate("UpdateDT2", null);
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            DataImport dtimpt = new DataImport();
            dtimpt.Show();
        }
    }

    class DT
    {
        public int id;
        public int roadID;
        public double x;
        public double y;
        public int ci;
        //public double longitude;
        //public double latitude;
        //public int gxid;
        //public int gyid;
        //public float recePwr;
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
