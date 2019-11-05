using LTE.DB;
using LTE.WebAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace LTE.WebAPI.Controllers
{
    public class LoadShpLayerController : ApiController
    {
        public class shpLayer
        {
            public string CellName;
        }
        // POST: api/LoadShpLayer
        [HttpPost]
        public Result getShpByCellName([FromBody]shpLayer ob)
        {
            Result res = new Result();
            object shpobj = IbatisHelper.ExecuteQueryForObject("getShpByCellName", ob.CellName);
            if (shpobj != null)
            {
                res.ok = true;
                res.obj = shpobj;
                res.code = "1";
            }
            else
            {
                res.ok = false;
                res.msg = "资源不存在,请联系管理员";
                res.code = "3";
            }
            return res;
        }

    }
}
