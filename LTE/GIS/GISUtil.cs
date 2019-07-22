using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Analyst3D;
using ESRI.ArcGIS.Geometry;
using System.Drawing;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Carto;

namespace LTE.GIS
{
    class GISUtil
    {
        /// <summary>
        /// 获取RGB颜色
        /// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static IRgbColor GetRGB(int r, int g, int b)
        {
            IRgbColor pColor;
            pColor = new RgbColorClass();
            pColor.Red = r;
            pColor.Green = g;
            pColor.Blue = b;
            return pColor;
        }

        /// <summary>
        /// 转换成ArcGIS识别的颜色
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static IRgbColor ConvertToRgbColor(Color color)
        {
            IRgbColor rgbColor = new RgbColorClass();
            rgbColor.RGB = color.B * 65536 + color.G * 256 + color.R;

            return rgbColor;

        }

        #region 查询结果作为新图层
        ////QI至IFeatureSelection
        //IFeatureSelection pFeatureSelection = pFeatureLayer as IFeatureSelection;

        //pFeatureSelection.SelectFeatures(null,esriSelectionResultEnum.esriSelectionResultNew, false);
        ////QI到ISelectionSet
        //ISelectionSet pSelectionSet = pFeatureSelection.SelectionSet;
        //if (pSelectionSet.Count > 0)
        //{

        //    IFeatureLayerDefinition pFDefinition = pFeatureLayer as IFeatureLayerDefinition;
        //    //创建新图层
        //    IFeatureLayer pNewFeatureLayer = pFDefinition.CreateSelectionLayer("干扰小区", true, null, null);
        //    pNewFeatureLayer.Name = "查询结果";
        //    GISMapApplication.Instance.AddLayer(pNewFeatureLayer as ILayer);
        //}
        #endregion 查询结果作为新图层


        /// <summary>
        /// 查找Feature
        /// </summary>
        ///  <param name="layerNames">图层名称</param>
        /// <param name="pColumnContent">要定位网元的名称内容</param>
        public static IFeature FindFeature(string layerNames, string pColumnContent)
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
                return null;

            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;

            IQueryFilter queryFilter = new QueryFilter();
            queryFilter.WhereClause = ColumnName + "='" + pColumnContent + "'";
            IFeatureCursor pFCursor = pFeatureClass.Search(queryFilter, false);

            IFeature pFeature = pFCursor.NextFeature();

            while (pFeature != null)
            {

                return pFeature;

            }

            return pFeature;

        }
    }
}
