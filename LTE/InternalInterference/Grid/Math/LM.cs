using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace LTE.InternalInterference
{
    public class MulPtLoc
    {
        //double p0 = 53;      // 干扰源发射功率--未知，这里是为了得到各点的接收功率模拟值
        double xt;// = 668400 + 600;  // 干扰源位置--未知，这里是为了得到各点的接收功率模拟值
        double yt;// = 3545720 + 2200;
        //double f = 1800;     // 干扰源频率--未知，这里是为了得到各点的接收功率模拟值

        int n_data;// = 6;   // 数据组数
        int n_iter = 50;  // 迭代次数
        int n_param = 2;  // 参数维数

        public MulPtLoc(double xt1, double yt1, int n_data1)
        {
            xt = xt1;
            yt = yt1;
            n_data = n_data1;
        }

        // 启发式规则3，多点定位
        // X：坐标 P：功率
        public double[] solve(ref List<List<double>> X, ref List<double> P)
        {
            // 构造参数
            //double xt1 = 668400, yt1 = 3545720;
            //double disX = xt1;    // 控制已知点相对于原点的偏移
            //double disY = yt1;
            //double R = 3000;         // 控制已知点之间的距离
            //List<List<double>> X = new List<List<double>>();           // 位置
            //X.Add(new List<double> { disX + R * Math.Sqrt(3), disY });
            //X.Add(new List<double> { disX - R * Math.Sqrt(3), disY });
            //X.Add(new List<double> { disX + R * Math.Sqrt(3) / 2, disY + R * 1.5 });
            //X.Add(new List<double> { disX - R * Math.Sqrt(3) / 2, disY + R * 1.5 });
            //X.Add(new List<double> { disX - R * Math.Sqrt(3) / 2, disY - R * 1.5 });
            //X.Add(new List<double> { disX + R * Math.Sqrt(3) / 2, disY - R * 1.5 });

            //List<double> P = pwr(X);  // 功率

            // 变量赋值
            double u = 0.01; // 阻尼系数初值
            Matrix wk = new Matrix(2, 1);
            wk[0, 0] = xt;// + 2000.0;  // 初始猜测
            wk[1, 0] = yt;// + 2000.0;
            bool updateJ = true;

            // 迭代
            Matrix err = new Matrix(n_data - 1, 1);    // 误差函数
            Matrix err_tmp = new Matrix(n_data - 1, 1);
            Matrix J = new Matrix(n_data - 1, n_param);  // 雅可比矩阵
            double last_mse = 0;
            double mse = 0, mse_tmp = 0;

            for (int it = 0; it < n_iter; it++)
            {
                if (updateJ == true)
                {
                    mse = 0;
                    mse_tmp = 0;

                    for (int i = 1; i < n_data; i++)
                    {
                        // 计算误差
                        err[i - 1, 0] = E(wk[0, 0], wk[1, 0], X[i][0], X[i][1], P[i], X[0][0], X[0][1], P[0]);
                        mse += Math.Pow(err[i - 1, 0], 2);

                        // 根据当前估计值，计算雅可比矩阵
                        double[] d = deriv(wk[0, 0], wk[1, 0], X[i][0], X[i][1], P[i], X[0][0], X[0][1], P[0]);
                        J[i - 1, 0] = d[0];
                        J[i - 1, 1] = d[1];
                    }
                    mse /= n_data;
                }

                // H = J.T * J + u * np.eye(n_param);  
                Matrix eye = new Matrix(n_param, n_param);
                for (int i = 0; i < n_param; i++)
                    eye[i, i] = 1;
                Matrix H = J.transpose() * J + u * eye;

                // dw = -H.I * J.T * err
                Matrix dw = -1 * H.converse() * J.transpose() * err;

                // g = J.T * err
                Matrix g = J.transpose() * err;

                //wk_tmp = wk + dw
                Matrix wk_tmp = wk + dw;

                // 计算新的估计值及对应的误差
                mse_tmp = 0;
                for (int i = 1; i < n_data; i++)
                {
                    err_tmp[i - 1, 0] = E(wk_tmp[0, 0], wk_tmp[1, 0], X[i][0], X[i][1], P[i], X[0][0], X[0][1], P[0]);
                    mse_tmp += Math.Pow(err_tmp[i - 1, 0], 2);
                }

                mse_tmp /= n_data;

                if (mse_tmp < mse)
                {
                    u = u / 10;
                    wk = wk_tmp;
                    mse = mse_tmp;
                    updateJ = true;
                }
                else
                {
                    updateJ = false;
                    u = u * 10;
                }

                //Console.WriteLine("step = {0}, abs(mse-lase_mse) = {1}", it, Math.Abs(mse - last_mse));
                if (Math.Abs(mse - last_mse) < 0.00001)
                    break;

                last_mse = mse;  // 记录上一个 mse 的位置 
            }

            return new double[] { wk[0, 0], wk[1, 0] };
            // 打印结果
            //Console.WriteLine("定位结果：{0} {1}", wk[0, 0], wk[1, 0]);
            //Console.WriteLine(Math.Sqrt(Math.Pow(wk[0, 0] - xt, 2) + Math.Pow(wk[1, 0] - yt, 2)));
            //Console.WriteLine();
            //for (int i = 0; i < n_data; i++)
            //{
            //    Console.WriteLine("{0} {1}", X[i][0], X[i][1]);
            //}
        }

        //List<double> pwr(List<List<double>> X)
        //{
        //    List<double> P = new List<double>();
        //    for (int i = 0; i < n_data; i++)
        //    {
        //        double d = Math.Sqrt(Math.Pow(xt - X[i][0], 2) + Math.Pow(yt - X[i][1], 2));
        //        P.Add(p0 - 32.45 - 20 * Math.Log10(f) - 20 * Math.Log10(d * 0.001));
        //    }
        //    return P;
        //}

        // 误差函数
        double E(double x, double y, double xi, double yi, double pi, double x1, double y1, double p1)
        {
            return (Math.Pow(x - x1, 2) + Math.Pow(y - y1, 2)) / (Math.Pow(x - xi, 2) + Math.Pow(y - yi, 2)) - Math.Pow(10, (pi - p1) / 10);
        }

        // 数值求导  
        double[] deriv(double x, double y, double xi, double yi, double pi, double x1, double y1, double p1)
        {
            double d1 = Math.Pow(x - x1, 2) + Math.Pow(y - y1, 2);
            double di = Math.Pow(x - xi, 2) + Math.Pow(y - yi, 2);
            double dx = (2 * (x - x1) * di - 2 * (x - xi) * d1) / (di * di);
            double dy = (2 * (y - y1) * di - 2 * (y - yi) * d1) / (di * di);
            return new double[] { dx, dy };
        }
    }
}
