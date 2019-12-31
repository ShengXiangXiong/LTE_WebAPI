using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// 2018.12.04
namespace LTE.Beam
{
    /// <summary>
    /// 射线类型，直射、水平反射、侧面反射、水平绕射、垂直绕射、透射
    /// </summary>
    public enum RayType { Direction, HReflection, VReflection, HDiffraction, VDiffraction, Transimission };

    public class NodeInfo
    {
        private double distance;
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
        public Vector3 Normal { get; set; }
        /// <summary>
        /// 当前射线来源的类型，如果是初始射线，则为直射线，
        /// 如果是经过反射得到的射线，则为反射线，
        /// 如果是经过绕射得到的射线，则为水平绕射线或垂直绕射线，
        /// 如果是经过透射得到的射线，则为透射线
        /// </summary>
        public RayType rayType { get; set; }
        /// <summary>
        /// 与射线相交的建筑物
        /// </summary>
        public int buildingID { get; set; }

        public double attenuation { get; set; }

        public double recePwr { get; set; }

        /// <summary>
        /// 大地坐标
        /// </summary>
        public static double getDistanceOf3DPoints(Point a, Point b)
        {
            return Math.Sqrt(Math.Pow(a.getPosition().x - b.getPosition().x, 2) + Math.Pow(a.getPosition().y - b.getPosition().y, 2) + Math.Pow(a.getPosition().z - b.getPosition().z, 2));
        }

        public NodeInfo(Point PointOfIncidence, Point CrossPoint, Point SideFromPoint, Point SideToPoint, int buildingID, double BuildingHeight, Vector3 normal, RayType rayType, double angle)
        {
            this.distance = getDistanceOf3DPoints(PointOfIncidence, CrossPoint);
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

        public NodeInfo(NodeInfo node)
        {
            this.distance = node.distance;
            if (node.PointOfIncidence != null && node.PointOfIncidence.m_position != null)
                this.PointOfIncidence = new Point(node.PointOfIncidence);
            if (node.CrossPoint != null && node.CrossPoint.m_position != null)
                this.CrossPoint = new Point(node.CrossPoint);
            //this.SideFromPoint = new Point(SideFromPoint);
            //this.SideToPoint = new Point(SideToPoint);
            this.buildingID = buildingID;
            this.BuildingHeight = BuildingHeight;
            if (node.Normal != null)
                this.Normal = new Vector3(node.Normal);
            this.rayType = node.rayType;
            this.Angle = node.Angle;

        }

        public NodeInfo(Point PointOfIncidence, Point CrossPoint, RayType rayType, double angle)
        {
            this.distance = getDistanceOf3DPoints(PointOfIncidence, CrossPoint);
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
    }
}

