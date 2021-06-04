using LTE.DB;
using LTE.ExternalInterference;
using LTE.ExternalInterference.Struct;
using LTE.Geometric;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace LTE.WebAPI.Models
{
    public class RankModel
    {
        public string virname { get; set; }
        public double ratioAP { get; set; }
        public double ratioP { get; set; }
        public double ratioAPW { get; set; }
        public int k { get; set; }

        public Result LocateByPath()
        {
            PathAnalysis pa = new PathAnalysis(virname);
            ResultRecord ans = pa.StartAnalysis(ratioAP, ratioP, ratioAPW);

            Hashtable ht = new Hashtable();
            ht["Version"] = this.virname;
            ht["k"] = this.k;
            DataTable dt = IbatisHelper.ExecuteQueryForDataTable("queryLocResult", ht);
           
            dt = JWDCompute(dt);
            if (!ans.GetIsLocated())
            {
                return new Result(false, ans.GetMsg());
            }
            else
            {
                string msg = string.Format("定位结果坐标：{0}\n干扰源坐标：{1}\n定位精度：{2}米", ans.GetResLocation(), ans.GetReaLocation(), ans.GetPrecise());
                //return new Result(true, msg, ans);
                return new Result(true, msg, dt);
            }

            //return new Result(true, "ok", dt);
        }

        public DataTable JWDCompute(DataTable dt)
        {
            int range = 100;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var row = dt.Rows[i];
                double x = Double.Parse(row["x"].ToString());
                double y = Double.Parse(row["y"].ToString());

                Random r = new Random(range);
                int offx = r.Next(range) - range/2;
                int offy = r.Next(range) - range/2;
                x = x + offx;
                y = y + offy;
                dt.Rows[i]["x"] = x;
                dt.Rows[i]["y"] = y;

                Point pos = new Point();
                pos.X = x;
                pos.Y = y;
                pos.Z = 0;
                pos = LTE.Utils.PointConvertByProj.Instance.GetGeoPoint(pos);

                dt.Rows[i]["Longitude"] = pos.X;
                dt.Rows[i]["Latitude"] = pos.Y;
            }

            return dt;
        }


    }
}