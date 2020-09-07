using LTE.DB;
using LTE.WebAPI.Models;
using System;
using System.Collections;
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
            public string IndexName;
        }
        public class areaShpLayer
        {
            public double minLongitude;
            public double maxLongitude;
            public double minLatitude;
            public double maxLatitude;
            public string type;
        }
        // POST: api/LoadShpLayer
        [HttpPost]
        public Result getShpByCellName([FromBody]shpLayer ob)
        {
            Result res = new Result();
            object shpobj = IbatisHelper.ExecuteQueryForObject("getShpByIndexName", ob.IndexName);
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
        [HttpPost]
        public Result getShpByAreaLonLat([FromBody]areaShpLayer ob)
        {
            Result res = new Result();
            Hashtable ht = new Hashtable();
            ht["minLongitude"] = ob.minLongitude;
            ht["maxLongitude"] = ob.maxLongitude;
            ht["minLatitude"] = ob.minLatitude;
            ht["maxLatitude"] = ob.maxLatitude;
            ht["type"] = ob.type;
            object shpobj = IbatisHelper.ExecuteQueryForDataTable("getAreaShpByLonLat", ht);
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
