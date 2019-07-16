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
    /// <summary>
    /// 覆盖网格，gxid,gyid,enodeb,ci,cellname
    /// </summary>
    public class OperateDTLayer
    {
        private IFeatureLayer pFeatureLayer;
        private IFeatureClass pFeatureClass;
        private int RecePowerIndex;
        private int PathLossIndex;
        private int GXIDIndex;
        private int GYIDIndex;
        private int cellNameIndex;
        private int eNodeBIndex;
        private int CIIndex;

        // 列名
        public OperateDTLayer()
        {
            IFeatureWorkspace featureWorkspace = MapWorkSpace.getWorkSpace();
            IFeatureClass fclass = featureWorkspace.OpenFeatureClass(LayerNames.TDDriverTest);
            IFeatureLayer flayer = new FeatureLayer();
            pFeatureLayer.FeatureClass = pFeatureClass;

            //pFeatureLayer = GISMapApplication.Instance.GetLayer(LayerNames.TDDriverTest) as IFeatureLayer;
            //pFeatureClass = pFeatureLayer.FeatureClass;
            this.RecePowerIndex = pFeatureClass.FindField("RecePower");
            this.PathLossIndex = pFeatureClass.FindField("PathLoss");
            this.GXIDIndex = pFeatureClass.FindField("GXID");
            this.GYIDIndex = pFeatureClass.FindField("GYID");
            this.cellNameIndex = pFeatureClass.FindField("CellName");
            this.eNodeBIndex = pFeatureClass.FindField("eNodeB");
            this.CIIndex = pFeatureClass.FindField("CI");
        }


        /// <summary>
        /// 删除图层所有要素
        /// </summary>
        public void ClearLayer()
        {
            FeatureUtilities.DeleteFeatureLayerFeatrues(this.pFeatureLayer);
        }

        /// <summary>
        /// 构造路测网格
        /// </summary>
        /// <param name="cellname"></param>
        /// <param name="enodeb"></param>
        /// <param name="ci"></param>
        public bool constuctDTGrids()
        {
            DataTable gridTable = IbatisHelper.ExecuteQueryForDataTable("getDTdisplay", null);
            //DataTable gridTable = IbatisHelper.ExecuteQueryForDataTable("getDT", null); 
            if (gridTable.Rows.Count < 1)
                return false;

            IDataset dataset = (IDataset)pFeatureLayer.FeatureClass;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            IFeatureCursor pFeatureCursor = pFeatureClass.Insert(true);
            IFeatureBuffer pFeatureBuffer;

            int gxid, gyid;
            float x1, y1, x2, y2;
            float recePower;
            //循环添加
            foreach (DataRow dataRow in gridTable.Rows)
            {
                gxid = int.Parse(dataRow["gxid"].ToString());
                gyid = int.Parse(dataRow["gyid"].ToString());

                if (!(float.TryParse(dataRow["MinX"].ToString(), out x1) && float.TryParse(dataRow["MinY"].ToString(), out y1) && float.TryParse(dataRow["MaxX"].ToString(), out x2) && float.TryParse(dataRow["MaxY"].ToString(), out y2) && float.TryParse(dataRow["RecePower"].ToString(), out recePower)))
                    continue;

                IPoint pointA = GeometryUtilities.ConstructPoint2D(x1, y1);
                IPoint pointB = GeometryUtilities.ConstructPoint2D(x2, y1);
                IPoint pointC = GeometryUtilities.ConstructPoint2D(x2, y2);
                IPoint pointD = GeometryUtilities.ConstructPoint2D(x1, y2);

                IGeometryCollection pGeometryColl = GeometryUtilities.ConstructPolygon(new IPoint[] { pointA, pointB, pointC, pointD });

                pFeatureBuffer = pFeatureClass.CreateFeatureBuffer();
                pFeatureBuffer.Shape = pGeometryColl as IGeometry;
                pFeatureBuffer.set_Value(this.GXIDIndex, gxid);
                pFeatureBuffer.set_Value(this.GYIDIndex, gyid);
                pFeatureBuffer.set_Value(this.eNodeBIndex, 0);
                pFeatureBuffer.set_Value(this.CIIndex, 0);
                pFeatureBuffer.set_Value(this.cellNameIndex, "");
                if (recePower > -41)
                    pFeatureBuffer.set_Value(this.RecePowerIndex, -41);
                else if (recePower < -110)
                    pFeatureBuffer.set_Value(this.RecePowerIndex, -110);
                else
                    pFeatureBuffer.set_Value(this.RecePowerIndex, recePower);
                pFeatureBuffer.set_Value(this.PathLossIndex, 0);
                pFeatureCursor.InsertFeature(pFeatureBuffer);
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
