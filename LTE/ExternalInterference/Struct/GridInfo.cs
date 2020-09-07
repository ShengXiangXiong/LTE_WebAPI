using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
namespace LTE.ExternalInterference.Struct
{
    /// <summary>
    /// 栅格覆盖的单个射线路径信息
    /// </summary>
    public class GridInfo : IComparable
    {
        public string cellid;
        public int trajID;
        public int raylevel;
        public int rayType;
        public double x;
        public double y;
        public double z;
        public double recP;
        public double pathloss;//路径损耗
        public double emit;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="cellid"></param>
        /// <param name="trajID"></param>
        /// <param name="raylevel"></param>
        /// <param name="rayType"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="recP">单位为w</param>
        /// <param name="emitp">单位为w</param>
        public GridInfo(string cellid, int trajID, int raylevel, int rayType, double x, double y, double z, double recP, double emitp)
        {
            this.cellid = cellid;
            this.trajID = trajID;
            this.raylevel = raylevel;
            this.rayType = rayType;
            this.x = x;
            this.y = y;
            this.z = z;
            this.recP = recP;//单位为w
            this.pathloss = 10 * (Math.Log10(recP) + 3) - 10 * (Math.Log10(emitp) + 3);
            if (double.IsPositiveInfinity(this.pathloss))
            {
                Debug.WriteLine("x:" + x + "      y" + y + "    z:" +z+ "raylevel:" + raylevel + "      rayType" + rayType+"    trajID:"+ trajID+ "    cellid:" + cellid);
            }
            //if (pathloss > 0)
            //{
            //    Debug.WriteLine("pathloss" + pathloss);
            //}
            this.emit = emitp;
        }

        public GridInfo(GridInfo old)
        {
            this.cellid = old.cellid;
            this.trajID = old.trajID;
            this.raylevel = old.raylevel;
            this.rayType = old.rayType;
            this.x = old.x;
            this.y = old.y;
            this.z = old.z;
            this.recP = old.recP;//单位为w
            this.pathloss = old.pathloss;
            this.emit = old.emit;
        }

        public GridInfo(string cellid, int trajID, int raylevel, int rayType, double recP, double emitp)
        {
            this.cellid = cellid;
            this.trajID = trajID;
            this.raylevel = raylevel;
            this.rayType = rayType;
            this.recP = recP;//单位为w
            this.pathloss = 10 * (Math.Log10(recP) + 3) - 10 * (Math.Log10(emitp) + 3);
            //if (pathloss > 0)
            //{
            //    Debug.WriteLine("pathloss" + pathloss);
            //}
            this.emit = emitp;
        }
        /// <summary>
        /// 等于返回0值，大于返回大于0的值，小于返回小于0的值。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            GridInfo otherG = obj as GridInfo;
            if (this.pathloss < otherG.pathloss) return -1;
            else if (this.pathloss == otherG.pathloss) return 0;
            else return 1;
        }

    }
}
