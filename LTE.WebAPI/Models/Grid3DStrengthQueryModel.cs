using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Collections;

namespace LTE.WebAPI.Models
{
    public class Grid3DStrengthQueryModel 
    {
        /// <summary>
        /// 网格 x 坐标
        /// </summary>
        public int gxid { get; set; }

        /// <summary>
        /// 网格 y 坐标
        /// </summary>
        public int gyid { get; set; }

        /// <summary>
        /// 因无法呈现 3D 覆盖，因此给定栅格二维坐标，查询建筑物高度内的所有栅格场强
        /// </summary>
        /// <param name="rt">栅格二维坐标</param>
        /// <returns></returns>
        public Result query()
        {
            Hashtable para = new Hashtable();
            para["gxid"] = this.gxid;
            para["gyid"] = this.gyid;

            DataTable tb = DB.IbatisHelper.ExecuteQueryForDataTable("GetGrid3DStrength", para);
            if (tb.Rows.Count < 1)
            {
                return new Result(false, "数据为空！");
            }

            System.Text.StringBuilder msg = new System.Text.StringBuilder();
            msg.Append("GXID\tGYID\tGZID\t场强\n");
            for (int i = 0; i < tb.Rows.Count; i++ )
            {
                msg.Append(string.Format("{0}\t{1}\t{2}\t{3}\n", Convert.ToInt32(tb.Rows[i]["GXID"]),
                    Convert.ToInt32(tb.Rows[i]["GYID"]), Convert.ToInt32(tb.Rows[i]["level"]), 
                    Convert.ToDouble(tb.Rows[i]["ReceivedPowerdbm"])));
            }

            return new Result(true, msg.ToString());
        }
    }
}
