using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LTE.DB;
using System.Data;
using System.Collections;
using LTE.InternalInterference;
using LTE.Utils;
using LTE.Geometric;
using System.Data.SqlClient;
using System.Diagnostics;
namespace LTE.WebAPI.Models
{
   
    public class DTHandlerModel
    {
        private Dictionary<int, List<UseCell>> cellmap;
        public  Result ComputeInfRSRP()
        {
            DataTable reset = new DataTable();
            reset.Columns.Add("ID");
            reset.Columns.Add("x");
            reset.Columns.Add("y");
            reset.Columns.Add("Lon");
            reset.Columns.Add("Lat");
            reset.Columns.Add("RSRP");
            //去除无效数据
            IbatisHelper.ExecuteDelete("deleteNouseDTNoInf");
            IbatisHelper.ExecuteDelete("deleteNouseDTInf");
            DataTable dtnoinf = IbatisHelper.ExecuteQueryForDataTable("getDTNoInf", null);
            DataTable dtinf = IbatisHelper.ExecuteQueryForDataTable("getDTInf", null);
            if (dtinf.Rows.Count < 1 || dtnoinf.Rows.Count < 1)
            {
                return new Result(false, "无路测数据");
            }
            IbatisHelper.ExecuteDelete("deleteUINTF");
            bool[] flag = new bool[dtinf.Rows.Count];
            for (int i = 0; i < dtnoinf.Rows.Count; i++)
            {
                double lon = Convert.ToDouble(dtnoinf.Rows[i]["Lon"].ToString());
                double lat = Convert.ToDouble(dtnoinf.Rows[i]["Lat"].ToString());
                double RSRP1 = Convert.ToDouble(dtnoinf.Rows[i]["RSRP"].ToString());
                double SINR1 = Convert.ToDouble(dtnoinf.Rows[i]["SINR"].ToString());
                int CInoinf = Convert.ToInt32(dtnoinf.Rows[i]["CI"].ToString());
                int ID = Convert.ToInt32(dtnoinf.Rows[i]["ID"].ToString());
                for (int j = 0; j < dtinf.Rows.Count; j++)
                {
                    int CIinf = Convert.ToInt32(dtinf.Rows[j]["CI"].ToString());
                    if (flag[j] || CInoinf != CIinf)
                    {
                        continue;
                    }
                    else
                    {
                        double lon1 = Convert.ToDouble(dtinf.Rows[j]["Lon"].ToString());
                        double lat1 = Convert.ToDouble(dtinf.Rows[j]["Lat"].ToString());
                        double RSRP2 = Convert.ToDouble(dtinf.Rows[j]["RSRP"].ToString());
                        double SINR2 = Convert.ToDouble(dtinf.Rows[j]["SINR"].ToString());
                        if (CJWDHelper.distance(lon, lat, lon1, lat1) * 1000 <= 9)
                        {
                            double tmp = (Math.Pow(10, RSRP2 / 10) / Math.Pow(10, SINR2 / 10))
                                - (Math.Pow(10, RSRP1 / 10) / Math.Pow(10, SINR1 / 10));
                            if (tmp <= 0)
                            {
                                continue;
                            }
                            double UINTF = 10 * Math.Log10(tmp);
                            DataRow newRow = reset.NewRow();
                            newRow["ID"] = ID;
                            Point pt = new Point(lon, lat, 0);
                            pt = PointConvertByProj.Instance.GetProjectPoint(pt);
                            newRow["x"] = pt.X;
                            newRow["y"] = pt.Y;
                            newRow["Lon"] = lon;
                            newRow["Lat"] = lat;
                            newRow["RSRP"] = UINTF;
                            reset.Rows.Add(newRow);
                            flag[j] = true;
                            break;
                        }
                    }
                }
                if (reset.Rows.Count == 5000)
                {
                    writeFinalResultToDB(reset, "tbUINTF");
                }
                
            }
            if (reset.Rows.Count > 0)
            {
                writeFinalResultToDB(reset, "tbUINTF");
            }
            return new Result(true, "成功");
        }

        
        public Result UpdateDTData()
        {
            InitCellInfo();

            System.Data.DataTable dtable = new System.Data.DataTable();
            dtable.Columns.Add("ID");
            dtable.Columns.Add("x");
            dtable.Columns.Add("y");
            dtable.Columns.Add("eNodeBID");
            dtable.Columns.Add("CellID");
            dtable.Columns.Add("SCell_Dist");
            IbatisHelper.ExecuteDelete("deleteNousetbDTData", null);//去除无效路测数据
            IbatisHelper.ExecuteDelete("deletetbtmpDTData", null);
            //获取tbDTData数据，对每一行数据，计算得到需要的内容，然后
            DataTable dtInfo = IbatisHelper.ExecuteQueryForDataTable("SelectTbDTData", null);
            int ucount = 0;
            foreach (DataRow dtrow in dtInfo.Rows)
            {
                int id = int.Parse(dtrow["ID"].ToString());
                double lon = double.Parse(dtrow["Lon"].ToString());
                double lat = double.Parse(dtrow["Lat"].ToString());
                int pci = int.Parse(dtrow["PCI"].ToString());

                if (cellmap.ContainsKey(pci))
                {
                    List<UseCell> tmp = new List<UseCell>(cellmap[pci]);
                    double dis = double.MaxValue;
                    int minindex = 0;
                    for (int i = 0; i < tmp.Count; i++)
                    {
                        //Debug.WriteLine("tmp"+i+"   :" + tmp[i].x + "," + tmp[i].y);
                        double curdis = CJWDHelper.distance(lon, lat, tmp[i].lon, tmp[i].lat) * 1000;
                        if (curdis < dis)
                        {
                            dis = curdis;
                            minindex = i;
                        }
                        if (dis < 0.5)
                        {
                            Debug.WriteLine("pt.x");
                        }
                    }
                    //找到对应的小区信息，添加到dtable中
                    Point pt = new Point(lon, lat, 0);
                    pt = PointConvertByProj.Instance.GetProjectPoint(pt);
                    DataRow dr = dtable.NewRow();
                    dr["ID"] = id;
                    dr["x"] = pt.X;
                    dr["y"] = pt.Y;
                    dr["eNodeBID"] = tmp[minindex].eNodeB;
                    dr["CellID"] = tmp[minindex].cellID;
                    dr["SCell_Dist"] = dis;
                    dtable.Rows.Add(dr);
                    if (dtable.Rows.Count > 5000)
                    {
                        writeFinalResultToDB(dtable, "tbtmpDTData");
                    }
                }
                else
                {
                    ucount++;
                    continue;
                }
            }
            Debug.WriteLine(ucount + "条无效数据");
            writeFinalResultToDB(dtable, "tbtmpDTData");
            int count = IbatisHelper.ExecuteUpdate("UpdatetbDTDataByTmp", null);//更新的行数
            IbatisHelper.ExecuteUpdate("UpdatetbDTDataByCell", null);//根据eNodeID和CI更新btsname，cellname
            Debug.WriteLine("一共更新：" + count + "条数据");
            return new Result(true,"更新成功");
        }

        /// <summary>
        /// 写入数据库
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="tbname"></param>
        private void writeFinalResultToDB(DataTable dt, string tbname)
        {

            using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
            {
                bcp.BatchSize = 5000;
                bcp.BulkCopyTimeout = 1000;
                bcp.DestinationTableName = tbname;
                bcp.WriteToServer(dt);
                bcp.Close();
            }
            dt.Clear();
        }

        /// <summary>
        /// 获取小区信息
        /// </summary>
        void InitCellInfo()
        {
            //获取小区表信息，并存储到cellmap中，pci为key
            cellmap = new Dictionary<int, List<UseCell>>();
            DataTable celldt = IbatisHelper.ExecuteQueryForDataTable("SelectCellInfo", null);

            foreach (DataRow cellRow in celldt.Rows)
            {
                double lon = double.Parse(cellRow["Longitude"].ToString());
                double lat = double.Parse(cellRow["Latitude"].ToString());
                int eNodeB = int.Parse(cellRow["eNodeB"].ToString());
                int cellid = int.Parse(cellRow["CellID"].ToString());
                int pci = int.Parse(cellRow["PCI"].ToString());
                UseCell curcell = new UseCell(lon, lat, eNodeB, cellid);
                if (cellmap.ContainsKey(pci))
                {
                    List<UseCell> list = new List<UseCell>(cellmap[pci]);
                    cellmap.Remove(pci);
                    list.Add(curcell);
                    cellmap.Add(pci, list);
                }
                else
                {
                    List<UseCell> list = new List<UseCell>();
                    list.Add(curcell);
                    cellmap.Add(pci, list);
                }
            }

            //验证
            int count = 0;
            foreach (KeyValuePair<int, List<UseCell>> kv in cellmap)
            {
                count += kv.Value.Count;

            }
            Debug.WriteLine("共有小区数：" + count);
        }
       
    }
    class UseCell
    {
        public double lon;
        public double lat;
        public int eNodeB;
        public int cellID;
        public UseCell(double llon, double llat, int leNodeB, int lcellID)
        {
            lon = llon;
            lat = llat;
            eNodeB = leNodeB;
            cellID = lcellID;
        }
    }
}