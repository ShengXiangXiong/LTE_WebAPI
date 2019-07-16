using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using LTE.DB;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Analyst3D;

namespace LTE.GIS
{
    public class DrawPointDemo
    {
        public static bool refreshInfLayer()
        {
            DataTable gridTable = IbatisHelper.ExecuteQueryForDataTable("GetInfSource", null);
            if (gridTable.Rows.Count < 1)
                return false;

            IRgbColor pColor = new RgbColorClass();  //颜色
            IGraphicsLayer pLayer = ((object)GISMapApplication.Instance.Scene as IBasicMap).BasicGraphicsLayer;
            IPoint pt = null;

            pColor.RGB = System.Drawing.Color.FromArgb(0, 0, 255).ToArgb();

            foreach (DataRow dataRow in gridTable.Rows)
            {
                pt = GeometryUtilities.ConstructPoint3D(double.Parse(dataRow["x"].ToString()), double.Parse(dataRow["y"].ToString()), double.Parse(dataRow["z"].ToString()));
                DrawUtilities.DrawPoint(pLayer as IGraphicsContainer3D, pt, pColor, 25);
            }

            return true;
        }
    }
}
