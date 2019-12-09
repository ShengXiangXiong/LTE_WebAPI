using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LTE.Geometric;
using System.Collections;

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

        public double convertw2dbm(double w)
        {
            return 10 * (Math.Log10(w) + 3);
        }

        // 计算场强
        // coef：第一维为场景，第二维为各校正系数，依次为直射、反射、绕射
        public double calc(ref double[,] coef, int scenNum, int frequncy)
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
                    }
                }

                double amendDirSum = 0;
                for (int j = 0; j < scenNum; j++)
                    amendDirSum += coef[j, 0] * (scenDistance[j] / distance);

                double receivePwr = Math.Pow(nata / (4 * Math.PI), 2) * (traj[key].emitPwrW / Math.Pow(distance, (2 + amendDirSum))) * Math.Pow(reflectedR, 2) * Math.Pow(diffrctedR, 2);
                sumReceivePwrW += receivePwr;
            }

            sumPwrDbm = convertw2dbm(sumReceivePwrW);
            return sumPwrDbm;
        }
    }
}
