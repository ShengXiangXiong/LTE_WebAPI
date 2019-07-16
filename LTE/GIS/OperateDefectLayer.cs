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
    public class OperateDefectLayer
    {
        private IFeatureLayer pFeatureLayer;
        private IFeatureClass pFeatureClass;
        private int GXIDIndex;
        private int GYIDIndex;
        private int LevelIndex;
        private int RecePowerIndex;

        public OperateDefectLayer(string name)
        {
            IFeatureWorkspace featureWorkspace = MapWorkSpace.getWorkSpace();
            IFeatureClass fclass = featureWorkspace.OpenFeatureClass(name);
            IFeatureLayer flayer = new FeatureLayer();
            pFeatureLayer.FeatureClass = pFeatureClass;

            //pFeatureLayer = GISMapApplication.Instance.GetLayer(name) as IFeatureLayer;
            //pFeatureClass = pFeatureLayer.FeatureClass;
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

        public bool constuctGrid3Ds(int mingxid, int maxgxid, int mingyid, int maxgyid, short type)
        {
            DataTable gridTable = new DataTable();
            Hashtable para = new Hashtable();
            para["minGXID"] = mingxid;
            para["maxGXID"] = maxgxid;
            para["minGYID"] = mingyid;
            para["maxGYID"] = maxgyid;
            para["type"] = type;

            gridTable = IbatisHelper.ExecuteQueryForDataTable("getDefect", para);
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

            int gxid, gyid, level;
            double x1, y1, x2, y2, z;
            double recePower;
            double gbaseheight = GridHelper.getInstance().getGBaseHeight();
            double gheight = GridHelper.getInstance().getGHeight();
            //循环添加
            foreach (DataRow dataRow in gridTable.Rows)
            {
                gxid = int.Parse(dataRow["GXID"].ToString());
                gyid = int.Parse(dataRow["GYID"].ToString());
                level = int.Parse(dataRow["GZID"].ToString());

                if (!(double.TryParse(dataRow["MinX"].ToString(), out x1) && double.TryParse(dataRow["MinY"].ToString(), out y1)))
                    continue;
                if (!(double.TryParse(dataRow["MaxX"].ToString(), out x2) && double.TryParse(dataRow["MaxY"].ToString(), out y2)))
                    continue;
                if (!(double.TryParse(dataRow["ReceivedPowerdbm"].ToString(), out recePower)))
                    continue;
                z = gheight * level;

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
                pFeatureBuffer.set_Value(this.LevelIndex, level);

                if (recePower > -41)
                    pFeatureBuffer.set_Value(this.RecePowerIndex, -41);
                else
                    pFeatureBuffer.set_Value(this.RecePowerIndex, recePower);
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
