using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LTE.DB;
using LTE.Geometric;
using LTE.Utils;

namespace LTE.WebAPI.Models
{
    public class CalibrationRangeModel
    {
        public double MaxLatitude { get; set; }

        public double MaxLongitude { get; set; }

        public double MinLatitude { get; set; }

        public double MinLongitude { get; set; }

        /// <summary>
        /// 更新用于系数校正射线生成及校正的经纬度范围
        /// </summary>
        /// <returns></returns>
        public Result updateCalibrationRange()
        {
            try
            {
                //更新数据库中待生成轨迹所选区域范围,经纬度
                Hashtable paramHt = new Hashtable();
                double maxLon = Double.Parse(this.MaxLongitude.ToString());
                double maxLat = Double.Parse(this.MaxLatitude.ToString());
                double minLon = Double.Parse(this.MinLongitude.ToString());
                double minLat = Double.Parse(this.MinLatitude.ToString());
                paramHt["MaxLon"] = maxLon;
                paramHt["MaxLat"] = maxLat;
                paramHt["MinLon"] = minLon;
                paramHt["MinLat"] = minLat;

                Point pMin = new Point(minLon, minLat, 0);
                pMin = PointConvertByProj.Instance.GetProjectPoint(pMin);
                Point pMax = new Point(maxLon, maxLat, 0);
                pMax = PointConvertByProj.Instance.GetProjectPoint(pMax);
                paramHt["MinX"] = Math.Round(pMin.X,3);
                paramHt["MinY"] = Math.Round(pMin.Y,3);
                paramHt["MaxX"] = Math.Round(pMax.X,3);
                paramHt["MaxY"] = Math.Round(pMax.Y,3);

                IbatisHelper.ExecuteDelete("DeleteRayAdjRange", null);
                IbatisHelper.ExecuteInsert("insertRayAdjRange", paramHt);

                return new Result(true, "系数校正区域范围更新成功！");
            }
            catch (Exception e)
            {
                return new Result(false, "系数矫正区域范围更新失败!" + e);
            }
        }
    }
}