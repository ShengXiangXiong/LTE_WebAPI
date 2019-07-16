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
    // 2019.6.13 建筑物
    public class OperateBuildingLayer
    {
        private IFeatureLayer pFeatureLayer;
        private IFeatureClass pFeatureClass;
        private int bidIndex;
        private int heightIndex;

        // 列名
        public OperateBuildingLayer(string layerName)
        {
            IFeatureWorkspace featureWorkspace = MapWorkSpace.getWorkSpace();
            pFeatureClass = featureWorkspace.OpenFeatureClass(layerName);
            pFeatureLayer = new FeatureLayer();
            pFeatureLayer.FeatureClass = pFeatureClass;

            //pFeatureLayer = GISMapApplication.Instance.GetLayer(layerName) as IFeatureLayer;
            //pFeatureClass = pFeatureLayer.FeatureClass;
            this.bidIndex = pFeatureClass.FindField("BID");
            this.heightIndex = pFeatureClass.FindField("Height");
        }


        /// <summary>
        /// 删除图层所有要素
        /// </summary>
        public void ClearLayer()
        {
            FeatureUtilities.DeleteFeatureLayerFeatrues(this.pFeatureLayer);
        }

        /// <summary>
        /// 建筑物  高度+海拔
        /// </summary>
        public bool constuctBuilding()
        {
            DataTable gridTable = IbatisHelper.ExecuteQueryForDataTable("getBuildingInfo", null);
            if (gridTable.Rows.Count < 1)
                return false;

            IDataset dataset = (IDataset)pFeatureLayer.FeatureClass;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            IFeatureCursor pFeatureCursor = pFeatureClass.Insert(true);
            IFeatureBuffer pFeatureBuffer;

            List<IPoint> pts = new List<IPoint>();

            int id = Convert.ToInt32(gridTable.Rows[0]["BuildingID"]);
            double x = Convert.ToDouble(gridTable.Rows[0]["VertexX"]);
            double y = Convert.ToDouble(gridTable.Rows[0]["VertexY"]);
            double height = Convert.ToDouble(gridTable.Rows[0]["BHeight"]);
            double altitude = Convert.ToDouble(gridTable.Rows[0]["BAltitude"]);
            IPoint pointA = GeometryUtilities.ConstructPoint3D(x, y, 0);
            pts.Add(pointA);

            int lastid = id;

            //循环添加
            for(int i=1; i<gridTable.Rows.Count; i++)
            {
                DataRow dataRow = gridTable.Rows[i];

                id = Convert.ToInt32(gridTable.Rows[i]["BuildingID"]);
                
                if (i == gridTable.Rows.Count - 1 || id != lastid)
                {
                    IGeometryCollection pGeometryColl = GeometryUtilities.ConstructPolygon(pts);
                    GeometryUtilities.MakeZAware(pGeometryColl as IGeometry);
                    pFeatureBuffer = pFeatureClass.CreateFeatureBuffer();
                    pFeatureBuffer.Shape = pGeometryColl as IGeometry;
                    pFeatureBuffer.set_Value(this.bidIndex, lastid);
                    pFeatureBuffer.set_Value(this.heightIndex, height + altitude);
                    pFeatureCursor.InsertFeature(pFeatureBuffer);

                    lastid = id;
                    pts.Clear();
                }

                x = Convert.ToDouble(gridTable.Rows[i]["VertexX"]);
                y = Convert.ToDouble(gridTable.Rows[i]["VertexY"]);
                height = Convert.ToDouble(gridTable.Rows[i]["BHeight"]);
                altitude = Convert.ToDouble(gridTable.Rows[i]["BAltitude"]);
                pointA = GeometryUtilities.ConstructPoint3D(x, y, 0);
                pts.Add(pointA);
            }

            //一次性提交
            pFeatureCursor.Flush();

            //stop editing   
            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);

            IFeatureClassManage pFeatureClassManage = (IFeatureClassManage)pFeatureClass;
            pFeatureClassManage.UpdateExtent();
            //GISMapApplication.Instance.RefreshLayer(pFeatureLayer);

            return true;
        }

        /// <summary>
        /// 建筑物  海拔
        /// </summary>
        public bool constuctBuilding1()
        {
            DataTable gridTable = IbatisHelper.ExecuteQueryForDataTable("getBuildingInfo", null);
            if (gridTable.Rows.Count < 1)
                return false;

            IDataset dataset = (IDataset)pFeatureLayer.FeatureClass;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            IFeatureCursor pFeatureCursor = pFeatureClass.Insert(true);
            IFeatureBuffer pFeatureBuffer;

            List<IPoint> pts = new List<IPoint>();

            int id = Convert.ToInt32(gridTable.Rows[0]["BuildingID"]);
            double x = Convert.ToDouble(gridTable.Rows[0]["VertexX"]);
            double y = Convert.ToDouble(gridTable.Rows[0]["VertexY"]);
            double altitude = Convert.ToDouble(gridTable.Rows[0]["BAltitude"]);
            IPoint pointA = GeometryUtilities.ConstructPoint3D(x, y, 0);
            pts.Add(pointA);

            int lastid = id;

            //循环添加
            for (int i = 1; i < gridTable.Rows.Count; i++)
            {
                id = Convert.ToInt32(gridTable.Rows[i]["BuildingID"]);
                
                if (i == gridTable.Rows.Count - 1 || id != lastid)
                {
                    IGeometryCollection pGeometryColl = GeometryUtilities.ConstructPolygon(pts);
                    GeometryUtilities.MakeZAware(pGeometryColl as IGeometry);
                    pFeatureBuffer = pFeatureClass.CreateFeatureBuffer();
                    pFeatureBuffer.Shape = pGeometryColl as IGeometry;
                    pFeatureBuffer.set_Value(this.bidIndex, lastid);
                    pFeatureBuffer.set_Value(this.heightIndex, altitude);
                    pFeatureCursor.InsertFeature(pFeatureBuffer);

                    lastid = id;
                    pts.Clear();
                }

                x = Convert.ToDouble(gridTable.Rows[i]["VertexX"]);
                y = Convert.ToDouble(gridTable.Rows[i]["VertexY"]);
                altitude = Convert.ToDouble(gridTable.Rows[i]["BAltitude"]);
                pointA = GeometryUtilities.ConstructPoint3D(x, y, 0);
                pts.Add(pointA);
            }

            //一次性提交
            pFeatureCursor.Flush();

            //stop editing   
            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);

            IFeatureClassManage pFeatureClassManage = (IFeatureClassManage)pFeatureClass;
            pFeatureClassManage.UpdateExtent();
            //GISMapApplication.Instance.RefreshLayer(pFeatureLayer);
            return true;
        }
    }
}
