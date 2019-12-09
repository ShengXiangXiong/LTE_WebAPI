using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Web;
using LTE.InternalInterference;
using System.Data;
using System.Data.SqlClient;
using LTE.DB;
using System.Diagnostics;
using LTE.Model;

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

            return parallelComputing(ref cellInfo, fromAngle, toAngle, eNodeB, CI, ref paList, false, false, false, true, LoadInfo.UserId.Value, LoadInfo.taskName.Value);
        }
    }

    // 记录用于干扰定位的射线
    public class RayLocRecordModel
    {
        //
        // GET: /RayLocRecordModel/
        #region 变量定义
        /// <summary>
        /// 干扰源点
        /// </summary>
        public string virsource { get; set; }
        /// <summary>
        /// 反向发射点发射半径
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
        public double sideSplitUnit { get; set; }

        /// <summary>
        /// 计算立体覆盖
        /// </summary>
        public bool computeIndoor { get; set; }

        /// <summary>
        /// 计算棱边绕射
        /// </summary>
        public bool computeVSide { get; set; }

        /// <summary>
        /// 直射校正系数
        /// </summary>
        public double directCoefficient { get; set; }

        /// <summary>
        /// 反射校正系数
        /// </summary>
        public double reflectCoefficient { get; set; }

        /// <summary>
        /// 绕射校正系数
        /// </summary>
        public double diffractCoefficient { get; set; }

        /// <summary>
        /// 菲涅尔绕射校正系数
        /// </summary>
        public double FCoefficient { get; set; }

        
        #endregion
        /// <summary>
        /// 计算射线
        /// </summary>
        /// <returns></returns>
        public Result RecordRayLoc()
        {
            Hashtable ht = new Hashtable();
            ht["fromName"] = virsource;
            //读取selectPoint 表信息
            DataTable tb = IbatisHelper.ExecuteQueryForDataTable("GetSelectedPoint", ht);
            
            if (tb.Rows.Count < 2)
            {
                return new Result(false, "该干扰源未完成选点操作，请重新选取干扰源"); ;
            }
            //清除表中tbRayLoc当前cellname对应的selectpoint对应的CI的数据
            IbatisHelper.ExecuteDelete("deletetbRayLoc", ht);
            
            for (int i = 0; i < tb.Rows.Count; i++)
            {
                CellInfo cellInfo = new CellInfo();
                cellInfo.SourcePoint = new Geometric.Point();
                cellInfo.SourcePoint.X = Convert.ToDouble(tb.Rows[i]["x"]);
                cellInfo.SourcePoint.Y = Convert.ToDouble(tb.Rows[i]["y"]);
                cellInfo.SourcePoint.Z = 1;
                cellInfo.SourceName = Convert.ToString(tb.Rows[i]["CI"]);
                cellInfo.CI = Convert.ToInt32(tb.Rows[i]["CI"]);
                cellInfo.Inclination = 0;

                cellInfo.EIRP = 53;
                cellInfo.Inclination = 7;
                cellInfo.diffracteCoefficient = (float)this.diffractCoefficient;
                cellInfo.reflectCoefficient = (float)this.reflectCoefficient;
                cellInfo.directCoefficient = (float)this.directCoefficient;
                cellInfo.diffracteCoefficient2 = (float)this.FCoefficient;
                cellInfo.Azimuth = Convert.ToDouble(tb.Rows[i]["Azimuth"]);
                double fromAngle = cellInfo.Azimuth - this.incrementAngle;
                double toAngle = cellInfo.Azimuth + this.incrementAngle;
                Result res = parallelComputing(cellInfo, fromAngle, toAngle);
                Debug.WriteLine("fromAngle " + fromAngle + "   toAngle" + toAngle);
                if (res.ok == false)
                {
                    return res;
                }
            }
            return new Result(true, "完成" + tb.Rows.Count + "个点的反向射线跟踪计算");
        }

        private Result parallelComputing(CellInfo cellInfo, double fromAngle, double toAngle)
        {
            string bidstext = "-1";

            LTE.Geometric.Point p = cellInfo.SourcePoint;
            ProcessArgs pa = new ProcessArgs();
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.UseShellExecute = true;
            psi.ErrorDialog = true;

            psi.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            psi.FileName = "LTE.MultiProcessController.exe";
            psi.Arguments = string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15} {16} {17} {18} {19} {20} {21} {22} {23} {24} {25} {26} {27} {28} {29} {30} {31} {32}",
                cellInfo.SourceName, p.X, p.Y, 0, 0, p.Z, cellInfo.eNodeB, cellInfo.CI,
                cellInfo.Azimuth, cellInfo.Inclination, cellInfo.cellType, cellInfo.frequncy, cellInfo.EIRP,
                cellInfo.directCoefficient, cellInfo.reflectCoefficient, cellInfo.diffracteCoefficient, cellInfo.diffracteCoefficient,
                fromAngle, toAngle, this.distance, this.reflectionNum, this.diffractionNum, this.computeIndoor,
                this.threadNum, bidstext, this.sideSplitUnit, this.computeVSide, false, false, true, false, LoadInfo.UserId.Value, LoadInfo.taskName.Value);

            try
            {
                pa.pro = Process.Start(psi);
                pa.pro.WaitForExit();
            }
            catch (InvalidOperationException exception)
            {
                return new Result(false, "多进程计算启动失败，原因： " + exception.Message);
            }
            catch (Exception ee)
            {
                return new Result(false, "多进程计算启动失败，原因： " + ee.Message);
            }
            return new Result(true);
        }

    }
}