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

namespace LTE.GIS
{
    // 2019.6.11 建筑物底边平滑
    public class OperateSmoothBuildingLayer
    {
        private IFeatureLayer pFeatureLayer;
        private IFeatureClass pFeatureClass;

        // 列名
        public OperateSmoothBuildingLayer()
        {
            pFeatureLayer = GISMapApplication.Instance.GetLayer(LayerNames.SmoothBuildingVertex) as IFeatureLayer;
            pFeatureClass = pFeatureLayer.FeatureClass;
        }


        /// <summary>
        /// 删除图层所有要素
        /// </summary>
        public void ClearLayer()
        {
            FeatureUtilities.DeleteFeatureLayerFeatrues(this.pFeatureLayer);
        }

        /// <summary>
        /// 平滑后的建筑物底边
        /// </summary>
        public void constuctBuildingVertex()
        {
            DataTable gridTable = IbatisHelper.ExecuteQueryForDataTable("GetAllBuildingVertex", null);

            IDataset dataset = (IDataset)pFeatureLayer.FeatureClass;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            IFeatureCursor pFeatureCursor = pFeatureClass.Insert(true);
            IFeatureBuffer pFeatureBuffer;

            List<IPoint> pts = new List<IPoint>();

            int id = Convert.ToInt32(gridTable.Rows[0]["BuildingID"]);
            float x = (float)Convert.ToDouble(gridTable.Rows[0]["VertexX"].ToString());
            float y = (float)Convert.ToDouble(gridTable.Rows[0]["VertexY"].ToString());
            IPoint pointA = GeometryUtilities.ConstructPoint3D(x, y, 0);
            pts.Add(pointA);

            int lastid = id;

            //循环添加
            for(int i=1; i<gridTable.Rows.Count; i++)
            {
                DataRow dataRow = gridTable.Rows[i];

                id = Convert.ToInt32(dataRow["BuildingID"]);
                if (!(float.TryParse(dataRow["VertexX"].ToString(), out x) && float.TryParse(dataRow["VertexY"].ToString(), out y)))
                    continue;

                if (i == gridTable.Rows.Count - 1 || id != lastid)
                {
                    IGeometryCollection pGeometryColl = GeometryUtilities.ConstructMultiPath(pts);
                    pFeatureBuffer = pFeatureClass.CreateFeatureBuffer();
                    pFeatureBuffer.Shape = pGeometryColl as IGeometry;
                    pFeatureCursor.InsertFeature(pFeatureBuffer);

                    lastid = id;
                    pts.Clear();
                }

                pointA = GeometryUtilities.ConstructPoint3D(x, y, 0);
                pts.Add(pointA);
            }

            //一次性提交
            pFeatureCursor.Flush();

            //stop editing   
            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);

            GISMapApplication.Instance.RefreshLayer(pFeatureLayer);
        }
    }
}
