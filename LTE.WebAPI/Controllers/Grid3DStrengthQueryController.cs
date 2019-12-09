using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LTE.WebAPI.Models;

namespace LTE.WebAPI.Controllers
{
    public class Grid3DStrengthQueryController : ApiController
    {
        /// <summary>
        /// 因无法呈现 3D 覆盖，因此给定栅格二维坐标，查询建筑物高度内的所有栅格场强
        /// </summary>
        /// <param name="grid">栅格二维坐标</param>
        /// <returns></returns>
        [HttpPost]
        public Result PostStrengthQuery([FromBody]Grid3DStrengthQueryModel grid)
        {
            return grid.query();
        }
    }
}
