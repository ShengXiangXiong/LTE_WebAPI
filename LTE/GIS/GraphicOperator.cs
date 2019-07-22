using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Analyst3D;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using stdole;

namespace LTE.GIS
{
    /// <summary>
    /// 图形操作
    /// </summary>
    internal class GraphicOperator
    {
        ///<summary>在地图上绘制指定颜色的图形</summary> 
        ///<param name="scene">地图</param>
        ///<param name="geometry">feature 的shape</param>
        ///<param name="rgbColor">颜色</param>
        ///<param name="outlineRgbColor">边框颜色</param>
        ///<param name="OffsetZs">Z偏值</param>
        ///      
        ///<remarks>Calling this function will not automatically make the graphics appear in the map area. Refresh the map area after after calling this function with Methods like IActiveView.Refresh or IActiveView.PartialRefresh.</remarks>
        internal static IElement AddGraphicToScene(IScene scene, ESRI.ArcGIS.Geometry.IGeometry geometry, ESRI.ArcGIS.Display.IRgbColor rgbColor, ESRI.ArcGIS.Display.IRgbColor outlineRgbColor, double OffsetZs)
        {
            //ESRI.ArcGIS.Carto.IGraphicsContainer graphicsContainer = (ESRI.ArcGIS.Carto.IGraphicsContainer)map; //IGraphicsContainer接口能删除
            IGraphicsContainer3D graphicsContainer3D = (IGraphicsContainer3D)scene.BasicGraphicsLayer;

            ESRI.ArcGIS.Carto.IElement element = null;
            if ((geometry.GeometryType) == ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPoint)
            {

                IPoint point = (Point)geometry;

                try
                {
                    double X = point.X;
                    double Y = point.Y;
                }
                catch
                {
                    return null;
                }
                point = GeometryUtilities.ConstructPoint3D(point, OffsetZs);
                // Marker symbols
                ESRI.ArcGIS.Display.ISimpleMarkerSymbol simpleMarkerSymbol = new ESRI.ArcGIS.Display.SimpleMarkerSymbolClass();
                simpleMarkerSymbol.Color = rgbColor;
                simpleMarkerSymbol.Outline = true;
                simpleMarkerSymbol.OutlineColor = rgbColor;
                simpleMarkerSymbol.Size = 12;
                simpleMarkerSymbol.Style = ESRI.ArcGIS.Display.esriSimpleMarkerStyle.esriSMSCircle;

                ESRI.ArcGIS.Carto.IMarkerElement markerElement = new ESRI.ArcGIS.Carto.MarkerElementClass();
                markerElement.Symbol = simpleMarkerSymbol;
                element = (ESRI.ArcGIS.Carto.IElement)markerElement; // Explicit Cast

                if (!(element == null))
                {

                    element.Geometry = point;

                    graphicsContainer3D.AddElement(element);


                }

            }
            else if ((geometry.GeometryType) == ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPolyline)
            {
                // Marker symbols
                ESRI.ArcGIS.Display.ISimpleLineSymbol simpleLineSymbol = new ESRI.ArcGIS.Display.SimpleLineSymbolClass();
                simpleLineSymbol.Color = rgbColor;
                simpleLineSymbol.Style = ESRI.ArcGIS.Display.esriSimpleLineStyle.esriSLSSolid;
                simpleLineSymbol.Width = 1;

                ESRI.ArcGIS.Carto.ILineElement lineElement = new ESRI.ArcGIS.Carto.LineElementClass();
                lineElement.Symbol = simpleLineSymbol;
                element = (ESRI.ArcGIS.Carto.IElement)lineElement; // Explicit Cast

                if (!(element == null))
                {
                    element.Geometry = geometry;
                    graphicsContainer3D.AddElement(element);

                }
            }
            else if ((geometry.GeometryType) == ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPolygon)
            {

                IZ iz = (IZ)geometry;
                iz.OffsetZs(OffsetZs);//z值向上偏移

                // Polygon elements
                ESRI.ArcGIS.Display.ISimpleFillSymbol simpleFillSymbol = new ESRI.ArcGIS.Display.SimpleFillSymbolClass();
                simpleFillSymbol.Color = rgbColor;
                simpleFillSymbol.Style = ESRI.ArcGIS.Display.esriSimpleFillStyle.esriSFSForwardDiagonal;
                ESRI.ArcGIS.Carto.IFillShapeElement fillShapeElement = new ESRI.ArcGIS.Carto.PolygonElementClass();
                fillShapeElement.Symbol = simpleFillSymbol;
                element = (ESRI.ArcGIS.Carto.IElement)fillShapeElement; // Explicit Cast

                if (!(element == null))
                {
                    element.Geometry = geometry;
                    graphicsContainer3D.AddElement(element);

                }

            }



            return element;
        }

        /// <summary>
        /// 在地图上绘制指定颜色的文字
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="geometry"></param>
        /// <param name="OffsetZs"></param>
        /// <param name="rgbColor"></param>
        /// <param name="text"></param>
        /// <param name="fontSize"></param>
        /// <returns></returns>
        internal static IElement AddTextGraphicToScene(IScene scene, ESRI.ArcGIS.Geometry.IGeometry geometry, double OffsetZs, ESRI.ArcGIS.Display.IRgbColor rgbColor, string text, int fontSize)
        {
            IGraphicsContainer3D graphicsContainer3D = (IGraphicsContainer3D)scene.BasicGraphicsLayer;

            IText3DElement pTextElement = new Text3DElementClass();
            IFillShapeElement pFillShapeElement = new Text3DElementClass();
            pTextElement.Text = text;


            IFillSymbol pFillSymbol = new SimpleFillSymbol();
            pFillSymbol.Color = rgbColor;//填充的颜色

            IPoint point;
            try
            {
                IArea3D Area3D = (IArea3D)geometry;

                point = Area3D.Centroid3D;
            }
            catch
            {
                point = (IPoint)geometry;
                GeometryUtilities.MakeZAware(point);
            }
            point.Z = point.Z + OffsetZs;
            pTextElement.AnchorPoint = point;//添加文本的坐标点
            pTextElement.Justification = esriT3DJustification.esriT3DJustifyCenter; //注记排放方式
            pTextElement.OrientationPlane = esriT3DOrientationPlane.esriT3DPlaneXY;//注记的旋转平面
            pTextElement.AxisRotation = esriT3DRotationAxis.esriT3DRotateAxisZ;//注记旋转轴
            //pTextElement.RotationAngle=....;//注记的旋转角度

            pTextElement.ZAxisScale = 1;
            pTextElement.Depth = 0.6;//文本的深度
            pTextElement.Height = fontSize;//文本的高度,即文字大小
            pTextElement.Update();
            pFillShapeElement = (IFillShapeElement)pTextElement;
            pFillShapeElement.Symbol = pFillSymbol;

            graphicsContainer3D.AddElement(pTextElement as IElement);
            return pTextElement as IElement;
        }


    }
}
