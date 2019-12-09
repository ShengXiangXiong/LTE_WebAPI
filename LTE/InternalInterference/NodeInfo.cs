using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LTE.Geometric;

namespace LTE.InternalInterference
{
    /// <summary>
    /// 射线类型，直射、水平反射、侧面反射、水平绕射、垂直绕射、透射
    /// </summary>
    public enum RayType { Direction, HReflection, VReflection, HDiffraction, VDiffraction, Transimission };

    // 记录射线信息
     [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public class NodeInfo
    {
        public double distance;
        /// <summary>
        /// 射线起点与交点距离
        /// </summary>
        public double Distance
        {
            get { return this.distance; }
        }
        /// <summary>
        /// 射线与楞或侧面夹角，以弧度表示
        /// </summary>
        public double Angle { get; set; }
        /// <summary>
        /// 射线起点
        /// </summary>
        public Point PointOfIncidence { get; set; }
        /// <summary>
        /// 射线与侧面交点
        /// </summary>
        public Point CrossPoint { get; set; }
        /// <summary>
        /// 底边起点
        /// </summary>
        public Point SideFromPoint { get; set; }
        /// <summary>
        /// 底边终点
        /// </summary>
        public Point SideToPoint { get; set; }
        /// <summary>
        /// 建筑物高度
        /// </summary>
        public double BuildingHeight { get; set; }
        /// <summary>
        /// 侧面法向
        /// </summary>
        public Vector3D Normal { get; set; }
        /// <summary>
        /// 当前射线来源的类型，如果是初始射线，则为直射线，
        /// 如果是经过反射得到的射线，则为反射线，
        /// 如果是经过绕射得到的射线，则为水平绕射线或垂直绕射线，
        /// 如果是经过透射得到的射线，则为透射线
        /// </summary>
        public RayType rayType { get; set; }

        public int buildingID { get; set; }

        public double attenuation { get; set; }

        public double recePwr { get; set; }


        /// <summary>
        /// 小区标识
        /// </summary>
        public int cellID { get; set; }
        /// <summary>
        /// 射线栅格标识
        /// </summary>
        public int gxid { get; set; }
        public int gyid { get; set; }
        // 射线经过各场景的比例，以分号分隔
        public string proportion { get; set; }

        /// <summary>
        /// 射线轨迹标识
        /// </summary>
        public int trajID { get; set; }
        /// <summary>
        /// 在各个场景中的距离
        /// </summary>
        public double[] trajScen { get; set; }

        public int startPointScen { get; set; }
        public int endPointScen { get; set; }

        public NodeInfo()
        {
        }

        public NodeInfo(Point PointOfIncidence, Point CrossPoint, Point SideFromPoint, Point SideToPoint, int buildingID, double BuildingHeight, Vector3D normal, RayType rayType, double angle)
        {
            this.distance = GeometricUtilities.GetDistanceOf3DPoints(PointOfIncidence, CrossPoint);
            this.PointOfIncidence = PointOfIncidence;
            this.CrossPoint = CrossPoint;
            this.SideFromPoint = SideFromPoint;
            this.SideToPoint = SideToPoint;
            this.buildingID = buildingID;
            this.BuildingHeight = BuildingHeight;
            this.Normal = normal;
            this.rayType = rayType;
            this.Angle = angle;
        }

        public NodeInfo(Point PointOfIncidence, Point CrossPoint, RayType rayType, double angle)
        {
            this.distance = GeometricUtilities.GetDistanceOf3DPoints(PointOfIncidence, CrossPoint);
            this.rayType = rayType;
            this.Angle = angle;
        }

        public NodeInfo(double distance, RayType rayType, double angle)
        {
            this.distance = distance;
            this.rayType = rayType;
            this.Angle = angle;
        }

        public NodeInfo(RayType rayType, double dis, double angle)
        {
            this.rayType = rayType;
            this.distance = dis;
            this.Angle = angle;
        }

        public NodeInfo(int cellID, int gxid, int gyid, int trajID, RayType rayType, double dis, double angle, double attenuation, double recePwr)
        {
            this.cellID = cellID;
            this.gxid = gxid;
            this.gyid = gyid;
            this.trajID = trajID;
            this.rayType = rayType;
            this.distance = dis;
            this.Angle = angle;
            this.attenuation = attenuation;
            this.recePwr = recePwr;
        }
    }

}
