using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;


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
    public class OperateCoverGirdLayer
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
        public OperateCoverGirdLayer(string layerName)
        {
            IFeatureWorkspace featureWorkspace = MapWorkSpace.getWorkSpace();
            pFeatureClass = featureWorkspace.OpenFeatureClass(layerName);
            pFeatureLayer = new FeatureLayer();
            pFeatureLayer.FeatureClass = pFeatureClass;
            //int n = pFeatureClass.FeatureCount(new QueryFilterClass());

            //pFeatureLayer = GISMapApplication.Instance.GetLayer(LayerNames.CoverGrids) as IFeatureLayer;
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
        /// 构造小区的一个网格
        /// </summary>
        /// <param name="cellName"></param>
        /// <param name="gxid"></param>
        /// <param name="gyid"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="recePower"></param>
        /// <param name="pathLoss"></param>
        public void constructGrid(string cellName, int enodeb, int ci, int gxid, int gyid, double x1, double y1, double x2, double y2, double recePower, double pathLoss)
        {
            IDataset dataset = (IDataset)pFeatureLayer.FeatureClass;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            IFeatureCursor pFeatureCursor = pFeatureClass.Insert(true);
            IFeatureBuffer pFeatureBuffer;

            IPoint pointA = GeometryUtilities.ConstructPoint2D(x1, y1);
            IPoint pointB = GeometryUtilities.ConstructPoint2D(x2, y1);
            IPoint pointC = GeometryUtilities.ConstructPoint2D(x2, y2);
            IPoint pointD = GeometryUtilities.ConstructPoint2D(x1, y2);

            IGeometryCollection pGeometryColl = GeometryUtilities.ConstructPolygon(new IPoint[] { pointA, pointB, pointC, pointD });

            pFeatureBuffer = pFeatureClass.CreateFeatureBuffer();
            pFeatureBuffer.Shape = pGeometryColl as IGeometry;
            pFeatureBuffer.set_Value(this.GXIDIndex, gxid);
            pFeatureBuffer.set_Value(this.GYIDIndex, gyid);
            pFeatureBuffer.set_Value(this.eNodeBIndex, enodeb);
            pFeatureBuffer.set_Value(this.CIIndex, ci);
            pFeatureBuffer.set_Value(this.cellNameIndex, cellName);
            pFeatureBuffer.set_Value(this.RecePowerIndex, recePower);
            pFeatureBuffer.set_Value(this.PathLossIndex, pathLoss);
            pFeatureCursor.InsertFeature(pFeatureBuffer);

            //一次性提交
            pFeatureCursor.Flush();

            //stop editing   
            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);

            IFeatureClassManage pFeatureClassManage = (IFeatureClassManage)pFeatureClass;
            pFeatureClassManage.UpdateExtent();

            //GISMapApplication.Instance.RefreshLayer(pFeatureLayer);
            //GISMapApplication.Instance.FullExtent(pFeatureLayer.AreaOfInterest);
        }

        /// <summary>
        /// 构造小区覆盖网格
        /// </summary>
        /// <param name="cellname"></param>
        /// <param name="enodeb"></param>
        /// <param name="ci"></param>
        public bool constuctCellGrids(string cellname, int enodeb, int ci)
        {
            DataTable gridTable = new DataTable();
            Hashtable ht = new Hashtable();
            ht["eNodeB"] = enodeb;
            ht["CI"] = ci;
            gridTable = IbatisHelper.ExecuteQueryForDataTable("GetSpecifiedCellGrids", ht);
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
            float recePower, pathLoss;
            //循环添加
            foreach (DataRow dataRow in gridTable.Rows)
            {
                gxid = int.Parse(dataRow["Gxid"].ToString());
                gyid = int.Parse(dataRow["Gyid"].ToString());

                if (!(float.TryParse(dataRow["MinX"].ToString(), out x1) && float.TryParse(dataRow["MinY"].ToString(), out y1) && float.TryParse(dataRow["MaxX"].ToString(), out x2) && float.TryParse(dataRow["MaxY"].ToString(), out y2) && float.TryParse(dataRow["ReceivedPowerdbm"].ToString(), out recePower) && float.TryParse(dataRow["PathLoss"].ToString(), out pathLoss)))
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
                pFeatureBuffer.set_Value(this.eNodeBIndex, enodeb);
                pFeatureBuffer.set_Value(this.CIIndex, ci);
                pFeatureBuffer.set_Value(this.cellNameIndex, cellname);
                if (recePower > -41)
                    pFeatureBuffer.set_Value(this.RecePowerIndex, -41);
                //else if(recePower < -110)
                //    pFeatureBuffer.set_Value(this.RecePowerIndex, -110);
                else
                    pFeatureBuffer.set_Value(this.RecePowerIndex, recePower);
                pFeatureBuffer.set_Value(this.PathLossIndex, pathLoss);
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

        /// <summary>
        /// 区域覆盖
        /// </summary>
        /// <param name="mingxid"></param>
        /// <param name="mingyid"></param>
        /// <param name="maxgxid"></param>
        /// <param name="maxgyid"></param>
        public bool constuctAreaGrids(int mingxid, int mingyid, int maxgxid, int maxgyid)
        {
            DataTable gridTable = new DataTable();
            Hashtable ht = new Hashtable();
            ht["MinGXID"] = mingxid;
            ht["MaxGXID"] = maxgxid;
            ht["MinGYID"] = mingyid;
            ht["MaxGYID"] = maxgyid;
            gridTable = IbatisHelper.ExecuteQueryForDataTable("GetSpecifiedAreaGrids", ht);
            if (gridTable.Rows.Count < 1)
                return false;
            /*
             *<select id="GetSpecifiedAreaGrids" parameterClass="Hashtable">
			select a.GXID, a.GYID, a.eNodeB, a.CI, d.MinLong, d.MinLat, d.MaxLong, d.MaxLat, a.ReceivedPowerdbm, a.PathLoss from tbGridPathloss a ,
			(select c.GXID, c.GYID, c.eNodeB, c.CI, max(c.ReceivedPowerdbm) ReceivedPowerdbm from tbGridPathloss c where c.GXID between '$MinGXID$' and '$MaxGXID$' and c.GYID between '$MinGYID$' and '$MaxGYID$'  group by c.gxid, c.gyid, c.eNodeB, c.ci having max(c.ReceivedPowerdbm) > -130 ) b,
			tbGridDem d
			where a.gxid = b.gxid and a.gyid = b.gyid and a.eNodeB = b.eNodeB and a.ci = b.ci and a.gxid = d.gxid and a.gyid = d.gyid
		    </select> 
             */
            IFeatureWorkspace featureWorkspace = MapWorkSpace.getWorkSpace();
            IFeatureClass fclass = featureWorkspace.OpenFeatureClass(LayerNames.AreaCoverGrids);
            IFeatureLayer flayer = new FeatureLayer();
            pFeatureLayer.FeatureClass = pFeatureClass;

            //IFeatureLayer flayer = GISMapApplication.Instance.GetLayer(LayerNames.AreaCoverGrids) as IFeatureLayer;
            FeatureUtilities.DeleteFeatureLayerFeatrues(flayer);

            //IFeatureClass fclass = flayer.FeatureClass;

            IDataset dataset = (IDataset)fclass;
            IWorkspace workspace = dataset.Workspace;
            //Cast for an IWorkspaceEdit   
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
            //start an edit session and operation               
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            IFeatureCursor pFeatureCursor = fclass.Insert(true);
            IFeatureBuffer pFeatureBuffer;

            int gxid, gyid, lac, ci;
            float x1, y1, x2, y2;
            float recePower, pathLoss;
            //循环添加
            foreach (DataRow dataRow in gridTable.Rows)
            {
                gxid = int.Parse(dataRow["GXID"].ToString());
                gyid = int.Parse(dataRow["GYID"].ToString());
                //lac = int.Parse(dataRow["eNodeB"].ToString());
                //ci = int.Parse(dataRow["CI"].ToString());
                recePower = float.Parse(dataRow["ReceivedPowerdbm"].ToString());
                pathLoss = float.Parse(dataRow["PathLoss"].ToString());

                if (!(float.TryParse(dataRow["MinX"].ToString(), out x1)
                    && float.TryParse(dataRow["MinY"].ToString(), out y1)
                    && float.TryParse(dataRow["MaxX"].ToString(), out x2)
                    && float.TryParse(dataRow["MaxY"].ToString(), out y2)))
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
                //else if(recePower < -110)
                //    pFeatureBuffer.set_Value(this.RecePowerIndex, -110);
                else
                    pFeatureBuffer.set_Value(this.RecePowerIndex, recePower);
                pFeatureBuffer.set_Value(this.PathLossIndex, pathLoss);
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



        //public void SyncGridStrengthPwr()
        //{
        //    DataTable gridTable = new DataTable();
        //    int gxid,gyid;
        //    float recePower,pathLoss;
        //    gridTable = IbatisHelper.ExecuteQueryForDataTable("GetGridStrengthPwToSync", null);
        //    IDataset dataset = (IDataset)this.pFeatureClass;
        //    IWorkspace workspace = dataset.Workspace;
        //    //Cast for an IWorkspaceEdit   
        //    IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
        //    //start an edit session and operation               
        //    workspaceEdit.StartEditing(true);
        //    workspaceEdit.StartEditOperation();

        //    IFeatureCursor pFeatureCursor;
        //    IFeature pFeature;
        //    IQueryFilter pQueryFilter=new QueryFilterClass();
        //    //ISpatialFilter pSpatialFilter = new SpatialFilterClass();
        //    pQueryFilter.SubFields="RecePower,PathLoss";
        //    foreach (DataRow row in gridTable.Rows)
        //    {
        //        if (!(int.TryParse(row[0].ToString(), out gxid) && int.TryParse(row[1].ToString(),out gyid) && float.TryParse(row[2].ToString(),out recePower) && float.TryParse(row[3].ToString(),out pathLoss)))
        //            continue;
        //        pQueryFilter.WhereClause = "\"GXID\"=" + gxid + " and \"GYID\"=" + gyid;
        //        //pSpatialFilter.WhereClause = "\"GXID\"=" + gxid + " and \"GYID\"=" + gyid;

        //        pFeatureCursor = pFeatureClass.Search(pQueryFilter, true);
        //        pFeature = pFeatureCursor.NextFeature();
        //        if (pFeature == null)
        //            continue;
        //        pFeature.set_Value(this.RecePowerIndex, recePower);
        //        pFeature.set_Value(this.PathLossIndex, pathLoss);
        //        pFeature.Store();
        //    }
        //    //stop editing   
        //    workspaceEdit.StopEditOperation();
        //    workspaceEdit.StopEditing(true);

        //}

        ///// <summary>
        ///// 将图层所有要素功率值置零
        ///// </summary>
        //public void ResetGridStrengthPwr()
        //{
        //    IDataset dataset = (IDataset)this.pFeatureClass;
        //    IWorkspace workspace = dataset.Workspace;
        //    //Cast for an IWorkspaceEdit   
        //    IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
        //    //start an edit session and operation               
        //    workspaceEdit.StartEditing(true);
        //    workspaceEdit.StartEditOperation();

        //    IFeatureCursor pFeatureCursor = pFeatureClass.Search(null, true);
        //    IFeature pFeature = pFeatureCursor.NextFeature();
        //    while (pFeature != null)
        //    {
        //        pFeature.set_Value(this.RecePowerIndex, 0);
        //        pFeature.set_Value(this.PathLossIndex, 0);
        //        pFeature = pFeatureCursor.NextFeature();
        //    }

        //    //stop editing   
        //    workspaceEdit.StopEditOperation();
        //    workspaceEdit.StopEditing(true);
        //}

        ///// <summary>
        ///// 删除图层所有要素
        ///// </summary>
        //public void ClearGridStrengthPwr()
        //{
        //    IDataset dataset = (IDataset)this.pFeatureClass;
        //    IWorkspace workspace = dataset.Workspace;
        //    //Cast for an IWorkspaceEdit   
        //    IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
        //    //start an edit session and operation               
        //    workspaceEdit.StartEditing(true);
        //    workspaceEdit.StartEditOperation();

        //    IFeatureCursor pFeatureCursor = pFeatureClass.Search(null, true);
        //    IFeature pFeature = pFeatureCursor.NextFeature();
        //    while (pFeature != null)
        //    {
        //        pFeature.Delete();
        //        pFeature = pFeatureCursor.NextFeature();
        //    }

        //    //stop editing   
        //    workspaceEdit.StopEditOperation();
        //    workspaceEdit.StopEditing(true);
        //}
    }
}
