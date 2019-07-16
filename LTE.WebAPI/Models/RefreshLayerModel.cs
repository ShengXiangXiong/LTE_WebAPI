using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using LTE.DB;
using LTE.InternalInterference;
using LTE.GIS;
using LTE.InternalInterference.Grid;
using System.Threading;

namespace LTE.WebAPI.Models
{
    #region 刷新小区图层
    public class RefreshCellLayerModel
    {
        public static Result RefreshCell()
        {
            LTE.GIS.OperateCellLayer cellLayer = new LTE.GIS.OperateCellLayer();
            if (!cellLayer.RefreshCellLayer())
                return new Result(false, "小区数据为空");
            return new Result(true);
        }
    }
    #endregion

    #region 刷新小区覆盖图层
    public class RefreshCellCoverLayerModel
    {
        /// <summary>
        /// 小区名称
        /// </summary>
        public string cellName { get; set; }


        // 刷新小区地面覆盖图层
        public Result refreshGroundCover()
        {
            CellInfo cellInfo = new CellInfo();
            cellInfo.SourceName = cellName;
            validateCell(ref cellInfo);

            if (!AnalysisEntry.DisplayAnalysis(cellInfo))
            {
                return new Result(false, "请先进行小区覆盖计算");
            }
            return new Result(true);
        }

        // 刷新小区立体覆盖图层
        public Result refresh3DCover()
        {
            CellInfo cellInfo = new CellInfo();
            cellInfo.SourceName = cellName;
            validateCell(ref cellInfo);

            if(!AnalysisEntry.Display3DAnalysis(cellInfo))
            {
                return new Result(false, "请先进行小区覆盖计算");
            }

            return new Result(true);
        }

        public Result validateCell(ref CellInfo cellInfo)
        {
            if (this.cellName == string.Empty)
            {
                return new Result(false, "请输入小区名称");
            }
            DataTable dt = IbatisHelper.ExecuteQueryForDataTable("SingleGetCellType", this.cellName);
            if (dt.Rows.Count == 0)
            {
                return new Result(false, "您输入的小区名称有误，请重新输入！");
            }
            cellInfo.eNodeB = Convert.ToInt32(dt.Rows[0]["eNodeB"]);
            cellInfo.CI = Convert.ToInt32(dt.Rows[0]["CI"]);
            return new Result(true);
        }
    }
    #endregion

    #region 刷新区域覆盖图层
    public class RefreshAreaCoverLayerModel
    {
        /// <summary>
        /// 最小经度
        /// </summary>
        public double minLongitude { get; set; }   

        /// <summary>
        /// 最小纬度
        /// </summary>
        public double minLatitude { get; set; }    

        /// <summary>
        /// 最大经度
        /// </summary>
        public double maxLongitude { get; set; }  

        /// <summary>
        /// 最大纬度
        /// </summary>
        public double maxLatitude { get; set; }    

        // 刷新区域地面覆盖图层
        public Result refreshGroundCoverLayer()
        {
            int minxid = 0, minyid = 0, maxxid = 0, maxyid = 0;
            getGridID(ref minxid, ref minyid, ref maxxid, ref maxyid);

            OperateCoverGirdLayer operateGrid = new OperateCoverGirdLayer(LayerNames.AreaCoverGrids);
            operateGrid.ClearLayer();
            if (!operateGrid.constuctAreaGrids(minxid, minyid, maxxid, maxyid))
                return new Result(false, "请先对区域内的小区进行覆盖计算");
            return new Result(true);
        }

        // 刷新区域立体覆盖图层
        public Result refresh3DCoverLayer()
        {
            int minxid = 0, minyid = 0, maxxid = 0, maxyid = 0;
            getGridID(ref minxid, ref minyid, ref maxxid, ref maxyid);

            OperateCoverGird3DLayer operateGrid = new OperateCoverGird3DLayer(LayerNames.AreaCoverGrid3Ds);
            operateGrid.ClearLayer();
            if (!operateGrid.constuctAreaGrid3Ds(minxid, minyid, maxxid, maxyid))
                return new Result(false, "请先对区域内的小区进行覆盖计算");
            return new Result(true);
        }

        private void getGridID(ref int minxid, ref int minyid, ref int maxxid, ref int maxyid)
        {
            ESRI.ArcGIS.Geometry.IPoint pMin = new ESRI.ArcGIS.Geometry.PointClass();
            pMin.X = this.minLongitude;
            pMin.Y = this.minLatitude;
            pMin.Z = 0;
            PointConvert.Instance.GetProjectPoint(pMin);

            ESRI.ArcGIS.Geometry.IPoint pMax = new ESRI.ArcGIS.Geometry.PointClass();
            pMax.X = this.maxLongitude;
            pMax.Y = this.maxLatitude;
            pMax.Z = 0;
            PointConvert.Instance.GetProjectPoint(pMax);

            GridHelper.getInstance().XYToGGrid(pMin.X, pMin.Y, ref minxid, ref minyid);
            GridHelper.getInstance().XYToGGrid(pMax.X, pMax.Y, ref maxxid, ref maxyid);
        }
    }
    #endregion

    #region 刷新虚拟路测图层
    public class RefreshDTdataLayerModel
    {
        public static Result refreshDTLayer()
        {
            OperateDTLayer layer = new OperateDTLayer();
            layer.ClearLayer();
            if (!layer.constuctDTGrids())
                return new Result(false, "路测数据不存在");
            return new Result(true);
        }
    }
    #endregion

    #region 刷新网内干扰图层
    public class RefreshAreaCoverDefectLayerModel
    {
        /// <summary>
        /// 最小经度
        /// </summary>
        public double minLongitude { get; set; }   

        /// <summary>
        /// 最小纬度
        /// </summary>
        public double minLatitude { get; set; }    

        /// <summary>
        /// 最大经度
        /// </summary>
        public double maxLongitude { get; set; }  
 
        /// <summary>
        /// 最大纬度
        /// </summary>
        public double maxLatitude { get; set; }    

        enum DefectType
        {
            Weak,         // 弱覆盖点
            Excessive,    // 过覆盖点
            Overlapped,   // 重叠覆盖点
            PCIconflict,  // PCI 冲突点
            PCIconfusion, // PCI 混淆
            PCImod3       // PCI 模 3 冲突点
        };

        // 刷新弱覆盖点图层
        public Result refreshWeakLayer()
        {
            return refreshLayer(LayerNames.Weak, (short)DefectType.Weak);
        }

        // 刷新过覆盖点图层
        public Result refreshExcessiveLayer()
        {
            return refreshLayer(LayerNames.Excessive, (short)DefectType.Excessive);
        }

        // 刷新重叠覆盖点图层
        public Result refreshOverlappedLayer()
        {
            return refreshLayer(LayerNames.Overlapped, (short)DefectType.Overlapped);
        }

        // 刷新PCI冲突点图层
        public Result refreshPCIconflictLayer()
        {
            return refreshLayer(LayerNames.PCIconflict, (short)DefectType.PCIconflict);
        }

        // 刷新PCI混淆点图层
        public Result refreshPCIconfusionLayer()
        {
            return refreshLayer(LayerNames.PCIconfusion, (short)DefectType.PCIconfusion);
        }

        // 刷新PCI mod3 对打点图层
        public Result refreshPCImod3Layer()
        {
            return refreshLayer(LayerNames.PCImod3, (short)DefectType.PCImod3);
        }

        private Result refreshLayer(string layerName, short type)
        {
            int minxid = 0, minyid = 0, maxxid = 0, maxyid = 0;
            getGridID(ref minxid, ref minyid, ref maxxid, ref maxyid);

            OperateDefectLayer operateGrid3d = new OperateDefectLayer(layerName);
            operateGrid3d.ClearLayer();
            if (!operateGrid3d.constuctGrid3Ds(minxid, minyid, maxxid, maxyid, type))
                return new Result(false, "数据为空");
            return new Result(true);
        }

        private void getGridID(ref int minxid, ref int minyid, ref int maxxid, ref int maxyid)
        {
            ESRI.ArcGIS.Geometry.IPoint pMin = new ESRI.ArcGIS.Geometry.PointClass();
            pMin.X = this.minLongitude;
            pMin.Y = this.minLatitude;
            pMin.Z = 0;
            PointConvert.Instance.GetProjectPoint(pMin);

            ESRI.ArcGIS.Geometry.IPoint pMax = new ESRI.ArcGIS.Geometry.PointClass();
            pMax.X = this.maxLongitude;
            pMax.Y = this.maxLatitude;
            pMax.Z = 0;
            PointConvert.Instance.GetProjectPoint(pMax);

            GridHelper.getInstance().XYToGGrid(pMin.X, pMin.Y, ref minxid, ref minyid);
            GridHelper.getInstance().XYToGGrid(pMax.X, pMax.Y, ref maxxid, ref maxyid);
        }
    }
    #endregion

    #region 刷新网外干扰图层
    public class RefreshInterferenceLayer
    {
        public static Result refreshInfLayer()
        {
            OperateInterferenceLocLayer layer = new OperateInterferenceLocLayer();
            layer.ClearLayer();
            if (!layer.constuctGrid3Ds())
                return new Result(false, "无干扰源");
            return new Result(true);
        }
    }
    #endregion

    #region 刷新地形 TIN 图层
    public class RefreshTINLayer
    {
        public static Result refreshTINLayer()
        {
            OperateTINLayer layer = new OperateTINLayer(LayerNames.TIN);
            layer.ClearLayer();
            if (!layer.constuctTIN())
                return new Result(false, "无TIN");

            OperateTINLayer layer1 = new OperateTINLayer(LayerNames.TIN1);
            layer1.ClearLayer();
            if (!layer1.constuctTIN())
                return new Result(false, "无TIN");

            return new Result(true);
        }
    }
    #endregion

    #region 刷新建筑物图层
    public class RefreshBuildingLayer
    {
        public static Result refreshBuildingLayer()
        {
            OperateBuildingLayer layer = new OperateBuildingLayer(LayerNames.Building);
            layer.ClearLayer();
            if (!layer.constuctBuilding())
                return new Result(false, "无建筑物数据");

            OperateBuildingLayer layer1 = new OperateBuildingLayer(LayerNames.Building1);
            layer1.ClearLayer();
            if (!layer1.constuctBuilding1())
                return new Result(false, "无建筑物数据");

            return new Result(true);
        }
    }
    #endregion

    #region 刷新建筑物底边平滑图层
    public class RefreshBuildingSmoothLayer
    {
        public static Result refreshBuildingSmoothLayer()
        {
            OperateSmoothBuildingLayer layer = new OperateSmoothBuildingLayer();
            layer.ClearLayer();
            if (!layer.constuctBuildingVertex())
                return new Result(false, "无建筑物数据");

            return new Result(true);
        }
    }
    #endregion
}