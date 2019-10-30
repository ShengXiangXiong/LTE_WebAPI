using LTE.Model;
using LTE.WebAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace LTE.WebAPI.Controllers
{
    public class LoadingController : ApiController
    {
        private Loading loading = Loading.getInstance();
        [HttpPost]
        public Result getLoadingInfo(int UserId)
        {
            if (loading.getLoadInfo()[UserId] != null)
            {
                return new Result(true, "", loading.getLoadInfo()[UserId]);
            }
            else
            {
                return new Result(false, "任务不存在");
            }
        }
        [HttpPost]
        [AllowAnonymous]
        public void updateLoadingInfo(LoadInfo loadInfo)
        {
            loading.updateLoading(loadInfo);
        }
        [HttpPost]
        [AllowAnonymous]
        public void addCountByMulti(LoadInfo loadInfo)
        {
            loading.addCount(loadInfo);
        }

    }
}
