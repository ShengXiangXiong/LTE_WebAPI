using LTE.WebAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace LTE.WebAPI.Controllers
{
    public class RankLocController : ApiController
    {
        [AllowAnonymous]
        [HttpPost]
        public Result LocateAnalysis([FromBody]RankModel pa)
        {
            return pa.LocateByPath();
        }

        // GET: api/RankLoc
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/RankLoc/5
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/RankLoc
        public void Post([FromBody]string value)
        {
        }

        // PUT: api/RankLoc/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/RankLoc/5
        public void Delete(int id)
        {
        }
    }
}
