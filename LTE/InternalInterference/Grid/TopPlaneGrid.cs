using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Data;
using System.Data.SqlClient;
using System.Collections;

using LTE.DB;
using LTE.Geometric;

namespace LTE.InternalInterference.Grid
{
    public static class TopPlaneGrid
    {
        public static List<Point> GetAllTopGrid(Point source, List<int> buildingIDs)
        {
            List<Point> ret = new List<Point>();
            for (int k = 0; k < buildingIDs.Count; k++)
            {
                int bid = buildingIDs[k];
                double z = BuildingGrid3D.getBuildingHeight(bid);
                if (z < source.Z)  // 当建筑物高度小于小区时，才会向该建筑物顶面引一条射线
                {
                    List<Point> l = BuildingGrid3D.getBuildingTopVertex(bid);
                    ret.AddRange(l);
                }
            }
            return ret;
        }
    }
}
