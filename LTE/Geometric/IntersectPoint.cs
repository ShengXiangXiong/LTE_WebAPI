using System;

using LTE.InternalInterference;

namespace LTE.Geometric
{
    public static class IntersectPoint
    {
        /// <summary> 

        /// 求一条直线与平面的交点 

        /// </summary> 

        /// <param name="planeVector">平面的法线向量，长度为3</param> 

        /// <param name="planePoint">平面经过的一点坐标，长度为3</param> 

        /// <param name="lineVector">直线的方向向量，长度为3</param> 

        /// <param name="linePoint">直线经过的一点坐标，长度为3</param> 

        /// <returns>返回交点坐标，长度为3</returns> 

        public static float[] CalPlaneLineIntersectPoint(float[] planeVector, float[] planePoint, float[] lineVector, float[] linePoint)
        {

            float[] returnResult = new float[3];

            float vp1, vp2, vp3, n1, n2, n3, v1, v2, v3, m1, m2, m3, t, vpt;

            vp1 = planeVector[0];

            vp2 = planeVector[1];

            vp3 = planeVector[2];

            n1 = planePoint[0];

            n2 = planePoint[1];

            n3 = planePoint[2];

            v1 = lineVector[0];

            v2 = lineVector[1];

            v3 = lineVector[2];

            m1 = linePoint[0];

            m2 = linePoint[1];

            m3 = linePoint[2];


            vpt = v1 * vp1 + v2 * vp2 + v3 * vp3;

            //首先判断直线是否与平面平行 

            if (vpt == 0)
            {

                returnResult = null;

            }

            else
            {

                t = ((n1 - m1) * vp1 + (n2 - m2) * vp2 + (n3 - m3) * vp3) / vpt;

                returnResult[0] = m1 + v1 * t;

                returnResult[1] = m2 + v2 * t;

                returnResult[2] = m3 + v3 * t;

            }

            return returnResult;

        }

        public static Point CalPlaneLineIntersectPoint(Vector3D planeVector, Point planePoint, Vector3D lineVector, Point linePoint)
        {
            Point crossPoint = new Point();
            double vpt, t;
            vpt = lineVector.dotProduct(planeVector);
            //首先判断直线是否与平面平行 
            if (vpt == 0)
            {
                crossPoint = null;
            }
            else
            {
                t = Vector3D.constructVector(linePoint, planePoint).dotProduct(planeVector) / vpt;

                if (Math.Abs(t) < 0.001)
                {
                    return null;
                }

                crossPoint.X = linePoint.X + lineVector.XComponent * t;
                crossPoint.Y = linePoint.Y + lineVector.YComponent * t;
                crossPoint.Z = linePoint.Z + lineVector.ZComponent * t;
            }
            return crossPoint;
        }

        /// <summary>
        /// 求空间直线与XOY平面的交点
        /// </summary>
        /// <param name="planePoin t">XOY平面上一点</param>
        /// <param name="linePoint">空间直线上一点</param>
        /// <param name="lineVector">空间直线向量</param>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       
        /// <returns></returns>
        public static Point CalTopPlaneLineIntersectPoint(Point planePoint, Point linePoint, Vector3D lineVector)
        {
            Point crossPoint = new Point();
            double vpt, t;
            vpt = lineVector.ZComponent;
            //首先判断直线是否与平面平行 
            if (vpt == 0)
            {
                crossPoint = null;
            }
            else
            {
                t = (planePoint.Z - linePoint.Z) / vpt;
                if (Math.Abs(t) < 0.001)
                {
                    return null;
                }
                crossPoint.X = linePoint.X + lineVector.XComponent * t;
                crossPoint.Y = linePoint.Y + lineVector.YComponent * t;
                crossPoint.Z = linePoint.Z + lineVector.ZComponent * t;
            }
            return crossPoint;
        }

        public static Point getIntersectPointOfPlaneLines(Point A, Point B, Point C, Point D)
        {
            ////get   the   intersection   point   of   line   L1,L2 
            ////returns:   
            ////0:   parallel 
            ////1:   unique   intersection   via   inter 
            ////2:   same   line   
            //int   GetIntersect(Point   L11,   Point   L12,   Point   L21,   Point   L22,   Point&   inter) 
            //{ 
            ////L1:   a1x+b1y=c1 
            //double   a1=L12.y-L11.y; 
            //double   b1=L11.x-L12.x; 
            //double   c1=L11.x*L12.y-L1.2.x*L11.y; 
            ////L2:   a2x+b2y=c2 
            //double   a2=L22.y-L21.y; 
            //double   b2=L21.x-L22.x; 
            //double   c2=L21.x*L22.y-L22.x*L21.y; 

            //double   detab=a1*b2-a2*b1; 
            //if(detab==0) 
            //{ 
            //double   r; 
            //if(a2!=0)   r=a1/a2; 
            //else   r=b1/b2; 

            //if(c1==0&&c2==0)   return   2; 
            //if(r==c1/c2)   return   2; 
            //else   return   0; 
            //} 

            //inter.x=(c1*b2-c2*b1)/detab; 
            //inter.y=(a1*c2-a2*c1)/detab; 
            //return   1; 
            //}
            Point crossPoint = new Point();
            double a1 = B.Y - A.Y;
            double b1 = A.X - B.X;
            double c1 = A.X * B.Y - B.X * A.Y;

            double a2 = D.Y - C.Y;
            double b2 = C.X - D.X;
            double c2 = C.X * D.Y - D.X * C.Y;

            double detab = a1 * b2 - a2 * b1;
            if (detab == 0)
            {
                crossPoint = null;
            }

            crossPoint.X = (c1 * b2 - c2 * b1) / detab;
            crossPoint.Y = (a1 * c2 - a2 * c1) / detab;
            return crossPoint;
        }

    }
}
