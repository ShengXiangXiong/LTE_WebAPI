using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LTE.WebAPI.Models;

namespace LTE.WebAPI.Controllers
{
    public class DebugController : ApiController
    {
        /// <summary>
        /// 多进程调试：小区覆盖计算
        /// </summary>
        /// <param name="rt">界面输入的参数</param>
        /// <returns></returns>
        [HttpPost]
        public Result PostRayRacing([FromBody]DebugCellRayTracingModel rt)
        {
            return rt.calc();
        }

        /// <summary>
        /// 多进程调试：记录用于系数校正的射线
        /// </summary>
        /// <param name="rt">界面输入的参数</param>
        /// <returns></returns>
        [HttpPost]
        public Result PostRayRecordAdj([FromBody]DebugRayRecordAdjModel rt)
        {
            return rt.rayRecord();
        }

        /// <summary>
        /// 多进程调试：记录用于干扰定位的射线
        /// </summary>
        /// <param name="rt">界面输入的参数</param>
        /// <returns></returns>
        [HttpPost]
        public Result PostRayRecordLoc([FromBody]DebugRayRecordLocModel rt)
        {
            return rt.rayRecord();
        }
    }
}
