using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Analyst3D;
using LTE.DB;
using LTE.InternalInterference.Grid;

namespace LTE.GIS
{
    /// <summary>
    /// </summary>
    public class OperateInterferenceLocLayer
    {
        private IFeatureLayer pFeatureLayer;
        private IFeatureClass pFeatureClass;
        private int GXIDIndex;
        private int GYIDIndex;
        private int LevelIndex;
        private int RecePowerIndex;

        public OperateInterferenceLocLayer()
        {
            pFeatureLayer = GISMapApplication.Instance.GetLayer(LayerNames.InfSource) as IFeatureLayer;
            pFeatureClass = pFeatureLayer.FeatureClass;
            this.GXIDIndex = pFeatureClass.FindField("GXID");
            this.GYIDIndex = pFeatureClass.FindField("GYID");
            this.LevelIndex = pFeatureClass.FindField("Level");
            this.RecePowerIndex = pFeatureClass.FindField("RecePower");
        }

        /// <summary>
        /// 删除图层所有要素
        /// </summary>
        public void ClearLayer()
        {
            FeatureUtilities.DeleteFeatureLayerFeatrues(this.pFeatureLayer);
        }

        public bool constuctGrid3Ds()
        {
            DataTable gridTable = IbatisHelper.ExecuteQueryForDataTable("GetInfSource", null);
            if (gridTable.Rows.Count < 1)
                return false;

            IDataset dataset = (IDataset)pFeatureLayer.FeatureClass;
            IWorkspace workspace = dataset.Workspace;
            //Cast for an IWorkspaceEdit   
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
            //start an edit session and operation               
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            IFeatureCursor pFeatureCursor = pFeatureClass.Insert(true);
            IFeatureBuffer pFeatureBuffer;

            int gxid = 0, gyid = 0;
            double x, y, r = 10, x1, y1, x2, y2, z;
            //循环添加
            foreach (DataRow dataRow in gridTable.Rows)
            {
                x = double.Parse(dataRow["x"].ToString());
                y = double.Parse(dataRow["y"].ToString());
                z = double.Parse(dataRow["z"].ToString());

                GridHelper.getInstance().XYToGGrid(x, y, ref gxid, ref gyid);

                x1 = x - r;
                x2 = x + r;
                y1 = y - r;
                y2 = y + r;

                IPoint pointA = GeometryUtilities.ConstructPoint3D(x1, y1, z);
                IPoint pointB = GeometryUtilities.ConstructPoint3D(x2, y1, z);
                IPoint pointC = GeometryUtilities.ConstructPoint3D(x2, y2, z);
                IPoint pointD = GeometryUtilities.ConstructPoint3D(x1, y2, z);

                IGeometryCollection pGeometryColl = GeometryUtilities.ConstructPolygon(new IPoint[] { pointA, pointB, pointC, pointD });
                GeometryUtilities.MakeZAware(pGeometryColl as IGeometry);

                pFeatureBuffer = pFeatureClass.CreateFeatureBuffer();
                pFeatureBuffer.Shape = pGeometryColl as IGeometry;
                pFeatureBuffer.set_Value(this.GXIDIndex, gxid);
                pFeatureBuffer.set_Value(this.GYIDIndex, gyid);
                pFeatureBuffer.set_Value(this.LevelIndex, z);

                pFeatureBuffer.set_Value(this.RecePowerIndex, -41);
                pFeatureCursor.InsertFeature(pFeatureBuffer);
            }

            //一次性提交
            pFeatureCursor.Flush();

            //stop editing   
            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);

            GISMapApplication.Instance.RefreshLayer(pFeatureLayer);
            return true;
        }
    }
}
