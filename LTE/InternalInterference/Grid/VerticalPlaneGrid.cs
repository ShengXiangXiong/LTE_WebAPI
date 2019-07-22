using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
//using LTE.Model;
using LTE.DB;
using LTE.GIS;
using LTE.Geometric;

namespace LTE.InternalInterference.Grid
{
    public static class VerticalPlaneGrid
    {
        private static double vplanegridlength = GridHelper.getInstance().getGHeight();

        /// <summary>
        /// 根据多边形，构造水平棱边棱上的点
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        public static List<Point> GetHEdgePoints(Point point1, Point point2, double pMargin, double height)
        {
            List<Point> edgePoints = new List<Point>();

            #region  也可以用，但比较疏
            //double x1 = point1.X;
            //double y1 = point1.Y;
            //double x2 = point2.X;
            //double y2 = point2.Y;

            //double dx = Math.Abs(x2 - x1);
            //double dy = Math.Abs(y2 - y1);


            //if (dy < 2)
            //{
            //    double y = (y1 + y2) / 2.0;
            //    if (x2 > x1)
            //    {
            //        for (double x = x1; x < x2; x += pMargin)
            //            edgePoints.Add(new Point(x, y, height));
            //    }
            //    else
            //    {
            //        for (double x = x1; x > x2; x -= pMargin)
            //            edgePoints.Add(new Point(x, y, height));
            //    }
            //}
            //else if (dx < 2)
            //{
            //    double x = (x1 + x2) / 2.0;
            //    if (y2 > y1)
            //    {
            //        for (double y = y1; y < y2; y += pMargin)
            //            edgePoints.Add(new Point(x, y, height));
            //    }
            //    else
            //    {
            //        for (double y = y1; y > y2; y -= pMargin)
            //            edgePoints.Add(new Point(x, y, height));
            //    }
            //}
            //else
            //{
            //    if (dx > dy)
            //    {
            //        double k = dy / dx;
            //        double ddy = pMargin * k;
            //        if (y1 > y2)
            //            ddy = -ddy;

            //        if (x1 < x2)
            //        {
            //            for (double x = x1, y = y1; x < x2; x += pMargin, y += ddy)
            //                edgePoints.Add(new Point(x, y, height));
            //        }
            //        else
            //        {
            //            for (double x = x1, y = y1; x > x2; x -= pMargin, y += ddy)
            //                edgePoints.Add(new Point(x, y, height)); ;
            //        }
            //    }
            //    else
            //    {
            //        double k = dx / dy;
            //        double ddx = pMargin * k;
            //        if (x1 > x2)
            //            ddx = -ddx;

            //        if (y1 < y2)
            //        {
            //            for (double y = y1, x = x1; y < y2; y += pMargin, x += ddx)
            //                edgePoints.Add(new Point(x, y, height));
            //        }
            //        else
            //        {
            //            for (double y = y1, x = x1; y > y2; y -= pMargin, x += ddx)
            //                edgePoints.Add(new Point(x, y, height));
            //        }
            //    }
            //}

            //return edgePoints;
            #endregion

            double len = Math.Sqrt(Math.Pow(point1.X - point2.X, 2) + Math.Pow(point1.Y - point2.Y, 2));  // 线段长度

            Vector2D a = new Vector2D(point1.X, point1.Y);
            Vector2D b = new Vector2D(point2.X, point2.Y);
            Line2D line = new Line2D(a, b);

            for (double i = pMargin; i < len - pMargin; i += pMargin)  // line = S + Ct
            {
                Point p = new Point();
                p.X = line.S.x + i * line.C.x;
                p.Y = line.S.y + i * line.C.y;
                p.Z = height;
                edgePoints.Add(p);
            }

            return edgePoints;
        }

        /// <summary>
        /// 根据多边形，构造立面的栅格中心点，不包括棱上的点
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        public static List<Point> GetCenterPoints(Point point1, Point point2, double pMargin)
        {
            //水平栅格数目
            double LineLength = Math.Sqrt((point1.X - point2.X) * (point1.X - point2.X) + (point1.Y - point2.Y) * (point1.Y - point2.Y));
            int PGridNum = (int)Math.Ceiling(LineLength / pMargin);//应该向上取整

            Point pleft = point1.X < point2.X ? point1 : point2;
            Point pright = point1.X < point2.X ? point2 : point1;

            //中心点坐标列表
            List<Point> centerPoints = new List<Point>();
            Point temp;

            //保证p1在p2的左边
            double sin = (pright.Y - pleft.Y) / LineLength;
            double cos = (pright.X - pleft.X) / LineLength;

            for (int j = 0; j < PGridNum; j++)
            {
                temp = new Point();
                temp.X = pleft.X + pMargin * (j + 0.5) * cos;
                temp.Y = pleft.Y + pMargin * (j + 0.5) * sin;
                if (temp.X > pright.X)
                {
                    if (j < 1)
                    {//尚未选出入射点，选取线段中点
                        temp.X = (pleft.X + pright.X) / 2;
                        temp.Y = (pleft.Y + pright.Y) / 2;
                        temp.Z = 0;
                        centerPoints.Add(temp);
                    }
                    break;
                }
                else
                {
                    temp.Z = 0;
                    centerPoints.Add(temp);
                }
            }
            return centerPoints;
        }

        /// <summary>
        /// 判断线段p1p2和q1q2是否相交
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        private static bool isInsert(Point p1, Point p2, Point q1, Point q2)
        {
            if (Math.Max(p1.X, p2.X) >= Math.Min(q1.X, q2.X) &&
                Math.Max(q1.X, q2.X) >= Math.Min(p1.X, p2.X) &&
                Math.Max(p1.Y, p2.Y) >= Math.Min(q1.Y, q2.Y) &&
                Math.Max(q1.Y, q2.Y) >= Math.Min(p1.Y, p2.Y) &&
                VectorMulpti(p1, p2, q1) * VectorMulpti(p1, p2, q2) <= 0 &&
                VectorMulpti(q1, q2, p1) * VectorMulpti(q1, q2, p2) <= 0)
                return true;
            return false;
        }

        /// <summary>
        /// 平面向量p1p2叉乘p1p3, x1*y2 - x2*y1
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <returns></returns>
        private static double VectorMulpti(Point p1, Point p2, Point p3)
        {
            double t = (p2.X - p1.X) * (p3.Y - p1.Y) - (p3.X - p1.X) * (p2.Y - p1.Y);
            return Math.Round(t, 6);
        }

        /// <summary>
        /// 获取建筑物相对原点可见的点坐标
        /// </summary>
        /// <param name="p"></param>
        /// <param name="buildingid"></param>
        /// <returns></returns>
        private static List<Point> getBuildingVPlanePoints(Point p, int buildingid, double pMargin)
        {
            double bheight = BuildingGrid3D.getBuildingHeight(buildingid);
            double bAltidue = BuildingGrid3D.getBuildingAltitude(buildingid); // 地形

            List<Point> bpoints = BuildingGrid3D.getBuildingVertex(buildingid);

            int vnum = (int)Math.Ceiling(bheight / vplanegridlength);
            int vnumBase = (int)Math.Ceiling(bAltidue / vplanegridlength); // 地形

            List<Point> ret = new List<Point>();

            for (int j = 0, k = bpoints.Count - 1, cnt = bpoints.Count; j < cnt; k = j++)
            {
                List<Point> pts = GetCenterPoints(bpoints[k], bpoints[j], pMargin);

                for (int m = 0; m < pts.Count; m++)
                {
                    //如果是凸多边形，则可以判断边的中点即可，如果是凹多边形，则需要都判断
                    //可以在数据库级别存储建筑物定点是计算出建筑物的凹凸性，那么此处就可以进行优化
                    if (isCover(p, pts[m], bpoints, k)) continue;

                    // 地形
                    for (int i = vnumBase; i < vnum; i++) 
                    {
                        ret.Add(new Point(pts[m].X, pts[m].Y, (i + 0.5) * vplanegridlength));
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// 判断一条线段是否与多边形相交，k表示要忽略的多边形的某条边的起点编号
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="points"></param>
        /// <param name="k">k表示要忽略的多边形的某条边的起点编号</param>
        /// <returns></returns>
        private static bool isCover(Point start, Point end, List<Point> points, int k)
        {
            for (int i = 0, j = points.Count - 1; i < points.Count; j = i++)
            {
                if (j == k) continue;
                if (isInsert(start, end, points[j], points[i]))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 构造建筑物侧面点的哈希表
        /// </summary>
        /// <param name="source"></param>
        /// <param name="buildingIDs"></param>
        /// <returns></returns>
        public static Hashtable CreateVerticalGridHash(Point source, List<int> buildingIDs, double gridLength)
        {
            Hashtable ht = new Hashtable();
            for (int i = 0; i < buildingIDs.Count; i++)
            {
                int bid = buildingIDs[i];
                ht.Add(bid, getBuildingVPlanePoints(source, bid, gridLength));
            }
            return ht;
        }

        /// <summary>
        /// 返回所有建筑物相对于原点可见的侧面点集合
        /// </summary>
        /// <param name="source"></param>
        /// <param name="buildingIDs"></param>
        /// <returns></returns>
        public static List<Point> GetAllVerticalGrid(Point source, List<int> buildingIDs, double gridLength)
        {
            List<Point> ret = new List<Point>();
            for (int i = 0; i < buildingIDs.Count; i++)
            {
                int bid = buildingIDs[i];
                ret.AddRange(getBuildingVPlanePoints(source, bid, gridLength));
            }
            return ret;
        }

    }
}
