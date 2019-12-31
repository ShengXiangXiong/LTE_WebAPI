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
        private double precise;
        private string msg;

        public ResultRecord(bool ans,string res,string real,double dis,string tips)
        {
            isLocated = ans;
            reslocation = res;
            realocation = real;
            precise = dis;
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
