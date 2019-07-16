using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LTE.DB;
using ESRI.ArcGIS.Geometry;
using LTE.GIS;
using System.Data.SqlClient;
using System.Collections;
using System.Data;
using LTE.InternalInterference.Grid;

namespace LTE.WebAPI.Models
{
    // 网格划分，包括地面栅格、建筑物栅格，均匀栅格加速结构
    public class GridModel
    {
        /// <summary>
        /// 网格名称
        /// </summary>
        public string name { get; set; }  

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
        /// 网格边长
        /// </summary>
        public double sideLength { get; set; }

        //给定DEM数据的最大固定范围的经纬度
        private double fixMinLongitude = 118;
        private double fixMaxLongitude = 120;
        private double fixMinLatitude = 31;
        private double fixMaxLatitude = 33;

        /// <summary>
        /// 2019.05.29 ShengXiang.Xiong
        /// 修改均匀栅格划分的范围，使其与DEM数据的栅格范围一致
        /// fix*表示该地区DEM数据确定的经纬度范围，minX等表示选择栅格划分的范围，gridLength表示栅格大小
        /// </summary>
        /// <param name="minX"></param>
        /// <param name="minY"></param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        /// <param name="gridLength"></param>
        public double[] tinAlignment(double minX, double minY, double maxX, double maxY, double gridLength)
        {
            ESRI.ArcGIS.Geometry.IPoint pMin = new ESRI.ArcGIS.Geometry.PointClass();
            pMin.X = this.fixMinLongitude;
            pMin.Y = this.fixMinLatitude;
            pMin.Z = 0;
            PointConvert.Instance.GetProjectPoint(pMin);

            ESRI.ArcGIS.Geometry.IPoint pMax = new ESRI.ArcGIS.Geometry.PointClass();
            pMax.X = this.fixMaxLongitude;
            pMax.Y = this.fixMaxLatitude;
            pMax.Z = 0;
            PointConvert.Instance.GetProjectPoint(pMax);


            minX = minX - (minX - pMin.X) % gridLength;
            maxX = maxX + (pMax.X - maxX) % gridLength;
            minY = minY - (minY - pMin.Y) % gridLength;
            maxY = maxY + (pMax.Y - maxY) % gridLength;

            //double dx = Math.Abs(maxX - minX);
            //double dy = Math.Abs(maxY - minY);
            //int cnty = Convert.ToInt32(Math.Ceiling(dy / gridLength));
            //int cntx = Convert.ToInt32(Math.Ceiling(dx / gridLength));

            return new double[4] { minX, minY, maxX, maxY };
        }

        /// <summary>
        /// 网格划分，10*10 公里耗时几个小时
        /// </summary>
        /// <returns></returns>
        public Result ConstructGrid()
        {
            // 地面网格
            try
            {
                IbatisHelper.ExecuteDelete("DeleteGroundGrids", null);
            }
            catch(Exception e)
            {
                return new Result(false, e.ToString());
            }

            System.Data.DataTable dtable = new System.Data.DataTable();
            dtable.Columns.Add("GXID");
            dtable.Columns.Add("GYID");
            dtable.Columns.Add("CLong");
            dtable.Columns.Add("CLat");
            dtable.Columns.Add("MinLong");
            dtable.Columns.Add("MinLat");
            dtable.Columns.Add("MaxLong");
            dtable.Columns.Add("MaxLat");
            dtable.Columns.Add("CX");
            dtable.Columns.Add("CY");
            dtable.Columns.Add("MinX");
            dtable.Columns.Add("MinY");
            dtable.Columns.Add("MaxX");
            dtable.Columns.Add("MaxY");
            dtable.Columns.Add("Dem");

            ESRI.ArcGIS.Geometry.IPoint pMin = new ESRI.ArcGIS.Geometry.PointClass();
            pMin.X = this.minLongitude;
            pMin.Y = this.minLatitude;
            pMin.Z = 0;
            PointConvert.Instance.GetProjectPoint(pMin);
            //double x1 = pMin.X;
            //double y1 = pMin.Y;

            ESRI.ArcGIS.Geometry.IPoint pMax = new ESRI.ArcGIS.Geometry.PointClass();
            pMax.X = this.maxLongitude;
            pMax.Y = this.maxLatitude;
            pMax.Z = 0;
            PointConvert.Instance.GetProjectPoint(pMax);
            //PointConvert.Instance.GetGeoPoint(pMin);
            //double x2 = pMax.X;
            //double y2 = pMax.Y;

            //与Tin数据栅格对齐 2019.5.30
            double[] tinAlignmentData = tinAlignment(pMin.X, pMin.Y, pMax.X, pMax.Y, 30);
            pMin.X = tinAlignmentData[0];
            pMin.Y = tinAlignmentData[1];
            pMin.X = tinAlignmentData[2];
            pMin.Y = tinAlignmentData[3];

            double dx = Math.Abs(pMax.X - pMin.X);
            double dy = Math.Abs(pMax.Y - pMin.Y);
            int cnty = Convert.ToInt32(Math.Ceiling(dy / this.sideLength));
            int cntx = Convert.ToInt32(Math.Ceiling(dx / this.sideLength));

            double gminX, gminY, gmaxX, gmaxY, gcX, gcY;
            gminX = pMin.X;
            gminY = pMin.Y;

            ESRI.ArcGIS.Geometry.IPoint p1 = new ESRI.ArcGIS.Geometry.PointClass();
            ESRI.ArcGIS.Geometry.IPoint p2 = new ESRI.ArcGIS.Geometry.PointClass();
            ESRI.ArcGIS.Geometry.IPoint p3 = new ESRI.ArcGIS.Geometry.PointClass();
            p1.Z = 0;
            p2.Z = 0;
            p3.Z = 0;
            //  地面栅格
            for (int x = 0; x < cntx; x++)
            {
                gminY = pMin.Y;
                gmaxX = gminX + this.sideLength;
                gcX = (gminX + gminX) / 2.0;
                p1.X = gminX;
                p2.X = gmaxX;
                p3.X = gcX;

                for (int y = 0; y < cnty; y++)
                {
                    gmaxY = gminY + this.sideLength;
                    gcY = (gminY + gmaxY) / 2.0;

                    p1.X = gminX;
                    p2.X = gmaxX;
                    p3.X = gcX;
                    p1.Y = gminY;
                    p2.Y = gmaxY;
                    p3.Y = gcY;
                    p1 =  PointConvert.Instance.GetGeoPoint(p1);
                    p2 = PointConvert.Instance.GetGeoPoint(p2);
                    p3 = PointConvert.Instance.GetGeoPoint(p3);

                    System.Data.DataRow thisrow = dtable.NewRow();
                    thisrow["GXID"] = x;
                    thisrow["GYID"] = y;
                    thisrow["CLong"] = p3.X;
                    thisrow["CLat"] = p3.Y;
                    thisrow["MinLong"] = p1.X;
                    thisrow["MinLat"] = p1.Y;
                    thisrow["MaxLong"] = p2.X;
                    thisrow["MaxLat"] = p2.Y;
                    thisrow["CX"] = gcX;
                    thisrow["CY"] = gcY;
                    thisrow["MinX"] = gminX;
                    thisrow["MinY"] = gminY;
                    thisrow["MaxX"] = gmaxX;
                    thisrow["MaxY"] = gmaxY;
                    thisrow["Dem"] = 0;
                    dtable.Rows.Add(thisrow);
                    gminY = gmaxY;
                }
                gminX = gmaxX;

                // 将地面栅格分批写入数据库
                if (dtable.Rows.Count > 50000)
                {
                    using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                    {
                        bcp.BatchSize = dtable.Rows.Count;
                        bcp.BulkCopyTimeout = 1000;
                        bcp.DestinationTableName = "tbGridDem";
                        bcp.WriteToServer(dtable);
                        bcp.Close();
                    }
                    dtable.Clear();
                }
            }
            // 最后一批地面栅格
            using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
            {
                bcp.BatchSize = dtable.Rows.Count;
                bcp.BulkCopyTimeout = 1000;
                bcp.DestinationTableName = "tbGridDem";
                bcp.WriteToServer(dtable);
                bcp.Close();
            }
            dtable.Clear();

            // 地图范围
            Hashtable ht = new Hashtable();
            ht["id"] = 1;
            ht["AreaMinLong"] = this.minLongitude;
            ht["AreaMinLat"] = this.minLatitude;
            ht["AreaMaxLong"] = this.maxLongitude;
            ht["AreaMaxLat"] = this.maxLatitude;
            ht["GGridSize"] = Convert.ToByte(this.sideLength);
            ht["MaxGGXID"] = cntx - 1;
            ht["MaxGGYID"] = cnty - 1;

            // 2017.4.28 添加
            ht["AreaMinX"] = pMin.X;
            ht["AreaMinY"] = pMin.Y;
            ht["AreaMaxX"] = pMax.X;
            ht["AreaMaxY"] = pMax.Y;
            ht["GHeight"] = 3;
            ht["GBaseHeight"] = 1.5;
            ht["AGridSize"] = 30;
            ht["AGridVSize"] = 30;
            int cnty1 = Convert.ToInt16(Math.Ceiling(dy / 30.0));
            int cntx1 = Convert.ToInt16(Math.Ceiling(dx / 30.0));
            ht["MaxAGXID"] = cntx1 - 1;
            ht["MaxAGYID"] = cnty1 - 1;

            // 暂时不用，当截取地图中一小部分时用到
            ht["MinX"] = pMin.X;
            ht["MinY"] = pMin.Y;
            ht["MinGGXID"] = 0;
            ht["MinGGYID"] = 0;
            ht["MinAGXID"] = 0;
            ht["MinAGYID"] = 0;
            IbatisHelper.ExecuteInsert("insertGridRange", ht);

            DataTable tb = new DataTable();
            tb = IbatisHelper.ExecuteQueryForDataTable("getBuildingVertex1", null);

            Result rt = new Result(true);

            rt = buildingGrids(pMin.X, pMin.Y, pMax.X, pMax.Y, this.sideLength, cntx, cnty);  // 建筑物网格
            if (!rt.ok)
                return rt;

            rt = calcAcclerate(pMin.X, pMin.Y, pMax.X, pMax.Y, 30, cntx1, cnty1, ref tb);  // 均匀栅格
            if (!rt.ok)
                return rt;
            
            return rt;
        }

        class P
        {
            public int id;
            public int x;
            public int y;
            public int z;

            public P() { }

            public P(int id1, int x1, int y1, int z1)
            {
                id = id1;
                x = x1;
                y = y1;
                z = z1;
            }
        }

        class BuildingVertex
        {
            public int bid;
            public double vx, vy, vz;
            public int vid;
        }

        // 计算建筑物的表面网格   2018.4.19
        Result buildingGrids(double minX, double minY, double maxX, double maxY, double gridsize, int maxgxid, int maxgyid)
        {
            // 删除旧的建筑物网格
            try
            {
                IbatisHelper.ExecuteDelete("DeleteBuildingGrid", null);
            }
            catch (Exception e)
            {
                return new Result(false, e.ToString());
            }

            double err = GridHelper.getInstance().getGGridSize() / 2 + 1;
            DataTable tb = new DataTable();
            tb = IbatisHelper.ExecuteQueryForDataTable("getBuildingVertex22", null);

            double h = (int)GridHelper.getInstance().getGHeight();
            if (tb.Rows.Count < 1)
            {
                return new Result(false, "无建筑物顶点数据");
            }
            else
            {
                List<P> grids = new List<P>();
                List<BuildingVertex> tmp = new List<BuildingVertex>();

                BuildingVertex bv = new BuildingVertex();
                bv.bid = Convert.ToInt32(tb.Rows[0]["BuildingID"].ToString());
                bv.vx = Convert.ToDouble(tb.Rows[0]["VertexX"].ToString());
                bv.vy = Convert.ToDouble(tb.Rows[0]["VertexY"].ToString());
                bv.vz = Convert.ToDouble(tb.Rows[0]["Bheight"].ToString());
                bv.vid = Convert.ToInt32(tb.Rows[0]["VIndex"].ToString());
                int lastid = bv.bid;
                tmp.Add(bv);

                double pMargin = gridsize;
                double dh = GridHelper.getInstance().getGHeight();

                for (int i = 1; i < tb.Rows.Count; i++)
                {
                    BuildingVertex bv1 = new BuildingVertex();
                    bv1.bid = Convert.ToInt32(tb.Rows[i]["BuildingID"].ToString());
                    bv1.vx = Convert.ToDouble(tb.Rows[i]["VertexX"].ToString());
                    bv1.vy = Convert.ToDouble(tb.Rows[i]["VertexY"].ToString());
                    bv1.vz = Convert.ToDouble(tb.Rows[i]["Bheight"].ToString());
                    bv1.vid = Convert.ToInt32(tb.Rows[i]["VIndex"].ToString());

                    if (i == tb.Rows.Count - 1 || bv1.bid != lastid)
                    {
                        double maxx = tmp[0].vx, maxy = tmp[0].vy, minx = tmp[0].vx, miny = tmp[0].vy;
                        for (int j = 1; j < tmp.Count; ++j)
                        {
                            if (tmp[j].vx > maxx)
                                maxx = tmp[j].vx;
                            if (tmp[j].vx < minx)
                                minx = tmp[j].vx;
                            if (tmp[j].vy > maxy)
                                maxy = tmp[j].vy;
                            if (tmp[j].vy < miny)
                                miny = tmp[j].vy;
                        }

                        int gzid = (int)(tmp[0].vz / GridHelper.getInstance().getGHeight()) + 1;
                        int minGxid = 0, minGyid = 0, maxGxid = 0, maxGyid = 0;
                        GridHelper.getInstance().XYToGGrid(minx, miny, ref minGxid, ref minGyid);
                        GridHelper.getInstance().XYToGGrid(maxx, maxy, ref maxGxid, ref maxGyid);
                        if (minGxid == -1 || minGyid == -1)
                            continue;

                        double x = 0, y = 0, z = 0;
                        for (int j = minGxid; j <= maxGxid; j++)
                        {
                            for (int k = minGyid; k <= maxGyid; k++)
                            {
                                GridHelper.getInstance().GridToXYZ(j, k, 0, ref x, ref y, ref z);

                                #region 点是否在平面或边上
                                bool okPlane = false;  // 点是否在平面上
                                bool okEdge = false;  // 点是否在边上

                                double tx = Math.Round(x, 3);
                                for (int ii = 0, jj = tmp.Count - 1; ii < tmp.Count; jj = ii++)
                                {
                                    if ((tmp[ii].vy > y) != (tmp[jj].vy > y))
                                    {
                                        double tmp1 = Math.Round((tmp[jj].vx - tmp[ii].vx) * (y - tmp[ii].vy) / (tmp[jj].vy - tmp[ii].vy) + tmp[ii].vx, 3);

                                        if (Math.Abs(tx - tmp1) < err)
                                        {
                                            okPlane = true;
                                            okEdge = true;
                                            break;
                                        }
                                        else if (tx < tmp1)
                                        {
                                            okPlane = !okPlane;
                                        }
                                    }
                                }

                                if (okPlane)
                                {
                                    grids.Add(new P(tmp[0].bid, j, k, gzid));
                                    grids.Add(new P(tmp[0].bid, j, k, 1));

                                    if (okEdge)
                                    {
                                        for (int zid = 1; zid < gzid; zid++)
                                            grids.Add(new P(tmp[0].bid, j, k, zid));
                                    }
                                }
                                #endregion
                            }
                        }

                        lastid = bv1.bid;
                        tmp.Clear();
                    }
                    tmp.Add(bv1);
                }

                // 最后一个建筑物
                int p = tb.Rows.Count - 1;

                // 写入数据库
                System.Data.DataTable tb1 = new System.Data.DataTable();
                tb1.Columns.Add("BuildingID");
                tb1.Columns.Add("GXID");
                tb1.Columns.Add("GYID");
                tb1.Columns.Add("GZID");

                for (int i = 0; i < grids.Count; i++)
                {
                    System.Data.DataRow thisrow = tb1.NewRow();
                    thisrow["BuildingID"] = grids[i].id;
                    thisrow["GXID"] = grids[i].x;
                    thisrow["GYID"] = grids[i].y;
                    thisrow["GZID"] = grids[i].z;
                    tb1.Rows.Add(thisrow);

                    if (tb1.Rows.Count >= 5000)
                    {
                        using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                        {
                            bcp.BatchSize = tb1.Rows.Count;
                            bcp.BulkCopyTimeout = 1000;
                            bcp.DestinationTableName = "tbBuildingGrid3D";
                            bcp.WriteToServer(tb1);
                            bcp.Close();
                        }
                        tb1.Clear();
                    }
                }

                using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                {
                    bcp.BatchSize = tb1.Rows.Count;
                    bcp.BulkCopyTimeout = 1000;
                    bcp.DestinationTableName = "tbBuildingGrid3D";
                    bcp.WriteToServer(tb1);
                    bcp.Close();
                }
                tb1.Clear();
            }

            return new Result(true);
        }

        // 计算建筑物所在的加速网格   2017.5.4加
        Result calcAcclerate(double minX, double minY, double maxX, double maxY, double agridsize, int maxagxid, int maxagyid, ref DataTable tb)
        {
            try
            {
                IbatisHelper.ExecuteDelete("DeleteBuildingAccrelate", null);
            }
            catch (Exception e)
            {
                return new Result(false, e.ToString());
            }

            if (tb.Rows.Count < 1)
            {
                return new Result(false, "无建筑物顶点数据");
            }
            else
            {
                // KeyValuePair：
                // key--均匀栅格ID
                // value--建筑物高度
                List<KeyValuePair<int, double>>[,] grids = new List<KeyValuePair<int, double>>[maxagxid, maxagyid];
                for (int i = 0; i < maxagxid; i++)
                {
                    for (int j = 0; j < maxagyid; j++)
                        grids[i, j] = new List<KeyValuePair<int, double>>();
                }
                for (int i = 0; i < tb.Rows.Count; i++)
                {
                    // 得到建筑物底面的轴对齐包围盒
                    int id = Convert.ToInt32(tb.Rows[i]["BuildingID"].ToString());
                    double height = Convert.ToDouble(tb.Rows[i]["Bheight"].ToString());
                    double x = Convert.ToDouble(tb.Rows[i]["minX"].ToString());
                    double y = Convert.ToDouble(tb.Rows[i]["minY"].ToString());
                    double x1 = Convert.ToDouble(tb.Rows[i]["maxX"].ToString());
                    double y1 = Convert.ToDouble(tb.Rows[i]["maxY"].ToString());

                    // 建筑物底面跨越的均匀栅格
                    if (x < minX || x1 > maxX || y < minY || y1 > maxY)
                        continue;
                    double dx = Math.Abs(x - minX);
                    double dy = Math.Abs(y - minY);
                    double dx1 = Math.Abs(x1 - minX);
                    double dy1 = Math.Abs(y1 - minY);
                    int minGxid = Convert.ToInt32(Math.Ceiling(dx / agridsize)) - 1;
                    int minGyid = Convert.ToInt32(Math.Ceiling(dy / agridsize)) - 1;
                    int maxGxid = Convert.ToInt32(Math.Ceiling(dx1 / agridsize)) - 1;
                    int maxGyid = Convert.ToInt32(Math.Ceiling(dy1 / agridsize)) - 1;
                    if (minGxid == -1 || minGyid == -1)
                        continue;
                    for (int j = minGxid; j <= maxGxid; j++)
                    {
                        for (int k = minGyid; k <= maxGyid; k++)
                        {
                            grids[j, k].Add(new KeyValuePair<int, double>(id, height));
                        }
                    }
                }

                // 写入数据库
                System.Data.DataTable tb1 = new System.Data.DataTable();
                tb1.Columns.Add("GXID");
                tb1.Columns.Add("GYID");
                tb1.Columns.Add("BuildingID");
                tb1.Columns.Add("BuildingHeight");

                int n = 0;
                for (int i = 0; i < maxagxid; i++)
                {
                    for (int j = 0; j < maxagyid; j++)
                    {
                        if (grids[i, j].Count > 0)
                        {
                            for (int k = 0; k < grids[i, j].Count; k++)
                            {
                                n++;
                                System.Data.DataRow thisrow = tb1.NewRow();
                                thisrow["GXID"] = i;
                                thisrow["GYID"] = j;
                                thisrow["BuildingID"] = grids[i, j][k].Key;
                                thisrow["BuildingHeight"] = grids[i, j][k].Value;
                                tb1.Rows.Add(thisrow);
                            }
                        }
                    }
                }

                using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                {
                    bcp.BatchSize = n;
                    bcp.BulkCopyTimeout = 1000;
                    bcp.DestinationTableName = "tbAccelerationGridBuildingOverlap";
                    bcp.WriteToServer(tb1);
                    bcp.Close();
                }

                // 建立建筑物加速网格
                IbatisHelper.ExecuteDelete("DeleteBuildingAccrelate1", null);
                IbatisHelper.ExecuteInsert("InsertBuildingAccelerate1", null);   // 加速网格高度为30米
                IbatisHelper.ExecuteUpdate("UpdateBuildingAccelerate2", null);
                IbatisHelper.ExecuteInsert("InsertBuildingAccelerate3", null);
                IbatisHelper.ExecuteInsert("InsertBuildingAccelerate4", null);

                return new Result(true);
            }
        }
    }
}