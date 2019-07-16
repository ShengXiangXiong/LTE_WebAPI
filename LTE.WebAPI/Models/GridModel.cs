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

        /// <summary>
        /// 网格划分，10*10 公里耗时几个小时
        /// </summary>
        /// <returns></returns>
        public Result ConstructGrid()
        {
            // 经纬度转投影坐标
            ESRI.ArcGIS.Geometry.IPoint pMin = new ESRI.ArcGIS.Geometry.PointClass();
            pMin.X = this.minLongitude;
            pMin.Y = this.minLatitude;
            pMin.Z = 0;
            PointConvert.Instance.GetProjectPoint(pMin);

            ESRI.ArcGIS.Geometry.IPoint pMax = new ESRI.ArcGIS.Geometry.PointClass();
            pMax.X = this.maxLongitude;
            pMax.Y = this.maxLatitude;
            pMax.Z = 0;
            PointConvert.Instance.GetProjectPoint(pMax);

            // 最大栅格和均匀栅格
            double dx = Math.Abs(pMax.X - pMin.X);
            double dy = Math.Abs(pMax.Y - pMin.Y);
            int maxgxid = Convert.ToInt32(Math.Ceiling(dx / this.sideLength));
            int maxgyid = Convert.ToInt32(Math.Ceiling(dy / this.sideLength));
            int maxAgxid = Convert.ToInt16(Math.Ceiling(dx / 30.0));
            int maxAgyid = Convert.ToInt16(Math.Ceiling(dy / 30.0));

            Result rt = new Result(true);

            // 2019.6.11 地图范围
            rt = calcRange(minLongitude, minLatitude, maxLongitude, maxLatitude, pMin.X, pMin.Y, pMax.X, pMax.Y, maxAgxid, maxAgyid, maxgxid, maxgyid, this.sideLength);
            if (!rt.ok)
                return rt;

            // 2019.5.28 地形和地形所在的加速栅格
            rt = calcTIN(pMin.X, pMin.Y, pMax.X, pMax.Y, 30, maxAgxid, maxAgyid);
            if (!rt.ok)
                return rt;

            // 2019.6.5 得到建筑物海拔，基于地形
            rt = calcBuildingAltitude(pMin.X, pMin.Y, pMax.X, pMax.Y, 0, 0, maxAgxid, maxAgyid, 0, 0, maxgxid, maxgyid);
            if (!rt.ok)
                return rt;

            // 2019.6.11 建筑物所在的加速栅格，基于地形
            rt = calcAcclerateBuilding(pMin.X, pMin.Y, pMax.X, pMax.Y, 30, maxAgxid, maxAgyid);
            if (!rt.ok)
                return rt;

            // 2019.6.11 建筑物表面栅格，基于地形
            rt = calcBuildingGrids(pMin.X, pMin.Y, pMax.X, pMax.Y, this.sideLength, maxgxid, maxgyid);
            if (!rt.ok)
                return rt;

            // 2019.6.11 地面栅格
            rt = calcGroundGrid(pMin.X, pMin.Y, maxgxid, maxgyid, this.sideLength);
            if (!rt.ok)
                return rt;
            
            return rt;
        }

        // 2018.6.11 计算建筑物所在的栅格，基于地形 
        Result calcBuildingGrids(double minX, double minY, double maxX, double maxY, double gridsize, int maxgxid, int maxgyid)
        {
            // 删除旧的建筑物网格
            IbatisHelper.ExecuteDelete("DeleteBuildingGrid", null);

            double err = GridHelper.getInstance().getGGridSize() / 2 + 1;
            DataTable tb = IbatisHelper.ExecuteQueryForDataTable("getBuildingVertexSmooth", null);

            double h = (int)GridHelper.getInstance().getGHeight();
            if (tb.Rows.Count < 1)
            {
                return new Result(false, "无建筑物顶点信息");
            }
            else
            {
                System.Data.DataTable tb1 = new System.Data.DataTable();
                tb1.Columns.Add("BuildingID");
                tb1.Columns.Add("GXID");
                tb1.Columns.Add("GYID");
                tb1.Columns.Add("GZID");

                List<BuildingVertex> tmp = new List<BuildingVertex>();

                BuildingVertex bv = new BuildingVertex();
                bv.bid = Convert.ToInt32(tb.Rows[0]["BuildingID"].ToString());
                bv.vx = Convert.ToDouble(tb.Rows[0]["VertexX"].ToString());
                bv.vy = Convert.ToDouble(tb.Rows[0]["VertexY"].ToString());
                bv.altitude = Convert.ToDouble(tb.Rows[0]["BAltitude"].ToString());  // 地形
                bv.vz = bv.altitude + Convert.ToDouble(tb.Rows[0]["Bheight"].ToString()); // 地形
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
                    bv1.altitude = Convert.ToDouble(tb.Rows[i]["BAltitude"].ToString()); // 地形
                    bv1.vz = bv1.altitude + Convert.ToDouble(tb.Rows[i]["Bheight"].ToString()); // 地形
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

                        int gzidBase = (int)(tmp[0].altitude / GridHelper.getInstance().getGHeight());  // 基于地形，海拔处的栅格高度
                        if (gzidBase < 0)
                            gzidBase = 1;
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
                                bool okPlane = false;
                                bool okEdge = false;

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
                                    // 建筑物顶面
                                    System.Data.DataRow thisrow = tb1.NewRow();
                                    thisrow["BuildingID"] = tmp[0].bid;
                                    thisrow["GXID"] = j;
                                    thisrow["GYID"] = k;
                                    thisrow["GZID"] = gzid;
                                    tb1.Rows.Add(thisrow);

                                    thisrow = tb1.NewRow();
                                    thisrow["BuildingID"] = tmp[0].bid;
                                    thisrow["GXID"] = j;
                                    thisrow["GYID"] = k;
                                    thisrow["GZID"] = 1;
                                    tb1.Rows.Add(thisrow);

                                    // 建筑物侧面
                                    if (okEdge)
                                    {
                                        for (int zid = gzidBase; zid < gzid; zid++) // 基于地形，海拔以下没有侧面栅格
                                        {
                                            thisrow = tb1.NewRow();
                                            thisrow["BuildingID"] = tmp[0].bid;
                                            thisrow["GXID"] = j;
                                            thisrow["GYID"] = k;
                                            thisrow["GZID"] = zid;
                                            tb1.Rows.Add(thisrow);
                                        }
                                    }
                                }
                                #endregion
                            }
                        }

                        lastid = bv1.bid;
                        tmp.Clear();
                    }
                    tmp.Add(bv1);

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

                // 最后一批
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

        // 2019.6.5 根据地形得到建筑物海拔
        Result calcBuildingAltitude(double minX, double minY, double maxX, double maxY,
            int minAgxid, int minAgyid, int maxAgxid, int maxAgyid,
            int mingxid, int mingyid, int maxgxid, int maxgyid)
        {
            // 读取范围内的加速栅格信息
            AccelerateStruct.setAccGridRange(minAgxid, minAgyid, maxAgxid, maxAgyid);
            AccelerateStruct.constructAccelerateStructAltitude();

            // 读取范围内的地形 TIN
            TINInfo.setBound(minX, minY, maxX, maxY);
            TINInfo.constructTINVertex();

            // 读取范围内的建筑物中心点
            BuildingGrid3D.setGGridRange(mingxid, mingyid, maxgxid, maxgyid);
            BuildingGrid3D.constructBuildingCenter();

            // 计算建筑物底面中心的海拔
            Dictionary<int, double> altitude = new Dictionary<int, double>();
            foreach (var build in BuildingGrid3D.buildingCenter)
            {
                int bid = build.Key;

                // 建筑物底面中心所在的 TIN
                Grid3D agridid = new Grid3D();
                Geometric.Point center = new Geometric.Point(BuildingGrid3D.buildingCenter[bid].X, BuildingGrid3D.buildingCenter[bid].Y, 0);
                bool ok = GridHelper.getInstance().PointXYZToAccGrid(center, ref agridid);  // 建筑物底面中心所在的均匀栅格
                if (!ok)
                {
                    altitude[bid] = 0;
                    continue;
                }
                string key = string.Format("{0},{1},{2}", agridid.gxid, agridid.gyid, agridid.gzid);
                List<int> TINs = AccelerateStruct.gridTIN[key];

                // 建筑物底面中心的海拔
                for (int i = 0; i < TINs.Count; i++)
                {
                    List<Geometric.Point> pts = TINInfo.getTINVertex(TINs[i]);

                    if (pts.Count < 3)
                        return new Result(false, "TIN 数据出错");


                    bool inTIN = Geometric.PointHeight.isInside(pts[0], pts[1], pts[2],
                        BuildingGrid3D.buildingCenter[bid].X, BuildingGrid3D.buildingCenter[bid].Y);

                    if (inTIN) // 位于当前 TIN 三角形内
                    {
                        double alt = Geometric.PointHeight.getPointHeight(pts[0], pts[1], pts[2],
                            BuildingGrid3D.buildingCenter[bid].X, BuildingGrid3D.buildingCenter[bid].Y);
                        altitude[bid] = alt;
                        break;
                    }
                }
            }

            // 更新数据库
            System.Data.DataTable tb = IbatisHelper.ExecuteQueryForDataTable("getBuilding", null);
            for (int i = 0; i < tb.Rows.Count; i++)
            {
                int bid = Convert.ToInt32(tb.Rows[i]["BuildingID"].ToString());
                if (!altitude.Keys.Contains(bid))
                    continue;
                tb.Rows[i]["BAltitude"] = altitude[bid];
            }
            IbatisHelper.ExecuteDelete("DeleteBuilding", null);  // 删除旧的
            using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))  // 写入新的
            {
                bcp.BatchSize = tb.Rows.Count;
                bcp.BulkCopyTimeout = 1000;
                bcp.DestinationTableName = "tbBuilding";
                bcp.WriteToServer(tb);

                bcp.Close();
            }
            tb.Clear();

            AccelerateStruct.clearAccelerateStruct();
            TINInfo.clear();
            BuildingGrid3D.clearBuildingData();

            return new Result(true);
        }

        // 2019.5.28 记录每个均匀栅格内有哪些地形 TIN 三角形
        Result calcTIN(double minX, double minY, double maxX, double maxY, double agridsize, int maxAgxid, int maxAgyid)
        {
            Hashtable ht = new Hashtable();
            ht["minX"] = minX;
            ht["maxX"] = maxX;
            ht["minY"] = minY;
            ht["maxY"] = maxY;

            // 从原始数据中读取区域范围内的地形 TIN 最低点
            DataTable tbHeight = IbatisHelper.ExecuteQueryForDataTable("GetMinHeight", ht);
            if (tbHeight.Rows.Count < 1)
            {
                return new Result(false, "没有 TIN 数据");
            }
            double minHeight = Convert.ToDouble(tbHeight.Rows[0][0]);

            // 从原始数据中读取区域范围内的地形 TIN，更新局部数据，以最低点高度为基准
            DataTable tb = IbatisHelper.ExecuteQueryForDataTable("GetTINVertexOriginal", ht);
            if (tb.Rows.Count < 1)
            {
                return new Result(true, "TIN 数据为空");
            }
            for (int i = 0; i < tb.Rows.Count; i++)
            {
                tb.Rows[i]["VertexHeight"] = Convert.ToDouble(tb.Rows[i]["VertexHeight"]) - minHeight;
            }
            IbatisHelper.ExecuteDelete("DeleteTIN", null);
            using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
            {
                bcp.BatchSize = tb.Rows.Count;
                bcp.BulkCopyTimeout = 1000;
                bcp.DestinationTableName = "tbTIN";
                bcp.WriteToServer(tb);
                bcp.Close();
            }

            // 将每个 TIN 记录到均匀栅格

            // 删除旧的数据
            IbatisHelper.ExecuteDelete("DeleteAccrelateTIN", null);

            // 写入数据库
            System.Data.DataTable tb1 = new System.Data.DataTable();
            tb1.Columns.Add("GXID");
            tb1.Columns.Add("GYID");
            tb1.Columns.Add("GZID");
            tb1.Columns.Add("TINID");

            HashSet<string> set = new HashSet<string>();

            for (int i = 0; i < tb.Rows.Count; i += 3)
            {
                // 得到 TIN 三角形的轴对齐包围盒
                double minTINx = double.MaxValue;
                double minTINy = double.MaxValue;
                double maxTINx = double.MinValue;
                double maxTINy = double.MinValue;
                double maxTINz = double.MinValue;
                int id = 0;

                for (int j = i; j < i + 3; j++)
                {
                    id = Convert.ToInt32(tb.Rows[j]["TINID"].ToString());
                    double x = Convert.ToDouble(tb.Rows[j]["VertexX"].ToString());
                    double y = Convert.ToDouble(tb.Rows[j]["VertexY"].ToString());
                    double z = Convert.ToDouble(tb.Rows[j]["VertexHeight"].ToString());

                    if (x < minTINx) minTINx = x;
                    if (y < minTINy) minTINy = y;
                    if (x > maxTINx) maxTINx = x;
                    if (y > maxTINy) maxTINy = y;
                    if (z > maxTINz) maxTINz = z;
                }

                int minGxid = Convert.ToInt32(Math.Ceiling((minTINx - minX) / agridsize)) - 1;
                int minGyid = Convert.ToInt32(Math.Ceiling((minTINy - minY) / agridsize)) - 1;
                int maxGxid = Convert.ToInt32(Math.Ceiling((maxTINx - minX) / agridsize)) - 1;
                int maxGyid = Convert.ToInt32(Math.Ceiling((maxTINy - minY) / agridsize)) - 1;
                int maxGzid = Convert.ToInt32(Math.Ceiling(maxTINz / agridsize)) - 1;

                // TIN 三角形跨越的均匀栅格
                bool ok = (minTINx >= minX || maxTINx <= maxX || minTINy >= minY || maxTINy <= maxY);

                if (minGxid < 0)
                {
                    if (ok)
                        minGxid = 0;
                    else
                        continue;
                }
                if (minGyid < 0)
                {
                    if (ok)
                        minGyid = 0;
                    else
                        continue;
                }
                if (maxGxid > maxAgxid)
                {
                    if (ok)
                        maxGxid = maxAgxid;
                    else
                        continue;
                }
                if (maxGyid > maxAgyid)
                {
                    if (ok)
                        maxGyid = maxAgyid;
                    else
                        continue;
                }

                if (maxGzid > 2)
                    maxGzid = 2;

                for (int j = minGxid; j <= maxGxid; j++)
                {
                    for (int k = minGyid; k <= maxGyid; k++)
                    {
                        for (int h = 0; h <= maxGzid; h++)
                        {
                            string key = string.Format("{0},{1},{2},{3}", j, k, h + 1, id);
                            if (set.Contains(key))
                                continue;
                            set.Add(key);

                            System.Data.DataRow thisrow = tb1.NewRow();
                            thisrow["GXID"] = j;
                            thisrow["GYID"] = k;
                            thisrow["GZID"] = h + 1;
                            thisrow["TINID"] = id;
                            tb1.Rows.Add(thisrow);
                        }
                    }
                }

                if (tb1.Rows.Count > 5000)
                {
                    using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                    {
                        bcp.BatchSize = tb1.Rows.Count;
                        bcp.BulkCopyTimeout = 1000;
                        bcp.DestinationTableName = "tbAccelerateGridTIN";
                        bcp.WriteToServer(tb1);
                        bcp.Close();
                        tb1.Rows.Clear();
                    }
                }
            }

            // 最后一批
            using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
            {
                bcp.BatchSize = tb1.Rows.Count;
                bcp.BulkCopyTimeout = 1000;
                bcp.DestinationTableName = "tbAccelerateGridTIN";
                bcp.WriteToServer(tb1);
                bcp.Close();
                tb1.Rows.Clear();
            }
            return new Result(true);

        }

        // 计算建筑物所在的加速网格  2019.6.13 改
        Result calcAcclerateBuilding(double minX, double minY, double maxX, double maxY, double agridsize, int maxAgxid, int maxAgyid)
        {
            DataTable tb = IbatisHelper.ExecuteQueryForDataTable("getBuildingVertexSmooth1", null);

            if (tb.Rows.Count < 1)
            {
                return new Result(false, "无建筑物顶点数据");
            }
            else
            {
                IbatisHelper.ExecuteDelete("DeleteBuildingAccrelate1", null);

                HashSet<string> set = new HashSet<string>();
                System.Data.DataTable tb1 = new System.Data.DataTable();
                tb1.Columns.Add("GXID");
                tb1.Columns.Add("GYID");
                tb1.Columns.Add("GZID");
                tb1.Columns.Add("BuildingID");

                for (int i = 0; i < tb.Rows.Count; i++)
                {
                    // 得到建筑物底面的轴对齐包围盒
                    int id = Convert.ToInt32(tb.Rows[i]["BuildingID"].ToString());
                    double height = Convert.ToDouble(tb.Rows[i]["Bheight"].ToString());
                    double altitude = Convert.ToDouble(tb.Rows[i]["BAltitude"].ToString());  // 基于地形
                    double x = Convert.ToDouble(tb.Rows[i]["minX"].ToString());
                    double y = Convert.ToDouble(tb.Rows[i]["minY"].ToString());
                    double x1 = Convert.ToDouble(tb.Rows[i]["maxX"].ToString());
                    double y1 = Convert.ToDouble(tb.Rows[i]["maxY"].ToString());

                    // 建筑物底面跨越的均匀栅格
                    int minGxid = Convert.ToInt32(Math.Ceiling((x - minX) / agridsize)) - 1;
                    int minGyid = Convert.ToInt32(Math.Ceiling((y - minY) / agridsize)) - 1;
                    int maxGxid = Convert.ToInt32(Math.Ceiling((x1 - minX) / agridsize)) - 1;
                    int maxGyid = Convert.ToInt32(Math.Ceiling((y1 - minY) / agridsize)) - 1;
                    int maxGzid = Convert.ToInt32(Math.Ceiling((height + altitude) / agridsize)) - 1;

                    bool ok = (x >= minX || x1 <= maxX || y >= minY || y1 <= maxY);

                    if (minGxid < 0)
                    {
                        if (ok)
                            minGxid = 0;
                        else
                            continue;
                    }
                    if (minGyid < 0)
                    {
                        if (ok)
                            minGyid = 0;
                        else
                            continue;
                    }
                    if (maxGxid > maxAgxid)
                    {
                        if (ok)
                            maxGxid = maxAgxid;
                        else
                            continue;
                    }
                    if (maxGyid > maxAgyid)
                    {
                        if (ok)
                            maxGyid = maxAgyid;
                        else
                            continue;
                    }

                    if (maxGzid > 2)
                        maxGzid = 2;

                    for (int j = minGxid; j <= maxGxid; j++)
                    {
                        for (int k = minGyid; k <= maxGyid; k++)
                        {
                            for (int h = 0; h <= maxGzid; h++)
                            {
                                string key = string.Format("{0},{1},{2},{3}", j, k, h + 1, id);
                                if (set.Contains(key))
                                    continue;
                                set.Add(key);

                                System.Data.DataRow thisrow = tb1.NewRow();
                                thisrow["GXID"] = j;
                                thisrow["GYID"] = k;
                                thisrow["GZID"] = h + 1;
                                thisrow["BuildingID"] = id;
                                tb1.Rows.Add(thisrow);
                            }
                        }
                    }

                    if (tb1.Rows.Count > 5000)
                    {
                        using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                        {
                            bcp.BatchSize = tb1.Rows.Count;
                            bcp.BulkCopyTimeout = 1000;
                            bcp.DestinationTableName = "tbAccelerateGridBuilding";
                            bcp.WriteToServer(tb1);
                            bcp.Close();
                            tb1.Clear();
                        }
                    }
                }

                // 写入数据库
                using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                {
                    bcp.BatchSize = tb1.Rows.Count;
                    bcp.BulkCopyTimeout = 1000;
                    bcp.DestinationTableName = "tbAccelerateGridBuilding";
                    bcp.WriteToServer(tb1);
                    bcp.Close();
                    tb1.Clear();
                }

                return new Result(true);
            }
        }

        // 地图范围
        Result calcRange(double minlng, double minlat, double maxlng, double maxlat,
                        double minX, double minY, double maxX, double maxY,
                        int maxAgxid, int maxAgyid, int maxgxid, int maxgyid, double gridlength)
        {
            Hashtable ht = new Hashtable();
            ht["id"] = 1;
            ht["AreaMinLong"] = minlng;
            ht["AreaMinLat"] = minlat;
            ht["AreaMaxLong"] = maxlng;
            ht["AreaMaxLat"] = maxlat;
            ht["GGridSize"] = Convert.ToByte(gridlength);
            ht["MaxGGXID"] = maxgxid - 1;
            ht["MaxGGYID"] = maxgyid - 1;

            // 2017.4.28 添加
            ht["AreaMinX"] = minX;
            ht["AreaMinY"] = minY;
            ht["AreaMaxX"] = maxX;
            ht["AreaMaxY"] = maxY;
            ht["GHeight"] = 3;
            ht["GBaseHeight"] = 1.5;
            ht["AGridSize"] = 30;
            ht["AGridVSize"] = 30;
            ht["MaxAGXID"] = maxAgxid - 1;
            ht["MaxAGYID"] = maxAgyid - 1;

            // 暂时不用，当截取地图中一小部分时用到
            ht["MinX"] = minX;
            ht["MinY"] = minY;
            ht["MinGGXID"] = 0;
            ht["MinGGYID"] = 0;
            ht["MinAGXID"] = 0;
            ht["MinAGYID"] = 0;
            IbatisHelper.ExecuteInsert("insertGridRange", ht);

            return new Result(true);
        }

        // 地面网格
        Result calcGroundGrid(double minX, double minY, int maxgxid, int maxgyid, double gridlength)
        {
            IbatisHelper.ExecuteDelete("DeleteGroundGrids", null);

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

            double gminX, gminY, gmaxX, gmaxY, gcX, gcY;
            gminX = minX;
            gminY = minY;

            ESRI.ArcGIS.Geometry.IPoint p1 = new ESRI.ArcGIS.Geometry.PointClass();
            ESRI.ArcGIS.Geometry.IPoint p2 = new ESRI.ArcGIS.Geometry.PointClass();
            ESRI.ArcGIS.Geometry.IPoint p3 = new ESRI.ArcGIS.Geometry.PointClass();
            p1.Z = 0;
            p2.Z = 0;
            p3.Z = 0;
            //  地面栅格
            for (int x = 0; x < maxgxid; x++)
            {
                gminY = minY;
                gmaxX = gminX + gridlength;
                gcX = (gminX + gmaxX) / 2.0;
                p1.X = gminX;
                p2.X = gmaxX;
                p3.X = gcX;

                for (int y = 0; y < maxgyid; y++)
                {
                    gmaxY = gminY + gridlength;
                    gcY = (gminY + gmaxY) / 2.0;

                    p1.X = gminX;
                    p2.X = gmaxX;
                    p3.X = gcX;
                    p1.Y = gminY;
                    p2.Y = gmaxY;
                    p3.Y = gcY;
                    PointConvert.Instance.GetGeoPoint(p1);
                    PointConvert.Instance.GetGeoPoint(p2);
                    PointConvert.Instance.GetGeoPoint(p3);

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

            return new Result(true);
        }

        class BuildingVertex
        {
            public int bid;
            public double vx, vy, vz;
            public int vid;
            public double altitude;
        }

    }
}