using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LTE.InternalInterference
{
    public class Regress
    {
        public Regress()
        {

        }

        // 最小二乘法线性回归   y = bx + a
        public int CalcRegress(ref List<double> x, double[] y, int start, int end, out double b, out double a, out double maxErr)
        {
            double sumX = 0;
            double sum_y = 0;
            double avgX;
            double avg_y;

            int dataCnt = end - start + 1;

            if (dataCnt < 2)
            {
                a = 0;
                b = 0;
                maxErr = 0;
                return -1;
            }

            for (int i = start; i <= end; i++)
            {
                sumX += x[i];
                sum_y += y[i];
            }

            avgX = sumX / dataCnt;
            avg_y = sum_y / dataCnt;

            double SPxy = 0;
            double SSx = 0;

            for (int i = start; i <= end; i++)
            {
                SPxy += (x[i] - avgX) * (y[i] - avg_y);
                SSx += (x[i] - avgX) * (x[i] - avgX);
            }

            if (SSx == 0)
            {
                a = 0;
                b = 0;
                maxErr = 0;
                return -1;
            }
            b = SPxy / SSx;
            a = avg_y - b * avgX;


            //下面代码计算最大偏差            
            maxErr = 0;
            for (int i = start; i <= end; i++)
            {
                double yi = a + b * x[i];
                double absErrYi = Math.Abs(yi - y[i]);

                if (absErrYi > maxErr)
                {
                    maxErr = absErrYi;
                }
            }
            return 0;
        }

        // 最小二乘法线性回归   y = bx + a
        public int CalcRegress(ref List<double> x, ref List<double> y, int start, int end, out double b, out double a, out double maxErr)
        {
            double sumX = 0;
            double sum_y = 0;
            double avgX;
            double avg_y;

            int dataCnt = end - start + 1;

            if (dataCnt < 2)
            {
                a = 0;
                b = 0;
                maxErr = 0;
                return -1;
            }

            for (int i = start; i <= end; i++)
            {
                sumX += x[i];
                sum_y += y[i];
            }

            avgX = sumX / dataCnt;
            avg_y = sum_y / dataCnt;

            double SPxy = 0;
            double SSx = 0;

            for (int i = start; i <= end; i++)
            {
                SPxy += (x[i] - avgX) * (y[i] - avg_y);
                SSx += (x[i] - avgX) * (x[i] - avgX);
            }

            if (SSx == 0)
            {
                a = 0;
                b = 0;
                maxErr = 0;
                return -1;
            }
            b = SPxy / SSx;
            a = avg_y - b * avgX;


            //下面代码计算最大偏差            
            maxErr = 0;
            for (int i = start; i <= end; i++)
            {
                double yi = a + b * x[i];
                double absErrYi = Math.Abs(yi - y[i]);

                if (absErrYi > maxErr)
                {
                    maxErr = absErrYi;
                }
            }
            return 0;
        }

    }
}
