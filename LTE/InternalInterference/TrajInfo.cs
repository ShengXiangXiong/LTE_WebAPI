using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LTE.Geometric;
using System.Collections;
using System.IO;
using LTE.Calibration;

namespace LTE.InternalInterference
{
    // 用于系数校正
    public class TrajInfo
    {
        public Dictionary<int, RayInfo> traj;
        public double sumPwrDbm;
        public double sumReceivePwrW;

        public TrajInfo()
        {
            traj = new Dictionary<int, RayInfo>();
            sumPwrDbm = 0;
            sumReceivePwrW = 0;
        }

        public double convertdbm2w(double dbm) {
            return Math.Pow(10, (dbm / 10) - 3);
        }

        public double convertw2dbm(double w)
        {
            double pwrDbm=10 * (Math.Log10(w) + 3);
            
            //临时调试
            if (double.IsNaN(pwrDbm))
            {
                int testTest = 1;
            }

            return pwrDbm;
        }

        // 计算场强
        // coef：第一维为场景，第二维为各校正系数，依次为直射、反射、绕射
        public double calc_Max(ref double[,] coef, int scenNum, int frequncy, double dtTemp,string keyCell,string outPath)
        {
            StreamWriter sw = new StreamWriter(outPath, true, Encoding.Default);

            sw.WriteLine("-------------------------------------------------------");
            sw.WriteLine("栅格id：" + keyCell);

            int guijiID = 0;


            sumReceivePwrW = 0;

            double nata = 300.0 / (1805 + 0.2 * (frequncy - 511));  // f(n) = 1805 + 0.2*(n－511) MHz  // 小区频率，与 

            foreach (int key in traj.Keys)  // 当前栅格收到的某个小区的每个轨迹
            {
                double[] guijiLeixingTongJi = new double[3];

                guijiID += 1;

                bool isZhiShe = true;

                double distance = 0;         // 射线传播总距离
                double[] scenDistance = new double[scenNum];
                double reflectedR = 1;       // 反射系数
                double diffrctedR = 1;       // 绕射系数

                for (int j = 0; j < traj[key].rayList.Count; ++j)  // 对于一条轨迹中的每条射线
                {
                    double defAngle = traj[key].rayList[j].DefAngle;//偏转角 弧度制 数值【0 ， 3.1415926】
                    double defAngleDegree = defAngle * 180 / 3.1415926;

                    distance += traj[key].rayList[j].distance;
                    for (int k = 0; k < scenNum; k++)
                    {
                        scenDistance[k] += traj[key].rayList[j].trajScen[k];
                    }

                    if (traj[key].rayList[j].rayType == RayType.VReflection || traj[key].rayList[j].rayType == RayType.HReflection)
                    {
                        //reflectedR *= traj[key].rayList[j].attenuation * coef[traj[key].rayList[j].endPointScen, 1];
                        double A = coef[traj[key].rayList[j].endPointScen, 1];
                        double B = coef[traj[key].rayList[j].endPointScen, 2];
                        double C = coef[traj[key].rayList[j].endPointScen, 3];
                        reflectedR *= Math.Pow(10,-(A * defAngle * defAngle + B * defAngle + C)); // 二次拟合函数
                    }
                    else if (traj[key].rayList[j].rayType == RayType.HDiffraction || traj[key].rayList[j].rayType == RayType.VDiffraction)
                    {
                        //diffrctedR *= traj[key].rayList[j].attenuation * coef[traj[key].rayList[j].endPointScen, 2];
                        double D = coef[traj[key].rayList[j].endPointScen, 4];
                        double E = coef[traj[key].rayList[j].endPointScen, 5];
                        double F = coef[traj[key].rayList[j].endPointScen, 6];
                        diffrctedR *= Math.Pow(10,-(D * defAngle * defAngle + E * defAngle + F)); // 二次拟合函数

                        //临时调试
                        if (double.IsNaN(diffrctedR))
                        {
                            int testTest = 1;
                        }

                        //diffrctedR = 1;//
                    }
                }


                if (isZhiShe == true)
                {
                    guijiLeixingTongJi[0] += 1;
                }


                double amendDirSum = 0;
                for (int j = 0; j < scenNum; j++)
                    amendDirSum += coef[j, 0] * (scenDistance[j] / distance);


                double p0 = traj[key].emitPwrW;

                double receivePwr = Math.Pow(nata / (4 * Math.PI), 2) * (p0 / Math.Pow(distance, (2 + amendDirSum))) * Math.Pow(reflectedR, 2) * Math.Pow(diffrctedR, 2);

                double tmpDBM = convertw2dbm(receivePwr);

                sw.WriteLine(guijiID.ToString() + ": power :" + receivePwr.ToString() + "  dbm:" + tmpDBM.ToString() + " 直反绕次数统计:" + guijiLeixingTongJi[0].ToString() + " " + guijiLeixingTongJi[1].ToString() + " " + guijiLeixingTongJi[2].ToString()+" P0 "+ traj[key].emitPwrW.ToString());

                sumReceivePwrW += receivePwr;
            }

            sumPwrDbm = convertw2dbm(sumReceivePwrW);

            sw.WriteLine("合计: power " +"真实路测值："+convertdbm2w(dtTemp).ToString()+" 预测值： "+ sumReceivePwrW.ToString() + "  dbm:"+"真实路测值：" + dtTemp.ToString()+ " 预测值："+sumPwrDbm.ToString());
            sw.Close();

            return sumPwrDbm;
        }


        // 计算场强
        // coef：第一维为场景，第二维为各校正系数，依次为直射、反射、绕射
        public double calc(ref double[,] coef, int scenNum, int frequncy)
        {
            sumReceivePwrW = 0;

            double nata = 300.0 / (1805 + 0.2 * (frequncy - 511));  // f(n) = 1805 + 0.2*(n－511) MHz  // 小区频率，与 

            foreach (int key in traj.Keys)  // 对于同一个栅格中的每条轨迹
            {
                double distance = 0;         // 射线传播总距离
                double[] scenDistance = new double[scenNum];
                double reflectedR = 1;       // 反射系数
                double diffrctedR = 1;       // 绕射系数

                //todo 若有多条直射、含反射、含绕射的射线，只取最强的一条 

                //调试用
                List<NodeInfo> rayListTmp = traj[key].rayList;

                for (int j = 0; j < traj[key].rayList.Count; ++j)  // 对于一条轨迹中的每条射线
                {
                    distance += traj[key].rayList[j].distance;
                    for (int k = 0; k < scenNum; k++)
                    {
                        scenDistance[k] += traj[key].rayList[j].trajScen[k];
                    }

                    if (traj[key].rayList[j].rayType == RayType.VReflection || traj[key].rayList[j].rayType == RayType.HReflection)
                    {
                        reflectedR *= traj[key].rayList[j].attenuation * coef[traj[key].rayList[j].endPointScen, 1];
                    }
                    else if (traj[key].rayList[j].rayType == RayType.HDiffraction || traj[key].rayList[j].rayType == RayType.VDiffraction)
                    {
                        diffrctedR *= traj[key].rayList[j].attenuation * coef[traj[key].rayList[j].endPointScen, 2];
                        //diffrctedR = 1;//
                    }
                }

                double amendDirSum = 0;
                for (int j = 0; j < scenNum; j++)
                    amendDirSum += coef[j, 0] * (scenDistance[j] / distance);

                double receivePwrDir = Math.Pow(nata / (4 * Math.PI), 2) * (traj[key].emitPwrW / Math.Pow(distance, (2 + amendDirSum))) * Math.Pow(reflectedR, 2);
                double receivePwrDirDbm = convertw2dbm(receivePwrDir);
                double receivePwr =receivePwrDir* Math.Pow(diffrctedR, 2);
                double receivePwrDbm=convertw2dbm(receivePwr);

                sumReceivePwrW += receivePwr;
            }

            sumPwrDbm = convertw2dbm(sumReceivePwrW);
            return sumPwrDbm;
        }

        // 计算轨迹接收值，用二次函数拟合
        public double calcWithNewFormula(ref double[,] coef, int scenNum, int frequency,ref ErrorGridStaticInfo errorGridStaticInfo){

            // https://www.docin.com/p-1056206660.html


            sumReceivePwrW = 0;

            double nata = 300.0 / (1805 + 0.2 * (frequency - 511));  // f(n) = 1805 + 0.2*(n－511) MHz  // 小区频率，与 

            //直射只取最强的一条
            double maxDirReceivePwr = double.MinValue;//记录最强直射轨迹接收功率
            int maxDirTrajKey = -1;//记录最强直射轨迹的轨迹key
            double maxDirTrajDistance = -1;//记录最强直射轨迹的传播距离
            double maxDirP0 =-1;
            double maxDirD1 =-1;
            double maxDirAmendDirSum = -1;
            double maxDirD2 = -1;

            Boolean hasDirTrajFlag = true;//栅格中包含只有直射线段的轨迹


            foreach (int key in traj.Keys)  // 对于同一个栅格中的每条轨迹
            {
                double distance = 0;         // 射线传播总距离
                double[] scenDistance = new double[scenNum];
                double reflectedR = 1;       // 反射系数
                double diffrctedR = 1;       // 绕射系数

                //todo 若有多条直射、含反射、含绕射的射线，只取最强的一条 

                for (int j = 0; j < traj[key].rayList.Count; ++j)  // 对于一条轨迹中的每条射线
                {
                    double defAngle = traj[key].rayList[j].DefAngle;//偏转角 弧度制 数值【0 ， 3.1415926】
                    double defAngleDegree = defAngle * 180 / 3.1415926;  
 
                    distance += traj[key].rayList[j].distance;
                    for (int k = 0; k < scenNum; k++)
                    {
                        scenDistance[k] += traj[key].rayList[j].trajScen[k];
                    }
       
                    if (traj[key].rayList[j].rayType == RayType.VReflection || traj[key].rayList[j].rayType == RayType.HReflection)
                    {
                        hasDirTrajFlag = false;

                        //reflectedR *= traj[key].rayList[j].attenuation * coef[traj[key].rayList[j].endPointScen, 1];
                        double A = coef[traj[key].rayList[j].endPointScen, 1];
                        double B = coef[traj[key].rayList[j].endPointScen, 2];
                        double C = coef[traj[key].rayList[j].endPointScen, 3];

                        //reflectedR *= 0.5*Math.Pow(10, -(A * defAngle * defAngle + B * defAngle + C)); // 指数二次拟合函数
                        reflectedR *= A * defAngle * defAngle + B * defAngle + C; // 二次拟合函数

                    }
                    else if (traj[key].rayList[j].rayType == RayType.HDiffraction || traj[key].rayList[j].rayType == RayType.VDiffraction)
                    {
                        hasDirTrajFlag = false;

                        //diffrctedR *= traj[key].rayList[j].attenuation * coef[traj[key].rayList[j].endPointScen, 2];
                        double D = coef[traj[key].rayList[j].endPointScen, 4];
                        double E = coef[traj[key].rayList[j].endPointScen, 5];
                        double F = coef[traj[key].rayList[j].endPointScen, 6];
                        
                        //diffrctedR *= 0.1*Math.Pow(10, -(D * defAngle * defAngle + E * defAngle + F)); // 指数二次拟合函数
                        diffrctedR *= D * defAngle * defAngle + E * defAngle + F; // 指二次拟合函数

                        //临时调试
                        if (double.IsNaN(diffrctedR))
                        {
                            int testTest = 1;
                        }

                        //diffrctedR = 1;//
                    }
                }

                double amendDirSum = 0;
                for (int j = 0; j < scenNum; j++)
                    amendDirSum += coef[j, 0] * (scenDistance[j] / distance);



                //把这四个都恢复
                //double receivePwrDir = Math.Pow(nata / (4 * Math.PI), 2) * (traj[key].emitPwrW / Math.Pow(distance, (2 + amendDirSum))) *Math.Pow(reflectedR, 2);
                //double receivePwrDirDbm = convertw2dbm(receivePwrDir);
                //double receivePwr = receivePwrDir * Math.Pow(diffrctedR, 2);
                //double receivePwrDbm = convertw2dbm(receivePwr);

                //调试
                double p0 = traj[key].emitPwrW;
                double d1 = Math.Pow(nata / (4 * Math.PI), 2);
                double d2 = 1 / Math.Pow(distance, (2 + amendDirSum));
                double d3 = Math.Pow(reflectedR, 2);
                double d4 = Math.Pow(diffrctedR, 2);
                double receivePwr = p0 * d1 * d2 * d3 * d4;

                if (hasDirTrajFlag)//如果包含只有直射线段的轨迹
                {
                    if (receivePwr > maxDirReceivePwr) {//对于直射轨迹，更新最大直射轨迹的值，后面将最大值累加到误差，这里先不累加
                        maxDirReceivePwr = receivePwr;
                        maxDirTrajKey = key;//更新轨迹Key
                        maxDirTrajDistance = distance;//更新距离

                        maxDirP0 = p0;
                        maxDirD1 = d1;
                        maxDirD2 = d2;
                        maxDirAmendDirSum = amendDirSum;
                    } 
                }
                else {
                    sumReceivePwrW += receivePwr;
                }


                //临时调试
                if (double.IsNaN(sumReceivePwrW))
                {
                    int testTest = 1;
                }

            }

            //sumPwrDbm = convertw2dbm(sumReceivePwrW);

            if (hasDirTrajFlag)
            {//如果有只有直射线段的轨迹
                sumReceivePwrW += maxDirReceivePwr;//累加最大直射误差
                if (errorGridStaticInfo != null) {//只在最后统计使用时需要修改最强直射轨迹结构体
                    errorGridStaticInfo.maxPowerTrajKey = maxDirTrajKey;//更新
                    errorGridStaticInfo.distance = maxDirTrajDistance;//更新
                    errorGridStaticInfo.p0 = maxDirP0 ;
                    errorGridStaticInfo.d1 = maxDirD1  ;
                    errorGridStaticInfo.d2 = maxDirD2  ;
                    errorGridStaticInfo.amendDirSum=maxDirAmendDirSum ;
                }          
            }

            //临时调试
            if (sumReceivePwrW < 0)
            {
                int testTest = 1;
            }
            if (sumReceivePwrW > 0)
            {
                int testTest = 1;
            }
            if (sumReceivePwrW == 0)
            {
                int testTest = 1;
            }
            if (double.IsNaN(sumReceivePwrW)  )
            {
                int testTest = 1;
            }
           
            //临时调试
            sumPwrDbm = 10 * (Math.Log10(sumReceivePwrW) + 3);

            //临时调试
            if (double.IsNaN(sumPwrDbm))
            {
                int testTest = 1;
            }

            if (sumPwrDbm > -200)
            {
                int testTest = 1;
            }
            if (sumPwrDbm == System.Double.NaN)
            {
                int testTest = 1;
            }  
            
            return sumPwrDbm;
        }


        // 计算轨迹接收值，用二次函数拟合，用于统计最小二乘矩阵
        public void calcWithNewFormulaToMatrix(ref double[,] coef, int scenNum, int frequency,double dtPwrDbm,ref double[] W, ref double y)
        {
            y = -1;

            // https://www.docin.com/p-1056206660.html


            sumReceivePwrW = 0;

            double nata = 300.0 / (1805 + 0.2 * (frequency - 511));  // f(n) = 1805 + 0.2*(n－511) MHz  // 小区频率，与 

            //直射只取最强的一条
            double maxDirReceivePwr = double.MinValue;//记录最强直射轨迹接收功率
            int maxDirTrajKey = -1;//记录最强直射轨迹的轨迹key
            double maxDirTrajDistance = -1;//记录最强直射轨迹的传播距离
            double maxDirP0 = -1;
            double maxDirD1 = -1;
            double maxDirAmendDirSum = -1;
            double maxDirD2 = -1;
            double[] maxScenDistance = new double[scenNum];

            Boolean hasDirTrajFlag = true;//栅格中包含只有直射线段的轨迹


            foreach (int key in traj.Keys)  // 对于同一个栅格中的每条轨迹
            {
                double distance = 0;         // 射线传播总距离
                double[] scenDistance = new double[scenNum];
                double reflectedR = 1;       // 反射系数
                double diffrctedR = 1;       // 绕射系数

                //todo 若有多条直射、含反射、含绕射的射线，只取最强的一条 

                for (int j = 0; j < traj[key].rayList.Count; ++j)  // 对于一条轨迹中的每条射线
                {
                    double defAngle = traj[key].rayList[j].DefAngle;//偏转角 弧度制 数值【0 ， 3.1415926】
                    double defAngleDegree = defAngle * 180 / 3.1415926;

                    distance += traj[key].rayList[j].distance;
                    for (int k = 0; k < scenNum; k++)
                    {
                        scenDistance[k] += traj[key].rayList[j].trajScen[k];
                    }

                    if (traj[key].rayList[j].rayType == RayType.VReflection || traj[key].rayList[j].rayType == RayType.HReflection)
                    {
                        hasDirTrajFlag = false;

                        //reflectedR *= traj[key].rayList[j].attenuation * coef[traj[key].rayList[j].endPointScen, 1];
                        double A = coef[traj[key].rayList[j].endPointScen, 1];
                        double B = coef[traj[key].rayList[j].endPointScen, 2];
                        double C = coef[traj[key].rayList[j].endPointScen, 3];

                        //reflectedR *= 0.5*Math.Pow(10, -(A * defAngle * defAngle + B * defAngle + C)); // 指数二次拟合函数
                        reflectedR *= A * defAngle * defAngle + B * defAngle + C; // 二次拟合函数

                    }
                    else if (traj[key].rayList[j].rayType == RayType.HDiffraction || traj[key].rayList[j].rayType == RayType.VDiffraction)
                    {
                        hasDirTrajFlag = false;

                        //diffrctedR *= traj[key].rayList[j].attenuation * coef[traj[key].rayList[j].endPointScen, 2];
                        double D = coef[traj[key].rayList[j].endPointScen, 4];
                        double E = coef[traj[key].rayList[j].endPointScen, 5];
                        double F = coef[traj[key].rayList[j].endPointScen, 6];

                        //diffrctedR *= 0.1*Math.Pow(10, -(D * defAngle * defAngle + E * defAngle + F)); // 指数二次拟合函数
                        diffrctedR *= D * defAngle * defAngle + E * defAngle + F; // 指二次拟合函数

                        //临时调试
                        if (double.IsNaN(diffrctedR))
                        {
                            int testTest = 1;
                        }

                        //diffrctedR = 1;//
                    }
                }

                double amendDirSum = 0;
                for (int j = 0; j < scenNum; j++)
                    amendDirSum += coef[j, 0] * (scenDistance[j] / distance);



                //把这四个都恢复
                //double receivePwrDir = Math.Pow(nata / (4 * Math.PI), 2) * (traj[key].emitPwrW / Math.Pow(distance, (2 + amendDirSum))) *Math.Pow(reflectedR, 2);
                //double receivePwrDirDbm = convertw2dbm(receivePwrDir);
                //double receivePwr = receivePwrDir * Math.Pow(diffrctedR, 2);
                //double receivePwrDbm = convertw2dbm(receivePwr);

                //调试
                double p0 = traj[key].emitPwrW;
                double d1 = Math.Pow(nata / (4 * Math.PI), 2);
                double d2 = 1 / Math.Pow(distance, (2 + amendDirSum));
                double d3 = Math.Pow(reflectedR, 2);
                double d4 = Math.Pow(diffrctedR, 2);
                double receivePwr = p0 * d1 * d2 * d3 * d4;

                if (hasDirTrajFlag)//如果包含只有直射线段的轨迹
                {
                    if (receivePwr > maxDirReceivePwr)
                    {//对于直射轨迹，更新最大直射轨迹的值，后面将最大值累加到误差，这里先不累加
                        maxDirReceivePwr = receivePwr;
                        maxDirTrajKey = key;//更新轨迹Key
                        maxDirTrajDistance = distance;//更新距离

                        maxDirP0 = p0;
                        maxDirD1 = d1;
                        maxDirD2 = d2;
                        maxDirAmendDirSum = amendDirSum;
                        for (int k = 0; k < scenNum; k++)
                        {
                            maxScenDistance[k] = scenDistance[k];
                        }
                    }
                }
                else
                {
                    sumReceivePwrW += receivePwr;
                }

            }

            //sumPwrDbm = convertw2dbm(sumReceivePwrW);

            if (hasDirTrajFlag)
            {//如果有只有直射线段的轨迹
                sumReceivePwrW += maxDirReceivePwr;//累加最大直射误差


                //更新用于最小二乘的矩阵W，y
                for (int k = 0; k < scenNum; k++)
                {
                    W[k] = maxScenDistance[k]/ maxDirTrajDistance;
                }
                y = (dtPwrDbm - 10 * Math.Log10(maxDirP0 + 3) + 10 * Math.Log10(Math.Pow(nata / (4 * Math.PI), 2)) + 20 * Math.Log10(maxDirTrajDistance)) / 10 * Math.Log10(maxDirTrajDistance);
            }

        }

        // 计算轨迹接收值，calcWithNewFormula简洁版，未使用
        public double calcWithNewFormulaTemp(ref double[,] coef, int scenNum, int frequency)
        {

            // https://www.docin.com/p-1056206660.html

            sumReceivePwrW = 0;

            double nata = 300.0 / (1805 + 0.2 * (frequency - 511));  // f(n) = 1805 + 0.2*(n－511) MHz  // 小区频率，与 

            //直射只取最强的一条
            double maxDirReceivePwr = double.MinValue;//记录最强直射轨迹接收功率

            Boolean hasDirTrajFlag = true;//栅格中包含只有直射线段的轨迹


            foreach (int key in traj.Keys)  // 对于同一个栅格中的每条轨迹
            {
                double distance = 0;         // 射线传播总距离
                double[] scenDistance = new double[scenNum];
                double reflectedR = 1;       // 反射系数
                double diffrctedR = 1;       // 绕射系数

                for (int j = 0; j < traj[key].rayList.Count; ++j)  // 对于一条轨迹中的每条射线
                {
                    double defAngle = traj[key].rayList[j].DefAngle;//偏转角 弧度制 数值【0 ， 3.1415926】
                    double defAngleDegree = defAngle * 180 / 3.1415926;

                    distance += traj[key].rayList[j].distance;
                    for (int k = 0; k < scenNum; k++)
                    {
                        scenDistance[k] += traj[key].rayList[j].trajScen[k];
                    }

                    if (traj[key].rayList[j].rayType == RayType.VReflection || traj[key].rayList[j].rayType == RayType.HReflection)
                    {
                        hasDirTrajFlag = false;

                        //reflectedR *= traj[key].rayList[j].attenuation * coef[traj[key].rayList[j].endPointScen, 1];
                        double A = coef[traj[key].rayList[j].endPointScen, 1];
                        double B = coef[traj[key].rayList[j].endPointScen, 2];
                        double C = coef[traj[key].rayList[j].endPointScen, 3];

                        //reflectedR *= 0.5*Math.Pow(10, -(A * defAngle * defAngle + B * defAngle + C)); // 指数二次拟合函数
                        reflectedR *= A * defAngle * defAngle + B * defAngle + C; // 二次拟合函数

                    }
                    else if (traj[key].rayList[j].rayType == RayType.HDiffraction || traj[key].rayList[j].rayType == RayType.VDiffraction)
                    {
                        hasDirTrajFlag = false;

                        //diffrctedR *= traj[key].rayList[j].attenuation * coef[traj[key].rayList[j].endPointScen, 2];
                        double D = coef[traj[key].rayList[j].endPointScen, 4];
                        double E = coef[traj[key].rayList[j].endPointScen, 5];
                        double F = coef[traj[key].rayList[j].endPointScen, 6];

                        //diffrctedR *= 0.1*Math.Pow(10, -(D * defAngle * defAngle + E * defAngle + F)); // 指数二次拟合函数
                        diffrctedR *= D * defAngle * defAngle + E * defAngle + F; // 指二次拟合函数

                    }
                }

                double amendDirSum = 0;
                for (int j = 0; j < scenNum; j++)
                    amendDirSum += coef[j, 0] * (scenDistance[j] / distance);

                double d1 = Math.Pow(nata / (4 * Math.PI), 2);
                double d2 = traj[key].emitPwrW / Math.Pow(distance, (2 + amendDirSum));
                double d3 = Math.Pow(reflectedR, 2);
                double d4 = Math.Pow(diffrctedR, 2);
                double receivePwr = d1 * d2 * d3 * d4;

                if (hasDirTrajFlag)//如果包含只有直射线段的轨迹
                {
                    if (receivePwr > maxDirReceivePwr)
                    {//对于直射轨迹，更新最大直射轨迹的值，后面将最大值累加到误差，这里先不累加
                        maxDirReceivePwr = receivePwr;
                    }
                }
                else
                {
                    sumReceivePwrW += receivePwr;
                }

            }

            if (hasDirTrajFlag)
            {//如果有只有直射线段的轨迹
                sumReceivePwrW += maxDirReceivePwr;//累加最大直射误差
            }

            sumPwrDbm = 10 * (Math.Log10(sumReceivePwrW) + 3);

            return sumPwrDbm;
        }

        //计算场强，对原公式取对数，以dbm为单位，方便限制校正系数
        public double calcBydbm(ref double[,] coef, int scenNum, int frequncy)
        {
            sumReceivePwrW = 0;

            double nata = 300.0 / (1805 + 0.2 * (frequncy - 511));  // f(n) = 1805 + 0.2*(n－511) MHz  // 小区频率，与 

            foreach (int key in traj.Keys)  // 当前栅格收到的某个小区的每个轨迹
            {
                double distance = 0;         // 射线传播总距离
                double[] scenDistance = new double[scenNum];
                double reflectedR = 1;       // 反射系数
                double diffrctedR = 1;       // 绕射系数

                for (int j = 0; j < traj[key].rayList.Count; ++j)  // 每个轨迹中的每条射线
                {
                    distance += traj[key].rayList[j].distance;
                    for (int k = 0; k < scenNum; k++)
                    {
                        scenDistance[k] += traj[key].rayList[j].trajScen[k];
                    }

                    if (traj[key].rayList[j].rayType == RayType.VReflection || traj[key].rayList[j].rayType == RayType.HReflection)
                    {
                        reflectedR *= traj[key].rayList[j].attenuation * coef[traj[key].rayList[j].endPointScen, 1];
                    }
                    else if (traj[key].rayList[j].rayType == RayType.HDiffraction || traj[key].rayList[j].rayType == RayType.VDiffraction)
                    {
                        diffrctedR *= traj[key].rayList[j].attenuation * coef[traj[key].rayList[j].endPointScen, 2];
                        //diffrctedR = 1;//
                    }
                }

                double amendDirSum = 0;
                for (int j = 0; j < scenNum; j++)
                    amendDirSum += coef[j, 0] * (scenDistance[j] / distance);

                double receivePwrDir = Math.Pow(nata / (4 * Math.PI), 2) * (traj[key].emitPwrW / Math.Pow(distance, (2 + amendDirSum))) * Math.Pow(reflectedR, 2);
                double receivePwrDirDbm = convertw2dbm(receivePwrDir);
                double receivePwr = receivePwrDir * Math.Pow(diffrctedR, 2);
                double receivePwrDbm = convertw2dbm(receivePwr);

                sumReceivePwrW += receivePwr;
            }

            sumPwrDbm = convertw2dbm(sumReceivePwrW);
            return sumPwrDbm;
        }
    }
}
