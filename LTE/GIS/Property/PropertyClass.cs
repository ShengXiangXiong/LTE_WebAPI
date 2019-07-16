using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using LTE.DB;

using LTE.Model;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;

namespace LTE.GIS
{
    /// <summary>
    /// 属性类
    /// </summary>
    public class PropertyClass
    {
        /// <summary>
        /// 获取对象
        /// </summary>
        /// <param name="pLayerName">要素图层名称</param>
        /// <param name="cellName">小区名称</param>
        /// <returns></returns>
        public static object GetCellInfo(string pLayerName, string cellName)
        {
            object obj = null;
            string layerName = pLayerName;

            switch (layerName)
            {
                case "GSM900小区":
                case "GSM1800小区":
                    {
                        PropertyCELL pc = IbatisHelper.ExecuteQueryForObject<PropertyCELL>("GETPropertyCELL", cellName);
                        //pc.Tilt = pc.Tilt > 0 ? pc.Tilt : 7;
                        obj = pc;
                        break;
 
                    }
            }

            return obj;

        }
    }
}
