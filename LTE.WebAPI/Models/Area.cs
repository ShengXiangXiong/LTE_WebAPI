using LTE.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace LTE.WebAPI.Models
{
    public class Area
    {
        /// <summary>
        /// 最小经度
        /// </summary>
        public double minLongitude { get; set; }

        /// <summary>
        /// 最小纬度
        /// </summary>
        public double minLatitude { get; set; }

        /// <summary>
        /// 最大经度
        /// </summary>
        public double maxLongitude { get; set; }

        /// <summary>
        /// 最大纬度
        /// </summary>
        public double maxLatitude { get; set; }

        /// <summary>
        /// 执行当前覆盖分析的用户ID
        /// </summary>
        public int userId { get; set; }

        /// <summary>
        /// 以方位角为中心，两边扩展的角度
        /// </summary>
        public double incrementAngle { get; set; }

        /// <summary>
        /// 线程个数
        /// </summary>
        public int threadNum { get; set; }

        /// <summary>
        /// 反射次数
        /// </summary>
        public int reflectionNum { get; set; }

        /// <summary>
        /// 绕射次数
        /// </summary>
        public int diffractionNum { get; set; }

        /// <summary>
        /// 建筑物棱边绕射点间隔
        /// </summary>
        public double diffPointsMargin { get; set; }

        /// <summary>
        /// 计算立体覆盖
        /// </summary>
        public bool computeIndoor { get; set; }

        /// <summary>
        /// 计算棱边绕射
        /// </summary>
        public bool computeDiffrac { get; set; }

        /// <summary>
        /// 直射校正系数
        /// </summary>
        public float directCoeff { get; set; }

        /// <summary>
        /// 反射校正系数
        /// </summary>
        public float reflectCoeff { get; set; }

        /// <summary>
        /// 绕射校正系数
        /// </summary>
        public float diffractCoeff { get; set; }

        /// <summary>
        /// 菲涅尔绕射校正系数
        /// </summary>
        public float diffractCoeff2 { get; set; }

        public bool matrixCover(matrix m1,matrix m2)
        {
            if (m1.MaxGx < m2.MinGx || m1.MinGx > m2.MaxGx || m1.MaxGy < m2.MinGy || m1.MinGy> m2.MaxGy)
            {
                return false;
            }
            return true;
        }
        public class matrix
        {
            double minGx;
            double minGy;
            double maxGx;
            double maxGy;
            public double MinGx { get => minGx; set => minGx = value; }
            public double MinGy { get => minGy; set => minGy = value; }
            public double MaxGx { get => maxGx; set => maxGx = value; }
            public double MaxGy { get => maxGy; set => maxGy = value; }
        }
        public Result computeAreaAnlysis()
        {

            LTE.Geometric.Point pMin = new Geometric.Point();
            pMin.X = this.minLongitude;
            pMin.Y = this.minLatitude;
            pMin.Z = 0;
            LTE.Utils.PointConvertByProj.Instance.GetProjectPoint(pMin);

            LTE.Geometric.Point pMax = new Geometric.Point();
            pMax.X = this.maxLongitude;
            pMax.Y = this.maxLatitude;
            pMax.Z = 0;
            LTE.Utils.PointConvertByProj.Instance.GetProjectPoint(pMax);

            Hashtable ht = new Hashtable();
            int plus = 3000;
            ht["minX"] = pMin.X-plus;
            ht["minY"] = pMin.Y-plus;
            ht["maxX"] = pMax.X+plus;
            ht["maxY"] = pMax.Y+plus;

            List<CellRayTracingModel> cells = new List<CellRayTracingModel>();
            DataTable dt = DB.IbatisHelper.ExecuteQueryForDataTable("getCellByArea", ht);
            matrix m1 = new matrix { MinGx = pMin.X, MinGy = pMin.Y, MaxGx = pMax.X, MaxGy = pMax.Y };
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                
                matrix m2 = new matrix();
                var row = dt.Rows[i];
                double dis = (double)row["CoverageRadius"];
                m2.MinGx = (double)row["x"] - dis;
                m2.MaxGx = (double)row["x"] + dis;
                m2.MinGy = (double)row["y"] - dis;
                m2.MaxGy = (double)row["y"] + dis;

                if (matrixCover(m1, m2))
                {
                    CellRayTracingModel c = new CellRayTracingModel();
                    c.cellName = (string)row["CellName"];
                    c.computeDiffrac = this.computeDiffrac;
                    c.computeIndoor = this.computeIndoor;
                    c.diffPointsMargin = this.diffPointsMargin;
                    c.diffractCoeff = this.diffractCoeff;
                    c.diffractCoeff2 = this.diffractCoeff2;
                    c.diffractionNum = this.diffractionNum;
                    c.directCoeff = this.directCoeff;
                    c.distance = dis;
                    c.incrementAngle = this.incrementAngle;
                    c.reflectCoeff = this.reflectCoeff;
                    c.reflectionNum = this.reflectionNum;
                    c.threadNum = this.threadNum;
                    c.userId = this.userId;
                    cells.Add(c);
                }
            }

            LoadInfo loadInfo = new LoadInfo();
            loadInfo.cnt = 0;
            loadInfo.count = cells.Count;
            loadInfo.taskName = "区域覆盖计算";
            loadInfo.UserId = this.userId;
            loadInfo.breakdown = false;

            Loading loading = Loading.getInstance();

            foreach (var item in cells)
            {
                try
                {
                    if (item.calc().ok)
                    {
                        loadInfo.cnt++;
                        loading.updateLoading(loadInfo);
                    }
                }
                catch (Exception)
                {
                    loadInfo.breakdown = true;
                    return new Result(false, "区域覆盖计算失败");
                }
            }
            if (loadInfo.cnt < loadInfo.count)
            {
                loadInfo.breakdown = true;
                return new Result(false, "区域覆盖计算失败");
            }

            return new Result(true, "区域覆盖计算完成");
        }
    }
}