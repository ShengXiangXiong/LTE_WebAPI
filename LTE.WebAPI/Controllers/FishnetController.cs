using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LTE.WebAPI.Models;

namespace LTE.WebAPI.Controllers
{
    public class FishnetController : ApiController
    {
        /// <summary>
        /// 生成渔网
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public Result PostFishnet()
        {
            return new FishnetModel().makeFishnet();
        }
    }
}
