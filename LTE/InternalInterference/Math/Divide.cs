using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace LTE.InternalInterference
{
    // 参考文献：基于启发式分割算法的气候突变检测研究 封国林 龚志强 董文杰 李建平
    public class Divide
    {
        // 下标从1开始

        //**********输入***********************
        public int n;           // 数据个数
        public double[] x;      // 待检测的数据
        public double P0;       // 显著性水平门限值，低于此值的不再分割，可取0.5到0.95
        public double L0;       // 最小分割尺度，子段长度小于此值的不再分割，一般不小于25

        //**********输出***********************
        public int[] FLAG;  // 分割点标记，长度与X相同
        public List<int> posV;          // 分割点位置

        //**********中间变量********************
        public double[] T;         // t检验序列
        public double[] m;         // 中位数滤波后

        public Divide(int L, double P, ref List<double> x1)
        {
            n = x1.Count;
            x = new double[n + 1];
            FLAG = new int[n + 1];
            T = new double[n + 1];
            m = new double[n + 1];
            posV = new List<int>();
            P0 = P;
            L0 = L;

            for (int i = 0; i < n; i++)
                x[i + 1] = x1[i];
        }

        // 主函数
        public void run()
        {
            for (int i = 0; i <= n; i++)
                FLAG[i] = 0;

            FLAG[1] = FLAG[n] = 1;
            posV.Add(1);

            mid();       // 中值滤波
            BGA(1, n);   // 序列分割

            posV.Add(n);
            posV.Sort();
        }

        //计算t检验统计序列的子函数，区间[start, end]
        //如果可分割，返回分割点，否则返回-1
        int Tseries(int start, int end)
        {
            int len = end - start + 1;

            //参数初始化
            double Tmax;    //t检验序列最大值
            double PTmax;   //Tmax对应的统计显著性    
            int pos;        //t检验序列最大值对应的下标

            int n1, n2;
            double mean_x1;  //左边部分的均值
            double std_x1;   //左边部分的标准差
            double mean_x2;  //右边部分的均值
            double std_x2;   //右边部分的标准差

            if (start >= n)
                return 0;

            //创建t检验序列
            for (int i = start + 1; i < end; i++)   //最左边以及最右边的点没有对应的t检验值（或者说，其值初始化为0）
            {
                mean_x1 = std_x1 = mean_x2 = std_x2 = 0;

                n1 = i - start + 1;  //左边序列的长度
                n2 = end - i + 1;    //右边序列的长度

                int j;
                for (j = start; j <= i; j++)
                    mean_x1 += x[j];
                mean_x1 /= n1;      //左边部分的均值

                for (j = i; j <= end; j++)
                    mean_x2 += x[j];
                mean_x2 /= n2;      //右边部分的均值

                for (j = start; j <= i; j++)
                    std_x1 += Math.Pow((x[j] - mean_x1), 2);
                std_x1 = Math.Sqrt(std_x1 / n1);  //左边部分的标准差

                for (j = i; j <= end; j++)
                    std_x2 += Math.Pow((x[j] - mean_x2), 2);
                std_x2 = Math.Sqrt(std_x2 / n2);    //右边部分的标准差

                //计算合并偏差
                double SD = (Math.Sqrt(1.0 / n1) + (1.0 / n2)) * Math.Sqrt(((n1 - 1.0) * Math.Pow(std_x1, 2) + (n2 - 1.0) * Math.Pow(std_x2, 2)) / (n1 + n2 - 2.0));
                T[i] = Math.Abs((mean_x1 - mean_x2) / SD);
            }

            //t检验序列最大值
            Tmax = T[start + 1];
            pos = start + 1;
            for (int i = start + 2; i < end; i++)
            {
                if (T[i] > Tmax)
                {
                    Tmax = T[i];
                    pos = i;
                }
            }

            //Tmax对应的统计显著性
            double Eta = 4.19 * Math.Log(len) - 11.54; //计算PTmax用的参数
            double Delta = 0.40;                 //计算PTmax用的参数
            double e = 1.0e-3;                   //计算PTmax用的参数
            int v = len - 2;                     //计算PTmax用的参数
            double c = v / (v + Math.Pow(Tmax, 2));
            PTmax = 1 - this.beta2(Delta * Eta, Delta, c, e);  //调用不完全beta函数 

            if (PTmax >= P0)  //满足分割条件，进行分割
                return pos;
            else
                return -1;
        }

        //非平稳时间序列突变检测的启发式分割算法——BG算法
        //区间范围：[start, end]
        public void BGA(int start, int end)
        {
            //产生第一个突变点
            int pos = Tseries(start, end);
            if (pos > 0)
            {
                FLAG[pos] = 1;
                posV.Add(pos);
            }

            int[] p = new int[n + 1];
            while (true)
            {
                for (int i = start; i <= end; i++)
                    p[i] = 0;

                int num = 0;  //当前子段数目
                int flagNum = 0;
                int TC = 0;
                for (int i = start; i <= end; i++)
                    if (FLAG[i] > 0)
                        p[num++] = i;
                num--;
                for (int i = 0; i < num; i++)  //每一子段
                {
                    int left = p[i];
                    int right = p[i + 1];
                    int subLen = right - left + 1;

                    if (subLen >= L0)
                    {
                        flagNum++;
                        pos = Tseries(left, right);
                        if (pos > 0)
                        {
                            FLAG[pos] = 1;
                            posV.Add(pos);
                            TC++;
                        }
                    }
                }

                if (TC == 0)  //如果所有子段都不满足分割条件
                    return;
                if (flagNum == 0)  //所有子段长度都小于临界值
                    return;
            }
        }

        void mid()  //中位数滤波
        {
            m[1] = x[1];
            double temp;
            for (int i = 2; i < n; i++)
            {
                temp = (x[i - 1] + x[i] + x[i + 1]) / 3;
                if (Math.Abs(temp - x[i]) > 3)
                    m[i] = temp;
                else
                    m[i] = x[i];
            }
            m[n] = x[n];
            for (int i = 0; i <= n; i++)
                x[i] = m[i];
        }

        /*=============================================================
        // 函 数 名：gammln
        // 功能描述：求解伽马函数的值的自然对数
        // 输入参数：x 求值的自变量
        // 返 回 值：伽马函数的值的自然对数
        //==============================================================*/
        double gammln(double x)
        {
            int i;
            double t, s;
            List<double> c = new List<double>{76.18009172947148,
            -86.50532032941677,  24.01409824083091,
            -1.231739572450155,  0.1208650973866179e-2,
            -0.5395239384953e-5};          /* 系数*/
            if (x < 0)
            {
                Console.WriteLine("incorrect input parameter");
                return 0;
            }
            s = 1.000000000190015;
            for (i = 0; i < 6; i++)                        /* 级数和*/
                s = s + c[i] / (x + i);
            t = x + 4.5;                                /* 已取对数*/
            t = t - (x - 0.5) * Math.Log(t);
            t = Math.Log(2.5066282746310005 * s) - t; /* 最后结果*/
            return t;
        }

        // 函 数 名：beta2
        // 功能描述：求解不完全贝塔积分的值
        // 输入参数：a 自变量a的值。要求a>0。
        //           b 自变量b的值。要求b>0。
        //           x 自变量x的值，要求0<=x<=1。
        //           e1 精度要求，当两次递推的值变化率小于e1时，认为已收敛
        // 返 回 值：不完全贝塔函数的值
        //==============================================================*/

        const int NMAX = 100;                         /* 迭代的最大次数*/
        const double EULER = 0.5772156649;
        const double FPMIN = 1.0e-30;                    /* 为防止除0使用的常数*/

        //double subf(double a, double b, double x, double e1); /* 计算连分式级数需要的变量和函数*/

        double beta2(double a, double b, double x, double e1)
        {
            double t;
            if ((x < 0.0) || (x > 1.0) || (a <= 0.0) || (b <= 0.0))
            {
                //Console.WriteLine("Bad input parameter");
                return 0;
            }
            else if (x == 0.0)                                /* x为0的情况*/
            {
                t = 0.0;
                return t;
            }
            else if (x == 1.0)                                /* x为1的情况*/
            {
                t = 1.0;
                return t;
            }
            else if (x > (a + 1.0) / (a + b + 2.0))
            {
                t = Math.Exp(gammln(a + b) - gammln(a) - gammln(b) + a * Math.Log(x) + b * Math.Log(1.0 - x)); /* 系数*/
                t = 1.0 - t * this.subf(b, a, 1.0 - x, e1) / b;               /* 使用连分式级数*/
                return t;
            }
            else
            {
                t = Math.Exp(gammln(a + b) - gammln(a) - gammln(b) + a * Math.Log(x) + b * Math.Log(1.0 - x)); /* 系数*/
                t = t * subf(a, b, x, e1) / a;                       /* 使用连分式级数*/
                return t;
            }
        }

        double subf(double a, double b, double x, double e1)
        {
            int n;
            double t, del, an, c, d;
            c = 1.0;
            d = 1.0 - (a + b) * x / (a + 1.0);
            if (Math.Abs(d) < FPMIN)
                d = FPMIN;
            d = 1.0 / d;
            t = d;
            for (n = 1; n < NMAX; n++)
            {
                an = n * (b - n) * x / ((a + 2.0 * n - 1.0) * (a + 2.0 * n));  /* 第2n节的系数a,此节的系数b为1*/
                d = an * d + 1.0;                              /* 计算d*/
                c = 1.0 + an / c;                              /* 计算c*/
                if (Math.Abs(d) < FPMIN)                       /* 检查cd的范围*/
                    d = FPMIN;
                if (Math.Abs(c) < FPMIN)
                    c = FPMIN;
                d = 1.0 / d;
                del = d * c;
                t = t * del;
                an = -(a + n) * (a + b + n) * x / ((a + 2.0 * n) * (a + 1.0 + 2.0 * n));/* 第2n+1节*/
                d = 1.0 + an * d;
                c = 1.0 + an / c;
                if (Math.Abs(d) < FPMIN)
                    d = FPMIN;
                if (Math.Abs(c) < FPMIN)
                    c = FPMIN;
                d = 1.0 / d;
                del = d * c;
                t = t * del;
                if (Math.Abs(del - 1.0) < e1)                       /* 级数部分已经收敛*/
                    return t;
            }
            //Console.WriteLine("没有收敛");          
            return t;
        }
    }
}
