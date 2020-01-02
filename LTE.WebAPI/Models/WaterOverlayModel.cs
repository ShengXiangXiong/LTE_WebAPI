using System;
using GisClient;
using System.Data;
using LTE.DB;

namespace LTE.WebAPI.Models
{
    public class WaterOverlayModel
    {
        public Result overlaywater()
        {
            DataTable dt1 = DB.IbatisHelper.ExecuteQueryForDataTable("GetFishnetShpState", null);  // Ibatis 数据访问，判断用户是否做了渔网生成
            if (dt1.Rows[0][0].ToString() == "0")
            { return new Result(false, "用户未进行渔网生成"); }
            //    else
            //    { return new Result(true, "用户未进行渔网生成"); }


            DataTable dt2 = DB.IbatisHelper.ExecuteQueryForDataTable("GetWater_overlayState", null);  // Ibatis 数据访问，判断用户是否做了水面叠加分析，做了则删除它
            if (dt2.Rows[0][0].ToString() == "1")//存在水面叠加图层
            {
                try//更新加速场景表，前提条件表
                {
                    IbatisHelper.ExecuteDelete("UpdatetbDependTableDuetoWater_overlay", null);
                    IbatisHelper.ExecuteDelete("deleteAdjcoefficient", null);
                    IbatisHelper.ExecuteUpdate("UpdatetbAccelerateGridSceneDuetoWater_overlay", null);
                }
                catch (Exception ex)
                { return new Result(false, ex.ToString()); }
            }

            try
            {
                GisClient.Result res = GisClient.ServiceApi.getGisLayerService().overlaywater();
                if (res.Ok)
                {
                    //更新tbDependTabled的Water_overlay
                    IbatisHelper.ExecuteUpdate("UpdatetbDependTableWater_overlay", null);
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
