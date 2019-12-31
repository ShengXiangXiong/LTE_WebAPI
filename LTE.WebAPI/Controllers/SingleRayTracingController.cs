using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LTE.WebAPI.Models;

namespace LTE.WebAPI.Controllers
{
    public class SingleRayTracingController : ApiController
    {
        /// <summary>
        /// 单射线跟踪：指定射线终点
        /// </summary>
        /// <param name="ray">终点坐标</param>
        /// <returns></returns>
        [HttpPost]
        public Result PostSingleRayTracing1([FromBody]SingleRayTracingModel1 ray)
        {
            return ray.rayTracing();
        }

        /// <summary>
        /// 单射线跟踪：指定射线方位角和下倾角
        /// </summary>
        /// <param name="ray">射线方位角，下倾角</param>
        /// <returns></returns>
        [HttpPost]
        public Result PostSingleRayTracing2([FromBody]SingleRayTracingModel2 ray)
        {
            return ray.rayTracing();
        }
    }
}
