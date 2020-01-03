using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LTE.WebAPI.Models;

namespace LTE.WebAPI.Controllers
{
    public class CalibrationController : ApiController
    { 
        /// <summary>
        /// 多场景系数校正：基于多目标遗传算法，对不同场景中的直射、反射和绕射系数进行校正
        /// </summary>
        /// <param name="cali"></param>
        /// <returns>校正后的直射、反射和绕射系数</returns>
        [HttpPost]
        public Result PostCalibrate([FromBody]CalibrationModel cali)
        {
            return cali.calilbrate();
        }
    }
}
