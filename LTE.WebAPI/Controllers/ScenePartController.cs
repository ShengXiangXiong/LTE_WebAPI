using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LTE.WebAPI.Models;

namespace LTE.WebAPI.Controllers
{
    public class ScenePartController : ApiController
    {
        /// <summary>
        /// 场景划分
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public Result PostScenePart()
        {
            return ScenePartModel.part();
        }
    }
}
