using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LTE.InternalInterference.Grid;
using System.Data;
using LTE.DB;
using LTE.GIS;

namespace LTE.WebAPI.Models
{
    // 建筑物底边平滑---贾英杰
    public class SmoothBuildingVertexModel
    {
        /// <summary>
        /// 获取建筑物底面平滑后的点集（大地坐标）
        /// 3个策略:
        /// 策略1: 斜线相对变化率
        /// 策略2: 累计边的距离
        /// 策略3: 确保线段之间不发生交叉
        double relativeMutationRate = 0.8;      // 策略1的控制参数
        double scaled_size = 0.02;              // 策略2的控制参数
        double path_low_bound = 1;              // 策略2的控制参数
        double constraint_scaled_size = 0.01;   // 策略3的控制参数
        double constraint_low_bound = 0.02;     // 策略3的控制参数

        public Result smoothBuildingPoints()
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

            try
            {
                IbatisHelper.ExecuteDelete("DeleteBuildingVertex", null);
            }
            catch(Exception e)
            {
                return new Result(false, e.ToString());
            }

            for (int i = minBid; i <= maxBid; i++)
            {
                List<LTE.Geometric.Point> bpoints = BuildingGrid3D.getBuildingVertexOriginal(i);

                List<LTE.Geometric.Point> ps = Process(ref bpoints);  // 2018-05-08
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

            return new Result(true);
        }

        public double cross(LTE.Geometric.Point p1,
                            LTE.Geometric.Point p2,
                            LTE.Geometric.Point p3) //跨立实验
        {
            double x1 = p2.X - p1.X;
            double y1 = p2.Y - p1.Y;
            double x2 = p3.X - p1.X;
            double y2 = p3.Y - p1.Y;
            return x1 * y2 - x2 * y1;
        }

        // ref https://blog.csdn.net/s0rose/article/details/78831570
        // 判断两线段是否相交
        public bool IsIntersec(LTE.Geometric.Point p1,
                               LTE.Geometric.Point p2,
                               LTE.Geometric.Point p3,
                               LTE.Geometric.Point p4) //判断两线段是否相交
        {
            bool D = false;

            //快速排斥，以l1、l2为对角线的矩形必相交，否则两线段不相交
            if (Math.Max(p1.X, p2.X) >= Math.Min(p3.X, p4.X)    //矩形1最右端大于矩形2最左端
                && Math.Max(p3.X, p4.X) >= Math.Min(p1.X, p2.X)   //矩形2最右端大于矩形最左端
                && Math.Max(p1.Y, p2.Y) >= Math.Min(p3.Y, p4.Y)   //矩形1最高端大于矩形最低端
                && Math.Max(p3.Y, p4.Y) >= Math.Min(p1.Y, p2.Y)) //矩形2最高端大于矩形最低端
            {

                //若通过快速排斥则进行跨立实验
                if (cross(p1, p2, p3) * cross(p1, p2, p4) <= 0 &&
                    cross(p3, p4, p1) * cross(p3, p4, p2) <= 0)
                {
                    D = true;
                }
                else
                {
                    D = false;
                }
            }
            else
            {
                D = false;
            }
            return D;
        }

        bool samePt(LTE.Geometric.Point p1, LTE.Geometric.Point p2)
        {
            return Math.Abs(p1.X - p2.X) <= 0.00000001 && Math.Abs(p1.Y - p2.Y) <= 0.00000001;
        }

        // 判断点p3和p4构成的线段是否和res_Points中的点构成的线段发生交叉
        // 例如: res_Pints=[p_0,p_1,p_2,p_3] 一共4个点
        // 则本函数判断 （p3,p4) 构成的线段是否和 (p_0,p_1),(p_1,p_2),(p_2,p_3)这3条线段发生线段交叉
        public bool checkValid(LTE.Geometric.Point p3,
                               LTE.Geometric.Point p4,
                               List<LTE.Geometric.Point> res_Points,
                               int n)
        {
            if (samePt(p4, res_Points[0]))
                return true;

            for (int idx = 0; idx < n - 2; ++idx)
            {
                if (IsIntersec(res_Points[idx], res_Points[idx + 1], p3, p4))
                    return false;
            }
            return true;
        }

        double distance(double y1, double y2, double x1, double x2)
        {
            return Math.Sqrt(Math.Pow(y1 - y2, 2) + Math.Pow(x1 - x2, 2));
        }

        double xmult(LTE.Geometric.Point p1, LTE.Geometric.Point p2, LTE.Geometric.Point p0)
        {
            return (p1.X - p0.X) * (p2.Y - p0.Y) - (p2.X - p0.X) * (p1.Y - p0.Y);
        }

        double Point2Line(LTE.Geometric.Point p0, LTE.Geometric.Point p1, LTE.Geometric.Point p2)
        {
            double den = distance(p1.Y, p2.Y, p1.X, p2.X);
            if (den < 0.00001)
                return 0.0;
            return Math.Abs(xmult(p0, p1, p2) / den);

        }

        public bool isConstraint(int cur_idx, int last_idx, double constrantBound, ref List<LTE.Geometric.Point> Vertixs)
        {
            bool local_flag = true;
            for (int i = last_idx + 1; i < cur_idx; ++i)
            {
                double d = Point2Line(Vertixs[i], Vertixs[last_idx], Vertixs[cur_idx]);
                if (Math.Abs(d) > constrantBound)
                {
                    local_flag = false;
                    break;
                }
            }
            return local_flag;
        }

        public List<LTE.Geometric.Point> Process(ref List<LTE.Geometric.Point> Vertixs)
        {
            //if (Vertixs.Count < 40)
            //    return Vertixs;

            List<LTE.Geometric.Point> res_Points = new List<Geometric.Point>();

            int last_idx = 0;  //用来保存当前 res_Points 中最后一个点在原始点集中的序号
            double global_path_sum = 0;

            // 用于求取整个建筑物的坐标极限
            double x_min = 10000000;
            double x_max = -10000000;
            double y_min = 10000000;
            double y_max = -10000000;
            for (int i = 0; i < Vertixs.Count; ++i)
            {
                if (x_min > Vertixs[i].X)
                    x_min = Vertixs[i].X;
                if (x_max < Vertixs[i].X)
                    x_max = Vertixs[i].X;
                if (y_min > Vertixs[i].Y)
                    y_min = Vertixs[i].Y;
                if (y_max < Vertixs[i].Y)
                    y_max = Vertixs[i].Y;
            }

            double pathBound = Math.Max(scaled_size * distance(y_min, y_max, x_min, x_max), path_low_bound);
            double constrantBound = Math.Max(constraint_scaled_size * distance(y_min, y_max, x_min, x_max), constraint_low_bound);

            for (int idx = 1; idx < Vertixs.Count - 1; ++idx)
            {
                if (Math.Abs(Vertixs[idx].X - Vertixs[idx - 1].X) < 0.0001)
                    continue;
                if (Math.Abs(Vertixs[idx + 1].X - Vertixs[idx].X) < 0.0001)
                    continue;

                double mutation;
                double k1 = (Vertixs[idx].Y - Vertixs[idx - 1].Y) / (Vertixs[idx].X - Vertixs[idx - 1].X);
                double k2 = (Vertixs[idx + 1].Y - Vertixs[idx].Y) / (Vertixs[idx + 1].X - Vertixs[idx].X);
                if (Math.Abs(k1) < 0.0000001)
                    mutation = relativeMutationRate + 1;
                else
                    mutation = (k2 - k1) / (k1);

                double l1 = distance(Vertixs[idx].Y, Vertixs[idx - 1].Y, Vertixs[idx].X, Vertixs[idx - 1].X);
                if (Math.Abs(mutation) <= relativeMutationRate)
                {
                    global_path_sum = global_path_sum + l1;

                    if (isConstraint(idx, last_idx, constrantBound, ref Vertixs))
                        continue;
                    else
                    {
                        if (res_Points.Count <= 3)
                        {
                            res_Points.Add(new Geometric.Point(Vertixs[idx - 1].X, Vertixs[idx - 1].Y, Vertixs[idx - 1].Z));
                            last_idx = idx - 1;  // update last_idx
                            continue;
                        }
                        else
                        {
                            if (checkValid(res_Points[res_Points.Count - 1], Vertixs[idx - 1], res_Points, res_Points.Count))
                            {
                                res_Points.Add(new Geometric.Point(Vertixs[idx - 1].X, Vertixs[idx - 1].Y, Vertixs[idx - 1].Z));
                                last_idx = idx - 1; // update last_idx;
                            }
                            else
                            {
                                bool local_flag = false;
                                for (int k = last_idx + 1; k < idx; ++k)
                                {
                                    res_Points.Add(new Geometric.Point(Vertixs[k].X, Vertixs[k].Y, Vertixs[k].Z));
                                    bool bool_1 = checkValid(res_Points[res_Points.Count - 1], Vertixs[idx], res_Points, res_Points.Count);
                                    bool bool_2 = checkValid(res_Points[res_Points.Count - 2], Vertixs[Vertixs.Count - 1], res_Points, res_Points.Count - 1);
                                    if (bool_1 && bool_2)
                                    {
                                        local_flag = true;
                                        last_idx = k;
                                        break;
                                    }
                                    res_Points.RemoveAt(res_Points.Count - 1);
                                }
                                if (local_flag)
                                {
                                    res_Points.Add(new Geometric.Point(Vertixs[idx].X, Vertixs[idx].Y, Vertixs[idx].Z));
                                    last_idx = idx;
                                }
                                else
                                {
                                    for (int k = last_idx + 1; k <= idx; ++k) // 压入所有中间节点和当前考察节点
                                        res_Points.Add(new Geometric.Point(Vertixs[k].X, Vertixs[k].Y, Vertixs[k].Z));
                                    last_idx = idx;
                                }
                            }
                        }
                        global_path_sum = 0;
                    }
                }
                else
                {
                    global_path_sum = global_path_sum + l1;
                    #region
                    if (global_path_sum >= pathBound)
                    {
                        if (res_Points.Count <= 3)
                        {
                            res_Points.Add(new Geometric.Point(Vertixs[idx].X, Vertixs[idx].Y, Vertixs[idx].Z));
                            last_idx = idx;  // update last_idx
                        }
                        else
                        {
                            if (checkValid(res_Points[res_Points.Count - 1], Vertixs[idx], res_Points, res_Points.Count))
                            {
                                res_Points.Add(new Geometric.Point(Vertixs[idx].X, Vertixs[idx].Y, Vertixs[idx].Z));
                                last_idx = idx; // update last_idx;
                            }
                            else
                            {
                                // Draw(res_Points, False, True)
                                // just generate 1 point
                                bool local_flag = false;
                                for (int k = last_idx + 1; k < idx; ++k)
                                {
                                    res_Points.Add(new Geometric.Point(Vertixs[k].X, Vertixs[k].Y, Vertixs[k].Z));
                                    bool bool_1 = checkValid(res_Points[res_Points.Count - 1], Vertixs[idx], res_Points, res_Points.Count);
                                    bool bool_2 = checkValid(res_Points[res_Points.Count - 2], Vertixs[Vertixs.Count - 1], res_Points, res_Points.Count - 1);
                                    if (bool_1 && bool_2)
                                    {
                                        local_flag = true;
                                        last_idx = k;
                                        break;
                                    }
                                    res_Points.RemoveAt(res_Points.Count - 1);
                                }
                                if (local_flag)
                                {
                                    res_Points.Add(new Geometric.Point(Vertixs[idx].X, Vertixs[idx].Y, Vertixs[idx].Z));
                                    last_idx = idx;
                                }
                                else
                                {
                                    for (int k = last_idx + 1; k <= idx; ++k) // 压入所有中间节点和当前考察节点
                                        res_Points.Add(new Geometric.Point(Vertixs[k].X, Vertixs[k].Y, Vertixs[k].Z));
                                    last_idx = idx;
                                }
                            }
                        }
                        global_path_sum = 0;
                    }
                    #endregion
                }
            }
            return res_Points;
        }
    }
}