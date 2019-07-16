using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Analyst3D;

using LTE.Model;
using LTE.DB;

using System.IO;

namespace LTE.GIS
{
    /// <summary>
    /// 刷新图层
    /// </summary>
    public class OperateCellLayer
    {
        /// <summary>
        /// 刷新GSM图层(包含900,1800及基站图层)
        /// </summary>
        public bool RefreshCellLayer()
        {
            //RefreshGSM900BTS();
            return RefreshGSM900Cell();
            //RefreshGSM1800BTS();
            //RefreshGSM1800Cell();\

        }

        /// <summary>
        /// 刷新GSM900小区图层
        /// </summary>
        public bool RefreshGSM900Cell()
        {

            IList<CELL> GSM900CellData = IbatisHelper.ExecuteQueryForList<CELL>("GetGSM900CellLayerData", null);
            if (GSM900CellData.Count < 1)
                return false;

            IFeatureWorkspace featureWorkspace = MapWorkSpace.getWorkSpace();
            IFeatureClass pFeatureClass = featureWorkspace.OpenFeatureClass(LayerNames.GSM900Cell);
            IFeatureLayer pFeatureLayer = new FeatureLayer();
            pFeatureLayer.FeatureClass = pFeatureClass;

            //IFeatureLayer pFeatureLayer = GISMapApplication.Instance.GetLayer(LayerNames.GSM900Cell) as IFeatureLayer;

            FeatureUtilities.DeleteFeatureLayerFeatrues(pFeatureLayer);

            //IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;

            #region 测试
            //设置字段
            //IField pField = new FieldClass();
            //IFieldEdit pFieldEdit = (IFieldEdit)pField;

            //添加新的zhcellname
            //pField = new FieldClass();
            //pFieldEdit = (IFieldEdit)pField;
            //pFieldEdit.Name_2 = System.DateTime.Now.Month.ToString()+System.DateTime.Now.Millisecond.ToString();
            //pFeatureClass.AddField(pField);

            //清除旧的field(从第三个开始删除,前面两个是必须字段，不能删除,第三个删除不了,下面作更新第三个)
            //while (pFeatureClass.Fields.FieldCount > 3)
            //{
            //    string fieldName = pFeatureClass.Fields.get_Field(2).Name;

            // FeatureUtilities.DeletField(pFeatureClass, fieldName);
            //}

            ////增加新的field

            ////因最后一个删除不了，这里作修改,cell_name
            //pField = new FieldClass();
            //pFieldEdit = (IFieldEdit)pField;
            //pFieldEdit.Name_2 = "cell_name";
            //pFeatureClass.AddField(pField);




            ////添加新的zhcellname
            //pField = new FieldClass();
            //pFieldEdit = (IFieldEdit)pField;
            //pFieldEdit.Name_2 = "zhcellname";
            //pFeatureClass.AddField(pField);

            ////添加新的longitude
            //pField = new FieldClass();
            //pFieldEdit = (IFieldEdit)pField;
            //pFieldEdit.Name_2 = "longitude";
            //pFeatureClass.AddField(pField);

            ////添加新的latitude
            //pField = new FieldClass();
            //pFieldEdit = (IFieldEdit)pField;
            //pFieldEdit.Name_2 = "latitude";
            //pFeatureClass.AddField(pField);

            ////添加新的height
            //pField = new FieldClass();
            //pFieldEdit = (IFieldEdit)pField;
            //pFieldEdit.Name_2 = "height";
            //pFeatureClass.AddField(pField);

            ////添加新的tilt
            //pField = new FieldClass();
            //pFieldEdit = (IFieldEdit)pField;
            //pFieldEdit.Name_2 = "tilt";
            //pFeatureClass.AddField(pField);
            #endregion 测试

            IDataset dataset = (IDataset)pFeatureClass;
            IWorkspace workspace = dataset.Workspace;
            //Cast for an IWorkspaceEdit   
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
            //start an edit session and operation               
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();


            IFeatureCursor pFeatureCursor = pFeatureClass.Insert(true);
            IFeatureBuffer pFeatureBuffer;


            //循环添加
            double circRadius = 60;//小区900生成的半径大小
            double every = 1;


            foreach (CELL gsm900Cell in GSM900CellData)
            {
                if ((!gsm900Cell.x.HasValue) || (!gsm900Cell.y.HasValue) || (!gsm900Cell.AntHeight.HasValue) || (!gsm900Cell.Azimuth.HasValue) || (!gsm900Cell.Tilt.HasValue))
                {
                    continue;
                }
                int lac = gsm900Cell.eNodeB;
                int ci = gsm900Cell.CI;
                string cellName = gsm900Cell.CellName;
                string cellNameChs = gsm900Cell.CellNameChs;
                double longitude = Convert.ToDouble(gsm900Cell.Longitude.Value);
                double latitude = Convert.ToDouble(gsm900Cell.Latitude.Value);
                double antHeight = Convert.ToDouble(gsm900Cell.AntHeight.Value);
                double azimuth = gsm900Cell.Azimuth.Value;
                double tilt = gsm900Cell.Tilt.Value;
                int EARFCN = gsm900Cell.EARFCN.Value;
                double EIRP = gsm900Cell.EIRP.Value;
                double radius = gsm900Cell.CoverageRadius.Value;
                tilt = tilt > 0 ? tilt : 7;

                pFeatureBuffer = pFeatureClass.CreateFeatureBuffer();//创建图元缓冲

                double Direction = (450 - azimuth) % 360;

                double x = Convert.ToDouble(gsm900Cell.x.Value);
                double y = Convert.ToDouble(gsm900Cell.y.Value);
                IPoint startPoint = GeometryUtilities.ConstructPoint3D(x, y, antHeight);
                IPoint leftPoint = GeometryUtilities.ConstructPoint3D(circRadius * Math.Cos((Direction - 18 * every) * Math.PI / 180) + x, y + circRadius * Math.Sin((Direction - 18 * every) * Math.PI / 180), antHeight);
                IPoint rightPoint = GeometryUtilities.ConstructPoint3D(circRadius * Math.Cos((Direction + 18 * every) * Math.PI / 180) + x, y + circRadius * Math.Sin((Direction + 9 * every) * Math.PI / 180), antHeight);

                IGeometryCollection pGeometryColl = GeometryUtilities.ConstructPolygon(new IPoint[] { startPoint, leftPoint, rightPoint });
                GeometryUtilities.MakeZAware(pGeometryColl as IGeometry);

                pFeatureBuffer.Shape = pGeometryColl as IGeometry;
                pFeatureBuffer.set_Value(pFeatureBuffer.Fields.FindField("eNodeB"), lac);
                pFeatureBuffer.set_Value(pFeatureBuffer.Fields.FindField("CI"), ci);
                pFeatureBuffer.set_Value(pFeatureBuffer.Fields.FindField("CellName"), cellName);
                pFeatureBuffer.set_Value(pFeatureBuffer.Fields.FindField("CellNameCN"), cellNameChs);
                pFeatureBuffer.set_Value(pFeatureBuffer.Fields.FindField("Longitude"), longitude);
                pFeatureBuffer.set_Value(pFeatureBuffer.Fields.FindField("Latitude"), latitude);
                pFeatureBuffer.set_Value(pFeatureBuffer.Fields.FindField("AntHeight"), antHeight);
                pFeatureBuffer.set_Value(pFeatureBuffer.Fields.FindField("Azimuth"), azimuth);
                pFeatureBuffer.set_Value(pFeatureBuffer.Fields.FindField("EARFCN"), EARFCN);
                pFeatureBuffer.set_Value(pFeatureBuffer.Fields.FindField("EIRP"), EIRP);
                pFeatureBuffer.set_Value(pFeatureBuffer.Fields.FindField("Tilt"), tilt);
                pFeatureBuffer.set_Value(pFeatureBuffer.Fields.FindField("Radius"), radius);
                pFeatureCursor.InsertFeature(pFeatureBuffer);

            }

            //一次性提交
            pFeatureCursor.Flush();

            //stop editing   
            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);

            //GISMapApplication.Instance.RefreshLayer(pFeatureLayer);
            GISMapApplication.Instance.FullExtent(pFeatureLayer.AreaOfInterest);
            return true;
        }

        /// <summary>
        /// 刷新GSM1800小区图层
        /// </summary>
        public void RefreshGSM1800Cell()
        {
            IList<CELL> GSM1800CellData = IbatisHelper.ExecuteQueryForList<CELL>("GetGSM1800CellLayerData", null);

            IFeatureLayer pFeatureLayer = GISMapApplication.Instance.GetLayer(LayerNames.GSM1800Cell) as IFeatureLayer;

            FeatureUtilities.DeleteFeatureLayerFeatrues(pFeatureLayer);

            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;


            IDataset dataset = (IDataset)pFeatureClass;
            IWorkspace workspace = dataset.Workspace;
            //Cast for an IWorkspaceEdit   
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
            //start an edit session and operation               
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            IFeatureCursor pFeatureCursor = pFeatureClass.Insert(true);
            IFeatureBuffer pFeatureBuffer;

            //循环添加
            double circRadius = 0.00025;//小区1800生成的半径大小
            double every = 1;


            foreach (CELL gsm1800Cell in GSM1800CellData)
            {
                if ((!gsm1800Cell.Longitude.HasValue) || (!gsm1800Cell.Latitude.HasValue) || (!gsm1800Cell.AntHeight.HasValue) || (!gsm1800Cell.Azimuth.HasValue) || (!gsm1800Cell.Tilt.HasValue))
                {
                    continue;
                }

                int lac = gsm1800Cell.eNodeB;
                int ci = gsm1800Cell.CI;
                string cellName = gsm1800Cell.CellName;
                string cellNameChs = gsm1800Cell.CellNameChs;
                double longitude = Convert.ToDouble(gsm1800Cell.Longitude.Value);
                double latitude = Convert.ToDouble(gsm1800Cell.Latitude.Value);
                double antHeight = Convert.ToDouble(gsm1800Cell.AntHeight.Value);
                double azimuth = gsm1800Cell.Azimuth.Value;
                double tilt = gsm1800Cell.Tilt.Value;
                tilt = tilt > 0 ? tilt : 7;

                pFeatureBuffer = pFeatureClass.CreateFeatureBuffer();//创建图元缓冲

                double Direction = (450 - azimuth) % 360;

                IPoint startPoint = GeometryUtilities.ConstructPoint3D(longitude, latitude, antHeight);
                IPoint leftPoint = GeometryUtilities.ConstructPoint3D(circRadius * Math.Cos((Direction - 18 * every) * Math.PI / 180) + longitude, latitude + circRadius * Math.Sin((Direction - 18 * every) * Math.PI / 180), antHeight);
                IPoint rightPoint = GeometryUtilities.ConstructPoint3D(circRadius * Math.Cos((Direction + 18 * every) * Math.PI / 180) + longitude, latitude + circRadius * Math.Sin((Direction + 9 * every) * Math.PI / 180), antHeight);

                //startPoint = PointConvert.Instance.GetProjectPoint(startPoint);
                //startPoint = GeometryUtilities.ConstructPoint3D(startPoint, antHeight);//增加高度
                //leftPoint = PointConvert.Instance.GetProjectPoint(leftPoint);
                //leftPoint = GeometryUtilities.ConstructPoint3D(leftPoint, antHeight);//增加高度
                //rightPoint = PointConvert.Instance.GetProjectPoint(rightPoint);
                //rightPoint = GeometryUtilities.ConstructPoint3D(rightPoint, antHeight);//增加高度

                IGeometryCollection pGeometryColl = GeometryUtilities.ConstructPolygon(new IPoint[] { startPoint, leftPoint, rightPoint });
                GeometryUtilities.MakeZAware(pGeometryColl as IGeometry);

                pFeatureBuffer.Shape = pGeometryColl as IGeometry;
                pFeatureBuffer.set_Value(pFeatureBuffer.Fields.FindField("eNodeB"), lac);
                pFeatureBuffer.set_Value(pFeatureBuffer.Fields.FindField("CI"), ci);
                pFeatureBuffer.set_Value(pFeatureBuffer.Fields.FindField("CellName"), cellName);
                pFeatureBuffer.set_Value(pFeatureBuffer.Fields.FindField("CellNameCN"), cellNameChs);
                pFeatureBuffer.set_Value(pFeatureBuffer.Fields.FindField("Longitude"), longitude);
                pFeatureBuffer.set_Value(pFeatureBuffer.Fields.FindField("Latitude"), latitude);
                pFeatureBuffer.set_Value(pFeatureBuffer.Fields.FindField("AntHeight"), antHeight);
                pFeatureBuffer.set_Value(pFeatureBuffer.Fields.FindField("Azimuth"), azimuth);
                pFeatureBuffer.set_Value(pFeatureBuffer.Fields.FindField("Tilt"), tilt);
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
        }
    }
}
