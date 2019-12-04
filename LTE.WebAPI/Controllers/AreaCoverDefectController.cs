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
    public class AreaCoverDefectController : ApiController
    {
        /// <summary>
        /// 网内干扰分析：分析给定区域中的网内干扰点，如过覆盖点、重叠覆盖点等
        /// </summary>
        /// <param name="defect">网内干扰分析范围</param>
        /// <returns>各种网内干扰点比例</returns>
        [HttpPost]
        [TaskLoadInfo(taskName = "网内干扰分析", type = TaskType.AreaInterference)]
        public Result PostAreaCoverDefect([FromBody]AreaCoverDefectModel defect)
        {
            return defect.defectAnalysis();
        }
    }
}
