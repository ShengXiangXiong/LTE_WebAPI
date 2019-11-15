using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
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
        //TODO：加一个非法参数检测切面，若前端输入到后端一个非法值，可能这边接收到的为null
        public Result PostGrid([FromBody]GridModel grid)
        {
            return grid.ConstructGrid();
        }
    }
}
