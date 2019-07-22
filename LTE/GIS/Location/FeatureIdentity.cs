using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geometry;
using System.Collections;

//using LTE.Property;
//using LTE.Component.PropertyInfo;

namespace LTE.GIS
{
    /// <summary>
    /// 要素识别   
    /// </summary>
    public class FeatureIdentity
    {

        /// <summary>
        /// 获取要素需要的信息
        /// </summary>
        /// <param name="p_LayerName"></param>
        /// <param name="pFeature"></param>
        /// <returns></returns>
        public static object GetFeatureInfo(string pLayerName, IFeature pFeature)
        {
            string layerName = pLayerName;
            object info = new object();

            switch (layerName)
            {
                case LayerNames.GSM900Cell:
                case LayerNames.GSM1800Cell:
                    {
                        //string name = pFeature.get_Value(pFeature.Fields.FindField("CellName")).ToString();
                        //object obj = PropertyClass.GetCellInfo(layerName, name);
                        LTE.Model.PropertyCELL obj = new LTE.Model.PropertyCELL();  // 2019.5.14 改成直接图层读取，避免查数据库
                        obj.CellName = pFeature.get_Value(pFeature.Fields.FindField("CellName")).ToString();
                        obj.CellNameChs = pFeature.get_Value(pFeature.Fields.FindField("CellNameCN")).ToString();
                        obj.AntHeight = Convert.ToDecimal(pFeature.get_Value(pFeature.Fields.FindField("AntHeight")).ToString());
                        obj.Azimuth = Convert.ToDouble(pFeature.get_Value(pFeature.Fields.FindField("Azimuth")).ToString());
                        obj.CI = Convert.ToInt32(pFeature.get_Value(pFeature.Fields.FindField("CI")).ToString());
                        obj.EARFCN = Convert.ToInt32(pFeature.get_Value(pFeature.Fields.FindField("EARFCN")).ToString());
                        obj.EIRP = Convert.ToInt32(pFeature.get_Value(pFeature.Fields.FindField("EIRP")).ToString());
                        obj.eNodeB = Convert.ToInt32(pFeature.get_Value(pFeature.Fields.FindField("eNodeB")).ToString());
                        obj.Latitude = Convert.ToDecimal(pFeature.get_Value(pFeature.Fields.FindField("Latitude")).ToString());
                        obj.Longitude = Convert.ToDecimal(pFeature.get_Value(pFeature.Fields.FindField("Longitude")).ToString());
                        obj.Tilt = Convert.ToInt32(pFeature.get_Value(pFeature.Fields.FindField("Tilt")).ToString());
                        obj.Radius = Convert.ToDouble(pFeature.get_Value(pFeature.Fields.FindField("Radius")).ToString());
                        PropertyGridControl.Instance.SetObject(obj);
                        break;
                    }
                case LayerNames.Projecton:
                    {
                        IFields fields = pFeature.Fields;
                        Hashtable ht = new Hashtable();
                        for (int i = 0; i < fields.FieldCount; i++)
                        {
                            IField field = fields.get_Field(i);
                            ht[field.Name] = pFeature.get_Value(i).ToString();
                        }
                        PropertyGridControl.Instance.SetObject(ht);
                    }
                    break;
            }

            return info;

        }


    }
}
