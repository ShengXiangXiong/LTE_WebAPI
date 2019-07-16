using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.SqlClient;

namespace LTE.WebAPI.Models
{
    public class ScenePartModel
    {
        // 对均匀栅格进行粗略的场景划分，后续应采用张明明的改进方式：通过聚类的方式对场景进行自动划分
        // 主要用于多场景系数校正
        public static Result part()
        {
            #region 之前的场景划分
                //DataTable tb = LTE.DB.IbatisHelper.ExecuteQueryForDataTable("getRays", null);

                //for (int i = 0; i < tb.Rows.Count; i++)
                //{
                //    // 三个场景：y < 3545765，3545765 < y < 3547435，y > 3547435
                //    double y = Convert.ToDouble(tb.Rows[i]["rayStartPointY"]);
                //    if (y < 3545765)
                //        tb.Rows[i]["startPointScen"] = 0;
                //    else if (y > 3545765 && y < 3547435)
                //        tb.Rows[i]["startPointScen"] = 1;
                //    else
                //        tb.Rows[i]["startPointScen"] = 2;

                //    double y1 = Convert.ToDouble(tb.Rows[i]["rayEndPointY"]);
                //    if (y1 < 3545765)
                //        tb.Rows[i]["endPointScen"] = 0;
                //    else if (y1 > 3545765 && y1 < 3547435)
                //        tb.Rows[i]["endPointScen"] = 1;
                //    else
                //        tb.Rows[i]["endPointScen"] = 2;

                //    // 计算穿过场景的比例
                //    double scene1 = 3545765, sceneLen2 = 3547435 - 3545765, scene3 = 3547435;
                //    double yMin = 0, yMax = 0;
                //    if (y < y1)
                //    {
                //        yMin = y;
                //        yMax = y1;
                //    }
                //    else
                //    {
                //        yMin = y1;
                //        yMax = y;
                //    }
                //    double yLen = yMax - yMin;
                //    double proportion1 = 0, proportion2 = 0, proportion3 = 0;
                //    if (yMin < scene1 && yMax > scene3)  // 覆盖3个场景
                //    {
                //        double yLen1 = scene1 - yMin;
                //        double yLen3 = yMax - scene3;
                //        proportion1 = yLen1 / yLen;
                //        proportion2 = sceneLen2 / yLen;
                //        proportion3 = yLen3 / yLen;
                //    }
                //    else if (yMin < scene1 && yMax > scene1 && yMax <= scene3) // 覆盖前2个场景
                //    {
                //        double yLen1 = scene1 - yMin;
                //        double yLen2 = yMax - scene1;
                //        proportion1 = yLen1 / yLen;
                //        proportion2 = yLen2 / yLen;
                //    }
                //    else if (yMin >= scene1 && yMin < scene3 && yMax > scene3) // 覆盖后2个场景
                //    {
                //        double xLen2 = scene3 - yMin;
                //        double xLen3 = yMax - scene3;
                //        proportion2 = xLen2 / yLen;
                //        proportion3 = xLen3 / yLen;
                //    }
                //    else if (yMin < scene1)  // 只覆盖第1个场景
                //    {
                //        proportion1 = 1;
                //    }
                //    else if (yMin >= scene1 && yMax <= scene3)
                //    {
                //        proportion2 = 1;
                //    }
                //    else if (yMax > scene3)
                //    {
                //        proportion3 = 1;
                //    }
                //    tb.Rows[i]["proportion"] = string.Format("{0:N3};{1:N3};{2:N3}", proportion1, proportion2, proportion3);
                //}

                //LTE.DB.IbatisHelper.ExecuteDelete("DeleteRays", null);

                //using (System.Data.SqlClient.SqlBulkCopy bcp = new System.Data.SqlClient.SqlBulkCopy(LTE.DB.DataUtil.ConnectionString))
                //{
                //    bcp.BatchSize = tb.Rows.Count;
                //    bcp.BulkCopyTimeout = 1000;
                //    bcp.DestinationTableName = "tbRayAdj";
                //    bcp.WriteToServer(tb);
                //    bcp.Close();

                //}
                //tb.Clear();
                #endregion

            int maxGxid = 0, maxGyid = 0;
            InternalInterference.Grid.GridHelper.getInstance().getMaxAccGridXY(ref maxGxid, ref maxGyid);

            DataTable tb = new DataTable();
            tb = DB.IbatisHelper.ExecuteQueryForDataTable("getAGridZ", null);
            if(tb.Rows.Count < 1)
            {
                return new Result(false, "无均匀栅格！");
            }

            int minGzid = Convert.ToInt32(tb.Rows[0][0]);
            int maxGzid = Convert.ToInt32(tb.Rows[0][1]);

            try
            {
                DB.IbatisHelper.ExecuteDelete("DeleteAccrelateGridScene", null);
            }
            catch(Exception e)
            {
                return new Result(false, e.ToString());
            }

            System.Data.DataTable dtable = new System.Data.DataTable();
            dtable.Columns.Add("GXID");
            dtable.Columns.Add("GYID");
            dtable.Columns.Add("GZID");
            dtable.Columns.Add("Scene");

            double oX = 0, oY = 0;
            InternalInterference.Grid.GridHelper.getInstance().getOriginXY(ref oX, ref oY);
            double len = InternalInterference.Grid.GridHelper.getInstance().getAGridSize();

            // 三个场景：y < 3545765，3545765 < y < 3547435，y > 3547435
            int scen1 = (int)((3545765 - oY) / len);
            int scen2 = (int)((3547435 - oY) / len);

            for (int x = 0; x <= maxGxid; x++)
            {
                for (int y = 0; y <= maxGyid; y++)
                {
                    for (int z = minGzid; z <= maxGzid; z++)
                    {
                        System.Data.DataRow thisrow = dtable.NewRow();
                        thisrow["GXID"] = x;
                        thisrow["GYID"] = y;
                        thisrow["GZID"] = z;

                        if (y < scen1)
                            thisrow["Scene"] = (byte)0;
                        else if (y < scen2)
                            thisrow["Scene"] = (byte)1;
                        else
                            thisrow["Scene"] = (byte)2;

                        dtable.Rows.Add(thisrow);
                    }
                }

                if (dtable.Rows.Count > 50000)
                {
                    using (System.Data.SqlClient.SqlBulkCopy bcp = new System.Data.SqlClient.SqlBulkCopy(DB.DataUtil.ConnectionString))
                    {
                        bcp.BatchSize = dtable.Rows.Count;
                        bcp.BulkCopyTimeout = 1000;
                        bcp.DestinationTableName = "tbAccelerateGridScene";
                        bcp.WriteToServer(dtable);
                        bcp.Close();
                    }
                    dtable.Clear();
                }
            }

            // 最后一批
            if (dtable.Rows.Count > 0)
            {
                using (System.Data.SqlClient.SqlBulkCopy bcp = new System.Data.SqlClient.SqlBulkCopy(DB.DataUtil.ConnectionString))
                {
                    bcp.BatchSize = dtable.Rows.Count;
                    bcp.BulkCopyTimeout = 1000;
                    bcp.DestinationTableName = "tbAccelerateGridScene";
                    bcp.WriteToServer(dtable);
                    bcp.Close();
                }
                dtable.Clear();
            }

            return new Result(true);
        }
    }
}