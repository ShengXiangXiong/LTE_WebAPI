using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using GisClient;
using LTE.DB;
using LTE.WebAPI.Attributes;
using LTE.WebAPI.Models;
using Result = LTE.WebAPI.Models.Result;

namespace LTE.WebAPI.Controllers
{
    public class RefreshLayerController : ApiController
    {
        /// <summary>
        /// 刷新小区图层
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [TaskLoadInfo(taskName = "小区基站图层", type = TaskType.AreaGSMLayer)]
        public Result PostRefreshCell()
        {
            return RefreshCellLayerModel.RefreshCell();
        }

        /// <summary>
        /// 刷新小区地面覆盖图层
        /// </summary>
        /// <param name="layer">小区名称</param>
        /// <returns></returns>
        [HttpPost]
        [TaskLoadInfo(taskName="小区地面覆盖图层",type = TaskType.CellCoverLayer)]
        public Result PostRefreshCellGroundCover([FromBody]RefreshCellCoverLayerModel layer)
        {
            return layer.refreshGroundCover();
        }

        /// <summary>
        /// 刷新小区立体覆盖图层
        /// </summary>
        /// <param name="layer">小区名称</param>
        /// <returns></returns>
        [HttpPost]
        [TaskLoadInfo(taskName = "小区立体覆盖图层", type = TaskType.CellCoverLayer)]
        public Result PostRefreshCell3DCover([FromBody]RefreshCellCoverLayerModel layer)
        {
            return layer.refresh3DCover();
        }

        /// <summary>
        /// 刷新区域地面覆盖图层
        /// </summary>
        /// <param name="layer">区域范围</param>
        /// <returns></returns>
        [HttpPost]
        [TaskLoadInfo(taskName = "区域地面覆盖图层", type = TaskType.AreaCoverLayer)]
        public Result PostRefreshAreaGroundCover([FromBody]RefreshAreaCoverLayerModel layer)
        {
            return layer.refreshGroundCoverLayer();
        }

        /// <summary>
        /// 刷新区域立体覆盖图层
        /// </summary>
        /// <param name="layer">区域范围</param>
        /// <returns></returns>
        [HttpPost]
        [TaskLoadInfo(taskName = "区域立体覆盖图层", type = TaskType.AreaCoverLayer)]
        public Result PostRefreshArea3DCover([FromBody]RefreshAreaCoverLayerModel layer)
        {
            return layer.refresh3DCoverLayer();
        }

        /// <summary>
        /// 刷新虚拟路测图层
        /// </summary>
        /// <returns></returns>
        //[HttpPost]
        //public Result PostRefreshDTdataLayer()
        //{
        //    return RefreshDTdataLayerModel.refreshDTLayer();
        //}

        /// <summary>
        /// 刷新弱覆盖点图层
        /// </summary>
        /// <param name="layer">区域范围</param>
        /// <returns></returns>
        [HttpPost]
        [TaskLoadInfo(taskName = "弱覆盖点图层", type = TaskType.AreaInterferenceLayer)]
        public Result PostRefreshWeakLayer([FromBody]RefreshAreaCoverDefectLayerModel layer)
        {
            return layer.refreshWeakLayer();
        }

        /// <summary>
        /// 刷新过覆盖点图层
        /// </summary>
        /// <param name="layer">区域范围</param>
        /// <returns></returns>
        [HttpPost]
        [TaskLoadInfo(taskName = "过覆盖点图层", type = TaskType.AreaInterferenceLayer)]
        public Result PostRefreshExcessiveLayer([FromBody]RefreshAreaCoverDefectLayerModel layer)
        {
            return layer.refreshExcessiveLayer();
        }

        /// <summary>
        /// 刷新重叠覆盖点图层
        /// </summary>
        /// <param name="layer">区域范围</param>
        /// <returns></returns>
        [HttpPost]
        [TaskLoadInfo(taskName = "重叠覆盖点图层", type = TaskType.AreaInterferenceLayer)]
        public Result PostRefreshOverlappedLayer([FromBody]RefreshAreaCoverDefectLayerModel layer)
        {
            return layer.refreshOverlappedLayer();
        }

        /// <summary>
        /// 刷新PCI冲突点图层
        /// </summary>
        /// <param name="layer">区域范围</param>
        /// <returns></returns>
        [HttpPost]
        [TaskLoadInfo(taskName = "PCI冲突点图层", type = TaskType.AreaInterferenceLayer)]
        public Result PostRefreshPCIconflictLayer([FromBody]RefreshAreaCoverDefectLayerModel layer)
        {
            return layer.refreshPCIconflictLayer();
        }

        /// <summary>
        /// 刷新PCI混淆点图层
        /// </summary>
        /// <param name="layer">区域范围</param>
        /// <returns></returns>
        [HttpPost]
        [TaskLoadInfo(taskName = "PCI混淆点图层", type = TaskType.AreaInterferenceLayer)]
        public Result PostRefreshPCIconfusionLayer([FromBody]RefreshAreaCoverDefectLayerModel layer)
        {
            return layer.refreshPCIconfusionLayer();
        }

        /// <summary>
        ///  刷新PCI mod3 对打点图层
        /// </summary>
        /// <param name="layer">区域范围</param>
        /// <returns></returns>
        [HttpPost]
        [TaskLoadInfo(taskName = "PCI mod3 对打点图层", type = TaskType.AreaInterferenceLayer)]
        public Result PostRefreshPCImod3Layer([FromBody]RefreshAreaCoverDefectLayerModel layer)
        {
            return layer.refreshPCImod3Layer();
        }

        /// <summary>
        /// 刷新网外干扰点图层
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public Result PostRefreshInterferenceLayer()
        {
            return RefreshInterferenceLayer.refreshInfLayer();
        }

        /// <summary>
        /// 刷新地形 TIN 图层 
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [TaskLoadInfo(taskName = " TIN 图层 ", type = TaskType.AreaTinLayer)]
        public Result PostRefreshTINLayer()
        {
            return RefreshTINLayer.refreshTINLayer();
        }

        /// <summary>
        /// 刷新建筑物图层
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [TaskLoadInfo(taskName = "建筑物图层", type = TaskType.AreaBuildingLayer)]
        public Result PostRefreshBuildingLayer()
        {
            return RefreshBuildingLayer.refreshBuildingLayer();
        }

        /// <summary>
        /// 刷新建筑物底边平滑图层
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [TaskLoadInfo(taskName = "建筑物底边平滑图层", type = TaskType.AreaBuildingLayer)]
        public Result PostRefreshBuildingSmoothLayer()
        {
            return RefreshBuildingSmoothLayer.refreshBuildingSmoothLayer();
        }

        [HttpPost]
        [TaskLoadInfo(taskName = "路测刷新", type = TaskType.RoadTestLayer)]
        public Result PostRefreshDTLayer([FromBody]RefreshDTLayerModel layer)
        {
            return layer.refreshDTLayer();
        }

        [HttpPost]
        [TaskLoadInfo(taskName = "反向跟踪起点刷新", type = TaskType.SelectedPointsLayer)]
        public Result PostRefreshSPLayer([FromBody]RefreshSPLayerModel layer)
        {
            return layer.refreshSPLayer();
        }

        /// <summary>
        /// 刷新固定终端图层
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [TaskLoadInfo(taskName = "固定终端图层", type = TaskType.AreaGSMLayer)]
        public Result PostFixTerminalLayer()
        {
            try
            {
                GisClient.Result res = GisClient.ServiceApi.getGisLayerService().refreshFixTerminalLayer();
                if (res.Ok)
                {
                    Hashtable ht = new Hashtable();
                    ht["IndexName"] = "固定终端";
                    ht["ShpName"] = res.ShpName;
                    ht["Type"] = "fix";
                    ht["DateTime"] = DateTime.Now;
                    IbatisHelper.ExecuteInsert("insShp", ht);

                    return new Result(true, "固定终端图层刷新成功");
                }
                else
                {
                    return new Result(false, "固定终端图层刷新失败");
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
}
