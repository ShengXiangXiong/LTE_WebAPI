using LTE.Model;
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
    public class LoadingController : ApiController
    {
        LoadInfo loadInfo = new LoadInfo();
        [HttpPost]
        public Result getLoadingInfo()
        {
            return new Result(true, "", loadInfo.GetLoadInfos());
        }
        //[HttpPost]
        //[AllowAnonymous]
        //public void updateLoadingInfo(LoadInfo loadInfo)
        //{
        //    loading.updateLoading(loadInfo);
        //}
        //[HttpPost]
        //[AllowAnonymous]
        //public void addCountByMulti(LoadInfo loadInfo)
        //{
        //    loading.addCount(loadInfo);
        //}

    }
}
