using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LTE.WebAPI.Models;

namespace LTE.WebAPI.Controllers
{
    public class DTdataController : ApiController
    {
        /// <summary>
        /// 删除虚拟路测
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Result DeleteVDT()
        {
            return DTdataModel.deleteVDT();
        }

        /// <summary>
        /// 生成虚拟路测
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public Result PostAddVDT()
        {
            return DTdataModel.addVDT();
        }

        /// <summary>
        /// 真实路测预处理
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public Result PostPreDT()
        {
            return DTdataModel.preDTdata();
        }

    }
}
