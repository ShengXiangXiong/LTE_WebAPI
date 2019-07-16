using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Http;
using System.Web.Http;
using LTE.WebAPI.Models;

namespace LTE.WebAPI.Controllers
{
    public class CellRayTracingController : ApiController
    {
        /// <summary>
        /// 基于射线跟踪进行小区覆盖计算
        /// </summary>
        /// <param name="rt">射线跟踪计算参数</param>
        /// <returns></returns>
        [HttpPost]
        public Result PostRayTracing([FromBody]CellRayTracingModel rt)
        {
            return rt.calc();
        }
    }
}
