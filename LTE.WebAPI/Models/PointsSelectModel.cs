﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Collections;
using LTE.Geometric;
using LTE.DB;
using System.Diagnostics;
using System.Data.SqlClient;
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
        private double maxx = 0, maxy = 0, minx = 0, miny = 0;

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
            reset.Columns.Add("CI");
            reset.Columns.Add("x");
            reset.Columns.Add("y");
            reset.Columns.Add("ReceivePW");
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
            Debug.WriteLine("进入距离约束阶段》》》");
            DataTable firstRet = ComputePointByD(dtinfo, this.pointNum, this.DisCons);
            if (firstRet != null && firstRet.Rows.Count > this.pointNum)//先进行距离筛选，
            {
                Debug.WriteLine("进入角度约束阶段》》》");
                DataTable secRet = ComputePointByA(firstRet, this.pointNum, this.AngleCons);

                if (secRet != null && secRet.Rows.Count == this.pointNum)
                {
                    Debug.WriteLine("进入信息填写阶段》》》");
                    //暂时试试用干扰源计算方位角的结果，如果用干扰源计算方位角不行，则说明不行
                    if (CompleteAzimuthVir(secRet) && tb.Rows.Count == this.pointNum)
                    {
                        //
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
                tb.Rows.Add(thisrow);
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
            double centerx = (maxx + minx) / 2;
            double centery = (maxy + miny) / 2;
            Geometric.Point start = new Geometric.Point(centerx, centery, 0);
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
                double x = Convert.ToInt32(dtinfo.Rows[i]["x"]);
                double y = Convert.ToInt32(dtinfo.Rows[i]["y"]);
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
            minx = Convert.ToInt32(points.Rows[0]["x"]);
            miny = Convert.ToInt32(points.Rows[0]["y"]);
            maxx = minx;
            maxy = miny;
            for (int i = 1; i < points.Rows.Count; i++)
            {
                //if (tb.Rows.Count == num) break;//找到指定数的点
                int id = Convert.ToInt32(points.Rows[i]["ID"]);
                double x = Convert.ToInt32(points.Rows[i]["x"]);
                double y = Convert.ToInt32(points.Rows[i]["y"]);


                Boolean flag = true;

                //与之前的每个点进行比较
                for (int j = ans.Rows.Count - 1; j >= 0; j--)
                {
                    flag = true;
                    double xf = Convert.ToDouble(ans.Rows[j]["x"]);
                    double yf = Convert.ToDouble(ans.Rows[j]["y"]);

                    double dis = this.distanceXY(x, y, xf, yf);

                    if (dis < distanceBT)
                    {
                        flag = false;
                        break;
                    }

                }
                if (flag)
                {
                    if (x > maxx)
                    {
                        maxx = x;
                    }
                    else if (minx > x)
                    {
                        minx = x;
                    }
                    if (y > maxy)
                    {
                        maxy = y;
                    }
                    else if (miny > y)
                    {
                        miny = y;
                    }
                    DataRow thisrow = ans.NewRow();
                    thisrow["x"] = x;
                    thisrow["y"] = y;
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
                Debug.WriteLine("添加完成");
            }
            else
            {
                Debug.WriteLine("已添加过该目标源");
            }

        }
    }
    

}