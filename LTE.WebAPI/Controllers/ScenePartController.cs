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
    public class ScenePartController : ApiController
    {
        /// <summary>
        /// 场景划分
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [ApiAuthorize(Roles = "admin")]
        [TaskLoadInfo(taskName = "场景划分", type = TaskType.ScenePart)]
        public Result PostScenePart()
        {
            return new ScenePartModel().part();
            //之前的调用
            //  return ScenePartModel.part();
        }

        /// <summary>
        /// 聚类图层生成
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [ApiAuthorize(Roles = "admin")]
        [TaskLoadInfo(taskName = "聚类图层生成", type = TaskType.ClusterShp)]
        public Result PostClusterShp()
        {
            return new ClusterShpModel().cluster();
        }

        /// <summary>
        /// 矫正系数
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [ApiAuthorize(Roles = "admin")]
        [TaskLoadInfo(taskName = "矫正系数", type = TaskType.AdjCoefficient)]
        public Result PostAdjCoefficient()
        {
            return new AdjCoefficientModel().AdjCoefficient();
        }
    }
}
