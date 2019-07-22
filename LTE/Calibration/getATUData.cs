using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using LTE.Geometric;

using LTE.GIS;
using LTE.DB;
using ESRI.ArcGIS.Geometry;
using LTE.InternalInterference.Grid;
using LTE.InternalInterference;

using System.IO;
using Excel = Microsoft.Office.Interop.Excel;

namespace LTE.Calibration
{
    //ATU数据预处理
    public class getATUData
    {
        //得到预处理后的ATU数据，存入atu中
        public void getPreAtu(ref Dictionary<string, AtuInfo> atu)
        {
            DataTable tb = new DataTable();
            tb = IbatisHelper.ExecuteQueryForDataTable("getAtuData", null);

            if (tb.Rows.Count < 1)
            {
                return;
            }
            else
            {
                for (int i = 0; i < tb.Rows.Count; i++)
                {
                    string cellID = Convert.ToString(tb.Rows[i]["cellID"].ToString());
                    double rsrp = Convert.ToDouble(tb.Rows[i]["RSRP"].ToString());
                    double longitude = Convert.ToDouble(tb.Rows[i]["Longitude"].ToString());
                    double latitude = Convert.ToDouble(tb.Rows[i]["Latitude"].ToString());

                    int gxid = 0;
                    int gyid = 0;

                    //根据路测数据的经纬度找到所在栅格坐标
                    if (GridHelper.getInstance().LngLatToGGrid(longitude, latitude, ref gxid, ref gyid))
                    {

                        string key = string.Format("{0},{1}", gxid, gyid);
                        if (atu.Keys.Contains(key))
                        {
                            if (atu[key].CellATU.Keys.Contains(cellID))
                            {
                                atu[key].CellATU[cellID].CellID = cellID;
                                atu[key].CellATU[cellID].AllRsrp += rsrp;
                                atu[key].CellATU[cellID].NumofAtu++;
                                if (rsrp > atu[key].CellATU[cellID].maxRSRP)
                                {
                                    atu[key].CellATU[cellID].maxRSRP = rsrp;
                                }
                                atu[key].CellATU[cellID].avgRSRP = atu[key].CellATU[cellID].AllRsrp / atu[key].CellATU[cellID].NumofAtu;
                            }
                            else
                            {
                                cellAtuInfo ca = new cellAtuInfo();
                                ca.CellID = cellID;
                                ca.AllRsrp += rsrp;
                                ca.NumofAtu = 1;
                                ca.maxRSRP = rsrp;
                                ca.avgRSRP = ca.AllRsrp / ca.NumofAtu;
                                atu[key].CellATU[cellID] = ca;

                            }
                        }
                        else
                        {
                            AtuInfo ai = new AtuInfo();
                            ai.gxid = gxid;
                            ai.gyid = gyid;
                            ai.CellATU = new Dictionary<string, cellAtuInfo>();
                            cellAtuInfo ca = new cellAtuInfo();
                            ca.CellID = cellID;
                            ca.AllRsrp = rsrp;
                            ca.maxRSRP = rsrp;
                            ca.NumofAtu = 1;
                            ca.avgRSRP = ca.AllRsrp / ca.NumofAtu;
                            if (ca != null)
                            {
                                ai.CellATU[cellID] = ca;
                            }
                            atu[key] = ai;

                        }
                    }

                }
            }

        }

        // 将atu中存放的数据存到数据库中
        public void ATUData2SQL(ref Dictionary<string, AtuInfo> atu)
        {
            System.Data.DataTable dtable = new System.Data.DataTable();
            dtable.Columns.Add("GXID");
            dtable.Columns.Add("GYID");
            dtable.Columns.Add("CELLID");
            dtable.Columns.Add("AllRsrp");
            dtable.Columns.Add("maxRSRP");
            dtable.Columns.Add("NumofAtu");
            dtable.Columns.Add("avgRSRP");
            dtable.Columns.Add("Scen");

            foreach (string key in atu.Keys)
            {
                foreach (string index in atu[key].CellATU.Keys)
                {
                    System.Data.DataRow thisrow = dtable.NewRow();
                    thisrow["GXID"] = atu[key].gxid;
                    thisrow["GYID"] = atu[key].gyid;
                    thisrow["CELLID"] = atu[key].CellATU[index].CellID;
                    thisrow["AllRsrp"] = atu[key].CellATU[index].AllRsrp;
                    thisrow["maxRSRP"] = atu[key].CellATU[index].maxRSRP;
                    thisrow["NumofAtu"] = atu[key].CellATU[index].NumofAtu;
                    thisrow["avgRSRP"] = atu[key].CellATU[index].avgRSRP;
                    if (Convert.ToInt32(atu[key].gxid) < 305)
                    {
                        thisrow["Scen"] = 1;
                    }
                    else if (Convert.ToInt32(atu[key].gxid) >= 305 && atu[key].gxid < 602)
                    {
                        thisrow["Scen"] = 2;
                    }
                    else
                        thisrow["Scen"] = 3;

                    dtable.Rows.Add(thisrow);

                }

                if (dtable.Rows.Count > 50000)
                {
                    using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                    {
                        bcp.BatchSize = dtable.Rows.Count;
                        bcp.BulkCopyTimeout = 1000;
                        bcp.DestinationTableName = "tbPreATU";
                        bcp.WriteToServer(dtable);
                        bcp.Close();
                    }
                    dtable.Clear();
                }
            }

            using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
            {
                bcp.BatchSize = dtable.Rows.Count;
                bcp.BulkCopyTimeout = 1000;
                bcp.DestinationTableName = "tbPreATU";
                bcp.WriteToServer(dtable);
                bcp.Close();
            }
            dtable.Clear();
        }

    }
}
