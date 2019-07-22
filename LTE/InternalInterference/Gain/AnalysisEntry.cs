using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Analyst3D;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;

using LTE.GIS;
using LTE.InternalInterference;
using LTE.DB;

namespace LTE.InternalInterference
{
    public class AnalysisEntry
    {
        //public void ExcuteSingleRayAnalysis(CellInfo cellInfo)
        //{
        //    if (cellInfo != null)
        //    {
        //        FrmSingleRayTracing1 frm = new FrmSingleRayTracing1(cellInfo.CellName);
        //        frm.Show();
        //    }
        //}
        public void ExcuteAnalysis(CellInfo cellInfo)
        {
            if (cellInfo != null)
            {
                FrmCellRayTracing frm = new FrmCellRayTracing(cellInfo.SourceName, cellInfo.eNodeB, cellInfo.CI);
                frm.Show();
                //IbatisHelper.ExecuteDelete("deleteSpecifiedCelltbGrids", cellInfo.CellName);
                //InterferenceAnalysis interAnalysis = new InterferenceAnalysis(cellInfo, 4, 2);
                //interAnalysis.rayTracing(1500);
                //DiffractedRayAnalysis diffAnalysis = new DiffractedRayAnalysis(cellInfo, 4, 2);
                //diffAnalysis.diffractedRayAnalysis(120, 1500);
            }
        }

        public static void DisplayAnalysis(SourceInfo sourceInfo)
        {
            if (sourceInfo != null)
            {
                string layerName = "小区" + sourceInfo.CI + "覆盖";
                OperateCoverGirdLayer operateGrid = new OperateCoverGirdLayer(layerName);
                operateGrid.ClearLayer();
                operateGrid.constuctCellGrids(sourceInfo.SourceName, sourceInfo.eNodeB, sourceInfo.CI);
            }
        }

        public static void Display3DAnalysis(SourceInfo sourceInfo)
        {
            if (sourceInfo != null)
            {
                string layerName = "小区" + sourceInfo.CI + "立体覆盖";
                OperateCoverGird3DLayer operateGrid = new OperateCoverGird3DLayer(layerName);
                operateGrid.ClearLayer();
                operateGrid.constuctCellGrid3Ds(sourceInfo.SourceName, sourceInfo.eNodeB, sourceInfo.CI);
            }
        }

        public static CellInfo getCellInfo()
        {
            List<string> cellTypeList = new List<string>() { LayerNames.GSM900Cell, LayerNames.GSM1800Cell };  //

            IFeatureLayer pFeatureLayer;
            IFeatureSelection pFestureSelection;
            ISelectionSet pSelection;
            IEnumIDs pEnumIDs;
            IFeature pFeature;

            foreach (string var in cellTypeList)
            {
                pFeatureLayer = GISMapApplication.Instance.GetLayer(var) as IFeatureLayer;
                if (pFeatureLayer == null)
                    return null;
                pFestureSelection = pFeatureLayer as IFeatureSelection;
                pSelection = pFestureSelection.SelectionSet;
                pEnumIDs = pSelection.IDs;
                int ID = pEnumIDs.Next();

                if (ID == -1)
                    continue;
                else
                {
                    int cellnameIndex = pFeatureLayer.FeatureClass.Fields.FindField("CellName");
                    int eNodeBIndex = pFeatureLayer.FeatureClass.Fields.FindField("eNodeB");
                    int CIIndex = pFeatureLayer.FeatureClass.Fields.FindField("CI");
                    pFeature = pFeatureLayer.FeatureClass.GetFeature(ID);
                    string cellName = pFeature.get_Value(cellnameIndex).ToString();
                    int lac = Convert.ToInt32(pFeature.get_Value(eNodeBIndex).ToString());
                    int ci = Convert.ToInt32(pFeature.get_Value(CIIndex).ToString());
                    CellInfo cellinfo = new CellInfo(cellName, lac, ci);
                    return cellinfo;
                }
            }

            return null;
        }
    }
}
