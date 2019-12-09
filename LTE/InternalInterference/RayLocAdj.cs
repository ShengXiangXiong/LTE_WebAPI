using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LTE.Geometric;
using LTE.InternalInterference.Grid;

namespace LTE.InternalInterference
{
    // 记录用于定位、系数校正的射线 2019.1.11
    public class RayLocAdj
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
        /// <summary>
        /// 记录栅格场强结果
        /// </summary>
        private Dictionary<string, GridStrength> gridStrengths;

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

        public List<RaysNode> rayLoc;  // 2018.12.18 用于定位
        public Dictionary<string, List<RayNode>> rayAdj;  // 用于系数校正

        public double distance;
        bool isRayLoc, isRayAdj;

        public int scenNum = 3;  // 场景数量 2019.3.25

        /// 计算场强函数
        /// </summary>
        private CalcGridStrength calcStrength;

        public RayLocAdj(CellInfo sourceInfo, int reflectionLevel, int diffrectionLevel, bool computeIndoor,
            double distance, double fromAngle, double toAngle, double deltaA, bool isRayLoc, bool isRayAdj)
        {
            this.computeIndoor = computeIndoor;
            this.ReflectionLevel = reflectionLevel;
            this.DiffrectionLevel = diffrectionLevel;
            this.sourceInfo = sourceInfo;
            this.gridStrengths = new Dictionary<string, GridStrength>();
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

            if ((toAngle - fromAngle + 360) % 360 > deltaA)
                toAngle = fromAngle + deltaA;
            double from = fromAngle * Math.PI / 180;
            double to = toAngle * Math.PI / 180;
            double alpha = (from + to) / 2.0;
            double theta = Math.Abs(to - from) / 2;
            double r = Math.Min(1.5 * distance, distance / Math.Cos(theta));  // 半对角线长度
            double r1 = 0.5 * distance;
            this.rayLoc = new List<RaysNode>();  // 2018.12.18 用于定位
            this.rayAdj = new Dictionary<string, List<RayNode>>();  // 用于系数校正 
            this.isRayLoc = isRayLoc;
            this.isRayAdj = isRayAdj;

            // 2019.6.14 地形修改之后加入
            if (isRayLoc)
                this.scenNum = AdjCoeffHelper.getInstance().getSceneNum();
            else if (isRayAdj)
                this.scenNum = AdjCoeffHelper1.getInstance().getSceneNum();
        }

        // 用于系数校正
        public Dictionary<string, List<RayNode>> getRayList()
        {
            return this.rayAdj;
        }

        void cloneRays(ref List<NodeInfo> rays, ref RayNode raynode)
        {
            raynode.rayList = new List<NodeInfo>();
            for (int i = 0; i < rays.Count; ++i)
            {
                NodeInfo no = new NodeInfo(rays[i].PointOfIncidence, rays[i].CrossPoint, rays[i].SideFromPoint, rays[i].SideToPoint, rays[i].buildingID,
                    rays[i].BuildingHeight, rays[i].Normal, rays[i].rayType, rays[i].Angle);
                no.attenuation = rays[i].attenuation;
                no.proportion = rays[i].proportion;
                no.startPointScen = rays[i].startPointScen;
                no.endPointScen = rays[i].endPointScen;
                raynode.rayList.Add(no);
            }
        }

        // 用于系数校正
        public void addToRayAdj(ref List<NodeInfo> rayList)
        {
            if (rayList.Count == 0)
                return;

            int gxid = -1, gyid = -1, gzid = 0;
            Geometric.Point t_p = rayList[rayList.Count - 1].CrossPoint;
            if (!GridHelper.getInstance().XYToGGrid(t_p.X, t_p.Y, ref gxid, ref gyid))
                return;
            if (t_p.Z > 1)
                gzid = (int)Math.Ceiling(t_p.Z / GridHelper.getInstance().getGHeight()) + 1;
            string key = String.Format("{0},{1},{2}", gxid, gyid, gzid);

            if (!RayHelper.getInstance().ok(key)) // 入地栅格不位于路测路径中
            {
                return;
            }

            Geometric.Point endPoint = rayList[0].CrossPoint;
            Geometric.Point originPoint = rayList[0].PointOfIncidence;

            double varAzimuth = 0, varInclination = 0;
            GeometricUtilities.getAzimuth_Inclination(originPoint, endPoint, out varAzimuth, out varInclination);
            double[] ret = this.calcStrength.calcRayStrength(varAzimuth, varAzimuth, ref rayList);

            double recvPwrDbm = this.calcStrength.convertw2dbm(ret[0]);
            if (recvPwrDbm > -130)
            {
                RayNode rays = new RayNode();
                rays.cellid = this.sourceInfo.CI;
                rays.startPwrW = ret[2];
                rays.recePwrW = ret[0];
                cloneRays(ref rayList, ref rays);

                if (this.rayAdj.Keys.Contains(key))
                {
                    rayAdj[key].Add(rays);
                }
                else
                {
                    List<RayNode> list = new List<RayNode>();
                    list.Add(rays);
                    rayAdj[key] = list;
                }
            }
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
            //if (sum == 0)  // 这种情况应该不会出现
            //{
            //    string p = "1";
            //    for (int i = 0; i < scenNum - 1; i++)
            //        p += ";0";
            //    ray.proportion = p;
            //}
            //else
            {
                string p = "";
                for (int i = 0; i < scenNum - 1; i++)
                    p += Math.Round(scene[i] / sum, 3).ToString() + ";";
                p += Math.Round(scene[scenNum - 1] / sum, 3).ToString();
                ray.proportion = p;
            }
        }

        /// <summary>
        /// 跟踪初级直射线传播  用于系数校正
        /// </summary>
        /// <param name="originPoint">射线原点</param>
        /// <param name="endPoint">射线的第一次终点</param>
        /// <param name="rayList"></param>
        /// <param name="sourceInfo">小区</param>
        /// <param name="rayType">射线类型</param>
        /// <param name="type">初级直射线类型，1：连向地面；2：连向楼顶；3：连向可见侧面；4：连向可见棱边</param>
        /// <param name="coverageRadius">小区理论覆盖半径</param>
        public void rayTracingFirstAdj(Point originPoint, Point endPoint, List<NodeInfo> rayList, SourceInfo sourceInfo, RayType rayType, int type)
        {
            if (double.IsNaN(sourceInfo.RayAzimuth))
            {
                return;
            }

            Grid3D curAccGrid;
            Vector3D dir = Vector3D.constructVector(originPoint, endPoint);
            dir.unit();

            //获取空间直线所经过的栅格
            DDA3D lineCrossGrid = new DDA3D(originPoint, dir);

            NodeInfo ray = null;
            RayType trayType = RayType.Direction;
            List<Point> polygonPoints = null;

            this.rayCount++;
            this.rayCountDir++;

            int[] scene = new int[scenNum];

            do
            {
                curAccGrid = lineCrossGrid.getNextCrossAccGrid();  // 得到射线当前走到了哪个均匀栅格

                if (curAccGrid == null)
                {
                    break;
                }

                // 2019.3.25 场景记录
                string grid = string.Format("{0},{1},{2}", curAccGrid.gxid, curAccGrid.gyid, curAccGrid.gzid);
                scene[AccelerateStruct.gridScene[grid]]++;

                ray = this.getInfoOfLineCrossAccGrid(originPoint, dir, curAccGrid, ref polygonPoints, ref trayType);  // 射线与当前均匀栅格内的建筑进行碰撞检测

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

                        addToRayAdj(ref rayList);
                    }
                    // 2019.5.30 可能与地形碰撞
                    else if (ray.SideFromPoint == null)
                    {
                        rayScene(ref ray, ref scene);  // 2019.3.25 场景记录
                        rayList.Add(ray);
                        addToRayAdj(ref rayList);
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
                        addToRayAdj(ref rayList);
                    }
                    // 直接到达建筑物顶面
                    else if (Math.Abs(ray.CrossPoint.Z - endPoint.Z) < 0.5)
                    {
                        this.rayCountDirB++;
                        Vector3D normal = new Vector3D(0, 0, 1);

                        rayScene(ref ray, ref scene);  // 2019.3.25 场景记录

                        // 计算建筑物顶面接收到的场强
                        rayList.Add(ray);  // 先把初级直射线加进来

                        // 跟踪后续反射线
                        Vector3D inDir = Vector3D.constructVector(originPoint, endPoint);
                        inDir.unit();
                        ReflectedRay refRay = new ReflectedRay(ray);
                        Vector3D refDir = refRay.ConstructReflectedRay(ref inDir);
                        rayCountRef++;
                        this.rayCountRef1++;
                        this.rayTracingAdj(ray.CrossPoint, refDir, rayList, sourceInfo, trayType);
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
                        addToRayAdj(ref rayList);
                    }
                    else
                    {
                        // 直接到达建筑物侧面
                        bool ok1 = Math.Abs(ray.CrossPoint.X - endPoint.X) < 0.5;
                        bool ok2 = Math.Abs(ray.CrossPoint.Y - endPoint.Y) < 0.5;
                        bool ok3 = Math.Abs(ray.CrossPoint.Z - endPoint.Z) < 0.5;
                        if (ok1 && ok2 && ok3)
                        {
                            rayScene(ref ray, ref scene);  // 2019.3.25 场景记录
                            rayList.Add(ray);  // 先把初级直射线加进来

                            // 跟踪后续反射线
                            ReflectedRay refRay = new ReflectedRay(ray);
                            Vector3D inDir = Vector3D.constructVector(originPoint, endPoint);
                            inDir.unit();
                            Vector3D refDir = refRay.ConstructReflectedRay(ref inDir);
                            rayCountRef++;
                            this.rayCountRef1++;
                            this.rayTracingAdj(ray.CrossPoint, refDir, rayList, sourceInfo, trayType);
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
                        addToRayAdj(ref rayList);
                    }
                    else
                    {
                        bool ok1 = Math.Abs(ray.CrossPoint.X - endPoint.X) < 0.5;
                        bool ok2 = Math.Abs(ray.CrossPoint.Y - endPoint.Y) < 0.5;
                        bool ok3 = Math.Abs(ray.CrossPoint.Z - endPoint.Z) < 0.5;
                        if (ok1 && ok2 && ok3) // 到达建筑物棱边
                        {
                            rayScene(ref ray, ref scene);  // 2019.3.25 场景记录
                            rayList.Add(ray);  // 先把初级直射线加进来

                            // 跟踪后续绕射线
                            DiffractedRay diffRay = new DiffractedRay(ray, polygonPoints);
                            List<Vector3D> difDirs;
                            if (trayType == RayType.VDiffraction) // 垂直绕射
                            {
                                difDirs = diffRay.DiffractedRay_VerticalSide(originPoint, dir, 20);
                            }
                            else
                            {   // 水平绕射
                                difDirs = diffRay.DiffractedRay_HorizontalSide(originPoint, dir, 20);
                            }

                            // 递归
                            foreach (var difDir in difDirs)
                            {
                                rayCountDif++;
                                rayCountDif1++;
                                this.rayTracingAdj(ray.CrossPoint, difDir, rayList, sourceInfo, trayType);
                            }
                        }
                    }
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// 跟踪某种类型的射线传播  用于系数校正
        /// </summary>
        /// <param name="originPoint">射线原点</param>
        /// <param name="dir">射线方向</param>
        /// <param name="rayList"></param>
        /// <param name="sourceInfo">小区</param>
        /// <param name="rayType">射线类型</param>
        /// <param name="coverageRadius">小区理论覆盖半径</param>
        public void rayTracingAdj(Point originPoint, Vector3D dir, List<NodeInfo> rayList, SourceInfo sourceInfo, RayType rayType)
        {
            if (double.IsNaN(sourceInfo.RayAzimuth))
            {
                return;
            }

            rayCount++;

            int reflectionCounter, diffractionCounter;

            this.getCurrentLevel(rayList, out reflectionCounter, out diffractionCounter);

            int sumw = diffractionCounter * 3 + reflectionCounter;
            if (sumw >= 5)
            {
                if (rayList.Count > 0)
                {
                    addToRayAdj(ref rayList);
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

                ray = (NodeInfo)this.getInfoOfLineCrossAccGrid(originPoint, dir, curAccGrid, ref polygonPoints, ref trayType);

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

                    if (flag)
                    {
                        Vector3D normal = new Vector3D(0, 0, 1);
                        ray = new NodeInfo(originPoint, crossPoint, new Point(-1, -1, -1), new Point(-1, -1, -1), -1, 0, null, rayType, Vector3D.getAngle(ref dir, ref normal) - Math.PI / 2.0);
                        rayScene(ref ray, ref scene);  // 2019.3.25 场景记录
                        rayList.Add(ray);

                        addToRayAdj(ref rayList);

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
                addToRayAdj(ref rayList);
                return;
            }

            rayScene(ref ray, ref scene);  // 2019.3.25 场景记录
            rayList.Add(ray);

            if (trayType == RayType.HReflection || trayType == RayType.VReflection)
            {
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
                this.rayTracingAdj(ray.CrossPoint, refDir, rayList, sourceInfo, trayType);
            }
            else
            {
                DiffractedRay diffRay = new DiffractedRay(ray, polygonPoints);
                List<Vector3D> difDirs;
                if (trayType == RayType.VDiffraction) // 垂直绕射
                {
                    difDirs = diffRay.DiffractedRay_VerticalSide(originPoint, dir, 20);
                }
                else
                {   // 水平绕射
                    difDirs = diffRay.DiffractedRay_HorizontalSide(originPoint, dir, 20);
                }

                //递归
                foreach (var difDir in difDirs)
                {
                    rayCountDif++;

                    if (diffractionCounter == 0)  // 也可能是经过反射后生成的初级绕射线
                        this.rayCountDif1++;
                    else if (diffractionCounter == 1)
                        this.rayCountDif2++;

                    this.rayTracingAdj(ray.CrossPoint, difDir, rayList, sourceInfo, trayType);
                }
            }
            rayList.Remove(ray);
        }


        public void addToRayLoc(ref List<NodeInfo> rayList)
        {
            if (rayList.Count == 0)
                return;

            Geometric.Point endPoint = rayList[0].CrossPoint;
            Geometric.Point originPoint = rayList[0].PointOfIncidence;

            double varAzimuth = 0, varInclination = 0;
            GeometricUtilities.getAzimuth_Inclination(originPoint, endPoint, out varAzimuth, out varInclination);
            double[] ret = this.calcStrength.calcRayStrength(varAzimuth, varAzimuth, ref rayList);

            double recvPwrDbm = this.calcStrength.convertw2dbm(ret[0]);
            if (recvPwrDbm > -130)
            {
                RaysNode rays = new RaysNode();
                rays.emitPwrDbm = this.calcStrength.convertw2dbm(ret[2]);
                rays.recvPwrDbm = recvPwrDbm;
                rays.rayList = new List<NodeInfo>(rayList);
                this.rayLoc.Add(rays);
            }
        }

        /// <summary>
        /// 跟踪初级直射线传播 2018.12.18 用于定位
        /// </summary>
        /// <param name="originPoint">射线原点</param>
        /// <param name="endPoint">射线的第一次终点</param>
        /// <param name="rayList"></param>
        /// <param name="sourceInfo">小区</param>
        /// <param name="rayType">射线类型</param>
        /// <param name="type">初级直射线类型，1：连向地面；2：连向楼顶；3：连向可见侧面；4：连向可见棱边</param>
        /// <param name="coverageRadius">小区理论覆盖半径</param>
        public void rayTracingFirstLoc(Point originPoint, Point endPoint, List<NodeInfo> rayList, SourceInfo sourceInfo, RayType rayType, int type)
        {
            if (double.IsNaN(sourceInfo.RayAzimuth))
            {
                return;
            }

            Grid3D curAccGrid;
            Vector3D dir = Vector3D.constructVector(originPoint, endPoint);
            dir.unit();

            //获取空间直线所经过的栅格
            DDA3D lineCrossGrid = new DDA3D(originPoint, dir);

            NodeInfo ray = null;
            RayType trayType = RayType.Direction;
            List<Point> polygonPoints = null;

            this.rayCount++;
            this.rayCountDir++;

            int[] scene = null;
            if (this.scenNum > 0)
                scene = new int[scenNum];
            do
            {
                curAccGrid = lineCrossGrid.getNextCrossAccGrid();  // 得到射线当前走到了哪个均匀栅格

                if (curAccGrid == null)
                {
                    break;
                }

                if (this.scenNum > 0)
                {
                    // 2019.4.25 场景记录
                    string grid = string.Format("{0},{1},{2}", curAccGrid.gxid, curAccGrid.gyid, curAccGrid.gzid);
                    scene[AccelerateStruct.gridScene[grid]]++;
                }

                // 地形
                ray = this.getInfoOfLineCrossAccGrid(originPoint, dir, curAccGrid, ref polygonPoints, ref trayType);  // 射线与当前均匀栅格内的建筑进行碰撞检测

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
                        if (this.scenNum > 0)
                            rayScene(ref ray, ref scene);  // 2019.4.25 场景记录
                        rayList.Add(ray);

                        addToRayLoc(ref rayList);
                    }
                    // 2019.5.30 可能与地形碰撞
                    else if (ray.SideFromPoint == null)
                    {
                        if (this.scenNum > 0)
                            rayScene(ref ray, ref scene);  // 2019.4.25 场景记录
                        rayList.Add(ray);
                    }
                    break;

                case 3:
                    if (ray == null)
                        break;

                    // 2019.5.30 可能与地形碰撞
                    if (ray.SideFromPoint == null)
                    {
                        if (this.scenNum > 0)
                            rayScene(ref ray, ref scene);  // 2019.4.25 场景记录
                        rayList.Add(ray);
                        addToRayLoc(ref rayList);
                    }
                    else
                    {
                        // 直接到达建筑物侧面
                        bool ok1 = Math.Abs(ray.CrossPoint.X - endPoint.X) < 0.5;
                        bool ok2 = Math.Abs(ray.CrossPoint.Y - endPoint.Y) < 0.5;
                        bool ok3 = Math.Abs(ray.CrossPoint.Z - endPoint.Z) < 0.5;
                        if (ok1 && ok2 && ok3)
                        {
                            if (this.scenNum > 0)
                                rayScene(ref ray, ref scene);  // 2019.4.25 场景记录
                            rayList.Add(ray);  // 先把初级直射线加进来
                            addToRayLoc(ref rayList);

                            // 跟踪后续反射线
                            ReflectedRay refRay = new ReflectedRay(ray);
                            Vector3D inDir = Vector3D.constructVector(originPoint, endPoint);
                            inDir.unit();
                            Vector3D refDir = refRay.ConstructReflectedRay(ref inDir);
                            rayCountRef++;
                            this.rayCountRef1++;
                            this.rayTracingLoc(ray.CrossPoint, refDir, rayList, sourceInfo, trayType);
                        }
                    }
                    break;

                case 4:
                    if (ray == null)
                        break;

                    // 2019.5.30 可能与地形碰撞
                    if (ray.SideFromPoint == null)
                    {
                        if (this.scenNum > 0)
                            rayScene(ref ray, ref scene);  // 2019.4.25 场景记录
                        rayList.Add(ray);
                        addToRayLoc(ref rayList);
                    }
                    else
                    {
                        bool ok1 = Math.Abs(ray.CrossPoint.X - endPoint.X) < 0.5;
                        bool ok2 = Math.Abs(ray.CrossPoint.Y - endPoint.Y) < 0.5;
                        bool ok3 = Math.Abs(ray.CrossPoint.Z - endPoint.Z) < 0.5;
                        if (ok1 && ok2 && ok3)
                        {
                            if (this.scenNum > 3)
                                rayScene(ref ray, ref scene);  // 2019.4.25 场景记录
                            rayList.Add(ray);  // 先把初级直射线加进来

                            // 跟踪后续绕射线
                            DiffractedRay diffRay = new DiffractedRay(ray, polygonPoints);
                            List<Vector3D> difDirs;
                            if (trayType == RayType.VDiffraction) // 垂直绕射
                            {
                                difDirs = diffRay.DiffractedRay_VerticalSide(originPoint, dir, 20);
                            }
                            else
                            {   // 水平绕射
                                difDirs = diffRay.DiffractedRay_HorizontalSide(originPoint, dir, 20);
                            }

                            // 递归
                            foreach (var difDir in difDirs)
                            {
                                rayCountDif++;
                                rayCountDif1++;
                                this.rayTracingLoc(ray.CrossPoint, difDir, rayList, sourceInfo, trayType);
                            }
                        }
                    }
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// 跟踪某种类型的射线传播 2018.12.18 用于定位
        /// </summary>
        /// <param name="originPoint">射线原点</param>
        /// <param name="dir">射线方向</param>
        /// <param name="rayList"></param>
        /// <param name="sourceInfo">小区</param>
        /// <param name="rayType">射线类型</param>
        /// <param name="coverageRadius">小区理论覆盖半径</param>
        public void rayTracingLoc(Point originPoint, Vector3D dir, List<NodeInfo> rayList, SourceInfo sourceInfo, RayType rayType)
        {
            if (double.IsNaN(sourceInfo.RayAzimuth))
            {
                return;
            }

            rayCount++;

            int reflectionCounter, diffractionCounter;

            this.getCurrentLevel(rayList, out reflectionCounter, out diffractionCounter);

            int sumw = diffractionCounter * 3 + reflectionCounter;
            if (sumw >= 5)
            {
                if (dis(rayList[0].PointOfIncidence, rayList[rayList.Count - 1].CrossPoint) < distance * 1.5)
                {
                    if (rayList.Count > 0)
                    {
                        addToRayLoc(ref rayList);
                    }
                }
                return;
            }

            Grid3D curAccGrid;
            //获取空间直线所经过的栅格
            DDA3D lineCrossGrid = new DDA3D(originPoint, dir);
            NodeInfo ray = null;
            RayType trayType = RayType.Direction;
            List<Point> polygonPoints = null;

            int[] scene = null;
            if (this.scenNum > 0)
                scene = new int[scenNum];

            do
            {
                curAccGrid = lineCrossGrid.getNextCrossAccGrid();

                if (curAccGrid == null)
                {
                    break;
                }

                if (this.scenNum > 0)
                {
                    // 2019.4.25 场景记录
                    string grid = string.Format("{0},{1},{2}", curAccGrid.gxid, curAccGrid.gyid, curAccGrid.gzid);
                    scene[AccelerateStruct.gridScene[grid]]++;
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

                    if (flag)
                    {
                        Vector3D normal = new Vector3D(0, 0, 1);
                        ray = new NodeInfo(originPoint, crossPoint, new Point(-1, -1, -1), new Point(-1, -1, -1), -1, 0, null, rayType, Vector3D.getAngle(ref dir, ref normal) - Math.PI / 2.0);
                        if (this.scenNum > 0)
                            rayScene(ref ray, ref scene);  // 2019.4.25 场景记录
                        rayList.Add(ray);

                        rayList[rayList.Count - 1].CrossPoint.Z = 0;
                        if (dis(rayList[0].PointOfIncidence, rayList[rayList.Count - 1].CrossPoint) < distance * 1.2)
                        {
                            addToRayLoc(ref rayList);
                        }
                        rayList.Remove(ray);
                    }
                }
                return;
            }
            // 2019.5.30 可能与地形碰撞
            else if (ray.SideFromPoint == null)
            {
                if (this.scenNum > 0)
                    rayScene(ref ray, ref scene);  // 2019.4.25 场景记录
                rayList.Add(ray);
                addToRayLoc(ref rayList);
                return;
            }

            if (this.scenNum > 0)
                rayScene(ref ray, ref scene);  // 2019.4.25 场景记录
            rayList.Add(ray);

            if (trayType == RayType.HReflection || trayType == RayType.VReflection)
            {
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
                this.rayTracingLoc(ray.CrossPoint, refDir, rayList, sourceInfo, trayType);
            }
            else
            {
                DiffractedRay diffRay = new DiffractedRay(ray, polygonPoints);
                List<Vector3D> difDirs;
                if (trayType == RayType.VDiffraction) // 垂直绕射
                {
                    difDirs = diffRay.DiffractedRay_VerticalSide(originPoint, dir, 20);
                }
                else
                {   // 水平绕射
                    difDirs = diffRay.DiffractedRay_HorizontalSide(originPoint, dir, 20);
                }

                //递归
                foreach (var difDir in difDirs)
                {
                    rayCountDif++;

                    if (diffractionCounter == 0)  // 也可能是经过反射后生成的初级绕射线
                        this.rayCountDif1++;
                    else if (diffractionCounter == 1)
                        this.rayCountDif2++;

                    this.rayTracingLoc(ray.CrossPoint, difDir, rayList, sourceInfo, trayType);
                }
            }
            rayList.Remove(ray);
        }

        double dis(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2) + Math.Pow(p1.Z - p2.Z, 2));
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
                for (int i = 0; i < TINs.Count; i++)
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
                    if (!PointHeight.isInside(polygonPoints[2], polygonPoints[1], polygonPoints[0], crossPt.X, crossPt.Y))
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

        //<summary>
        ///固定方向射线与建筑物相交预处理
        ///选出距离source最近的底边
        ///</summary>
        ///<param name="startp">射线发出点</param>
        ///<param name="dir">射线方向</param>
        ///<param name="Vertex">建筑物底面顶点序列</param>
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

    }
}
