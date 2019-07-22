using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LTE.Geometric;
using LTE.DB;
using LTE.GIS;

namespace LTE.InternalInterference.Grid
{
    /// <summary>
    /// 地面栅格相关计算，包括获取覆盖区域内的地面栅格中心，获取栅格的中心点
    /// </summary>
    public class GroundGrid
    {
        public static Dictionary<string, Point> ggrids;
        private static double MinGxid;
        private static double MinGyid;
        private static double MaxGxid;
        private static double MaxGyid;

        public static void setBound(double mingxid, double mingyid, double maxgxid, double maxgyid)
        {
            MinGxid = mingxid;
            MinGyid = mingyid;
            MaxGxid = maxgxid;
            MaxGyid = maxgyid;
        }

        public static int constructGGrids()
        {
            //Console.WriteLine("{0}", 1);
            ggrids = new Dictionary<string, Point>();

            Hashtable ht = new Hashtable();
            ht["minGXID"] = MinGxid;
            ht["maxGXID"] = MaxGxid;
            ht["minGYID"] = MinGyid;
            ht["maxGYID"] = MaxGyid;
            //Console.WriteLine("{0}", MinGxid);

            DataTable grids = IbatisHelper.ExecuteQueryForDataTable("getGroundGridsCenterPre", ht);
            //Console.WriteLine("{0}", grids.Rows.Count);
            double x, y;
            int gxid, gyid;
            string key;
            for (int i = 0, cnt = grids.Rows.Count; i < cnt; i++)
            {
                gxid = Convert.ToInt32(grids.Rows[i]["GXID"]);
                gyid = Convert.ToInt32(grids.Rows[i]["GYID"]);
                x = Convert.ToDouble(grids.Rows[i]["CX"]);
                y = Convert.ToDouble(grids.Rows[i]["CY"]);
                key = string.Format("{0},{1}", gxid, gyid);
                ggrids.Add(key, new Point(x, y, 0));
            }
            return ggrids.Count;
        }
        /// <summary>
        /// 获取中心点在范围内的地面栅格中心点
        /// </summary> 
        /// <returns></returns>
        public static int constructGGrids(ref Geometric.Point p1, ref Geometric.Point p2,
            ref Geometric.Point p3, ref Geometric.Point p4)
        {
            //Console.WriteLine("{0}", 1);
            ggrids = new Dictionary<string, Point>();

            Hashtable ht = new Hashtable();

            Grid3D gid1 = new Grid3D(), gid2 = new Grid3D(), gid3 = new Grid3D(), gid4 = new Grid3D();
            GridHelper.getInstance().PointXYZToGrid3D1(p1, ref gid1);
            GridHelper.getInstance().PointXYZToGrid3D1(p2, ref gid2);
            GridHelper.getInstance().PointXYZToGrid3D1(p3, ref gid3);
            GridHelper.getInstance().PointXYZToGrid3D1(p4, ref gid4);

            //Console.WriteLine("from: {0}", from * 180 / Math.PI);
            //Console.WriteLine("to: {0}", to * 180 / Math.PI);
            //Console.WriteLine("alpha: {0}", alpha * 180 / Math.PI);
            //Console.WriteLine("theta: {0}", theta * 180 / Math.PI);

            ht["x1"] = gid1.gxid;
            ht["x2"] = gid2.gxid;
            ht["x3"] = gid3.gxid;
            ht["x4"] = gid4.gxid;
            ht["y1"] = gid1.gyid;
            ht["y2"] = gid2.gyid;
            ht["y3"] = gid3.gyid;
            ht["y4"] = gid4.gyid;
            DataTable grids = IbatisHelper.ExecuteQueryForDataTable("getGroundGridsCenter", ht);
            //Console.WriteLine("{0}", grids.Rows.Count);
            double x, y;
            int gxid, gyid;
            string key;
            for (int i = 0, cnt = grids.Rows.Count; i < cnt; i++)
            {
                gxid = Convert.ToInt32(grids.Rows[i]["GXID"]);
                gyid = Convert.ToInt32(grids.Rows[i]["GYID"]);
                x = Convert.ToDouble(grids.Rows[i]["CX"]);
                y = Convert.ToDouble(grids.Rows[i]["CY"]);
                key = string.Format("{0},{1}", gxid, gyid);
                ggrids.Add(key, new Point(x, y, 0));
            }
            return ggrids.Count;
        }

        /// <summary>
        /// 获取扇区内的点  2018.12.18
        /// </summary>
        /// <param name="source"></param>
        /// <param name="distance">单位米</param>
        /// <param name="fromAngle">方位角</param>
        /// <param name="toAngle">方位角</param>
        /// <param name="DisAngle">需要排除的区域,角度坐标为极坐标, 如果没有要排除的区域，则传null</param>
        /// <returns></returns>
        public static List<Point> getPointBySector(Point source, double distance, double fromAngle, double toAngle, double interval)
        {
            double minX = 0, minY = 0, maxX = 0, maxY = 0;
            GridHelper.getInstance().getMinXY(ref minX, ref minY);
            GridHelper.getInstance().getMaxXY(ref maxX, ref maxY);

            //边界，大地坐标
            double minx = Math.Max(minX, source.X - distance);
            double miny = Math.Max(minY, source.Y - distance);
            double maxx = Math.Min(maxX, source.X + distance);
            double maxy = Math.Min(maxY, source.Y + distance);

            //double minx = source.X - distance;
            //double miny = source.Y - distance;
            //double maxx = source.X + distance;
            //double maxy = source.Y + distance;
            //便于比较边缘地带
            distance += 0.1;

            double from = GeometricUtilities.ConvertGeometricArithmeticAngle(toAngle + 1);
            double to = GeometricUtilities.ConvertGeometricArithmeticAngle(fromAngle - 1);

            from = GeometricUtilities.GetRadians(from);
            to = GeometricUtilities.GetRadians(to);

            List<Point> ret = new List<Point>();

            for (double x = minx; x < maxx; x += interval)
            {
                for (double y = miny; y < maxy; y += interval)
                {
                    Point p = new Point(x, y, 0);
                    Polar pr = GeometricUtilities.getPolarCoord(source, p);

                    // 将位于扇区覆盖范围内的点加进来   
                    if (pr.r < distance && isInRange(pr.theta, from, to))
                    {
                        ret.Add(p);
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// 根据地面栅格ID获取范围内的地面栅格中心点
        /// </summary>
        /// <param name="gxid"></param>
        /// <param name="gyid"></param>
        /// <returns></returns>
        public static Point getGGridCenter(int gxid, int gyid)
        {
            string key = string.Format("{0},{1}", gxid, gyid);
            return ggrids.ContainsKey(key) ? ggrids[key] : null;
        }

        /// <summary>
        /// 根据建筑物栅格ID获取范围内的地面栅格中心点
        /// </summary>
        /// <param name="gxid"></param>
        /// <param name="gyid"></param>
        /// <param name="gzid"></param>
        /// <returns></returns>
        public static Point getBGridCenter(int gxid, int gyid, int gzid)
        {
            string key = string.Format("{0},{1}", gxid, gyid);
            if (!ggrids.ContainsKey(key))
            {
                return null;
            }
            Point ret = ggrids[key];
            ret.Z = GridHelper.getInstance().getGHeight() * (gzid - 1) + GridHelper.getInstance().getGBaseHeight();
            return ret;
        }

        /// <summary>
        /// 获取扇区与地面栅格交集内的栅格中心点
        /// </summary>
        /// <param name="source"></param>
        /// <param name="distance">单位米</param>
        /// <param name="fromAngle">方位角</param>
        /// <param name="toAngle">方位角</param>
        /// <param name="DisAngle">需要排除的区域,角度坐标为极坐标, 如果没有要排除的区域，则传null</param>
        /// <returns></returns>
        public static List<Point> getGGridCenterBySector(Point source, double distance, double fromAngle, double toAngle, List<TriangleBound> DisAngle)
        {
            double minX = 0, minY = 0, maxX = 0, maxY = 0;
            GridHelper.getInstance().getMinXY(ref minX, ref minY);
            GridHelper.getInstance().getMaxXY(ref maxX, ref maxY);

            //边界，大地坐标
            double minx = Math.Max(minX, source.X - distance);
            double miny = Math.Max(minY, source.Y - distance);
            double maxx = Math.Min(maxX, source.X + distance);
            double maxy = Math.Min(maxY, source.Y + distance);

            //double minx = source.X - distance;
            //double miny = source.Y - distance;
            //double maxx = source.X + distance;
            //double maxy = source.Y + distance;
            //便于比较边缘地带
            distance += 0.1;

            double from = GeometricUtilities.ConvertGeometricArithmeticAngle(toAngle + 1);
            double to = GeometricUtilities.ConvertGeometricArithmeticAngle(fromAngle - 1);

            from = GeometricUtilities.GetRadians(from);
            to = GeometricUtilities.GetRadians(to);

            List<Point> ret = new List<Point>();
            Point p;
            Polar pr;

            foreach (KeyValuePair<string, Point> kv in ggrids)
            {
                p = kv.Value;
                if (p.X > minx && p.X < maxx && p.Y > miny && p.Y < maxy)
                {
                    pr = GeometricUtilities.getPolarCoord(source, p);

                    // 将位于扇区覆盖范围内，且不在建筑物内的地面栅格加进来   
                    if (pr.r < distance && isInRange(pr.theta, from, to))//&& !isInBuilding(p, ref DisAngle))
                    //if (pr.r < distance && isInRange(pr.theta, from, to) && !isInRange(pr, p, source, ref DisAngle))
                    {
                        ret.Add(p);
                    }
                }
            }
            return ret;
        }

        private static bool isInBuilding(Point p, ref List<TriangleBound> DisAngle)
        {
            if (DisAngle == null)
                return false;
            int cnt = DisAngle.Count;

            bool isEdge;
            int startIndex = 0;
            for (int i = 0; i < cnt; i++)
            {
                List<Point> vertex = BuildingGrid3D.getBuildingVertex(DisAngle[i].buildingid);
                if (GeometricUtilities.PointInPolygon(vertex.ToArray(), p, out isEdge, ref startIndex))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 地面栅格是否位于被建筑物遮挡的范围内
        /// </summary>
        /// <param name="pr"></param>
        /// <param name="DisAngle"></param>
        /// <returns></returns>
        private static bool isInRange(Polar pr, Point p, Point source, ref List<TriangleBound> DisAngle)
        {
            if (DisAngle == null)
            {
                return false;
            }
            int cnt = DisAngle.Count;
            for (int i = 0; i < cnt; i++)
            {
                if (pr.r > DisAngle[i].distance && isInRange(pr.theta, DisAngle[i].minTheta, DisAngle[i].maxTheta))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 极点是否在范围内
        /// </summary>
        /// <param name="pr"></param>
        /// <param name="DisAngle"></param>
        /// <returns></returns>
        private static bool isInRange(Polar pr, ref List<TriangleBound> DisAngle)
        {
            if (DisAngle == null)
            {
                return false;
            }
            int cnt = DisAngle.Count;
            for (int i = 0; i < cnt; i++)
            {
                if (pr.r < DisAngle[i].distance)
                {
                    break;
                }
                if (isInRange(pr.theta, DisAngle[i].minTheta, DisAngle[i].maxTheta))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 判断angle是否在from和to的角度内，二者有可能大小相反，大小相反的情况是角度位于x轴两侧
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        private static bool isInRange(double angle, double from, double to)
        {
            return from < to ? (angle > from && angle < to) : (angle < to || angle > from);
        }
    }
}
