using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LTE.WebAPI.Models;
using LTE.WebAPI.Attributes;
using System.Text;
using System.IO;

namespace LTE.WebAPI.Controllers
{
    public class FishnetController : ApiController
    {
        //public static  int a = 0;
        /// <summary>
        /// 生成渔网
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [ApiAuthorize(Roles = "admin")]
        [TaskLoadInfo(taskName = "渔网生成", type = TaskType.Fishnet)]
        public Result PostFishnet()
        {
            // if (a == 0)
            // {
            //     a++;
            //     Result res = new FishnetModel().makeFishnet();
            //     a--;
            //     return res;
            //  return new Result(true,"11111");
            // }
            //  else
            // { return null; }
            return new FishnetModel().makeFishnet();
        }
    }
}
