using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LTE.DB;
using LTE.InternalInterference;

namespace LTE.WebAPI.Models
{
    // 单射线跟踪公共部分
    public class SingleRayTracingModel
    {
        public string cellName { get; set; } // 小区名称
        protected Result validateCell()
        {
            object o;
            try
            {
                o = IbatisHelper.ExecuteScalar("SingleGetCellType", this.cellName);
            }
            catch(Exception e)
            {
                return new Result(false, e.ToString());
            }

            if (o == null)
            {
                return new Result(false, "您输入的小区名称有误，请重新输入！");
            }
            return new Result(true);
        }

    }

    // 单射线跟踪：指定射线终点
    public class SingleRayTracingModel1 : SingleRayTracingModel
    {
        /// <summary>
        /// 终点经度
        /// </summary>
        public double longitude { get; set; }   
 
        /// <summary>
        /// 终点纬度
        /// </summary>
        public double latitude { get; set; }    

        public Result rayTracing()
        {
            Result rt = this.validateCell();
            if (!rt.ok)
                return rt;

            CellInfo cellInfo = new CellInfo(this.cellName, 0, 0);
            LTE.Geometric.Point p = new Geometric.Point(this.longitude, this.latitude, 0);
            p = LTE.Utils.PointConvertByProj.Instance.GetGeoPoint(p);

            RayTracing interAnalysis = new RayTracing(cellInfo, 3, 2, false);
            string msg = interAnalysis.SingleRayAnalysis(p.X, p.Y, p.Z);
            if (msg == "")
                return new Result(false, "射线到达建筑物顶面或经过若干次反射超出地图范围以致无法到达地面");
            return new Result(true, msg);
        }
    }

    // 单射线跟踪：指定射线方位角和下倾角
    public class SingleRayTracingModel2 : SingleRayTracingModel
    {
        /// <summary>
        /// 射线方位角
        /// </summary>
        public double direction { get; set; }   

        /// <summary>
        /// 射线下倾角
        /// </summary>
        public double inclination { get; set; } 

        public Result rayTracing()
        {
            Result rt = this.validateCell();
            if (!rt.ok)
                return rt;

            CellInfo cellInfo = new CellInfo(this.cellName, 0, 0);

            RayTracing interAnalysis = new RayTracing(cellInfo, 4, 2, false);
            string msg = interAnalysis.SingleRayAnalysis(this.direction, this.inclination);
            if (msg == "")
                return new Result(false, "射线到达建筑物顶面或经过若干次反射超出地图范围以致无法到达地面");
            return new Result(true, msg);
        }
    }
}