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
using System.Diagnostics;
using LTE.Model;
using LTE.WebAPI.Utils;

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
            //2019.07.20 方便后面计算，统一以m为单位
            pMax.X = Convert.ToInt32(Math.Ceiling(pMax.X));
            pMax.Y = Convert.ToInt32(Math.Ceiling(pMax.Y));
            pMin.X = Convert.ToInt32(Math.Ceiling(pMin.X));
            pMin.Y = Convert.ToInt32(Math.Ceiling(pMin.Y));
            double dx = Math.Abs(pMax.X - pMin.X);
            double dy = Math.Abs(pMax.Y - pMin.Y);
            //int maxgxid = Convert.ToInt32(Math.Ceiling(dx / gridlength));
            //int maxgyid = Convert.ToInt32(Math.Ceiling(dy / gridlength));
            //int maxAgxid = Convert.ToInt16(Math.Ceiling(dx / 30.0));
            //int maxAgyid = Convert.ToInt16(Math.Ceiling(dy / 30.0));

            //2019.07.20 fix bug 以左下角坐标标志栅格，原代码（如上）却在计算最大栅格id时记录的是右上，这里将最大栅格id也修正为左下
            //因为后续的分页操作是在网格id的基础上进行分页操作，所以最大栅格id的值会对后续的栅格划分有影响
            int maxgxid = Convert.ToInt32(Math.Ceiling(dx / sideLength)) - 1;
            int maxgyid = Convert.ToInt32(Math.Ceiling(dy / sideLength)) - 1;
            int maxAgxid = Convert.ToInt16(Math.Ceiling(dx / 30.0)) - 1;
            int maxAgyid = Convert.ToInt16(Math.Ceiling(dy / 30.0)) - 1;

            pMax.X = pMin.X + (maxAgxid + 1) * 30;
            pMax.Y = pMin.Y + (maxAgyid + 1) * 30;

            Result rt = new Result(true);

            // 2019.6.11 地图范围,2019.7.26增加tin数据的最高点和最低点
            rt = calcRange(minLongitude, minLatitude, maxLongitude, maxLatitude, pMin.X, pMin.Y, pMax.X, pMax.Y, maxAgxid, maxAgyid, maxgxid, maxgyid, this.sideLength);
            if (!rt.ok)
                return rt;

            //// 2019.5.28 地形和地形所在的加速栅格
            rt = calcTIN(pMin.X, pMin.Y, pMax.X, pMax.Y, 30, maxAgxid, maxAgyid);
            if (!rt.ok)
                return rt;

            //// 2019.6.5 得到建筑物海拔，基于地形
            rt = calcBuildingAltitude(pMin.X, pMin.Y, pMax.X, pMax.Y, 0, 0, maxAgxid, maxAgyid, 0, 0, maxgxid, maxgyid);
            if (!rt.ok)
                return rt;

            //// 2019.6.11 建筑物所在的加速栅格，基于地形
            rt = calcAcclerateBuilding(pMin.X, pMin.Y, pMax.X, pMax.Y, 30, maxAgxid, maxAgyid);
            if (!rt.ok)
                return rt;

            //// 2019.6.11 建筑物表面栅格，基于地形
            rt = calcBuildingGrids(pMin.X, pMin.Y, pMax.X, pMax.Y, this.sideLength, maxgxid, maxgyid);
            if (!rt.ok)
                return rt;

            //根据tin数据更新cell的altitude信息
            rt = addAltitude(maxAgxid, maxAgyid, 30,batchSize:100);
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
            int pageindex = 0;
            int pagesize = 10000;
            Hashtable ht = new Hashtable();
            ht["pageindex"] = pageindex;
            ht["pagesize"] = pagesize;
            DataTable tb = IbatisHelper.ExecuteQueryForDataTable("getBuildingVertexSmooth", ht);
            double h = (int)GridHelper.getInstance().getGHeight();

            System.Data.DataTable tb1 = new System.Data.DataTable();
            tb1.Columns.Add("BuildingID");
            tb1.Columns.Add("GXID");
            tb1.Columns.Add("GYID");
            tb1.Columns.Add("GZID");

            if (tb.Rows.Count < 1)
            {
                return new Result(false, "该范围内没有建筑物");
            }
            while (tb.Rows.Count > 0)
            {
                //System.Data.DataTable tb1 = new System.Data.DataTable();
                //tb1.Columns.Add("BuildingID");
                //tb1.Columns.Add("GXID");
                //tb1.Columns.Add("GYID");
                //tb1.Columns.Add("GZID");
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
                ht["pageindex"] = ++pageindex;
                tb = IbatisHelper.ExecuteQueryForDataTable("getBuildingVertexSmooth", ht);
            }

            return new Result(true);
        }

        // 2019.6.5 根据地形得到建筑物海拔
        Result calcBuildingAltitude(double minX, double minY, double maxX, double maxY,
            int minAxid, int minAyid, int maxAxid, int maxAyid,
            int mingxid, int mingyid, int maxgxid, int maxgyid)
        {
            //2019.07.20 分页操作，防止内存溢出
            int pageSize = 300;
            int minAgxid = minAxid;
            //int minAgyid = minAyid;
            int maxAgxid = Math.Min(maxAxid, minAxid + pageSize);
            //int maxAgyid = Math.Min(maxAyid, minAxid + pageSize);

            while (minAgxid <= maxAxid)
            {
                int minAgyid = 0;
                int maxAgyid = Math.Min(maxAyid, minAgyid + pageSize);
                while (minAgyid <= maxAyid)
                {
                    //根据均匀栅格网格id获得平面坐标范围(注意坐标代表的是左下角的id，所以求范围时，min用原始坐标id计算即可，而max就得在原始坐标的基础上+1计算才行)
                    double minGX = minX + minAgxid * 30;
                    double minGY = minY + minAgyid * 30;
                    double maxGX = minX + (maxAgxid + 1) * 30;
                    double maxGY = minY + (maxAgyid + 1) * 30;

                    // 读取范围内的加速栅格信息
                    AccelerateStruct.setAccGridRange(minAgxid, minAgyid, maxAgxid, maxAgyid);
                    //AccelerateStruct.constructAccelerateStructAltitude();
                    AccelerateStruct.constructGridTin();

                    // 读取范围内的地形 TIN
                    TINInfo.setBound(minx: minGX, miny: minGY, maxx: maxGX, maxy: maxGY);
                    TINInfo.constructTINVertex();

                    // 读取范围内的建筑物中心点
                    //BuildingGrid3D.setGGridRange(mingxid, mingyid, maxgxid, maxgyid);
                    //BuildingGrid3D.constructBuildingCenter();
                    BuildingGrid3D.constructBuildingCenterByArea(minGx: minGX, minGy: minGY, maxGx: maxGX, maxGy: maxGY);

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

                            if (TINs[i] == 173047)
                            {
                                Console.WriteLine(TINInfo.getTINVertex(TINs[i]));
                            }

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
                    //TODO: 部分更新building数据表
                    Hashtable ht = new Hashtable();
                    ht["minGX"] = minGX;
                    ht["maxGX"] = maxGX;
                    ht["minGY"] = minGY;
                    ht["maxGY"] = maxGY;
                    System.Data.DataTable tb = IbatisHelper.ExecuteQueryForDataTable("getBuildingByArea", ht);
                    //System.Data.DataTable tb = IbatisHelper.ExecuteQueryForDataTable("getBuilding", null);
                    for (int i = 0; i < tb.Rows.Count; i++)
                    {
                        int bid = Convert.ToInt32(tb.Rows[i]["BuildingID"].ToString());
                        if (!altitude.Keys.Contains(bid))
                            continue;
                        tb.Rows[i]["BAltitude"] = altitude[bid];
                    }
                    //IbatisHelper.ExecuteDelete("DeleteBuilding", null);  // 删除旧的
                    IbatisHelper.ExecuteDelete("DeleteBuildingByArea", ht);

                    using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))  // 写入新的
                    {
                        bcp.BatchSize = tb.Rows.Count;
                        bcp.BulkCopyTimeout = 1000;
                        bcp.DestinationTableName = "tbBuilding";
                        bcp.WriteToServer(tb);

                        bcp.Close();
                    }
                    tb.Clear();
                    //xsx 及时清理内存
                    AccelerateStruct.clearAccelerateStruct();
                    TINInfo.clear();
                    BuildingGrid3D.clearBuildingData();
                    minAgyid = maxAgyid + 1;
                    maxAgyid = Math.Min(maxAgyid + pageSize, maxAyid);
                }
                minAgxid = maxAgxid + 1;
                maxAgxid = Math.Min(maxAgxid + pageSize, maxAxid);
            }
            return new Result(true);
        }

        // 2019.5.28 记录每个均匀栅格内有哪些地形 TIN 三角形
        /// <summary>
        /// 2019.7.21 xsx 修改源代码逻辑，增添分页操作，按栅格面积分页，解决数据完整性问题
        /// </summary>
        /// <param name="minX"></param>
        /// <param name="minY"></param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        /// <param name="agridsize"></param>
        /// <param name="maxAxid"></param>
        /// <param name="maxAyid"></param>
        /// <returns></returns>
        Result calcTIN(double minX, double minY, double maxX, double maxY, double agridsize, int maxAxid, int maxAyid)
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

            //用于记录TIN加速栅格
            System.Data.DataTable tb1 = new System.Data.DataTable();
            tb1.Columns.Add("GXID");
            tb1.Columns.Add("GYID");
            tb1.Columns.Add("GZID");
            tb1.Columns.Add("TINID");

            //IbatisHelper.ExecuteDelete("DeleteTIN", null);
            //IbatisHelper.ExecuteDelete("DeleteAccrelateTIN", null);
            IbatisHelper.ExecuteDelete("TruncateTIN", null);
            IbatisHelper.ExecuteDelete("TruncateAccelerateGridTIN", null);

            //2019.07.24 xsx 通过sql实现tbtin数据的生成，不需要分批
            ht["minHeight"] = minHeight;
            IbatisHelper.ExecuteInsert("gennerateTin", ht);

            //2019.07.20 分页操作，防止内存泄漏
            int pageSize = 500;
            int minAgxid = 0;
            int maxAgxid = Math.Min(maxAxid, minAgxid + pageSize);
            while (minAgxid <= maxAxid)
            {
                int minAgyid = 0;
                int maxAgyid = Math.Min(maxAyid, minAgyid + pageSize);
                while (minAgyid <= maxAyid)
                {
                    //根据均匀栅格网格id获得平面坐标范围（注意坐标代表的是左下角的id，所以求范围时，min用原始坐标id计算即可，而max就得在原始坐标的基础上+1计算才行）
                    //double minX = minsX + minAgxid * 30;
                    //double minY = minsY + minAgyid * 30;
                    //double maxX = minsX + (maxAgxid + 1) * 30;
                    //double maxY = minsY + (maxAgyid + 1) * 30;
                    //ht["minX"] = minX;
                    //ht["minY"] = minY;
                    //ht["maxX"] = maxX;
                    //ht["maxY"] = maxY;

                    //int pageindex = 0;
                    //int pagesize = 10000;
                    //ht["pageindex"] = pageindex;
                    //ht["pagesize"] = pagesize;

                    // 从原始数据中读取区域范围内的地形 TIN，更新局部数据，以最低点高度为基准
                    //DataTable tb = IbatisHelper.ExecuteQueryForDataTable("GetTINVertexOriginal", ht);

                    //2019.07.25 xsx 通过矩形覆盖方式取，防止顶点在外面在内的特殊情况
                    //DataTable tb = IbatisHelper.ExecuteQueryForDataTable("GetTINVertexByArea", ht);
                    ht["minXGrid"] = minAgxid;
                    ht["maxXGrid"] = maxAgxid;
                    ht["minYGrid"] = minAgyid;
                    ht["maxYGrid"] = maxAgyid;
                    DataTable tb = IbatisHelper.ExecuteQueryForDataTable("GetTINVertexByGrid", ht);

                    if (tb.Rows.Count < 1)
                    {
                        //continue;//说明在本区域的tin数据为空
                        return new Result(false, "TIN 数据为空,该地区地形数据不完整，请重新规划范围");
                    }

                    //for (int i = 0; i < tb.Rows.Count; i++)
                    //{
                    //    tb.Rows[i]["VertexHeight"] = Convert.ToDouble(tb.Rows[i]["VertexHeight"]) - minHeight;
                    //}

                    //using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                    //{
                    //    bcp.BatchSize = tb.Rows.Count;
                    //    bcp.BulkCopyTimeout = 1000;
                    //    bcp.DestinationTableName = "tbTIN";
                    //    bcp.WriteToServer(tb);
                    //    bcp.Close();
                    //}

                    //HashSet<string> set = new HashSet<string>();
                    int id = 0;
                    //int lastid = 0;

                    for (int i = 0; i < tb.Rows.Count; i += 3)
                    {
                        // 得到 TIN 三角形的轴对齐包围盒
                        double minTINx = double.MaxValue;
                        double minTINy = double.MaxValue;
                        double maxTINx = double.MinValue;
                        double maxTINy = double.MinValue;
                        double maxTINz = double.MinValue;

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
                        ////2019.07.18 xsx 及时清理掉set中以前的数据，防止内存泄漏，因为数据是按照TINID顺序处理的，所以如果当前处理的ID和以前的ID不同的话，必然不会重复，也就是说set中只存当前TINID的数据
                        //if (id != lastid)
                        //{
                        //    set.Clear();
                        //    lastid = id;
                        //}

                        //以左下角作为网格id坐标
                        //int minGxid = Convert.ToInt32(Math.Ceiling((minTINx - minX) / agridsize)) - 1;
                        //int minGyid = Convert.ToInt32(Math.Ceiling((minTINy - minY) / agridsize)) - 1;
                        //int maxGxid = Convert.ToInt32(Math.Ceiling((maxTINx - minX) / agridsize)) - 1;
                        //int maxGyid = Convert.ToInt32(Math.Ceiling((maxTINy - minY) / agridsize)) - 1;
                        //int maxGzid = Convert.ToInt32(Math.Ceiling(maxTINz / agridsize)) - 1;

                        //2019.07.25 xsx 修改为正确的向下取整方式，当x y 恰好在边界上，海拔z为0时，前者会造成负数
                        int minGxid = (int)Math.Floor((minTINx - minX) / agridsize);
                        int minGyid = (int)Math.Floor((minTINy - minY) / agridsize);
                        int maxGxid = (int)Math.Floor((maxTINx - minX) / agridsize);
                        int maxGyid = (int)Math.Floor((maxTINy - minY) / agridsize);
                        int maxGzid = (int)Math.Floor(maxTINz / agridsize);

                        // TIN 三角形跨越的均匀栅格

                        //范围修正，舍去TIN超过给定栅格范围的数据
                        minGxid = Math.Max(minGxid, minAgxid);
                        minGyid = Math.Max(minGyid, minAgyid);
                        maxGxid = Math.Min(maxGxid, maxAgxid);
                        maxGyid = Math.Min(maxGyid, maxAgyid);

                        for (int j = minGxid; j <= maxGxid; j++)
                        {
                            for (int k = minGyid; k <= maxGyid; k++)
                            {
                                for (int h = 0; h <= maxGzid; h++)
                                {
                                    //string key = string.Format("{0},{1},{2},{3}", j, k, h + 1, id);
                                    //if (set.Contains(key))
                                    //    continue;
                                    //set.Add(key);

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
                    minAgyid = maxAgyid + 1;
                    maxAgyid = Math.Min(maxAgyid + pageSize, maxAyid);
                }
                minAgxid = maxAgxid + 1;
                maxAgxid = Math.Min(maxAgxid + pageSize, maxAxid);
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
                    int minGxid = (int)Math.Floor((x - minX) / agridsize);
                    int minGyid = (int)Math.Floor((y - minY) / agridsize);
                    int maxGxid = (int)Math.Floor((x1 - minX) / agridsize);
                    int maxGyid = (int)Math.Floor((y1 - minY) / agridsize);
                    int maxGzid = (int)Math.Floor((height + altitude) / agridsize);

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

        /// <summary>
        /// 根据划分好的加速栅格，按栅格筛选出所有在此栅格的tin数据
        /// </summary>
        /// <param name="maxAxid"></param>
        /// <param name="maxAyid"></param>
        /// <param name="agridsize"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        Result addAltitude(int maxAxid, int maxAyid, int agridsize=30, int batchSize=300)
        {
            Hashtable ht = new Hashtable();
            DataTable cells = IbatisHelper.ExecuteQueryForDataTable("getAllCellProjPos", null);
            List<CELL> res = new List<CELL>();
            List<tbAccelerateGridTIN> cellGrids = new List<tbAccelerateGridTIN>();
            Dictionary<string, List<CELL>> grid2cell = new Dictionary<string, List<CELL>>();

            foreach (DataRow cell in cells.Rows)
            {
                int gxid = 0;
                int gyid = 0;
                int gzid = 0;
                if (GridHelper.getInstance().XYZToAccGrid(Convert.ToDouble(cell["x"]),Convert.ToDouble(cell["y"]),0,ref gxid,ref gyid,ref gzid))
                {
                    string key = string.Format("{0},{1}", gxid,gyid);
                    CELL cELL = new CELL { ID=Convert.ToInt32(cell["ID"]),x = Convert.ToDecimal(cell["x"]), y = Convert.ToDecimal(cell["y"]) };
                    if (grid2cell.ContainsKey(key))
                    {
                        grid2cell[key].Add(cELL);
                    }
                    else
                    {
                        List<CELL> ls = new List<CELL>();
                        ls.Add(cELL);
                        grid2cell[key] = ls;
                        tbAccelerateGridTIN gd = new tbAccelerateGridTIN { GXID = gxid, GYID = gyid };
                        cellGrids.Add(gd);
                    }
                }
            }

            DataTable grid2Tin = IbatisHelper.ExecuteQueryForDataTable("getTinByGrids", cellGrids);
            Dictionary<string, List<Geometric.Point[]>> grid2vertx = new Dictionary<string, List<Geometric.Point[]>>();
            for (int i = 0; i < grid2Tin.Rows.Count; i += 3)
            {
                Geometric.Point[] ps = new Geometric.Point[3];
                int cnt = 0;
                string key = string.Format("{0},{1}", grid2Tin.Rows[i]["gxid"], grid2Tin.Rows[i]["gyid"]);
                object tinid = grid2Tin.Rows[i]["tinid"];
                for (int j = i; j < i + 3; j++)
                {
                    if (!tinid.Equals(grid2Tin.Rows[j]["tinid"]))
                    {
                        return new Result(false, tinid.ToString()+" tin数据不完整");
                    }
                    double x = Convert.ToDouble(grid2Tin.Rows[j]["vertexX"]);
                    double y = Convert.ToDouble(grid2Tin.Rows[j]["vertexY"]);
                    double z = Convert.ToDouble(grid2Tin.Rows[j]["vertexHeight"]);
                    ps[cnt] = new Geometric.Point(x, y, z);
                    cnt++;
                }
                if (grid2vertx.ContainsKey(key))
                {
                    grid2vertx[key].Add(ps);
                }
                else
                {
                    List<Geometric.Point[]> points = new List<Geometric.Point[]>();
                    points.Add(ps);
                    grid2vertx[key] = points;
                }
            }

            foreach (string key in grid2vertx.Keys)
            {
                List<Geometric.Point[]> points = grid2vertx[key];
                List<CELL> cELLs = grid2cell[key];
                foreach (CELL cell in cELLs)
                {
                    double x = Convert.ToDouble(cell.x);
                    double y = Convert.ToDouble(cell.y);
                    foreach (Geometric.Point[] point in points)
                    {
                        bool inTIN = Geometric.PointHeight.isInside(point[0], point[1], point[2], x, y);
                        if (inTIN)
                        {
                            double alt = Geometric.PointHeight.getPointHeight(point[0], point[1], point[2], x, y);
                            cell.Altitude = Convert.ToDecimal(alt);
                            res.Add(cell);
                        }
                    }
                }
            }
            int rows = IbatisHelper.ExecuteUpdate("CELLBatchUpdateAltitude", res);
            #region batch operate
            //int minAgxid = minGxid;
            //int maxAgxid = Math.Min(maxGxid, minGxid + batchSize);
            //while (minAgxid <= maxGxid)
            //{
            //    int minAgyid = minGyid;
            //    int maxAgyid = Math.Min(maxGyid, minAgyid + batchSize);
            //    while (minAgyid <= maxGyid)
            //    {
            //        ht["minXGrid"] = minAgxid;
            //        ht["maxXGrid"] = maxAgxid;
            //        ht["minYGrid"] = minAgyid;
            //        ht["maxYGrid"] = maxAgyid;
            //        DataTable grid2Tin = IbatisHelper.ExecuteQueryForDataTable("getGridTinvertex", ht);
            //        Dictionary<string, List<Geometric.Point[]>> grid2vertx = new Dictionary<string, List<Geometric.Point[]>>();

            //        //保证从数据库中得到的grid2Tin中的数据是有序的
            //        for (int i = 0;i< grid2Tin.Rows.Count; i += 3)
            //        {
            //            Geometric.Point[] ps = new Geometric.Point[3];
            //            int cnt = 0;
            //            string key = string.Format("{0},{1}", grid2Tin.Rows[i]["gxid"], grid2Tin.Rows[i]["gyid"]);
            //            for (int j = i; j < i + 3; j++)
            //            {
            //                double x = Convert.ToDouble(grid2Tin.Rows[i]["vertexX"]);
            //                double y = Convert.ToDouble(grid2Tin.Rows[i]["vertexY"]);
            //                ps[cnt] = new Geometric.Point(x,y);
            //                cnt++;
            //            }
            //            if (cnt < 3)
            //            {
            //                continue;
            //            }
            //            if (!grid2vertx.ContainsKey(key))
            //            {
            //                grid2vertx[key].Add(ps);
            //            }
            //            else
            //            {
            //                List<Geometric.Point[]> points = new List<Geometric.Point[]>();
            //                points.Add(ps);
            //                grid2vertx[key] = points;
            //            }
            //        }

            //        foreach(string key in grid2vertx.Keys)
            //        {
            //            List<Geometric.Point[]> points = grid2vertx[key];
            //            List<CELL> cELLs = grid2cell[key];
            //            foreach(CELL cell in cELLs)
            //            {
            //                double x = Convert.ToDouble(cell.x);
            //                double y = Convert.ToDouble(cell.y);
            //                foreach (Geometric.Point[] point in points)
            //                {
            //                    bool inTIN = Geometric.PointHeight.isInside(point[0], point[1], point[2], x, y);
            //                    if (inTIN)
            //                    {
            //                        double alt = Geometric.PointHeight.getPointHeight(point[0], point[1], point[2], x, y);
            //                        cell.Altitude = Convert.ToDecimal(alt);
            //                        res.Add(cell);
            //                    }
            //                }
            //            }
            //        }

            //        minAgyid = maxAgyid + 1;
            //        maxAgyid = Math.Min(maxAgyid + batchSize, maxGyid);
            //    }
            //    minAgxid = maxAgxid + 1;
            //    maxAgxid = Math.Min(maxAgxid + batchSize, maxGxid);
            //}
            #endregion

            return new Result(true);
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

            //增加tin数据的最高点和最低点高度值(m)，by JinHJ
            DataTable tb = IbatisHelper.ExecuteQueryForDataTable("getMaxTinHeight", null);
            double maxTinHeight = Convert.ToDouble(tb.Rows[0][0].ToString());

            tb = IbatisHelper.ExecuteQueryForDataTable("getMinTinHeight", null);
            double minTinHeight = Convert.ToDouble(tb.Rows[0][0].ToString());

            ht["MaxTinHeight"] = maxTinHeight;
            ht["MinTinHeight"] = minTinHeight;

            IbatisHelper.ExecuteInsert("insertGridRange", ht);

            return new Result(true);
        }

        // 地面网格
        Result calcGroundGrid(double minX, double minY, int maxgxid, int maxgyid, double gridlength)
        {
            //IbatisHelper.ExecuteDelete("DeleteGroundGrids", null);
            IbatisHelper.ExecuteDelete("TruncateGroundGrids");

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

            //为后面使用proj.net库转换坐标做准备,点类型更改为LTE.Geometric.Point类型,by JinHaijia
            LTE.Geometric.Point p1 = new LTE.Geometric.Point();
            LTE.Geometric.Point p2 = new LTE.Geometric.Point();
            LTE.Geometric.Point p3 = new LTE.Geometric.Point();

            //旧版使用arcgis接口转换坐标
            //ESRI.ArcGIS.Geometry.IPoint p1 = new ESRI.ArcGIS.Geometry.PointClass();
            //ESRI.ArcGIS.Geometry.IPoint p2 = new ESRI.ArcGIS.Geometry.PointClass();
            //ESRI.ArcGIS.Geometry.IPoint p3 = new ESRI.ArcGIS.Geometry.PointClass();

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
                    //PointConvert.Instance.GetGeoPoint(p1);
                    //PointConvert.Instance.GetGeoPoint(p2);
                    //PointConvert.Instance.GetGeoPoint(p3);
                    p1 = PointConvertByProj.Instance.GetGeoPoint(p1);
                    p2 = PointConvertByProj.Instance.GetGeoPoint(p2);
                    p3 = PointConvertByProj.Instance.GetGeoPoint(p3);


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
                if (dtable.Rows.Count > 5000)
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

            System.Diagnostics.Debug.WriteLine("地面栅格划分结束时间:"+DateTime.Now.ToString());

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