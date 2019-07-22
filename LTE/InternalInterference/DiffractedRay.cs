using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LTE.GIS;
using LTE.Geometric;
using LTE.InternalInterference.Grid;

namespace LTE.InternalInterference
{
    public class DiffractedRay
    {
        private static object _missing = Type.Missing;
        private const double centralAxis = 1;
        private NodeInfo nodeInfo;
        private List<Point> polygonPoints;
        private Point[] pPoints;

        public DiffractedRay(NodeInfo nodeInfo, List<Point> polygonPoints)
        {
            this.nodeInfo = nodeInfo;
            this.polygonPoints = polygonPoints;
            this.pPoints = this.polygonPoints.ToArray();
        }

        // interval: 绕射点间隔
        public List<Vector3D> DiffractedRay_HorizontalSide(Point originPoint, Vector3D dir, int interval)
        {
            // 返回值
            List<Vector3D> refDirs = new List<Vector3D>();

            // 构造被绕射边向量，选择与入射线方向相同的那个方向
            Vector3D edgeDir = Vector3D.constructVector(this.nodeInfo.SideFromPoint, this.nodeInfo.SideToPoint);
            if (edgeDir.dotProduct(dir) < 0)
            {
                edgeDir = new Vector3D(-edgeDir.XComponent, -edgeDir.YComponent, -edgeDir.ZComponent);
            }
            edgeDir.unit();

            // 用于判断是否射入建筑物内
            List<Point> polygonPoints = BuildingGrid3D.getBuildingVertex(this.nodeInfo.buildingID);

            Vector3D down = new Vector3D(0, 0, -1);
            Vector2D v1 = new Vector2D(this.nodeInfo.SideFromPoint.X, this.nodeInfo.SideFromPoint.Y);
            Vector2D v2 = new Vector2D(this.nodeInfo.SideToPoint.X, this.nodeInfo.SideToPoint.Y);
            Line2D seg = new Line2D(v1, v2);
            Vector2D p = new Vector2D();
            Vector2D side = new Vector2D();
            seg.getPtNorm(out p, out side);
            Vector3D side1 = new Vector3D(-side.x, -side.y, 0);

            for (int i = 0; i < 360; i += interval)
            {
                Vector3D dif = RotateAroundAxisAny(edgeDir, (double)i * Math.PI / 180.0, dir);
                if (dif.dotProduct(down) > 0 && dif.dotProduct(side1) > 0)  // 与顶面和侧面的夹角都为锐角，则射入建筑物内
                    continue;
                refDirs.Add(dif);
            }

            return refDirs;
        }

        // interval: 绕射点间隔
        public List<Vector3D> DiffractedRay_VerticalSide(Point originPoint, Vector3D dir, int interval)
        {
            // 返回值
            List<Vector3D> refDirs = new List<Vector3D>();

            // 旋转轴
            Vector3D axis = new Vector3D(0, 0, 0);
            if (dir.ZComponent < 0)  // 入射线方向朝下
            {
                axis.ZComponent = -1;
            }
            else
            {
                axis.ZComponent = 1;
            }

            // 判断是否射入建筑物内
            List<Point> polygonPoints = BuildingGrid3D.getBuildingVertex(this.nodeInfo.buildingID);
            int id = 0;  // 记录交点的下标
            int n = polygonPoints.Count;
            for (int i = 0; i < polygonPoints.Count; i++)
            {
                if ((Math.Abs(this.nodeInfo.CrossPoint.X - polygonPoints[i].X) < 0.5 && Math.Abs(this.nodeInfo.CrossPoint.Y - polygonPoints[i].Y) < 0.5))
                {
                    id = i;
                    break;
                }
            }

            Vector3D nor1 = Vector3D.constructVector(polygonPoints[id], polygonPoints[(id + 1) % n]);
            Vector3D nor2 = Vector3D.constructVector(polygonPoints[id], polygonPoints[(id - 1 + n) % n]);
            nor1.ZComponent = 0;
            nor2.ZComponent = 0;

            for (int i = 0; i < 360; i += interval)
            {
                Vector3D dif = RotateAroundAxisZ((double)i * Math.PI / 180.0, dir);
                if (dif.dotProduct(nor1) > 0 && dif.dotProduct(nor2) > 0)  // 与该顶点的两条边夹角都为锐角，则射入建筑物内
                    continue;
                refDirs.Add(dif);
            }
            return refDirs;
        }

        // 构造向量v绕任意Z轴的旋转
        // 旋转轴为单位向量
        // theta是旋转的量，以弧度表示，用左手法自自在在来定义“正方向”
        // 平移部分是零
        public Vector3D RotateAroundAxisZ(double theta, Vector3D v)
        {
            // 取得旋转角的sin和cos值
            double s, c;
            s = Math.Sin(theta);
            c = Math.Cos(theta);

            double m11 = c, m12 = s, m13 = 0;
            double m21 = -s, m22 = c, m23 = 0;
            double m31 = 0, m32 = 0, m33 = 1;

            Vector3D v1 = new Vector3D();
            v1.XComponent = v.XComponent * m11 + v.YComponent * m21 + v.ZComponent * m31;
            v1.YComponent = v.XComponent * m12 + v.YComponent * m22 + v.ZComponent * m32;
            v1.ZComponent = v.XComponent * m13 + v.YComponent * m23 + v.ZComponent * m33;
            return v1;
        }

        // 构造向量v绕任意轴axis的旋转，旋转轴通过原点
        // 旋转轴为单位向量
        // theta是旋转的量，以弧度表示，用左手法自自在在来定义“正方向”
        // 平移部分是零
        public Vector3D RotateAroundAxisAny(Vector3D axis, double theta, Vector3D v)
        {
            // 判断旋转周是否为单位向向量
            if (Math.Abs(axis.Magnitude - 1) > 0.1)
            {
                Console.WriteLine("旋转周不是单位向量！");
                return null;
            }

            // 取得旋转角的sin和cos值
            double s, c;
            s = Math.Sin(theta);
            c = Math.Cos(theta);

            // 计算1-cos(theta）和一些公用的子表达式
            double a = 1.0 - c;
            double ax = a * axis.XComponent;
            double ay = a * axis.YComponent;
            double az = a * axis.ZComponent;

            // 矩阵元素的值
            double m11 = ax * axis.XComponent + c;
            double m12 = ax * axis.YComponent + axis.ZComponent * s;
            double m13 = ax * axis.ZComponent - axis.YComponent * s;

            double m21 = ay * axis.XComponent - axis.ZComponent * s;
            double m22 = ay * axis.YComponent + c;
            double m23 = ay * axis.ZComponent + axis.XComponent * s;

            double m31 = az * axis.XComponent + axis.YComponent * s;
            double m32 = az * axis.YComponent - axis.XComponent * s;
            double m33 = az * axis.ZComponent + c;

            Vector3D v1 = new Vector3D();
            v1.XComponent = v.XComponent * m11 + v.YComponent * m21 + v.ZComponent * m31;
            v1.YComponent = v.XComponent * m12 + v.YComponent * m22 + v.ZComponent * m32;
            v1.ZComponent = v.XComponent * m13 + v.YComponent * m23 + v.ZComponent * m33;
            return v1;
        }
    }
}
