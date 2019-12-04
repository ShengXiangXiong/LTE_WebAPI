﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LTE.WebAPI.Attributes;
using LTE.WebAPI.Models;

namespace LTE.WebAPI.Controllers
{
    public class RayRecordController : ApiController
    {
        /// <summary>
        /// 记录用于干扰定位的射线
        /// </summary>
        /// <param name="ray">界面输入参数</param>
        /// <returns></returns>
        [HttpPost]
        [TaskLoadInfo(taskName = "射线记录Loc", type = TaskType.RayRecordLoc)]
        public Result PostRayRecordLoc([FromBody]RayLocRecordModel ray)
        {
            return ray.RecordRayLoc();
        }

        /// <summary>
        /// 记录用于系数校正的射线
        /// </summary>
        /// <param name="ray">界面输入参数</param>
        /// <returns></returns>
        [HttpPost]
        [TaskLoadInfo(taskName = "射线记录Adj", type = TaskType.RayRecordAdj)]
        public Result PostRayRecordAdj([FromBody]RayRecordAdjModel ray)
        {
            return ray.rayRecord();
        }
    }
}
