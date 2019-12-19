using LTE.Model;
using LTE.WebAPI.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace LTE.WebAPI.Attributes
{
    public class TaskLoadInfoAttribute: ActionFilterAttribute
    {
        public string taskName { get; set; }
        public TaskType type { get; set; }
        private LoadInfo loadInfo = new LoadInfo();

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            bool layer = true;
            //string taskName = typeof(TaskType).GetEnumName(type);
            string taskName1 = taskName;
            taskName1 += "_";
            switch (type)
            {
                case TaskType.CellCoverLayer:
                    var obj = (RefreshCellCoverLayerModel)actionContext.ActionArguments["layer"];
                    taskName1 += obj.cellName;
                    break;
                case TaskType.AreaCoverLayer:
                    var obj1 = (RefreshAreaCoverLayerModel)actionContext.ActionArguments["layer"];
                    taskName1 += String.Format("{0}_{1}_{2}_{3}", obj1.minLongitude, obj1.minLatitude, obj1.maxLongitude, obj1.maxLatitude);
                    break;
                case TaskType.AreaGSMLayer:
                    taskName1 += "南京";
                    break;
                case TaskType.CellCoverCompu:
                    var obj2 = (CellRayTracingModel)actionContext.ActionArguments["rt"];
                    taskName1 += obj2.cellName;
                    layer = false;
                    break;
                case TaskType.AreaCoverCompu:
                    var obj3 = (Area)actionContext.ActionArguments["area"];
                    layer = false;
                    taskName1 += String.Format("{0}_{1}_{2}_{3}", obj3.minLongitude, obj3.minLatitude, obj3.maxLongitude, obj3.maxLatitude);
                    break;
                case TaskType.AreaInterference:
                    var obj4 = (AreaCoverDefectModel)actionContext.ActionArguments["defect"];
                    layer = false;
                    taskName1 += String.Format("{0}_{1}_{2}_{3}", obj4.minLongitude, obj4.minLatitude, obj4.maxLongitude, obj4.maxLatitude);
                    break;
                case TaskType.RayRecordAdj:
                    var obj5 = (RayRecordAdjModel)actionContext.ActionArguments["ray"];
                    layer = false;
                    taskName1 += obj5.cellName;
                    break;
                case TaskType.RayRecordLoc:
                    var obj6 = (RayLocRecordModel)actionContext.ActionArguments["ray"];
                    layer = false;
                    taskName1 += obj6.virsource;
                    break;
                default:
                    break;
            }
            LoadInfo.taskName.Value = taskName1;
            //提前通知远程系统的初始化进度信息
            if (layer)
            {
                GisClient.Result res = GisClient.ServiceApi.getGisLayerService().setLoadInfo(LoadInfo.UserId.Value, taskName1);
            }
            loadInfo.loadCreate();
            ////清空AOP的描述型字段值，否则它会一直保持，导致下一次请求带有上一次的描述值
            //taskName = null;
        }
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            string executeResult = GetResponseValues(actionExecutedContext);
            Result res = JsonConvert.DeserializeObject<Result>(executeResult);
            if (res.ok)
            {
                loadInfo.loadFinish();
            }
            else
            {
                loadInfo.loadBreakDown();
            }

        }
        /// <summary>
        /// 读取action返回的result
        /// </summary>
        /// <param name="actionExecutedContext"></param>
        /// <returns></returns>
        public string GetResponseValues(HttpActionExecutedContext actionExecutedContext)
        {
            Stream stream = actionExecutedContext.Response.Content.ReadAsStreamAsync().Result;
            Encoding encoding = Encoding.UTF8;
            /*
            这个StreamReader不能关闭，也不能dispose， 关了就傻逼了
            因为你关掉后，后面的管道  或拦截器就没办法读取了
            */
            var reader = new StreamReader(stream, encoding);
            string result = reader.ReadToEnd();
            /*
            这里也要注意：   stream.Position = 0; 
            当你读取完之后必须把stream的位置设为开始
            因为request和response读取完以后Position到最后一个位置，交给下一个方法处理的时候就会读不到内容了。
            */
            stream.Position = 0;
            return result;
        }
    }
}