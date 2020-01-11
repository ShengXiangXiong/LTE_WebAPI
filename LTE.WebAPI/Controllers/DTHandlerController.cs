using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LTE.WebAPI.Attributes;
using LTE.WebAPI.Models;

namespace LTE.WebAPI.Controllers
{
    public class DTHandlerController : ApiController
    {
        /// <summary>
        /// 根据两个表sinr计算干扰点信号强度
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [TaskLoadInfo(taskName = "计算RSRP", type = TaskType.ComputeInfRSRP)]
        public Result PostComputeInfRSRP([FromBody]PreHandleDTForLoc rt)
        {
            return rt.ComputeInfRSRP();
        }

        /// <summary>
        /// 更新处理路测信息
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public Result PostUpdateDTData()
        {
            return new DTHandlerModel().UpdateDTData();
        }
    }
}