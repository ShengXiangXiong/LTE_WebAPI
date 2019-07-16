using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LTE.DB;
using System.Data;
using System.Data.SqlClient;
using LTE.InternalInterference.Grid;
using LTE.GIS;
using ESRI.ArcGIS.Geometry;

namespace LTE.WebAPI.Models
{
    // 路测管理
    public class DTdataModel
    {
        #region 虚拟路测

        // 删除虚拟路测
        public static Result deleteVDT()
        {
            try
            {
                IbatisHelper.ExecuteDelete("DeleteDT", null);
            }
            catch (Exception e)
            {
                return new Result(false, e.ToString());
            }
            return new Result(true);
        }

        // 生成虚拟路测
        public static Result addVDT()
        {
            // 注意：确保已完成区域覆盖计算

            // 每条路径上的点序列，确保每两个点之间的路径接近于直线
            double[] xx1 = { 667987, 667596, 667172, 667001 };
            double[] yy1 = { 3545330, 3545454, 3545668, 3545775 };
            double[] xx2 = { 667001, 667063, 667090, 667260, 667264, 667296, 667338, 667299, 667192 };
            double[] yy2 = { 3545775, 3546033, 3546242, 3546968, 3547139, 3547208, 3547361, 3547476, 3547626 };
            double[] xx3 = { 667192, 667291, 667663, 667881, 668101, 668244, 669079 };
            double[] yy3 = { 3547626, 3547600, 3547590, 3547618, 3547609, 3547594, 3547436 };
            double[] xx4 = { 669079, 668983 };
            double[] yy4 = { 3547436, 3546849 };

            List<List<double>> vx = new List<List<double>>();
            List<List<double>> vy = new List<List<double>>();

            vx.Add(new List<double>(xx1));
            vx.Add(new List<double>(xx2));
            vx.Add(new List<double>(xx3));
            vx.Add(new List<double>(xx4));
            vy.Add(new List<double>(yy1));
            vy.Add(new List<double>(yy2));
            vy.Add(new List<double>(yy3));
            vy.Add(new List<double>(yy4));

            // 起始序号
            int id = 0;
            int roadid = 0;

            // 虚拟路测路径生成
            DTPathGen(ref vx, ref vy, ref id, ref roadid);

            // 虚拟路测场强填写
            DTStrength();

            return new Result(true);
        }

        // 虚拟路测路径生成
        // vx, vy 为每条虚拟路测路径上的点序列，每两点之间近乎直线
        public static void DTPathGen(ref List<List<double>> vx, ref List<List<double>> vy, ref int id, ref int roadid)
        {
            DataTable dtable = new DataTable();
            dtable.Columns.Add("id", Type.GetType("System.Int32"));
            dtable.Columns.Add("dateTime", Type.GetType("System.DateTime"));
            dtable.Columns.Add("RoadID", Type.GetType("System.Int32"));
            dtable.Columns.Add("x", Type.GetType("System.Decimal"));
            dtable.Columns.Add("y", Type.GetType("System.Decimal"));
            dtable.Columns.Add("longtitude", Type.GetType("System.Decimal"));
            dtable.Columns.Add("latitude", Type.GetType("System.Decimal"));
            dtable.Columns.Add("gxid", Type.GetType("System.Int32"));
            dtable.Columns.Add("gyid", Type.GetType("System.Int32"));
            dtable.Columns.Add("RecePowerDbm", Type.GetType("System.Double")); 

            for (int k = 0; k < vx.Count; k++)
            {

                double x1 = vx[k][0], y1 = vy[k][0];
                double x2, y2;

                for (int i = 1; i < vx[k].Count; i++)
                {
                    x2 = vx[k][i];
                    y2 = vy[k][i];

                    gen(ref id, roadid, x1, y1, x2, y2, ref dtable);

                    x1 = x2;
                    y1 = y2;
                }
                roadid++;
            }

            try
            {
                using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                {
                    bcp.BatchSize = dtable.Rows.Count;
                    bcp.BulkCopyTimeout = 1000;
                    bcp.DestinationTableName = "tbDT";
                    bcp.WriteToServer(dtable);
                    bcp.Close();
                }
            }
            catch (Exception e) 
            { 
                Console.WriteLine(e.ToString());
            }
            dtable.Clear();
        }

        // 每一条虚拟路测路径生成
        static void gen(ref int id, int roadid, double x1, double y1, double x2, double y2, ref DataTable dtable)
        {
            double dx = Math.Abs(x2 - x1);
            double dy = Math.Abs(y2 - y1);

            double dis = 5;  // 间隔

            if (dy < 10)
            {
                double y = (y1 + y2) / 2.0;
                if (x2 > x1)
                {
                    for (double x = x1; x < x2; x += dis)
                    {
                        DateTime dt = DateTime.Now; 
                        System.Data.DataRow thisrow = dtable.NewRow();
                        thisrow["id"] = id++;
                        thisrow["dateTime"] = System.Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                        thisrow["RoadID"] = roadid;
                        thisrow["x"] = Math.Round(x, 1);
                        thisrow["y"] = Math.Round(y, 1);
                        thisrow["longtitude"] = 0;
                        thisrow["latitude"] = 0;
                        thisrow["gxid"] = 0;
                        thisrow["gyid"] = 0;
                        thisrow["RecePowerDbm"] = 0;
                        dtable.Rows.Add(thisrow);
                    }
                }
                else
                {
                    for (double x = x1; x > x2; x -= dis)
                    {
                        DateTime dt = DateTime.Now;
                        System.Data.DataRow thisrow = dtable.NewRow();
                        thisrow["id"] = id++;
                        thisrow["dateTime"] = System.Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                        thisrow["RoadID"] = roadid;
                        thisrow["x"] = Math.Round(x, 1);
                        thisrow["y"] = Math.Round(y, 1);
                        thisrow["longtitude"] = 0;
                        thisrow["latitude"] = 0;
                        thisrow["gxid"] = 0;
                        thisrow["gyid"] = 0;
                        thisrow["RecePowerDbm"] = 0;
                        dtable.Rows.Add(thisrow);
                    }
                }
            }
            else if (dx < 10)
            {
                double x = (x1 + x2) / 2.0;
                if (y2 > y1)
                {
                    for (double y = y1; y < y2; y += dis)
                    {
                        DateTime dt = DateTime.Now;
                        System.Data.DataRow thisrow = dtable.NewRow();
                        thisrow["id"] = id++;
                        thisrow["dateTime"] = System.Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                        thisrow["RoadID"] = roadid;
                        thisrow["x"] = Math.Round(x, 1);
                        thisrow["y"] = Math.Round(y, 1);
                        thisrow["longtitude"] = 0;
                        thisrow["latitude"] = 0;
                        thisrow["gxid"] = 0;
                        thisrow["gyid"] = 0;
                        thisrow["RecePowerDbm"] = 0;
                        dtable.Rows.Add(thisrow);
                    }
                }
                else
                {
                    for (double y = y1; y > y2; y -= dis)
                    {
                        DateTime dt = DateTime.Now;
                        System.Data.DataRow thisrow = dtable.NewRow();
                        thisrow["id"] = id++;
                        thisrow["dateTime"] = System.Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                        thisrow["RoadID"] = roadid;
                        thisrow["x"] = Math.Round(x, 1);
                        thisrow["y"] = Math.Round(y, 1);
                        thisrow["longtitude"] = 0;
                        thisrow["latitude"] = 0;
                        thisrow["gxid"] = 0;
                        thisrow["gyid"] = 0;
                        thisrow["RecePowerDbm"] = 0;
                        dtable.Rows.Add(thisrow);
                    }
                }
            }
            else
            {
                if (dx > dy)
                {
                    double k = dy / dx;
                    double ddy = dis * k;
                    if (y1 > y2)
                        ddy = -ddy;

                    if (x1 < x2)
                    {
                        for (double x = x1, y = y1; x < x2; x += dis, y += ddy)
                        {
                            DateTime dt = DateTime.Now;
                            System.Data.DataRow thisrow = dtable.NewRow();
                            thisrow["id"] = id++;
                            thisrow["dateTime"] = System.Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                            thisrow["RoadID"] = roadid;
                            thisrow["x"] = Math.Round(x, 1);
                            thisrow["y"] = Math.Round(y, 1);
                            thisrow["longtitude"] = 0;
                            thisrow["latitude"] = 0;
                            thisrow["gxid"] = 0;
                            thisrow["gyid"] = 0;
                            thisrow["RecePowerDbm"] = 0;
                            dtable.Rows.Add(thisrow);
                        }
                    }
                    else
                    {
                        for (double x = x1, y = y1; x > x2; x -= dis, y += ddy)
                        {
                            DateTime dt = DateTime.Now;
                            System.Data.DataRow thisrow = dtable.NewRow();
                            thisrow["id"] = id++;
                            thisrow["dateTime"] = System.Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                            thisrow["RoadID"] = roadid;
                            thisrow["x"] = Math.Round(x, 1);
                            thisrow["y"] = Math.Round(y, 1);
                            thisrow["longtitude"] = 0;
                            thisrow["latitude"] = 0;
                            thisrow["gxid"] = 0;
                            thisrow["gyid"] = 0;
                            thisrow["RecePowerDbm"] = 0;
                            dtable.Rows.Add(thisrow);
                        }
                    }
                }
                else
                {
                    double k = dx / dy;
                    double ddx = dis * k;
                    if (x1 > x2)
                        ddx = -ddx;

                    if (y1 < y2)
                    {
                        for (double y = y1, x = x1; y < y2; y += dis, x += ddx)
                        {
                            DateTime dt = DateTime.Now;
                            System.Data.DataRow thisrow = dtable.NewRow();
                            thisrow["id"] = id++;
                            thisrow["dateTime"] = System.Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                            thisrow["RoadID"] = roadid;
                            thisrow["x"] = Math.Round(x, 1);
                            thisrow["y"] = Math.Round(y, 1);
                            thisrow["longtitude"] = 0;
                            thisrow["latitude"] = 0;
                            thisrow["gxid"] = 0;
                            thisrow["gyid"] = 0;
                            thisrow["RecePowerDbm"] = 0;
                            dtable.Rows.Add(thisrow);
                        }
                    }
                    else
                    {
                        for (double y = y1, x = x1; y > y2; y -= dis, x += ddx)
                        {
                            DateTime dt = DateTime.Now;
                            System.Data.DataRow thisrow = dtable.NewRow();
                            thisrow["id"] = id++;
                            thisrow["dateTime"] = System.Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                            thisrow["RoadID"] = roadid;
                            thisrow["x"] = Math.Round(x, 1);
                            thisrow["y"] = Math.Round(y, 1);
                            thisrow["longtitude"] = 0;
                            thisrow["latitude"] = 0;
                            thisrow["gxid"] = 0;
                            thisrow["gyid"] = 0;
                            thisrow["RecePowerDbm"] = 0;
                            dtable.Rows.Add(thisrow);
                        }
                    }
                }
            }
        }

        // 虚拟路测场强填写
        public static void DTStrength()
        {
            // 读取原始路测
            DataTable tb1 = IbatisHelper.ExecuteQueryForDataTable("getDT1", null);
            int n = tb1.Rows.Count;
            List<DT> tbDT = new List<DT>();
            for (int i = 0; i < tb1.Rows.Count; i++)
            {
                DT dt = new DT();
                dt.id = Convert.ToInt32(tb1.Rows[i]["id"].ToString());
                dt.roadID = Convert.ToInt32(tb1.Rows[i]["roadID"].ToString());
                dt.ci = Convert.ToInt32(tb1.Rows[i]["ci"].ToString());
                //dt.longitude = Convert.ToDouble(tb1.Rows[i]["longtitude"].ToString());
                //dt.latitude = Convert.ToDouble(tb1.Rows[i]["latitude"].ToString());
                dt.x = Convert.ToDouble(tb1.Rows[i]["x"].ToString());
                dt.y = Convert.ToDouble(tb1.Rows[i]["y"].ToString());
                tbDT.Add(dt);
            }

            // 得到网格编号，写入数据库
            System.Data.DataTable tb = new System.Data.DataTable();
            tb.Columns.Add("id");
            tb.Columns.Add("dateTime");
            tb.Columns.Add("GXID");
            tb.Columns.Add("GYID");
            tb.Columns.Add("ci");
            tb.Columns.Add("RoadID");
            tb.Columns.Add("x");
            tb.Columns.Add("y");
            tb.Columns.Add("longtitude");
            tb.Columns.Add("latitude");
            tb.Columns.Add("RecePowerDbm");
            

            int xid = 0, yid = 0;
            for (int i = 0; i < tbDT.Count; i++)
            {
                GridHelper.getInstance().XYToGGrid(tbDT[i].x, tbDT[i].y, ref xid, ref yid); // 网格编号

                IPoint p = new PointClass();
                p.X = tbDT[i].x;
                p.Y = tbDT[i].y;
                p.Z = 0;
                PointConvert.Instance.GetGeoPoint(p);

                DateTime dt = DateTime.Now;

                System.Data.DataRow thisrow = tb.NewRow();
                thisrow["id"] = tbDT[i].id;
                thisrow["dateTime"] = dt.ToLocalTime().ToString();
                thisrow["GXID"] = xid;
                thisrow["GYID"] = yid;
                thisrow["ci"] = tbDT[i].ci;
                thisrow["RoadID"] = tbDT[i].roadID;
                thisrow["x"] = tbDT[i].x;
                thisrow["y"] = tbDT[i].y;
                thisrow["longtitude"] = Math.Round(p.X, 6);
                thisrow["latitude"] = Math.Round(p.Y, 6);
                thisrow["RecePowerDbm"] = 0;
                tb.Rows.Add(thisrow);
            }

            // 删除原始路测
            IbatisHelper.ExecuteDelete("DeleteDT", null);

            using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
            {
                bcp.BatchSize = tb.Rows.Count;
                bcp.BulkCopyTimeout = 1000;
                bcp.DestinationTableName = "tbDT";
                bcp.WriteToServer(tb);
                bcp.Close();
            }
            tb.Clear();

            // 得到网格对应的路测数据
            IbatisHelper.ExecuteUpdate("UpdateDT1", null);
            IbatisHelper.ExecuteUpdate("UpdateDT2", null);
        }
        #endregion

        #region 真实路测  -- 滕佳言

        // 真实路测预处理
        // LTE 中的真实的路测格式目前仍未给出，暂时采用以前 GSM 中的路测格式
        public static Result preDTdata()
        {
            Result rt = new Result();

            Dictionary<string, AtuInfo> AtuDic = new Dictionary<string, AtuInfo>();
            rt = getPreAtu(ref AtuDic);
            if (!rt.ok)
                return rt;

            ATUData2SQL(ref AtuDic);

            return rt;
        }

        //得到预处理后的ATU数据，存入atu中
        public static Result getPreAtu(ref Dictionary<string, AtuInfo> atu)
        {
            DataTable tb = new DataTable();
            tb = IbatisHelper.ExecuteQueryForDataTable("getAtuData", null);

            if (tb.Rows.Count < 1)
            {
                return new Result(false, "无路测数据！");
            }
            else
            {
                for (int i = 0; i < tb.Rows.Count; i++)
                {
                    string cellID = Convert.ToString(tb.Rows[i]["cellID"].ToString());
                    double rsrp = Convert.ToDouble(tb.Rows[i]["RSRP"].ToString());
                    double longitude = Convert.ToDouble(tb.Rows[i]["Longitude"].ToString());
                    double latitude = Convert.ToDouble(tb.Rows[i]["Latitude"].ToString());

                    int gxid = 0;
                    int gyid = 0;

                    //根据路测数据的经纬度找到所在栅格坐标
                    if (GridHelper.getInstance().LngLatToGGrid(longitude, latitude, ref gxid, ref gyid))
                    {

                        string key = string.Format("{0},{1}", gxid, gyid);
                        if (atu.Keys.Contains(key))
                        {
                            if (atu[key].CellATU.Keys.Contains(cellID))
                            {
                                atu[key].CellATU[cellID].CellID = cellID;
                                atu[key].CellATU[cellID].AllRsrp += rsrp;
                                atu[key].CellATU[cellID].NumofAtu++;
                                if (rsrp > atu[key].CellATU[cellID].maxRSRP)
                                {
                                    atu[key].CellATU[cellID].maxRSRP = rsrp;
                                }
                                atu[key].CellATU[cellID].avgRSRP = atu[key].CellATU[cellID].AllRsrp / atu[key].CellATU[cellID].NumofAtu;
                            }
                            else
                            {
                                cellAtuInfo ca = new cellAtuInfo();
                                ca.CellID = cellID;
                                ca.AllRsrp += rsrp;
                                ca.NumofAtu = 1;
                                ca.maxRSRP = rsrp;
                                ca.avgRSRP = ca.AllRsrp / ca.NumofAtu;
                                atu[key].CellATU[cellID] = ca;

                            }
                        }
                        else
                        {
                            AtuInfo ai = new AtuInfo();
                            ai.gxid = gxid;
                            ai.gyid = gyid;
                            ai.CellATU = new Dictionary<string, cellAtuInfo>();
                            cellAtuInfo ca = new cellAtuInfo();
                            ca.CellID = cellID;
                            ca.AllRsrp = rsrp;
                            ca.maxRSRP = rsrp;
                            ca.NumofAtu = 1;
                            ca.avgRSRP = ca.AllRsrp / ca.NumofAtu;
                            if (ca != null)
                            {
                                ai.CellATU[cellID] = ca;
                            }
                            atu[key] = ai;

                        }
                    }

                }
            }
            return new Result(true);
        }

        // 将atu中存放的数据存到数据库中
        public static void ATUData2SQL(ref Dictionary<string, AtuInfo> atu)
        {
            System.Data.DataTable dtable = new System.Data.DataTable();
            dtable.Columns.Add("GXID");
            dtable.Columns.Add("GYID");
            dtable.Columns.Add("CELLID");
            dtable.Columns.Add("AllRsrp");
            dtable.Columns.Add("maxRSRP");
            dtable.Columns.Add("NumofAtu");
            dtable.Columns.Add("avgRSRP");
            dtable.Columns.Add("Scen");

            foreach (string key in atu.Keys)
            {
                foreach (string index in atu[key].CellATU.Keys)
                {
                    System.Data.DataRow thisrow = dtable.NewRow();
                    thisrow["GXID"] = atu[key].gxid;
                    thisrow["GYID"] = atu[key].gyid;
                    thisrow["CELLID"] = atu[key].CellATU[index].CellID;
                    thisrow["AllRsrp"] = atu[key].CellATU[index].AllRsrp;
                    thisrow["maxRSRP"] = atu[key].CellATU[index].maxRSRP;
                    thisrow["NumofAtu"] = atu[key].CellATU[index].NumofAtu;
                    thisrow["avgRSRP"] = atu[key].CellATU[index].avgRSRP;
                    if (Convert.ToInt32(atu[key].gxid) < 305)
                    {
                        thisrow["Scen"] = 1;
                    }
                    else if (Convert.ToInt32(atu[key].gxid) >= 305 && atu[key].gxid < 602)
                    {
                        thisrow["Scen"] = 2;
                    }
                    else
                        thisrow["Scen"] = 3;

                    dtable.Rows.Add(thisrow);

                }

                if (dtable.Rows.Count > 50000)
                {
                    using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                    {
                        bcp.BatchSize = dtable.Rows.Count;
                        bcp.BulkCopyTimeout = 1000;
                        bcp.DestinationTableName = "tbPreATU";
                        bcp.WriteToServer(dtable);
                        bcp.Close();
                    }
                    dtable.Clear();
                }
            }

            using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
            {
                bcp.BatchSize = dtable.Rows.Count;
                bcp.BulkCopyTimeout = 1000;
                bcp.DestinationTableName = "tbPreATU";
                bcp.WriteToServer(dtable);
                bcp.Close();
            }
            dtable.Clear();
        }
        #endregion
    }

    //用于存储atu路测数据
    public class AtuInfo
    {
        public double gxid;                                // 路测点坐标
        public double gyid;
        public Dictionary<string, cellAtuInfo> CellATU;     // 某主小区内的路测点
    }

    //以CellID为主小区的路测数据
    public class cellAtuInfo
    {
        public string CellID;                       // 路测点的主小区
        public double AllRsrp;                      // 路测点内的路测点rsrp叠加
        public double maxRSRP;                      // 路测点内的最大RSRP
        public int NumofAtu;                        // 路测点内的路测数量
        public double avgRSRP;                      // 路测点内的平均RSRP
    }
}