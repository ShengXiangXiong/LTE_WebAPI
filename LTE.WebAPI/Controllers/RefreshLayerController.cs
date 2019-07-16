using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LTE.WebAPI.Models;

namespace LTE.WebAPI.Controllers
{
    public class RefreshLayerController : ApiController
    {
        /// <summary>
        /// 刷新小区图层
        /// </summary>
        /// <returns></returns>
        [HttpPost]
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
        public Result PostRefreshArea3DCover([FromBody]RefreshAreaCoverLayerModel layer)
        {
            return layer.refresh3DCoverLayer();
        }

        /// <summary>
        /// 刷新虚拟路测图层
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public Result PostRefreshDTdataLayer()
        {
            return RefreshDTdataLayerModel.refreshDTLayer();
        }

        /// <summary>
        /// 刷新弱覆盖点图层
        /// </summary>
        /// <param name="layer">区域范围</param>
        /// <returns></returns>
        [HttpPost]
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
    }
}
