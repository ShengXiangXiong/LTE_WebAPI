using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LTE.Geometric;
using LTE.DB;
using System.Collections;

namespace LTE.InternalInterference.Grid
{
    /// <summary>
    /// 大地坐标转化为加速网格类
    /// </summary>
    public class GridHelper
    {
        private static double minlong = -1.0;
        private static double minlat = -1.0;
        private static double maxlong = -1.0;
        private static double maxlat = -1.0;
        private static double oX = -1.0;
        private static double oY = -1.0;
        private static double minX = -1.0;
        private static double minY = -1.0;
        private static double maxX = -1.0;
        private static double maxY = -1.0;
        /// <summary>
        /// 地面网格边长
        /// </summary>
        private static int ggridsize = -1;//单位米
        /// <summary>
        /// 立体网格的高度
        /// </summary>
        private static double gheight = -1.0;
        /// <summary>
        /// 立体网格面相对当前层的高度
        /// </summary>
        private static double gbaseheight = -1.0;
        /// <summary>
        /// 地面网格最大xyID
        /// </summary>
        private static int MaxGGXID = -1;
        private static int MaxGGYID = -1;
        private static int MinGGXID = -1;
        private static int MinGGYID = -1;
        /// <summary>
        /// 加速网格边长
        /// </summary>
        private static int agridsize = -1;//单位米
        /// <summary>
        /// 加速网格高度
        /// </summary>
        private static int agridvsize = -1;//单位米
        /// <summary>
        /// 加速网格最大xyID
        /// </summary>
        private static int MaxAGXID = -1;
        private static int MaxAGYID = -1;
        private static int MinAGXID = -1;
        private static int MinAGYID = -1;

        private static GridHelper instance = null;
        private static object syncRoot = new object();

        public static GridHelper getInstance()
        {
            if (instance == null)
            {
                lock (syncRoot)
                {
                    if (instance == null)
                    {
                        instance = new GridHelper();
                        Init();
                    }
                }
            }
            return instance;
        }

        /// <summary>
        /// 初始化网格边界条件
        /// </summary>
        /// <returns></returns>
        private static bool Init()
        {
            DataTable area = new DataTable();
            Hashtable ht = new Hashtable();
            ht["id"] = 1;  // 1表示用的是全局地图，2表示用的是局部地图
            area = IbatisHelper.ExecuteQueryForDataTable("getGridRange", ht);
             
            if (area.Rows.Count < 1)
            {
                return false;
            }
            else
            {
                minlong = Convert.ToDouble(area.Rows[0]["AreaMinLong"].ToString());
                minlat = Convert.ToDouble(area.Rows[0]["AreaMinLat"].ToString());
                maxlong = Convert.ToDouble(area.Rows[0]["AreaMaxLong"].ToString());
                maxlat = Convert.ToDouble(area.Rows[0]["AreaMaxLat"].ToString());
                oX = Convert.ToDouble(area.Rows[0]["AreaMinX"].ToString());
                oY = Convert.ToDouble(area.Rows[0]["AreaMinY"].ToString());
                maxX = Convert.ToDouble(area.Rows[0]["AreaMaxX"].ToString());
                maxY = Convert.ToDouble(area.Rows[0]["AreaMaxY"].ToString());
                ggridsize = Convert.ToInt32(area.Rows[0]["GGridSize"].ToString());
                MaxGGXID = Convert.ToInt32(area.Rows[0]["MaxGGXID"].ToString());
                MaxGGYID = Convert.ToInt32(area.Rows[0]["MaxGGYID"].ToString());
                gheight = Convert.ToDouble(area.Rows[0]["GHeight"].ToString());
                gbaseheight = Convert.ToDouble(area.Rows[0]["GBaseHeight"].ToString());
                agridsize = Convert.ToInt32(area.Rows[0]["AGridSize"].ToString());
                agridvsize = Convert.ToInt32(area.Rows[0]["AGridVSize"].ToString());
                MaxAGXID = Convert.ToInt32(area.Rows[0]["MaxAGXID"].ToString());
                MaxAGYID = Convert.ToInt32(area.Rows[0]["MaxAGYID"].ToString());
                minX = Convert.ToDouble(area.Rows[0]["MinX"].ToString());
                minY = Convert.ToDouble(area.Rows[0]["MinY"].ToString());
                MinGGXID = Convert.ToInt32(area.Rows[0]["MinGGXID"].ToString());
                MinGGYID = Convert.ToInt32(area.Rows[0]["MinGGYID"].ToString());
                MinAGXID = Convert.ToInt32(area.Rows[0]["MinAGXID"].ToString());
                MinAGYID = Convert.ToInt32(area.Rows[0]["MinAGYID"].ToString());
            }
            return true;
        }

        /// <summary>
        /// 判断点(经纬度坐标)是否在加速网格内
        /// </summary>
        /// <param name="lng"></param>
        /// <param name="lat"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public bool checkLLHInGrid(double lng, double lat, double height)
        {
            return !(lng < minlong || lng > maxlong || lat < minlat || lat > maxlat || height < 0);
        }

        /// <summary>
        /// 判断点(大地坐标)是否在加速网格内
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public bool checkXYZInGrid(double x, double y, double z)
        {
            return !(x < minX || x > maxX || y < minY || y > maxY || z < 0);
        }

        /// <summary>
        /// 判断点(大地坐标)是否在加速网格内
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public bool checkPointXYZInGrid(Point p)
        {
            return this.checkXYZInGrid(p.X, p.Y, p.Z);
        }

        /// <summary>
        /// 判断点(经纬度)是否在加速网格内
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        //public bool checkPointLLHInGrid(Point p)
        //{
        //    return this.checkLLHInGrid(p.Lng, p.Lat, p.Z);
        //}

        /// <summary>
        /// 获取网格原点坐标(经纬度)，亦即建筑物左下角坐标
        /// </summary>
        /// <param name="lng"></param>
        /// <param name="lat"></param>
        /// <returns></returns>
        public void getOriginLngLat(ref double lng, ref double lat)
        {
            lng = minlong;
            lat = minlat;
        }

        /// <summary>
        /// 获取网格原点坐标(大地坐标)，亦即建筑物左下角坐标
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public void getOriginXY(ref double x, ref double y)
        {
            x = oX;
            y = oY;
        }

        /// <summary>
        /// 获取区域网格最小坐标
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public void getMinXY(ref double x, ref double y)
        {
            x = minX;
            y = minY;
        }

        /// <summary>
        /// 获取区域网格最大坐标
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public void getMaxXY(ref double x, ref double y)
        {
            x = maxX;
            y = maxY;
        }

        /// <summary>
        /// 获取立体网格的边长
        /// </summary>
        /// <returns></returns>
        public int getGGridSize()
        {
            return ggridsize;
        }

        /// <summary>
        /// 获取立体网格的高度
        /// </summary>
        /// <returns></returns>
        public double getGHeight()
        {
            return gheight;
        }

        /// <summary>
        /// 获取立体网格初始高度
        /// </summary>
        /// <returns></returns>
        public double getGBaseHeight()
        {
            return gbaseheight;
        }

        /// <summary>
        /// 获取加速网格底面边长
        /// </summary>
        /// <returns></returns>
        public int getAGridSize()
        {
            return agridsize;
        }

        /// <summary>
        /// 获取加速网格的高度
        /// </summary>
        /// <returns></returns>
        public int getAGridVSize()
        {
            return agridvsize;
        }

        /// <summary>
        /// 获取地面网格xy最大值
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public void getMaxGGridXY(ref int x, ref int y)
        {
            x = MaxGGXID;
            y = MaxGGYID;
        }

        /// <summary>
        /// 获取地面网格xy最小值
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public void getMinGGridXY(ref int x, ref int y)
        {
            x = MinGGXID;
            y = MinGGYID;
        }

        /// <summary>
        /// 获取加速网格xy最小值
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public void getMinAccGridXY(ref int x, ref int y)
        {
            x = MinAGXID;
            y = MinAGYID;
        }

        /// <summary>
        /// 获取加速网格xy最大值
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public void getMaxAccGridXY(ref int x, ref int y)
        {
            x = MaxAGXID;
            y = MaxAGYID;
        }

        /// <summary>
        /// 返回经纬度所在的地面网格，以区域左下角为原点
        /// </summary>
        /// <param name="lng"></param>
        /// <param name="lat"></param>
        /// <param name="gxid"></param>
        /// <param name="gyid"></param>
        /// <returns></returns>
        public bool LngLatToGGrid(double lng, double lat, ref int gxid, ref int gyid)
        {
            if (checkLLHInGrid(lng, lat, 0))
            {
                //同划分网格一致
                double dy = CJWDHelper.distance(lng, minlat, lng, lat);
                double dx = CJWDHelper.distance(minlong, lat, lng, lat);
                gxid = (int)(Math.Floor(dx * 1000.0 / ggridsize));
                gyid = (int)(Math.Floor(dy * 1000.0 / ggridsize));
                return true;
            }
            return false;
        }

        /// <summary>
        /// 返回大地坐标点所在的地面网格，以区域左下角为原点
        /// </summary>
        /// <param name="lng"></param>
        /// <param name="lat"></param>
        /// <param name="gxid"></param>
        /// <param name="gyid"></param>
        /// <returns></returns>
        public bool XYToGGrid(double x, double y, ref int gxid, ref int gyid)
        {
            if (checkXYZInGrid(x, y, 0))
            {
                //同划分网格一致
                double dy = y - oY;
                double dx = x - oX;
                gxid = (int)Math.Floor(dx / ggridsize);
                gyid = (int)Math.Floor(dy / ggridsize);

                return true;
            }
            return false;
        }



        /// <summary>
        /// 返回大地坐标点所在的地面网格，以区域左下角为原点
        /// </summary>
        /// <param name="lng"></param>
        /// <param name="lat"></param>
        /// <param name="gxid"></param>
        /// <param name="gyid"></param>
        /// <returns></returns>
        public bool XYZToGGrid(Point p, ref Grid3D grid3D)
        {
            if (checkXYZInGrid(p.X, p.Y, 0))
            {
                //同划分网格一致
                double dy = p.Y - oY;
                double dx = p.X - oX;
                grid3D.gxid = (int)Math.Floor(dx / ggridsize);
                grid3D.gyid = (int)Math.Floor(dy / ggridsize);
                grid3D.gzid = (int)Math.Floor(p.Z / gheight);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 返回空间点(经纬度)所在的加速网格坐标
        /// </summary>
        /// <param name="lng"></param>
        /// <param name="lat"></param>
        /// <param name="height">单位米</param>
        /// <param name="gxid"></param>
        /// <param name="gyid"></param>
        /// <param name="gzid">1,2,3</param>
        /// <returns></returns>
        public bool LngLatHeightToAccGrid(double lng, double lat, double height, ref int gxid, ref int gyid, ref int gzid)
        {
            bool ret;
            if (ret = checkLLHInGrid(lng, lat, height))
            {
                //同划分网格的思路
                double dy = CJWDHelper.distance(lng, minlat, lng, lat);
                double dx = CJWDHelper.distance(minlong, lat, lng, lat);
                gxid = (int)Math.Floor(dx * 1000.0 / agridsize);
                gyid = (int)Math.Floor(dy * 1000.0 / agridsize);
                height = Math.Round(height, 3);
                gzid = (height <= agridvsize ? 1 : (height <= (agridvsize << 1) ? 2 : 3));
                //gzid = (int)Math.Ceiling(height / (double)agridvsize);
                //gzid = gzid < 4 ? gzid : 3 ;
            }
            return ret;
        }

        /// 返回空间点(大地坐标)所在的加速网格坐标
        public bool XYZToAccGrid(double x, double y, double z, ref int gxid, ref int gyid, ref int gzid)
        {
            bool ret = true;
            if (ret = checkXYZInGrid(x, y, z))
            {
                //同划分网格的思路
                double dy = y - oY;
                double dx = x - oX;
                gxid = (int)(Math.Floor(dx / agridsize));
                gyid = (int)(Math.Floor(dy / agridsize));
                z = Math.Round(z, 3);
                gzid = (z <= agridvsize ? 1 : (z <= (agridvsize << 1) ? 2 : 3));
            }
            return ret;
        }

        /// <summary>
        /// 返回加速网格坐标所在的空间中心点坐标
        /// </summary>
        /// <param name="lng"></param>
        /// <param name="lat"></param>
        /// <param name="height">单位米</param>
        /// <param name="gxid"></param>
        /// <param name="gyid"></param>
        /// <param name="gzid">1,2,3</param>
        /// <returns></returns>
        public void AccGridToXYZ(int gxid, int gyid, int gzid, ref double x, ref double y, ref double z)
        {
            double half = 0.5 * agridsize;
            x = oX + gxid * agridsize + half;
            y = oY + gyid * agridsize + half;
            z = 0 + gzid * agridsize + half;
        }

        /// <summary>
        /// 返回网格坐标所在的空间中心点坐标
        /// </summary>
        /// <param name="lng"></param>
        /// <param name="lat"></param>
        /// <param name="height">单位米</param>
        /// <param name="gxid"></param>
        /// <param name="gyid"></param>
        /// <param name="gzid">1,2,3</param>
        /// <returns></returns>
        public void GridToXYZ(int gxid, int gyid, int gzid, ref double x, ref double y, ref double z)
        {
            double half = 0.5 * ggridsize;
            x = oX + gxid * ggridsize + half;
            y = oY + gyid * ggridsize + half;
            z = 0 + (gzid - 1) * ggridsize + half;
        }
        /// <summary>
        /// 返回网格中心点对应的地理坐标
        /// </summary>
        /// <param name="gxid"></param>
        /// <param name="gyid"></param>
        /// <param name="gzid"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public Point GridToGeo(int gxid, int gyid)
        {
            double half = 0.5 * ggridsize;
            double x = oX + gxid * ggridsize + half;
            double y = oY + gyid * ggridsize + half;
            LTE.Geometric.Point p = new LTE.Geometric.Point();
            p.X = x;
            p.Y = y;
            return PointConvertByProj.Instance.GetGeoPoint(p);
        }
        /// <summary>
        /// 返回5*5网格左下角对应的地理坐标
        /// </summary>
        /// <param name="gxid"></param>
        /// <param name="gyid"></param>
        /// <param name="gzid"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public Point GridToLeftDownGeo(int gxid, int gyid)
        {
            double x = oX + gxid * ggridsize;
            double y = oY + gyid * ggridsize;
            LTE.Geometric.Point p = new LTE.Geometric.Point();
            p.X = x;
            p.Y = y;
            return PointConvertByProj.Instance.GetGeoPoint(p);
        }
        /// <summary>
        /// 返回5*5网格右上角对应的地理坐标
        /// </summary>
        /// <param name="gxid"></param>
        /// <param name="gyid"></param>
        /// <param name="gzid"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public Point GridToRightUpGeo(int gxid, int gyid)
        {
            double x = oX + gxid * (ggridsize + 1);
            double y = oY + gyid * (ggridsize + 1);
            LTE.Geometric.Point p = new LTE.Geometric.Point();
            p.X = x;
            p.Y = y;
            return PointConvertByProj.Instance.GetGeoPoint(p);
        }

        /// <summary>
        /// 返回空间点所在的加速网格坐标
        /// </summary>
        /// <param name="p"></param>
        /// <param name="gxid"></param>
        /// <param name="gyid"></param>
        /// <param name="gzid"></param>
        /// <returns></returns>
        //public bool PointLLHToAccGrid(Point p, ref Grid3D grid)
        //{
        //    return LngLatHeightToAccGrid(p.Lng, p.Lat, p.Z, ref grid.gxid, ref grid.gyid, ref grid.gzid);
        //}

        /// <summary>
        /// 返回空间点(大地坐标)所在的加速网格坐标
        /// </summary>
        /// <param name="p"></param>
        /// <param name="gxid"></param>
        /// <param name="gyid"></param>
        /// <param name="gzid"></param>
        /// <returns></returns>
        public bool PointXYZToAccGrid(Point p, ref Grid3D grid)
        {
            return XYZToAccGrid(p.X, p.Y, p.Z, ref grid.gxid, ref grid.gyid, ref grid.gzid);
        }

        /*
        /// <summary>
        /// 返回点(经纬度坐标)在加速栅格中距离左下角的距离坐标，单位米
        /// </summary>
        /// <param name="p"></param>
        /// <param name="ingrid"></param>
        /// <returns></returns>
        public bool PointLLHInAccGrid(Point p, ref Point ingrid)
        {
            bool ret;
            Grid3D grid = new Grid3D();
            if (ret = PointLLHToAccGrid(p, ref grid))
            {
                ingrid.X = CJWDHelper.distance(minlong, p.Lat, p.Lng, p.Lat) * 1000.0 - agridsize * grid.gxid;
                ingrid.Y = CJWDHelper.distance(p.Lng, minlat, p.Lng, p.Lat) * 1000.0 - agridsize * grid.gyid;
                ingrid.Z = p.Z - agridvsize * (grid.gzid - 1);
            }
            //Console.WriteLine(ret);
            return ret;
        }
        */

        /// <summary>
        /// 返回点(大地坐标)在加速栅格中距离左下角的距离坐标，单位米
        /// </summary>
        /// <param name="p"></param>
        /// <param name="ingrid"></param>
        /// <returns></returns>
        public bool PointXYZInAccGrid(Point p, ref Point ingrid)
        {
            bool ret;
            Grid3D grid = new Grid3D();
            if (ret = PointXYZToAccGrid(p, ref grid))
            {
                ingrid.X = p.X - oX - agridsize * grid.gxid;
                ingrid.Y = p.Y - oY - agridsize * grid.gyid;
                ingrid.Z = p.Z - agridvsize * (grid.gzid - 1);
            }
            return ret;
        }

        /*
        /// <summary>
        /// 空间点（经纬度）所在的立体网格
        /// </summary>
        /// <param name="p"></param>
        /// <param name="grid"></param>
        /// <returns></returns>
        public bool PointLLHToGrid3D(Point p, ref Grid3D grid)
        {
            bool ret = p.Z > 0.0 && LngLatToGGrid(p.Lng, p.Lat, ref grid.gxid, ref grid.gyid);
            if (ret)
            {
                grid.gzid = (int)Math.Ceiling(p.Z / gheight);
            }
            return ret;
        }
        */

        /// <summary>
        /// 空间点（大地坐标）所在的立体网格
        /// </summary>
        /// <param name="p"></param>
        /// <param name="grid"></param>
        /// <returns></returns>
        public bool PointXYZToGrid3D(Point p, ref Grid3D grid)
        {
            bool ret = p.Z > 0.0 && XYToGGrid(p.X, p.Y, ref grid.gxid, ref grid.gyid);
            if (ret)
            {
                grid.gzid = (int)Math.Ceiling(p.Z / gheight);
            }
            return ret;
        }


        public bool XYToGGrid1(double x, double y, ref int gxid, ref int gyid)
        {
            if (checkXYZInGrid(x, y, 0))
            {
                //同划分网格一致
                double dy = y - oY;
                double dx = x - oX;
                gxid = (int)(Math.Floor(dx / ggridsize));
                gyid = (int)(Math.Floor(dy / ggridsize));
                return true;
            }
            return false;
        }
        /// <summary>
        /// 空间点（大地坐标）所在的立体网格
        /// </summary>
        public void PointXYZToGrid3D1(Point p, ref Grid3D grid)
        {
            double dy = p.Y - oY;
            double dx = p.X - oX;
            grid.gxid = (int)(Math.Floor(dx / ggridsize));
            grid.gyid = (int)(Math.Floor(dy / ggridsize));
        }


        /*
        /// <summary>
        /// 空间点（经纬度）在立体网格内部距离左下角距离坐标，单位米
        /// </summary>
        /// <param name="p"></param>
        /// <param name="ingrid"></param>
        /// <returns></returns>
        public bool PointLLHInGrid3D(Point p, ref Point ingrid)
        {
            Grid3D grid = new Grid3D();
            bool ret = PointLLHToGrid3D(p, ref grid);
            if (ret)
            {

                ingrid.X = CJWDHelper.distance(minlong, p.Lat, p.Lng, p.Lat) * 1000.0 - ggridsize * grid.gxid;
                ingrid.Y = CJWDHelper.distance(p.Lng, minlat, p.Lng, p.Lat) * 1000.0 - ggridsize * grid.gyid;
                ingrid.Z = p.Z - gheight * (grid.gzid - 1);
            }
            return ret;
        }
        */

        /// <summary>
        /// 空间点（大地坐标）在立体网格内部距离左下角距离坐标，单位米
        /// </summary>
        /// <param name="p"></param>
        /// <param name="ingrid"></param>
        /// <returns></returns>
        public bool PointXYZInGrid3D(Point p, ref Point ingrid)
        {
            Grid3D grid = new Grid3D();
            bool ret = PointXYZToGrid3D(p, ref grid);
            if (ret)
            {
                ingrid.X = p.X - oX - ggridsize * grid.gxid;
                ingrid.Y = p.Y - oY - ggridsize * grid.gyid;
                ingrid.Z = p.Z - gheight * (grid.gzid - 1);
            }
            return ret;
        }

        public bool PointXYZGrid(Point p, ref Grid3D grid, int gridxy, int gridz)
        {

            bool ret = p.Z >= 0.0 && checkXYZInGrid(p.X, p.Y, 0);

            if (ret)
            {
                //同划分网格一致
                double dy = p.Y - oY;
                double dx = p.X - oX;
                grid.gxid = dx == 0 ? 0 : Convert.ToInt32(Math.Ceiling(dx / gridxy)) - 1;
                grid.gyid = dy == 0 ? 0 : Convert.ToInt32(Math.Ceiling(dy / gridxy)) - 1;
                grid.gzid = p.Z == 0 ? 0 : Convert.ToInt32(Math.Ceiling(p.Z / gridz)) - 1;
                
            }

            return ret;
        }

        /// <summary>
        /// 2019/3/1
        /// 将加速栅格转为地理坐标
        /// </summary>
        /// <param name="p"></param>
        /// <param name="grid"></param>
        /// <param name="gridxy"></param>
        /// <param name="gridz"></param>
        /// <returns></returns>
        public void PointGridXYZ(ref Point p, Grid3D grid, int agridsize, int agridVsize)
        {

            double half = 0.5 * agridsize;
            p.X = oX + grid.gxid * agridsize + half;
            p.Y = oY + grid.gyid * agridsize + half;
            p.Z = (grid.gzid + 0.5) * agridVsize;

        }
    }

    /// <summary>
    /// 加速网格/立体覆盖网格
    /// </summary>
    public class Grid3D
    {
        //取值0,1,2...
        public int gxid;
        public int gyid;
        //取值1,2,3...
        public int gzid;

        public Grid3D()
        {
            gxid = 0;
            gyid = 0;
        }

        public Grid3D(Grid3D g)
        {
            this.gxid = g.gxid;
            this.gyid = g.gyid;
            this.gzid = g.gzid;
        }
        public Grid3D(int xid, int yid, int zid)
        {
            this.gxid = xid;
            this.gyid = yid;
            this.gzid = zid;
        }
        public bool Equals(Grid3D g)
        {
            return g != null && this.gxid == g.gxid && this.gyid == g.gyid && this.gzid == g.gzid;
        }

    }

}
