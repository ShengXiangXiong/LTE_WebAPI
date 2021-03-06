﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LTE.WebAPI.Models;
using LTE.WebAPI.Attributes;
namespace LTE.WebAPI.Controllers
{
    public class PointsSelectController : ApiController
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rt"></param>
        /// <returns></returns>
        [HttpPost]
        
        public Result SelectPoints([FromBody]PointsSelectModel rt)
        {
            Result res = rt.GetPointsAuto();
            return res;
        }

        public Result SelectPointsInf([FromBody]PointsSelectModel rt)
        {
            Result res = rt.GetPointsAutoReal();
            return res;
        }

        public Result UpdateSelectPoints([FromBody]UpdateSP rt)
        {
            Result res = rt.UpdateSelectPoints();
            return res;
        }
        // GET api/<controller>
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<controller>/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<controller>
        public void Post([FromBody]string value)
        {
        }

        // PUT api/<controller>/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        public void Delete(int id)
        {
        }
    }
}