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
                        string columnName = "CellName";
                        int index = pFeature.Fields.FindField(columnName);
                        string name = pFeature.get_Value(index).ToString();
                        object obj = PropertyClass.GetCellInfo(layerName, name);
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
