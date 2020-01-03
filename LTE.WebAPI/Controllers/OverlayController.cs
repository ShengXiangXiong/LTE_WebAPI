using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LTE.WebAPI.Models;

namespace LTE.WebAPI.Controllers
{
    public class OverlayController : ApiController
    {
        /// <summary>
        /// 建筑物叠加分析
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public Result PostBuildingOverlay()
        {
            return new BuildingOverlayModel().overlaybuilding();
        }

        /// <summary>
        /// 水面叠加分析
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public Result PostWaterOverlay()
        {
            return new WaterOverlayModel().overlaywater();
        }


        /// <summary>
        /// 水面叠加分析
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public Result PostGrassOverlay()
        {
            return new GrassOverlayModel().overlaygrass();
        }
    }
}
