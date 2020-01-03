using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

using LTE.InternalInterference;
using LTE.Geometric;

namespace LTE.InternalInterference
{
    // 用于系数校正
    // 一条轨迹上的所有射线信息
    public class RayInfo
    {
        public double emitPwrW;                   // 发射功率
        public List<NodeInfo> rayList;

        public double recePwrW;                   // 接收信号强度（功率）
        public double recePwrDem;                 //接收信号强度（dbm）
        public double distance;                   //射线传播总距离(m)

        public RayInfo()
        {
            rayList = new List<NodeInfo>();
        }

        public RayInfo(List<NodeInfo> rayList, double emitPwrW) {
            this.rayList = rayList;
            this.emitPwrW = emitPwrW;
            this.distance = 0;
        }

        //计算一条轨迹（包含多个射线段）的接收功率,使用多场景校正系数
        public void calcRecePwrW(ref double[,] coef, int scenNum, int frequency,CellType cellType=CellType.GSM1800) {

            updateDefAngle();//更新轨迹各个射线段的偏转角
            double nata = 0;
            if (cellType == CellType.GSM1800)
            {
                //f(n) = 1805 + 0.2*(n－511) MHz
                nata = 300.0 / (1805 + 0.2 * (frequency - 511));// f(n) = 1805 + 0.2*(n－511) MHz  
            }
            else
            {
                //f(n) = 935 + 0.2n MHz
                nata = 300.0 / (935 + 0.2 * frequency);
            }

            double[] scenDistance = new double[scenNum];
            double reflectedR = 1;       // 反射系数
            double diffrctedR = 1;       // 绕射系数

            for (int j = 0; j < rayList.Count; ++j)  // 对于一条轨迹中的每条射线
            {
                double defAngle = rayList[j].DefAngle;//偏转角 弧度制 数值【0 ， 3.1415926】
                double defAngleDegree = defAngle * 180 / 3.1415926;

                distance += rayList[j].distance;//distance表示轨迹各射线段总距离
                // 统计每个场景的射线距离
                string[] scenArr = rayList[j].proportion.Split(';');
                for (int k = 0; k < scenNum; k++)
                    scenDistance[k] += Convert.ToDouble(scenArr[k]) * rayList[j].distance;
                
                rayList[j].attenuation = 1;
                if (rayList[j].rayType == RayType.VReflection || rayList[j].rayType == RayType.HReflection)
                {
                    double A = coef[rayList[j].endPointScen, 1];
                    double B = coef[rayList[j].endPointScen, 2];
                    double C = coef[rayList[j].endPointScen, 3];

                    //reflectedR *= 0.5*Math.Pow(10, -(A * defAngle * defAngle + B * defAngle + C)); // 指数二次拟合函数
                    double attenua = A * defAngle * defAngle + B * defAngle + C; // 二次拟合函数
                    reflectedR *= attenua;
                    rayList[j].attenuation = attenua;
                }
                else if (rayList[j].rayType == RayType.HDiffraction || rayList[j].rayType == RayType.VDiffraction)
                {
                    double D = coef[rayList[j].endPointScen, 4];
                    double E = coef[rayList[j].endPointScen, 5];
                    double F = coef[rayList[j].endPointScen, 6];

                    //diffrctedR *= 0.1*Math.Pow(10, -(D * defAngle * defAngle + E * defAngle + F)); // 指数二次拟合函数
                    double attenua = D * defAngle * defAngle + E * defAngle + F; // 指数二次拟合函数
                    diffrctedR *= attenua; // 指数二次拟合函数
                    rayList[j].attenuation = attenua;
                }
            }

            double amendDirSum = 0;
            for (int j = 0; j < scenNum; j++)
                amendDirSum += coef[j, 0] * (scenDistance[j] / distance);

            double p0 = emitPwrW;
            double d1 = Math.Pow(nata / (4 * Math.PI), 2);
            double d2 = 1 / Math.Pow(distance, (2 + amendDirSum));
            double d3 = Math.Pow(reflectedR, 2);
            double d4 = Math.Pow(diffrctedR, 2);
            recePwrW = p0 * d1 * d2 * d3 * d4;

            double recePwrDem = 10 * (Math.Log10(recePwrW) + 3);
        }

        //更新一条轨迹每个射线段的偏转角
        private void updateDefAngle()
        {
            // 遍历每个轨迹的射线段
            for (int j = 0; j < rayList.Count; ++j)  // 对于一条轨迹中的每条射线
            {
                NodeInfo nodeCur = rayList[j];//当前射线段
                if (j == 0)
                {
                    nodeCur.DefAngle = -1;//如果是轨迹的第一条射线段，那么肯定是直射线，转角赋默认值-1
                }
                else
                {
                    NodeInfo nodeBef = rayList[j - 1];//前一条射线段
                    NewVector3D vecBef = new NewVector3D(nodeBef.PointOfIncidence, nodeBef.CrossPoint);
                    NewVector3D vecCur = new NewVector3D(nodeCur.PointOfIncidence, nodeCur.CrossPoint);
                    double defAngle = vecBef.calcDefAngle(ref vecCur);
                    nodeCur.DefAngle = defAngle;
                }
            }
        }
    }
}
