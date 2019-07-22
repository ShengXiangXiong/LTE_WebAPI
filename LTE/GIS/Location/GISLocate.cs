using System;
using System.Collections.Generic;
using System.Text;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Controls;
using System.Windows.Forms;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Analyst3D;

using System.Collections;

//using LTE.Component;
//using LTE.Property;


namespace LTE.GIS
{
    /// <summary>
    /// 地图定位的通用类
    /// </summary>
    public class GISLocate
    {
        private IElement element;
        private static GISLocate instance = null; //当前对象的实例
        private static System.Object m_syncObject = new System.Object();   // 同步对象

        /// <summary>
        /// 当前实例对象
        /// </summary>
        public static GISLocate Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (m_syncObject)
                    {
                        if (instance == null)
                        {
                            instance = new GISLocate();
                        }
                    }

                }
                return instance;
            }
        }

        //当前构造函数
        public GISLocate()
        {

        }


        public event EventHandler<FeatureClickEventArgs> FeatureClick;//声明小区点击事件

        /// <summary>
        /// 将地图定位到指定点
        /// </summary>
        /// <param name="p">指定点</param>
        public void LocateToPoint(IPoint p)
        {
            ILayer referLayer = GISMapApplication.Instance.GetLayer(LayerNames.Projecton);//定位居中参照图层。
            IEnvelope pEnvelope = referLayer.AreaOfInterest;
            pEnvelope.CenterAt(p);
            pEnvelope.Expand(0.15, 0.15, true);
            GISMapApplication.Instance.FullExtent(pEnvelope);
        }

        /// <summary>
        /// 地图定位方法
        /// </summary>
        ///  <param name="layerNames">图层名称</param>
        /// <param name="pColumnContent">要定位网元的名称内容</param>
        public bool Locate(string layerNames, string pColumnContent)
        {

            IFeatureLayer pFeatureLayer = null;
            string ColumnName = null;//图层中小区对应的名称
            string columnLON = null;
            string columnLAT = null;
            switch (layerNames)
            {
                case LayerNames.GSM900Cell:
                    {
                        pFeatureLayer = GISMapApplication.Instance.GetLayer(LayerNames.GSM900Cell) as IFeatureLayer;
                        ColumnName = "cellname";
                        columnLON = "longitude";
                        columnLAT = "latitude";
                        break;
                    }

                case LayerNames.GSM1800Cell:
                    {
                        pFeatureLayer = GISMapApplication.Instance.GetLayer(LayerNames.GSM1800Cell) as IFeatureLayer;
                        ColumnName = "cellname";
                        columnLON = "longitude";
                        columnLAT = "latitude";
                        break;
                    }

            }

            if (pFeatureLayer == null)
                return false;

            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;


            IQueryFilter queryFilter = new QueryFilter();
            queryFilter.WhereClause = ColumnName + "='" + pColumnContent + "'";
            IFeatureCursor pFCursor = pFeatureClass.Search(queryFilter, false);

            IFeature pFeature = pFCursor.NextFeature();

            while (pFeature != null)
            {
                int indexOfName = pFeatureClass.FindField(ColumnName);
                int indexLon = pFeatureClass.FindField(columnLON);
                int indexLat = pFeatureClass.FindField(columnLAT);
                if (pFeature.get_Value(indexOfName).ToString() == pColumnContent)
                {
                    //定位到查询的图元
                    double lon = Convert.ToDouble(pFeature.get_Value(indexLon));
                    double lat = Convert.ToDouble(pFeature.get_Value(indexLat));

                    ISelectionEnvironment pSelectionEnv = new SelectionEnvironmentClass();
                    pSelectionEnv.CombinationMethod = ESRI.ArcGIS.Carto.esriSelectionResultEnum.esriSelectionResultNew;

                    if (pFeatureLayer.Selectable == true)
                    {

                        GISMapApplication.Instance.Scene.ClearSelection();

                        ICommand pCommand = new ControlsZoomToSelectedCommandClass();
                        
                        //Select by Shape
                        IPoint pNewCenterPoint = new PointClass();
                        pNewCenterPoint.X = lon;
                        pNewCenterPoint.Y = lat;
                        PointConvert.Instance.GetProjectPoint(pNewCenterPoint);
                        pSelectionEnv.DefaultColor = GISUtil.GetRGB(255, 0, 0);


                        // 定位居中
                        ILayer referLayer = GISMapApplication.Instance.GetLayer("建筑物");//定位居中参照图层。
                        IEnvelope pEnvelope = referLayer.AreaOfInterest;
                        //IEnvelope pEnvelope = pFeatureLayer.AreaOfInterest;
                        pEnvelope.CenterAt(pNewCenterPoint);
                        pEnvelope.Expand(0.1, 0.1, true);
                        GISMapApplication.Instance.FullExtent(pEnvelope);

                        //GISMapApplication.Instance.Scene.ClearSelection();
                        //GISMapApplication.Instance.Scene.SelectByShape(pFeature.Shape, pSelectionEnv, true);
                        GISMapApplication.Instance.Scene.SelectFeature(pFeatureLayer, pFeature);

                        HandlerFeatureData(layerNames, pFeature);

                        return true;
                    }

                }
            }

            return false;

        }
        
        /// <summary>
        /// 处理要素数据,触发事件
        /// </summary>
        /// <param name="pLayerName"></param>
        /// <param name="pFeature"></param>
        public void HandlerFeatureData(string pLayerName, IFeature pFeature)
        {
            //清空原有的图形选择
            IGraphicsContainer3D graphicsContainer3D = (IGraphicsContainer3D)GISMapApplication.Instance.Scene.BasicGraphicsLayer;
            if (element != null)
            {
                graphicsContainer3D.DeleteElement(element);
            }


            object info = FeatureIdentity.GetFeatureInfo(pLayerName, pFeature);

            //给小区地图赋上名称
            switch (pLayerName)
            {
                case LayerNames.GSM900Cell:
                case LayerNames.GSM1800Cell:
                    {
                        if (info is string)
                        {
                            AddCellNameOnMap(info.ToString(), pFeature.Shape);
                        }
                        break;
                    }
            }

            if (FeatureClick != null)
            {
                FeatureClick(this, new FeatureClickEventArgs(pLayerName, info));
            }
        }

        /// <summary>
        /// 地图上呈现小区的名称
        /// </summary>
        /// <param name="cellName"></param>
        /// <param name="geometry"></param>
        public void AddCellNameOnMap(string cellName, IGeometry geometry)
        {
            //加载textElement
            IRgbColor itextColor = new RgbColorClass();
            itextColor.Red = 0;
            itextColor.Green = 0;
            itextColor.Blue = 0;

            element = GraphicOperator.AddTextGraphicToScene(GISMapApplication.Instance.Scene, geometry, 3, itextColor, cellName, 6);

        }



    }
}
