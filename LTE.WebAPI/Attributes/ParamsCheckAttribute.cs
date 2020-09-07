using LTE.WebAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Helpers;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace LTE.WebAPI.Attributes
{
    public class ParamsCheckAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            base.OnActionExecuting(actionContext);
            //获取请求参数
            foreach (var item in actionContext.ActionArguments)
            {
                if (item.Value == null)
                {
                    //定义了response就会提前返回response，不会在执行controller
                    var response = actionContext.Response = actionContext.Response ?? new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                    //response.StatusCode = HttpStatusCode.Forbidden;
                    var content = new Result
                    {
                        ok = false,
                        msg = "参数错误",
                        code = "4"
                    };
                    response.Content = new StringContent(Json.Encode(content), Encoding.UTF8, "application/json");
                }
            }
        }

    }
}