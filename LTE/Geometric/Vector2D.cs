using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LTE.Geometric
{
    class Vector2D
    {
        public double x, y, len;

        public Vector2D construct(Point a, Point b)
        {
            return new Vector2D(b.X - a.X, b.Y - a.Y);
        }

        public Vector2D(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public Vector2D(double x1, double y1, double x2, double y2)
        {
            x = x2 - x1;
            y = y2 - y1;
        }

        public Vector2D()
        {
        }

        public void unit()
        {
            this.len = Math.Sqrt(x * x + y * y);
            this.x /= this.len;
            this.y /= this.len;
            this.len = 1;
        }

        public double dot(ref Vector2D v)
        {
            return (this.x * v.x + this.y * v.y);
        }

    }
}
