using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LTE.WebAPI.Models;

namespace LTE.WebAPI.Controllers
{
    public class CellRadiusController : ApiController
    {
        /// <summary>
        /// 小区理论覆盖半径计算：根据小区的基本配置信息来计算小区的覆盖半径
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public Result PostCellRadius()
        {
            return new CellRadiusModel().calcRadius();
        }
    }
}
