using System;
namespace LTE.Geometric
{
    public class Point
    {
        public double X;
        public double Y;
        public double Z;

        public Point()
        {
        }

        public Point(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }


        public Point(Point p): this(p.X, p.Y, p.Z)
        {
        }

        public Point clone()
        {
            return new Point(this);
        }

        public static double getSlopeXY(Point A, Point B)
        {
            double deteX = B.X - A.X;
            double deteY = B.Y - A.Y;
            return deteY / deteX;
        }
        public static double getSlopeXZ(Point A, Point B)
        {
            double deteX = B.X - A.X;
            double deteZ = B.Z - A.Z;
            return deteZ / deteX;
 
        }

        public static double getSlopeYZ(Point A, Point B)
        {
            double deteY = B.Y - A.Y;
            double deteZ = B.Z - A.Z;
            return deteZ / deteY;
        }

        public static bool isEqual(Point a, Point b)
        {
            if (Math.Abs(a.X - b.X) < 0.00000001 && Math.Abs(a.Y - b.Y) < 0.000000001) return true;
            return false;
        }

        /// <summary>
        /// 2919-5-29 by Jiang
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static double distance(Point A, Point B)
        {
            double deteX = Math.Pow((A.X - B.X), 2);
            double deteY = Math.Pow((A.Y - B.Y), 2);
            double deteZ = Math.Pow((A.Z - B.Z), 2);
            double distance = Math.Sqrt(deteX + deteY + deteZ);
            return distance;
        }
        /// <summary>
        /// 2919-5-29 by Jiang
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static double distanceXY(Point A, Point B)
        {
            double deteX = Math.Pow((A.X - B.X), 2);
            double deteY = Math.Pow((A.Y - B.Y), 2);
            //double deteZ = Math.Pow((A.Z - B.Z), 2);
            //double distance = Math.Sqrt(deteX + deteY + deteZ);
            double distance = Math.Sqrt(deteX + deteY);
            return distance;
        }
    }

    public class Polar
    {
        public double r;
        public double theta;

        public Polar()
        {
        }

        public Polar(double r, double theta)
        {
            this.r = r;
            this.theta = theta;
        }

        public Polar(Polar p): this(p.r, p.theta)
        {
        }
    }
}
