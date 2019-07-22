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
