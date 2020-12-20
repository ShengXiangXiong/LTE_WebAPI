using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Web;
using LTE.InternalInterference;
using System.Data;
using System.Data.SqlClient;
using LTE.DB;
using LTE.Model;
using System.Diagnostics;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;
using System.Management;
using LTE.Utils;
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
            
            return parallelComputing(ref cellInfo, fromAngle, toAngle, eNodeB, CI, ref paList, false, false, false, true, LoadInfo.UserId.Value, "射线记录Adj");
        }
    }
    // 记录用于干扰定位的射线
    public class RayLocRecordModel
    {

        private LoadInfo loadInfo = new LoadInfo();

        private int threadNum;
        
        //
        // GET: /RayLocRecordModel/
        #region 变量定义
        //public int userId { get; set; }
        /// <summary>
        /// 干扰源点
        /// </summary>
        public string virsource { get; set; }
        ///// <summary>
        ///// 反向发射点发射半径
        ///// </summary>
        //public double distance { get; set; }

        /// <summary>
        /// 以方位角为中心，两边扩展的角度
        /// </summary>
        public double incrementAngle { get; set; }

        ///// <summary>
        ///// 线程个数
        ///// </summary>
        //public int threadNum { get; set; }

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
        private bool computeIndoor = false;

        /// <summary>
        /// 计算棱边绕射
        /// </summary>
        private bool computeVSide = true;

        ///// <summary>
        ///// 直射校正系数
        ///// </summary>
        //public double directCoefficient { get; set; }

        ///// <summary>
        ///// 反射校正系数
        ///// </summary>
        //public double reflectCoefficient { get; set; }

        ///// <summary>
        ///// 绕射校正系数
        ///// </summary>
        //public double diffractCoefficient { get; set; }

        ///// <summary>
        ///// 菲涅尔绕射校正系数
        ///// </summary>
        //public double FCoefficient { get; set; }


        #endregion
        /// <summary>
        /// 计算射线
        /// </summary>
        /// <returns></returns>
        public Result RecordRayLoc(bool load=false)
        {
            threadNum = 2;
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
            DateTime t1, t2;
            t1 = DateTime.Now;
            loadInfo.loadCountAdd(tb.Rows.Count);
            for (int i = 0; i < tb.Rows.Count; i++)
            {
                CellInfo cellInfo = new CellInfo();
                cellInfo.SourcePoint = new Geometric.Point();
                cellInfo.SourcePoint.X = Convert.ToDouble(tb.Rows[i]["x"]);
                cellInfo.SourcePoint.Y = Convert.ToDouble(tb.Rows[i]["y"]);
                //海拔抹平为13
                cellInfo.SourcePoint.Z = 13;
                cellInfo.SourceName = Convert.ToString(tb.Rows[i]["CI"]);
                cellInfo.CI = Convert.ToInt32(tb.Rows[i]["CI"]);
                cellInfo.Inclination = 0;

                cellInfo.EIRP = 53;
                cellInfo.Inclination = 7;
                cellInfo.diffracteCoefficient = (float)1;
                cellInfo.reflectCoefficient = (float)1;
                cellInfo.directCoefficient = (float)0.3;
                cellInfo.diffracteCoefficient2 = (float)1;
                cellInfo.Azimuth = Convert.ToDouble(tb.Rows[i]["Azimuth"]);
                double fromAngle = cellInfo.Azimuth - this.incrementAngle;
                double toAngle = cellInfo.Azimuth + this.incrementAngle;
                double dis = Convert.ToDouble(tb.Rows[i]["Distance"]);
                Result res = new Result();
                Debug.WriteLine(i + "     " + tb.Rows[i]["CI"] + "      fromAngle " + fromAngle + "   toAngle" + toAngle);
                //if (way == 0)
                //{
                //    res = parallelComputing(cellInfo, fromAngle, toAngle);
                //}
                //else
                //{
                //    res = parallelComputing(cellInfo, fromAngle, toAngle,dis);
                //}
                if (load)
                {
                    res = parallelComputing(cellInfo, fromAngle, toAngle, dis,LoadInfo.UserId.Value,LoadInfo.taskName.Value);
                }
                else
                {
                    res = parallelComputing(cellInfo, fromAngle, toAngle, dis,-1,"default");
                }
                if (res.ok == false)
                {
                    IbatisHelper.ExecuteDelete("deletetbRayLoc", ht);
                    return res;
                }
                loadInfo.loadHashAdd(1);
            }
            t2 = DateTime.Now;
            Debug.WriteLine("计算时长：" + (t2 - t1));

            return new Result(true, "完成" + tb.Rows.Count + "个点的反向射线跟踪计算");
        }

        //private Result parallelComputing(CellInfo cellInfo, double fromAngle, double toAngle)
        //{
        //    string bidstext = "-1";

        //    LTE.Geometric.Point p = cellInfo.SourcePoint;
        //    ProcessArgs pa = new ProcessArgs();
        //    ProcessStartInfo psi = new ProcessStartInfo();
        //    psi.UseShellExecute = true;
        //    psi.ErrorDialog = true;

        //    psi.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
        //    psi.FileName = "LTE.MultiProcessController.exe";
        //    psi.Arguments = string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15} {16} {17} {18} {19} {20} {21} {22} {23} {24} {25} {26} {27} {28} {29} {30} {31} {32}",
        //        cellInfo.SourceName, p.X, p.Y, 0, 0, p.Z, cellInfo.eNodeB, cellInfo.CI,
        //        cellInfo.Azimuth, cellInfo.Inclination, cellInfo.cellType, cellInfo.frequncy, cellInfo.EIRP,
        //        cellInfo.directCoefficient, cellInfo.reflectCoefficient, cellInfo.diffracteCoefficient, cellInfo.diffracteCoefficient,
        //        fromAngle, toAngle, this.distance, this.reflectionNum, this.diffractionNum, this.computeIndoor,
        //        this.threadNum, bidstext, this.sideSplitUnit, this.computeVSide, false, false, true, false, LoadInfo.UserId.Value, LoadInfo.taskName.Value);

        //    try
        //    {
        //        pa.pro = Process.Start(psi);
        //        pa.pro.WaitForExit();
        //    }
        //    catch (InvalidOperationException exception)
        //    {
        //        return new Result(false, "多进程计算启动失败，原因： " + exception.Message);
        //    }
        //    catch (Exception ee)
        //    {
        //        return new Result(false, "多进程计算启动失败，原因： " + ee.Message);
        //    }
        //    return new Result(true);
        //}


        private Result parallelComputing(CellInfo cellInfo, double fromAngle, double toAngle,double dis,int userId,string taskName)
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
                fromAngle, toAngle, dis, this.reflectionNum, this.diffractionNum, this.computeIndoor,
                this.threadNum, bidstext, this.sideSplitUnit, this.computeVSide, false, false, true, false, userId, taskName);

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
    #region 注释版本，有点问题
    /*// 记录用于干扰定位的射线
    public class RayLocRecordModel
    {
        //
        // GET: /RayLocRecordModel/
        #region 变量定义
        public int userId { get; set; }
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
        private LoadInfo loadInfo = new LoadInfo();
        private HttpClient httpClient = new HttpClient();
        private string taskName = "射线记录计算";
        #endregion

        /// <summary>
        /// post执行进度
        /// </summary>
        /// <param name="loadInfo"></param>
        /// <param name="action"></param>
        public async void doPostLoading(LoadInfo loadInfo, string action)
        {
            string url = "http://localhost:3298/api/Loading/" + action;

            HttpContent httpContent = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(loadInfo));
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            httpContent.Headers.ContentType.CharSet = "utf-8";
            try
            {
                //httpClient = new HttpClient();
                //AuthenticationHeaderValue authValue = new AuthenticationHeaderValue("Basic", token);
                //httpClient.DefaultRequestHeaders.Authorization = authValue;
                HttpResponseMessage response = await httpClient.PostAsync(url, httpContent);
                //Console.WriteLine("response:"+response.IsSuccessStatusCode);
            }
            catch (Exception)
            {
                throw;
            }
        }
        /// <summary>
        /// 计算射线
        /// </summary>
        /// <returns></returns>
        public Result RecordRayLoc()
        {
            DateTime t1, t2;
            t1 = DateTime.Now;

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
            //loadInfo.UserId = userId;
            //loadInfo.taskName = taskName;
            //loadInfo.cnt = 0;
            //loadInfo.count = tb.Rows.Count;
            //doPostLoading(loadInfo, "addCountByMulti");
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
                //Debug.WriteLine("fromAngle " + fromAngle + "   toAngle" + toAngle);
                if (res.ok == false)
                {
                    Debug.WriteLine("多进程执行失败");
                    return res;
                }
                //loadInfo.cnt = i+1;
                //doPostLoading(loadInfo, "updateLoadingInfo");
            }
            t2 = DateTime.Now;
            Debug.WriteLine("用时："+(t2-t1));
            return new Result(true, "完成");
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
                this.threadNum, bidstext, this.sideSplitUnit, this.computeVSide, false, false, true, false, userId,"射线记录Loc");

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

    }*/
    #endregion
}