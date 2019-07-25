using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LTE.Geometric;
using LTE.DB;
using LTE.GIS;

namespace LTE.InternalInterference.Grid
{
    // 2019.5.28 地形
    public class TINInfo
    {
        public static Dictionary<int, List<Point>> TINVertex = new Dictionary<int, List<Point>>();
        private static double MinX;
        private static double MinY;
        private static double MaxX;
        private static double MaxY;

        public static void clear()
        {
            TINVertex.Clear();
        }

        public static void setBound(double minx, double miny, double maxx, double maxy)
        {
            MinX = minx;
            MinY = miny;
            MaxX = maxx;
            MaxY = maxy;
        }

        // 获取区域内的 TIN 
        public static int constructTINVertex()
        {
            //清除前一部分区域的数据，防止内存溢出 2019.7.22 xsx
            TINVertex.Clear();

            Hashtable ht = new Hashtable();
            ht["minX"] = MinX;
            ht["maxX"] = MaxX;
            ht["minY"] = MinY;
            ht["maxY"] = MaxY;

            //DataTable dt = IbatisHelper.ExecuteQueryForDataTable("GetTINVertex", ht);
            //通过矩形覆盖方式取，防止顶点在外面在内的特殊情况
            DataTable dt = IbatisHelper.ExecuteQueryForDataTable("GetTINVertexByArea", ht);

            List<Point> vcollection;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                int TINid = Convert.ToInt32(dt.Rows[i]["TINID"]);
                double x = Convert.ToDouble(dt.Rows[i]["VertexX"]);
                double y = Convert.ToDouble(dt.Rows[i]["VertexY"]);
                double z =  Convert.ToDouble(dt.Rows[i]["VertexHeight"]);
                Point t = new Point(x, y, z);

                if (TINVertex.ContainsKey(TINid))
                {
                    TINVertex[TINid].Add(t);
                }
                else
                {
                    vcollection = new List<Point>();
                    vcollection.Add(t);
                    TINVertex.Add(TINid, vcollection);
                }
            }
            return TINVertex.Count;
        }

        public static List<Point> getTINVertex(int TINid)
        {
            if(TINVertex.Keys.Contains(TINid))
            {
                return TINVertex[TINid];
            }
            return null;
        }

        public static double getTINMaxHeight(int TINid)
        {
            if (!TINVertex.Keys.Contains(TINid))
                return -1;

            double height = TINVertex[TINid][0].Z;
            for (int i = 1; i < TINVertex[TINid].Count; i++)
                if (TINVertex[TINid][i].Z > height)
                    height = TINVertex[TINid][i].Z;
            return height;
        }
    }
}
