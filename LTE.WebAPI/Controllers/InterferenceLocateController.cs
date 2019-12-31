using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LTE.WebAPI.Models;

namespace LTE.WebAPI.Controllers
{
    // 网外干扰定位
    public class InterferenceLocateController : ApiController
    {
        /// <summary>
        /// 基于三条启发式规则逐步压缩干扰区域
        /// </summary>
        /// <param name="inf">初始干扰区域范围</param>
        /// <returns>压缩后的干扰区域范围</returns>
        [HttpPost]
        public Result PostRules([FromBody]InterferenceLocateModel inf)
        {
            return inf.rules();
        }

        /// <summary>
        /// 干扰源候选位置评估
        /// </summary>
        /// <returns>干扰源候选位置</returns>
        [HttpPost]
        public Result PostCandidate()
        {
            return new InterferenceLocateModel().candidate();
        }
    }
}
