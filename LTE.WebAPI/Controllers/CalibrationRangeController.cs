using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LTE.WebAPI.Models;

namespace LTE.WebAPI.Controllers
{
    public class CalibrationRangeController : ApiController
    {
        /// <summary>
        /// 更新系数校正射线生成及用于校正的经纬度范围
        /// </summary>
        /// <param name="calibrationRange"></param>
        /// <returns></returns>
        [HttpPost]
        public Result PostCalibrationRange([FromBody]CalibrationRangeModel calibrationRange)
        {
            return calibrationRange.updateCalibrationRange();
        }
    }
}
