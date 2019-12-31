using LTE.WebAPI.Attributes;
using LTE.WebAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace LTE.WebAPI.Controllers
{
    public class AreaCoverAnalysisController : ApiController
    {

        [HttpPost]
        [TaskLoadInfo(taskName = "区域覆盖分析", type = TaskType.AreaCoverCompu)]
        //[ApiAuthorize(Roles = "admin")]
        public Result Post([FromBody]Area area)
        {
            return area.computeAreaAnlysis();
        }


        // DELETE: api/AreaCoverAnalysis/5
        public void Delete(int id)
        {
        }
    }
}
