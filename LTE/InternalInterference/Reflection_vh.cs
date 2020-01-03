// Decompiled with JetBrains decompiler
// Type: ReflectionCoefficient.Reflection_vh
// Assembly: ReflectionCoefficient, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 63DF204A-8332-41C6-BAD7-6B7BCF904AFB

using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace LTE.InternalInterference
{
    public class Reflection_vh
    {
        public ArrayList GetReflection(double e, double econductivity, double angle)
        {
            Complex complex = new Complex(e, -econductivity * 60.0 / 3.0);
            ArrayList arrayList = new ArrayList();
            double num = 3.1415926 * angle / 180.0;
            arrayList.Add((object)Complex.Abs(((Complex)Math.Sin(num) - Complex.Sqrt(complex - (Complex)Math.Pow(Math.Cos(num), 2.0))) / ((Complex)Math.Sin(num) + Complex.Sqrt(complex - (Complex)Math.Pow(Math.Cos(num), 2.0)))));
            arrayList.Add((object)Complex.Abs((complex * (Complex)Math.Sin(num) - Complex.Sqrt(complex - (Complex)Math.Pow(Math.Cos(num), 2.0))) / (complex * (Complex)Math.Sin(num) + Complex.Sqrt(complex - (Complex)Math.Pow(Math.Cos(num), 2.0)))));
            return arrayList;
        }

        public double Getangel22(double angle)
        {
            SortedList<double, double> sortedList = new SortedList<double, double>();
            sortedList.Add(0.0, -95.0);
            sortedList.Add(5.0, -80.0);
            sortedList.Add(10.0, -75.0);
            sortedList.Add(15.0, -70.0);
            sortedList.Add(25.0, -72.0);
            sortedList.Add(30.0, -62.0);
            sortedList.Add(50.0, -59.0);
            sortedList.Add(125.0, -47.0);
            sortedList.Add(150.0, -40.0);
            sortedList.Add(160.0, -30.0);
            sortedList.Add(175.0, -38.0);
            sortedList.Add(200.0, -30.0);
            sortedList.Add(225.0, -46.0);
            sortedList.Add(250.0, -55.0);
            sortedList.Add(275.0, -57.0);
            double num = 0.0;
            for (int index = 0; index < sortedList.Count; ++index)
            {
                if (sortedList.Keys[index] <= (double)(int)angle)
                    num = (sortedList.Values[index + 1] - sortedList.Values[index]) / (sortedList.Keys[index + 1] - sortedList.Keys[index]) * ((double)(int)angle - sortedList.Keys[index]) + sortedList.Values[index];
            }
            return num;
        }

        public double Getangel248(double angle)
        {
            SortedList<double, double> sortedList = new SortedList<double, double>();
            sortedList.Add(0.0, -55.0);
            sortedList.Add(25.0, -52.0);
            sortedList.Add(30.0, -50.0);
            sortedList.Add(50.0, -42.0);
            sortedList.Add(70.0, -30.0);
            sortedList.Add(90.0, -35.0);
            sortedList.Add(100.0, -34.0);
            sortedList.Add(110.0, -29.0);
            sortedList.Add(125.0, -38.0);
            sortedList.Add(150.0, -50.0);
            sortedList.Add(175.0, -55.0);
            sortedList.Add(200.0, -60.0);
            sortedList.Add(250.0, -68.0);
            sortedList.Add(275.0, -70.0);
            double num = 0.0;
            for (int index = 0; index < sortedList.Count; ++index)
            {
                if (sortedList.Keys[index] <= (double)(int)angle)
                    num = (sortedList.Values[index + 1] - sortedList.Values[index]) / (sortedList.Keys[index + 1] - sortedList.Keys[index]) * ((double)(int)angle - sortedList.Keys[index]) + sortedList.Values[index];
            }
            return num;
        }

        public double Getangel75(double angle)
        {
            SortedList<double, double> sortedList = new SortedList<double, double>();
            sortedList.Add(0.0, 0.02);
            sortedList.Add(30.0, 0.01);
            sortedList.Add(45.0, 0.02);
            sortedList.Add(70.0, 0.04);
            sortedList.Add(90.0, 0.75);
            sortedList.Add(100.0, 0.16);
            sortedList.Add(110.0, 0.25);
            sortedList.Add(135.0, 0.11);
            sortedList.Add(150.0, 0.1);
            sortedList.Add(180.0, 0.09);
            sortedList.Add(225.0, 0.15);
            sortedList.Add(250.0, 0.44);
            sortedList.Add(270.0, 0.01);
            double num = 0.0;
            for (int index = 0; index < sortedList.Count; ++index)
            {
                if (sortedList.Keys[index] <= (double)(int)angle)
                    num = (sortedList.Values[index + 1] - sortedList.Values[index]) / (sortedList.Keys[index + 1] - sortedList.Keys[index]) * ((double)(int)angle - sortedList.Keys[index]) + sortedList.Values[index];
            }
            return num;
        }

        public double Getangel135(double angle)
        {
            SortedList<double, double> sortedList = new SortedList<double, double>();
            sortedList.Add(0.0, 0.01);
            sortedList.Add(25.0, 0.1);
            sortedList.Add(45.0, 0.25);
            sortedList.Add(50.0, 0.28);
            sortedList.Add(70.0, 0.15);
            sortedList.Add(90.0, 0.1);
            sortedList.Add(135.0, 0.08);
            sortedList.Add(180.0, 0.1);
            sortedList.Add(200.0, 0.13);
            sortedList.Add(225.0, 0.28);
            sortedList.Add(250.0, 0.07);
            sortedList.Add(270.0, 0.01);
            double num = 0.0;
            for (int index = 0; index < sortedList.Count; ++index)
            {
                if (sortedList.Keys[index] <= (double)(int)angle)
                    num = (sortedList.Values[index + 1] - sortedList.Values[index]) / (sortedList.Keys[index + 1] - sortedList.Keys[index]) * ((double)(int)angle - sortedList.Keys[index]) + sortedList.Values[index];
            }
            return num;
        }
    }
}
