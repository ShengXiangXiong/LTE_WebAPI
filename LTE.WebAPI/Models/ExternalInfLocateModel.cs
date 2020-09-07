using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LTE.ExternalInterference;
using LTE.ExternalInterference.Struct;
namespace LTE.WebAPI.Models
{
    public class ExternalInfLocateModel
    {
        public string virname { get; set; }
        public double ratioAP { get; set; }
        public double ratioP { get; set; }
        public double ratioAPW { get; set; }

        public Result LocateByPath()
        {
            PathAnalysis pa = new PathAnalysis(virname);
            ResultRecord ans = pa.StartAnalysis(ratioAP, ratioP, ratioAPW);
            if (!ans.GetIsLocated())
            {
                return new Result(false, ans.GetMsg());
            }
            else
            {
                string msg = string.Format("定位结果坐标：{0}\n干扰源坐标：{1}\n定位精度：{2}米", ans.GetResLocation(), ans.GetReaLocation(), ans.GetPrecise());
                return new Result(true, msg, ans);
            }
        }
    }
}