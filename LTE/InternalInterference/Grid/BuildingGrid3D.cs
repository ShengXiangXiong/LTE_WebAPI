using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LTE.Model;
using LTE.DB;
using System.Data;
using LTE.GIS;
using LTE.Geometric;
using System.IO;

namespace LTE.InternalInterference.Grid
{
    /// <summary>
    /// 空间网格类
    /// </summary>
    public class BuildingGrid3D
    {
        //输入数据：覆盖范围的栅格编号
        private static int minGXID = -1;
        private static int maxGXID = -1;
        private static int minGYID = -1;
        private static int maxGYID = -1;
        //空间网格加速结构
        public static Dictionary<int, List<string>> bgrid3d = new Dictionary<int, List<string>>();
        public static Dictionary<int, Point> buildingCenter = new Dictionary<int, Point>();
        public static Dictionary<int, double> buildingHeight = new Dictionary<int, double>();
        public static Dictionary<int, List<Point>> buildingVertex = new Dictionary<int, List<Point>>();
        private static Dictionary<int, List<Point>> buildingTopVertex = new Dictionary<int, List<Point>>();
        //未平滑顶点
        public static Dictionary<int, List<Point>> buildingVertexOriginal = new Dictionary<int, List<Point>>();

        //指定范围内的building ID的最大最小值
        private static int minID = Int32.MaxValue, maxID = Int32.MinValue;

        //建筑物的building ID的最大最小值，在获取所有建筑物原始顶点后可用
        private static int minBID = Int32.MaxValue, maxBID = Int32.MinValue;

        public static Dictionary<int, double> buildingAltitude = new Dictionary<int, double>();  // 2019.6.11 地形

        /// <summary>
        /// 设置空间网格二维边界
        /// </summary>
        /// <param name="mingxid"></param>
        /// <param name="mingyid"></param>
        /// <param name="maxgxid"></param>
        /// <param name="maxgyid"></param>
        public static void setGGridRange(int mingxid, int mingyid, int maxgxid, int maxgyid)
        {
            minGXID = mingxid;
            minGYID = mingyid;
            maxGXID = maxgxid;
            maxGYID = maxgyid;
        }

        /// <summary>
        /// 检查计算栅格是否在设置区域中
        /// </summary>
        /// <param name="gxid"></param>
        /// <param name="gyid"></param>
        /// <returns></returns>
        public static bool checkInRange(int gxid, int gyid)
        {
            return (gxid >= minGXID && gxid <= maxGXID && gyid >= minGYID && gyid <= maxGYID);
        }

        public static void constructGrid3D()
        {
            Hashtable para = new Hashtable();
            para["minGXID"] = minGXID;
            para["maxGXID"] = maxGXID;
            para["minGYID"] = minGYID;
            para["maxGYID"] = maxGYID;
            DataTable dt = IbatisHelper.ExecuteQueryForDataTable("GetBuildingGrid3D", para);
            for (int i = 0; i < dt.Rows.Count; i++)//按行遍历DataTable
            {
                int buildingid = Convert.ToInt32(dt.Rows[i][0].ToString());
                //gxid,gyid,gzid
                string value = dt.Rows[i][1].ToString() + "," + dt.Rows[i][2].ToString() + "," + dt.Rows[i][3].ToString();
                if (bgrid3d.ContainsKey(buildingid))
                {
                    bgrid3d[buildingid].Add(value);
                }
                else
                {
                    List<string> list = new List<string>();
                    list.Add(value);
                    bgrid3d.Add(buildingid, list);
                }
            }
        }

        /// <summary>
        /// 从数据库表tbGrid3D中取出所有符合条件的数据,并以GXID,GYID,GZID排序，组成空间网格集合
        /// </summary>
        /// <returns></returns>
        public static void constructGrid3D(ref Geometric.Point p1, ref Geometric.Point p2,
            ref Geometric.Point p3, ref Geometric.Point p4)
        {
            Hashtable para = new Hashtable();

            Grid3D gid1 = new Grid3D(), gid2 = new Grid3D(), gid3 = new Grid3D(), gid4 = new Grid3D();
            GridHelper.getInstance().PointXYZToGrid3D1(p1, ref gid1);
            GridHelper.getInstance().PointXYZToGrid3D1(p2, ref gid2);
            GridHelper.getInstance().PointXYZToGrid3D1(p3, ref gid3);
            GridHelper.getInstance().PointXYZToGrid3D1(p4, ref gid4);

            para["x1"] = gid1.gxid;
            para["x2"] = gid2.gxid;
            para["x3"] = gid3.gxid;
            para["x4"] = gid4.gxid;
            para["y1"] = gid1.gyid;
            para["y2"] = gid2.gyid;
            para["y3"] = gid3.gyid;
            para["y4"] = gid4.gyid;
            DataTable dt = IbatisHelper.ExecuteQueryForDataTable("GetBuildingGrid3D1", para);
            //Console.WriteLine(string.Format("{0} {1} {3}  {4} {5} {6} {7}", gid1.gxid, gid2.gxid, gid3.gxid, gid4.gxid, gid1.gyid, gid2.gyid, gid3.gyid, gid4.gyid);
            for (int i = 0; i < dt.Rows.Count; i++)//按行遍历DataTable
            {
                int buildingid = Convert.ToInt32(dt.Rows[i][0].ToString());
                //gxid,gyid,gzid
                string value = dt.Rows[i][1].ToString() + "," + dt.Rows[i][2].ToString() + "," + dt.Rows[i][3].ToString();
                if (bgrid3d.ContainsKey(buildingid))
                {
                    bgrid3d[buildingid].Add(value);
                }
                else
                {
                    List<string> list = new List<string>();
                    list.Add(value);
                    bgrid3d.Add(buildingid, list);
                }
            }
        }

        public static void getBuildingIDRange(out int min, out int max)
        {
            min = minID;
            max = maxID;
        }

        public static void getAllBuildingIDRange(out int min, out int max)
        {
            min = minBID;
            max = maxBID;
        }

        public static int getDataMemory()
        {
            int ret = 0;
            IDictionaryEnumerator de = bgrid3d.GetEnumerator();
            while (de.MoveNext())
            {
                ret += 4;
                List<string> s = (List<string>)de.Value;
                for (int i = 0; i < s.Count; i++)
                {
                    ret += (s[i].ToCharArray().Length) * 2;
                }
            }

            ret += buildingCenter.Count * (4 + 8 * 3 + 8);

            de = buildingVertex.GetEnumerator();
            while (de.MoveNext())
            {
                ret += 4;
                ret += ((List<Point>)de.Value).Count * (8 * 3);
            }

            return ret;
        }

        /// <summary>
        /// 建筑物中是否存在指定空间网格
        /// </summary>
        /// <param name="buildingid"></param>
        /// <param name="gxid"></param>
        /// <param name="gyid"></param>
        /// <param name="gzid"></param>
        /// <returns></returns>
        public static bool isBuildingExistGrid3D(int buildingid, int gxid, int gyid, int gzid)
        {
            string value = gxid + "," + gyid + "," + gzid;
            return bgrid3d.ContainsKey(buildingid) && bgrid3d[buildingid].Contains(value);
        }

        /// <summary>
        /// 获取建筑物海拔
        /// </summary>
        /// <param name="buildingid"></param>
        /// <returns></returns>
        public static double getBuildingAltitude(int buildingid)
        {
            return buildingAltitude.ContainsKey(buildingid) ? buildingAltitude[buildingid] : 0;
        }

        public static void constructBuildingData()
        {
            Hashtable ht = new Hashtable();
            ht["minGXID"] = minGXID;
            ht["maxGXID"] = maxGXID;
            ht["minGYID"] = minGYID;
            ht["maxGYID"] = maxGYID;

            DataTable dt = IbatisHelper.ExecuteQueryForDataTable("GetBuildingCenterPre", ht);

            int bid;
            double x, y, z, altitude;
            for (int i = 0; i < dt.Rows.Count; i++)//按行遍历DataTable
            {
                bid = Convert.ToInt32(dt.Rows[i]["BuildingID"]);
                if (bid > maxID) maxID = bid;
                if (bid < minID) minID = bid;
                x = Convert.ToDouble(dt.Rows[i]["BCenterX"]);
                y = Convert.ToDouble(dt.Rows[i]["BCenterY"]);
                z = Convert.ToDouble(dt.Rows[i]["BHeight"]);
                altitude = Convert.ToDouble(dt.Rows[i]["BAltitude"]);  // 地形
                buildingCenter.Add(bid, new Point(x, y, 0));
                buildingHeight.Add(bid, altitude + z); // 地形
                buildingAltitude.Add(bid, altitude); // 地形
            }


            dt = IbatisHelper.ExecuteQueryForDataTable("GetBuildingVertexPre", ht);
            List<Point> vcollection;
            Point t;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                bid = Convert.ToInt32(dt.Rows[i]["BuildingID"]);
                x = Convert.ToDouble(dt.Rows[i]["VertexX"]);
                y = Convert.ToDouble(dt.Rows[i]["VertexY"]);
                t = new Point(x, y, 0);

                if (buildingVertex.ContainsKey(bid))
                {
                    buildingVertex[bid].Add(t);
                }
                else
                {
                    vcollection = new List<Point>();
                    vcollection.Add(t);
                    buildingVertex.Add(bid, vcollection);
                }
            }

            dt = IbatisHelper.ExecuteQueryForDataTable("getBuildingTopVertexPre", ht);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                bid = Convert.ToInt32(dt.Rows[i]["BuildingID"]);
                x = Convert.ToDouble(dt.Rows[i]["CX"]);
                y = Convert.ToDouble(dt.Rows[i]["CY"]);
                z = buildingHeight[bid];
                t = new Point(x, y, z);

                //sw.Write(bid + ": " + c.X + " " + c.Y + "\n");

                if (buildingTopVertex.ContainsKey(bid))
                {
                    buildingTopVertex[bid].Add(t);
                }
                else
                {
                    vcollection = new List<Point>();
                    vcollection.Add(t);
                    buildingTopVertex.Add(bid, vcollection);
                }
            }
        }

        /// <summary>
        /// 求比基准高度高的建筑物遮挡距离和角度范围
        /// </summary>
        /// <param name="source"></param>
        /// <param name="buildingids"></param>
        /// <param name="baseHeight"></param>
        /// <returns></returns>
        public static Dictionary<int, TriangleBound> getShelterDisAndAngleBeam(Point source, List<int> buildingids, double baseHeight)
        {
            // key：建筑物 ID
            Dictionary<int, TriangleBound> bdis = new Dictionary<int, TriangleBound>();

            TriangleBound tb = new TriangleBound();
            double height, sz = baseHeight;
            int bid;
            for (int i = 0, cnt = buildingids.Count; i < cnt; i++)
            {
                bid = buildingids[i];
                height = buildingHeight[bid];
                if (height > sz)
                {
                    tb = new TriangleBound();
                    tb.height = height;
                    tb.baseHeight = 0;
                    tb.distance = GeometricUtilities.GetDistanceOf2DPoints(source, buildingCenter[bid]);
                    bdis[bid] = tb;
                }
            }

            List<KeyValuePair<int, TriangleBound>> bdisOrder = bdis.OrderBy(c => c.Value.distance).ToList();

            Dictionary<int, TriangleBound> ret = new Dictionary<int, TriangleBound>();

            foreach (var kv in bdisOrder)
            {
                bid = kv.Key;
                tb = bdis[bid];

                // 得到每个建筑物相对于原点的最小、最大角度
                if (getBoundPointIndex(source, buildingVertex[bid], ref tb))
                {
                    tb.buildingid = bid;
                    ret[bid] = tb;
                }
            }

            //去掉一些遮挡的
            List<TriangleBound> ret1 = new List<TriangleBound>(ret.Values);
            double min1, max1, min2, max2;
            for (int i = ret1.Count - 1; i > 0; i--)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    min1 = ret1[j].minTheta;
                    max1 = ret1[j].maxTheta;
                    min2 = ret1[i].minTheta;
                    max2 = ret1[i].maxTheta;

                    // j比i范围广，且在i前面
                    if (max2 < max1 && min2 > min1)
                    {
                        if (ret1[j].height >= ret1[i].height)  // j完全挡住了i
                        {
                            ret.Remove(ret1[i].buildingid);
                            ret1.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// 构建建筑物底面点数据和中心点以及高度数据，顶面所有点
        /// </summary>
        public static void constructBuildingData(ref Geometric.Point p1, ref Geometric.Point p2,
            ref Geometric.Point p3, ref Geometric.Point p4)
        {
            DateTime t0, t1, t2, t3;
            t0 = DateTime.Now;

            Hashtable ht = new Hashtable();

            Grid3D gid1 = new Grid3D(), gid2 = new Grid3D(), gid3 = new Grid3D(), gid4 = new Grid3D();
            GridHelper.getInstance().PointXYZToGrid3D1(p1, ref gid1);
            GridHelper.getInstance().PointXYZToGrid3D1(p2, ref gid2);
            GridHelper.getInstance().PointXYZToGrid3D1(p3, ref gid3);
            GridHelper.getInstance().PointXYZToGrid3D1(p4, ref gid4);

            ht["x1"] = gid1.gxid;
            ht["x2"] = gid2.gxid;
            ht["x3"] = gid3.gxid;
            ht["x4"] = gid4.gxid;
            ht["y1"] = gid1.gyid;
            ht["y2"] = gid2.gyid;
            ht["y3"] = gid3.gyid;
            ht["y4"] = gid4.gyid;

            DataTable dt = IbatisHelper.ExecuteQueryForDataTable("GetBuildingCenter", ht);

            int bid;
            double x, y, z, altitude;
            for (int i = 0; i < dt.Rows.Count; i++)//按行遍历DataTable
            {
                bid = Convert.ToInt32(dt.Rows[i]["BuildingID"]);
                if (bid > maxID) maxID = bid;
                if (bid < minID) minID = bid;
                x = Convert.ToDouble(dt.Rows[i]["BCenterX"]);
                y = Convert.ToDouble(dt.Rows[i]["BCenterY"]);
                z = Convert.ToDouble(dt.Rows[i]["BHeight"]);
                altitude = Convert.ToDouble(dt.Rows[i]["BAltitude"]);  // 地形
                buildingCenter.Add(bid, new Point(x, y, 0));
                buildingHeight.Add(bid, z + altitude); // 地形
                buildingAltitude.Add(bid, altitude); // 地形
            }

            t1 = DateTime.Now;

            dt = IbatisHelper.ExecuteQueryForDataTable("GetBuildingVertex", ht);
            List<Point> vcollection;
            Point t;

            //string path = @"f:\t2.txt";
            //StreamWriter sw = File.CreateText(path);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                bid = Convert.ToInt32(dt.Rows[i]["BuildingID"]);
                x = Convert.ToDouble(dt.Rows[i]["VertexX"]);
                y = Convert.ToDouble(dt.Rows[i]["VertexY"]);
                t = new Point(x, y, 0);

                if (buildingVertex.ContainsKey(bid))
                {
                    buildingVertex[bid].Add(t);
                }
                else
                {
                    vcollection = new List<Point>();
                    vcollection.Add(t);
                    buildingVertex.Add(bid, vcollection);
                }
            }
            //sw.Close();
            t2 = DateTime.Now;

            dt = IbatisHelper.ExecuteQueryForDataTable("getBuildingTopVertex", ht);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                bid = Convert.ToInt32(dt.Rows[i]["BuildingID"]);
                x = Convert.ToDouble(dt.Rows[i]["CX"]);
                y = Convert.ToDouble(dt.Rows[i]["CY"]);
                z = buildingHeight[bid];
                t = new Point(x, y, z);

                //sw.Write(bid + ": " + c.X + " " + c.Y + "\n");

                if (buildingTopVertex.ContainsKey(bid))
                {
                    buildingTopVertex[bid].Add(t);
                }
                else
                {
                    vcollection = new List<Point>();
                    vcollection.Add(t);
                    buildingTopVertex.Add(bid, vcollection);
                }
            }
            //sw.Close();
            t3 = DateTime.Now;

            Console.WriteLine(string.Format("建筑物底面中心：{0}秒", (t1 - t0).TotalMilliseconds / 1000));
            Console.WriteLine(string.Format("建筑物底面顶点：{0}秒", (t2 - t1).TotalMilliseconds / 1000));
            Console.WriteLine(string.Format("建筑物顶面顶点：{0}", (t3 - t2).TotalMilliseconds / 1000));
        }

        // 2019.6.5 为计算建筑物海拔做准备
        public static void constructBuildingCenter()
        {
            Hashtable ht = new Hashtable();
            ht["minGXID"] = minGXID;
            ht["maxGXID"] = maxGXID;
            ht["minGYID"] = minGYID;
            ht["maxGYID"] = maxGYID;

            DataTable dt = IbatisHelper.ExecuteQueryForDataTable("GetBuildingCenterPre", ht);

            int bid;
            double x, y, z;
            for (int i = 0; i < dt.Rows.Count; i++)//按行遍历DataTable
            {
                bid = Convert.ToInt32(dt.Rows[i]["BuildingID"]);
                if (bid > maxID) maxID = bid;
                if (bid < minID) minID = bid;
                x = Convert.ToDouble(dt.Rows[i]["BCenterX"]);
                y = Convert.ToDouble(dt.Rows[i]["BCenterY"]);
                z = Convert.ToDouble(dt.Rows[i]["BHeight"]);
                buildingCenter.Add(bid, new Point(x, y, z));
            }
        }
        // 2019.7.20 xsx 建筑物栅格还未划分时，根据范围和中心点得到建筑物中心点
        public static void constructBuildingCenterByArea(double minGx, double maxGx, double minGy, double maxGy)
        {
            //清除前一部分区域的数据，防止内存溢出 2019.7.22 xsx
            buildingCenter.Clear();

            Hashtable ht = new Hashtable();
            ht["minGX"] = minGx;
            ht["maxGX"] = maxGx;
            ht["minGY"] = minGy;
            ht["maxGY"] = maxGy;

            DataTable dt = IbatisHelper.ExecuteQueryForDataTable("GetBuildingCenterByArea", ht);

            int bid;
            double x, y, z;
            for (int i = 0; i < dt.Rows.Count; i++)//按行遍历DataTable
            {
                bid = Convert.ToInt32(dt.Rows[i]["BuildingID"]);
                if (bid > maxID) maxID = bid;
                if (bid < minID) minID = bid;
                x = Convert.ToDouble(dt.Rows[i]["BCenterX"]);
                y = Convert.ToDouble(dt.Rows[i]["BCenterY"]);
                z = Convert.ToDouble(dt.Rows[i]["BHeight"]);
                buildingCenter.Add(bid, new Point(x, y, z));
            }
            //return dt;
        }
        /// <summary>
        /// 平滑处理
        /// </summary>
        public static void constructBuildingVertexOriginal()
        {
            maxBID = int.MinValue;
            minBID = int.MaxValue;

            DataTable dt = IbatisHelper.ExecuteQueryForDataTable("GetBuildingVertexOriginal", null);
            List<Point> vcollection;
            Point t;

            int bid;
            double x, y;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                bid = Convert.ToInt32(dt.Rows[i]["BuildingID"]);
                if (bid > maxBID) maxBID = bid;
                if (bid < minBID) minBID = bid;

                x = Convert.ToDouble(dt.Rows[i]["VertexX"]);
                y = Convert.ToDouble(dt.Rows[i]["VertexY"]);
                t = new Point(x, y, 0);

                if (buildingVertexOriginal.ContainsKey(bid))
                {
                    buildingVertexOriginal[bid].Add(t);
                }
                else
                {
                    vcollection = new List<Point>();
                    vcollection.Add(t);
                    buildingVertexOriginal.Add(bid, vcollection);
                }
            }
        }



        /// <summary>
        /// 获取建筑物中心点(大地坐标)
        /// </summary>
        /// <param name="buildingid"></param>
        /// <returns></returns>
        public static Point getBuildingCenter(int buildingid)
        {
            return buildingCenter.ContainsKey(buildingid) ? buildingCenter[buildingid] : null;
        }

        /// <summary>
        /// 获取建筑物高度
        /// </summary>
        /// <param name="buildingid"></param>
        /// <returns></returns>
        public static double getBuildingHeight(int buildingid)
        {
            return buildingHeight.ContainsKey(buildingid) ? buildingHeight[buildingid] : -1.0;
        }

        /// <summary>
        /// 获取建筑物底面点集
        /// </summary>
        /// <param name="buildingid"></param>
        /// <returns></returns>
        public static List<Point> getBuildingVertex(int buildingid)
        {
            return buildingVertex.ContainsKey(buildingid) ? buildingVertex[buildingid] : new List<Point>();
        }

        /// <summary>
        /// 获取建筑物顶面点集
        /// </summary>
        /// <param name="buildingid"></param>
        /// <returns></returns>
        public static List<Point> getBuildingTopVertex(int buildingid)
        {
            return buildingTopVertex.ContainsKey(buildingid) ? buildingTopVertex[buildingid] : new List<Point>();
        }

        /// <summary>
        /// 获取原建筑物底面点集
        /// </summary>
        /// <param name="buildingid"></param>
        /// <returns></returns>
        public static List<Point> getBuildingVertexOriginal(int buildingid)
        {
            return buildingVertexOriginal.ContainsKey(buildingid) ? buildingVertexOriginal[buildingid] : new List<Point>();
        }

        /// <summary>
        /// 根据覆盖扇形获取建筑物id
        /// </summary>
        /// <param name="source"></param>
        /// <param name="distance"></param>
        /// <param name="fromAngle"></param>
        /// <param name="toAngle"></param>
        /// <returns></returns>
        public static List<int> getBuildingIDBySector(Point source, double distance, double fromAngle, double toAngle)
        {
            double minX = 0, minY = 0, maxX = 0, maxY = 0;
            GridHelper.getInstance().getMinXY(ref minX, ref minY);
            GridHelper.getInstance().getMaxXY(ref maxX, ref maxY);

            //边界，大地坐标
            double minx = Math.Max(minX, source.X - distance);
            double miny = Math.Max(minY, source.Y - distance);
            double maxx = Math.Min(maxX, source.X + distance);
            double maxy = Math.Min(maxY, source.Y + distance);

            distance += 0.1;

            // (450-oldAngle)%360;
            double from = GeometricUtilities.ConvertGeometricArithmeticAngle(toAngle + 1);
            double to = GeometricUtilities.ConvertGeometricArithmeticAngle(fromAngle - 1);

            //Console.WriteLine("from: {0}", from);
            //Console.WriteLine("to: {0}", to);

            from = GeometricUtilities.GetRadians(from);
            to = GeometricUtilities.GetRadians(to);

            List<int> ret = new List<int>();
            Point p;
            Polar pr;
            foreach (KeyValuePair<int, Point> kv in buildingCenter)
            {
                p = kv.Value;
                if (p.X > minx && p.X < maxx && p.Y > miny && p.Y < maxy)
                {
                    pr = GeometricUtilities.getPolarCoord(source, p);
                    if (pr.r < distance && (isInRange(pr.theta, from, to))) // || isInRange(pr.theta + Math.PI * 2, from, to)))
                    {
                        ret.Add(Convert.ToInt32(kv.Key));
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// 求比基准高度高的建筑物遮挡距离和角度范围
        /// </summary>
        /// <param name="source"></param>
        /// <param name="buildingids"></param>
        /// <param name="baseHeight"></param>
        /// <returns></returns>
        /// /// <summary>
        /// 建筑物遮挡关系（极坐标系）
        /// </summary>
        /*
        public struct TriangleBound
        {
            //建筑物id
            public int buildingid;
            public int totalVertex;
            //角度最小的边界点的索引
            public int minIndex;
            public double minTheta;
            //角度最大的边界点的索引
            public int maxIndex;
            public double maxTheta;
            //离原点较近的边是否是从minIndex到maxIndex
            public bool stob;
            //原点与建筑物的距离（近似值）
            public double distance;
            //违背遮挡高度
            public double baseHeight;
            //建筑物高度
            public double height;
        }
        */
        public static List<TriangleBound> getShelterDisAndAngle(Point source, List<int> buildingids, double baseHeight)
        {
            Dictionary<int, TriangleBound> bdis = new Dictionary<int, TriangleBound>();

            TriangleBound tb = new TriangleBound();
            double height, sz = baseHeight, altitude;
            int bid;
            for (int i = 0, cnt = buildingids.Count; i < cnt; i++)
            {
                bid = buildingids[i];
                height = buildingHeight[bid];
                altitude = buildingAltitude[bid];  // 地形
                if (height > sz)
                {
                    tb = new TriangleBound();
                    tb.height = height;
                    tb.baseHeight = altitude; // 地形
                    tb.distance = GeometricUtilities.GetDistanceOf2DPoints(source, buildingCenter[bid]);
                    bdis[bid] = tb;
                }
            }

            List<KeyValuePair<int, TriangleBound>> bdisOrder = bdis.OrderBy(c => c.Value.distance).ToList();

            List<TriangleBound> ret = new List<TriangleBound>();

            foreach (var kv in bdisOrder)
            {
                bid = kv.Key;
                tb = bdis[bid];

                // 得到每个建筑物相对于原点的最小、最大角度
                if (getBoundPointIndex(source, buildingVertex[bid], ref tb))
                {
                    tb.buildingid = bid;
                    ret.Add(tb);
                }
            }

            //去掉一些遮挡的
            double min1, max1, min2, max2;
            for (int i = ret.Count - 1; i > 0; i--)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    min1 = ret[j].minTheta;
                    max1 = ret[j].maxTheta;
                    min2 = ret[i].minTheta;
                    max2 = ret[i].maxTheta;

                    // j比i范围广，且在i前面
                    if (max2 < max1 && min2 > min1)
                    {
                        if (ret[j].height >= ret[i].height)  // j完全挡住了i
                        {
                            ret.RemoveAt(i);
                            break;
                        }
                        else if (ret[j].height > ret[i].baseHeight) // j部分遮住了i
                        {
                            TriangleBound tt = ret[i];  // 更新i的可见度 
                            tt.baseHeight = Math.Max(ret[j].height, tt.baseHeight);  // 地形
                            ret[i] = tt;
                        }
                    }
                }
            }

            return ret;
        }


        /// <summary>
        /// 相对原点的多边形边界
        /// </summary>
        /// <param name="source"></param>
        /// <param name="vertex"></param>
        /// <param name="mintheta"></param>
        /// <param name="maxtheta"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static bool getBoundPointIndex(Point source, List<Point> vertex, ref TriangleBound tb)
        {
            bool isEdge;
            int startIndex = 0;
            //原点是否在多边形内部
            if (GeometricUtilities.PointInPolygon(vertex.ToArray(), source, out isEdge, ref startIndex))
            {
                return false;
            }
            tb.maxIndex = -1;
            tb.maxTheta = -1;
            tb.minIndex = int.MaxValue - 500;
            tb.minTheta = 1000;
            tb.totalVertex = vertex.Count;
            double theta;

            Dictionary<int, Polar> tmp = new Dictionary<int, Polar>();
            int cnt = vertex.Count;
            Polar p;

            //求相对原点的极坐标，根据theta角判断边界
            for (int i = 0; i < cnt; i++)
            {
                p = GeometricUtilities.getPolarCoord(source, vertex[i]);
                theta = p.theta;
                tmp.Add(i, p);
                if (theta < tb.minTheta)
                {
                    tb.minIndex = i;
                    tb.minTheta = theta;
                }
                if (theta > tb.maxTheta)
                {
                    tb.maxIndex = i;
                    tb.maxTheta = theta;
                }
            }
            //说明多边形与x轴相交
            if (tb.maxTheta - tb.minTheta > Math.PI)
            {
                double th;
                tb.maxIndex = -1;
                tb.maxTheta = -1;
                tb.minIndex = int.MaxValue - 500;
                tb.minTheta = 1000;
                foreach (var kv in tmp)
                {
                    th = kv.Value.theta;
                    if (th < Math.PI)
                    {
                        if (th > tb.maxTheta)
                        {
                            tb.maxIndex = kv.Key;
                            tb.maxTheta = th;
                        }
                    }
                    else if (th < tb.minTheta)
                    {
                        tb.minIndex = kv.Key;
                        tb.minTheta = th;
                    }
                }
            }
            tb.stob = tmp[tb.minIndex].r > tmp[(tb.minIndex + 1) % cnt].r;
            return true;
        }

        private static bool isInRange(double angle, double from, double to)
        {
            return from < to ? (angle > from && angle < to) : (angle < to || angle > from);
        }

        /// <summary>
        /// 根据遮挡关系获取所有建筑物棱边绕射点
        /// </summary>
        /// <param name="baseHeight"></param>
        /// <param name="tbList"></param>
        /// <param name="gridLength"></param>
        /// <returns></returns>
        public static List<Point> getBuildingsEdgePointsByShelter(double baseHeight, List<TriangleBound> tbList, double gridLength)
        {
            List<Point> ret = new List<Point>();

            foreach (TriangleBound tb in tbList)
            {
                //if(tb.buildingid == 3250)
                ret.AddRange(getOneBuildingEdgePointsByShelter(baseHeight, tb, gridLength));
            }

            return ret;
        }

        /// <summary>
        /// 获取一个建筑物棱边绕射点
        /// </summary>
        /// <param name="baseHeight"></param>
        /// <param name="tb"></param>
        /// <param name="gridLength"></param>
        /// <returns></returns>
        private static List<Point> getOneBuildingEdgePointsByShelter(double baseHeight, TriangleBound tb, double gridLength)
        {
            List<Point> ret = new List<Point>();

            Point t;
            List<Point> v = buildingVertex[tb.buildingid];

            //获取垂直棱边
            for (int i = tb.minIndex, e = (tb.maxIndex + 1) % tb.totalVertex; i != e; i = (i + 1) % tb.totalVertex)
            {
                double d = gridLength / 2;
                for (double j = tb.baseHeight + d; j < tb.height - d; j += gridLength)
                {
                    t = new Point(v[i]);
                    t.Z = j;
                    ret.Add(t);
                }
            }

            //获取水平棱边
            //for (int i = tb.minIndex, e = (tb.maxIndex + 1) % tb.totalVertex; i != e; i = (i + 1) % tb.totalVertex)
            //{
            //    List<Point> tmp = VerticalPlaneGrid.GetHEdgePoints(v[i], v[(i + 1) % tb.totalVertex], gridLength, tb.height);
            //    ret.AddRange(tmp);
            //}


            if (baseHeight > tb.height)
            {
                for (int i = 0; i < tb.totalVertex - 1; i++)
                {
                    List<Point> tmp = VerticalPlaneGrid.GetHEdgePoints(v[i], v[(i + 1) % tb.totalVertex], gridLength, tb.height);
                    ret.AddRange(tmp);
                }
            }
            else
            {
                int i = tb.minIndex, end = (tb.maxIndex + 1) % tb.totalVertex;
                int j = (i - 1 + tb.totalVertex) % tb.totalVertex;
                for (; i != end; j = i, i = (i + 1) % tb.totalVertex)
                {
                    List<Point> tmp = VerticalPlaneGrid.GetHEdgePoints(v[i], v[j], gridLength, tb.height);
                    ret.AddRange(tmp);
                }
            }
            return ret;
        }

        /// <summary>
        /// 清空建筑物网格数据
        /// </summary>
        public static void clearGrid3D()
        {
            bgrid3d.Clear();
        }

        /// <summary>
        /// 清空建筑物相关数据
        /// </summary>
        public static void clearBuildingData()
        {
            buildingCenter.Clear();
            buildingVertex.Clear();
            buildingHeight.Clear();
        }

        /// <summary>
        /// 清空建筑物顶点（未平滑）数据
        /// </summary>
        public static void clearBuildingVertexOriginal()
        {
            buildingVertexOriginal.Clear();
        }

    }

    /// <summary>
    /// 建筑物遮挡关系（极坐标系）
    /// </summary>
    public struct TriangleBound
    {
        //建筑物id
        public int buildingid;
        public int totalVertex;
        //角度最小的边界点的索引
        public int minIndex;
        public double minTheta;
        //角度最大的边界点的索引
        public int maxIndex;
        public double maxTheta;
        //离远点较近的边是否是从minIndex到maxIndex
        public bool stob;
        //远点与建筑物的距离（近似值）
        public double distance;
        //被遮挡高度
        public double baseHeight;
        //建筑物高度
        public double height;
    }

}
