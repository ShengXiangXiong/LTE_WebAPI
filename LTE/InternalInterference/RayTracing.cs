using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Data;
using System.Windows.Forms;

using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Analyst3D;
using ESRI.ArcGIS.Geodatabase;

//using LTE.Enos;
using LTE.GIS;
using LTE.Geometric;
using LTE.DB;
using LTE.InternalInterference.Grid;
using System.Threading;
using System.Runtime.InteropServices;
using LTE.Win32Lib;

namespace LTE.InternalInterference
{
    public class RayTracing
    {
        private static object _missing = Type.Missing;
        //反射次数
        private int ReflectionLevel;
        //绕射次数
        private int DiffrectionLevel;
        //是否计算室内覆盖
        private bool computeIndoor;
        //射线跟踪小区
        private CellInfo sourceInfo;
        //初始射线数，用于多线程计数
        private int rayCounter = 0;
        //需要计算场强射线（入地或者入射到室内）
        private int recRayCounter = 0;

        private int linecnter = 0;

        public int scenNum = 3;  // 场景数量 2019.3.25

        // 二次计算用到
        public double distance;
        Point p1, p2, p3, p4;
        public List<ReRay> reRays;
        public bool isRecordReRay;  // 2019.5.22

        public long rayCount;     // 统计射线总数
        public int rayCountDir;  // 统计直射线总数
        public int rayCountRef;  // 统计反射线总数
        public long rayCountDif;  // 统计绕射线总数
        public int rayCountTra;  // 统计透射线总数
        public int rayCountDirG;  // 统计地面直射线总数
        public int rayCountDirB;  // 统计楼顶直射线总数
        public long rayCountDif1;  // 统计初级绕射线总数
        public long rayCountDif2;  // 统计次级绕射线总数
        public int rayCountRef1;  // 统计初级反射线总数
        public int rayCountRef2;  // 统计次级反射线总数
        public int rayCountRef3;  // 统计三级反射线总数
        private IntPtr parentHandle;

        private IFeatureClass pFeatureClass;
        private int heightIndex;

        public double calTime;
        /// <summary>
        /// 记录栅格场强结果
        /// </summary>
        private Dictionary<string, GridStrength> gridStrengths;
        //private List<GridStrength> gridStrengths;
        /// <summary>
        /// 计算场强函数
        /// </summary>
        private CalcGridStrength calcStrength;

        //public ConsoleShow cs;

        public RayTracing()
        {
            this.scenNum = AdjCoeffHelper.getInstance().getSceneNum();
        }

        public RayTracing(CellInfo sourceInfo, int reflectionLevel, int diffrectionLevel, bool computeIndoor)
        {
            this.computeIndoor = computeIndoor;
            this.ReflectionLevel = reflectionLevel;
            this.DiffrectionLevel = diffrectionLevel;
            this.sourceInfo = sourceInfo;
            this.calTime = 0;
            this.gridStrengths = new Dictionary<string, GridStrength>();
            //this.gridStrengths = new List<GridStrength>();
            this.calcStrength = new CalcGridStrength(this.sourceInfo, this.gridStrengths);
            rayCount = 0;
            rayCountDir = 0;
            rayCountRef = 0;
            rayCountDif = 0;
            rayCountDirG = 0;
            rayCountDirB = 0;
            rayCountTra = 0;
            rayCountDif1 = 0;
            rayCountDif2 = 0;
            rayCountRef1 = 0;
            rayCountRef2 = 0;
            rayCountRef3 = 0;
            this.isRecordReRay = false;
            this.scenNum = AdjCoeffHelper.getInstance().getSceneNum();

            /*
            IFeatureLayer pFeatureLayer = GISMapApplication.Instance.GetLayer(LayerNames.Projecton) as IFeatureLayer;
            this.pFeatureClass = pFeatureLayer.FeatureClass;
            this.heightIndex = pFeatureClass.Fields.FindField("Height");
            */
            //cs = new ConsoleShow(); 
        }

        public RayTracing(CellInfo sourceInfo, int reflectionLevel, int diffrectionLevel, bool computeIndoor, double distance,
            bool isRecordReRay1, ref Geometric.Point p11, ref Geometric.Point p22, ref Geometric.Point p33, ref Geometric.Point p44)
        {
            this.computeIndoor = computeIndoor;
            this.ReflectionLevel = reflectionLevel;
            this.DiffrectionLevel = diffrectionLevel;
            this.sourceInfo = sourceInfo;
            this.calTime = 0;
            this.gridStrengths = new Dictionary<string, GridStrength>();
            //this.gridStrengths = new List<GridStrength>();
            this.calcStrength = new CalcGridStrength(this.sourceInfo, this.gridStrengths);
            rayCount = 0;
            rayCountDir = 0;
            rayCountRef = 0;
            rayCountDif = 0;
            rayCountDirG = 0;
            rayCountDirB = 0;
            rayCountTra = 0;
            rayCountDif1 = 0;
            rayCountDif2 = 0;
            rayCountRef1 = 0;
            rayCountRef2 = 0;
            rayCountRef3 = 0;
            this.distance = distance;
            reRays = new List<ReRay>();
            this.isRecordReRay = isRecordReRay1;
            
            this.p1 = p11;
            this.p2 = p22;
            this.p3 = p33;
            this.p4 = p44;

            this.scenNum = AdjCoeffHelper.getInstance().getSceneNum();
        }

        public void setParentHandle(IntPtr handle)
        {
            this.parentHandle = handle;
        }


        /// <summary>
        /// 获取栅格覆盖分析结果
        /// </summary>
        /// <returns></returns>
        public List<GridStrength> getGridStrengths()
        {
            return this.gridStrengths.Values.ToList();
            //List<GridStrength> gslist = new List<GridStrength>();
            //foreach (var value in this.gridStrengths.Values)
            //{
            //    gslist.Add(value);
            //}
            //return gslist;
        }

        public int getRayCounter()
        {
            return rayCounter;
        }

        public void setRayCounter(int cnt)
        {
            rayCounter = cnt;
        }

        public int getRecRayCounter()
        {
            return recRayCounter;
        }

        public void setRecRayCounter(int cnt)
        {
            recRayCounter = cnt;
        }

        public void setComputeIndoor(bool computeIndoor)
        {
            this.computeIndoor = computeIndoor;
        }

        /// <summary>
        /// 两个数乘积小于等于0
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool productLEZero(double x, double y)
        {
            return (x <= 0 && y >= 0) || (x >= 0 && y <= 0);
        }

        /*
        //石倩楠
        ///<summary>
        ///固定方向射线与建筑物相交预处理
        ///</summary>
        ///<param name="source">射线发出点</param>
        ///<param name="dir">射线方向</param>
        ///<param name="Vertex">建筑物底面顶点序列</param>
        public List<Point> getCorssedEdge(Point startp, Vector3D dir, List<Point> Vertex)
        {
            //返回值
            List<Point> ret = new List<Point>();

            //旋转以endpoint为原点，射线为Y轴
            //建筑物底面顶点进行平移
            List<Point> newVertex = new List<Point>();//存储变换后的坐标值
            Point temp;
            for (int i = 0, cnt = Vertex.Count; i < cnt; i++)
            {
                temp = new Point(Vertex[i].X - end.X, Vertex[i].Y - end.Y, 0);
                newVertex.Add(temp);
            }
            //发射点进行坐标平移
            startp.X -= end.X;
            startp.Y -= end.Y;

            //求出坐标变换所需三角函数的值
            double sin, cos, dis = Math.Sqrt(Math.Pow(source.X, 2) + Math.Pow(source.Y, 2));
            sin = source.X / dis;
            cos = source.Y / dis;

            //source坐标旋转后
            source.Y = dis;
            source.X = 0;

            //建筑物底面顶点进行坐标旋转

            double tx = 0;
            double ty = 0;
            for (int i = 0, cnt = newVertex.Count; i < cnt; i++)
            {
                tx = newVertex[i].X * cos - newVertex[i].Y * sin;
                ty = newVertex[i].Y * cos + newVertex[i].X * sin;
                newVertex[i].X = tx;
                newVertex[i].Y = ty;
            }

            //筛选符合条件的边
            List<int> plist = new List<int>();//存储可能相交的边的顶点的下标，两点为一组

            //首先筛选可能相交的边
            for (int i = 0, cnt = newVertex.Count, j = cnt - 1; i < cnt; j = i++)
            {
                if (this.productLEZero(newVertex[j].X, newVertex[i].X) && (newVertex[j].Y >= -2 || newVertex[i].Y >= -2) && (newVertex[j].Y < source.Y + 2 || newVertex[i].Y < source.Y + 2))
                {
                    plist.Add(j);
                    plist.Add(i);
                }
            }//for

            if (plist.Count == 0) return null;

            //从可能相交的边中取出最终相交的边:取距离source最近的点所在的棱边
            double miny = double.MaxValue / 10.0;
            int minp = -1, pCnt = plist.Count;//最近点下标记录
            double k, crossy, disy;
            for (int i = 0; i < pCnt; i += 2)
            {
                try
                {
                    k = (newVertex[plist[i]].Y - newVertex[plist[i + 1]].Y) / (newVertex[plist[i]].X - newVertex[plist[i + 1]].X);
                    crossy = (newVertex[plist[i]].Y - newVertex[plist[i]].X * k);
                    if (crossy > -2 && crossy < source.Y + 2 && miny > (disy = source.Y - crossy))
                    {
                        minp = i;
                        miny = disy;
                    }
                }
                catch
                {
                    continue;
                }
            }

            if (minp > -1)
            {
                ret.Add(Vertex[plist[minp]]);
                ret.Add(Vertex[plist[minp + 1]]);
            }

            return ret;
        }//石倩楠
        */

        ///<summary>
        ///固定方向射线与建筑物相交预处理
        ///选出距离source最近的底边
        ///</summary>
        ///<param name="startp">射线发出点</param>
        ///<param name="dir">射线方向</param>
        ///<param name="Vertex">建筑物底面顶点序列，顺时针排列</param>
        ///<param name="normal">侧面法向</param>
        public List<Point> getCorssedEdge(Point startp, Vector3D dir, List<Point> Vertex, out Vector3D normal)
        {
            List<Point> ans = new List<Point>();           // 返回值
            List<Line2D> lines = new List<Line2D>();       // 多边形的各条边
            List<double> hitTimes = new List<double>();    // 射线击中多边形各边的时间

            Line2D seg;

            for (int i = 0, j = Vertex.Count - 1; i < Vertex.Count; j = i++)
            {
                Vector2D pt1 = new Vector2D(Vertex[j].X, Vertex[j].Y);
                Vector2D pt2 = new Vector2D(Vertex[i].X, Vertex[i].Y);

                seg = new Line2D(pt1, pt2);                   // 当前边
                lines.Add(seg);
            }

            // 计算射线与每条边相交时间
            Vector2D p, n, t;
            double num, den;
            double hitT = 0, inT = 0, outT = double.MaxValue;
            for (int i = 0; i < lines.Count; i++)
            {
                // p为直线上的点，n为直线的法线
                lines[i].getPtNorm(out p, out n);

                t = new Vector2D(p.x - startp.X, p.y - startp.Y);
                num = n.x * t.x + n.y * t.y;      // num = n * t
                den = n.x * dir.XComponent + n.y * dir.YComponent;  // den = n * dir
                if (den == 0)
                {
                    normal = null;
                    return null;
                }
                hitT = num / den;
                hitTimes.Add(hitT);   // 得到击中时间，并加入列表

                if (den < 0)  // 射线是射入的
                {
                    //if (hitT > outT)
                    //{
                    //    normal = null;
                    //    return null;
                    //}
                    if (hitT > inT)
                        inT = hitT;     // 选择较大的
                }
            }

            // 得到多边形被击中的边
            if (inT > 0)
            {
                for (int i = 0; i < hitTimes.Count; i++)
                {
                    if (hitTimes[i] == inT)
                    {
                        ans = new List<Point>();
                        Point tmp1 = new Point(lines[i].A.x, lines[i].A.y, 0);
                        Point tmp2 = new Point(lines[i].B.x, lines[i].B.y, 0);
                        ans.Add(tmp1);
                        ans.Add(tmp2);
                        lines[i].getPtNorm(out p, out n);
                        n.unit();  // 单位化
                        normal = new Vector3D(n.x, n.y, 0);  // 侧面法向
                        return ans;     // 返回结果
                    }
                }
            }
            normal = null;
            return ans;
        }

        /// <summary>
        /// 2019.5.10 求射线与均匀栅格侧面的入交点和出交点，如果两个交点高度均比当前均匀栅格内所有的建筑物、TIN 高，则不需要进行碰撞检测
        /// </summary>
        /// <param name="origin">射线起点</param>
        /// <param name="dir">射线方向</param>
        /// <param name="grid">均匀栅格</param>
        /// <param name="buildingids">均匀栅格内的建筑物 ID</param>
        /// <param name="TINs">均匀栅格内的 TIN ID</param>
        /// <param name="crossWithBuilding">是否可能与当前均匀栅格内的建筑物碰撞</param>
        /// <param name="crossWithTIN">是否可能与当前均匀栅格内的 TIN 碰撞</param>
        /// <returns>true：射线不需要与当前均匀栅格内的建筑物、TIN 进行碰撞检测；false：需要进行碰撞检测</returns>
        public bool rayCrossAGrid3D(Point origin, Vector3D dir, Grid3D grid, out List<int> buildingids, out List<int> TINs, 
            out bool crossWithBuilding, out bool crossWithTIN)
        {
            crossWithBuilding = false;
            crossWithTIN = false;

            buildingids = AccelerateStruct.getAccelerateStruct(grid.gxid, grid.gyid, grid.gzid);
            TINs = AccelerateStruct.getAccelerateStructTIN(grid.gxid, grid.gyid, grid.gzid);

            if (buildingids == null)
            {
                return false;
            }

            // 均匀栅格底面的 4 个顶点
            List<Point> Vertix = new List<Point>();
            double oX = 0, oY = 0;
            GridHelper.getInstance().getOriginXY(ref oX, ref oY);
            double agridsize = GridHelper.getInstance().getAGridSize();
            double x1 = oX + grid.gxid * agridsize;
            double y1 = oY + grid.gyid * agridsize;
            Vertix.Add(new Point(x1, y1, 0));
            Vertix.Add(new Point(x1 + agridsize, y1, 0));
            Vertix.Add(new Point(x1 + agridsize, y1 + agridsize, 0));
            Vertix.Add(new Point(x1, y1 + agridsize, 0));
            int n = 4;

            for (int i = 0, j = n - 1; i < n; j = i, i++)  // 求射线与均匀栅格 4 个侧面的交点
            {
                Vector2D pt1 = new Vector2D(Vertix[j].X, Vertix[j].Y);
                Vector2D pt2 = new Vector2D(Vertix[i].X, Vertix[i].Y);
                Line2D seg = new Line2D(pt1, pt2);  // 当前边
                Vector2D pt, normal;  // p为直线上的点，n为直线的法线
                seg.getPtNorm(out pt, out normal);  

                Point crossPoint = GeometricUtilities.Intersection(origin, dir, Vertix[i], new Vector3D(normal.x, normal.y, 0));

                for (int k = 0; k < buildingids.Count; k++)  
                {
                    // 射线与均匀栅格侧面的交点比建筑物低，有可能会与建筑物发生碰撞
                    if (crossPoint != null && crossPoint.Z < BuildingGrid3D.getBuildingHeight(buildingids[k]))
                    {
                        crossWithBuilding = true;
                        return false;
                    }
                }

                // 2019.5.28 地形
                if (crossWithTIN)   // 已经与地形碰撞过了
                    continue;

                if (TINs == null)  // 当前均匀栅格内无地形
                    continue;

                for (int k = 0; k < TINs.Count; k++)
                {
                    // TIN 的最高点
                    double height = TINInfo.getTINMaxHeight(TINs[k]);

                    // 射线与均匀栅格侧面的交点比 TIN 低，有可能会与 TIN 发生碰撞
                    if (crossPoint != null && crossPoint.Z < height)
                    {
                        crossWithTIN = true;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 根据加速网格和空间直线获取相交射线
        /// </summary>
        /// <param name="origin">射线起点</param>
        /// <param name="dir">射线方向</param\\\\\\
        /// <param name="grid"></param>
        /// <param name="buildingPolygon">绕射所需</param>
        /// <returns></returns>
        public NodeInfo getInfoOfLineCrossAccGrid(Point origin, Vector3D dir, Grid3D grid, ref List<Point> buildingPolygon, ref RayType rayType)
        {
            if (grid == null) return null;
            
            // 2019.5.10 加速
            List<int> buildingids, TINs;
            bool crossWithBuilding, crossWithTIN;
            if (this.rayCrossAGrid3D(origin, dir, grid, out buildingids, out TINs, out crossWithBuilding, out crossWithTIN))
                return null;

            // 射线可能与建筑物碰撞
            #region
            if (crossWithBuilding)
            {
                int bcnt = buildingids.Count;

                //加速栅格内建筑物按到入射点的距离从小到大排序
                Dictionary<int, double> ID_Distance = new Dictionary<int, double>();
                for (int i = 0; i < bcnt; i++)
                {
                    Point centroid = BuildingGrid3D.getBuildingCenter(buildingids[i]);
                    if (centroid == null)
                    {
                        continue;
                    }
                    ID_Distance.Add(buildingids[i], GeometricUtilities.GetDistanceOf2DPoints(origin, centroid));
                }

                //从近到远依次遍历建筑物，计算射线
                var keyvalues = from keyvalue in ID_Distance orderby keyvalue.Value ascending select keyvalue;
                foreach (var keyvalue in keyvalues)
                {
                    List<Point> polygonPoints = BuildingGrid3D.getBuildingVertex(keyvalue.Key);
                    if (polygonPoints == null || polygonPoints.Count < 2)
                        continue;

                    double buildingHeight = BuildingGrid3D.getBuildingHeight(keyvalue.Key);

                    Point crossWithTop, crossWithSidePlane;
                    int topEdgeIndex = -1;

                    //如果入射点比建筑物低，则不可能与建筑物顶面内有交点，否则计算可能存在的交点
                    crossWithTop = (origin.Z - buildingHeight < 0) ? null : this.getTopPlaneLineIntersectPoint(origin, dir, polygonPoints, buildingHeight, ref rayType, ref topEdgeIndex);
                    
                    //如果不存在顶面交点，再计算可能存在的侧面交点
                    if (crossWithTop == null)
                    {
                        Vector3D normal;  // 侧面法向
                        List<Point> crossEdge = this.getCorssedEdge(origin, dir, polygonPoints, out normal);

                        if (crossEdge != null && crossEdge.Count == 2)
                        {
                            double altitude = BuildingGrid3D.getBuildingAltitude(keyvalue.Key);  // 地形
                            this.getSidePlanePoint(origin, dir, crossEdge[0], crossEdge[1], normal, buildingHeight,
                                altitude, // 地形
                                out crossWithSidePlane, ref rayType);

                            if (crossWithSidePlane != null 
                                && GridHelper.getInstance().checkPointXYZInGrid(crossWithSidePlane) 
                                && !PointComparer.Equals1(origin, crossWithSidePlane))  //排除入射点
                            {
                                buildingPolygon = polygonPoints;
                                return new NodeInfo(origin, crossWithSidePlane, crossEdge[0], crossEdge[1], keyvalue.Key, buildingHeight, normal, rayType, Vector3D.getAngle(ref dir, ref normal) - Math.PI / 2.0);
                            }
                        }

                    }
                    else
                    {
                        if (GridHelper.getInstance().checkPointXYZInGrid(crossWithTop))
                        {
                            //射线与顶面楞相交
                            if (rayType == RayType.HDiffraction)
                            {
                                int nextEdgePointIndex = (topEdgeIndex + 1) % polygonPoints.Count;

                                Vector3D edge = Vector3D.constructVector(polygonPoints[topEdgeIndex], polygonPoints[nextEdgePointIndex]);

                                //此处射线类型不写也行，射线类型是由跟踪类型决定的，此处所求的射线类型用于下次跟踪
                                buildingPolygon = polygonPoints;
                                return new NodeInfo(origin, crossWithTop, polygonPoints[topEdgeIndex], polygonPoints[nextEdgePointIndex], keyvalue.Key, buildingHeight, edge, rayType, Vector3D.getAngle(ref dir, ref edge));
                            }
                            else
                            {
                                Vector3D normal = new Vector3D(0, 0, 1);

                                //射线在顶面反射
                                Point tmp1 = polygonPoints[0];
                                Point tmp2 = polygonPoints[1];
                                tmp1.Z = tmp2.Z = buildingHeight;
                                buildingPolygon = polygonPoints;
                                return new NodeInfo(origin, crossWithTop, tmp1, tmp2, keyvalue.Key, buildingHeight, normal, rayType, Vector3D.getAngle(ref dir, ref normal) - Math.PI / 2.0);
                            }
                        }
                    }
                }
            }
            #endregion

            // 2019.5.28 地形
            // 射线可能与 TIN 碰撞 
            #region
            if (crossWithTIN)
            {
                // 依次遍历 TIN，计算射线
                for(int i=0; i<TINs.Count; i++)
                {
                    List<Point> polygonPoints = TINInfo.getTINVertex(TINs[i]);  // 点的序列为顺时针
                    if (polygonPoints == null || polygonPoints.Count < 2)
                        continue;

                    // TIN 的法向量
                    Vector3D normal = GeometricUtilities.normalOfPlane(polygonPoints[2], polygonPoints[1], polygonPoints[0]);

                    Point crossPt = GeometricUtilities.Intersection(origin, dir, polygonPoints[0], normal);

                    // 不存在交点
                    if (crossPt == null)
                        continue;

                    // 交点不在多边形范围内
                    if(!PointHeight.isInside(polygonPoints[2], polygonPoints[1], polygonPoints[0], crossPt.X, crossPt.Y))
                        continue;

                    // 构造射线
                    if (GridHelper.getInstance().checkPointXYZInGrid(crossPt))
                    {
                        return new NodeInfo(origin, crossPt, null, null, TINs[i], 0, normal, rayType, Vector3D.getAngle(ref dir, ref normal));
                    }
                }
            }
            #endregion

            return null;
        }

        double dis(Point p1, Point p2)                         
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2) + Math.Pow(p1.Z - p2.Z, 2));
        }

        public void rayScene(ref NodeInfo ray, ref int[] scene)
        {
            // 2019.3.25 场景记录
            int gxid = 0, gyid = 0, gzid = 0;
            // 射线起点场景
            GridHelper.getInstance().XYZToAccGrid(ray.PointOfIncidence.X, ray.PointOfIncidence.Y, ray.PointOfIncidence.Z, ref gxid, ref gyid, ref gzid);
            ray.startPointScen = AccelerateStruct.gridScene[string.Format("{0},{1},{2}", gxid, gyid, gzid)];
            // 射线终点场景
            GridHelper.getInstance().XYZToAccGrid(ray.CrossPoint.X, ray.CrossPoint.Y, ray.CrossPoint.Z, ref gxid, ref gyid, ref gzid);
            ray.endPointScen = AccelerateStruct.gridScene[string.Format("{0},{1},{2}", gxid, gyid, gzid)];
            // 射线经过的场景比例
            double sum = 0;
            for (int i = 0; i < scenNum; i++)
                sum += scene[i];

            string p = "";
            for (int i = 0; i < scenNum-1; i++)
                p += Math.Round(scene[i] / sum, 3).ToString() + ";";
            p += Math.Round(scene[scenNum - 1] / sum, 3).ToString();

            ray.proportion = p;
        }

        public void rayTracingFirst(Point originPoint, Point endPoint, List<NodeInfo> rayList, SourceInfo sourceInfo, RayType rayType, int type, double coverageRadius)
        {
            // 采用界面输入的校正系数
            if (this.scenNum == 0)
            {
                rayTracingFirstOri(originPoint, endPoint, rayList, sourceInfo, rayType, type, coverageRadius);
            }
            else  // 采用数据库中的多场景校正系数
            {
                rayTracingFirstAdj(originPoint, endPoint, rayList, sourceInfo, rayType, type, coverageRadius);
            }
        }

        /// <summary>
        /// 跟踪初级直射线传播
        /// </summary>
        /// <param name="originPoint">射线原点</param>
        /// <param name="endPoint">射线的第一次终点</param>
        /// <param name="rayList"></param>
        /// <param name="sourceInfo">小区</param>
        /// <param name="rayType">射线类型</param>
        /// <param name="type">初级直射线类型，1：连向地面；2：连向楼顶；3：连向可见侧面；4：连向可见棱边</param>
        /// <param name="coverageRadius">小区理论覆盖半径</param>
        public void rayTracingFirstOri(Point originPoint, Point endPoint, List<NodeInfo> rayList, SourceInfo sourceInfo, RayType rayType, int type, double coverageRadius)
        {
            if (double.IsNaN(sourceInfo.RayAzimuth))
            {
                return;
            }

            this.rayCount++;
            this.rayCountDir++;

            Grid3D curAccGrid;
            Vector3D dir = Vector3D.constructVector(originPoint, endPoint);
            dir.unit();

            //获取空间直线所经过的栅格
            DDA3D lineCrossGrid = new DDA3D(originPoint, dir);

            NodeInfo ray = null;
            RayType trayType = RayType.Direction;
            List<Point> polygonPoints = null;

            do
            {
                curAccGrid = lineCrossGrid.getNextCrossAccGrid();

                if (curAccGrid == null)
                {
                    break;
                }

                // 地形
                ray = this.getInfoOfLineCrossAccGrid(originPoint, dir, curAccGrid, ref polygonPoints, ref trayType);

                if (ray != null)  
                {
                    ray.rayType = rayType;
                    break;
                }
            } while (true);

            switch (type)
            {
                case 1:
                    // 射线没有与建筑物相交，直接到达地面
                    if (ray == null)
                    {
                        Vector3D normal = new Vector3D(0, 0, 1);
                        this.rayCountDirG++;
                        ray = new NodeInfo(originPoint, endPoint, new Point(-1, -1, -1), new Point(-1, -1, -1), -1, 0, null, rayType, Vector3D.getAngle(ref dir, ref normal) - Math.PI / 2.0);
                        rayList.Add(ray);
                        this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, false);
                    }
                    // 2019.5.30 可能与地形碰撞
                    else if (ray.SideFromPoint == null)
                    {
                        rayList.Add(ray);
                        this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, false);
                    }
                    // 2017.6.18  穿过建筑物到达地面
                    else
                    {
                        // 2019.5.24 不再计算
                        //Vector3D normal = new Vector3D(0, 0, 1);
                        //ray = new NodeInfo(originPoint, endPoint, new Point(-1, -1, -1), new Point(-1, -1, -1), -1, 0, null, rayType, Vector3D.getAngle(ref dir, ref normal) - Math.PI / 2.0);
                        //rayList.Add(ray);
                        //this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, true);
                    }
                    break;

                case 2:
                    if (ray == null)
                        break;

                    // 2019.5.30 可能与地形碰撞
                    if (ray.SideFromPoint == null)
                    {
                        rayList.Add(ray);
                        this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, false);
                    }
                    // 直接到达建筑物顶面
                    else if (Math.Abs(ray.CrossPoint.Z - endPoint.Z) < 0.5)
                    {
                        this.rayCountDirB++;
                        Vector3D normal = new Vector3D(0, 0, 1);

                        // 计算建筑物顶面接收到的场强
                        NodeInfo ray1 = new NodeInfo(originPoint, endPoint, new Point(-1, -1, -1), new Point(-1, -1, -1), -1, 0, null, rayType, Vector3D.getAngle(ref dir, ref normal) - Math.PI / 2.0);
                        rayList.Add(ray1);
                        this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, false);
                        rayList.Remove(ray1);

                        rayList.Add(ray);  // 先把初级直射线加进来

                        // 跟踪后续反射线
                        Vector3D inDir = Vector3D.constructVector(originPoint, endPoint);
                        inDir.unit();
                        ReflectedRay refRay = new ReflectedRay(ray);
                        Vector3D refDir = refRay.ConstructReflectedRay(ref inDir);
                        rayCountRef++;
                        this.rayCountRef1++;
                        this.rayTracing(ray.CrossPoint, refDir, rayList, sourceInfo, trayType, coverageRadius);
                    }
                    // 2017.7.4 穿过中间建筑物后到达建筑物顶面
                    else
                    {
                        // 2019.5.24 不再计算
                        //Vector3D normal = new Vector3D(0, 0, 1);
                        //ray = new NodeInfo(originPoint, endPoint, new Point(-1, -1, -1), new Point(-1, -1, -1), -1, 0, null, rayType, Vector3D.getAngle(ref dir, ref normal) - Math.PI / 2.0);
                        //rayList.Add(ray);
                        //this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, true);
                    }
                    break;

                case 3:
                    if (ray == null)
                        break;

                    // 2019.5.30 可能与地形碰撞
                    if (ray.SideFromPoint == null)
                    {
                        rayList.Add(ray);
                        this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, false);
                    }
                    else
                    {
                        // 2017.8.2 直接到达建筑物侧面
                        bool ok1 = Math.Abs(ray.CrossPoint.X - endPoint.X) < 0.5;
                        bool ok2 = Math.Abs(ray.CrossPoint.Y - endPoint.Y) < 0.5;
                        bool ok3 = Math.Abs(ray.CrossPoint.Z - endPoint.Z) < 0.5;
                        if (ok1 && ok2 && ok3)
                        {
                            rayList.Add(ray);  // 先把初级直射线加进来

                            if (this.computeIndoor)
                            {
                                this.rayCount++;
                                //this.rayCountTra++;
                                //this.TransmissionAnalysis(rayList, sourceInfo);
                                this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, false);
                            }

                            // 跟踪后续反射线
                            ReflectedRay refRay = new ReflectedRay(ray);
                            Vector3D inDir = Vector3D.constructVector(originPoint, endPoint);
                            inDir.unit();
                            Vector3D refDir = refRay.ConstructReflectedRay(ref inDir);
                            rayCountRef++;
                            this.rayCountRef1++;
                            this.rayTracing(ray.CrossPoint, refDir, rayList, sourceInfo, trayType, coverageRadius);
                        }
                    }
                    break;

                case 4:
                    if (ray == null)
                        break;

                    // 2019.5.30 可能与地形碰撞
                    if (ray.SideFromPoint == null)
                    {
                        rayList.Add(ray);
                        this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, false);
                    }
                    else
                    {
                        // 2017.8.2 直接到达建筑物棱边
                        bool ok1 = Math.Abs(ray.CrossPoint.X - endPoint.X) < 0.5;
                        bool ok2 = Math.Abs(ray.CrossPoint.Y - endPoint.Y) < 0.5;
                        bool ok3 = Math.Abs(ray.CrossPoint.Z - endPoint.Z) < 0.5;
                        if (ok1 && ok2 && ok3)
                        {
                            rayList.Add(ray);  // 先把初级直射线加进来

                            // 跟踪后续绕射线
                            DiffractedRay diffRay = new DiffractedRay(ray, polygonPoints);
                            List<Vector3D> difDirs;
                            if (trayType == RayType.VDiffraction) // 垂直绕射
                            {
                                if (dis(originPoint, ray.CrossPoint) < coverageRadius)
                                    difDirs = diffRay.DiffractedRay_VerticalSide(originPoint, dir, 5);
                                else
                                    difDirs = diffRay.DiffractedRay_VerticalSide(originPoint, dir, 10);
                            }
                            else
                            {   // 水平绕射
                                if (dis(originPoint, ray.CrossPoint) < coverageRadius)
                                    difDirs = diffRay.DiffractedRay_HorizontalSide(originPoint, dir, 5);
                                else
                                    difDirs = diffRay.DiffractedRay_HorizontalSide(originPoint, dir, 10);
                            }

                            // 递归
                            foreach (var difDir in difDirs)
                            {
                                rayCountDif++;
                                rayCountDif1++;

                                this.rayTracing(ray.CrossPoint, difDir, rayList, sourceInfo, trayType, coverageRadius);
                            }
                        }
                    }
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// 跟踪某种类型的射线传播
        /// </summary>
        /// <param name="originPoint">射线原点</param>
        /// <param name="dir">射线方向</param>
        /// <param name="rayList"></param>
        /// <param name="sourceInfo">小区</param>
        /// <param name="rayType">射线类型</param>
        /// <param name="coverageRadius">小区理论覆盖半径</param>
        public void rayTracing(Point originPoint, Vector3D dir, List<NodeInfo> rayList, SourceInfo sourceInfo, RayType rayType, double coverageRadius)
        {
            if (double.IsNaN(sourceInfo.RayAzimuth))
            {
                return;
            }

            rayCount++;  // 可能大于所有类型射线加起来的数量和,因为最后一次入地没有归入任何类型

            int reflectionCounter, diffractionCounter;

            this.getCurrentLevel(rayList, out reflectionCounter, out diffractionCounter);

            int sumw = diffractionCounter * 3 + reflectionCounter;
            if (sumw >= 5)
            {
                if (dis(rayList[0].PointOfIncidence, rayList[rayList.Count - 1].CrossPoint) < distance * 1.5)
                {
                    this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, false);
                }
                return;
            }

            Grid3D curAccGrid;
            //获取空间直线所经过的栅格
            DDA3D lineCrossGrid = new DDA3D(originPoint, dir);
            NodeInfo ray = null;
            RayType trayType = RayType.Direction;
            List<Point> polygonPoints = null;

            
            do
            {
                curAccGrid = lineCrossGrid.getNextCrossAccGrid();

                if (curAccGrid == null)
                {
                    break;
                }

                // 地形
                ray = this.getInfoOfLineCrossAccGrid(originPoint, dir, curAccGrid, ref polygonPoints, ref trayType);

                if (ray != null)
                {
                    ray.rayType = rayType;
                    break;
                }
            } while (true);

            //射线没有与建筑物相交且射线方向向下
            if (ray == null)
            {
                if (dir.ZComponent < 0)
                {
                    // 计算射线与地面的交点
                    Point planePoint = new Point(200, 200, 0);  // 地面上的某个点
                    Point crossPoint = IntersectPoint.CalTopPlaneLineIntersectPoint(planePoint, originPoint, dir);

                    bool flag = false;
                    if (crossPoint != null)
                        flag = GridHelper.getInstance().checkPointXYZInGrid(crossPoint);

                    #region  二次计算。记录出界的射线，以便再次投入
                    // 判断与地面的交点是否位于当前区域内，如果不是，说明出界了
                    if (flag && this.isRecordReRay)
                    {
                        // 不位于区域内
                        bool ok1 = (p1.X - crossPoint.X) * (p2.Y - crossPoint.Y) - (p2.X - crossPoint.X) * (p1.Y - crossPoint.Y) > 0;
                        bool ok2 = (p2.X - crossPoint.X) * (p3.Y - crossPoint.Y) - (p3.X - crossPoint.X) * (p2.Y - crossPoint.Y) > 0;
                        bool ok3 = (p3.X - crossPoint.X) * (p4.Y - crossPoint.Y) - (p4.X - crossPoint.X) * (p3.Y - crossPoint.Y) > 0;
                        bool ok4 = (p4.X - crossPoint.X) * (p1.Y - crossPoint.Y) - (p1.X - crossPoint.X) * (p4.Y - crossPoint.Y) > 0;
                        if (!(ok1 && ok2 && ok3 && ok4))
                        {
                            #region 计算与区域边界的交点
                            double x1, x2, y1, y2, x3, y3, x4, y4;  // x1 x2 y1 y2 位于射线上  x3 y3 x4 y4 位于区域上
                            double z1, z2;
                            x1 = originPoint.X;
                            y1 = originPoint.Y;
                            z1 = originPoint.Z;
                            x2 = crossPoint.X;
                            y2 = crossPoint.Y;
                            z2 = crossPoint.Z;

                            // 寻找相交边
                            if (!ok1)
                            {
                                x3 = p1.X; y3 = p1.Y;
                                x4 = p2.X; y4 = p2.Y;
                            }
                            else if (!ok2)
                            {
                                x3 = p2.X; y3 = p2.Y;
                                x4 = p3.X; y4 = p3.Y;
                            }
                            else if (!ok3)
                            {
                                x3 = p3.X; y3 = p3.Y;
                                x4 = p4.X; y4 = p4.Y;
                            }
                            else
                            {
                                x3 = p1.X; y3 = p1.Y;
                                x4 = p4.X; y4 = p4.Y;
                            }

                            double den = ((y3 - y4) * (x1 - x2) - (x3 - x4) * (y1 - y2));
                            if (Math.Abs(den) > 0.1)
                            {
                                double t = ((y1 - y3) * (x3 - x4) - (y3 - y4) * (x1 - x3)) / den;
                                Point pt = new Point();
                                pt.X = x1 + (x2 - x1) * t;
                                pt.Y = y1 + (y2 - y1) * t;
                                pt.Z = z1 + (z2 - z1) * t;
                            #endregion

                                // 计算交点处的功率
                                List<NodeInfo> nodes = cloneRaylist(ref rayList);
                                Vector3D normal = new Vector3D(0, 0, 1);
                                ray = new NodeInfo(originPoint, pt, rayType, Vector3D.getAngle(ref dir, ref normal) - Math.PI / 2.0);
                                nodes.Add(ray);
                                double[] ret = this.calcStrength.calcRayStrength(sourceInfo.RayAzimuth, sourceInfo.RayInclination, ref nodes);
                                double pwr = this.calcStrength.convertw2dbm(ret[0]);   // 交点处的功率
                                nodes.Clear();

                                // 记录交点、射线方向、功率
                                if (pwr < 1000 && pwr > -110 && pt.X < 1000000 && pt.X > -1000000)
                                {
                                    ReRay reray = new ReRay(pt, pwr, dir, rayType);
                                    reRays.Add(reray);
                                }
                            }
                        }
                    }
                    #endregion

                    if (flag)
                    {
                        Vector3D normal = new Vector3D(0, 0, 1);
                        ray = new NodeInfo(originPoint, crossPoint, new Point(-1, -1, -1), new Point(-1, -1, -1), -1, 0, null, rayType, Vector3D.getAngle(ref dir, ref normal) - Math.PI / 2.0);
                        rayList.Add(ray);

                        rayList[rayList.Count - 1].CrossPoint.Z = 0;
                        if (dis(rayList[0].PointOfIncidence, rayList[rayList.Count - 1].CrossPoint) < distance * 1.5)
                        {
                            this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, false);
                        }
                        rayList.Remove(ray);
                    }
                }
                return;
            }
            // 2019.5.30 可能与地形碰撞
            else if (ray.SideFromPoint == null)
            {
                rayList.Add(ray);
                this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, false);
                return;
            }

            rayList.Add(ray);

            if (trayType == RayType.HReflection || trayType == RayType.VReflection)
            {
                if (this.computeIndoor)
                {
                    this.rayCount++;
                    this.rayCountTra++;
                    //this.TransmissionAnalysis(rayList, sourceInfo);
                    this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, false);
                }

                ReflectedRay refRay = new ReflectedRay(ray);
                Vector3D refDir = refRay.ConstructReflectedRay(ref dir);  // 反射线方向

                rayCountRef++;
                if (reflectionCounter == 0)
                    rayCountRef1++;
                else if (reflectionCounter == 1)
                    rayCountRef2++;
                else if (reflectionCounter == 2)
                    rayCountRef3++;

                //递归
                this.rayTracing(ray.CrossPoint, refDir, rayList, sourceInfo, trayType, coverageRadius);
            }
            else
            {
                DiffractedRay diffRay = new DiffractedRay(ray, polygonPoints);
                List<Vector3D> difDirs;
                if (trayType == RayType.VDiffraction) // 垂直绕射
                {
                    if (dis(rayList[0].PointOfIncidence, ray.CrossPoint) < coverageRadius)
                        difDirs = diffRay.DiffractedRay_VerticalSide(originPoint, dir, 5);
                    else
                        difDirs = diffRay.DiffractedRay_VerticalSide(originPoint, dir, 10);
                }
                else
                {   // 水平绕射
                    if (dis(rayList[0].PointOfIncidence, ray.CrossPoint) < coverageRadius)
                        difDirs = diffRay.DiffractedRay_HorizontalSide(originPoint, dir, 5);
                    else
                        difDirs = diffRay.DiffractedRay_HorizontalSide(originPoint, dir, 10);
                }

                //递归
                foreach (var difDir in difDirs)
                {
                    rayCountDif++;

                    if (diffractionCounter == 0)  // 也可能是经过反射后生成的初级绕射线
                        this.rayCountDif1++;
                    else if (diffractionCounter == 1)
                        this.rayCountDif2++;

                    this.rayTracing(ray.CrossPoint, difDir, rayList, sourceInfo, trayType, coverageRadius);
                }
            }
            rayList.Remove(ray);
        }


        /// <summary>
        /// 跟踪初级直射线传播
        /// </summary>
        /// <param name="originPoint">射线原点</param>
        /// <param name="endPoint">射线的第一次终点</param>
        /// <param name="rayList"></param>
        /// <param name="sourceInfo">小区</param>
        /// <param name="rayType">射线类型</param>
        /// <param name="type">初级直射线类型，1：连向地面；2：连向楼顶；3：连向可见侧面；4：连向可见棱边</param>
        /// <param name="coverageRadius">小区理论覆盖半径</param>
        public void rayTracingFirstAdj(Point originPoint, Point endPoint, List<NodeInfo> rayList, SourceInfo sourceInfo, RayType rayType, int type, double coverageRadius)
        {
            if (double.IsNaN(sourceInfo.RayAzimuth))
            {
                return;
            }

            this.rayCount++;
            this.rayCountDir++;

            Grid3D curAccGrid;
            Vector3D dir = Vector3D.constructVector(originPoint, endPoint);
            dir.unit();

            //获取空间直线所经过的栅格
            DDA3D lineCrossGrid = new DDA3D(originPoint, dir);
            NodeInfo ray = null;
            RayType trayType = RayType.Direction;
            List<Point> polygonPoints = null;

            int[] scene = new int[scenNum];

            do
            {
                curAccGrid = lineCrossGrid.getNextCrossAccGrid();

                if (curAccGrid == null)
                {
                    break;
                }

                // 2019.3.25 场景记录
                string grid = string.Format("{0},{1},{2}", curAccGrid.gxid, curAccGrid.gyid, curAccGrid.gzid);
                scene[AccelerateStruct.gridScene[grid]]++;

                // 地形
                ray = this.getInfoOfLineCrossAccGrid(originPoint, dir, curAccGrid, ref polygonPoints, ref trayType);

                if (ray != null)
                {
                    ray.rayType = rayType;
                    break;
                }
            } while (true);

            switch (type)
            {
                case 1:
                    // 射线没有与建筑物相交，直接到达地面
                    if (ray == null)
                    {
                        Vector3D normal = new Vector3D(0, 0, 1);
                        this.rayCountDirG++;
                        ray = new NodeInfo(originPoint, endPoint, new Point(-1, -1, -1), new Point(-1, -1, -1), -1, 0, null, rayType, Vector3D.getAngle(ref dir, ref normal) - Math.PI / 2.0);
                        rayScene(ref ray, ref scene);  // 2019.3.25 场景记录
                        rayList.Add(ray);
                        this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, false);
                    }
                    // 2019.5.30 可能与地形碰撞
                    else if (ray.SideFromPoint == null)
                    {
                        rayScene(ref ray, ref scene);  // 2019.3.25 场景记录
                        rayList.Add(ray);
                        this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, false);
                    }
                    // 2017.6.18  穿过建筑物到达地面
                    else
                    {
                        // 2019.5.24 不再计算
                        //Vector3D normal = new Vector3D(0, 0, 1);
                        //ray = new NodeInfo(originPoint, endPoint, new Point(-1, -1, -1), new Point(-1, -1, -1), -1, 0, null, rayType, Vector3D.getAngle(ref dir, ref normal) - Math.PI / 2.0);
                        //rayScene(ref ray, ref scene);  // 2019.3.25 场景记录
                        //rayList.Add(ray);
                        //this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, true);
                    }
                    break;

                case 2:
                    if (ray == null)
                        break;

                    // 2019.5.30 可能与地形碰撞
                    if (ray.SideFromPoint == null)
                    {
                        rayScene(ref ray, ref scene);  // 2019.3.25 场景记录
                        rayList.Add(ray);
                        this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, false);
                    }
                    // 直接到达建筑物顶面
                    else if (Math.Abs(ray.CrossPoint.Z - endPoint.Z) < 0.5)
                    {
                        this.rayCountDirB++;
                        Vector3D normal = new Vector3D(0, 0, 1);

                        // 计算建筑物顶面接收到的场强
                        NodeInfo ray1 = new NodeInfo(originPoint, endPoint, new Point(-1, -1, -1), new Point(-1, -1, -1), -1, 0, null, rayType, Vector3D.getAngle(ref dir, ref normal) - Math.PI / 2.0);
                        rayScene(ref ray1, ref scene);  // 2019.3.25 场景记录
                        rayList.Add(ray1);
                        this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, false);
                        rayList.Remove(ray1);

                        rayScene(ref ray, ref scene);  // 2019.3.25 场景记录
                        rayList.Add(ray);  // 先把初级直射线加进来

                        // 跟踪后续反射线
                        Vector3D inDir = Vector3D.constructVector(originPoint, endPoint);
                        inDir.unit();
                        ReflectedRay refRay = new ReflectedRay(ray);
                        Vector3D refDir = refRay.ConstructReflectedRay(ref inDir);
                        rayCountRef++;
                        this.rayCountRef1++;
                        this.rayTracingAdj(ray.CrossPoint, refDir, rayList, sourceInfo, trayType, coverageRadius);
                    }
                    // 2017.7.4 穿过中间建筑物后到达建筑物顶面
                    else
                    {
                        // 2019.5.24 不再计算
                        //Vector3D normal = new Vector3D(0, 0, 1);
                        //ray = new NodeInfo(originPoint, endPoint, new Point(-1, -1, -1), new Point(-1, -1, -1), -1, 0, null, rayType, Vector3D.getAngle(ref dir, ref normal) - Math.PI / 2.0);
                        //rayScene(ref ray, ref scene);  // 2019.3.25 场景记录
                        //rayList.Add(ray);
                        //this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, true);
                    }
                    break;

                case 3:
                    if (ray == null)
                        break;

                    // 2019.5.30 可能与地形碰撞
                    if (ray.SideFromPoint == null)
                    {
                        rayScene(ref ray, ref scene);  // 2019.3.25 场景记录
                        rayList.Add(ray);
                        this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, false);
                    }
                    else
                    {
                        // 2017.8.2 直接到达建筑物侧面
                        bool ok1 = Math.Abs(ray.CrossPoint.X - endPoint.X) < 0.5;
                        bool ok2 = Math.Abs(ray.CrossPoint.Y - endPoint.Y) < 0.5;
                        bool ok3 = Math.Abs(ray.CrossPoint.Z - endPoint.Z) < 0.5;
                        if (ok1 && ok2 && ok3)
                        {
                            rayScene(ref ray, ref scene);  // 2019.3.25 场景记录
                            rayList.Add(ray);  // 先把初级直射线加进来

                            if (this.computeIndoor)
                            {
                                this.rayCount++;
                                //this.rayCountTra++;
                                //this.TransmissionAnalysis(rayList, sourceInfo);
                                this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, false);
                            }

                            // 跟踪后续反射线
                            ReflectedRay refRay = new ReflectedRay(ray);
                            Vector3D inDir = Vector3D.constructVector(originPoint, endPoint);
                            inDir.unit();
                            Vector3D refDir = refRay.ConstructReflectedRay(ref inDir);
                            rayCountRef++;
                            this.rayCountRef1++;
                            this.rayTracingAdj(ray.CrossPoint, refDir, rayList, sourceInfo, trayType, coverageRadius);
                        }
                    }
                    
                    break;

                case 4:
                    if (ray == null)
                        break;

                    // 2019.5.30 可能与地形碰撞
                    if (ray.SideFromPoint == null)
                    {
                        rayScene(ref ray, ref scene);  // 2019.3.25 场景记录
                        rayList.Add(ray);
                        this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, false);
                    }
                    else
                    {
                        // 2017.8.2 直接到达建筑物棱边
                        bool ok1 = Math.Abs(ray.CrossPoint.X - endPoint.X) < 0.5;
                        bool ok2 = Math.Abs(ray.CrossPoint.Y - endPoint.Y) < 0.5;
                        bool ok3 = Math.Abs(ray.CrossPoint.Z - endPoint.Z) < 0.5;
                        if (ok1 && ok2 && ok3)
                        {
                            rayScene(ref ray, ref scene);  // 2019.3.25 场景记录
                            rayList.Add(ray);  // 先把初级直射线加进来

                            // 跟踪后续绕射线
                            DiffractedRay diffRay = new DiffractedRay(ray, polygonPoints);
                            List<Vector3D> difDirs;
                            if (trayType == RayType.VDiffraction) // 垂直绕射
                            {
                                if (dis(originPoint, ray.CrossPoint) < coverageRadius)
                                    difDirs = diffRay.DiffractedRay_VerticalSide(originPoint, dir, 5);
                                else
                                    difDirs = diffRay.DiffractedRay_VerticalSide(originPoint, dir, 10);
                            }
                            else
                            {   // 水平绕射
                                if (dis(originPoint, ray.CrossPoint) < coverageRadius)
                                    difDirs = diffRay.DiffractedRay_HorizontalSide(originPoint, dir, 5);
                                else
                                    difDirs = diffRay.DiffractedRay_HorizontalSide(originPoint, dir, 10);
                            }

                            // 递归
                            foreach (var difDir in difDirs)
                            {
                                rayCountDif++;
                                rayCountDif1++;

                                this.rayTracingAdj(ray.CrossPoint, difDir, rayList, sourceInfo, trayType, coverageRadius);
                            }
                        }
                    }
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// 跟踪某种类型的射线传播
        /// </summary>
        /// <param name="originPoint">射线原点</param>
        /// <param name="dir">射线方向</param>
        /// <param name="rayList"></param>
        /// <param name="sourceInfo">小区</param>
        /// <param name="rayType">射线类型</param>
        /// <param name="coverageRadius">小区理论覆盖半径</param>
        public void rayTracingAdj(Point originPoint, Vector3D dir, List<NodeInfo> rayList, SourceInfo sourceInfo, RayType rayType, double coverageRadius)
        {
            if (double.IsNaN(sourceInfo.RayAzimuth))
            {
                return;
            }

            rayCount++;  // 可能大于所有类型射线加起来的数量和,因为最后一次入地没有归入任何类型

            int reflectionCounter, diffractionCounter;

            this.getCurrentLevel(rayList, out reflectionCounter, out diffractionCounter);

            int sumw = diffractionCounter * 3 + reflectionCounter;
            if (sumw >= 5)
            {
                if (dis(rayList[0].PointOfIncidence, rayList[rayList.Count - 1].CrossPoint) < distance * 1.5)
                {
                    this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, false);
                }
                return;
            }

            Grid3D curAccGrid;
            //获取空间直线所经过的栅格
            DDA3D lineCrossGrid = new DDA3D(originPoint, dir);
            NodeInfo ray = null;
            RayType trayType = RayType.Direction;
            List<Point> polygonPoints = null;

            int[] scene = new int[scenNum];
            do
            {
                curAccGrid = lineCrossGrid.getNextCrossAccGrid();

                if (curAccGrid == null)
                {
                    break;
                }

                // 2019.3.25 场景记录
                string grid = string.Format("{0},{1},{2}", curAccGrid.gxid, curAccGrid.gyid, curAccGrid.gzid);
                scene[AccelerateStruct.gridScene[grid]]++;

                // 地形
                ray = this.getInfoOfLineCrossAccGrid(originPoint, dir, curAccGrid, ref polygonPoints, ref trayType);

                if (ray != null)
                {
                    ray.rayType = rayType;
                    break;
                }
            } while (true);

            //射线没有与建筑物相交且射线方向向下
            if (ray == null)
            {
                if (dir.ZComponent < 0)
                {
                    // 计算射线与地面的交点
                    Point planePoint = new Point(200, 200, 0);  // 地面上的某个点
                    Point crossPoint = IntersectPoint.CalTopPlaneLineIntersectPoint(planePoint, originPoint, dir);

                    bool flag = false;
                    if (crossPoint != null)
                        flag = GridHelper.getInstance().checkPointXYZInGrid(crossPoint);

                    #region  二次计算。记录出界的射线，以便再次投入
                    // 判断与地面的交点是否位于当前区域内，如果不是，说明出界了
                    if (flag && this.isRecordReRay)
                    {
                        // 不位于区域内
                        bool ok1 = (p1.X - crossPoint.X) * (p2.Y - crossPoint.Y) - (p2.X - crossPoint.X) * (p1.Y - crossPoint.Y) > 0;
                        bool ok2 = (p2.X - crossPoint.X) * (p3.Y - crossPoint.Y) - (p3.X - crossPoint.X) * (p2.Y - crossPoint.Y) > 0;
                        bool ok3 = (p3.X - crossPoint.X) * (p4.Y - crossPoint.Y) - (p4.X - crossPoint.X) * (p3.Y - crossPoint.Y) > 0;
                        bool ok4 = (p4.X - crossPoint.X) * (p1.Y - crossPoint.Y) - (p1.X - crossPoint.X) * (p4.Y - crossPoint.Y) > 0;
                        if (!(ok1 && ok2 && ok3 && ok4))
                        {
                            #region 计算与区域边界的交点
                            double x1, x2, y1, y2, x3, y3, x4, y4;  // x1 x2 y1 y2 位于射线上  x3 y3 x4 y4 位于区域上
                            double z1, z2;
                            x1 = originPoint.X;
                            y1 = originPoint.Y;
                            z1 = originPoint.Z;
                            x2 = crossPoint.X;
                            y2 = crossPoint.Y;
                            z2 = crossPoint.Z;

                            // 寻找相交边
                            if (!ok1)
                            {
                                x3 = p1.X; y3 = p1.Y;
                                x4 = p2.X; y4 = p2.Y;
                            }
                            else if (!ok2)
                            {
                                x3 = p2.X; y3 = p2.Y;
                                x4 = p3.X; y4 = p3.Y;
                            }
                            else if (!ok3)
                            {
                                x3 = p3.X; y3 = p3.Y;
                                x4 = p4.X; y4 = p4.Y;
                            }
                            else
                            {
                                x3 = p1.X; y3 = p1.Y;
                                x4 = p4.X; y4 = p4.Y;
                            }

                            double den = ((y3 - y4) * (x1 - x2) - (x3 - x4) * (y1 - y2));
                            if (Math.Abs(den) > 0.1)
                            {
                                double t = ((y1 - y3) * (x3 - x4) - (y3 - y4) * (x1 - x3)) / den;
                                Point pt = new Point();
                                pt.X = x1 + (x2 - x1) * t;
                                pt.Y = y1 + (y2 - y1) * t;
                                pt.Z = z1 + (z2 - z1) * t;
                            #endregion

                                // 计算交点处的功率
                                List<NodeInfo> nodes = cloneRaylist(ref rayList);
                                Vector3D normal = new Vector3D(0, 0, 1);
                                ray = new NodeInfo(originPoint, pt, rayType, Vector3D.getAngle(ref dir, ref normal) - Math.PI / 2.0);
                                rayScene(ref ray, ref scene);  // 2019.3.25 场景记录
                                nodes.Add(ray);
                                double[] ret = this.calcStrength.calcRayStrength(sourceInfo.RayAzimuth, sourceInfo.RayInclination, ref nodes);
                                double pwr = this.calcStrength.convertw2dbm(ret[0]);   // 交点处的功率
                                nodes.Clear();

                                // 记录交点、射线方向、功率
                                if (pwr < 1000 && pwr > -110 && pt.X < 1000000 && pt.X > -1000000)
                                {
                                    ReRay reray = new ReRay(pt, pwr, dir, rayType);
                                    reRays.Add(reray);
                                }
                            }
                        }
                    }
                    #endregion

                    if (flag)
                    {
                        Vector3D normal = new Vector3D(0, 0, 1);
                        ray = new NodeInfo(originPoint, crossPoint, new Point(-1, -1, -1), new Point(-1, -1, -1), -1, 0, null, rayType, Vector3D.getAngle(ref dir, ref normal) - Math.PI / 2.0);
                        rayScene(ref ray, ref scene);  // 2019.3.25 场景记录
                        rayList.Add(ray);

                        rayList[rayList.Count - 1].CrossPoint.Z = 0;
                        if (dis(rayList[0].PointOfIncidence, rayList[rayList.Count - 1].CrossPoint) < distance * 1.5)
                        {
                            this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, false);
                        }
                        rayList.Remove(ray);
                    }
                }
                return;
            }
            // 2019.5.30 可能与地形碰撞
            else if (ray.SideFromPoint == null)
            {
                rayScene(ref ray, ref scene);  // 2019.3.25 场景记录
                rayList.Add(ray);
                this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, false);
                return;
            }

            rayScene(ref ray, ref scene);  // 2019.3.25 场景记录
            rayList.Add(ray);

            if (trayType == RayType.HReflection || trayType == RayType.VReflection)
            {
                if (this.computeIndoor)
                {
                    this.rayCount++;
                    this.rayCountTra++;
                    //this.TransmissionAnalysis(rayList, sourceInfo);
                    this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, false);
                }

                ReflectedRay refRay = new ReflectedRay(ray);
                Vector3D refDir = refRay.ConstructReflectedRay(ref dir);  // 反射线方向

                rayCountRef++;
                if (reflectionCounter == 0)
                    rayCountRef1++;
                else if (reflectionCounter == 1)
                    rayCountRef2++;
                else if (reflectionCounter == 2)
                    rayCountRef3++;

                //递归
                this.rayTracingAdj(ray.CrossPoint, refDir, rayList, sourceInfo, trayType, coverageRadius);
            }
            else
            {
                DiffractedRay diffRay = new DiffractedRay(ray, polygonPoints);
                List<Vector3D> difDirs;
                if (trayType == RayType.VDiffraction) // 垂直绕射
                {
                    if (dis(rayList[0].PointOfIncidence, ray.CrossPoint) < coverageRadius)
                        difDirs = diffRay.DiffractedRay_VerticalSide(originPoint, dir, 5);
                    else
                        difDirs = diffRay.DiffractedRay_VerticalSide(originPoint, dir, 10);
                }
                else
                {   // 水平绕射
                    if (dis(rayList[0].PointOfIncidence, ray.CrossPoint) < coverageRadius)
                        difDirs = diffRay.DiffractedRay_HorizontalSide(originPoint, dir, 5);
                    else
                        difDirs = diffRay.DiffractedRay_HorizontalSide(originPoint, dir, 10);
                }

                //递归
                foreach (var difDir in difDirs)
                {
                    rayCountDif++;

                    if (diffractionCounter == 0)  // 也可能是经过反射后生成的初级绕射线
                        this.rayCountDif1++;
                    else if (diffractionCounter == 1)
                        this.rayCountDif2++;

                    this.rayTracingAdj(ray.CrossPoint, difDir, rayList, sourceInfo, trayType, coverageRadius);
                }
            }
            rayList.Remove(ray);
        }

        List<NodeInfo> cloneRaylist(ref List<NodeInfo> rays)
        {
            List<NodeInfo> nodes = new List<NodeInfo>();
            for (int i = 0; i < rays.Count; i++)
            {
                NodeInfo node = new NodeInfo(rays[i].Distance, rays[i].rayType, rays[i].Angle);
                nodes.Add(node);
            }
            return nodes;
        }

        /// <summary>
        /// 跟踪某种类型的射线传播
        /// </summary>
        /// <param name="originPoint">射线原点</param>
        /// <param name="dir">射线方向</param>
        /// <param name="rayList"></param>
        /// <param name="sourceInfo">小区</param>
        /// <param name="rayType">射线类型</param>
        /// <param name="coverageRadius">小区理论覆盖半径</param>
        public void rayTracing(Point originPoint, Vector3D dir, List<NodeInfo> rayList, RayType rayType, double emitPwr)
        {
            rayCount++;  // 可能大于所有类型射线加起来的数量和,因为最后一次入地没有归入任何类型

            int reflectionCounter, diffractionCounter;

            this.getCurrentLevel(rayList, out reflectionCounter, out diffractionCounter);

            int sumw = diffractionCounter * 3 + reflectionCounter;
            if (sumw >= 5)
            {
                if (dis(rayList[0].PointOfIncidence, rayList[rayList.Count - 1].CrossPoint) < distance * 1.5)
                {
                    this.CalcOutDoorRayStrength(rayList, emitPwr);
                }
                return;
            }

            Grid3D curAccGrid;
            //获取空间直线所经过的栅格
            DDA3D lineCrossGrid = new DDA3D(originPoint, dir);
            NodeInfo ray = null;
            RayType trayType = RayType.Direction;
            List<Point> polygonPoints = null;

            int round = 0;

            do
            {
                //0ms
                ++round;
                curAccGrid = lineCrossGrid.getNextCrossAccGrid();

                /////
                if (curAccGrid == null)
                {
                    break;
                }

                //t3 = DateTime.Now;
                ray = this.getInfoOfLineCrossAccGrid(originPoint, dir, curAccGrid, ref polygonPoints, ref trayType);

                if (ray != null)
                {
                    //MessageBox.Show(string.Format("ray {0},{1},{2},{3}", ray.buildingID, ray.CrossPoint.X, ray.CrossPoint.Y, ray.CrossPoint.Z));
                    ray.rayType = rayType;
                    break;
                }
            } while (true);

            //射线没有与建筑物相交且射线方向向下
            if (ray == null)
            {
                if (dir.ZComponent < 0)
                {
                    // 计算射线与地面的交点
                    Point planePoint = new Point(200, 200, 0);  // 地面上的某个点
                    Point crossPoint = IntersectPoint.CalTopPlaneLineIntersectPoint(planePoint, originPoint, dir);

                    bool flag = false;
                    if (crossPoint != null)
                        flag = GridHelper.getInstance().checkPointXYZInGrid(crossPoint);

                    if (flag)
                    {
                        Vector3D normal = new Vector3D(0, 0, 1);
                        ray = new NodeInfo(originPoint, crossPoint, new Point(-1, -1, -1), new Point(-1, -1, -1), -1, 0, null, rayType, Vector3D.getAngle(ref dir, ref normal) - Math.PI / 2.0);
                        rayList.Add(ray);

                        rayList[rayList.Count - 1].CrossPoint.Z = 0;
                        this.CalcOutDoorRayStrength(rayList, emitPwr);

                        rayList.Remove(ray);
                    }
                }
                return;
            }

            rayList.Add(ray);

            if (trayType == RayType.HReflection || trayType == RayType.VReflection)
            {
                if (this.computeIndoor)
                {
                    this.rayCount++;
                    this.rayCountTra++;
                    this.TransmissionAnalysis(rayList, sourceInfo);
                }
                else
                    this.CalcOutDoorRayStrength(rayList, sourceInfo.RayAzimuth, sourceInfo.RayInclination, true);

                ReflectedRay refRay = new ReflectedRay(ray);
                Vector3D refDir = refRay.ConstructReflectedRay(ref dir);  // 反射线方向

                rayCountRef++;
                if (reflectionCounter == 0)
                    rayCountRef1++;
                else if (reflectionCounter == 1)
                    rayCountRef2++;
                else if (reflectionCounter == 2)
                    rayCountRef3++;

                //递归
                this.rayTracing(ray.CrossPoint, refDir, rayList, trayType, emitPwr);
            }
            else
            {
                DiffractedRay diffRay = new DiffractedRay(ray, polygonPoints);
                List<Vector3D> difDirs;
                if (trayType == RayType.VDiffraction) // 垂直绕射
                {
                    difDirs = diffRay.DiffractedRay_VerticalSide(originPoint, dir, 10);
                }
                else
                {   // 水平绕射
                    difDirs = diffRay.DiffractedRay_HorizontalSide(originPoint, dir, 10);
                }

                //递归
                foreach (var difDir in difDirs)
                {
                    rayCountDif++;

                    if (diffractionCounter == 0)  // 也可能是经过反射后生成的初级绕射线
                        this.rayCountDif1++;
                    else if (diffractionCounter == 1)
                        this.rayCountDif2++;

                    this.rayTracing(ray.CrossPoint, difDir, rayList, trayType, emitPwr);
                }
            }
            rayList.Remove(ray);
        }

        // 2018.12.04
        /// <summary>
        /// 计算室外覆盖场强，beam
        /// </summary>
        /// <param name="rays"></param>
        /// <param name="rayAzimuth">初始射线方位角</param>
        /// <param name="rayInclination">初始射线下倾角</param>
        /// <param name="isT">是否为初始直射线与建筑物相交</param>
        public void CalcOutDoorRayStrengthBeam(ref List<Beam.NodeInfo> rays, double rayAzimuth, double rayInclination)
        {

            //Console.WriteLine("rays[rays.Count - 1].CrossPoint.Z = 0 ");
            this.recRayCounter++;
            this.calcStrength.CalcAndMergeGGridStrengthBeam(rayAzimuth, rayInclination, ref rays);
        }

        /// <summary>
        /// 计算室外覆盖场强
        /// </summary>
        /// <param name="rays"></param>
        /// <param name="rayAzimuth">初始射线方位角</param>
        /// <param name="rayInclination">初始射线下倾角</param>
        /// <param name="isT">是否为初始直射线与建筑物相交</param>
        public void CalcOutDoorRayStrength(List<NodeInfo> rays, double rayAzimuth, double rayInclination, bool isT)
        {
            if (rays.Count == 0)//|| rays[rays.Count - 1].CrossPoint.Z > 0)
            {
                return;
            }

            //Console.WriteLine("rays[rays.Count - 1].CrossPoint.Z = 0 ");
            this.recRayCounter++;
            this.calcStrength.CalcAndMergeGGridStrength(rayAzimuth, rayInclination, rays, isT);
        }

        /// <summary>
        /// 计算室外覆盖场强，二次投射
        /// </summary>
        /// <param name="rays"></param>
        /// <param name="rayAzimuth">初始射线方位角</param>
        /// <param name="rayInclination">初始射线下倾角</param>
        /// <param name="isT">是否为初始直射线与建筑物相交</param>
        public void CalcOutDoorRayStrength(List<NodeInfo> rays, double emitPwr)
        {
            if (rays.Count == 0)//|| rays[rays.Count - 1].CrossPoint.Z > 0)
            {
                return;
            }

            //Console.WriteLine("rays[rays.Count - 1].CrossPoint.Z = 0 ");
            this.recRayCounter++;
            this.calcStrength.CalcAndMergeGGridStrength(rays, emitPwr);
        }

        /// <summary>
        /// 透射线跟踪计算
        /// </summary>
        /// <param name="rays"></param>
        /// <param name="sourceInfo"></param>
        public void TransmissionAnalysis(List<NodeInfo> rays, SourceInfo sourceInfo)
        {
            NodeInfo ray = rays.Last();
            double azimuth, inclination;
            if (rays.Count == 1)
            {
                azimuth = sourceInfo.RayAzimuth;
                inclination = sourceInfo.RayInclination;
            }
            else
            {
                GeometricUtilities.getAzimuth_Inclination(ray.PointOfIncidence, ray.CrossPoint, out azimuth, out inclination);
            }

            if (double.IsNaN(azimuth))
            {
                return;
            }

            Point innerstart = ray.CrossPoint.clone(), innerend;
            int floor = (int)Math.Ceiling(innerstart.Z / GridHelper.getInstance().getGHeight());

            if (inclination > 1)//往下打
            {
                innerend = GeometricUtilities.getRayCrossPointWithPlane(innerstart, azimuth, inclination, (floor - 1) * GridHelper.getInstance().getGHeight());
            }
            else if (inclination < -1)//往上打
            {
                innerend = GeometricUtilities.getRayCrossPointWithPlane(innerstart, azimuth, 0 - inclination, floor * GridHelper.getInstance().getGHeight());
            }
            else//平打
            {
                //构造一个结束点
                double arithmeticAzimuth = GeometricUtilities.ConvertGeometricArithmeticAngle(azimuth);
                innerend = GeometricUtilities.getPointByDegreeDistance(innerstart, arithmeticAzimuth, 500);
            }
            this.calcStrength.CalcAndMergeBGridStrength(azimuth, inclination, rays, innerstart, innerend);
        }

        //------------------------------------单射线开始-------------------------------
        /// <summary>
        /// 单条射线追踪
        /// </summary>
        /// <param name="singleRay">指定方向</param>
        public void SingleRayAnalysis(FrmSingleRayTracing2 singleRay)
        {
            this.sourceInfo.RayAzimuth = singleRay.direction;
            this.sourceInfo.RayInclination = singleRay.inclination;
            Point crossWithGround = GeometricUtilities.getCrossedPoint_Ray_Ground(this.sourceInfo.SourcePoint, singleRay.direction, singleRay.inclination);

            #region 加速结构
            //------------------------生成加速结构开始-------------------------------------------------
            Grid3D accgrid = new Grid3D(), ggrid = new Grid3D();

            CellInfo cellInfo = this.sourceInfo;
            double distance = 800;  // 小区半径
            // 返回空间点(大地坐标)所在的加速网格坐标
            if (!GridHelper.getInstance().PointXYZToAccGrid(cellInfo.SourcePoint, ref accgrid))
            {
                Console.WriteLine("无法获取小区所在加速网格坐标，计算结束！");
                return;
            }

            //建筑物信息加速
            // 空间点（大地坐标）所在的立体网格
            if (!GridHelper.getInstance().PointXYZToGrid3D(cellInfo.SourcePoint, ref ggrid))
            {
                Console.WriteLine("无法获取小区所在地面网格坐标，计算结束！");
                return;
            }

            int mingxid = -1, maxgxid = -1, mingyid = -1, maxgyid = -1;
            int gridlength = GridHelper.getInstance().getAGridSize();
            int deltagrid = (int)Math.Ceiling(distance * 1.5 / gridlength);

            int maxAGXID = 0, maxAGYID = 0, minAGXID = 0, minAGYID = 0;
            GridHelper.getInstance().getMaxAccGridXY(ref maxAGXID, ref maxAGYID);
            GridHelper.getInstance().getMinAccGridXY(ref minAGXID, ref minAGYID);
            mingxid = Math.Max(minAGXID, accgrid.gxid - deltagrid);
            mingyid = Math.Max(minAGYID, accgrid.gyid - deltagrid);
            maxgxid = Math.Min(maxAGXID, accgrid.gxid + deltagrid);
            maxgyid = Math.Min(maxAGYID, accgrid.gyid + deltagrid);
            //mingxid = accgrid.gxid - deltagrid;
            //mingyid = accgrid.gyid - deltagrid;
            //maxgxid = accgrid.gxid + deltagrid;
            //maxgyid = accgrid.gyid + deltagrid;

            AccelerateStruct.clearAccelerateStruct();
            BuildingGrid3D.clearBuildingData();
            BuildingGrid3D.clearBuildingVertexOriginal();
            BuildingGrid3D.clearGrid3D();
            AccelerateStruct.setAccGridRange(mingxid, mingyid, maxgxid, maxgyid);

            // 从数据库表tbAccelerateGridBuilding中取出所有符合条件的数据,并以GXID,GYID,GZID排序, 构造结果集的哈希表
            AccelerateStruct.constructAccelerateStruct();
            //MessageBox.Show("AccelerateStruct.constructAccelerateStruct()");
            gridlength = GridHelper.getInstance().getGGridSize();
            deltagrid = (int)Math.Ceiling(distance * 1.5 / gridlength);
            int maxGXID = 0, maxGYID = 0, minGXID = 0, minGYID = 0;
            GridHelper.getInstance().getMaxGGridXY(ref maxGXID, ref maxGYID);
            GridHelper.getInstance().getMinGGridXY(ref minGXID, ref minGYID);
            mingxid = Math.Max(minGXID, ggrid.gxid - deltagrid);
            mingyid = Math.Max(minGYID, ggrid.gyid - deltagrid);
            maxgxid = Math.Min(maxGXID, ggrid.gxid + deltagrid);
            maxgyid = Math.Min(maxGYID, ggrid.gyid + deltagrid);
            //mingxid = ggrid.gxid - deltagrid;
            //mingyid = ggrid.gyid - deltagrid;
            //maxgxid = ggrid.gxid + deltagrid;
            //maxgyid = ggrid.gyid + deltagrid;

            BuildingGrid3D.setGGridRange(mingxid, mingyid, maxgxid, maxgyid);
            // 构建建筑物底面中心点、高度数据、所有点
            BuildingGrid3D.constructBuildingData();
            //MessageBox.Show("BuildingGrid3D.constructBuildingData()");

            // 平滑处理
            BuildingGrid3D.constructBuildingVertexOriginal();
            //MessageBox.Show("BuildingGrid3D.constructBuildingVertexOriginal()");

            deltagrid = (int)Math.Ceiling(deltagrid / 1.5);
            mingxid = Math.Max(minGXID, ggrid.gxid - deltagrid);
            mingyid = Math.Max(minGYID, ggrid.gyid - deltagrid);
            maxgxid = Math.Min(maxGXID, ggrid.gxid + deltagrid);
            maxgyid = Math.Min(maxGYID, ggrid.gyid + deltagrid);
            //mingxid = ggrid.gxid - deltagrid;
            //mingyid = ggrid.gyid - deltagrid;
            //maxgxid = ggrid.gxid + deltagrid;
            //maxgyid = ggrid.gyid + deltagrid;
            GroundGrid.setBound(mingxid, mingyid, maxgxid, maxgyid);
            //Console.Write("{0} {1} {2} {3}", ggrid.gxid - deltagrid, ggrid.gyid - deltagrid, ggrid.gxid + deltagrid, ggrid.gyid + deltagrid);
            //MessageBox.Show("GroundGrid.setBound");

            // 获取中心点在范围内的地面栅格中心点
            if (GroundGrid.constructGGrids() == 0)
            {
                System.Environment.Exit(0);
            }
            //------------------------生成加速结构结束-------------------------------------------------
            #endregion

            Vector3D dir = Vector3D.constructVector(this.sourceInfo.SourcePoint, crossWithGround);
            dir.unit();
            List<NodeInfo> rayList = new List<NodeInfo>();
            List<Grid3D> DDA = new List<Grid3D>();
            if (this.scenNum == 0)
            {
                this.SingleRayAnalysis(this.sourceInfo.SourcePoint, dir, ref rayList, RayType.Direction, ref DDA);
            }
            else
            {
                this.SingleRayAnalysisAdj(this.sourceInfo.SourcePoint, dir, ref rayList, RayType.Direction, ref DDA);
            }

            if (rayList.Count > 0)
            {
                CalcGridStrength calcStrength = new CalcGridStrength(sourceInfo);
                double[] recv = calcStrength.calcRayStrength(sourceInfo.RayAzimuth, sourceInfo.RayInclination, ref rayList);
                double recvPwrDbm = calcStrength.convertw2dbm(recv[0]);
                double pathLoss = recvPwrDbm - calcStrength.calcDbmPt(sourceInfo.RayAzimuth, sourceInfo.RayInclination);
                singleRay.AddRow(singleRay.cellName, sourceInfo.EIRP, recv[1], recvPwrDbm, pathLoss);
            }

            //画反射线 
            List<ESRI.ArcGIS.Geometry.IPoint> linePoints = new List<ESRI.ArcGIS.Geometry.IPoint>();
            ESRI.ArcGIS.Geometry.IPoint p = GeometryUtilities.ConstructPoint3D(sourceInfo.SourcePoint.X, sourceInfo.SourcePoint.Y, sourceInfo.SourcePoint.Z);
            linePoints.Add(p);  // p为三维点
            for (int i = 0; i < rayList.Count; i++)
            {
                if (rayList[i].rayType == RayType.HReflection || rayList[i].rayType == RayType.VReflection || i == 0)
                {
                    p = GeometryUtilities.ConstructPoint3D(rayList[i].CrossPoint.X, rayList[i].CrossPoint.Y, rayList[i].CrossPoint.Z);
                    linePoints.Add(p);
                }
            }
            IGraphicsLayer pGraphicsLayer = (GISMapApplication.Instance.Scene as IBasicMap).BasicGraphicsLayer;
            DrawUtilities.DrawLine(pGraphicsLayer as IGraphicsContainer3D, linePoints);
            GISMapApplication.Instance.RefreshLayer(pGraphicsLayer);
        }

        /// <summary>
        /// 单条射线追踪
        /// </summary>
        public void SinglePrepare1(Point originPoint, double distance)
        {
            #region 加速结构
            //------------------------生成加速结构开始-------------------------------------------------
            Grid3D accgrid = new Grid3D(), ggrid = new Grid3D();

            CellInfo cellInfo = this.sourceInfo;
            // 返回空间点(大地坐标)所在的加速网格坐标
            if (!GridHelper.getInstance().PointXYZToAccGrid(originPoint, ref accgrid))
            {
                Console.WriteLine("无法获取小区所在加速网格坐标，计算结束！");
                return;
            }

            //建筑物信息加速
            // 空间点（大地坐标）所在的立体网格
            if (!GridHelper.getInstance().PointXYZToGrid3D(originPoint, ref ggrid))
            {
                Console.WriteLine("无法获取小区所在地面网格坐标，计算结束！");
                return;
            }

            int mingxid = -1, maxgxid = -1, mingyid = -1, maxgyid = -1;
            int gridlength = GridHelper.getInstance().getAGridSize();
            int deltagrid = (int)Math.Ceiling(distance / gridlength);

            int maxAGXID = 0, maxAGYID = 0, minAGXID = 0, minAGYID = 0;
            GridHelper.getInstance().getMaxAccGridXY(ref maxAGXID, ref maxAGYID);
            GridHelper.getInstance().getMinAccGridXY(ref minAGXID, ref minAGYID);
            mingxid = Math.Max(minAGXID, accgrid.gxid - deltagrid);
            mingyid = Math.Max(minAGYID, accgrid.gyid - deltagrid);
            maxgxid = Math.Min(maxAGXID, accgrid.gxid + deltagrid);
            maxgyid = Math.Min(maxAGYID, accgrid.gyid + deltagrid);
            //mingxid = accgrid.gxid - deltagrid;
            //mingyid = accgrid.gyid - deltagrid;
            //maxgxid = accgrid.gxid + deltagrid;
            //maxgyid = accgrid.gyid + deltagrid;

            AccelerateStruct.clearAccelerateStruct();
            BuildingGrid3D.clearBuildingData();
            BuildingGrid3D.clearBuildingVertexOriginal();
            BuildingGrid3D.clearGrid3D();

            AccelerateStruct.setAccGridRange(mingxid, mingyid, maxgxid, maxgyid);

            // 从数据库表tbAccelerateGridBuilding中取出所有符合条件的数据,并以GXID,GYID,GZID排序, 构造结果集的哈希表
            AccelerateStruct.constructAccelerateStruct();
            //MessageBox.Show("AccelerateStruct.constructAccelerateStruct()");
            gridlength = GridHelper.getInstance().getGGridSize();
            deltagrid = (int)Math.Ceiling(distance / gridlength);
            int maxGXID = 0, maxGYID = 0, minGXID = 0, minGYID = 0;
            GridHelper.getInstance().getMaxGGridXY(ref maxGXID, ref maxGYID);
            GridHelper.getInstance().getMinGGridXY(ref minGXID, ref minGYID);
            mingxid = ggrid.gxid - deltagrid;
            mingyid = ggrid.gyid - deltagrid;
            maxgxid = ggrid.gxid + deltagrid;
            maxgyid = ggrid.gyid + deltagrid;

            BuildingGrid3D.setGGridRange(mingxid, mingyid, maxgxid, maxgyid);
            // 构建建筑物底面中心点、高度数据、所有点
            BuildingGrid3D.constructBuildingData();
            //------------------------生成加速结构结束-------------------------------------------------
            #endregion

            //SingleRayJudge(originPoint, dir, ref dic);
        }

        /// <summary>
        /// </summary>
        /// <param name="originPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="singleRay"></param>
        /// <param name="featureID"></param>
        public bool SingleRayJudge1(Point originPoint, Vector3D dir)
        {
            int maxAx = 0, maxAy = 0;
            GridHelper.getInstance().getMaxAccGridXY(ref maxAx, ref maxAy);

            Grid3D curAccGrid;

            //获取空间直线所经过的栅格
            DDA3D lineCrossGrid = new DDA3D(originPoint, dir);
            RayType trayType = RayType.Direction;

            do
            {
                curAccGrid = lineCrossGrid.getNextCrossAccGrid();
                /////
                if (curAccGrid == null)
                {
                    return true;
                }

                //t3 = DateTime.Now;
                if (this.getInfoOfLineCrossAccGrid1(originPoint, dir, curAccGrid, ref trayType))
                    return false;
            } while (curAccGrid.gxid < maxAx && curAccGrid.gxid >= 0 && curAccGrid.gyid < maxAy && curAccGrid.gyid >= 0 && curAccGrid.gzid < 4 && curAccGrid.gzid >= 0);

            return true;  // 到达地面
        }

        /// <summary>
        /// 单条射线追踪
        /// </summary>
        public void SinglePrepare(double leftBound, double rightBound, double downBound, double upBound)
        {
            #region 加速结构
            //------------------------生成加速结构开始-------------------------------------------------
            Grid3D accgrid = new Grid3D(), ggrid = new Grid3D();

            int minagxid = -1, maxagxid = -1, minagyid = -1, maxagyid = -1, gzid = 0;
            GridHelper.getInstance().XYZToAccGrid(leftBound, downBound, 0, ref minagxid, ref minagyid, ref gzid);
            GridHelper.getInstance().XYZToAccGrid(rightBound, upBound, 0, ref maxagxid, ref maxagyid, ref gzid);

            int maxAGXID = 0, maxAGYID = 0, minAGXID = 0, minAGYID = 0;
            GridHelper.getInstance().getMaxAccGridXY(ref maxAGXID, ref maxAGYID);
            GridHelper.getInstance().getMinAccGridXY(ref minAGXID, ref minAGYID);
            minagxid = Math.Max(minAGXID, minagxid);
            minagyid = Math.Max(minAGYID, minagyid);
            maxagxid = Math.Min(maxAGXID, maxagxid);
            maxagyid = Math.Min(maxAGYID, maxagyid);

            AccelerateStruct.clearAccelerateStruct();
            BuildingGrid3D.clearBuildingData();
            BuildingGrid3D.clearBuildingVertexOriginal();
            BuildingGrid3D.clearGrid3D();

            AccelerateStruct.setAccGridRange(minagxid, minagyid, maxagxid, maxagyid);

            // 从数据库表tbAccelerateGridBuilding中取出所有符合条件的数据,并以GXID,GYID,GZID排序, 构造结果集的哈希表
            AccelerateStruct.constructAccelerateStruct();

            int mingxid = -1, maxgxid = -1, mingyid = -1, maxgyid = -1;
            int maxGXID = 0, maxGYID = 0, minGXID = 0, minGYID = 0;
            GridHelper.getInstance().getMaxGGridXY(ref maxGXID, ref maxGYID);
            GridHelper.getInstance().getMinGGridXY(ref minGXID, ref minGYID);
            GridHelper.getInstance().XYToGGrid(leftBound, downBound, ref mingxid, ref mingyid);
            GridHelper.getInstance().XYToGGrid(rightBound, upBound, ref maxgxid, ref maxgyid);
            mingxid = Math.Max(minGXID, mingxid);
            mingyid = Math.Max(minGYID, mingyid);
            maxgxid = Math.Min(maxGXID, maxgxid);
            maxgyid = Math.Min(maxGYID, maxgyid);

            BuildingGrid3D.setGGridRange(mingxid, mingyid, maxgxid, maxgyid);
            // 构建建筑物底面中心点、高度数据、所有点
            BuildingGrid3D.constructBuildingData();
            //MessageBox.Show("BuildingGrid3D.constructBuildingData()");

            // 平滑处理
            //BuildingGrid3D.constructBuildingVertexOriginal();
            //MessageBox.Show("BuildingGrid3D.constructBuildingVertexOriginal()");

            //deltagrid = (int)Math.Ceiling(deltagrid / 1.5);
            //mingxid = Math.Max(minGXID, ggrid.gxid - deltagrid);
            //mingyid = Math.Max(minGYID, ggrid.gyid - deltagrid);
            //maxgxid = Math.Min(maxGXID, ggrid.gxid + deltagrid);
            //maxgyid = Math.Min(maxGYID, ggrid.gyid + deltagrid);
            ////mingxid = ggrid.gxid - deltagrid;
            ////mingyid = ggrid.gyid - deltagrid;
            ////maxgxid = ggrid.gxid + deltagrid;
            ////maxgyid = ggrid.gyid + deltagrid;
            //GroundGrid.setBound(mingxid, mingyid, maxgxid, maxgyid);
            //Console.Write("{0} {1} {2} {3}", ggrid.gxid - deltagrid, ggrid.gyid - deltagrid, ggrid.gxid + deltagrid, ggrid.gyid + deltagrid);
            //MessageBox.Show("GroundGrid.setBound");

            // 获取中心点在范围内的地面栅格中心点
            //if (GroundGrid.constructGGrids() == 0)
            //{
            //    System.Environment.Exit(0);
            //}
            //------------------------生成加速结构结束-------------------------------------------------
            #endregion

            //SingleRayJudge(originPoint, dir, ref dic);
        }

        /// <summary>
        /// 根据加速网格和空间直线获取相交射线
        /// </summary>
        /// <param name="origin">射线起点</param>
        /// <param name="dir">射线方向</param>
        /// <param name="grid"></param>
        /// <param name="buildingPolygon">绕射所需</param>
        /// <returns></returns>
        public bool getInfoOfLineCrossAccGrid1(Point origin, Vector3D dir, Grid3D grid, ref RayType rayType)
        {
            if (grid == null) return false;

            //DateTime t1, t2, t3, t4;
            List<int> buildingids = AccelerateStruct.getAccelerateStruct(grid.gxid, grid.gyid, grid.gzid);

            if (buildingids == null)
                return false;
            int bcnt = buildingids.Count;
            if (bcnt == 0)
                return false;

            //加速栅格内建筑物按到入射点的距离从小到大排序
            Dictionary<int, double> ID_Distance = new Dictionary<int, double>();
            for (int i = 0; i < bcnt; i++)
            {
                Point centroid = BuildingGrid3D.getBuildingCenter(buildingids[i]);
                if (centroid == null)
                {
                    continue;
                }
                ID_Distance.Add(buildingids[i], GeometricUtilities.GetDistanceOf2DPoints(origin, centroid));
            }

            //从近到远依次遍历建筑物，计算射线
            var keyvalues = from keyvalue in ID_Distance orderby keyvalue.Value ascending select keyvalue;
            NodeInfo ray = null;
            foreach (var keyvalue in keyvalues)
            {
                //if (keyvalue.Key == 3250)
                //    Console.WriteLine("1");
                List<Point> polygonPoints = BuildingGrid3D.getBuildingVertex(keyvalue.Key);
                if (polygonPoints == null || polygonPoints.Count < 2)
                    continue;

                double buildingHeight = BuildingGrid3D.getBuildingHeight(keyvalue.Key);

                Point crossWithTop, crossWithSidePlane;
                ray = null;
                int topEdgeIndex = -1;

                //t3 = DateTime.Now;
                //如果入射点比建筑物低，则不可能与建筑物顶面内有交点，否则计算可能存在的交点
                crossWithTop = (origin.Z - buildingHeight < 0) ? null : this.getTopPlaneLineIntersectPoint(origin, dir, polygonPoints, buildingHeight, ref rayType, ref topEdgeIndex);
                //如果不存在顶面交点，再计算可能存在的侧面交点
                if (crossWithTop == null)
                {
                    Vector3D normal;  // 侧面法向
                    List<Point> crossEdge = this.getCorssedEdge(origin, dir, polygonPoints, out normal);

                    if (crossEdge != null && crossEdge.Count == 2)
                    {
                        double altitude = BuildingGrid3D.getBuildingHeight(keyvalue.Key);  // 地形
                        this.getSidePlanePoint(origin, dir, crossEdge[0], crossEdge[1], normal, buildingHeight, altitude, out crossWithSidePlane, ref rayType);

                        if (crossWithSidePlane != null && GridHelper.getInstance().checkPointXYZInGrid(crossWithSidePlane) && !PointComparer.Equals1(origin, crossWithSidePlane))//排除入射点
                        {
                            return true;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="originPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="singleRay"></param>
        /// <param name="featureID"></param>
        public void SingleRayJudge(Point originPoint, Vector3D dir, ref Dictionary<string, int> dic, bool flag)//, int minAx, int maxAx, int minAy, int maxAy)
        {
            int maxAx = 0, maxAy = 0;
            GridHelper.getInstance().getMaxAccGridXY(ref maxAx, ref maxAy);

            Grid3D curAccGrid;

            //获取空间直线所经过的栅格
            DDA3D lineCrossGrid = new DDA3D(originPoint, dir);
            bool collide = false;
            NodeInfo ray = null;
            RayType trayType = RayType.Direction;
            List<Point> polygonPoints = null;
            bool first = false;
            dir.unit();

            do
            {
                curAccGrid = lineCrossGrid.getNextCrossAccGrid();
                /////
                if (curAccGrid == null)
                {
                    return;
                }

                //t3 = DateTime.Now;
                NodeInfo rayTmp = this.getInfoOfLineCrossAccGrid(originPoint, dir, curAccGrid, ref polygonPoints, ref trayType);
                if (first)
                {
                    first = true;
                    ray = rayTmp;
                }
                //bool ok = this.getInfoOfLineCrossAccGrid1(originPoint, dir, curAccGrid, ref trayType);
                if (rayTmp != null)
                {
                    collide = true;
                }

                // 如果已经发生碰撞，
                // 如果是强点发出的射线，则之后经过的栅格分值-1
                // 如果是弱点发出的射线，则之后经过的栅格分值+1
                if (collide)
                {
                    string key = String.Format("{0},{1},{2}", curAccGrid.gxid, curAccGrid.gyid, curAccGrid.gzid);
                    if (flag)  // 强信号点
                    {
                        if (dic.ContainsKey(key))
                            dic[key]--;
                        else
                            dic[key] = -1;
                    }
                    else
                    {
                        if (dic.ContainsKey(key))
                            dic[key]++;
                        else
                            dic[key] = 1;
                    }
                }
                // 如果到目前为止，没有发生碰撞，
                // 如果是强点发出的射线，则之后经过的栅格分值+1
                // 如果是弱点发出的射线，则之后经过的栅格分值-1
                else
                {
                    string key = String.Format("{0},{1},{2}", curAccGrid.gxid, curAccGrid.gyid, curAccGrid.gzid);
                    if (flag)  // 强信号点
                    {
                        if (dic.ContainsKey(key))
                            dic[key]++;
                        else
                            dic[key] = 1;
                    }
                    else
                    {
                        if (dic.ContainsKey(key))
                            dic[key]--;
                        else
                            dic[key] = -1;
                    }
                }

            } while (curAccGrid.gxid < maxAx && curAccGrid.gxid >= 0 && curAccGrid.gyid < maxAy && curAccGrid.gyid >= 0 && curAccGrid.gzid < 4 && curAccGrid.gzid >= 1);

            // 跟踪反射线
            // 对于弱信号点，可假设是由反射/绕射形成的，所以对于反射线经过的格子，分值加1
            if (!flag) // 弱信号点
            {
                if (ray != null && (trayType == RayType.HReflection || trayType == RayType.VReflection))
                {
                    ReflectedRay refRay = new ReflectedRay(ray);
                    Vector3D refDir = refRay.ConstructReflectedRay(ref dir);  // 反射线方向
                    refDir.unit();

                    //递归
                    int level = 0;
                    this.SingleRayJudgeNext(ray.CrossPoint, refDir, ref dic, trayType, ref level);
                }
            }
        }

        public void SingleRayJudgeNext(Point originPoint, Vector3D dir, ref Dictionary<string, int> dic, RayType trayType, ref int level)//, int minAx, int maxAx, int minAy, int maxAy)
        {
            int maxAx = 0, maxAy = 0;
            GridHelper.getInstance().getMaxAccGridXY(ref maxAx, ref maxAy);

            Grid3D curAccGrid;

            //获取空间直线所经过的栅格
            DDA3D lineCrossGrid = new DDA3D(originPoint, dir);
            NodeInfo ray = null;
            //RayType trayType = RayType.Direction;
            List<Point> polygonPoints = null;

            do
            {
                curAccGrid = lineCrossGrid.getNextCrossAccGrid();
                /////
                if (curAccGrid == null)
                {
                    return;
                }

                //t3 = DateTime.Now;
                ray = this.getInfoOfLineCrossAccGrid(originPoint, dir, curAccGrid, ref polygonPoints, ref trayType);

                if (ray == null)
                {
                    string key = String.Format("{0},{1},{2}", curAccGrid.gxid, curAccGrid.gyid, curAccGrid.gzid);
                    if (dic.ContainsKey(key))
                        dic[key]++;
                    else
                        dic[key] = 1;
                }
                else  // 如果发生碰撞
                {
                    ++level;
                    if (level >= 2)
                        return;
                }

            } while (curAccGrid.gxid < maxAx && curAccGrid.gxid >= 0 && curAccGrid.gyid < maxAy && curAccGrid.gyid >= 0 && curAccGrid.gzid < 4 && curAccGrid.gzid >= 1);

            // 跟踪反射线
            // 对于弱信号点，可假设是由反射/绕射形成的，所以对于反射线经过的格子，分值加1
            if (ray != null && (trayType == RayType.HReflection || trayType == RayType.VReflection))
            {
                ReflectedRay refRay = new ReflectedRay(ray);
                Vector3D refDir = refRay.ConstructReflectedRay(ref dir);  // 反射线方向
                refDir.unit();

                //递归
                this.SingleRayJudgeNext(ray.CrossPoint, refDir, ref dic, trayType, ref level);
            }
        }

        /// <summary>
        /// 单条射线追踪
        /// </summary>
        /// <param name="singleRay">指定地面交点</param>
        public void SingleRayAnalysis(FrmSingleRayTracing1 singleRay)
        {
            Point crossWithGround = new Point(singleRay.x, singleRay.y, singleRay.z);

            double varAzimuth = 0;
            double varInclination = 0;

            GeometricUtilities.getAzimuth_Inclination(this.sourceInfo.SourcePoint, crossWithGround, out varAzimuth, out varInclination);

            this.sourceInfo.RayAzimuth = varAzimuth;
            this.sourceInfo.RayInclination = varInclination;

            #region 加速结构
            //------------------------生成加速结构开始-------------------------------------------------
            Grid3D accgrid = new Grid3D(), ggrid = new Grid3D();

            CellInfo cellInfo = this.sourceInfo;
            double distance = 800;  // 小区半径
            // 返回空间点(大地坐标)所在的加速网格坐标
            if (!GridHelper.getInstance().PointXYZToAccGrid(cellInfo.SourcePoint, ref accgrid))
            {
                Console.WriteLine("无法获取小区所在加速网格坐标，计算结束！");
                return;
            }

            //建筑物信息加速
            // 空间点（大地坐标）所在的立体网格
            if (!GridHelper.getInstance().PointXYZToGrid3D(cellInfo.SourcePoint, ref ggrid))
            {
                Console.WriteLine("无法获取小区所在地面网格坐标，计算结束！");
                return;
            }

            int mingxid = -1, maxgxid = -1, mingyid = -1, maxgyid = -1;
            int gridlength = GridHelper.getInstance().getAGridSize();
            int deltagrid = (int)Math.Ceiling(distance * 1.5 / gridlength);

            int maxAGXID = 0, maxAGYID = 0, minAGXID = 0, minAGYID = 0;
            GridHelper.getInstance().getMaxAccGridXY(ref maxAGXID, ref maxAGYID);
            GridHelper.getInstance().getMinAccGridXY(ref minAGXID, ref minAGYID);
            mingxid = Math.Max(minAGXID, accgrid.gxid - deltagrid);
            mingyid = Math.Max(minAGYID, accgrid.gyid - deltagrid);
            maxgxid = Math.Min(maxAGXID, accgrid.gxid + deltagrid);
            maxgyid = Math.Min(maxAGYID, accgrid.gyid + deltagrid);
            //mingxid = accgrid.gxid - deltagrid;
            //mingyid = accgrid.gyid - deltagrid;
            //maxgxid = accgrid.gxid + deltagrid;
            //maxgyid = accgrid.gyid + deltagrid;

            AccelerateStruct.clearAccelerateStruct();
            BuildingGrid3D.clearBuildingData();
            BuildingGrid3D.clearBuildingVertexOriginal();
            BuildingGrid3D.clearGrid3D();
            AccelerateStruct.setAccGridRange(mingxid, mingyid, maxgxid, maxgyid);

            // 从数据库表tbAccelerateGridBuilding中取出所有符合条件的数据,并以GXID,GYID,GZID排序, 构造结果集的哈希表
            AccelerateStruct.constructAccelerateStruct();
            //MessageBox.Show("AccelerateStruct.constructAccelerateStruct()");
            gridlength = GridHelper.getInstance().getGGridSize();
            deltagrid = (int)Math.Ceiling(distance * 1.5 / gridlength);
            int maxGXID = 0, maxGYID = 0, minGXID = 0, minGYID = 0;
            GridHelper.getInstance().getMaxGGridXY(ref maxGXID, ref maxGYID);
            GridHelper.getInstance().getMinGGridXY(ref minGXID, ref minGYID);
            mingxid = Math.Max(minGXID, ggrid.gxid - deltagrid);
            mingyid = Math.Max(minGYID, ggrid.gyid - deltagrid);
            maxgxid = Math.Min(maxGXID, ggrid.gxid + deltagrid);
            maxgyid = Math.Min(maxGYID, ggrid.gyid + deltagrid);
            //mingxid = ggrid.gxid - deltagrid;
            //mingyid = ggrid.gyid - deltagrid;
            //maxgxid = ggrid.gxid + deltagrid;
            //maxgyid = ggrid.gyid + deltagrid;

            BuildingGrid3D.setGGridRange(mingxid, mingyid, maxgxid, maxgyid);
            // 构建建筑物底面中心点、高度数据、所有点
            BuildingGrid3D.constructBuildingData();
            //MessageBox.Show("BuildingGrid3D.constructBuildingData()");

            // 平滑处理
            BuildingGrid3D.constructBuildingVertexOriginal();
            //MessageBox.Show("BuildingGrid3D.constructBuildingVertexOriginal()");

            deltagrid = (int)Math.Ceiling(deltagrid / 1.5);
            mingxid = Math.Max(minGXID, ggrid.gxid - deltagrid);
            mingyid = Math.Max(minGYID, ggrid.gyid - deltagrid);
            maxgxid = Math.Min(maxGXID, ggrid.gxid + deltagrid);
            maxgyid = Math.Min(maxGYID, ggrid.gyid + deltagrid);
            //mingxid = ggrid.gxid - deltagrid;
            //mingyid = ggrid.gyid - deltagrid;
            //maxgxid = ggrid.gxid + deltagrid;
            //maxgyid = ggrid.gyid + deltagrid;
            GroundGrid.setBound(mingxid, mingyid, maxgxid, maxgyid);
            //Console.Write("{0} {1} {2} {3}", ggrid.gxid - deltagrid, ggrid.gyid - deltagrid, ggrid.gxid + deltagrid, ggrid.gyid + deltagrid);
            //MessageBox.Show("GroundGrid.setBound");

            // 获取中心点在范围内的地面栅格中心点
            if (GroundGrid.constructGGrids() == 0)
            {
                System.Environment.Exit(0);
            }
            //------------------------生成加速结构结束-------------------------------------------------
            #endregion

            #region   不需要，仅仅为了调试
            /*
            //获取覆盖扇形范围内的建筑物id，从BuildingGrid3D.buildingCenter中筛
            Point source = cellInfo.SourcePoint;
            double fromAngle = 55;
            double toAngle = 98.33;
            //double fromAngle = 98.33;
            //double toAngle = 141.67;
            List<int> bids = BuildingGrid3D.getBuildingIDBySector(source, distance, fromAngle, toAngle);
            Console.WriteLine("building num = {0}", bids.Count);

            //将位于扇区覆盖范围内的地面栅格加进来
            List<Point> gfPoints = GroundGrid.getGGridCenterBySector(source, distance, fromAngle, toAngle, null);
            Console.WriteLine("ground grid num = {0}", gfPoints.Count);

            //建筑物顶面栅格
            List<Point> topPoints = TopPlaneGrid.GetAllTopGrid(source, bids);
            Console.WriteLine("top grid num = {0}", topPoints.Count);

            //建筑物立面栅格
            List<Point> vPoints = VerticalPlaneGrid.GetAllVerticalGrid(source, bids, 5);
            double mergeAngle = 5.0 / 2000;//弧度制
            // 合并射线终点，射线的角度小于mergeAngle的合并
            List<Point> vmPoints = GeometricUtilities.mergePointsByAngle(source, vPoints, mergeAngle);
            Console.WriteLine("vertical grid num = {0}", vmPoints.Count);

            //将建筑物按照到小区的距离排序，得到每个建筑物相对于小区的最小、最大角度，然后去掉被遮挡的建筑，并更新被遮挡建筑物可见部分
            List<TriangleBound> disAngle = BuildingGrid3D.getShelterDisAndAngle(source, bids, 0);
            Console.WriteLine("disAngle num = {0}", disAngle.Count);

            //建筑物棱边栅格
            List<Point> diffPoints = BuildingGrid3D.getBuildingsEdgePointsByShelter(source.Z, disAngle, 5);
            */
            #endregion

            //double[] receiveList = this.rayTracing1(this.sourceInfo.SourcePoint, crossWithGround, new List<NodeInfo>(), sourceInfo, RayType.Direction);

            Vector3D dir = Vector3D.constructVector(this.sourceInfo.SourcePoint, crossWithGround);
            dir.unit();
            List<NodeInfo> rayList = new List<NodeInfo>();
            List<Grid3D> DDA = new List<Grid3D>();
            if (this.scenNum == 0)
            {
                this.SingleRayAnalysis(this.sourceInfo.SourcePoint, dir, ref rayList, RayType.Direction, ref DDA);
            }
            else
            {
                this.SingleRayAnalysisAdj(this.sourceInfo.SourcePoint, dir, ref rayList, RayType.Direction, ref DDA);
            }

            if (rayList.Count > 0)
            {
                CalcGridStrength calcStrength = new CalcGridStrength(sourceInfo);
                double[] recv = calcStrength.calcRayStrength(sourceInfo.RayAzimuth, sourceInfo.RayInclination, ref rayList);
                double recvPwrDbm = calcStrength.convertw2dbm(recv[0]);
                double pathLoss = recvPwrDbm - calcStrength.calcDbmPt(sourceInfo.RayAzimuth, sourceInfo.RayInclination);
                singleRay.AddRow(singleRay.cellName, sourceInfo.EIRP, recv[1], recvPwrDbm, pathLoss);
            }

            //画反射线 
            List<ESRI.ArcGIS.Geometry.IPoint> linePoints = new List<ESRI.ArcGIS.Geometry.IPoint>();
            ESRI.ArcGIS.Geometry.IPoint p = GeometryUtilities.ConstructPoint3D(sourceInfo.SourcePoint.X, sourceInfo.SourcePoint.Y, sourceInfo.SourcePoint.Z);
            linePoints.Add(p);  // p为三维点
            for (int i = 0; i < rayList.Count; i++)
            {
                if (rayList[i].rayType == RayType.HReflection || rayList[i].rayType == RayType.VReflection || i == 0)
                {
                    p = GeometryUtilities.ConstructPoint3D(rayList[i].CrossPoint.X, rayList[i].CrossPoint.Y, rayList[i].CrossPoint.Z);
                    linePoints.Add(p);
                }
            }
            IGraphicsLayer pGraphicsLayer = (GISMapApplication.Instance.Scene as IBasicMap).BasicGraphicsLayer;
            DrawUtilities.DrawLine(pGraphicsLayer as IGraphicsContainer3D, linePoints);
            GISMapApplication.Instance.RefreshLayer(pGraphicsLayer);

            /*
            // 画绕射线  移动到了函数 public void SingleRayAnalysis(Point originPoint, Vector3D dir, ref List<NodeInfo> rayList, RayType rayType, ref List<Grid3D> DDA)
            // 画绕射线的时候要将画反射线注释掉
            ESRI.ArcGIS.Geometry.IPoint p1 = null;
            if (rayList.Count > 0)
                p1 = GeometryUtilities.ConstructPoint3D(rayList[0].CrossPoint.X, rayList[0].CrossPoint.Y, rayList[0].CrossPoint.Z);
            for (int i = 1; i < rayList.Count; i++)
            {
                if (rayList[i].rayType == RayType.VDiffraction || rayList[i].rayType == RayType.HDiffraction)
                {
                    linePoints.Clear();
                    linePoints.Add(p1);  // p为三维点
                    p = GeometryUtilities.ConstructPoint3D(rayList[i].CrossPoint.X, rayList[i].CrossPoint.Y, rayList[i].CrossPoint.Z);
                    linePoints.Add(p);
                    DrawUtilities.DrawLine(pGraphicsLayer as IGraphicsContainer3D, linePoints);
                }
            }
            */

            //网格数据
            //ESRI.ArcGIS.Geometry.IPoint geoCrossWithGround = (ESRI.ArcGIS.Geometry.IPoint)GeometryUtilities.ConvertProjToGeo(GeometryUtilities.CopyPoint3D(LocPointToArcPoint(ref crossWithGround)) as ESRI.ArcGIS.Geometry.IGeometry);
            //int gxid = -1, gyid = -1;
            //GridHelper.getInstance().LngLatToGGrid(geoCrossWithGround.X, geoCrossWithGround.Y, ref gxid, ref gyid);
            //Hashtable crossWithGroundGrid = new Hashtable();
            //crossWithGroundGrid["gxid"] = gxid;
            //crossWithGroundGrid["gyid"] = gyid;
            //DataTable gridTable = IbatisHelper.ExecuteQueryForDataTable("Get_CrossWithGround_Grid", crossWithGroundGrid);
            //if (gridTable.Rows.Count == 0)
            //    return;
            //float x1, y1, x2, y2;

            //if (!(float.TryParse(gridTable.Rows[0][0].ToString(), out x1) && float.TryParse(gridTable.Rows[0][1].ToString(), out y1) && float.TryParse(gridTable.Rows[0][2].ToString(), out x2) && float.TryParse(gridTable.Rows[0][3].ToString(), out y2)))
            //    return;


            //接收功率
            //todo

            //GridStrengthPwr gridStreng = new GridStrengthPwr(this.rayList, this.sourceInfo);
            //double recePower, pathLoss, hLoss, vLoss;
            //gridStreng.singleCalPassLose(out recePower, out pathLoss, out hLoss, out vLoss);
            //recePower = pathLoss = hLoss = vLoss = 0;
            //recePower = receiveList[0];

            //double distance = GeometricUtilities.GetDistanceOf3DPoints(this.sourceInfo.SourcePoint, crossWithGround);
            //double horizontalD = GeometricUtilities.GetDistanceOf2DPoints(this.sourceInfo.SourcePoint, crossWithGround);
            //CellInfo varCellInfo = this.sourceInfo as CellInfo;
            //singleRay.AddRow(this.sourceInfo.SourceName, Math.Round(varCellInfo.EIRP, 3), Math.Round(distance, 3), Math.Round(horizontalD, 3), Math.Round(hLoss, 3), Math.Round(vLoss, 3), Math.Round(recePower, 3), Math.Round(pathLoss, 3));

            //画网格
            //OperateCoverGirdLayer operateGrid = new OperateCoverGirdLayer();
            //operateGrid.constructGrid(this.sourceInfo.SourceName, this.sourceInfo.eNodeB, this.sourceInfo.CI, gxid, gyid, x1, y1, x2, y2, recePower, pathLoss);

        }

        /// <summary>
        /// </summary>
        /// <param name="originPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="singleRay"></param>
        /// <param name="featureID"></param>
        public void SingleRayAnalysis(Point originPoint, Vector3D dir, ref List<NodeInfo> rayList, RayType rayType, ref List<Grid3D> DDA)
        {
            int reflectionCounter = 0, diffractionCounter = 0;
            this.getCurrentLevel(rayList, out reflectionCounter, out diffractionCounter);
            if (reflectionCounter >= 3 || diffractionCounter >= 2)
            {
                return;// this.calcStrength.calcRayStrength(sourceInfo.RayAzimuth, sourceInfo.RayInclination, ref rayList);
            }

            Grid3D curAccGrid;
            //获取空间直线所经过的栅格
            DDA3D lineCrossGrid = new DDA3D(originPoint, dir);
            NodeInfo ray = null;
            RayType trayType = RayType.Direction;
            List<Point> polygonPoints = null;

            do
            {
                curAccGrid = lineCrossGrid.getNextCrossAccGrid();
                /////
                if (curAccGrid == null)
                {
                    break;
                }
                //t3 = DateTime.Now;
                ray = this.getInfoOfLineCrossAccGrid(originPoint, dir, curAccGrid, ref polygonPoints, ref trayType);

                if (ray != null)
                {
                    ray.rayType = rayType;
                    break;
                }
            } while (true);

            //射线没有与建筑物相交且射线方向向下
            if (ray == null)
            {
                if (dir.ZComponent < 0)
                {
                    // 计算射线与地面的交点
                    Point planePoint = new Point(200, 200, 0);  // 地面上的某个点
                    Point crossPoint = IntersectPoint.CalTopPlaneLineIntersectPoint(planePoint, originPoint, dir);

                    bool flag = GridHelper.getInstance().checkPointXYZInGrid(crossPoint);
                    if (flag)
                    {
                        ray = new NodeInfo(originPoint, crossPoint, null, null, -1, 0, null, rayType, -1);
                        rayList.Add(ray);
                    }
                }
                else
                {
                    Point planePoint = new Point(200, 200, 100);  // 上方的某个点
                    Point crossPoint = IntersectPoint.CalTopPlaneLineIntersectPoint(planePoint, originPoint, dir);

                    ray = new NodeInfo(originPoint, crossPoint, null, null, -1, 0, null, rayType, -1);
                    rayList.Add(ray);
                }
                return;// this.calcStrength.calcRayStrength(sourceInfo.RayAzimuth, sourceInfo.RayInclination, ref rayList);
            }

            rayList.Add(ray);

            if (trayType == RayType.HReflection || trayType == RayType.VReflection)
            {
                if (this.computeIndoor)
                {
                    this.TransmissionAnalysis(rayList, sourceInfo);
                }
                ReflectedRay refRay = new ReflectedRay(ray);
                Vector3D refDir = refRay.ConstructReflectedRay(ref dir);  // 反射线方向

                //递归
                this.SingleRayAnalysis(ray.CrossPoint, refDir, ref rayList, trayType, ref DDA);
            }
            else
            {
                DiffractedRay diffRay = new DiffractedRay(ray, polygonPoints);
                List<Vector3D> difDirs;
                if (trayType == RayType.VDiffraction)
                {
                    difDirs = diffRay.DiffractedRay_VerticalSide(originPoint, dir, 5);
                }

                else
                {//水平绕射
                    difDirs = diffRay.DiffractedRay_HorizontalSide(originPoint, dir, 5);
                }

                #region 绘制绕射线
                //foreach (var difDir in difDirs)
                //{
                //    if (difDir.ZComponent < 0)
                //    {
                //        // 计算射线与地面的交点
                //        Point planePoint = new Point(200, 200, 0);  // 地面上的某个点
                //        Point crossPoint = IntersectPoint.CalTopPlaneLineIntersectPoint(planePoint, rayList[0].CrossPoint, difDir);

                //        bool flag = GridHelper.getInstance().checkPointXYZInGrid(crossPoint);
                //        if (flag)
                //        {
                //            ray = new NodeInfo(rayList[0].CrossPoint, crossPoint, null, null, -1, 0, null, RayType.HDiffraction, -1);
                //            rayList.Add(ray);
                //        }
                //    }
                //}
                //// 画绕射线
                //IGraphicsLayer pGraphicsLayer = (GISMapApplication.Instance.Scene as IBasicMap).BasicGraphicsLayer;
                //List<ESRI.ArcGIS.Geometry.IPoint> linePoints = new List<ESRI.ArcGIS.Geometry.IPoint>();
                //ESRI.ArcGIS.Geometry.IPoint p = null;
                //ESRI.ArcGIS.Geometry.IPoint p1 = null;

                //if (rayList.Count > 0)
                //{
                //    linePoints.Clear();
                //    p = GeometryUtilities.ConstructPoint3D(originPoint.X, originPoint.Y, originPoint.Z);
                //    p1 = GeometryUtilities.ConstructPoint3D(rayList[0].CrossPoint.X, rayList[0].CrossPoint.Y, rayList[0].CrossPoint.Z);
                //    linePoints.Add(p);
                //    linePoints.Add(p1);  // p为三维点
                //    DrawUtilities.DrawLine(pGraphicsLayer as IGraphicsContainer3D, linePoints);
                //}
                //for (int i = 1; i < rayList.Count; i++)
                //{
                //    if (rayList[i].rayType == RayType.VDiffraction || rayList[i].rayType == RayType.HDiffraction)
                //    {
                //        linePoints.Clear();
                //        linePoints.Add(p1);  // p为三维点
                //        p = GeometryUtilities.ConstructPoint3D(rayList[i].CrossPoint.X, rayList[i].CrossPoint.Y, rayList[i].CrossPoint.Z);
                //        linePoints.Add(p);
                //        DrawUtilities.DrawLine(pGraphicsLayer as IGraphicsContainer3D, linePoints);
                //    }
                //}
                //return;
                #endregion

                //递归
                foreach (var difDir in difDirs)
                {
                    this.SingleRayAnalysis(ray.CrossPoint, difDir, ref rayList, trayType, ref DDA);
                }
            }
        }

        public void SingleRayAnalysisAdj(Point originPoint, Vector3D dir, ref List<NodeInfo> rayList, RayType rayType, ref List<Grid3D> DDA)
        {
            int reflectionCounter = 0, diffractionCounter = 0;
            this.getCurrentLevel(rayList, out reflectionCounter, out diffractionCounter);
            if (reflectionCounter >= 3 || diffractionCounter >= 2)
            {
                return;// this.calcStrength.calcRayStrength(sourceInfo.RayAzimuth, sourceInfo.RayInclination, ref rayList);
            }

            Grid3D curAccGrid;
            //获取空间直线所经过的栅格
            DDA3D lineCrossGrid = new DDA3D(originPoint, dir);
            NodeInfo ray = null;
            RayType trayType = RayType.Direction;
            List<Point> polygonPoints = null;
            int[] scene = new int[scenNum];

            do
            {
                curAccGrid = lineCrossGrid.getNextCrossAccGrid();
                /////
                if (curAccGrid == null)
                {
                    break;
                }

                // 2019.3.25 场景记录
                string grid = string.Format("{0},{1},{2}", curAccGrid.gxid, curAccGrid.gyid, curAccGrid.gzid);
                scene[AccelerateStruct.gridScene[grid]]++;

                //t3 = DateTime.Now;
                ray = this.getInfoOfLineCrossAccGrid(originPoint, dir, curAccGrid, ref polygonPoints, ref trayType);

                if (ray != null)
                {
                    ray.rayType = rayType;
                    break;
                }
            } while (true);

            //射线没有与建筑物相交且射线方向向下
            if (ray == null)
            {
                if (dir.ZComponent < 0)
                {
                    // 计算射线与地面的交点
                    Point planePoint = new Point(200, 200, 0);  // 地面上的某个点
                    Point crossPoint = IntersectPoint.CalTopPlaneLineIntersectPoint(planePoint, originPoint, dir);

                    bool flag = GridHelper.getInstance().checkPointXYZInGrid(crossPoint);
                    if (flag)
                    {
                        ray = new NodeInfo(originPoint, crossPoint, null, null, -1, 0, null, rayType, -1);
                        rayScene(ref ray, ref scene);  // 2019.3.25 场景记录
                        rayList.Add(ray);
                    }
                }
                else
                {
                    Point planePoint = new Point(200, 200, 100);  // 上方的某个点
                    Point crossPoint = IntersectPoint.CalTopPlaneLineIntersectPoint(planePoint, originPoint, dir);

                    ray = new NodeInfo(originPoint, crossPoint, null, null, -1, 0, null, rayType, -1);
                    rayScene(ref ray, ref scene);  // 2019.3.25 场景记录
                    rayList.Add(ray);
                }
                return;// this.calcStrength.calcRayStrength(sourceInfo.RayAzimuth, sourceInfo.RayInclination, ref rayList);
            }

            rayScene(ref ray, ref scene);  // 2019.3.25 场景记录
            rayList.Add(ray);

            if (trayType == RayType.HReflection || trayType == RayType.VReflection)
            {
                if (this.computeIndoor)
                {
                    this.TransmissionAnalysis(rayList, sourceInfo);
                }
                ReflectedRay refRay = new ReflectedRay(ray);
                Vector3D refDir = refRay.ConstructReflectedRay(ref dir);  // 反射线方向

                //递归
                this.SingleRayAnalysisAdj(ray.CrossPoint, refDir, ref rayList, trayType, ref DDA);
            }
            else
            {
                DiffractedRay diffRay = new DiffractedRay(ray, polygonPoints);
                List<Vector3D> difDirs;
                if (trayType == RayType.VDiffraction)
                {
                    difDirs = diffRay.DiffractedRay_VerticalSide(originPoint, dir, 5);
                }

                else
                {//水平绕射
                    difDirs = diffRay.DiffractedRay_HorizontalSide(originPoint, dir, 5);
                }

                #region 绘制绕射线
                //foreach (var difDir in difDirs)
                //{
                //    if (difDir.ZComponent < 0)
                //    {
                //        // 计算射线与地面的交点
                //        Point planePoint = new Point(200, 200, 0);  // 地面上的某个点
                //        Point crossPoint = IntersectPoint.CalTopPlaneLineIntersectPoint(planePoint, rayList[0].CrossPoint, difDir);

                //        bool flag = GridHelper.getInstance().checkPointXYZInGrid(crossPoint);
                //        if (flag)
                //        {
                //            ray = new NodeInfo(rayList[0].CrossPoint, crossPoint, null, null, -1, 0, null, RayType.HDiffraction, -1);
                //            rayList.Add(ray);
                //        }
                //    }
                //}
                //// 画绕射线
                //IGraphicsLayer pGraphicsLayer = (GISMapApplication.Instance.Scene as IBasicMap).BasicGraphicsLayer;
                //List<ESRI.ArcGIS.Geometry.IPoint> linePoints = new List<ESRI.ArcGIS.Geometry.IPoint>();
                //ESRI.ArcGIS.Geometry.IPoint p = null;
                //ESRI.ArcGIS.Geometry.IPoint p1 = null;

                //if (rayList.Count > 0)
                //{
                //    linePoints.Clear();
                //    p = GeometryUtilities.ConstructPoint3D(originPoint.X, originPoint.Y, originPoint.Z);
                //    p1 = GeometryUtilities.ConstructPoint3D(rayList[0].CrossPoint.X, rayList[0].CrossPoint.Y, rayList[0].CrossPoint.Z);
                //    linePoints.Add(p);
                //    linePoints.Add(p1);  // p为三维点
                //    DrawUtilities.DrawLine(pGraphicsLayer as IGraphicsContainer3D, linePoints);
                //}
                //for (int i = 1; i < rayList.Count; i++)
                //{
                //    if (rayList[i].rayType == RayType.VDiffraction || rayList[i].rayType == RayType.HDiffraction)
                //    {
                //        linePoints.Clear();
                //        linePoints.Add(p1);  // p为三维点
                //        p = GeometryUtilities.ConstructPoint3D(rayList[i].CrossPoint.X, rayList[i].CrossPoint.Y, rayList[i].CrossPoint.Z);
                //        linePoints.Add(p);
                //        DrawUtilities.DrawLine(pGraphicsLayer as IGraphicsContainer3D, linePoints);
                //    }
                //}
                //return;
                #endregion

                //递归
                foreach (var difDir in difDirs)
                {
                    this.SingleRayAnalysisAdj(ray.CrossPoint, difDir, ref rayList, trayType, ref DDA);
                }
            }
        }

        public ESRI.ArcGIS.Geometry.IPoint LocPointToArcPoint(ref Point p)
        {
            ESRI.ArcGIS.Geometry.IPoint point = new ESRI.ArcGIS.Geometry.PointClass();
            point.X = p.X;
            point.Y = p.Y;
            point.Z = p.Z;
            return point;
        }

        public ESRI.ArcGIS.Geometry.IPoint LocPointToArcPoint(Point p)
        {
            ESRI.ArcGIS.Geometry.IPoint point = new ESRI.ArcGIS.Geometry.PointClass();
            point.X = p.X;
            point.Y = p.Y;
            point.Z = p.Z;
            return point;
        }

        public Point ArcPointToLocPoint(ref ESRI.ArcGIS.Geometry.IPoint p)
        {
            Point point = new Point();
            point.X = p.X;
            point.Y = p.Y;
            point.Z = p.Z;
            return point;
        }

        public Point ArcPointToLocPoint(ESRI.ArcGIS.Geometry.IPoint p)
        {
            Point point = new Point();
            point.X = p.X;
            point.Y = p.Y;
            point.Z = p.Z;
            return point;
        }

        //-------------------------------------单射线结束-------------------------------

        /// <summary>
        /// 获取已经计算的射线的反射次数和绕射次数
        /// </summary>
        /// <param name="reflectionCounter"></param>
        /// <param name="diffractionCounter"></param>
        public void getCurrentLevel(List<NodeInfo> rayList, out int reflectionCounter, out int diffractionCounter)
        {
            reflectionCounter = 0;
            diffractionCounter = 0;
            foreach (var nodeInfo in rayList)
            {
                if (nodeInfo.rayType == RayType.HReflection || nodeInfo.rayType == RayType.VReflection)
                    reflectionCounter++;
                else if (nodeInfo.rayType == RayType.HDiffraction || nodeInfo.rayType == RayType.VDiffraction)
                    diffractionCounter++;
            }
        }

        /// <summary>
        /// 获取射线与建筑物顶面的交点
        /// </summary>
        /// <param name="start"></param>
        /// <param name="dir"></param>
        /// <param name="points"></param>
        /// <param name="height"></param>
        /// <param name="rayType">相交类型</param>
        /// <param name="edgeIndex">相交棱起点索引</param>
        /// <returns></returns>
        public Point getTopPlaneLineIntersectPoint(Point start, Vector3D dir, List<Point> points, double height, ref RayType rayType, ref int edgeIndex)
        {
            //DateTime t1, t2;
            //t1 = DateTime.Now;
            bool isEdge;

            Point t = points.First(), planePoint = new Point(t.X, t.Y, height);
            Point crossPoint = IntersectPoint.CalTopPlaneLineIntersectPoint(planePoint, start, dir);

            if (crossPoint != null)
            {

                if (PointComparer.Equals1(start, crossPoint))
                {
                    crossPoint = null;
                }
                else
                {
                    bool insect = GeometricUtilities.PointInPolygon(points.ToArray(), crossPoint, out isEdge, ref edgeIndex);
                    if (insect)
                    {
                        rayType = isEdge ? RayType.HDiffraction : RayType.HReflection;
                    }
                    else
                    {
                        crossPoint = null;
                    }
                }
            }
            //t2 = DateTime.Now;
            //Console.WriteLine("getTopPlaneLineIntersectPoint : {0} points - {1} ms\n", points.Count, (t2-t1).TotalMilliseconds);
            return crossPoint;
        }

        /// <summary>
        /// 获取射线与建筑物侧面的交点，大地坐标
        /// </summary>
        /// <param name="start">射线起点</param>
        /// <param name="dir">射线方向</param>
        /// <param name="point1">测试底面上一点</param>
        /// <param name="point2">测试底面上另一点</param>
        /// <param name="normal">测试法向</param>
        /// <param name="height">建筑物高度</param>
        /// <param name="crossPoint">射线与顶面交点</param>
        /// <param name="rayType">射线类型</param>
        public void getSidePlanePoint(Point start, Vector3D dir, Point point1, Point point2, Vector3D normal, double height, 
            double altitude, // 地形
            out Point crossPoint, ref RayType rayType)
        {
            //求射线与面的交点
            crossPoint = GeometricUtilities.Intersection(start, dir, point1, normal);
            if (crossPoint == null) return;

            //判断点有没有在侧面多边形内
            double x1 = Math.Min(point1.X, point2.X);
            double x2 = Math.Max(point1.X, point2.X);
            double y1 = Math.Min(point1.Y, point2.Y);
            double y2 = Math.Max(point1.Y, point2.Y);

            if (crossPoint.X > x2 || crossPoint.X < x1 || crossPoint.Y > y2 || crossPoint.Y < y1 || crossPoint.Z > height 
                || crossPoint.Z <= altitude)  // 地形
            {
                crossPoint = null;
            }
            else if ((Math.Abs(crossPoint.X - point1.X) < 0.1 && Math.Abs(crossPoint.Y - point1.Y) < 0.1) ||
                      (Math.Abs(crossPoint.X - point2.X) < 0.1 && Math.Abs(crossPoint.Y - point2.Y) < 0.1))
            {
                rayType = RayType.VDiffraction;
            }
            else if (Math.Abs(crossPoint.Z - height) < 0.1)
            {
                rayType = RayType.HDiffraction;
            }
            else
            {
                rayType = RayType.VReflection;
            }
        }

        /// <summary>
        /// 建筑物顶面方程，方程法向量为单位法向量
        /// </summary>
        /// <param name="start1"></param>
        /// <param name="start2"></param>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="point3"></param>
        /// <param name="high"></param>
        /// <param name="normal"></param>
        /// <param name="d"></param>
        public void getTopPlaneEqua(Point start1, Point start2, Point point1, Point point2, Point point3, double high, ref Vector3D normal, ref double d)
        {
            Vector3D PA = Vector3D.constructVector(point2, point1);
            Vector3D PB = Vector3D.constructVector(point2, point3);

            //法向量单位化,法向量进行单位化是为了减少数值间的计算。
            normal = PA.crossProduct(PB).unit();

            //选择正确的法向量
            Vector3D.getNormalVector(start1, start2, ref normal);

            d = -(normal.XComponent * point1.X + normal.YComponent * point1.Y + normal.ZComponent * (point1.Z + high));
        }

    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct GridStrength
    {
        public int GXID;
        public int GYID;
        //level = 0 表示地面，level>1表示室内
        public int Level;
        public Point GCenter;
        public int eNodeB;
        public int CI;
        public double FieldIntensity;
        public int DirectNum;
        public double DirectPwrW;
        public double MaxDirectPwrW;
        public int RefNum;
        public double RefPwrW;
        public double MaxRefPwrW;
        //反射建筑物id
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string RefBuildingID;
        public int DiffNum;
        public double DiffPwrW;
        public double MaxDiffPwrW;
        //绕射建筑物id
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string DiffBuildingID;
        public int TransNum;
        public double TransPwrW;
        public double MaxTransPwrW;
        //透射建筑物id
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string TransmitBuildingID;
        public double BTSGridDistance;
        public double ReceivedPowerW;
        public double ReceivedPowerdbm;
        public double PathLoss;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct MMFGSStruct
    {
        public int GXID;
        public int GYID;
        //level = 0 表示地面，level>1表示室内
        public int Level;
        public double x;
        public double y;
        public double z;
        public int eNodeB;
        public int CI;
        public double FieldIntensity;
        public int DirectNum;
        public double DirectPwrW;
        public double MaxDirectPwrW;
        public int RefNum;
        public double RefPwrW;
        public double MaxRefPwrW;
        //反射建筑物id
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string RefBuildingID;
        public int DiffNum;
        public double DiffPwrW;
        public double MaxDiffPwrW;
        //绕射建筑物id
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string DiffBuildingID;
        public int TransNum;
        public double TransPwrW;
        public double MaxTransPwrW;
        //透射建筑物id
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string TransmitBuildingID;
        public double BTSGridDistance;
        public double ReceivedPowerW;
        public double ReceivedPowerdbm;
        public double PathLoss;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct MMFReRayStruct
    {
        public int CI;
        public double emitX;
        public double emitY;
        public double emitZ;
        public double pwrDbm;
        public double dirX;
        public double dirY;
        public double dirZ;
        public byte type;
    }

    public class ReRay
    {
        public double emitPtX, emitPtY, emitPtZ;   // 发射点
        public double pwrDbm;  // 发射功率
        public double dirX, dirY, dirZ;   // 发射方向
        public RayType type;

        public ReRay()
        {

        }

        public ReRay(Point emitPt1, double pwr, Vector3D dir1, RayType type)
        {
            emitPtX = emitPt1.X;
            emitPtY = emitPt1.Y;
            emitPtZ = emitPt1.Z;
            pwrDbm = pwr;
            dirX = dir1.XComponent;
            dirY = dir1.YComponent;
            dirZ = dir1.ZComponent;
            this.type = type;
        }
    };

}
