using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Data;

using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Analyst3D;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;

using LTE.GIS;
using LTE.Geometric;
using LTE.InternalInterference;
using LTE.InternalInterference.Grid;

namespace LTE.InternalInterference
{
    public static class InterferenceFeatureLayerAnalysis
    {
        public static void drawLine(List<NodeInfo> rayList)
        {
            List<IPoint> ps = new List<IPoint>();
            IGraphicsContainer3D layer = GISMapApplication.Instance.GetLayer(LayerNames.Rays) as IGraphicsContainer3D;
            LTE.Geometric.Point t;
            for (int i = 0, cnt = rayList.Count; i < cnt; i++)
            {
                t = rayList[i].PointOfIncidence;
                IPoint t_p = new PointClass();
                t_p.X = t.X;
                t_p.Y = t.Y;
                t_p.Z = t.Z;
                ps.Add(t_p);
                t = rayList[i].CrossPoint;
                t_p = new PointClass();
                t_p.X = t.X;
                t_p.Y = t.Y;
                t_p.Z = t.Z;
                ps.Add(t_p);
                DrawUtilities.DrawLine(layer, ps);
                ps.Clear();
            }
        }

        public static void drawSector(LTE.Geometric.Point p, double fromAngle, double toAngle, double radius)
        {
            IPoint centralPoint = GeometryUtilities.ConstructPoint2D(p.X, p.Y);

            double arithmeticToAngle = GeometricUtilities.GetRadians(GeometricUtilities.ConvertGeometricArithmeticAngle(toAngle));

            double angle = (toAngle - fromAngle) % 360;
            bool isCCW = true;
            if (angle > 180)
            {
                angle = 360 - angle;
                isCCW = false;
            }
            if (angle == 0)
                angle = 360;
            double arcDistance = radius * GeometricUtilities.GetRadians(angle);

            IPoint fromPoint = GeometryUtilities.ConstructPoint_AngleDistance(centralPoint, arithmeticToAngle, radius);
            ICircularArc circularArc = GeometryUtilities.ConstructCircularArc(centralPoint, fromPoint, isCCW, arcDistance);
            IPoint toPoint = circularArc.ToPoint;

            ISegment fromSegment = GeometryUtilities.ConstructLine(centralPoint, fromPoint) as ISegment;
            ISegment toSegment = GeometryUtilities.ConstructLine(toPoint, centralPoint) as ISegment;

            ISegment[] segmentArray = new ISegment[] { fromSegment, circularArc as ISegment, toSegment };
            IGeometryCollection polygon = GeometryUtilities.ConstructPolygon(segmentArray);

            //画扇形
            IGraphicsContainer3D graphicsContainer3D = GISMapApplication.Instance.GetLayer(LayerNames.Rays) as IGraphicsContainer3D;
            IPolygonElement polygonElement = new PolygonElementClass();
            IElement element = polygonElement as IElement;
            element.Geometry = polygon as IGeometry;
            graphicsContainer3D.AddElement(element);
        }

        /// <summary>
        /// 获取扇形与地面网格相交的网格中心点
        /// </summary>
        /// <param name="p"></param>
        /// <param name="fromAngle"></param>
        /// <param name="toAngle"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static List<LTE.Geometric.Point> getSelectedGridsCenterPoints(LTE.Geometric.Point p, double fromAngle, double toAngle, double radius)
        {

            IPoint centralPoint = GeometryUtilities.ConstructPoint2D(p.X, p.Y);

            double arithmeticToAngle = GeometricUtilities.GetRadians(GeometricUtilities.ConvertGeometricArithmeticAngle(toAngle));

            double angle = (toAngle - fromAngle) % 360;
            bool isCCW = true;
            if (angle > 180)
            {
                angle = 360 - angle;
                isCCW = false;
            }
            if (angle == 0)
                angle = 360;
            double arcDistance = radius * GeometricUtilities.GetRadians(angle);


            IPoint fromPoint = GeometryUtilities.ConstructPoint_AngleDistance(centralPoint, arithmeticToAngle, radius);
            ICircularArc circularArc = GeometryUtilities.ConstructCircularArc(centralPoint, fromPoint, isCCW, arcDistance);
            IPoint toPoint = circularArc.ToPoint;

            ISegment fromSegment = GeometryUtilities.ConstructLine(centralPoint, fromPoint) as ISegment;
            ISegment toSegment = GeometryUtilities.ConstructLine(toPoint, centralPoint) as ISegment;

            ISegment[] segmentArray = new ISegment[] { fromSegment, circularArc as ISegment, toSegment };
            IGeometryCollection polygon = GeometryUtilities.ConstructPolygon(segmentArray);

            //画扇形
            //IGraphicsContainer3D graphicsContainer3D = GISMapApplication.Instance.GetLayer(LayerNames.Rays) as IGraphicsContainer3D;
            //IPolygonElement polygonElement = new PolygonElementClass();
            //IElement element = polygonElement as IElement;
            //element.Geometry = polygon as IGeometry;
            //graphicsContainer3D.AddElement(element);

            IGeometry pGeometry = GeometryUtilities.ConvertProjToGeo(polygon as IGeometry);
            //地面网格图层是经纬度
            IFeatureLayer groundFeatureLayer = GISMapApplication.Instance.GetLayer(LayerNames.GroundGrids) as IFeatureLayer;
            IFeatureClass groundFeatureClass = groundFeatureLayer.FeatureClass;

            ISpatialFilter spatialFilter = new SpatialFilterClass();
            spatialFilter.Geometry = pGeometry;
            //spatialFilter.Geometry = pPolygon as IGeometry;
            spatialFilter.GeometryField = groundFeatureClass.ShapeFieldName;
            spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
            IFeatureCursor featureCursor = groundFeatureClass.Search(spatialFilter, false);

            int CenterXIndex = groundFeatureClass.Fields.FindField("CenterX");
            int CenterYIndex = groundFeatureClass.Fields.FindField("CenterY");
            int xindex = groundFeatureClass.Fields.FindField("GXID");
            int yindex = groundFeatureClass.Fields.FindField("GYID");

            IFeature pFeature;
            List<LTE.Geometric.Point> centerPoints = new List<LTE.Geometric.Point>();
            while ((pFeature = featureCursor.NextFeature()) != null)
            {
                int gxid = (int)pFeature.get_Value(xindex);
                int gyid = (int)pFeature.get_Value(yindex);
                double centerX = double.Parse(pFeature.get_Value(CenterXIndex).ToString());
                double centerY = double.Parse(pFeature.get_Value(CenterYIndex).ToString());

                IPoint crossWithGround = GeometryUtilities.ConstructPoint3D(centerX, centerY, 0);
                double lng = crossWithGround.X, lat = crossWithGround.Y;
                crossWithGround = (IPoint)GeometryUtilities.ConvertGeoToProj(crossWithGround as IGeometry);

                centerPoints.Add(new LTE.Geometric.Point(crossWithGround.X, crossWithGround.Y, crossWithGround.Z));
            }

            System.Runtime.InteropServices.Marshal.ReleaseComObject(featureCursor);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(groundFeatureLayer);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(groundFeatureClass);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(spatialFilter);
            System.Runtime.InteropServices.Marshal.ReleaseThreadCache();

            return centerPoints;
        }

        public static List<int> getSelectedBuildings(LTE.Geometric.Point p, double fromAngle, double toAngle, double radius)
        {
            IPoint centralPoint = GeometryUtilities.ConstructPoint2D(p.X, p.Y);

            double arithmeticToAngle = GeometricUtilities.GetRadians(GeometricUtilities.ConvertGeometricArithmeticAngle(toAngle));
            double angle = (toAngle - fromAngle + 360) % 360;
            bool isCCW = true;
            if (angle > 180)
            {
                angle = 360 - angle;
                isCCW = false;
            }
            else if (angle == 0)
                angle = 360;

            double arcDistance = radius * GeometricUtilities.GetRadians(angle);

            IPoint fromPoint = GeometryUtilities.ConstructPoint_AngleDistance(centralPoint, arithmeticToAngle, radius);
            ICircularArc circularArc = GeometryUtilities.ConstructCircularArc(centralPoint, fromPoint, isCCW, arcDistance); //逆时针
            IPoint toPoint = circularArc.ToPoint;

            ISegment fromSegment = GeometryUtilities.ConstructLine(centralPoint, fromPoint) as ISegment;
            ISegment toSegment = GeometryUtilities.ConstructLine(toPoint, centralPoint) as ISegment;

            ISegment[] segmentArray = new ISegment[] { fromSegment, circularArc as ISegment, toSegment };
            IGeometryCollection polygon = GeometryUtilities.ConstructPolygon(segmentArray);
            IPolygon pPolygon = polygon as IPolygon;

            IGeometry pGeometry = GeometryUtilities.ConvertProjToGeo(pPolygon as IGeometry);

            IFeatureLayer pFeatureLayer = GISMapApplication.Instance.GetLayer(LayerNames.Projecton) as IFeatureLayer;
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;

            ISpatialFilter spatialFilter = new SpatialFilterClass();
            spatialFilter.Geometry = pGeometry;
            spatialFilter.GeometryField = pFeatureClass.ShapeFieldName;
            spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

            //Execute the spatialfilter
            IFeatureCursor featureCursor = pFeatureClass.Search(spatialFilter, false);

            IFeature pFeature = null;
            List<int> bids = new List<int>();
            while ((pFeature = featureCursor.NextFeature()) != null)
            {
                bids.Add(pFeature.OID);
            }

            return bids;
        }
    }
}
