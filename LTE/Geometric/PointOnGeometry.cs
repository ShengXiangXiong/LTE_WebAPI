using System;

namespace LTE.Geometric
{
    public static class PointOnGeometry
    {
        /// <summary>
        /// 判断点是否在平面线段上
        /// </summary>
        /// <param name="a">判断点</param>
        /// <param name="from">线段起点</param>
        /// <param name="to">线段终点</param>
        /// <returns></returns>
        public static Boolean PointOnPlaneSegment(Point a, Point from, Point to)
        {
            if (a == null || from == null || to == null)
                return false;
            return a.X >= Math.Min(from.X, to.X) && a.X <= Math.Max(from.X, to.X) && a.Y >= Math.Min(from.Y, to.Y) && a.Y <= Math.Max(from.Y, to.Y) && ((a.X - from.X) * (from.Y - to.Y) == (a.Y - from.Y) * (from.X - to.X));
        }

        /// <summary>
        /// 判断点是否在空间线段上
        /// </summary>
        /// <param name="point">判断点</param>
        /// <param name="point1">线段上一点</param>
        /// <param name="point2">线段上一点</param>
        /// <returns></returns>
        public static bool pointOnEdge(Point point, Point point1, Point point2)
        {
            Vector3D vector1 = Vector3D.constructVector(point1, point);
            Vector3D vector2 = Vector3D.constructVector(point2, point);

            double cosine = vector1.dotProduct(vector2) / (vector1.Magnitude * vector2.Magnitude);
            double t = Math.Round(cosine, 3);

            return (t == -1.000 || t == 1.000);
        }
    }
}
