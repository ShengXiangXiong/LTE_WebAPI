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
    // 2019.5.30 地形
    public class OperateTINLayer
    {
        private IFeatureLayer pFeatureLayer;
        private IFeatureClass pFeatureClass;

        // 列名
        public OperateTINLayer(string layerName)
        {
            pFeatureLayer = GISMapApplication.Instance.GetLayer(layerName) as IFeatureLayer;
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
        /// 构造 TIN 轮廓
        /// </summary>
        public void constuctTIN()
        {
            double minX = 0, minY = 0, maxX = 0, maxY = 0;
            InternalInterference.Grid.GridHelper.getInstance().getMinXY(ref minX, ref minY);
            InternalInterference.Grid.GridHelper.getInstance().getMaxXY(ref maxX, ref maxY);

            Hashtable ht = new Hashtable();
            ht["minX"] = minX;
            ht["maxX"] = maxX;
            ht["minY"] = minY;
            ht["maxY"] = maxY;
            DataTable gridTable = IbatisHelper.ExecuteQueryForDataTable("GetTINVertex", ht);

            IDataset dataset = (IDataset)pFeatureLayer.FeatureClass;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            IFeatureCursor pFeatureCursor = pFeatureClass.Insert(true);
            IFeatureBuffer pFeatureBuffer;

            float x, y, z;
            List<IPoint> pts = new List<IPoint>();

            //循环添加
            foreach (DataRow dataRow in gridTable.Rows)
            {
                if (!(float.TryParse(dataRow["VertexX"].ToString(), out x) && float.TryParse(dataRow["VertexY"].ToString(), out y) && float.TryParse(dataRow["VertexHeight"].ToString(), out z)))
                    continue;

                IPoint pointA = GeometryUtilities.ConstructPoint3D(x, y, z);
                pts.Add(pointA);

                if (pts.Count >= 3)
                {
                    IGeometryCollection pGeometryColl = GeometryUtilities.ConstructMultiPath(pts);
                    GeometryUtilities.MakeZAware(pGeometryColl as IGeometry);
                    pFeatureBuffer = pFeatureClass.CreateFeatureBuffer();
                    pFeatureBuffer.Shape = pGeometryColl as IGeometry;
                    pFeatureCursor.InsertFeature(pFeatureBuffer);

                    pts.Clear();
                }
            }

            //一次性提交
            pFeatureCursor.Flush();

            //stop editing   
            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);

            GISMapApplication.Instance.RefreshLayer(pFeatureLayer);
        }

        /// <summary>
        /// 构造 TIN 面
        /// </summary>
        public void constuctTIN1()
        {
            double minX = 0, minY = 0, maxX = 0, maxY = 0;
            InternalInterference.Grid.GridHelper.getInstance().getMinXY(ref minX, ref minY);
            InternalInterference.Grid.GridHelper.getInstance().getMaxXY(ref maxX, ref maxY);

            Hashtable ht = new Hashtable();
            ht["minX"] = minX;
            ht["maxX"] = maxX;
            ht["minY"] = minY;
            ht["maxY"] = maxY;
            DataTable gridTable = IbatisHelper.ExecuteQueryForDataTable("GetTINVertex", ht);

            IDataset dataset = (IDataset)pFeatureLayer.FeatureClass;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            IFeatureCursor pFeatureCursor = pFeatureClass.Insert(true);
            IFeatureBuffer pFeatureBuffer;

            float x, y, z;
            List<IPoint> pts = new List<IPoint>();

            //循环添加
            foreach (DataRow dataRow in gridTable.Rows)
            {
                if (!(float.TryParse(dataRow["VertexX"].ToString(), out x) && float.TryParse(dataRow["VertexY"].ToString(), out y) && float.TryParse(dataRow["VertexHeight"].ToString(), out z)))
                    continue;

                IPoint pointA = GeometryUtilities.ConstructPoint3D(x, y, z);
                pts.Add(pointA);

                if (pts.Count >= 3)
                {
                    IGeometryCollection pGeometryColl = GeometryUtilities.ConstructPolygon(new IPoint[] { pts[0], pts[1], pts[2] });
                    GeometryUtilities.MakeZAware(pGeometryColl as IGeometry);
                    pFeatureBuffer = pFeatureClass.CreateFeatureBuffer();
                    pFeatureBuffer.Shape = pGeometryColl as IGeometry;
                    pFeatureCursor.InsertFeature(pFeatureBuffer);

                    pts.Clear();
                }
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
