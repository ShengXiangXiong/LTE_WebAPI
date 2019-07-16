using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LTE.InternalInterference;
using System.Data;
using System.Data.SqlClient;
using LTE.DB;

namespace LTE.WebAPI.Models
{
    // 记录用于系数校正的射线
    // 先生成虚拟路测，只会记录射线终点位于虚拟路测中的轨迹，而不是所有轨迹
    // 如果虚拟路测为空，则生成的射线轨迹不会记在数据库中
    public class RayRecordAdjModel : CellRayTracing
    {
        public Result rayRecord()
        {
            List<ProcessArgs> paList = new List<ProcessArgs>();
            int eNodeB = 0, CI = 0;
            string cellType = "";

            Result rt = validateCell(ref eNodeB, ref CI, ref cellType);
            if (!rt.ok)
                return rt;

            CellInfo cellInfo = new CellInfo(this.cellName, eNodeB, CI, this.directCoeff, this.reflectCoeff, this.diffractCoeff, this.diffractCoeff2);
            double fromAngle = cellInfo.Azimuth - this.incrementAngle;
            double toAngle = cellInfo.Azimuth + this.incrementAngle;

            return parallelComputing(ref cellInfo, fromAngle, toAngle, eNodeB, CI, ref paList, false, false, false, true);
        }
    }

    // 记录用于干扰定位的射线
    public class RayRecordLocModel : CellRayTracing
    {
        /// <summary>
        /// 是否手动指定发射源（可选）
        /// </summary>
        public bool manSource { get; set; } 

        /// <summary>
        /// 发射源 ID
        /// </summary>
        public int sourceID { get; set; } 
   
        /// <summary>
        /// 发射源 x 坐标
        /// </summary>
        public double sourceX { get; set; }   

        /// <summary>
        /// 发射源 y 坐标
        /// </summary>
        public double sourceY { get; set; }   

        /// <summary>
        /// 发射源 z 坐标
        /// </summary>
        public double sourceZ { get; set; }   

        /// <summary>
        /// 是否手动指定方位角（可选）
        /// </summary>
        public bool manDir { get; set; } 

        /// <summary>
        /// 方位角中心线末端 x 坐标
        /// </summary>
        public double dirX { get; set; }   

        /// <summary>
        /// 方位角中心线末端 y 坐标
        /// </summary>
        public double dirY { get; set; }   


        public Result rayRecord()
        {
            List<ProcessArgs> paList = new List<ProcessArgs>();
            double fromAngle = 0, toAngle = 0;
            int eNodeB = 0, CI = 0;
            string cellType = "";
            CellInfo cellInfo;

            if (this.manSource)  // 手动指定发射源
            {
                cellInfo = new CellInfo();
                cellInfo.SourcePoint = new Geometric.Point(this.sourceX, this.sourceY, this.sourceZ);
                cellInfo.SourceName = this.sourceID.ToString();
                cellInfo.CI = this.sourceID;
                cellInfo.EIRP = 53;
                cellInfo.Inclination = -7;
                cellInfo.diffracteCoefficient = this.diffractCoeff;
                cellInfo.reflectCoefficient = this.reflectCoeff;
                cellInfo.directCoefficient = this.directCoeff;
                cellInfo.diffracteCoefficient2 = this.diffractCoeff2;

                if (this.manDir) // 手动指定方位角
                {
                    Geometric.Point end = new Geometric.Point(this.dirX, this.dirY, 0);
                    cellInfo.Azimuth = LTE.Geometric.GeometricUtilities.getPolarCoord(end, cellInfo.SourcePoint).theta / Math.PI * 180;
                }
                else if (this.cellName != null && this.cellName != string.Empty)  // 方位角中心线末端为指定小区
                {
                    Result rt = validateCell(ref eNodeB, ref CI, ref cellType);
                    if (!rt.ok)
                        return rt;

                    CellInfo end = new CellInfo(this.cellName, eNodeB, CI, this.directCoeff, this.reflectCoeff, this.diffractCoeff, this.diffractCoeff2);
                    cellInfo.Azimuth = LTE.Geometric.GeometricUtilities.getPolarCoord(end.SourcePoint, cellInfo.SourcePoint).theta / Math.PI * 180;
                }
                else
                {
                    return new Result(false, "请填写小区名称或手动指定方位角");
                }

                fromAngle = cellInfo.Azimuth - this.incrementAngle;
                toAngle = cellInfo.Azimuth + this.incrementAngle;
            }
            else  // 发射源为指定的小区
            {
                Result rt = validateCell(ref eNodeB, ref CI, ref cellType);
                if (!rt.ok)
                    return rt;

                cellInfo = new CellInfo(this.cellName, eNodeB, CI, this.directCoeff, this.reflectCoeff, this.diffractCoeff, this.diffractCoeff2);
                fromAngle = cellInfo.Azimuth - this.incrementAngle;
                toAngle = cellInfo.Azimuth + this.incrementAngle;
            }

            return parallelComputing(ref cellInfo, fromAngle, toAngle, eNodeB, CI, ref paList, false, false, true, false);
        }

    }
}