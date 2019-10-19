using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using LTE.DB;
using LTE.InternalInterference;
using LTE.InternalInterference.Grid;
using System.Threading;
using GisClient;

namespace LTE.WebAPI.Models
{
    #region 刷新小区图层
    public class RefreshCellLayerModel
    {
        public static Result RefreshCell()
        {
            //LTE.GIS.OperateCellLayer cellLayer = new LTE.GIS.OperateCellLayer();
            //if (!cellLayer.RefreshCellLayer())
            //    return new Result(false, "小区数据为空");
            //return new Result(true);
            try
            {
                GisClient.Result res = GisClient.ServiceApi.getGisLayerService().RefreshCell();
                if (res.Ok)
                {
                    return new Result(true, "小区图层刷新成功");
                }
                else
                {
                    return new Result(false, "小区图层刷新失败");
                }
            }
            catch (Exception e)
            {
                return new Result(false, "远程调用失败" + e);
            }
            finally
            {
                ServiceApi.CloseConn();
            }
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
            //CellInfo cellInfo = new CellInfo();
            //cellInfo.SourceName = cellName;
            //validateCell(ref cellInfo);

            //if (!AnalysisEntry.DisplayAnalysis(cellInfo))
            //{
            //    return new Result(false, "请先进行小区覆盖计算");
            //}
            //return new Result(true);
            try
            {
                GisClient.Result res = GisClient.ServiceApi.getGisLayerService().refreshGroundCover(cellName);
                if (res.Ok)
                {
                    return new Result(true, "地面覆盖图层刷新成功");
                }
                else
                {
                    return new Result(false, "地面覆盖图层刷新失败");
                }
            }
            catch (Exception e)
            {
                return new Result(false, "远程调用失败" + e);
            }
            finally
            {
                ServiceApi.CloseConn();
            }
        }

        // 刷新小区立体覆盖图层
        public Result refresh3DCover()
        {
            //CellInfo cellInfo = new CellInfo();
            //cellInfo.SourceName = cellName;
            //validateCell(ref cellInfo);

            //if(!AnalysisEntry.Display3DAnalysis(cellInfo))
            //{
            //    return new Result(false, "请先进行小区覆盖计算");
            //}

            //return new Result(true);
            try
            {
                GisClient.Result res = GisClient.ServiceApi.getGisLayerService().refresh3DCover(cellName);
                if (res.Ok)
                {
                    return new Result(true, "地面覆盖图层刷新成功");
                }
                else
                {
                    return new Result(false, "地面覆盖图层刷新失败");
                }
            }
            catch (Exception e)
            {
                return new Result(false, "远程调用失败" + e);
            }
            finally
            {
                ServiceApi.CloseConn();
            }
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
            int minXid = 0, minYid = 0, maxXid = 0, maxYid = 0;
            getGridID(ref minXid, ref minYid, ref maxXid, ref maxYid);
            try
            {
                GisClient.Result res = GisClient.ServiceApi.getGisLayerService().refreshGroundCoverLayer(minXid, minYid, maxXid, maxYid);
                if (res.Ok)
                {
                    return new Result(true, "区域地面覆盖图层刷新成功");
                }
                else
                {
                    return new Result(false, "区域地面覆盖图层刷新失败");
                }
            } 
            catch (Exception e)
            {
                return new Result(false, "远程调用失败" + e);
            }
            finally
            {
                ServiceApi.CloseConn();
            }

            //OperateCoverGirdLayer operateGrid = new OperateCoverGirdLayer();
            //operateGrid.ClearLayer();
            //if (!operateGrid.constuctAreaGrids(minXid, minYid, maxXid, maxYid))
            //    return new Result(false, "请先对区域内的小区进行覆盖计算");
            //return new Result(true);
        }

        // 刷新区域立体覆盖图层
        public Result refresh3DCoverLayer()
        {
            int minXid = 0, minYid = 0, maxXid = 0, maxYid = 0;
            getGridID(ref minXid, ref minYid, ref maxXid, ref maxYid);

            try
            {
                GisClient.Result res = GisClient.ServiceApi.getGisLayerService().refresh3DCoverLayer(minXid, minYid, maxXid, maxYid);
                if (res.Ok)
                {
                    return new Result(true, "区域立体覆盖图层刷新成功");
                }
                else
                {
                    return new Result(false, "区域立体覆盖图层刷新失败");
                }
            }
            catch (Exception e)
            {
                return new Result(false, "远程调用失败" + e);
            }
            finally
            {
                ServiceApi.CloseConn();
            }
            //OperateCoverGird3DLayer operateGrid = new OperateCoverGird3DLayer(LayerNames.AreaCoverGrid3Ds);
            //operateGrid.ClearLayer();
            //if (!operateGrid.constuctAreaGrid3Ds(minxid, minyid, maxxid, maxyid))
            //    return new Result(false, "请先对区域内的小区进行覆盖计算");
            //return new Result(true);
        }

        private void getGridID(ref int minxid, ref int minyid, ref int maxxid, ref int maxyid)
        {
            Geometric.Point pMin = new Geometric.Point();
            pMin.X = this.minLongitude;
            pMin.Y = this.minLatitude;
            pMin.Z = 0;
            LTE.Utils.PointConvertByProj.Instance.GetProjectPoint(pMin);

            Geometric.Point pMax = new Geometric.Point();
            pMax.X = this.maxLongitude;
            pMax.Y = this.maxLatitude;
            pMax.Z = 0;
            LTE.Utils.PointConvertByProj.Instance.GetProjectPoint(pMax);

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
            try
            {
                GisClient.Result res = GisClient.ServiceApi.getGisLayerService().refreshDTLayer();
                if (res.Ok)
                {
                    return new Result(true, "路测图层刷新成功");
                }
                else
                {
                    return new Result(false, "路测图层刷新失败");
                }
            }
            catch (Exception e)
            {
                return new Result(false, "远程调用失败" + e);
            }
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


        // 刷新弱覆盖点图层
        public Result refreshWeakLayer()
        {
            return refreshLayer(DefectType.Weak);
        }

        // 刷新过覆盖点图层
        public Result refreshExcessiveLayer()
        {
            return refreshLayer(DefectType.Excessive);
        }

        // 刷新重叠覆盖点图层
        public Result refreshOverlappedLayer()
        {
            return refreshLayer(DefectType.Overlapped);
        }

        // 刷新PCI冲突点图层
        public Result refreshPCIconflictLayer()
        {
            return refreshLayer(DefectType.PCIconflict);
        }

        // 刷新PCI混淆点图层
        public Result refreshPCIconfusionLayer()
        {
            return refreshLayer(DefectType.PCIconfusion);
        }

        // 刷新PCI mod3 对打点图层
        public Result refreshPCImod3Layer()
        {
            return refreshLayer(DefectType.PCImod3);
        }

        private Result refreshLayer(DefectType type)
        {
            int minXid = 0, minYid = 0, maxXid = 0, maxYid = 0;
            getGridID(ref minXid, ref minYid, ref maxXid, ref maxYid);

            try
            {
                GisClient.Result res = GisClient.ServiceApi.getGisLayerService().refreshDefectLayer(minXid, minYid, maxXid, maxYid,type);
                if (res.Ok)
                {
                    return new Result(true, "网内干扰图层刷新成功");
                }
                else
                {
                    return new Result(false, "网内干扰图层刷新失败");
                }
            }
            catch (Exception e)
            {
                return new Result(false, "远程调用失败" + e);
            }
            finally
            {
                ServiceApi.CloseConn();
            }
            //OperateDefectLayer operateGrid3d = new OperateDefectLayer(layerName);
            //operateGrid3d.ClearLayer();
            //if (!operateGrid3d.constuctGrid3Ds(minxid, minyid, maxxid, maxyid, type))
            //    return new Result(false, "数据为空");
            //return new Result(true);
        }

        private void getGridID(ref int minxid, ref int minyid, ref int maxxid, ref int maxyid)
        {
            Geometric.Point pMin = new Geometric.Point();
            pMin.X = this.minLongitude;
            pMin.Y = this.minLatitude;
            pMin.Z = 0;
            LTE.Utils.PointConvertByProj.Instance.GetProjectPoint(pMin);

            Geometric.Point pMax = new Geometric.Point();
            pMax.X = this.maxLongitude;
            pMax.Y = this.maxLatitude;
            pMax.Z = 0;
            LTE.Utils.PointConvertByProj.Instance.GetProjectPoint(pMax);

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
            try
            {
                GisClient.Result res = GisClient.ServiceApi.getGisLayerService().refreshInfLayer();
                if (res.Ok)
                {
                    return new Result(true, "网外干扰图层刷新成功");
                }
                else
                {
                    return new Result(false, "网外干扰图层刷新失败");
                }
            }
            catch (Exception e)
            {
                return new Result(false, "远程调用失败" + e);
            }
            finally
            {
                ServiceApi.CloseConn();
            }
        }
    }
    #endregion

    #region 刷新地形 TIN 图层
    public class RefreshTINLayer
    {
        public static Result refreshTINLayer()
        {
            //OperateTINLayer layer = new OperateTINLayer(LayerNames.TIN);
            //layer.ClearLayer();
            //if (!layer.constuctTIN())
            //    return new Result(false, "无TIN");

            //OperateTINLayer layer1 = new OperateTINLayer(LayerNames.TIN1);
            //layer1.ClearLayer();
            //if (!layer1.constuctTIN())
            //    return new Result(false, "无TIN");

            //return new Result(true);
            try
            {
                GisClient.Result res = GisClient.ServiceApi.getGisLayerService().refreshTINLayer();
                if (res.Ok)
                {
                    return new Result(true, "Tin图层刷新成功");
                }
                else
                {
                    return new Result(false, "Tin图层刷新失败");
                }
            }
            catch (Exception e)
            {
                return new Result(false, "远程调用失败" + e);
            }
            finally
            {
                ServiceApi.CloseConn();
            }
        }
    }
    #endregion

    #region 刷新建筑物图层
    public class RefreshBuildingLayer
    {
        public static Result refreshBuildingLayer()
        {
            //OperateBuildingLayer layer = new OperateBuildingLayer(LayerNames.Building);
            //layer.ClearLayer();
            //if (!layer.constuctBuilding()) 
            //    return new Result(false, "无建筑物数据");

            //OperateBuildingLayer layer1 = new OperateBuildingLayer(LayerNames.Building1);
            //layer1.ClearLayer();
            //if (!layer1.constuctBuilding1())
            //    return new Result(false, "无建筑物数据");

            //return new Result(true);
            try
            {
                GisClient.Result res = GisClient.ServiceApi.getGisLayerService().refreshBuildingLayer();
                if (res.Ok)
                {
                    return new Result(true, "建筑物图层刷新成功");
                }
                else
                {
                    return new Result(false, "建筑物图层刷新失败");
                }
            }
            catch (Exception e)
            {
                return new Result(false, "远程调用失败" + e);
            }
            finally
            {
                ServiceApi.CloseConn();
            }
        }
    }
    #endregion

    #region 刷新建筑物底边平滑图层
    public class RefreshBuildingSmoothLayer
    {
        public static Result refreshBuildingSmoothLayer()
        {
            //OperateSmoothBuildingLayer layer = new OperateSmoothBuildingLayer();
            //layer.ClearLayer();
            //if (!layer.constuctBuildingVertex())
            //    return new Result(false, "无建筑物数据");

            //return new Result(true);
            try
            {
                GisClient.Result res = GisClient.ServiceApi.getGisLayerService().refreshBuildingSmoothLayer();
                if (res.Ok)
                {
                    return new Result(true, "建筑物底边平滑图层刷新成功");
                }
                else
                {
                    return new Result(false, "建筑物底边平滑图层刷新失败");
                }
            }
            catch (Exception e)
            {
                return new Result(false, "远程调用失败" + e);
            }
            finally
            {
                ServiceApi.CloseConn();
            }
        }
    }
    #endregion
}