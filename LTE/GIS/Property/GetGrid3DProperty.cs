using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LTE.DB;
using LTE.Model;

using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Analyst3D;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;
using LTE.InternalInterference.Grid;

namespace LTE.GIS
{
    public class GetGrid3DProperty
    {
        public object getSelectedGrid(string layerName)
        {
            IFeatureLayer pFeatureLayer;
            IFeatureSelection pFestureSelection;
            ISelectionSet pSelection;
            IEnumIDs pEnumIDs;
            IFeature pFeature;

            pFeatureLayer = GISMapApplication.Instance.GetLayer(layerName) as IFeatureLayer;
            pFestureSelection = pFeatureLayer as IFeatureSelection;
            pSelection = pFestureSelection.SelectionSet;
            pEnumIDs = pSelection.IDs;
            int ID = pEnumIDs.Next();

            if (ID == -1)
                return null;

            int eNodeBIndex = pFeatureLayer.FeatureClass.Fields.FindField("eNodeB");
            int CIIndex = pFeatureLayer.FeatureClass.Fields.FindField("CI");
            int GXIDIndex = pFeatureLayer.FeatureClass.Fields.FindField("GXID");
            int GYIDIndex = pFeatureLayer.FeatureClass.Fields.FindField("GYID");
            int LevelIndex = pFeatureLayer.FeatureClass.Fields.FindField("Level");

            pFeature = pFeatureLayer.FeatureClass.GetFeature(ID);
            int eNodeB = int.Parse(pFeature.get_Value(eNodeBIndex).ToString());
            int CI = int.Parse(pFeature.get_Value(CIIndex).ToString());
            int GXID = int.Parse(pFeature.get_Value(GXIDIndex).ToString());
            int GYID = int.Parse(pFeature.get_Value(GYIDIndex).ToString());
            int Level = int.Parse(pFeature.get_Value(LevelIndex).ToString());

            Hashtable ht = new Hashtable();
            ht["eNodeB"]=eNodeB;
            ht["CI"] = CI;
            ht["GXID"]=GXID;
            ht["GYID"]=GYID;
            ht["Level"] = Level;

            PropertyGrid pg =IbatisHelper.ExecuteQueryForObject<PropertyGrid>("GETGrid3DProperties",ht);
            pg.Height = (pg.Level - 1) * GridHelper.getInstance().getGHeight() + GridHelper.getInstance().getGBaseHeight();

            return (object)pg;
        }

 
    }
}
