using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTE.ExternalInterference.Struct
{
    public class ResultRecord
    {
        private bool isLocated;
        private string reslocation;
        private string realocation;
        public double lon;//经度
        public double lat;//纬度
        private double precise;
        private string msg;

        public ResultRecord(bool ans,double tlon,double tlat, string res, string real,double dis,string tips)
        {
            isLocated = ans;
            reslocation = res;
            realocation = real;
            lon = Math.Round(tlon,6);
            lat = Math.Round(tlat,6);
            precise = dis;
            msg = tips;
        }
        public ResultRecord(bool ans, string res, string real, double dis, string tips)
        {
            isLocated = ans;
            reslocation = res;
            realocation = real;
            precise = dis;
            msg = tips;
        }
        public ResultRecord(bool ans, double tlon, double tlat, string tips)
        {
            isLocated = ans;
            reslocation = "";
            realocation = "";
            lon = Math.Round(tlon, 4);
            lat = Math.Round(tlat, 4);
            precise = -1;
            msg = tips;
        }
        public ResultRecord(bool ans, string tips)
        {
            isLocated = ans;
            msg = tips;
        }

        public bool GetIsLocated()
        {
            return this.isLocated;
        }
        public string GetResLocation()
        {
            return reslocation;
        }
        public string GetReaLocation()
        {
            return realocation;
        }
        public double GetPrecise()
        {
            return precise;
        }
        public string GetMsg()
        {
            return msg;
        }
    }
}
