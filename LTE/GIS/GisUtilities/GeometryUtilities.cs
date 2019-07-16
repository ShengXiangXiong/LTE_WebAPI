using System;
using System.Linq;
using System.Collections.Generic;

using System.IO;

using ESRI.ArcGIS.Geometry;

using LTE.Geometric;
using LTE.InternalInterference;

namespace LTE.GIS
{
    public static class GeometryUtilities
    {
        private static object _missing = Type.Missing;

        public static void MakeZAware(IGeometry geometry)
        {
            IZAware zAware = geometry as IZAware;
            zAware.ZAware = true;
        }

        public static IVector3D ConstructVector3D(double xComponent, double yComponent, double zComponent)
        {
            IVector3D vector3D = new Vector3DClass();
            vector3D.SetComponents(xComponent, yComponent, zComponent);
            return vector3D;
        }


        public static IEnvelope2 Construct_Quadiant_Envelope(double originX, double originY, double width, double height, int Quadrant)
        {
            IEnvelope2 pEnvelope = new EnvelopeClass();
            switch (Quadrant)
            {
                case 1: pEnvelope.PutCoords(originX, originY, originX + width, originY + height); break;
                case 2: pEnvelope.PutCoords(originX - width, originY + height, originX, originY + height); break;
                case 3: pEnvelope.PutCoords(originX - width, originY - height, originX, originY); break;
                case 4: pEnvelope.PutCoords(originX, originY - height, originX + width, originY); break;
            }
            return pEnvelope;
        }

        public static IPoint ConstructPoint_AngleDistance(IPoint point, double angle, double distance)
        {
            IPoint pPoint = new PointClass();
            IConstructPoint pConstructPoint = pPoint as IConstructPoint;
            pConstructPoint.ConstructAngleDistance(point, angle, distance);
            return pPoint;
        }



        //public static IRing Construct_Quadiant_Envelope(double originX, double originY, double width, double height, double angle)
        //{
        //    IEnvelope2 pEnvelope = new EnvelopeClass();
        //    switch (Quadrant)
        //    {
        //        case 1: pEnvelope.PutCoords(originX, originY, originX + width, originY + height); break;
        //        case 2: pEnvelope.PutCoords(originX - width, originY + height, originX, originY + height); break;
        //        case 3: pEnvelope.PutCoords(originX - width, originY - height, originX, originY); break;
        //        case 4: pEnvelope.PutCoords(originX, originY - height, originX + width, originY); break;
        //    }
        //    return pEnvelope;
        //}


        public static IPoint ConstructPoint3D(double x, double y, double z)
        {
            IPoint point = ConstructPoint2D(x, y);
            point.Z = z;

            MakeZAware(point as IGeometry);

            return point;
        }

        public static LTE.Geometric.Point ConstructPoint3D(LTE.Geometric.Point point, double z)
        {
            return new LTE.Geometric.Point(point.X, point.Y, z);
        }

        public static IPoint ConstructPoint3D(IPoint point, double z)
        {
            IPoint point3D = ConstructPoint2D(point.X, point.Y);
            point3D.Z = z;

            MakeZAware(point3D as IGeometry);

            return point3D;
        }


        public static IPoint ConstructPoint2D(double x, double y)
        {
            IPoint point = new PointClass();
            point.X = x;
            point.Y = y;
            point.Z = 0;
            return point;
        }

        public static IPoint ConstructPoint2D(IPoint point3D)
        {
            IPoint point2D = new PointClass();
            point2D.X = point3D.X;
            point2D.Y = point3D.Y;
            return point2D;
        }

        public static IPoint CopyPoint2D(IPoint point)
        {
            IPoint cPoint = new PointClass();
            cPoint.X = point.X;
            cPoint.Y = point.Y;
            return cPoint;
        }

        public static IPoint CopyPoint3D(IPoint point)
        {
            IPoint cPoint;
            cPoint = new PointClass();
            cPoint.X = point.X;
            cPoint.Y = point.Y;
            cPoint.Z = point.Z;
            MakeZAware(cPoint as IGeometry);
            return cPoint;
        }

        public static IGeometry ConvertCS(IGeometry geometry, ISpatialReference original, ISpatialReference current)
        {
            geometry.SpatialReference = original;
            geometry.Project(current);
            return geometry;
        }
        public static IGeometry ConvertGeoToProj(IGeometry geometry)
        {
            ISpatialReferenceFactory2 originalSpatialReferenceFactory = new SpatialReferenceEnvironmentClass();
            ISpatialReference originalSpatialReference = originalSpatialReferenceFactory.CreateGeographicCoordinateSystem((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);

            ISpatialReferenceFactory2 currentSpatialReferenceFactory = new SpatialReferenceEnvironmentClass();
            ISpatialReference currentSpatialReference = currentSpatialReferenceFactory.CreateProjectedCoordinateSystem((int)esriSRProjCSType.esriSRProjCS_WGS1984UTM_49N);

            return GeometryUtilities.ConvertCS(geometry, originalSpatialReference, currentSpatialReference);
        }

        public static IGeometry ConvertProjToGeo(IGeometry geometry)
        {
            ISpatialReferenceFactory2 originalSpatialReferenceFactory = new SpatialReferenceEnvironmentClass();
            ISpatialReference originalSpatialReference = originalSpatialReferenceFactory.CreateProjectedCoordinateSystem((int)esriSRProjCSType.esriSRProjCS_WGS1984UTM_49N);

            ISpatialReferenceFactory2 currentSpatialReferenceFactory = new SpatialReferenceEnvironmentClass();
            ISpatialReference currentSpatialReference = currentSpatialReferenceFactory.CreateGeographicCoordinateSystem((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);

            return GeometryUtilities.ConvertCS(geometry, originalSpatialReference, currentSpatialReference);
        }

        public static ILine ConstructLine(IPoint from, IPoint to)
        {
            ILine pline = new LineClass();
            pline.PutCoords(from, to);
            return pline;
        }

        public static ICircularArc ConstructCircle(IPoint centerPoint, double radius)
        {
            ICircularArc circularArc = new CircularArcClass();
            IConstructCircularArc constructCircluarArc = circularArc as IConstructCircularArc;
            constructCircluarArc.ConstructCircle(centerPoint, radius, true);
            return circularArc;
        }

        public static ICircularArc ConstructCircularArc(IPoint centerPoint, IPoint fromPoint, bool isCCW, double arcDistance)
        {
            ICircularArc circularArc = new CircularArcClass();
            IConstructCircularArc constructCircluarArc = circularArc as IConstructCircularArc;
            constructCircluarArc.ConstructArcDistance(centerPoint, fromPoint, isCCW, arcDistance);
            return circularArc;
        }


        public static IGeometryCollection ConstructPolygon(IPoint[] pointArray)
        {
            //创建一个Ring对象，通过ISegmentCollection接口向其中添加Segment对象
            object o = Type.Missing;
            ISegmentCollection pSegCollection = new RingClass();
            for (int i = 0; i < pointArray.Length - 1; i++)
            {
                IPoint from = pointArray[i];
                IPoint to = pointArray[i + 1];

                ILine pLine = new LineClass();
                //设置Line对象的起始终止点
                pLine.PutCoords(from, to);
                //QI到ISegment
                ISegment pSegment = pLine as ISegment;
                pSegCollection.AddSegment(pSegment, ref o, ref o);
            }
            //QI到IRing接口封闭Ring对象，使其有效
            IRing pRing = pSegCollection as IRing;
            pRing.Close();

            //使用Ring对象构建Polygon对象
            IGeometryCollection pGeometryColl = new PolygonClass();
            pGeometryColl.AddGeometry(pRing, ref o, ref o);

            return pGeometryColl;
        }

        public static IGeometryCollection ConstructSector(IPoint[] pointArray, double circRadius, double angle)
        {
            // 边
            ILine pLine = new LineClass();
            pLine.PutCoords(pointArray[0], pointArray[1]);

            // 弧
            //ESRI.ArcGIS.Geometry.ICircularArc cir = new CircularArcClass();
            //ESRI.ArcGIS.Geometry.IConstructCircularArc con = new CircularArcClass();
            //cir = con as ICircularArc;
            //cir.PutCoordsByAngle(pointArray[0], angle * Math.PI / 180, 40 * Math.PI / 180, circRadius);

            // 边
            ILine pLine1 = new LineClass();
            pLine.PutCoords(pointArray[2], pointArray[0]);

            //创建一个Ring对象，通过ISegmentCollection接口向其中添加Segment对象
            object o = Type.Missing;
            ISegmentCollection pSegCollection = new PathClass();

            // 添加进pSegCollection
            pSegCollection.AddSegment(pLine as ISegment, ref o, ref o);
            //pSegCollection.AddSegment(cir as ISegment, ref o, ref o);
            pSegCollection.AddSegment(pLine1 as ISegment, ref o, ref o);

            // QI到IRing接口封闭Ring对象，使其有效
            //IPath pRing = pSegCollection as IPath;
            //pRing.Close();

            // 使用Ring对象构建Polygon对象
            IGeometryCollection pGeometryColl = new PolylineClass();
            pGeometryColl.AddGeometry(pSegCollection as IGeometry, ref o, ref o);

            return pGeometryColl;
        }

        public static IGeometryCollection ConstructPolygon(ISegment[] segmentArray)
        {
            //创建一个Ring对象，通过ISegmentCollection接口向其中添加Segment对象
            object o = Type.Missing;
            ISegmentCollection pSegCollection = new RingClass();
            for (int i = 0; i < segmentArray.Length; i++)
            {
                pSegCollection.AddSegment(segmentArray[i], ref o, ref o);
            }
            //QI到IRing接口封闭Ring对象，使其有效
            IRing pRing = pSegCollection as IRing;
            pRing.Close();

            //使用Ring对象构建Polygon对象
            IGeometryCollection pGeometryColl = new PolygonClass();
            pGeometryColl.AddGeometry(pRing, ref o, ref o);

            return pGeometryColl;
        }

        public static IPoint[] GetPointCollectionOfPolygon(IPolygon polygon)
        {
            Polygon pPolygon = polygon as Polygon;
            int pointCount = pPolygon.PointCount;
            IPoint[] pointArray = new IPoint[pointCount];
            int j = 0;
            IPoint prePoint = GeometryUtilities.ConstructPoint2D(0, 0), t_p;
            for (int i = 0; i < pointCount; i++)
            {
                t_p = pPolygon.get_Point(i);
                if (t_p == null) continue;
                if (Math.Pow(prePoint.X - t_p.X, 2) + Math.Pow(prePoint.Y - t_p.Y, 2) < 9) continue;
                pointArray[j] = prePoint = t_p;
                pointArray[j].Z = 0;
                j++;
            }
            pointArray = pointArray.Take(j).ToArray();
            return pointArray;
        }

        public static List<IPoint> convertToIPoint(List<LTE.Geometric.Point> points)
        {
            List<IPoint> ret = new List<IPoint>();
            IPoint t1;
            LTE.Geometric.Point t2;
            for (int i = 0, cnt = points.Count; i < cnt; i++)
            {
                t1 = new PointClass();
                t2 = points[i];
                t1.X = t2.X;
                t1.Y = t2.Y;
                t1.Z = t2.Z;
                MakeZAware(t1 as IGeometry);
                ret.Add(t1);
            }
            return ret;
        }

        /// <summary>
        /// 获取polygon的点集（大地坐标），如果一个点距离前一个点距离太小，则忽略掉
        /// </summary>
        /// <param name="polygon">大地坐标</param>
        /// <returns></returns>
        public static List<IPoint> GetPointListOfPolygon(IPolygon polygon)
        {
            Polygon pPolygon = polygon as Polygon;
            int pointCount = pPolygon.PointCount;
            List<IPoint> pointList = new List<IPoint>();
            IPoint prePoint = GeometryUtilities.ConstructPoint2D(0, 0), curPoint;
            for (int i = 0; i < pointCount; i++)
            {
                curPoint = pPolygon.get_Point(i);
                if (curPoint == null) continue;
                if (Math.Pow(prePoint.X - curPoint.X, 2) + Math.Pow(prePoint.Y - curPoint.Y, 2) < 9)
                    continue;
                prePoint = curPoint;
                pointList.Add(GeometryUtilities.ConstructPoint3D(curPoint, 0));

            }
            return pointList;
        }

        public static ISegmentCollection GetSegmentCollectionOfPolygon(IPolygon pPolygon)
        {
            ISegmentCollection pSegCollection = new RingClass();
            IRing pRing = pSegCollection as IRing;
            pPolygon.QueryExteriorRings(ref pRing);
            return pRing as ISegmentCollection;
            //try
            //{
            //    ISegmentCollection pSegCollection = new RingClass();
            //    IRing pRing = pSegCollection as IRing;
            //    pPolygon.QueryExteriorRings(ref pRing);
            //    return pRing as ISegmentCollection;
            //}
            //catch (Exception e)
            //{
            //    return null;
            //}
        }


        public static ISegmentCollection GetSegmentCollectionOfPolygon(IGeometry pGeometry)
        {
            IPolygon4 pPolygon = pGeometry as IPolygon4;
            IGeometryBag exteriorRings = pPolygon.ExteriorRingBag;
            IEnumGeometry exteriorRingsEnum = exteriorRings as IEnumGeometry;
            IRing currentExteriorRing = exteriorRingsEnum.Next() as IRing;
            return currentExteriorRing as ISegmentCollection;
        }

        public static IGeometryCollection ConstructMultiPatchOutline(IGeometry multiPatchGeometry)
        {
            IGeometryCollection outlineGeometryCollection = new GeometryBagClass();

            IGeometryCollection multiPatchGeometryCollection = multiPatchGeometry as IGeometryCollection;

            for (int i = 0; i < multiPatchGeometryCollection.GeometryCount; i++)
            {
                IGeometry geometry = multiPatchGeometryCollection.get_Geometry(i);

                switch (geometry.GeometryType)
                {
                    case (esriGeometryType.esriGeometryTriangleStrip):
                        outlineGeometryCollection.AddGeometryCollection(ConstructTriangleStripOutline(geometry));
                        break;

                    case (esriGeometryType.esriGeometryTriangleFan):
                        outlineGeometryCollection.AddGeometryCollection(ConstructTriangleFanOutline(geometry));
                        break;

                    case (esriGeometryType.esriGeometryTriangles):
                        outlineGeometryCollection.AddGeometryCollection(ConstructTrianglesOutline(geometry));
                        break;

                    case (esriGeometryType.esriGeometryRing):
                        outlineGeometryCollection.AddGeometry(ConstructRingOutline(geometry), ref _missing, ref _missing);
                        break;

                    default:
                        throw new Exception("Unhandled Geometry Type. " + geometry.GeometryType);
                }
            }

            return outlineGeometryCollection;
        }

        public static IGeometryCollection ConstructTriangleStripOutline(IGeometry triangleStripGeometry)
        {
            IGeometryCollection outlineGeometryCollection = new GeometryBagClass();

            IPointCollection triangleStripPointCollection = triangleStripGeometry as IPointCollection;

            // TriangleStrip: a linked strip of triangles, where every vertex (after the first two) completes a new triangle.
            //                A new triangle is always formed by connecting the new vertex with its two immediate predecessors.

            for (int i = 2; i < triangleStripPointCollection.PointCount; i++)
            {
                IPointCollection outlinePointCollection = new PolylineClass();

                outlinePointCollection.AddPoint(triangleStripPointCollection.get_Point(i - 2), ref _missing, ref _missing);
                outlinePointCollection.AddPoint(triangleStripPointCollection.get_Point(i - 1), ref _missing, ref _missing);
                outlinePointCollection.AddPoint(triangleStripPointCollection.get_Point(i), ref _missing, ref _missing);
                outlinePointCollection.AddPoint(triangleStripPointCollection.get_Point(i - 2), ref _missing, ref _missing); //Simulate: Polygon.Close

                IGeometry outlineGeometry = outlinePointCollection as IGeometry;

                MakeZAware(outlineGeometry);

                outlineGeometryCollection.AddGeometry(outlineGeometry, ref _missing, ref _missing);
            }

            return outlineGeometryCollection;
        }

        public static IGeometryCollection ConstructTriangleFanOutline(IGeometry triangleFanGeometry)
        {
            IGeometryCollection outlineGeometryCollection = new GeometryBagClass();

            IPointCollection triangleFanPointCollection = triangleFanGeometry as IPointCollection;

            // TriangleFan: a linked fan of triangles, where every vertex (after the first two) completes a new triangle. 
            //              A new triangle is always formed by connecting the new vertex with its immediate predecessor 
            //              and the first vertex of the part.

            for (int i = 2; i < triangleFanPointCollection.PointCount; i++)
            {
                IPointCollection outlinePointCollection = new PolylineClass();

                outlinePointCollection.AddPoint(triangleFanPointCollection.get_Point(0), ref _missing, ref _missing);
                outlinePointCollection.AddPoint(triangleFanPointCollection.get_Point(i - 1), ref _missing, ref _missing);
                outlinePointCollection.AddPoint(triangleFanPointCollection.get_Point(i), ref _missing, ref _missing);
                outlinePointCollection.AddPoint(triangleFanPointCollection.get_Point(0), ref _missing, ref _missing); //Simulate: Polygon.Close

                IGeometry outlineGeometry = outlinePointCollection as IGeometry;

                MakeZAware(outlineGeometry);

                outlineGeometryCollection.AddGeometry(outlineGeometry, ref _missing, ref _missing);
            }

            return outlineGeometryCollection;
        }

        public static IGeometryCollection ConstructTrianglesOutline(IGeometry trianglesGeometry)
        {
            IGeometryCollection outlineGeometryCollection = new GeometryBagClass();

            IPointCollection trianglesPointCollection = trianglesGeometry as IPointCollection;

            // Triangles: an unlinked set of triangles, where every three vertices completes a new triangle.

            if ((trianglesPointCollection.PointCount % 3) != 0)
            {
                throw new Exception("Triangles Geometry Point Count Must Be Divisible By 3. " + trianglesPointCollection.PointCount);
            }
            else
            {
                for (int i = 0; i < trianglesPointCollection.PointCount; i += 3)
                {
                    IPointCollection outlinePointCollection = new PolylineClass();

                    outlinePointCollection.AddPoint(trianglesPointCollection.get_Point(i), ref _missing, ref _missing);
                    outlinePointCollection.AddPoint(trianglesPointCollection.get_Point(i + 1), ref _missing, ref _missing);
                    outlinePointCollection.AddPoint(trianglesPointCollection.get_Point(i + 2), ref _missing, ref _missing);
                    outlinePointCollection.AddPoint(trianglesPointCollection.get_Point(i), ref _missing, ref _missing); //Simulate: Polygon.Close

                    IGeometry outlineGeometry = outlinePointCollection as IGeometry;

                    MakeZAware(outlineGeometry);

                    outlineGeometryCollection.AddGeometry(outlineGeometry, ref _missing, ref _missing);
                }
            }

            return outlineGeometryCollection;
        }

        public static IGeometry ConstructRingOutline(IGeometry ringGeometry)
        {
            IGeometry outlineGeometry = new PolylineClass();

            IPointCollection outlinePointCollection = outlineGeometry as IPointCollection;

            IPointCollection ringPointCollection = ringGeometry as IPointCollection;

            for (int i = 0; i < ringPointCollection.PointCount; i++)
            {
                outlinePointCollection.AddPoint(ringPointCollection.get_Point(i), ref _missing, ref _missing);
            }

            outlinePointCollection.AddPoint(ringPointCollection.get_Point(0), ref _missing, ref _missing); //Simulate: Polygon.Close

            MakeZAware(outlineGeometry);

            return outlineGeometry;
        }
    }
}