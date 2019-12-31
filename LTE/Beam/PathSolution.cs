using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/* Copyright (c) 2004-2005, Samuli Laine */
// Copyright (c) 2018-2019, 尹静萍 
// 文献：Samuli Laine, Samuel Siltanen, Tapio Lokki, Lauri Savioja. Accelerated beam tracing algorithm[J]. Applied Acoustics, 2009, 70(1): 172-181.
namespace LTE.Beam
{
    public class SolutionNode
    {
        public int m_parent;
        public Polygon m_polygon;
        public Polygon m_clipedPolygon;

        public SolutionNode()
        {
        }
    };

    public class Path
    {
        public int m_order;
        public List<Vector3> m_points;
        public List<Polygon> m_polygons;

        public Path()
        {
            m_points = new List<Vector3>();
            m_polygons = new List<Polygon>();
        }

        public Path(ref Path p)
        {
            m_order = p.m_order;
            m_points = new List<Vector3>(p.m_points);
            m_polygons = new List<Polygon>(p.m_polygons);
        }
    };

    // 2018.12.04
    public class Rays
    {
        public List<NodeInfo> m_rays;
        public double emitPwrDbm;  // 2018.12.11
        public double recvPwrDbm;  // 2018.12.11

        public Rays()
        {
            m_rays = new List<NodeInfo>();
        }

        public Rays(int cnt)
        {
            m_rays = new List<NodeInfo>(new NodeInfo[cnt]);
        }
    }

    public class PathSolution
    {
        private Room m_room;
        public Point m_source;
        public Point m_listener;
        private int m_maximumOrder;

        private List<Polygon> m_polygonCache;
        private List<Vector3> m_validateCache;
        private Dictionary<float, List<int>> m_pathFirstSet;

        public List<SolutionNode> m_solutionNodes;
        private List<Vector4> m_failPlanes;
        private List<Vector4> m_distanceSkipCache;
        private Vector3 m_cachedSource;

        private List<Path> m_paths;

        public int numPaths() { return m_paths.Count; }
        public Path getPath(int i) { return m_paths[i]; }

        private List<NodeInfo> m_raysCache;  // 2018.12.04
        public List<Rays> m_rays; // 2018.12.04
        private int m_level; // 2018.12.05
        private int m_node;  // 2018.12.05
        private LTE.InternalInterference.CalcGridStrength calcStrength;  // 计算场强函数  2018.12.11
        private List<int> m_leaves;  // 2018.12.12  beam 树叶子节点索引 
        private Random rand;  // 2018.12.13
        public int rayCount;  // 2019.1.12
        //------------------------------------------------------------------------

        float EPS_SIMILAR_PATHS = 5;
        float EPS_DEGENERATE_POLYGON_AREA = 1;
        int DISTANCE_SKIP_BUCKET_SIZE = 16;

        //------------------------------------------------------------------------

        //------------------------------------------------------------------------

        public PathSolution() { }

        public PathSolution(ref Room room,
                           ref Point source,
                           ref Point listener,
                           int maximumOrder)
        {
            m_room = room;
            m_source = source;
            m_listener = listener;
            m_maximumOrder = maximumOrder;

            m_pathFirstSet = new Dictionary<float, List<int>>();
            m_polygonCache = new List<Polygon>(new Polygon[maximumOrder]);
            m_validateCache = new List<Vector3>(new Vector3[maximumOrder * 2]);
            m_paths = new List<Path>();
            //m_paths1 = new List<Path1>();  // 2018.12.11
            m_solutionNodes = new List<SolutionNode>();
            m_failPlanes = new List<Vector4>();
            m_distanceSkipCache = new List<Vector4>();
            m_cachedSource = new Vector3();
            m_rays = new List<Rays>();  // 2018.12.04
            m_raysCache = new List<NodeInfo>(new NodeInfo[maximumOrder + 1]);  // 2018.12.04
            m_level = 0; // 2018.12.05
            m_node = 0; // 2018.12.05
            m_leaves = new List<int>(); // 2018.12.12
            rayCount = 0;
        }

        public PathSolution(ref Room room, int maximumOrder, ref LTE.InternalInterference.CellInfo sourceInfo)
        {
            m_room = room;
            m_maximumOrder = maximumOrder;
            this.calcStrength = new LTE.InternalInterference.CalcGridStrength(sourceInfo, null); ;  // 2018.12.11

            m_pathFirstSet = new Dictionary<float, List<int>>();
            m_polygonCache = new List<Polygon>(new Polygon[maximumOrder]);
            m_validateCache = new List<Vector3>(new Vector3[maximumOrder * 2]);
            m_paths = new List<Path>();
            m_solutionNodes = new List<SolutionNode>();
            m_failPlanes = new List<Vector4>();
            m_distanceSkipCache = new List<Vector4>();
            m_cachedSource = new Vector3();
            m_rays = new List<Rays>();  // 2018.12.04
            m_raysCache = new List<NodeInfo>(new NodeInfo[maximumOrder + 1]);  // 2018.12.04
            m_level = 0; // 2018.12.05
            m_node = 0; // 2018.12.05
            m_leaves = new List<int>(); // 2018.12.12
            rand = new Random();  // 2018.12.13
            rayCount = 0;
        }

        //------------------------------------------------------------------------

        public void clearCache()
        {
            m_solutionNodes.Clear();
            m_failPlanes.Clear();
        }

        //------------------------------------------------------------------------
        public void beamTracingPath()
        {
            Vector3 source = m_source.getPosition();
            Vector3 target = m_listener.getPosition();

            m_paths.Clear();

            if (m_solutionNodes.Count == 0 || m_cachedSource.x != source.x || m_cachedSource.y != source.y || m_cachedSource.z != source.z)  // target 改变后不影响
            {
                clearCache();

                SolutionNode root = new SolutionNode();  // beam 根节点
                root.m_polygon = null;
                root.m_parent = -1;
                m_solutionNodes.Add(root);  // 记录障碍物面及父 beam id

                DateTime t0 = DateTime.Now;
                Beam beam = new Beam();

                m_cachedSource.x = source.x;
                m_cachedSource.y = source.y;
                m_cachedSource.z = source.z;

                Path path = new Path();
                buildBeamTree(ref source, ref beam, 0, 0);

                DateTime t1 = DateTime.Now;
                Console.WriteLine("beam 树建立时间：{0} s", (t1 - t0).TotalMilliseconds / 1000);
                Console.WriteLine(string.Format("beam 树节点总数：{0}", m_solutionNodes.Count));
                Console.WriteLine(string.Format("beam 树叶节点总数：{0}", m_leaves.Count));

                getPaths();     // 获取多条主路径
                //getPaths1();  // 只获取一条主路径
            }
        }

        // 去掉被遮挡的面 2019.1.2
        public HashSet<int> getPolygonsID(Vector3 top, ref List<Polygon> polygons)
        {
            List<int> buildingids = new List<int>();
            for (int i = 0; i < polygons.Count; i++)
            {
                if (!polygons[i].m_ground)
                    buildingids.Add(polygons[i].m_buildingID);
            }

            LTE.Geometric.Point source = new LTE.Geometric.Point(top.x, top.y, top.z);
            Dictionary<int, LTE.InternalInterference.Grid.TriangleBound> dic = LTE.InternalInterference.Grid.BuildingGrid3D.getShelterDisAndAngleBeam(source, buildingids, 0);

            HashSet<int> ret = new HashSet<int>(dic.Keys.ToList());
            return ret;
        }

        // 2018.12.12
        // 建立 beam 树，得到 m_solutionNodes，去掉部分遮挡
        // 存储每个 beam 节点中离目标点最近的 beam 侧面
        public void buildBeamTree(ref Vector3 source, ref Beam beam, int order, int parentIndex)
        {
            // 达到最大层数或入地
            if (order >= m_maximumOrder || (beam != null && beam.getPolygon().m_ground))
            {
                m_leaves.Add(m_solutionNodes.Count - 1);
                return;
            }

            List<Polygon> polygons = new List<Polygon>();
            m_room.getKD().beamCast(ref beam, ref polygons);

            HashSet<int> pid = getPolygonsID(beam.getTop(), ref polygons); // 去掉被遮挡的面 2019.1.2

            for (int i = (int)polygons.Count - 1; i >= 0; i--)  // 当前 KD 节点中包含的所有多边形
            {
                Polygon orig = polygons[i];
                Vector3 imgSource = Vector4.mirror(ref source, orig.getPleq());

                if (parentIndex > 0)  // 有父 beam，跳过一些特殊情况
                {
                    Polygon ppoly = m_solutionNodes[parentIndex].m_polygon;
                    if (orig.m_id == ppoly.m_id)  // 如果与上一个面是同一个面，跳过
                        continue;

                    Vector3 testSource = Vector4.mirror(ref imgSource, ppoly.getPleq());
                    if ((source - testSource).length() < EPS_SIMILAR_PATHS)  // 如果与上一个源相距太近
                        continue;
                }
                else  // 2018.12.11  被遮挡的面不构成第一级反射 beam
                {
                    if (!orig.m_first)
                        continue;
                }

                // 地形
                if (!orig.m_ground && !pid.Contains(orig.m_buildingID))  // 完全被遮挡的建筑物面  2019.1.2
                    continue;

                Polygon poly = new Polygon(ref orig);
                if (poly.clip(ref beam) == Polygon.ClipResult.CLIP_VANISHED)
                    continue;

                if (poly.getArea() < EPS_DEGENERATE_POLYGON_AREA)
                    continue;

                Beam b = new Beam(ref imgSource, ref poly);

                SolutionNode node = new SolutionNode();
                node.m_polygon = orig;
                node.m_clipedPolygon = poly;
                node.m_parent = parentIndex;
                m_solutionNodes.Add(node);

                buildBeamTree(ref imgSource, ref b, order + 1, m_solutionNodes.Count - 1);
            }
        }

        // 2018.12.13
        public void getPaths()
        {
            for (int i = 0; i < m_leaves.Count; i++)
            {
                int nodeIndex = m_leaves[i];

                // 收集 beam 多边形
                int order = 0;
                List<List<Vector3>> pts = new List<List<Vector3>>(); // 每个多边形内的点
                int maxCnt = 0, minCnt = 1000000;
                // 只有靠近发射源的面 nodeIndex == 0
                while (nodeIndex != 0)  // m_polygonCache 中，第一个面是靠近目标的面，最后一个面是靠近发射源的面
                {
                    m_polygonCache[order++] = new Polygon(ref m_solutionNodes[nodeIndex].m_polygon);

                    List<Vector3> pt = m_solutionNodes[nodeIndex].m_polygon.getInerPoints(10, ref m_solutionNodes[nodeIndex].m_clipedPolygon.m_points);

                    if (pt.Count == 0)
                    {
                        // 得到 poly 的中心
                        Polygon poly = m_solutionNodes[nodeIndex].m_clipedPolygon;
                        float cx = 0, cy = 0, cz = 0;
                        int n = poly.m_points.Count;
                        for (int k = 0; k < n; k++)
                        {
                            cx += poly.m_points[k].x;
                            cy += poly.m_points[k].y;
                            cz += poly.m_points[k].z;
                        }
                        Vector3 tmp = new Vector3(cx / n, cy / n, cz / n);
                        pt.Add(tmp);
                    }

                    pts.Add(pt);
                    maxCnt = Math.Max(maxCnt, pt.Count);
                    minCnt = Math.Min(minCnt, pt.Count);

                    nodeIndex = m_solutionNodes[nodeIndex].m_parent;
                }

                int times = (maxCnt + minCnt) / 2;
                if (times == 0)
                    times = 1;

                int pre = m_rays.Count;
                HashSet<String> pathExist = new HashSet<string>();

                #region 随机组合 times 条主路径
                for (int k = 0; k < times; k++)
                {
                    string route = "";

                    bool ok = true;
                    Vector3 t;
                    int prePolyId = -1;
                    Rays rays = new Rays();
                    Vector3 s = new Vector3(ref m_source.m_position);
                    for (int j = order - 1; j >= 0; j--)  // 从靠近源的面开始
                    {
                        Polygon poly = m_polygonCache[j];

                        int id = rand.Next(0, pts[j].Count);
                        t = pts[j][id];
                        route += string.Format("{0}", id) + ",";

                        Ray ray = new Ray(ref s, ref t);
                        if (m_room.getKD().rayCastAny(ref ray, poly.m_id, prePolyId))  // 非法路径
                        {
                            ok = false;
                            break;
                        }

                        s = t;
                        prePolyId = poly.m_id;

                        NodeInfo rayInfo;
                        if (j == order - 1)
                        {
                            rayInfo = Ray.createRay(ref ray);
                        }
                        else
                        {
                            Vector4 pleq = poly.getPleq();
                            rayInfo = Ray.createRay(ref ray, ref pleq);
                        }
                        rayInfo.buildingID = poly.m_buildingID;

                        if (double.IsNaN(rayInfo.Angle) || double.IsInfinity(rayInfo.Angle))
                        {
                            ok = false;
                            break;
                        }

                        if (pathExist.Contains(route))  // 该路径已经存在过了
                            ok = false;
                        else
                            rays.m_rays.Add(rayInfo);
                    }

                    if (ok)  // 整个路径段合法
                    {
                        pathExist.Add(route);

                        double rayAzimuth = 0;
                        double rayIncination = 0;
                        LTE.Geometric.Point startp = new Geometric.Point(rays.m_rays[0].PointOfIncidence.m_position.x, rays.m_rays[0].PointOfIncidence.m_position.y, rays.m_rays[0].PointOfIncidence.m_position.z);
                        LTE.Geometric.Point endp = new Geometric.Point(rays.m_rays[0].CrossPoint.m_position.x, rays.m_rays[0].CrossPoint.m_position.y, rays.m_rays[0].CrossPoint.m_position.z);
                        LTE.Geometric.GeometricUtilities.getAzimuth_Inclination(startp, endp, out rayAzimuth, out rayIncination);
                        double[] ret = this.calcStrength.calcRayStrengthBeam(rayAzimuth, rayIncination, ref rays.m_rays);
                        rays.emitPwrDbm = this.calcStrength.convertw2dbm(ret[2]);
                        rays.recvPwrDbm = this.calcStrength.convertw2dbm(ret[0]);

                        m_rays.Add(rays);
                        rayCount += rays.m_rays.Count;
                    }
                }
                #endregion

                #region 加入最短主路径  2018.12.20
                //if (m_rays.Count - pre > 0 && times > 1)
                //{
                //    List<Vector3> ss = new List<Vector3>();
                //    ss.Add(new Vector3(ref m_source.m_position));
                //    pts.Add(ss);
                //    string route = "";
                //    Vector3[] path = plane(ref pts, maxCnt, ref route);  // 多阶段图最短路径
                //    if (!pathExist.Contains(route))  // 该路径未被加入
                //    {
                //        bool ok = true;
                //        Vector3 t;
                //        int prePolyId = -1;
                //        Rays rays = new Rays();
                //        Vector3 s = path[order];
                //        for (int j = order - 1; j >= 0; j--)  // 从靠近源的面开始
                //        {
                //            Polygon poly = m_polygonCache[j];

                //            t = path[j];

                //            Ray ray = new Ray(ref s, ref t);
                //            if (m_room.getKD().rayCastAny(ref ray, poly.m_id, prePolyId))  // 非法路径
                //            {
                //                ok = false;
                //                break;
                //            }

                //            s = t;
                //            prePolyId = poly.m_id;

                //            NodeInfo rayInfo;
                //            if (j == order - 1)
                //            {
                //                rayInfo = Ray.createRay(ref ray);
                //            }
                //            else
                //            {
                //                Vector4 pleq = poly.getPleq();
                //                rayInfo = Ray.createRay(ref ray, ref pleq);
                //            }
                //            rayInfo.buildingID = poly.m_buildingID;

                //            if (double.IsNaN(rayInfo.Angle) || double.IsInfinity(rayInfo.Angle))
                //            {
                //                ok = false;
                //                break;
                //            }

                //            rays.m_rays.Add(rayInfo);
                //        }

                //        if (ok)  // 整个路径段合法
                //        {
                //            double rayAzimuth = 0;
                //            double rayIncination = 0;
                //            LTE.Geometric.Point startp = new Geometric.Point(rays.m_rays[0].PointOfIncidence.m_position.x, rays.m_rays[0].PointOfIncidence.m_position.y, rays.m_rays[0].PointOfIncidence.m_position.z);
                //            LTE.Geometric.Point endp = new Geometric.Point(rays.m_rays[0].CrossPoint.m_position.x, rays.m_rays[0].CrossPoint.m_position.y, rays.m_rays[0].CrossPoint.m_position.z);
                //            LTE.Geometric.GeometricUtilities.getAzimuth_Inclination(startp, endp, out rayAzimuth, out rayIncination);
                //            double[] ret = this.calcStrength.calcRayStrengthBeam(rayAzimuth, rayIncination, ref rays.m_rays);
                //            rays.emitPwrDbm = this.calcStrength.convertw2dbm(ret[2]);
                //            rays.recvPwrDbm = this.calcStrength.convertw2dbm(ret[0]);

                //            m_rays.Add(rays);
                //        }
                //    }
                //}
                #endregion
            }
        }

        // 2018.12.20 多阶段图
        // pts: 每阶段有哪些点
        // 返回最佳点序列
        public Vector3[] plane(ref List<List<Vector3>> pts, int maxCnt, ref string route)
        {
            int stageNum = pts.Count + 1;  // 加入超级汇点
            double[,] minRoad = new double[stageNum, maxCnt];
            Vector3[] path = new Vector3[stageNum];
            int[] route1 = new int[stageNum];
            pts.Reverse();

            // 初始化
            for (int i = 0; i < stageNum; i++)
            {
                for (int j = 0; j < maxCnt; j++)
                    minRoad[i, j] = double.MaxValue;
            }
            for (int i = 0; i < pts[0].Count; i++)
                minRoad[0, i] = 0;

            // 求解
            int k;
            for (k = 0; k < stageNum - 2; k++)
            {
                for (int q = 0; q < pts[k].Count; q++)
                {
                    for (int p = 0; p < pts[k + 1].Count; p++)
                    {
                        double tmp = minRoad[k, q] + dis(pts[k][q], pts[k + 1][p]);
                        if (tmp < minRoad[k + 1, p])
                        {
                            minRoad[k + 1, p] = tmp;
                            path[k] = pts[k][q];
                            route1[k] = q;
                        }
                    }
                }
            }
            // 最后一阶段
            k = stageNum - 2;
            for (int q = 0; q < pts[k].Count; q++)
            {
                if (minRoad[k, q] < minRoad[k + 1, 0])
                {
                    minRoad[k + 1, 0] = minRoad[k, q];
                    path[k] = pts[k][q];
                    route1[k] = q;
                }
            }

            // 路径顶点序列
            for (int i = 1; i < stageNum - 1; i++)
            {
                route += route1[i] + ",";
            }

            return path;
        }

        double dis(Vector3 a, Vector3 b)
        {
            return Math.Sqrt(Math.Pow(a.x - b.x, 2) + Math.Pow(a.y - b.y, 2) + Math.Pow(a.z - b.z, 2));
        }

        // 2018.12.12
        public void getPaths1()
        {
            for (int i = 0; i < m_leaves.Count; i++)
            {
                Rays rays = new Rays();

                Vector3 s = new Vector3(ref m_source.m_position);

                int nodeIndex = m_leaves[i];

                // 收集多边形
                int order = 0;
                // 只有靠近发射源的面 nodeIndex == 0
                List<Vector3> pts = new List<Vector3>();
                while (nodeIndex != 0)  // m_polygonCache 中，第一个面是靠近目标的面，最后一个面是靠近发射源的面
                {
                    m_polygonCache[order++] = new Polygon(ref m_solutionNodes[nodeIndex].m_polygon);

                    // 得到 poly 的中心
                    Polygon poly = m_solutionNodes[nodeIndex].m_clipedPolygon;
                    float cx = 0, cy = 0, cz = 0;
                    int n = poly.m_points.Count;
                    for (int k = 0; k < n; k++)
                    {
                        cx += poly.m_points[k].x;
                        cy += poly.m_points[k].y;
                        cz += poly.m_points[k].z;
                    }
                    Vector3 pt = new Vector3(cx / n, cy / n, cz / n);
                    pts.Add(pt);

                    nodeIndex = m_solutionNodes[nodeIndex].m_parent;
                }

                bool ok = true;
                Vector3 t;
                int prePolyId = -1;
                for (int j = order - 1; j >= 0; j--)  // 从靠近源的面开始
                {
                    Polygon poly = m_polygonCache[j];

                    t = pts[j];

                    Ray ray = new Ray(ref s, ref t);
                    if (m_room.getKD().rayCastAny(ref ray, poly.m_id, prePolyId))  // 非法路径
                    {
                        ok = false;
                        break;
                    }

                    s = t;
                    prePolyId = poly.m_id;

                    NodeInfo rayInfo;
                    if (j == order - 1)
                    {
                        rayInfo = Ray.createRay(ref ray);
                    }
                    else
                    {
                        Vector4 pleq = poly.getPleq();
                        rayInfo = Ray.createRay(ref ray, ref pleq);
                    }
                    rayInfo.buildingID = poly.m_buildingID;
                    rays.m_rays.Add(rayInfo);
                }

                if (ok)  // 整个路径段合法
                {
                    double rayAzimuth = 0;
                    double rayIncination = 0;
                    LTE.Geometric.Point startp = new Geometric.Point(rays.m_rays[0].PointOfIncidence.m_position.x, rays.m_rays[0].PointOfIncidence.m_position.y, rays.m_rays[0].PointOfIncidence.m_position.z);
                    LTE.Geometric.Point endp = new Geometric.Point(rays.m_rays[0].CrossPoint.m_position.x, rays.m_rays[0].CrossPoint.m_position.y, rays.m_rays[0].CrossPoint.m_position.z);
                    LTE.Geometric.GeometricUtilities.getAzimuth_Inclination(startp, endp, out rayAzimuth, out rayIncination);
                    double[] ret = this.calcStrength.calcRayStrengthBeam(rayAzimuth, rayIncination, ref rays.m_rays);
                    rays.emitPwrDbm = this.calcStrength.convertw2dbm(ret[2]);
                    rays.recvPwrDbm = this.calcStrength.convertw2dbm(ret[0]);

                    m_rays.Add(rays);
                }
            }
        }

        // 找到最有可能失败的面
        // 平面已经被归一化
        // 取离该点最近的面
        public Vector4 getFailPlane(ref Beam beam, ref Vector3 target)
        {
            Vector4 failPlane = new Vector4(0, 0, 0, 1);
            if (beam.numPleqs() > 0)
                failPlane = beam.getPleq(0);

            for (int i = 1; i < beam.numPleqs(); i++)
                if (Vector4.dot(ref target, beam.getPleq(i)) < Vector4.dot(ref target, ref failPlane))
                    failPlane = beam.getPleq(i);

            return failPlane;
        }

        // failPlane 也是输出结果
        public void validatePath(ref Vector3 source,
                                ref Vector3 target,
                                int nodeIndex,
                                ref Vector4 failPlane)
        {
            // 收集多边形
            int order = 0;
            // 只有靠近发射源的面 nodeIndex == 0
            while (nodeIndex != 0)  // m_polygonCache 中，第一个面是靠近目标的面，最后一个面是靠近发射源的面
            {
                m_polygonCache[order++] = m_solutionNodes[nodeIndex].m_polygon;
                nodeIndex = m_solutionNodes[nodeIndex].m_parent;
            }

            // 重建虚拟源
            Vector3 imgSource = source;
            for (int i = order - 1; i >= 0; i--)  // 从发射源开始重建虚拟源
                imgSource = Vector4.mirror(ref imgSource, m_polygonCache[i].getPleq());

            // 失败面测试
            Vector3 s = imgSource;
            Vector3 t = target;

            bool missed = false;
            int missOrder = -1;
            Polygon missPoly = null;
            Ray missRay = new Ray(new Vector3(0, 0, 0), new Vector3(0, 0, 0));
            bool missSide = false;

            for (int i = 0; i < order; i++)  // 从靠近目标的面开始
            {
                Polygon poly = m_polygonCache[i];
                Vector4 pleq = poly.getPleq();
                Ray ray = new Ray(ref s, ref t);

                // 射线完全位于障碍物的一边，不可能发射反射
                if (Vector4.dot(ref s, ref pleq) * Vector4.dot(ref t, ref pleq) > 0)
                {
                    missed = true;
                    missSide = true;
                    missOrder = i;
                    missPoly = poly;
                    missRay = ray;
                    break;
                }

                // 射线没有与障碍物产生交点，不可能发生反射
                if (!ray.intersectExt(ref poly))
                {
                    missed = true;
                    missSide = false;
                    missOrder = i;
                    missPoly = poly;
                    missRay = ray;
                    break;
                }

                // 射线与障碍物的交点  2018.12.04
                Vector3 isect = new Vector3();
                NodeInfo rayInfo = Ray.intersect(ref ray, ref pleq, out isect);
                rayInfo.buildingID = poly.m_buildingID;
                m_raysCache[i] = rayInfo;

                s = Vector4.mirror(ref s, ref pleq);  // 新的虚拟源
                t = isect;

                m_validateCache[i * 2] = isect;
                m_validateCache[i * 2 + 1] = s;
            }

            // 传播失败面
            if (missed)
            {
                Vector4 missPlane = new Vector4(0, 0, 0, 0);
                if (missSide)
                {
                    // 根据面方程重建
                    missPlane = missPoly.getPleq();
                    if (Vector4.dot(ref missRay.m_a, ref missPlane) > 0)
                        missPlane.opNegative();
                }
                else
                {
                    // 根据失败的 beam 边重建
                    Beam beam = new Beam(ref missRay.m_a, ref missPoly);
                    missPlane = beam.getPleq(1);
                    for (int i = 2; i < beam.numPleqs(); i++)
                        if (Vector4.dot(ref missRay.m_b, beam.getPleq(i)) < Vector4.dot(ref missRay.m_b, ref missPlane))
                            missPlane = beam.getPleq(i);
                }

                // 传播失败面
                for (int i = missOrder - 1; i >= 0; i--)  // 从当前面到接近目标的面
                    missPlane = Vector3.mirror(ref missPlane, m_polygonCache[i].getPleq());

                // 由于浮点精度，可能出错，重新计算
                if (Vector4.dot(ref target, ref missPlane) > 0)
                {
                    // 从失败面重建 beam
                    Beam beam = new Beam();
                    imgSource = source;
                    for (int i = order - 1; i >= 0; i--)
                    {
                        Polygon poly = m_polygonCache[i];
                        poly.clip(ref beam);

                        imgSource = Vector4.mirror(ref imgSource, poly.getPleq());
                        beam = new Beam(ref imgSource, ref poly);
                    }

                    // 更新失败面
                    missPlane = getFailPlane(ref beam, ref target);
                }

                // 归一化
                missPlane.normalize();
                failPlane = missPlane;
                return;
            }

            // 检测路径是否合法
            t = target;
            for (int i = 0; i < order; i++)  // 从接收点开始检测
            {
                Vector3 isect = m_validateCache[i * 2];
                Ray ray = new Ray(ref isect, ref t);
                if (m_room.getKD().rayCastAny(ref ray))
                    return;

                t = isect;
            }
            Ray ray1 = new Ray(ref source, ref t);
            if (m_room.getKD().rayCastAny(ref ray1))  // 检测到发射源的路径是否合法
                return;

            // 将合法路径加入结果
            Path path = new Path();
            path.m_order = order;
            path.m_points = new List<Vector3>(new Vector3[order + 2]);
            //path.m_polygons = new List<Polygon>(new Polygon[order]);

            // 2018.12.04
            Rays rays = new Rays(order);
            if (order > 0)
                m_raysCache[order - 1].PointOfIncidence = new Point(source.x, source.y, source.z);

            t = target;
            for (int i = 0; i < order; i++)
            {
                path.m_points[order - i + 1] = t;
                // path.m_polygons[order - i - 1] = m_polygonCache[i];

                t = m_validateCache[i * 2];
                rays.m_rays[order - i - 1] = m_raysCache[i];  // 2018.12.04
            }

            path.m_points[0] = source;
            path.m_points[1] = t;

            #region 将相似的路径移除---效果不明显
            //float fval = Vector3.dot(path.m_points[1], new Vector3(1, 1, 1));  // 
            //float fmin = fval - 2 * EPS_SIMILAR_PATHS;
            //float fmax = fval + 2 * EPS_SIMILAR_PATHS;

            //foreach (List<int> paths in m_pathFirstSet.Values)
            //{
            //    for (int i = 0; i < paths.Count; i++)
            //    {
            //        Path p = m_paths[paths[i]];
            //        if (p.m_order != order)
            //            continue;
            //        bool safe = false;
            //        for (int k = 1; k < (int)p.m_points.Count - 1; k++)
            //        {
            //            if ((p.m_points[k] - path.m_points[k]).lengthSqr() > EPS_SIMILAR_PATHS * EPS_SIMILAR_PATHS)
            //            {
            //                safe = true;
            //                break;
            //            }
            //        }
            //        if (!safe)
            //            return;
            //    }
            //}

            //if (m_pathFirstSet.Keys.Contains(fval))
            //{
            //    m_pathFirstSet[fval].Add(m_paths.Count);
            //}
            //else
            //{
            //    m_pathFirstSet[fval] = new List<int>();
            //    m_pathFirstSet[fval].Add(m_paths.Count);
            //}
            #endregion

            m_paths.Add(path);

            // 2018.12.04  最后一段射线
            Vector3 pt0 = path.m_points[path.m_points.Count - 2];
            Vector3 pt1 = path.m_points[path.m_points.Count - 1];
            NodeInfo ray2;
            if (rays.m_rays.Count > 0)
            {
                ray2 = Ray.oneRay(pt0, pt1, RayType.VReflection);
            }
            else
            {
                ray2 = Ray.oneRay(pt0, pt1, RayType.Direction);
            }

            rays.m_rays.Add(ray2);
            m_rays.Add(rays);
        }

        // 建立 beam 树，得到 m_solutionNodes，不考虑遮挡
        // 存储每个 beam 节点中离目标点最近的 beam 侧面
        public void solveRecursive(ref Vector3 source, ref Vector3 target, ref Beam beam, int order, int parentIndex)
        {
            m_failPlanes.Add(new Vector4(getFailPlane(ref beam, ref target)));  // 离目标点最近的面

            if (order >= m_maximumOrder)
            {
                return;
            }

            List<Polygon> polygons = new List<Polygon>();
            m_room.getKD().beamCast(ref beam, ref polygons);
            for (int i = (int)polygons.Count - 1; i >= 0; i--)  // 当前 KD 节点中包含的所有多边形
            {
                Polygon orig = polygons[i];
                Vector3 imgSource = Vector4.mirror(ref source, orig.getPleq());

                if (parentIndex > 0)  // 有父 beam，跳过一些特殊情况
                {
                    Polygon ppoly = m_solutionNodes[parentIndex].m_polygon;
                    if (orig.m_id == ppoly.m_id)  // 如果与上一个面是同一个面，跳过
                        continue;

                    Vector3 testSource = Vector4.mirror(ref imgSource, ppoly.getPleq());
                    if ((source - testSource).length() < EPS_SIMILAR_PATHS)  // 如果与上一个源相距太近
                        continue;
                }
                else  // 2018.12.11  被遮挡的面不构成初级 beam
                {
                    if (!orig.m_first)
                        continue;
                }

                Polygon poly = new Polygon(ref orig);
                if (poly.clip(ref beam) == Polygon.ClipResult.CLIP_VANISHED)
                    continue;

                if (poly.getArea() < EPS_DEGENERATE_POLYGON_AREA)
                    continue;

                Beam b = new Beam(ref imgSource, ref poly);

                SolutionNode node = new SolutionNode();
                node.m_polygon = orig;
                //node.m_clipedPolygon = poly;
                node.m_parent = parentIndex;
                m_solutionNodes.Add(node);

                solveRecursive(ref imgSource, ref target, ref b, order + 1, m_solutionNodes.Count - 1);

                //if (order == 0)
                //   Console.WriteLine("building beam tree.. {0}% ({1}) {2}\r", 100 - (float)i / (float)polygons.Count() * 100, m_solutionNodes.Count, m_node++);
            }

            //if (order == 0)
            //{
            //    Console.WriteLine("{0}", m_level++);
            //}
        }

        #region beam 覆盖分析

        // 2018.12.12
        // 建立 beam 树，得到 m_solutionNodes，去掉部分遮挡
        // 存储每个 beam 节点中离目标点最近的 beam 侧面
        public void buildBeamTree1(ref Vector3 source, ref Vector3 target, ref Beam beam, int order, int parentIndex)
        {
            m_failPlanes.Add(new Vector4(getFailPlane(ref beam, ref target)));  // 离目标点最近的面

            if (order >= m_maximumOrder)
            {
                m_leaves.Add(m_solutionNodes.Count - 1);
                return;
            }

            List<Polygon> polygons = new List<Polygon>();
            m_room.getKD().beamCast(ref beam, ref polygons);

            HashSet<int> pid = getPolygonsID(beam.getTop(), ref polygons); // 得到未被遮挡的建筑物面 2019.1.2

            for (int i = (int)polygons.Count - 1; i >= 0; i--)  // 当前 KD 节点中包含的所有多边形
            {
                Polygon orig = polygons[i];
                Vector3 imgSource = Vector4.mirror(ref source, orig.getPleq());

                if (parentIndex > 0)  // 有父 beam，跳过一些特殊情况
                {
                    Polygon ppoly = m_solutionNodes[parentIndex].m_polygon;
                    if (orig.m_id == ppoly.m_id)  // 如果与上一个面是同一个面，跳过
                        continue;

                    Vector3 testSource = Vector4.mirror(ref imgSource, ppoly.getPleq());
                    if ((source - testSource).length() < EPS_SIMILAR_PATHS)  // 如果与上一个源相距太近
                        continue;
                }
                else  // 2018.12.11  被遮挡的面不构成第一级反射 beam
                {
                    if (!orig.m_first)
                        continue;
                }

                if (!pid.Contains(orig.m_buildingID))  // 完全被遮挡的建筑物面  2019.1.2
                    continue;

                Polygon poly = new Polygon(ref orig);
                if (poly.clip(ref beam) == Polygon.ClipResult.CLIP_VANISHED)
                    continue;

                if (poly.getArea() < EPS_DEGENERATE_POLYGON_AREA)
                    continue;

                Beam b = new Beam(ref imgSource, ref poly);

                SolutionNode node = new SolutionNode();
                node.m_polygon = orig;
                node.m_clipedPolygon = poly;
                node.m_parent = parentIndex;
                m_solutionNodes.Add(node);

                buildBeamTree1(ref imgSource, ref target, ref b, order + 1, m_solutionNodes.Count - 1);
            }
        }

        public void beamTracing()
        {
            int numProc = 0;
            int numTested = 0;

            Vector3 source = m_source.getPosition();
            Vector3 target = m_listener.getPosition();

            m_paths.Clear();

            // 只有当发射源改变了才会重新建立beam
            if (m_solutionNodes.Count == 0 || m_cachedSource.x != source.x || m_cachedSource.y != source.y || m_cachedSource.z != source.z)  // target 改变后不影响
            {
                clearCache();

                SolutionNode root = new SolutionNode();  // beam 根节点
                root.m_polygon = null;
                root.m_parent = -1;
                m_solutionNodes.Add(root);  // 记录障碍物面及父 beam id

                DateTime t0 = DateTime.Now;
                Beam beam = new Beam();
                buildBeamTree1(ref source, ref target, ref beam, 0, 0);  // 得到 m_solutionNodes，m_failPlanes
                //solveRecursive(ref source, ref target, ref beam, 0, 0);  // 得到 m_solutionNodes，m_failPlanes
                DateTime t1 = DateTime.Now;
                Console.WriteLine("束树建立时间：{0} s", (t1 - t0).TotalMilliseconds / 1000);
                Console.WriteLine(string.Format("束树节点总数：{0}", m_solutionNodes.Count));

                m_cachedSource.x = source.x;
                m_cachedSource.y = source.y;
                m_cachedSource.z = source.z;

                // 设置桶的数量
                // numBuckets <= m_s//olutionNodes.size()
                int numBuckets = (m_solutionNodes.Count + DISTANCE_SKIP_BUCKET_SIZE - 1) / DISTANCE_SKIP_BUCKET_SIZE;
                m_distanceSkipCache = new List<Vector4>();
                for (int i = 0; i < numBuckets; i++)
                    m_distanceSkipCache.Add(new Vector4(0, 0, 0, 0));
            }

            int n = m_solutionNodes.Count;
            int nb = (m_solutionNodes.Count + DISTANCE_SKIP_BUCKET_SIZE - 1) / DISTANCE_SKIP_BUCKET_SIZE;  // numBuckets
            List<Vector4> skipSphere = m_distanceSkipCache;
            for (int b = 0; b < nb; b++)
            {
                Vector4 fc = skipSphere[b];
                Vector3 r = target - new Vector3(fc.x, fc.y, fc.z);  // 与障碍物的距离
                if (r.lengthSqr() < fc.w)    // 接收点位于忽略球内，忽略所有路径测试。
                    continue;

                float maxdot = 0;
                numProc++;

                int imn = b * DISTANCE_SKIP_BUCKET_SIZE;   // 第 b 个桶
                int imx = imn + DISTANCE_SKIP_BUCKET_SIZE;
                if (imx > n)
                    imx = n;
                for (int i = imn; i < imx; i++)
                {
                    float d = Vector4.dot(ref target, m_failPlanes[i]);

                    if (d >= 0)  // 在失败面的前面，也就是在 beam 面的后面，可能有合法路径
                    {
                        Vector4 failPlane = m_failPlanes[i];
                        validatePath(ref source, ref target, i, ref failPlane);  // 验证路径，更新 m_failPlanes[i]，m_path
                        m_failPlanes[i] = failPlane;
                        int cnt = m_paths.Count;
                        numTested++;
                    }
                    if (i == imn || d > maxdot)
                        maxdot = d;
                }

                if (maxdot < 0)   // 桶中的所有节点都失败了，便可计算失败面到接收点的最短距离，作为半径，而接收点为球心
                    m_distanceSkipCache[b].set(target.x, target.y, target.z, maxdot * maxdot);
            }

            m_pathFirstSet.Clear();
        }

        #endregion
    }
}
