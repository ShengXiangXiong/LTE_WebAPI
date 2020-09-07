using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections;
using System.Text;
using LTE.InternalInterference;
using System.Data;
using LTE.DB;
using System.Runtime.InteropServices;
using System.Diagnostics;
using LTE.InternalInterference.ProcessControl;

namespace LTE.WebAPI.Models
{
    // 调试：射线跟踪公共部分
    public class DebugCellRayTracing
    {
        /// <summary>
        /// 小区名称
        /// </summary>
        public string cellName { get; set; }  

        /// <summary>
        /// 小区覆盖半径
        /// </summary>
        public double distance { get; set; }   

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
        /// 是否计算立体覆盖
        /// </summary>
        public bool computeIndoor { get; set; }  

        /// <summary>
        /// 是否计算棱边绕射
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

        public Result validateCell(ref int eNodeB, ref int CI, ref string cellType)
        {
            if (this.cellName == string.Empty)
            {
                return new Result(false, "请输入小区名称");
            }
            DataTable dt = IbatisHelper.ExecuteQueryForDataTable("SingleGetCellType", this.cellName);
            if (dt.Rows.Count == 0)
            {
                return new Result(false, "您输入的小区名称有误，请重新输入！");
            }
            eNodeB = Convert.ToInt32(dt.Rows[0]["eNodeB"]);
            CI = Convert.ToInt32(dt.Rows[0]["CI"]);
            cellType = Convert.ToString(dt.Rows[0]["NetType"]);
            return new Result(true);
        }
        public Result parallelComputing(ref CellInfo cellInfo, double fromAngle, double toAngle, bool isRayLoc, bool isRayAdj)
        {
            int processNum = this.threadNum;
            Random r = new Random(DateTime.Now.Second);
            LTE.Geometric.Point p = cellInfo.SourcePoint;
            double deltaA = (toAngle - fromAngle + 360) % 360 / processNum;
            double from, to;
            List<int> bids = new List<int>();

            for (int i = 0; i < processNum; i++)
            {
                from = (fromAngle + i * deltaA + 360) % 360;
                to = (fromAngle + (i + 1) * deltaA + 360) % 360;
                if (to > toAngle)
                {
                    to = toAngle;
                }

                Calc calc = new Calc(ref cellInfo, this.distance, from, to, this.reflectionNum, this.diffractionNum, this.computeIndoor, this.diffPointsMargin, bids, isRayLoc, isRayAdj);
                calc.start();
            }
            return new Result(true);
        }
    }

    // 调试：小区覆盖计算
    public class DebugCellRayTracingModel : DebugCellRayTracing
    {
        public Result calc()
        {
            int eNodeB = 0, CI = 0;
            string cellType = "";

            Result rt = validateCell(ref eNodeB, ref CI, ref cellType);
            if (!rt.ok)
                return rt;

            CellInfo cellInfo = new CellInfo(this.cellName, eNodeB, CI, this.directCoeff, this.reflectCoeff, this.diffractCoeff, this.diffractCoeff2);

            double fromAngle = cellInfo.Azimuth - this.incrementAngle;
            double toAngle = cellInfo.Azimuth + this.incrementAngle;

            return parallelComputing(ref cellInfo, fromAngle, toAngle, false, false);
        }
    }

    // 调试：记录用于系数校正的射线
    // 先生成虚拟路测，只会记录射线终点位于虚拟路测中的轨迹，而不是所有轨迹
    // 如果虚拟路测为空，则生成的射线轨迹不会记在数据库中
    public class DebugRayRecordAdjModel : DebugCellRayTracing
    {
        public Result rayRecord()
        {
            int eNodeB = 0, CI = 0;
            string cellType = "";

            Result rt = validateCell(ref eNodeB, ref CI, ref cellType);
            if (!rt.ok)
                return rt;

            CellInfo cellInfo = new CellInfo(this.cellName, eNodeB, CI, this.directCoeff, this.reflectCoeff, this.diffractCoeff, this.diffractCoeff2);
            double fromAngle = cellInfo.Azimuth - this.incrementAngle;
            double toAngle = cellInfo.Azimuth + this.incrementAngle;

            return parallelComputing(ref cellInfo, fromAngle, toAngle, false, true);
        }
    }

    // 调试：记录用于干扰定位的射线
    public class DebugRayRecordLocModel : DebugCellRayTracing
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

            return parallelComputing(ref cellInfo, fromAngle, toAngle, true, false);
        }
    }
        
}