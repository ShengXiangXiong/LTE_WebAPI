using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LTE.Geometric;
namespace LTE.ExternalInterference.Struct
{
    /// <summary>
    /// 用于存储path
    /// </summary>
    public class PathInfo
    {
        public string CellID;
        public int trajID;//路径标识
        public short rayLevel;//路径上的路段标识

        /// <summary>
        /// 路段起点与终点大地坐标
        /// </summary>
        public Point rayStartPoint;
        public Point rayEndPoint;
        public double sourceEmit;//记录该轨迹始发点接收功率

        public int rayType;
        public double distance;//已经多长
        public double emit;//单位为w，接收功率
        public double k1;//斜率
        public double k2;//斜率
        public double k3;//yz斜率

        public PathInfo(string CellID, int trajID, short raylevel, double startx, double starty, double startz, double endx, double endy, double endz, int rayType)
        {
            this.CellID = CellID;
            this.trajID = trajID;
            this.rayLevel = raylevel;
            this.rayStartPoint = new Point(startx, starty, startz);
            this.rayEndPoint = new Point(endx, endy, endz);
            this.rayType = rayType;

            this.emit = 0; //损耗计算,暂时还没有处理

            this.k1 = Point.getSlopeXY(rayStartPoint, rayEndPoint);
            this.k2 = Point.getSlopeXZ(rayStartPoint, rayEndPoint);
            this.k3 = Point.getSlopeYZ(rayStartPoint, rayEndPoint);


        }
    }
}
