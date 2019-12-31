using System;
using GisClient;
using System.Data;
using LTE.DB;

namespace LTE.WebAPI.Models
{
    public class ClusterShpModel
    {
        public Result cluster()
        {
            DataTable dt11 = DB.IbatisHelper.ExecuteQueryForDataTable("GetClustertoDBState", null);  // Ibatis 数据访问，判断用户是否做了场景划分
            if (dt11.Rows[0][0].ToString() == "0")
            { return new Result(false, "用户未进行场景划分"); }
            

            DataTable dt2 = DB.IbatisHelper.ExecuteQueryForDataTable("GetClusterShpState", null);  // Ibatis 数据访问，判断用户是否做了结果图层
            if (dt2.Rows[0][0].ToString() == "1")//做了结果图层
            {
                try//更新前提条件表
                {
                    IbatisHelper.ExecuteDelete("UpdatetbDependTableDuetoClusterShp", null);
                }
                catch (Exception ex)
                { return new Result(false, ex.ToString()); }
            }

            try
            {
                GisClient.Result res = GisClient.ServiceApi.getGisLayerService().cluster();
                if (res.Ok)
                {
                    //更新tbDependTabled的Grass_overlay
                    IbatisHelper.ExecuteUpdate("UpdatetbDependTableClusterShp", null);
                    return new Result(true, res.Msg);
                }
                else
                {
                    return new Result(false, res.Msg);
                }
            }
            catch (Exception e)
            {
                return new Result(false, "远程调用失败" + e);
            }
            finally
            {
                ServiceApi.CloseConn();
            }
        }
    }
}