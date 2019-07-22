using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;

namespace LTE.GIS
{
    public static class GraphicsUtilities
    {
        public static double GetAngle(IPoint from,IPoint to)
        {
            ILine pLine = new LineClass();
            pLine.PutCoords(from, to);
            return pLine.Angle;
        }

        public static bool Intersect_RayPology(IPoint from, IPolygon i_polygon, double angle)
        {
            double pAngle,maxAngle,minAngle;
            maxAngle=2*Math.PI;
            minAngle=0;
            Polygon polygon=i_polygon as Polygon;
            for (int i = 0; i < polygon.PointCount; i++)
            {
                IPoint to = polygon.get_Point(i);
                if (to == null) continue;
                pAngle = GetAngle(from, to);
                if (pAngle < maxAngle) maxAngle = pAngle;
                if (pAngle > minAngle) minAngle = pAngle;
            }
            if (angle < maxAngle && angle > minAngle) return true;
            return false;
        }

    }
}
