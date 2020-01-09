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
    public class PreHandleDTForLoc
    {
        /// <summary>
        /// 设定的干扰源的名字
        /// </summary>
        public string infname { get; set; }
        private DataTable reset;
        public PreHandleDTForLoc()
        {
            reset = new DataTable();
            reset.Columns.Add("ID");
            reset.Columns.Add("x");
            reset.Columns.Add("y");
            reset.Columns.Add("Lon");
            reset.Columns.Add("Lat");
            reset.Columns.Add("RSRP");
            reset.Columns.Add("InfName");
        }
        public Result ComputeInfRSRP()
        {
            DTHandlerModel dt = new DTHandlerModel();
            dt.UpdateDTDataForLoc();

            #region 计算路测数据RSRP入库
            //去除无效数据
            IbatisHelper.ExecuteDelete("deleteNouseDTNoInf");
            IbatisHelper.ExecuteDelete("deleteNouseDTInf");
            DataTable dtnoinf = IbatisHelper.ExecuteQueryForDataTable("getDTNoInf", null);
            DataTable dtinf = IbatisHelper.ExecuteQueryForDataTable("getDTInf", null);
            if (dtinf.Rows.Count < 1 || dtnoinf.Rows.Count < 1)
            {
                return new Result(false, "无路测数据");
            }
            Hashtable htinf = new Hashtable();
            htinf["InfName"] = this.infname;
            IbatisHelper.ExecuteDelete("deleteUINTF",htinf);//删除当前表里该干扰源对应的数据

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
                    double lon1 = Convert.ToDouble(dtinf.Rows[j]["Lon"].ToString());
                    double lat1 = Convert.ToDouble(dtinf.Rows[j]["Lat"].ToString());
                    if (flag[j] || CInoinf != CIinf)
                    {
                        if (CJWDHelper.distance(lon, lat, lon1, lat1) * 1000 < 1)//若距离太近，则该点不要
                        {
                            break;
                        }
                        else
                        {
                            continue;
                        }

                    }
                    else
                    {
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
                            newRow["InfName"] = this.infname;
                            reset.Rows.Add(newRow);
                            flag[j] = true;
                            break;
                        }
                    }
                }
                if (reset.Rows.Count == 5000)
                {
                    DTHandlerModel.writeFinalResultToDB(reset, "tbUINTF");
                }
            }
            dtinf.Clear();
            dtnoinf.Clear();
            #endregion

            #region 计算终端数据入库
            IbatisHelper.ExecuteDelete("deleteNouseTerminalMI", null);
            Hashtable ht = new Hashtable();
            ht["isInf"] = 0;
            //增加终端部分的RSRP处理
            DataTable ternoinf = IbatisHelper.ExecuteQueryForDataTable("getTerminalMI", ht);
            ht["isInf"] = 1;
            DataTable terinf = IbatisHelper.ExecuteQueryForDataTable("getTerminalMI", ht);
            bool[] Tflag = new bool[ternoinf.Rows.Count];
            for (int i = 0; i < ternoinf.Rows.Count; i++)
            {
                double lon = Convert.ToDouble(ternoinf.Rows[i]["Lon"].ToString());
                double lat = Convert.ToDouble(ternoinf.Rows[i]["Lat"].ToString());
                double RSRP1 = Convert.ToDouble(ternoinf.Rows[i]["RSRP"].ToString());
                double SINR1 = Convert.ToDouble(ternoinf.Rows[i]["SINR"].ToString());
                long IMSI = Convert.ToInt64(ternoinf.Rows[i]["IMSI"].ToString());

                for (int j = 0; j < terinf.Rows.Count; j++)
                {
                    long IMSIinf = Convert.ToInt64(terinf.Rows[j]["IMSI"].ToString());
                    double lon1 = Convert.ToDouble(terinf.Rows[j]["Lon"].ToString());
                    double lat1 = Convert.ToDouble(terinf.Rows[j]["Lat"].ToString());
                    if (Tflag[j] || IMSI != IMSIinf)
                    {
                        if (CJWDHelper.distance(lon, lat, lon1, lat1) * 1000 < 1)//若距离太近，则该点不要
                        {
                            break;
                        }
                        else
                        {
                            continue;
                        }

                    }
                    else
                    {
                        double RSRP2 = Convert.ToDouble(terinf.Rows[j]["RSRP"].ToString());
                        double SINR2 = Convert.ToDouble(terinf.Rows[j]["SINR"].ToString());
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
                            newRow["ID"] = i;
                            Point pt = new Point(lon, lat, 0);
                            pt = PointConvertByProj.Instance.GetProjectPoint(pt);
                            newRow["x"] = pt.X;
                            newRow["y"] = pt.Y;
                            newRow["Lon"] = lon;
                            newRow["Lat"] = lat;
                            newRow["RSRP"] = UINTF;
                            newRow["InfName"] = this.infname;
                            reset.Rows.Add(newRow);
                            Tflag[j] = true;
                            break;
                        }
                    }
                }
                if (reset.Rows.Count == 5000)
                {
                    DTHandlerModel.writeFinalResultToDB(reset, "tbUINTF");
                }
            }


            if (reset.Rows.Count > 0)
            {
                DTHandlerModel.writeFinalResultToDB(reset, "tbUINTF");
            }
            #endregion
            return new Result(true, "成功");
        }

    }
    public class DTHandlerModel
    {
        private DataTable dtable;
        private Dictionary<int, List<UseCell>> cellmap;

        public DTHandlerModel()
        {
            InitCellInfo();
            dtable = new System.Data.DataTable();
            dtable.Columns.Add("ID");
            dtable.Columns.Add("x");
            dtable.Columns.Add("y");
            dtable.Columns.Add("eNodeBID");
            dtable.Columns.Add("CellID");
            dtable.Columns.Add("SCell_Dist");
        }
        public Result UpdateDTData()
        {
            IbatisHelper.ExecuteDelete("deleteNousetbDTData", null);//去除无效路测数据
            //获取tbDTData数据，对每一行数据，计算得到需要的内容，然后
            DataTable dtInfo = IbatisHelper.ExecuteQueryForDataTable("SelectTbDTData", null);
            if (!GetTotalintoTmp(dtInfo))
            {
                return new Result(false, "更新失败，请查看日志");
            }
            int count = IbatisHelper.ExecuteUpdate("UpdatetbDTDataByTmp", null);//更新的行数
            IbatisHelper.ExecuteUpdate("UpdatetbDTDataByCell", null);//根据eNodeID和CI更新btsname，cellname
            Debug.WriteLine("TbDTData一共更新：" + count + "条数据");
            return new Result(true,"更新成功");
        }

        /// <summary>
        /// 用于更新用于定位的两个路测表
        /// </summary>
        /// <returns></returns>
        public Result UpdateDTDataForLoc()
        {
            IbatisHelper.ExecuteDelete("deleteNousetbDTInf", null);//去除无效路测数据
            IbatisHelper.ExecuteDelete("deleteNousetbDTNoInf", null);//去除无效路测数据
            
            //获取tbDTData数据，对每一行数据，计算得到需要的内容，然后
            DataTable dtInfo = IbatisHelper.ExecuteQueryForDataTable("SelectTbDTInf", null);
            if (!GetTotalintoTmp(dtInfo))
            {
                return new Result(false, "更新失败，请查看日志");
            }
            int count = IbatisHelper.ExecuteUpdate("UpdatetbDTInfByTmp", null);//更新的行数
            IbatisHelper.ExecuteUpdate("UpdatetbDTInfByCell", null);//根据eNodeID和CI更新btsname，cellname
            Debug.WriteLine("tbDTInf一共更新：" + count + "条数据");

            dtInfo.Clear();
            dtInfo = IbatisHelper.ExecuteQueryForDataTable("SelectTbDTNoInf", null);
            if (!GetTotalintoTmp(dtInfo))
            {
                return new Result(false, "更新失败，请查看日志");
            }
            count = IbatisHelper.ExecuteUpdate("UpdatetbDTNoInfByTmp", null);//更新的行数
            IbatisHelper.ExecuteUpdate("UpdatetbDTNoInfByCell", null);//根据eNodeID和CI更新btsname，cellname
            Debug.WriteLine("tbDTNoInf一共更新：" + count + "条数据");
            return new Result(true, "更新成功");
        }

        private bool GetTotalintoTmp(DataTable dtInfo)
        {
            try
            {
                IbatisHelper.ExecuteDelete("deletetbtmpDTData", null);
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
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.StackTrace);
                return false;
            }
            return true;
        }
        /// <summary>
        /// 写入数据库
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="tbname"></param>
        public static void writeFinalResultToDB(DataTable dt, string tbname)
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
            //cellmap = new Dictionary<int, List<UseCell>>();
            DataTable celldt = IbatisHelper.ExecuteQueryForDataTable("SelectCellInfo", null);
            if(cellmap!=null && cellmap.Count == celldt.Rows.Count)
            {
                Debug.WriteLine("已初始化，小区数："+cellmap.Count);
                return;
            }
            //获取小区表信息，并存储到cellmap中，pci为key
            cellmap = new Dictionary<int, List<UseCell>>();
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