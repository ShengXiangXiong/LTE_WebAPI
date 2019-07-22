using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;

namespace LTE.Calibration
{
    public class RandomToND
    {
        //求随机数平均值方法
        public double Ave(double[] a)
        {
            double sum = 0;
            foreach (double d in a)
            {
                sum = sum + d;
            }
            double ave = sum / a.Length;

            return ave;
        }
        //求随机数方差方法
        public double Var(double[] v)
        {
            //    double tt = 2;
            //double mm = tt ^ 2;

            double sum1 = 0;
            for (int i = 0; i < v.Length; i++)
            {
                double temp = v[i] * v[i];
                sum1 = sum1 + temp;

            }

            double sum = 0;
            foreach (double d in v)
            {
                sum = sum + d;
            }

            double var = sum1 / v.Length - (sum / v.Length) * (sum / v.Length);
            return var;
        }

        //求正态分布的随机数
        public void Fenbu(double[] f)
        {
            //double fenbu=new double[f.Length ];
            for (int i = 0; i < f.Length; i++)
            {
                double a = 0, b = 0;
                a = Math.Sqrt((-2) * Math.Log(f[i], Math.E));
                b = Math.Cos(2 * Math.PI * f[i]);
                f[i] = a * b * 0.3 + 1;

            }

        }

        /// <summary>
        ///  
        /// </summary>
        /// <param name="BigObj"> 整体目标的随机扰动误差</param>
        /// <param name="SmaObj">局部目标的随机扰动误差</param>
        public void RandomTo(ref double[] BigObj, ref double[] SmaObj)
        {

            //生成（0，1）之间的随机数
            Random ran = new Random();

            for (int i = 0; i < BigObj.Length; i++)
            {
                BigObj[i] = ran.Next(850, 1250) * 0.01;
            }
            //调用Ave方法、Var方法求得随机数均值和方差
            double BigAvenum = Ave(BigObj);
            double BigVarnum = Var(BigObj);

            for (int i = 0; i < SmaObj.Length; i++)
            {
                SmaObj[i] = ran.Next(250, 650) * 0.01;
            }
            double SmaAvenum = Ave(SmaObj);
            double SmaVarnum = Var(SmaObj);

            //写入文件
            //将100个随机数，均值，方差保存到文件“SourceData.txt”中
            string Datapath = (@"BigSourceData.txt");

            FileStream fs = new FileStream(Datapath, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);

            for (int j = 0; j < BigObj.Length; j++)
            {
                sw.WriteLine(BigObj[j]);

            }

            sw.Write("大目标的随机数均值和方差分别是{0}和{1}", BigAvenum, BigVarnum);
            sw.Close();

            string DatapathSma = (@"SmaSourceData.txt");


            FileStream fsSmal = new FileStream(DatapathSma, FileMode.Create);
            StreamWriter swSmal = new StreamWriter(fsSmal);

            for (int j = 0; j < SmaObj.Length; j++)
            {
                swSmal.WriteLine(SmaObj[j]);

            }

            swSmal.Write("小目标的随机数均值和方差分别是{0}和{1}", SmaAvenum, SmaVarnum);
            swSmal.Close();

        }
    }
}
