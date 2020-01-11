using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Collections;
using LTE.Geometric;
using LTE.DB;
using System.Diagnostics;
using System.Data.SqlClient;
using LTE.InternalInterference;
using LTE.Utils;
namespace LTE.WebAPI.Models
{
    public class PointsSelectModel
    {
        public string virname { get; set; }
        public int pointNum { get; set; }  //选点个数
        public double AngleCons { get; set; }  //路径阈值
        public double DisCons { get; set; }  //距离阈值
        public double RSRPCons { get; set; }  //损耗阈值
        private DataTable reset = new DataTable();//记录选取的point
        private DataTable tb = new DataTable();
        private double maxLon = 0, maxLat = 0, minLon = 0, minLat = 0;

        /// <summary>
        /// 构造函数
        /// </summary>
        public void Init()
        {
            //InitializeComponent();
            tb.Columns.Add("fromName");
            tb.Columns.Add("CI");
            tb.Columns.Add("x");
            tb.Columns.Add("y");
            tb.Columns.Add("ReceivePW");
            tb.Columns.Add("Azimuth");
            tb.Columns.Add("Distance");
            reset.Columns.Add("CI");
            reset.Columns.Add("Lon");
            reset.Columns.Add("Lat");
            reset.Columns.Add("x");
            reset.Columns.Add("y");
            reset.Columns.Add("ReceivePW");
        }

        private bool TestDis(DataTable dtinfo, ref double minD, ref double maxD, ref double avgD, ref double areax, ref double areay)
        {
            if (dtinfo == null || dtinfo.Rows.Count < 1)
            {
                Debug.WriteLine("传入参数不正确");
                return false;
            }
            Hashtable ht = new Hashtable();
            ht["BtsName"] = this.virname;
            DataTable tbcell = IbatisHelper.ExecuteQueryForDataTable("GettbSource", ht);
            if (tbcell == null|| tbcell.Rows.Count<1|| tbcell.Rows[0]["x"] == DBNull.Value || tbcell.Rows[0]["y"] == DBNull.Value)
            {
                Debug.WriteLine("未找到对应的小区地理信息");
                return false;
            }
            double endx = Convert.ToDouble(tbcell.Rows[0]["x"]);
            double endy = Convert.ToDouble(tbcell.Rows[0]["y"]);
            double sum = 0;
            double minx = double.MaxValue, miny = double.MaxValue, maxx = double.MinValue, maxy = double.MinValue;
            for (int i = 0; i < dtinfo.Rows.Count; i++)
            {
                double x = Convert.ToDouble(dtinfo.Rows[i]["x"]);
                double y = Convert.ToDouble(dtinfo.Rows[i]["y"]);
                if (x < minx) minx = x;
                if (x > maxx) maxx = x;
                if (y < miny) miny = y;
                if (y > maxy) maxy = y;
                double dis = distanceXY(x, y, endx, endy);
                if (dis < minD) minD = dis;
                if (dis > maxD) maxD = dis;
                sum += dis;
            }
            areax = (maxx - minx);
            areay = (maxy - miny);
            avgD = sum / dtinfo.Rows.Count;
            return true;
        }

        /// <summary>
        /// 选点程序
        /// </summary>
        /// <returns></returns>
        public Result GetPoints()
        {
            Init();
            AddToVirsource();
            Hashtable ht = new Hashtable();
            ht["BtsName"] = this.virname;
            ht["RSRP"] = this.RSRPCons;
            DataTable dtinfo = IbatisHelper.ExecuteQueryForDataTable("GetDTSet", ht);//获取大于RSRP的BtsName对应的路测信息
            
            DataTable firstRet = ComputePointByD(dtinfo, this.pointNum, this.DisCons);
            if (firstRet != null && firstRet.Rows.Count > this.pointNum)//先进行距离筛选，
            {
                Debug.WriteLine(dtinfo.Rows.Count+"进入角度约束阶段》》》" + firstRet.Rows.Count);
                DataTable secRet = ComputePointByA(firstRet, this.pointNum, this.AngleCons);

                if (secRet != null && secRet.Rows.Count == this.pointNum)
                {
                    Debug.WriteLine("进入信息填写阶段》》》");
                    //暂时试试用干扰源计算方位角的结果，如果用干扰源计算方位角不行，则说明不行
                    if (CompleteAzimuth(secRet) && tb.Rows.Count == this.pointNum)
                    {

                        double minD = double.MaxValue, maxD = double.MinValue, avgD = 0, areax = 0, areay = 0;
                        if (TestDis(tb, ref minD, ref maxD, ref avgD, ref areax, ref areay))
                        {
                            Debug.WriteLine("干扰源的距离平均：" + avgD + "  与干扰源的最远距离" + maxD + "  与干扰源的最近距离" + minD + "  包围盒长" + areax + "  包围盒宽" + areay);
                        }
                        Hashtable ht1 = new Hashtable();
                        ht1["fromName"] = this.virname;
                        IbatisHelper.ExecuteDelete("deletetbRayLoc", ht1);
                        WriteDataToBase(100);
                        return new Result { ok = true, msg = "成功写入数据库", code = "1" };;
                    }
                    else
                    {
                        
                        Debug.WriteLine("写入失败》》》");
                        return new Result(false, "写入失败");
                    }
                }
                else
                {
                    Debug.WriteLine("无满足角度约束的足够数量的点》》》");
                    return new Result(false, "无满足角度约束的足够数量的点》》》");
                }
            }
            else
            {
                Debug.WriteLine("无满足距离约束的足够数量的点》》》");
                return new Result(false, "无满足距离约束的足够数量的点》》》");
            }

        }

        public Result GetPointsByDis()
        {
            Init();
            AddToVirsource();
            Hashtable ht = new Hashtable();
            ht["BtsName"] = this.virname;
            ht["RSRP"] = this.RSRPCons;
            DataTable dtinfo = IbatisHelper.ExecuteQueryForDataTable("GetDTSet", ht);//获取大于RSRP的BtsName对应的路测信息
            if(dtinfo.Rows.Count< 2 * this.pointNum)
            {
                return new Result(false, "路测数据不够");
            }
            Debug.WriteLine("进入距离约束阶段》》》");
            DataTable firstRet = ComputePointByD(dtinfo, this.pointNum, this.DisCons);
            int itera = 50;
            while(true)
            {
                if ((firstRet != null && firstRet.Rows.Count >= this.pointNum && firstRet.Rows.Count <= 2 * this.pointNum)||itera-- == 0||this.DisCons>300)
                {
                    Debug.WriteLine("DisCons:"+ this.DisCons);
                    break;
                }
                else if(firstRet==null || firstRet.Rows.Count < this.pointNum)
                {
                    firstRet.Clear();
                    this.DisCons -= 10;
                    firstRet = ComputePointByD(dtinfo, this.pointNum, this.DisCons);
                }
                else
                {
                    firstRet.Clear();
                    this.DisCons += 10;
                    firstRet = ComputePointByD(dtinfo, this.pointNum, this.DisCons);
                }

            }
            if (firstRet != null && firstRet.Rows.Count >= this.pointNum)
            {  
                    if (CompleteAzimuth(firstRet))
                    {
                        double minD = double.MaxValue, maxD = double.MinValue, avgD = 0, areax = 0, areay = 0;
                        if (TestDis(tb, ref minD, ref maxD, ref avgD, ref areax, ref areay))
                        {
                            Debug.WriteLine("干扰源的距离平均：" + avgD + "  与干扰源的最远距离" + maxD + "  与干扰源的最近距离" + minD + "  包围盒长" + areax + "  包围盒宽" + areay);
                        }
                        Hashtable ht1 = new Hashtable();
                        ht1["fromName"] = this.virname;
                        IbatisHelper.ExecuteDelete("deletetbRayLoc", ht1);
                        WriteDataToBase(100);
                        return new Result { ok = true, msg = "成功写入数据库", code = "1" }; ;
                    }
                    else
                    {

                        Debug.WriteLine("写入失败》》》");
                        return new Result(false, "写入失败");
                    }
            }
            else
            {
                firstRet.Clear();
                this.DisCons -= 10;
                firstRet = ComputePointByD(dtinfo, this.pointNum, this.DisCons);
                if (firstRet == null || firstRet.Rows.Count < this.pointNum)
                {
                    Debug.WriteLine("无满足距离约束的足够数量的点》》》"+ firstRet.Rows.Count);
                    return new Result(false, "无满足距离约束的足够数量的点》》》");
                }
                else
                {
                    if (CompleteAzimuth(firstRet) && tb.Rows.Count == this.pointNum)
                    {
                        double minD = double.MaxValue, maxD = double.MinValue, avgD = 0, areax = 0, areay = 0;
                        if (TestDis(tb, ref minD, ref maxD, ref avgD, ref areax, ref areay))
                        {
                            Debug.WriteLine("干扰源的距离平均：" + avgD + "  与干扰源的最远距离" + maxD + "  与干扰源的最近距离" + minD + "  包围盒长" + areax + "  包围盒宽" + areay);
                        }
                        Hashtable ht1 = new Hashtable();
                        ht1["fromName"] = this.virname;
                        IbatisHelper.ExecuteDelete("deletetbRayLoc", ht1);
                        WriteDataToBase(100);
                        return new Result { ok = true, msg = "成功写入数据库", code = "1" }; ;
                    }
                    else
                    {
                        Debug.WriteLine("写入失败》》》");
                        return new Result(false, "写入失败");
                    }
                }
                    
            }

        }

        public Result GetPointsAuto()
        {
            Init();
            AddToVirsource();
            Hashtable ht = new Hashtable();
            ht["BtsName"] = this.virname;
            ht["RSRP"] = this.RSRPCons;
            DataTable dtinfo = IbatisHelper.ExecuteQueryForDataTable("GetDTSet", ht);//获取大于RSRP的BtsName对应的路测信息
            if (dtinfo.Rows.Count < 2 * this.pointNum)
            {
                return new Result(false, "路测数据不够");
            }
            Debug.WriteLine("进入距离约束阶段》》》");
            double curdis = this.DisCons;
            double currsrp = this.RSRPCons;
            DataTable firstRet = ComputePointByD(dtinfo, this.pointNum, curdis);
            int itera = 50;
            while (true)
            {
                if ((firstRet != null && firstRet.Rows.Count >= this.pointNum && firstRet.Rows.Count < 2 * this.pointNum) || itera-- == 0)
                {
                    Debug.WriteLine("DisCons:" + curdis);
                    break;
                }
                else if (firstRet == null || firstRet.Rows.Count < this.pointNum)
                {
                    if (curdis > this.DisCons)
                    {
                        curdis -= 5;
                        firstRet.Clear();
                        firstRet = ComputePointByD(dtinfo, this.pointNum, curdis);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    if (curdis > 300)//若是距离很大时，依然有很多备选路测点，则提高RSRP阈值
                    {
                        dtinfo.Clear();
                        currsrp += 1;
                        curdis = 300;
                        ht["RSRP"] = currsrp;
                        dtinfo = IbatisHelper.ExecuteQueryForDataTable("GetDTSet", ht);
                        firstRet.Clear();
                        firstRet = ComputePointByD(dtinfo, this.pointNum, curdis);
                    }
                    else
                    {
                        firstRet.Clear();
                        curdis += 10;
                        firstRet = ComputePointByD(dtinfo, this.pointNum, curdis);
                    }
                }
            }
            if (firstRet != null && firstRet.Rows.Count >= this.pointNum)
            {
                if (CompleteAzimuth(firstRet))
                {
                    double minD = double.MaxValue, maxD = double.MinValue, avgD = 0, areax = 0, areay = 0;
                    if (TestDis(tb, ref minD, ref maxD, ref avgD, ref areax, ref areay))
                    {
                        Debug.WriteLine("干扰源的距离平均：" + avgD + "  与干扰源的最远距离" + maxD + "  与干扰源的最近距离" + minD + "  包围盒长" + areax + "  包围盒宽" + areay);
                    }
                    Hashtable ht1 = new Hashtable();
                    ht1["fromName"] = this.virname;
                    IbatisHelper.ExecuteDelete("deletetbRayLoc", ht1);
                    WriteDataToBase(100);
                    return new Result { ok = true, msg = "成功写入数据库", code = "1" }; ;
                }
                else
                {
                    Debug.WriteLine("选点失败");
                    return new Result(false, "选点失败");
                }
            }
            else
            {
                Debug.WriteLine("无满足距离约束的足够数量的点》》》" + firstRet==null?0:firstRet.Rows.Count);
                return new Result(false, "无满足距离约束的足够数量的点");
            }
        }


        public Result GetPointsAutoReal()
        {
            Init();
            AddToVirsource();
            Hashtable ht = new Hashtable();
            ht["InfName"] = this.virname;
            ht["RSRP"] = this.RSRPCons;
            DataTable dtinfo = IbatisHelper.ExecuteQueryForDataTable("GettbUINTF", ht);//获取大于RSRP的BtsName对应的路测信息
            if (dtinfo.Rows.Count < 2 * this.pointNum)
            {
                return new Result(false, "路测数据不够");
            }
            Debug.WriteLine("进入距离约束阶段》》》");
            double curdis = this.DisCons;
            double currsrp = this.RSRPCons;
            DataTable firstRet = ComputePointByD(dtinfo, this.pointNum, curdis);
            int itera = 1000;
            while (true)
            {
                if ((firstRet != null && firstRet.Rows.Count >= this.pointNum && firstRet.Rows.Count < 2 * this.pointNum) || itera-- == 0)
                {
                    Debug.WriteLine("DisCons:" + curdis);
                    break;
                }
                else if (firstRet == null || firstRet.Rows.Count < this.pointNum)
                {
                    if (curdis > this.DisCons)
                    {
                        curdis -= 5;
                        firstRet.Clear();
                        firstRet = ComputePointByD(dtinfo, this.pointNum, curdis);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    if (curdis > 300)//若是距离很大时，依然有很多备选路测点，则提高RSRP阈值
                    {
                        dtinfo.Clear();
                        currsrp += 1;
                        curdis = 300;
                        ht["RSRP"] = currsrp;
                        dtinfo = IbatisHelper.ExecuteQueryForDataTable("GettbUINTF", ht);
                        firstRet.Clear();
                        firstRet = ComputePointByD(dtinfo, this.pointNum, curdis);
                    }
                    else
                    {
                        firstRet.Clear();
                        curdis += 10;
                        firstRet = ComputePointByD(dtinfo, this.pointNum, curdis);
                    }
                }
            }
            if (firstRet != null && firstRet.Rows.Count >= this.pointNum)
            {
                if (CompleteAzimuth(firstRet))
                {
                    double minD = double.MaxValue, maxD = double.MinValue, avgD = 0, areax = 0, areay = 0;
                    if (TestDis(tb, ref minD, ref maxD, ref avgD, ref areax, ref areay))
                    {
                        Debug.WriteLine("干扰源的距离平均：" + avgD + "  与干扰源的最远距离" + maxD + "  与干扰源的最近距离" + minD + "  包围盒长" + areax + "  包围盒宽" + areay);
                    }
                    Hashtable ht1 = new Hashtable();
                    ht1["fromName"] = this.virname;
                    IbatisHelper.ExecuteDelete("deletetbRayLoc", ht1);
                    WriteDataToBase(100);
                    return new Result { ok = true, msg = "成功写入数据库", code = "1" }; ;
                }
                else
                {
                    Debug.WriteLine("选点失败");
                    return new Result(false, "选点失败");
                }
            }
            else
            {
                Debug.WriteLine("无满足距离约束的足够数量的点》》》" + firstRet == null ? 0 : firstRet.Rows.Count);
                return new Result(false, "无满足距离约束的足够数量的点");
            }
        }
        /// <summary>
        /// 写入数据库
        /// </summary>
        /// <param name="batchSize"></param>
        public void WriteDataToBase(int batchSize)
        {
            Hashtable ht = new Hashtable();
            ht["cellname"] = this.virname;
            IbatisHelper.ExecuteDelete("deletbSelectedPoint", ht);
            using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
            {
                bcp.BatchSize = batchSize;
                bcp.BulkCopyTimeout = 1000;
                bcp.DestinationTableName = "tbSelectedPoints";
                bcp.WriteToServer(tb);
                bcp.Close();
            }
            Debug.WriteLine("入库完成");
        }
        //MessageBox.Show(this, "入库完成", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);


        /// <summary>
        /// 用目标干扰源计算方位角
        /// </summary>
        /// <param name="dtinfo"></param>
        /// <returns></returns>
        private Boolean CompleteAzimuthVir(DataTable dtinfo)
        {
            Hashtable ht = new Hashtable();
            ht["BtsName"] = this.virname;
            DataTable tbcell = IbatisHelper.ExecuteQueryForDataTable("GettbSource", ht);
            if (tbcell.Rows[0]["x"] == DBNull.Value || tbcell.Rows[0]["y"] == DBNull.Value)
            {
                Debug.WriteLine("未找到对应的小区地理信息");
                return false;
            }
            double endx = Convert.ToDouble(tbcell.Rows[0]["x"]);
            double endy = Convert.ToDouble(tbcell.Rows[0]["y"]);
            Point end = new Point(endx, endy, 0);
            tb.Clear();
            for (int i = 0; i < dtinfo.Rows.Count; i++)
            {
                Debug.WriteLine(i);
                double x = Convert.ToInt32(dtinfo.Rows[i]["x"]);
                double y = Convert.ToInt32(dtinfo.Rows[i]["y"]);
                Point start = new Point(x, y, 0);
                double azimuth = GeometricUtilities.getPolarCoord(start, end).theta / Math.PI * 180;
                azimuth = GeometricUtilities.ConvertGeometricArithmeticAngle(azimuth + 1);
                DataRow thisrow = tb.NewRow();
                thisrow["fromName"] = this.virname;
                thisrow["x"] = x;
                thisrow["y"] = y;
                thisrow["ReceivePW"] = dtinfo.Rows[i]["ReceivePW"];
                thisrow["CI"] = dtinfo.Rows[i]["CI"];
                thisrow["Azimuth"] = azimuth;
                thisrow["Distance"] = distanceXY(start.X, start.Y, end.X, end.Y) + 300;
                tb.Rows.Add(thisrow);
            }
            return true;
        }


        private Boolean CompleteAzimuth(DataTable dtinfo)
        {
            tb.Clear();
            double avgx = 0, avgy = 0;
            for (int i = 0; i < dtinfo.Rows.Count; i++)
            {
                double x = Convert.ToDouble(dtinfo.Rows[i]["x"]);
                double y = Convert.ToDouble(dtinfo.Rows[i]["y"]);
                avgx += x;
                avgy += y;
            }
            avgx /= dtinfo.Rows.Count;
            avgy /= dtinfo.Rows.Count;
            Geometric.Point endavg = new Geometric.Point(avgx, avgy, 0);
            double minx = double.MaxValue, miny = double.MaxValue, maxx = double.MinValue, maxy = double.MinValue;
            for (int i = 0; i < dtinfo.Rows.Count; i++)
            {
                //Debug.WriteLine(i);
                double x = Convert.ToDouble(dtinfo.Rows[i]["x"]);
                double y = Convert.ToDouble(dtinfo.Rows[i]["y"]);

                if (x < minx) minx = x;
                if (x > maxx) maxx = x;
                if (y < miny) miny = y;
                if (y > maxy) maxy = y;

                Geometric.Point start = new Geometric.Point(x, y, 0);
                
                double aziavg = LTE.Geometric.GeometricUtilities.getPolarCoord(start, endavg).theta / Math.PI * 180;
                aziavg = GeometricUtilities.ConvertGeometricArithmeticAngle(aziavg + 1);
                Debug.WriteLine("路测中点计算角度:" + aziavg);
                DataRow thisrow = tb.NewRow();
                thisrow["fromName"] = this.virname;
                thisrow["x"] = x;
                thisrow["y"] = y;
                thisrow["ReceivePW"] = dtinfo.Rows[i]["ReceivePW"];
                thisrow["CI"] = dtinfo.Rows[i]["CI"];
                thisrow["Azimuth"] = aziavg;
                thisrow["Distance"] = distanceXY(start.X, start.Y, endavg.X, endavg.Y) + 300;
                tb.Rows.Add(thisrow);
            }
            if (maxx - minx < 100 || maxy - maxx < 100)
            {
                tb.Clear();
                return false;
            }
            return true;
        }

        /// <summary>
        /// 角度约束
        /// </summary>
        /// <param name="dtinfo"></param>
        /// <param name="num"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        private DataTable ComputePointByA(DataTable dtinfo, int num, double angle)
        {
            //找到当前路测区域的中心点
            double centerx = (maxLon + minLon) / 2;
            double centery = (maxLat + minLat) / 2;
            Geometric.Point start = new Geometric.Point(centerx, centery, 0);
            LTE.Utils.PointConvertByProj.Instance.GetProjectPoint(start);
            DataTable ans = dtinfo.Clone();
            if (dtinfo.Rows.Count < num)
            {
                //MessageBox.Show(this, "ComputePointByA所筛选出的结果不足最小选点数", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return ans;
            }
            int len = dtinfo.Rows.Count;
            int acount = 0;//已满足条件的数量
            for (int i = 0; i < len - 1 && acount < num; i++)
            {
                double x = Convert.ToDouble(dtinfo.Rows[i]["x"]);
                double y = Convert.ToDouble(dtinfo.Rows[i]["y"]);
                Point curend = new Point(x, y, 0);
                double cura = GeometricUtilities.getPolarCoord(start, curend).theta / Math.PI * 180;
                cura = GeometricUtilities.ConvertGeometricArithmeticAngle(cura + 1);
                bool flag = true;

                //与之前的每个点进行比较
                for (int j = ans.Rows.Count - 1; j >= 0; j--)
                {
                    flag = true;
                    double xf = Convert.ToDouble(ans.Rows[j]["x"]);
                    double yf = Convert.ToDouble(ans.Rows[j]["y"]);
                    Point hisend = new Point(xf, yf, 0);
                    double hisa = GeometricUtilities.getPolarCoord(start, hisend).theta / Math.PI * 180;
                    hisa = GeometricUtilities.ConvertGeometricArithmeticAngle(hisa + 1);
                    if (Math.Abs(hisa - cura) < angle)
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                {
                    DataRow thisrow = ans.NewRow();
                    thisrow["x"] = x;
                    thisrow["y"] = y;
                    thisrow["ReceivePW"] = Convert.ToDouble(dtinfo.Rows[i]["ReceivePW"]);
                    thisrow["CI"] = Convert.ToInt32(dtinfo.Rows[i]["CI"]);
                    ans.Rows.Add(thisrow);
                    acount++;
                }
            }
            return ans;

        }

        /// <summary>
        /// 初步获取点间距满足要求的路测点
        /// </summary>
        /// <param name="points">从数据库读取的数据</param>
        /// <param name="num">选取的点的个数</param>
        /// <param name="distanceBT">点间距</param>
        /// <returns></returns>
        private DataTable ComputePointByD(DataTable points, int num, double distanceBT)
        {
            DataTable ans = reset.Clone();
            ///若筛选出的数据小于num
            if (points.Rows.Count < num)
            {
                //MessageBox.Show(this, "ComputePointByD所筛选出的结果不足最小选点数", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return ans;
            }
            minLon = Convert.ToInt32(points.Rows[0]["Lon"]);
            minLat = Convert.ToInt32(points.Rows[0]["Lat"]);
            maxLon = minLon;
            maxLat = minLat;
            for (int i = 1; i < points.Rows.Count; i++)
            {
                //if (tb.Rows.Count == num) break;//找到指定数的点
                int id = Convert.ToInt32(points.Rows[i]["ID"]);
                double Lon = Convert.ToDouble(points.Rows[i]["Lon"]);
                double Lat = Convert.ToDouble(points.Rows[i]["Lat"]);


                Boolean flag = true;

                //与之前的每个点进行比较
                for (int j = ans.Rows.Count - 1; j >= 0; j--)
                {
                    flag = true;
                    double Lonf = Convert.ToDouble(ans.Rows[j]["Lon"]);
                    double Latf = Convert.ToDouble(ans.Rows[j]["Lat"]);

                    double dis = CJWDHelper.distance(Lon,Lat,Lonf,Latf)*1000;

                    if (dis < distanceBT)
                    {
                        flag = false;
                        break;
                    }

                }
                if (flag)
                {
                    if (Lon > maxLon)
                    {
                        maxLon = Lon;
                    }
                    else if (minLon > Lon)
                    {
                        minLon = Lon;
                    }
                    if (Lat > maxLat)
                    {
                        maxLat = Lat;
                    }
                    else if (minLat > Lat)
                    {
                        minLat = Lat;
                    }
                    DataRow thisrow = ans.NewRow();
                    thisrow["Lon"] = Lon;
                    thisrow["Lat"] = Lat;
                    Point pt = new Point(Lon, Lat, 0);
                    pt = PointConvertByProj.Instance.GetProjectPoint(pt);
                    thisrow["x"] = pt.X;
                    thisrow["y"] = pt.Y;
                    thisrow["ReceivePW"] = Math.Pow(10, (Convert.ToDouble(points.Rows[i]["RSRP"]) / 10 - 3));
                    thisrow["CI"] = id;
                    ans.Rows.Add(thisrow);
                }
            }
            if (ans.Rows.Count < num)
            {
                //MessageBox.Show(this, "选点数不足最小选点数", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return ans;
            }
            return ans;
        }

        private double distanceXY(double x, double y, double ex, double ey)
        {
            double deteX = Math.Pow((x - ex), 2);
            double deteY = Math.Pow((y - ey), 2);
            double distance = Math.Sqrt(deteX + deteY);
            return distance;
        }
        private void AddToVirsource()
        {
            //判断是否已经有
            //若有，直接返回true
            Hashtable ht1 = new Hashtable();
            ht1["cellname"] = this.virname;
            DataTable isExist = IbatisHelper.ExecuteQueryForDataTable("GetVirSourceAllInfo", ht1);
            if (isExist.Rows.Count == 0 || isExist.Rows[0]["x"] == DBNull.Value)
            {
                Debug.WriteLine("未添加过该目标源");
                Hashtable htbts = new Hashtable();
                htbts["BtsName"] = this.virname;
                DataTable dtcell = IbatisHelper.ExecuteQueryForDataTable("GetSource", htbts);
                if(dtcell!=null && dtcell.Rows.Count == 1)
                {
                    Hashtable ht = new Hashtable();
                    ht["CellName"] = this.virname;
                    ht["Longitude"] = Convert.ToDouble(dtcell.Rows[0]["Longitude"]);
                    ht["Latitude"] = Convert.ToDouble(dtcell.Rows[0]["Latitude"]);
                    ht["x"] = Convert.ToDouble(dtcell.Rows[0]["x"]);
                    ht["y"] = Convert.ToDouble(dtcell.Rows[0]["y"]);
                    ht["z"] = 0;
                    ht["Altitude"] = 0;
                    ht["AntHeight"] = Convert.ToDouble(dtcell.Rows[0]["AntHeight"]);

                    // 2017.4.28 添加
                    ht["Tilt"] = Convert.ToInt32(dtcell.Rows[0]["Tilt"]);
                    ht["EIRP"] = Convert.ToInt32(dtcell.Rows[0]["EIRP"]);
                    ht["NetType"] = "";
                    ht["CI"] = Convert.ToInt32(dtcell.Rows[0]["CI"]);
                    ht["EARFCN"] = Convert.ToInt32(dtcell.Rows[0]["EARFCN"]);
                    ht["eNodeB"] = Convert.ToInt32(dtcell.Rows[0]["eNodeB"]);
                    IbatisHelper.ExecuteInsert("insertVirSource", ht);
                }
                else
                {
                    Debug.WriteLine("无该版本路测干扰源信息");
                }
            }
            else
            {
                Debug.WriteLine("已添加过该目标源");
            }

        }
    }

    public class UpdateSP
    {
        public string virname { get; set; }
        public double inflon { get; set; }
        public double inflat { get; set; }

        public Result UpdateSelectPoints()
        {
            Hashtable ht = new Hashtable();
            ht["fromName"] = this.virname;
            DataTable spinfo = IbatisHelper.ExecuteQueryForDataTable("GetSelectedPoint", ht);

            Point endavg = new Point(this.inflon,this.inflat,0);
            PointConvertByProj.Instance.GetProjectPoint(endavg);

            if (spinfo.Rows.Count < 1)
            {
                return new Result(false, "无初始选点数据");
            }
            for(int i = 0; i < spinfo.Rows.Count; i++)
            {
                int ci = Convert.ToInt32(spinfo.Rows[i]["CI"].ToString());
                double x = Convert.ToDouble(spinfo.Rows[i]["x"].ToString());
                double y = Convert.ToDouble(spinfo.Rows[i]["y"].ToString());
                Point start = new Point(x, y, 0);
                double Azimuth = LTE.Geometric.GeometricUtilities.getPolarCoord(start, endavg).theta / Math.PI * 180;
                Azimuth = GeometricUtilities.ConvertGeometricArithmeticAngle(Azimuth + 1);
                Hashtable htupdate = new Hashtable();
                htupdate["fromName"] = this.virname;
                htupdate["CI"] = ci;
                htupdate["Azimuth"] = Azimuth;
                IbatisHelper.ExecuteUpdate("UpdatetbSelectedPointByCI", htupdate);
            }
            return new Result(true, "更新成功");
        }
    }
    

}