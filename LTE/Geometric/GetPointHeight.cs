using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTE.Geometric
{
    public class PointHeight
    {
        /// <summary>
        /// 判断二维平面一点是否在三角形内(包括三角形的边和顶点)
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        /// <param name="x4"></param>
        /// <param name="y4"></param>
        /// <returns></returns>
        public static Boolean isInside(Point v1, Point v2, Point v3, double x4, double y4)
        {
            Vector2D vec0 = new Vector2D(v1.X, v1.Y, v3.X, v3.Y);
            Vector2D vec1 = new Vector2D(v1.X, v1.Y, v2.X, v2.Y);
            Vector2D vec2 = new Vector2D(v1.X, v1.Y, x4, y4);

            double dot00 = vec0.dot(ref vec0);
            double dot01 = vec0.dot(ref vec1);
            double dot02 = vec0.dot(ref vec2);
            double dot11 = vec1.dot(ref vec1);
            double dot12 = vec1.dot(ref vec2);

            double temp = 1 / (dot00 * dot11 - dot01 * dot01);
            double u = (dot11 * dot02 - dot01 * dot12) * temp;
            if (u < 0)
            {
                return false;
            }
            double v = (dot00 * dot12 - dot01 * dot02) * temp;
            if (v < 0)
            {
                return false;
            }

            return u + v <= 1;
        }


        /// <summary>
        /// 计算已知三角形组成的平面，求面内一点v4(已知x4、y4)的z坐标z4。
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        /// <param name="x4"></param>
        /// <param name="y4"></param>
        /// <returns></returns>
        public static double getPointHeight(Point v1, Point v2, Point v3, double x4, double y4)
        {
            double[] lineVector = { 0, 0, 1 };//直线方向向量
            Point linePoint = new Point(x4, y4, 0);//直线一点
            double[] planeVector = calNormal(v1, v2, v3);//平面法向量

            //求交点
            Point intersectPoint = new Point();//直线和平面交点
            intersectPoint = calPlaneLineIntersectPoint(planeVector, v1, lineVector, linePoint);
            if (intersectPoint == null)
            {
                //若平面与z轴平行，暂未处理
                return 0;
            }
            else
            {
                return intersectPoint.Z;
            }
        }


        /// <summary>
        /// 计算平面法向量
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        /// <returns></returns>
        private static double[] calNormal(Point v1, Point v2, Point v3)
        {
            //向量p1p2=(x2-x1,y2-y1,z2-z1), 向量p1p3=(x3-x1,y3-y1,z3-z1)
            //向量积 a×b=(a2b3-a3b2，a3b1-a1b3，a1b2-a2b1)
            double n1 = (v2.Y - v1.Y) * (v3.Z - v1.Z) - (v2.Z - v1.Z) * (v3.Y - v1.Y);
            double n2 = (v2.Z - v1.Z) * (v3.X - v1.X) - (v2.X - v1.X) * (v3.Z - v1.Z);
            double n3 = (v2.X - v1.X) * (v3.Y - v1.Y) - (v2.Y - v1.Y) * (v3.X - v1.X);
            //PS 平面方程: n1 * (x – x1) + n2 * (y – y1) + n3 * (z – z1) = 0 ;

            //平面法向量vn
            double[] vn = { n1, n2, n3 };
            return vn;
        }


        /// <summary>
        /// 求直线与面的交点
        /// </summary>
        /// <param name="planeVector">平面法向量</param>
        /// <param name="planePoint">平面一点</param>
        /// <param name="lineVector">直线一点</param>
        /// <param name="linePoint">直线方向向量</param>
        /// <returns>交点，平行则返回null</returns>
        private static Point calPlaneLineIntersectPoint(double[] planeVector, Point planePoint, double[] lineVector, Point linePoint)
        {

            Point p = new Point();

            double vp1, vp2, vp3, n1, n2, n3, v1, v2, v3, m1, m2, m3, t, vpt;

            vp1 = planeVector[0];

            vp2 = planeVector[1];

            vp3 = planeVector[2];

            n1 = planePoint.X;

            n2 = planePoint.Y;

            n3 = planePoint.Z;

            v1 = lineVector[0];

            v2 = lineVector[1];

            v3 = lineVector[2];

            m1 = linePoint.X;

            m2 = linePoint.Y;

            m3 = linePoint.Z;


            vpt = v1 * vp1 + v2 * vp2 + v3 * vp3;

            //首先判断直线是否与平面平行 

            if (vpt == 0)
            {

                p = null;

            }

            else
            {

                t = ((n1 - m1) * vp1 + (n2 - m2) * vp2 + (n3 - m3) * vp3) / vpt;

                p.X = m1 + v1 * t;

                p.Y = m2 + v2 * t;

                p.Z = m3 + v3 * t;

            }

            return p;

        }
    }
}
