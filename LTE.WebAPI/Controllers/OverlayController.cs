using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LTE.WebAPI.Models;
using LTE.WebAPI.Attributes;

namespace LTE.WebAPI.Controllers
{
    public class OverlayController : ApiController
    {
        /// <summary>
        /// 建筑物叠加分析
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [ApiAuthorize(Roles = "admin")]
        [TaskLoadInfo(taskName = "建筑物叠加分析", type = TaskType.BuildingOverlay)]
        public Result PostBuildingOverlay()
        {
            return new BuildingOverlayModel().overlaybuilding();
        }

        /// <summary>
        /// 水面叠加分析
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [ApiAuthorize(Roles = "admin")]
        [TaskLoadInfo(taskName = "水面叠加分析", type = TaskType.WaterOverlay)]
        public Result PostWaterOverlay()
        {
            return new WaterOverlayModel().overlaywater();
        }


        /// <summary>
        /// 草地叠加分析
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [ApiAuthorize(Roles = "admin")]
        [TaskLoadInfo(taskName = "草地叠加分析", type = TaskType.GrassOverlay)]
        public Result PostGrassOverlay()
        {
            return new GrassOverlayModel().overlaygrass();
        }
    }
}
