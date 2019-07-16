using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LTE.WebAPI.Models;

namespace LTE.WebAPI.Controllers
{
    public class SmoothBuildingVertexController : ApiController
    {
        /// <summary>
        /// 建筑物底边平滑
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public Result PostSmooth()
        {
            return new SmoothBuildingVertexModel().smoothBuildingPoints();
        }
    }
}
