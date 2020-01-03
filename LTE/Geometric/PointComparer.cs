using System;

namespace LTE.Geometric
{
    public class PointComparer 
    {
        /// <summary>
        /// 比较两点是否是同一点，大地坐标系
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public bool Equals(Point p1, Point p2)
        {
            if (Object.ReferenceEquals(p1, p2))
                return true;
            if (p1 == null || p2 == null)
                return false;
            if (Math.Round(p1.X, 3) == Math.Round(p2.X, 3) && Math.Round(p1.Y, 3) == Math.Round(p2.Y, 3) && Math.Round(p1.Z, 3) == Math.Round(p2.Z, 3))
                return true;
            else
                return false;
        }

        public static bool Equals1(Point p1, Point p2)
        {
            if (Object.ReferenceEquals(p1, p2))
                return true;
            if (p1 == null || p2 == null)
                return false;
            if (Math.Round(p1.X, 3) == Math.Round(p2.X, 3) && Math.Round(p1.Y, 3) == Math.Round(p2.Y, 3) && Math.Round(p1.Z, 3) == Math.Round(p2.Z, 3))
                return true;
            else
                return false;
        }

        /// <summary>
        /// 大地坐标系
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static int GetHashCode(Point p)
        {
            return (p == null) ? 0 : (int)(Math.Round(p.X,3)+Math.Round(p.Y,3)+Math.Round(p.Z,3));
        }
    }
}
