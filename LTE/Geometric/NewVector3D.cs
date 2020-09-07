using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTE.Geometric
{
    public class NewVector3D
    {
        public double x;
        public double y;
        public double z;
        public double magnitude;

        public NewVector3D(Point startPoint,Point endPoint) {
            this.x = endPoint.X - startPoint.X;
            this.y = endPoint.Y - startPoint.Y;
            this.z = endPoint.Z - startPoint.Z;
            magnitude = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2));
        }

        public double calcDefAngle(ref NewVector3D vec) {
            double dotResult = dotProduct(vec);
            double tmp = dotResult / (this.magnitude * vec.magnitude);

            double epison = 0.0001;
            if (Math.Abs(tmp - 1) < epison)
            {
                tmp = 1;
            }
            else if (Math.Abs(tmp + 1) < epison)
            {
                tmp = -1;
            }

            double defAngle= Math.Acos(tmp);

            if (double.IsNaN(defAngle))
            {
                int test = 1;
            }

            return defAngle;
        }

        /// 求向量点积
        public double dotProduct(NewVector3D A)
        {
            return (this.x * A.x + this.y * A.y + this.z * A.z);
        }
    }
}
