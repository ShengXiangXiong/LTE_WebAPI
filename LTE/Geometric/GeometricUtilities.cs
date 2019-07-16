using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LTE.GIS;
using LTE.InternalInterference;

namespace LTE.Geometric
{
    public static class GeometricUtilities
    {
        /// <summary>
        /// convert between a geographic rotation angle and an arithmetic rotation angle
        /// </summary>
        /// <param name="oldAngle">input parameter is measured degrees</param>
        /// <returns></returns>
        public static double ConvertGeometricArithmeticAngle(double oldAngle)
        {
            return (450 - oldAngle) % 360;
            //return 360 - ((oldAngle + 270) % 360);
        }
        /// <summary>
        /// convert  degrees to radians
        /// </summary>
        /// <param name="decimalDegrees">input parameter is measured degrees</param>
        /// <returns>radians</returns>
        public static double GetRadians(double decimalDegrees)
        {
            return decimalDegrees * (Math.PI / 180);
        }
        /// <summary>
        /// convert radians to degrees
        /// </summary>
        /// <param name="radians">input parameter is measured radians</param>
        /// <returns>degrees</returns>
        public static double GetDegrees(double radians)
        {
            return radians * (180 / Math.PI);
        }
        /// <summary>
        /// 大地坐标
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double GetDistanceOf3DPoints(Point a, Point b)
        {
            return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2) + Math.Pow(a.Z - b.Z, 2));
        }
        /// <summary>
        /// 大地坐标
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double GetDistanceOf2DPoints(Point a, Point b)
        {
            return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }

        /// <summary>
        /// 一点相对另一点的极坐标
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static Polar getPolarCoord(Point source, Point target)
        {
            double len = Math.Sqrt(Math.Pow(target.X - source.X, 2) + Math.Pow(target.Y - source.Y, 2));
            if (len < 0.1)
            {
                //便于计算
                return new Polar(double.MaxValue, 0);
            }
            double sin = (target.Y - source.Y) / len;
            double cos = (target.X - source.X) / len;

            double angle = Math.Asin(sin);
            if (sin >= 0)
            {
                angle = cos < 0 ? Math.PI - angle : angle;
            }
            else
            {
                angle = cos < 0 ? Math.PI - angle : Math.PI * 2 + angle;
            }
            return new Polar(len, angle);
        }

        /// <summary>
        /// 距离点a，一定距离和弧度的点b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="radian">弧度（平面角，非方位角，象限角）</param>
        /// <param name="distance">水平距离</param>
        /// <returns></returns>
        public static Point getPointByRadianDistance(Point p, double radian, double distance)
        {
            double degree = GetDegrees(radian);
            //JWD tp = CJWDHelper.GetJWDB(p.Lng, p.Lat, distance / 1000, ConvertGeometricArithmeticAngle(degree));
            return new Point(p.X + Math.Cos(radian) * distance, p.Y + Math.Sin(radian) * distance, p.Z);
        }

        /// <summary>
        /// 距离点a，一定距离和角度的点b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="radian">角度（平面角，象限角，非方位角）</param>
        /// <param name="distance">水平距离</param>
        /// <returns></returns>
        public static Point getPointByDegreeDistance(Point p, double degree, double distance)
        {
            double radian = GetRadians(degree);
            return new Point(p.X + Math.Cos(radian) * distance, p.Y + Math.Sin(radian) * distance, p.Z);
        }

        /// <summary>
        /// 点在区域中是否可见（现已废弃，替代方法 PointInPolygon）
        /// </summary>
        /// <param name="point"></param>
        /// <param name="pointColl"></param>
        /// <returns></returns>
        public static bool IsVisible_Region(Point point, List<Point> pointColl)
        {
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            System.Drawing.Region region = new System.Drawing.Region();
            List<System.Drawing.Point> pathPoints = new List<System.Drawing.Point>();
            foreach (var pathPoint in pointColl)
            {
                pathPoints.Add(new System.Drawing.Point((int)Math.Round(pathPoint.X), (int)Math.Round(pathPoint.Y)));
            }
            path.AddPolygon(pathPoints.ToArray());
            path.CloseFigure();
            region.MakeEmpty();
            region.Union(path);
            return region.IsVisible(new System.Drawing.Point((int)Math.Round(point.X), (int)Math.Round(point.Y)));
        }

        /// <summary>
        /// 未知方法，（现已废弃，替代方法 PointInPolygon）
        /// </summary>
        /// <param name="point"></param>
        /// <param name="pointColl"></param>
        /// <returns></returns>
        public static bool IsVisible_GraphicsPath(Point point, List<Point> pointColl)
        {
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            List<System.Drawing.Point> pathPoints = new List<System.Drawing.Point>();
            foreach (var pathPoint in pointColl)
            {
                pathPoints.Add(new System.Drawing.Point((int)Math.Round(pathPoint.X), (int)Math.Round(pathPoint.Y)));
            }
            path.AddPolygon(pathPoints.ToArray());
            path.CloseFigure();
            return path.IsVisible(new System.Drawing.Point((int)Math.Round(point.X), (int)Math.Round(point.Y)));
        }

        /// <summary>
        /// 获得两向量的夹角的余弦值
        /// </summary>
        /// <param name="vectorfrom"></param>
        /// <param name="vectorEnd1"></param>
        /// <param name="verctorEnd2"></param>
        /// <returns></returns>
        public static double getCosineOfVector3Ds(Point vectorfrom, Point vectorEnd1, Point vectorEnd2)
        {
            Vector3D vector1 = Vector3D.constructVector(vectorfrom, vectorEnd1);
            Vector3D vector2 = Vector3D.constructVector(vectorfrom, vectorEnd2);
            double cosine = vector1.dotProduct(vector2) / (vector1.Magnitude * vector2.Magnitude);
            return cosine;
        }

        /// <summary>
        /// 获得两向量的夹角（弧度）
        /// </summary>
        /// <param name="vectorfrom"></param>
        /// <param name="vectorEnd1"></param>
        /// <param name="verctorEnd2"></param>
        /// <returns></returns>
        public static double getRadianOfVector3Ds(Point vectorfrom, Point vectorEnd1, Point vectorEnd2)
        {
            return Math.Acos(getCosineOfVector3Ds(vectorfrom, vectorEnd1, vectorEnd2));
        }

        /// <summary>
        /// 获得两向量的夹角（角度）
        /// </summary>
        /// <param name="vectorfrom"></param>
        /// <param name="vectorEnd1"></param>
        /// <param name="verctorEnd2"></param>
        /// <returns></returns>
        public static double getDegreeOfVector3Ds(Point vectorfrom, Point vectorEnd1, Point vectorEnd2)
        {
            double theta = getRadianOfVector3Ds(vectorfrom, vectorEnd1, vectorEnd2);
            return theta * 180 / Math.PI;
        }

        /// <summary>
        /// 获取下倾角，下正上负
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns>下正上负</returns>
        public static double getInclination(Point start, Point end)
        {
            Vector3D vector = Vector3D.constructVector(start, end);
            return vector.Inclination;
        }

        /// <summary>
        /// 返回射线与水平面交点,方位角和下倾角都是角度
        /// </summary>
        /// <param name="p"></param>
        /// <param name="azimuth">角度</param>
        /// <param name="inclination">角度</param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Point getRayCrossPointWithPlane(Point p, double azimuth, double inclination, double planeheight)
        {
            double arithmeticAzimuth = GeometricUtilities.ConvertGeometricArithmeticAngle(azimuth);

            double r = Math.Abs(p.Z - planeheight) / Math.Tan(GeometricUtilities.GetRadians(inclination));
            if (r > 40075000)
            {
                return null;
            }
            Point ret = getPointByDegreeDistance(p, arithmeticAzimuth, r);
            ret.Z = planeheight;
            return ret;
        }

        /// <summary>
        /// 返回射线与地面交点(角度大于0)
        /// </summary>
        /// <param name="originPoint">射线起点</param>
        /// <param name="azimuth">角度</param>
        /// <param name="inclination">角度</param>
        /// <returns></returns>
        public static Point getCrossedPoint_Ray_Ground(Point originPoint, double azimuth, double inclination)
        {
            return getRayCrossPointWithPlane(originPoint, azimuth, inclination, 0);
        }

        /// <summary>
        /// 角度制
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="azimuth"></param>
        /// <param name="inclination"></param>
        /// <returns></returns>
        public static Point getCrossedPoint_Ray_Ground(Point start, Point end, out double azimuth, out double inclination)
        {
            Vector3D vector = Vector3D.constructVector(start, end);

            inclination = vector.Inclination;
            azimuth = vector.Azimuth;

            inclination = GeometricUtilities.GetDegrees(inclination);
            azimuth = GeometricUtilities.GetDegrees(azimuth);

            double t = Math.Round(inclination, 3);

            if (inclination < 90 && inclination > 0)
            {
                return getCrossedPoint_Ray_Ground(start, azimuth, inclination);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 获取与地面的交点
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static Point getCrossedPoint_Ray_Ground(Point start, Point end)
        {
            double a, b;
            return getCrossedPoint_Ray_Ground(start, end, out a, out b);
        }

        /// <summary>
        /// 弧度制
        /// </summary>
        /// <param name="start">起点</param>
        /// <param name="end">终点</param>
        /// <param name="azimuth"></param>
        /// <param name="inclination">下正上负</param>
        public static void getAzimuthInclinationRadian(Point start, Point end, out double azimuth, out double inclination)
        {
            Vector3D t = Vector3D.constructVector(start, end);
            azimuth = t.Azimuth;
            inclination = t.Inclination;
        }

        /// <summary>
        /// 角度制
        /// </summary>
        /// <param name="start">起点</param>
        /// <param name="end">终点</param>
        /// <param name="azimuth"></param>
        /// <param name="inclination">下正上负</param>
        public static void getAzimuth_Inclination(Point start, Point end, out double azimuth, out double inclination)
        {
            Vector3D t = Vector3D.constructVector(start, end);
            azimuth = GeometricUtilities.GetDegrees(t.Azimuth);
            inclination = GeometricUtilities.GetDegrees(t.Inclination);
        }

        /// <summary>
        /// 获取两向量间夹角的cos值
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double getAngleofVector3Ds(Vector3D a, Vector3D b)
        {
            double cos = a.dotProduct(b) / a.Magnitude / b.Magnitude;
            return cos;
        }

        /// <summary>
        /// 获取两向量间夹角的cos值
        /// </summary>
        /// <param name="vectorfrom"></param>
        /// <param name="vectorEnd1"></param>
        /// <param name="verctorEnd2"></param>
        /// <returns></returns>
        public static double getAngleofVector3Ds(Point vectorfrom, Point vectorEnd1, Point vectorEnd2)
        {
            Vector3D vector1 = Vector3D.constructVector(vectorfrom, vectorEnd1);
            Vector3D vector2 = Vector3D.constructVector(vectorfrom, vectorEnd2);
            return GeometricUtilities.getAngleofVector3Ds(vector1, vector2);
        }

        /// <summary>
        /// 判断点是否在多边形内，废弃方法，替代方法 PointInPolygon
        /// </summary>
        /// <param name="points"></param>
        /// <param name="point"></param>
        /// <param name="rayType"></param>
        /// <param name="diffractType"></param>
        /// <returns></returns>
        public static bool PointInBox2(Point[] points, Point point, out RayType rayType)
        {
            double sumTheta = 0;
            rayType = RayType.HReflection;

            Vector3D preV = new Vector3D(), nextV = new Vector3D();

            for (int i = 0; i < points.Length - 1; i++)
            {
                double alfa = 0;
                preV.XComponent = points[i].X - point.X;
                preV.YComponent = points[i].Y - point.Y;
                preV.ZComponent = 0;// points[i].Z - point.Z;

                if (i == points.Length - 2)
                {
                    nextV.XComponent = points[0].X - point.X;
                    nextV.YComponent = points[0].Y - point.Y;
                    nextV.ZComponent = 0;// points[0].Z - point.Z;
                }
                else
                {
                    nextV.XComponent = points[i + 1].X - point.X;
                    nextV.YComponent = points[i + 1].Y - point.Y;
                    nextV.ZComponent = 0;// points[i + 1].Z - point.Z;
                }
                //multiVec=0 则说明三点共线  >0说明 nextV在preV的逆时针方向 否侧 nextV在preV的顺时针方向
                //Double multiVec = multiDot(preV, nextV, point);

                double cos = GeometricUtilities.getAngleofVector3Ds(preV, nextV);
                alfa = Math.Acos(cos) * 180 / Math.PI;
                if (alfa > 180)
                    alfa = 360 - alfa;

                //multiVec=0 则说明三点共线  >0说明 nextV在preV的逆时针方向 否侧 nextV在preV的顺时针方向
                Double multiVec = preV.XComponent * nextV.YComponent - nextV.XComponent * preV.YComponent;
                if (multiVec > 0)
                    sumTheta = sumTheta + alfa;
                else if (multiVec < 0)
                    sumTheta = sumTheta - alfa;
                else
                {
                    rayType = RayType.HDiffraction;
                    return true;
                }
            }

            if (Math.Abs(Math.Abs(sumTheta) - 360) < 0.000001)
                return true;
            else
                return false;

        }

        /// <summary>
        /// 点是否在水平二维平面内
        /// </summary>
        /// <param name="points"></param>
        /// <param name="point"></param>
        /// <param name="isEdge">是否与棱相交</param>
        /// <param name="startPointIndex">相交棱起点索引</param>
        /// <returns></returns>
        public static bool PointInPolygon(Point[] points, Point point, out bool isEdge, ref int startPointIndex)
        {
            int i, j;
            //是否在平面内
            bool ret = false;
            isEdge = false;
            int cnt = points.Length;
            double minx, miny, maxx, maxy;
            minx = miny = double.MaxValue;
            maxx = maxy = 0;
            for (i = 0; i < cnt; i++)
            {
                if (points[i].X > maxx)
                {
                    maxx = points[i].X;
                }
                else if (points[i].X < minx)
                {
                    minx = points[i].X;
                }

                if (points[i].Y > maxy)
                {
                    maxy = points[i].Y;
                }
                else if (points[i].Y < miny)
                {
                    miny = points[i].Y;
                }
            }

            if (point.X < minx || point.X > maxx || point.Y < miny || point.Y > maxy)
            {
                return false;
            }

            double tx = Math.Round(point.X, 3);
            for (i = 0, j = cnt - 1; i < cnt; j = i++)
            {
                if ((points[i].Y > point.Y) != (points[j].Y > point.Y))
                {
                    double tmp = Math.Round((points[j].X - points[i].X) * (point.Y - points[i].Y) / (points[j].Y - points[i].Y) + points[i].X, 3);

                    if (Math.Abs(tx - tmp) < 0.5)
                    {
                        startPointIndex = j;
                        isEdge = true;
                        ret = true;
                        break;
                    }
                    else if (tx < tmp)
                    {
                        ret = !ret;
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// 求射线与面的交点，大地坐标
        /// </summary>
        /// <param name="start">射线起点</param>
        /// <param name="dir">射线方向</param>
        /// <param name="start1">面上的一点</param>
        /// <param name="normal">面的法向</param>
        /// <returns></returns>
        public static Point Intersection(Point start, Vector3D dir, Point start1, Vector3D normal)
        {
            Point interpoint;

            double t = normal.dotProduct(dir);
            if (t != 0)
            {
                Vector3D tmp = new Vector3D(start1.X - start.X, start1.Y - start.Y, start1.Z - start.Z);
                t = (tmp.dotProduct(normal)) / t;   // t = normal * (start1 - start) / (normal * dir)

                if (Math.Abs(t) < 0.001)
                {
                    return null;
                }
                interpoint = new Point();
                interpoint.X = start.X + dir.XComponent * t;
                interpoint.Y = start.Y + dir.YComponent * t;
                interpoint.Z = start.Z + dir.ZComponent * t;

                if (interpoint.X < 0 || interpoint.Y < 0 || interpoint.Z < 0)
                {
                    interpoint = null;
                }
            }
            else
            {
                interpoint = null;
            }

            return interpoint;
        }

        /// <summary>
        /// 合并射线终点，射线的角度小于angle的合并
        /// </summary>
        /// <param name="source"></param>
        /// <param name="points"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static List<Point> mergePointsByAngle(Point source, List<Point> points, double angle)
        {
            int cnt = points.Count;

            List<Point> ret = new List<Point>(cnt);
            Dictionary<int, Vector3D> tmp = new Dictionary<int, Vector3D>(cnt);

            //计算方位角、下倾角
            for (int i = 0; i < cnt; i++)
            {
                tmp.Add(i, Vector3D.constructVector(source, points[i]));
            }

            Vector3D v1, v2;
            for (int i = 0; i < cnt; i++)
            {
                if (!tmp.ContainsKey(i))
                {
                    continue;
                }
                v1 = tmp[i];
                for (int j = i + 1; j < cnt; j++)
                {
                    if (!tmp.ContainsKey(j))
                    {
                        continue;
                    }
                    v2 = tmp[j];
                    if (Math.Abs(v1.Azimuth - v2.Azimuth) < angle && Math.Abs(v1.Inclination - v2.Inclination) < angle)
                    {
                        tmp.Remove(j);
                    }
                }
                ret.Add(points[i]);
            }

            return ret;
        }

    }
}
