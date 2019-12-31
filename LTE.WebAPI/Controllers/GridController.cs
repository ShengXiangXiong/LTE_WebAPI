using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LTE.DB;
using LTE.WebAPI.Models;

namespace LTE.WebAPI.Controllers
{
    public class GridController : ApiController
    {
        /// <summary>
        /// 网格划分，包括地面栅格、建筑物栅格，均匀栅格加速结构
        /// </summary>
        /// <param name="grid">区域范围</param>
        /// <returns></returns>
        [HttpPost]
        public Result PostGrid([FromBody]GridModel grid)
        {
            return grid.ConstructGrid();
        }

    }
}
