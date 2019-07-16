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
    /// 覆盖网格，gxid,gyid,lac,ci,cellname
    /// </summary>
    public class OperateCoverGird3DLayer
    {
        private IFeatureLayer pFeatureLayer;
        private IFeatureClass pFeatureClass;
        private int RecePowerIndex;
        private int PathLossIndex;
        private int GXIDIndex;
        private int GYIDIndex;
        private int LevelIndex;
        private int cellNameIndex;
        private int eNodeBIndex;
        private int CIIndex;

        public OperateCoverGird3DLayer(string layerName)
        {
            IFeatureWorkspace featureWorkspace = MapWorkSpace.getWorkSpace();
            pFeatureClass = featureWorkspace.OpenFeatureClass(layerName);
            pFeatureLayer = new FeatureLayer();
            pFeatureLayer.FeatureClass = pFeatureClass;

            //pFeatureLayer = GISMapApplication.Instance.GetLayer(LayerNames.CoverGrid3Ds) as IFeatureLayer;
            //pFeatureClass = pFeatureLayer.FeatureClass;
            this.RecePowerIndex = pFeatureClass.FindField("RecePower");
            this.PathLossIndex = pFeatureClass.FindField("PathLoss");
            this.GXIDIndex = pFeatureClass.FindField("GXID");
            this.GYIDIndex = pFeatureClass.FindField("GYID");
            this.LevelIndex = pFeatureClass.FindField("Level");
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
        /// 构造小区的一个立体覆盖网格
        /// </summary>
        /// <param name="cellName"></param>
        /// <param name="lac"></param>
        /// <param name="ci"></param>
        /// <param name="gxid"></param>
        /// <param name="gyid"></param>
        /// <param name="level"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="z"></param>
        /// <param name="recePower"></param>
        /// <param name="pathLoss"></param>
        public void constructGrid3D(string cellName, int lac, int ci, int gxid, int gyid, int level, double x1, double y1, double x2, double y2, double z, double recePower, double pathLoss)
        {
            IDataset dataset = (IDataset)pFeatureLayer.FeatureClass;
            IWorkspace workspace = dataset.Workspace;
            //Cast for an IWorkspaceEdit   
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
            //start an edit session and operation               
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            IFeatureCursor pFeatureCursor = pFeatureClass.Insert(true);
            IFeatureBuffer pFeatureBuffer;

            IPoint pointA = GeometryUtilities.ConstructPoint3D(x1, y1, z);
            IPoint pointB = GeometryUtilities.ConstructPoint3D(x2, y1, z);
            IPoint pointC = GeometryUtilities.ConstructPoint3D(x2, y2, z);
            IPoint pointD = GeometryUtilities.ConstructPoint3D(x1, y2, z);

            IGeometryCollection pGeometryColl = GeometryUtilities.ConstructPolygon(new IPoint[] { pointA, pointB, pointC, pointD });

            pFeatureBuffer = pFeatureClass.CreateFeatureBuffer();
            pFeatureBuffer.Shape = pGeometryColl as IGeometry;
            pFeatureBuffer.set_Value(this.GXIDIndex, gxid);
            pFeatureBuffer.set_Value(this.GYIDIndex, gyid);
            pFeatureBuffer.set_Value(this.LevelIndex, level);
            pFeatureBuffer.set_Value(this.eNodeBIndex, lac);
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

            //IFeatureClassManage pFeatureClassManage = (IFeatureClassManage)pFeatureClass;
            //pFeatureClassManage.UpdateExtent();

            GISMapApplication.Instance.RefreshLayer(pFeatureLayer);
            //GISMapApplication.Instance.FullExtent(pFeatureLayer.AreaOfInterest);
        }

        /// <summary>
        /// 构造小区立体覆盖网格
        /// </summary>
        /// <param name="cellname"></param>
        /// <param name="lac"></param>
        /// <param name="ci"></param>
        public bool constuctCellGrid3Ds(string cellname, int eNodeB, int ci)
        {
            DataTable gridTable = new DataTable();
            Hashtable ht = new Hashtable();
            ht["eNodeB"] = eNodeB;
            ht["CI"] = ci;
            gridTable = IbatisHelper.ExecuteQueryForDataTable("GetSpecifiedCellGrid3Ds", ht);
            //gridTable = IbatisHelper.ExecuteQueryForDataTable("GetAreaGrid3Ds", null);
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
            double recePower, pathLoss;
            double gbaseheight = GridHelper.getInstance().getGBaseHeight();
            double gheight = GridHelper.getInstance().getGHeight();
            //循环添加
            foreach (DataRow dataRow in gridTable.Rows)
            {
                gxid = int.Parse(dataRow["Gxid"].ToString());
                gyid = int.Parse(dataRow["Gyid"].ToString());
                level = int.Parse(dataRow["level"].ToString());

                //if (!(double.TryParse(dataRow["CX"].ToString(), out x1) && double.TryParse(dataRow["CY"].ToString(), out y1)))
                //    continue;
                if (!(double.TryParse(dataRow["MinX"].ToString(), out x1) && double.TryParse(dataRow["MinY"].ToString(), out y1)))
                    continue;
                if (!(double.TryParse(dataRow["MaxX"].ToString(), out x2) && double.TryParse(dataRow["MaxY"].ToString(), out y2)))
                    continue;
                if (!(double.TryParse(dataRow["ReceivedPowerdbm"].ToString(), out recePower) && double.TryParse(dataRow["PathLoss"].ToString(), out pathLoss)))
                    continue;
                z = gheight * level;

                IPoint pointA = GeometryUtilities.ConstructPoint3D(x1, y1, z);
                IPoint pointB = GeometryUtilities.ConstructPoint3D(x2, y1, z);
                IPoint pointC = GeometryUtilities.ConstructPoint3D(x2, y2, z);
                IPoint pointD = GeometryUtilities.ConstructPoint3D(x1, y2, z);

                //IPoint pointA = GeometryUtilities.ConstructPoint3D(x1 - 2.5, y1 - 2.5, z - 1.5);
                //IPoint pointB = GeometryUtilities.ConstructPoint3D(x1 + 2.5, y1 - 2.5, z - 1.5);
                //IPoint pointC = GeometryUtilities.ConstructPoint3D(x1 + 2.5, y1 + 2.5, z + 1.5);
                //IPoint pointD = GeometryUtilities.ConstructPoint3D(x1 - 2.5, y1 + 2.5, z + 1.5);

                IGeometryCollection pGeometryColl = GeometryUtilities.ConstructPolygon(new IPoint[] { pointA, pointB, pointC, pointD });
                GeometryUtilities.MakeZAware(pGeometryColl as IGeometry);


                pFeatureBuffer = pFeatureClass.CreateFeatureBuffer();
                pFeatureBuffer.Shape = pGeometryColl as IGeometry;
                pFeatureBuffer.set_Value(this.GXIDIndex, gxid);
                pFeatureBuffer.set_Value(this.GYIDIndex, gyid);
                pFeatureBuffer.set_Value(this.LevelIndex, level);
                pFeatureBuffer.set_Value(this.eNodeBIndex, eNodeB);
                pFeatureBuffer.set_Value(this.CIIndex, ci);
                pFeatureBuffer.set_Value(this.cellNameIndex, cellname);
                //if(recePower > 

                if (recePower > -41)
                    pFeatureBuffer.set_Value(this.RecePowerIndex, -41);
                //else if (recePower <= -108 && recePower > -111)
                //    pFeatureBuffer.set_Value(this.RecePowerIndex, -72);
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
        /// 构造小区立体顶面覆盖网格
        /// </summary>
        /// <param name="cellname"></param>
        /// <param name="lac"></param>
        /// <param name="ci"></param>
        public void constuctCellGrid3Dtops(string cellname, int eNodeB, int ci)
        {
            DataTable gridTable = new DataTable();
            Hashtable ht = new Hashtable();
            ht["eNodeB"] = eNodeB;
            ht["CI"] = ci;
            gridTable = IbatisHelper.ExecuteQueryForDataTable("GetSpecifiedCellGrid3Ds", ht);
            //gridTable = IbatisHelper.ExecuteQueryForDataTable("GetAreaGrid3Ds", null);

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
            double recePower, pathLoss;
            double gbaseheight = GridHelper.getInstance().getGBaseHeight();
            double gheight = GridHelper.getInstance().getGHeight();
            //循环添加
            foreach (DataRow dataRow in gridTable.Rows)
            {
                gxid = int.Parse(dataRow["Gxid"].ToString());
                gyid = int.Parse(dataRow["Gyid"].ToString());
                level = int.Parse(dataRow["level"].ToString());

                if (!(double.TryParse(dataRow["MinX"].ToString(), out x1) && double.TryParse(dataRow["MinY"].ToString(), out y1) && double.TryParse(dataRow["MaxX"].ToString(), out x2) && double.TryParse(dataRow["MaxY"].ToString(), out y2) && double.TryParse(dataRow["ReceivedPowerdbm"].ToString(), out recePower) && double.TryParse(dataRow["PathLoss"].ToString(), out pathLoss)))
                    continue;
                z = gheight * (level - 1) + gbaseheight;

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
                pFeatureBuffer.set_Value(this.eNodeBIndex, eNodeB);
                pFeatureBuffer.set_Value(this.CIIndex, ci);
                pFeatureBuffer.set_Value(this.cellNameIndex, cellname);
                if (recePower > -41)
                    pFeatureBuffer.set_Value(this.RecePowerIndex, -41);
                //else if (recePower < -110)
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

            //IFeatureClassManage pFeatureClassManage = (IFeatureClassManage)pFeatureClass;
            //pFeatureClassManage.UpdateExtent();

            GISMapApplication.Instance.RefreshLayer(pFeatureLayer);
            //GISMapApplication.Instance.FullExtent(pFeatureLayer.AreaOfInterest);
        }

        /// <summary>
        /// 区域立体覆盖
        /// </summary>
        /// <param name="mingxid"></param>
        /// <param name="mingyid"></param>
        /// <param name="maxgxid"></param>
        /// <param name="maxgyid"></param>
        public bool constuctAreaGrid3Ds(int mingxid, int mingyid, int maxgxid, int maxgyid)
        {
            DataTable gridTable = new DataTable();
            Hashtable ht = new Hashtable();
            ht["MinGXID"] = mingxid;
            ht["MaxGXID"] = maxgxid;
            ht["MinGYID"] = mingyid;
            ht["MaxGYID"] = maxgyid;
            gridTable = IbatisHelper.ExecuteQueryForDataTable("GetSpecifiedAreaGrid3Ds", ht);
            if (gridTable.Rows.Count < 1)
                return false;

            IFeatureWorkspace featureWorkspace = MapWorkSpace.getWorkSpace();
            IFeatureClass fclass = featureWorkspace.OpenFeatureClass(LayerNames.AreaCoverGrid3Ds);
            IFeatureLayer flayer = new FeatureLayer();
            pFeatureLayer.FeatureClass = pFeatureClass;

            //IFeatureLayer flayer = GISMapApplication.Instance.GetLayer(LayerNames.AreaCoverGrid3Ds) as IFeatureLayer;
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

            int lac, ci, gxid, gyid, level;
            double x1, y1, x2, y2, z;
            double recePower, pathLoss;
            double gbaseheight = GridHelper.getInstance().getGBaseHeight();
            double gheight = GridHelper.getInstance().getGHeight();
            //循环添加
            foreach (DataRow dataRow in gridTable.Rows)
            {
                gxid = int.Parse(dataRow["GXID"].ToString());
                gyid = int.Parse(dataRow["GYID"].ToString());
                level = int.Parse(dataRow["Level"].ToString());
                //lac = int.Parse(dataRow["eNodeB"].ToString());
                //ci = int.Parse(dataRow["CI"].ToString());
                recePower = double.Parse(dataRow["ReceivedPowerdbm"].ToString());
                pathLoss = double.Parse(dataRow["PathLoss"].ToString());

                if (!(double.TryParse(dataRow["MinX"].ToString(), out x1)
                    && double.TryParse(dataRow["MinY"].ToString(), out y1)
                    && double.TryParse(dataRow["MaxX"].ToString(), out x2)
                    && double.TryParse(dataRow["MaxY"].ToString(), out y2)))
                    continue;

                z = gheight * (level - 1) + gbaseheight;

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
                pFeatureBuffer.set_Value(this.eNodeBIndex, 0);
                pFeatureBuffer.set_Value(this.CIIndex, 0);
                pFeatureBuffer.set_Value(this.cellNameIndex, "");
                if (recePower > -41)
                    pFeatureBuffer.set_Value(this.RecePowerIndex, -41);
                //else if(recePower < -100)
                //    pFeatureBuffer.set_Value(this.RecePowerIndex, -100);
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

    }
}
