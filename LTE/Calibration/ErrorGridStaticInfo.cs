using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTE.Calibration
{
    public class ErrorGridStaticInfo
    {
        public string key;//celId,gridX,gridY
        public double dtValDbm;//路测值
        public double calcValDbm;//计算值
        public int maxPowerTrajKey;//轨迹key
        public double distance;
        public double p0;
        public double d1;
        public double d2;
        public double amendDirSum;
        public double error;//收敛时的误差,计算值-路测值

        public string cellId;
        public double gridX;
        public double gridY;

        public ErrorGridStaticInfo() {

        }

        //public ErrorGridStaticInfo(String key,double error,double calcValDbm,double dtValDbm, int maxPowerTrajKey) {
        //    this.key = key;
        //    this.calcValDbm = calcValDbm;
        //    this.dtValDbm = dtValDbm;
        //    this.error = error;
        //    this.maxPowerTrajKey = maxPowerTrajKey;
        //}

        public void setInfo(String key, double error, double calcValDbm, double dtValDbm) {
            this.key = key;
            this.calcValDbm = calcValDbm;
            this.dtValDbm = dtValDbm;
            this.error = error;
        }

        //todo 转换
    }
}
