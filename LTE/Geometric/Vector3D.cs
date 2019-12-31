using System;

namespace LTE.Geometric
{
    /// <summary>
    /// 替代IVector3D
    /// </summary>
    public class Vector3D
    {
        //分量
        public double XComponent;
        public double YComponent;
        public double ZComponent;
        //xy长度
        public double Magnitude2D;
        /// <summary>
        /// 向量长
        /// </summary>
        public double Magnitude;
        /// <summary>
        /// 方位角（弧度制，与y轴夹角，顺时针方向）
        /// </summary>
        public double Azimuth;
        /// <summary>
        /// 下倾角（弧度制，与xy平面夹角，向下为正，向上为负）
        /// </summary>
        public double Inclination;

        public Vector3D()
        {
        }

        public Vector3D(double x, double y, double z)
        {
            this.SetComponents(x, y, z);
        }

        /// <summary>
        /// 计算方位角、下倾角
        /// </summary>
        private void calc()
        {
            if (this.Magnitude2D == 0)
            {
                this.Azimuth = 0;
            }
            else
            {
                //求方位角(相对于Y轴正方向)
                //设射线与Y轴正方向的夹角为α
                double sinAlpha = this.XComponent / this.Magnitude2D;
                double cosAlpha = this.YComponent / this.Magnitude2D;
                double alpha = Math.Asin(sinAlpha);
                //根据三角函数的符号确定象限及大小
                if (sinAlpha >= 0)
                {
                    this.Azimuth = cosAlpha < 0 ? Math.PI - alpha : alpha;
                }
                else
                {
                    this.Azimuth = cosAlpha < 0 ? Math.PI - alpha : Math.PI * 2 + alpha;
                }
            }

            if (this.Magnitude == 0)
            {
                this.Inclination = 0;
            }
            else
            {
                //求下倾角(相对于水平面上负下正)
                double sinBeta = 0 - this.ZComponent / this.Magnitude;
                this.Inclination = Math.Asin(sinBeta);
            }
        }

        public void SetComponents(double x, double y, double z)
        {
            this.XComponent = x;
            this.YComponent = y;
            this.ZComponent = z;
            double t = Math.Pow(x, 2) + Math.Pow(y, 2);
            this.Magnitude2D = Math.Sqrt(t);
            this.Magnitude = Math.Sqrt(t + Math.Pow(z, 2));
            this.calc();
        }

        public static Vector3D convertFromPoint(Point p)
        {
            return new Vector3D(p.X, p.Y, p.Z);
        }

        public static Vector3D constructVector(Point start, Point end)
        {
            Vector3D dir = new Vector3D(end.X - start.X, end.Y - start.Y, end.Z - start.Z);
            //dir.unit();
            return dir;
        }

        /// <summary>
        /// 向量单位化 
        /// </summary>
        public Vector3D unit()
        {
            if (this.Magnitude == 0)
            {
                calc();
            }
            this.XComponent /= this.Magnitude;
            this.YComponent /= this.Magnitude;
            this.ZComponent /= this.Magnitude;
            this.Magnitude2D /= this.Magnitude;
            this.Magnitude = 1;
            return this;
        }

        /// <summary>
        /// 求向量点积
        /// </summary>
        /// <param name="A"></param>
        /// <returns></returns>
        public double dotProduct(Vector3D A)
        {
            return (this.XComponent * A.XComponent + this.YComponent * A.YComponent + this.ZComponent * A.ZComponent);
        }

        /// <summary>
        /// 两个向量的叉积（矩阵形式），用于求法向量
        /// </summary>
        /// <param name="A"></param>
        /// <returns></returns>
        public Vector3D crossProduct(Vector3D A)
        {
            Vector3D ret = new Vector3D();
            double x, y, z;
            x = this.YComponent * A.ZComponent - this.ZComponent * A.YComponent;
            y = this.ZComponent * A.XComponent - this.XComponent * A.ZComponent;
            z = this.XComponent * A.YComponent - this.YComponent * A.XComponent;
            ret.SetComponents(x, y, z);
            return ret;
        }

        public Vector3D minus(ref Vector3D V)
        {
            return new Vector3D(XComponent - V.XComponent, YComponent - V.YComponent, ZComponent - V.ZComponent);
        }

        /// <summary>
        /// 求与射线成锐角的平面法向量
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="normal"></param>
        public static void getNormalVector(Point start, Point end, ref Vector3D normal)
        {
            Vector3D.getNormalVector(Vector3D.constructVector(start, end), ref normal);
        }

        /// <summary>
        /// 求与向量成锐角的平面法向量
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="normal"></param>
        public static void getNormalVector(Vector3D vector, ref Vector3D normal)
        {
            if (vector.dotProduct(normal) < 0)
            {
                normal.XComponent = -normal.XComponent;
                normal.YComponent = -normal.YComponent;
                normal.ZComponent = -normal.ZComponent;
            }
        }

        // 两向量的夹角，弧度
        public static double getAngle(ref Vector3D v1, ref Vector3D v2)
        {
            double d1 = Math.Sqrt(Math.Pow(v1.XComponent, 2) + Math.Pow(v1.YComponent, 2) + Math.Pow(v1.ZComponent, 2));
            double d2 = Math.Sqrt(Math.Pow(v2.XComponent, 2) + Math.Pow(v2.YComponent, 2) + Math.Pow(v2.ZComponent, 2));
            double angle = v1.dotProduct(v2) / (d1 * d2);
            return Math.Acos(angle);
        }
    }

}
