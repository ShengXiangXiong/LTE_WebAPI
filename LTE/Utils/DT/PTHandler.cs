using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LTE.DB;
using System.Diagnostics;
using System.Data.SqlClient;
using LTE.Geometric;
namespace LTE.Utils.DT
{
    class PTHandler
    {
        private Dictionary<int, List<UseCell>> cellmap;//存储所有小区信息

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool handlerCell()
        {
            InitCellInfo();
            DataTable dtable = new DataTable();
            dtable.Columns.Add("ID");
            dtable.Columns.Add("x");
            dtable.Columns.Add("y");
            dtable.Columns.Add("eNodeBID");
            dtable.Columns.Add("CellID");
            dtable.Columns.Add("BtsName");
            dtable.Columns.Add("SCell_Dist");

            //获取tbDTData数据，对每一行数据，计算得到需要的内容，然后
            DataTable dtInfo = IbatisHelper.ExecuteQueryForDataTable("SelectTbDTData", null);
            int ucount = 0;
            foreach (DataRow dtrow in dtInfo.Rows)
            {
                int id = int.Parse(dtrow["ID"].ToString());
                double lon = double.Parse(dtrow["Lon"].ToString());
                double lat = double.Parse(dtrow["Lat"].ToString());
                int pci = int.Parse(dtrow["PCI"].ToString());
                Point pt = new Point(lon, lat, 0);
                pt = PointConvertByProj.Instance.GetProjectPoint(pt);
                //IPoint pt = GeometryUtilities.ConstructPoint3D(lon, lat, 0);
                ////Debug.WriteLine(pt.X + "," + pt.Y);
                //PointConvert.Instance.GetProjectPoint(pt);
                //// Debug.WriteLine(pt.X + "," + pt.Y);

                if (cellmap.ContainsKey(pci))
                {
                    List<UseCell> tmp = new List<UseCell>(cellmap[pci]);
                    double dis = double.MaxValue;
                    int minindex = 0;
                    for (int i = 0; i < tmp.Count; i++)
                    {
                        //Debug.WriteLine("tmp"+i+"   :" + tmp[i].x + "," + tmp[i].y);
                        double curdis = distanceXY(pt.X, pt.Y, tmp[i].x, tmp[i].y);
                        if (curdis < dis)
                        {
                            dis = curdis;
                            minindex = i;
                        }
                    }
                    //找到对应的小区信息，添加到dtable中
                    DataRow dr = dtable.NewRow();
                    dr["ID"] = id;
                    dr["x"] = pt.X;
                    dr["y"] = pt.Y;
                    dr["eNodeBID"] = tmp[minindex].eNodeB;
                    dr["CellID"] = tmp[minindex].cellID;
                    dr["BtsName"] = tmp[minindex].btsname;
                    dr["SCell_Dist"] = dis;
                    dtable.Rows.Add(dr);
                }
                else
                {
                    ucount++;
                    continue;
                }
            }
            //Debug.WriteLine(ucount + "条无效数据");
            writeFinalResultToDB(dtable, "deletetbtmpDTData", "tbtmpDTData");
            int count = IbatisHelper.ExecuteUpdate("UpdatetbDTDataByTmp", null);//更新的行数
            //Debug.WriteLine("一共更新：" + count + "条数据");
            return true;
        }
        double distanceXY(double x, double y, double ex, double ey)
        {
            double deteX = Math.Pow((x - ex), 2);
            double deteY = Math.Pow((y - ey), 2);
            double distance = Math.Sqrt(deteX + deteY);
            return distance;
        }
        void InitCellInfo()
        {
            //获取小区表信息，并存储到cellmap中，pci为key
            cellmap = new Dictionary<int, List<UseCell>>();
            DataTable celldt = IbatisHelper.ExecuteQueryForDataTable("SelectCellInfo", null);

            foreach (DataRow cellRow in celldt.Rows)
            {
                double x = double.Parse(cellRow["x"].ToString());
                double y = double.Parse(cellRow["y"].ToString());
                string btsname = cellRow["BtsName"].ToString();
                int eNodeB = int.Parse(cellRow["eNodeB"].ToString());
                int cellid = int.Parse(cellRow["CellID"].ToString());
                int pci = int.Parse(cellRow["PCI"].ToString());
                UseCell curcell = new UseCell(x, y, btsname, eNodeB, cellid);
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
            //Debug.WriteLine("共有小区数：" + count);
        }

        #region 无用但暂且保留块
        ///// <summary>
        ///// 用于一开始单独测试时小区信息不完整的填充，
        ///// </summary>
        //public void HandlertbCell()
        //{
        //    System.Data.DataTable dtable = new System.Data.DataTable();
        //    dtable.Columns.Add("ID");
        //    dtable.Columns.Add("x");
        //    dtable.Columns.Add("y");

        //    //获取cell经纬度数据，对每一行数据，计算得到需要的内容，然后
        //    DataTable cellInfo = IbatisHelper.ExecuteQueryForDataTable("SelectCellLonLat", null);

        //    foreach (DataRow dtrow in cellInfo.Rows)
        //    {
        //        int id = int.Parse(dtrow["ID"].ToString());
        //        double lon = double.Parse(dtrow["Longitude"].ToString());
        //        double lat = double.Parse(dtrow["Latitude"].ToString());
        //        Point pt = new Point(lon, lat, 0);
        //        pt = PointConvertByProj.Instance.GetProjectPoint(pt);
        //        //IPoint pt = GeometryUtilities.ConstructPoint3D(lon, lat, 0);
        //        ////Debug.WriteLine(pt.X + "," + pt.Y);
        //        //PointConvert.Instance.GetProjectPoint(pt);
        //        //// Debug.WriteLine(pt.X + "," + pt.Y);
        //        DataRow dr = dtable.NewRow();
        //        dr["ID"] = id;
        //        dr["x"] = pt.X;
        //        dr["y"] = pt.Y;
        //        dtable.Rows.Add(dr);

        //    }
        //    writeFinalResultToDB(dtable, "deletetbtmpCell", "tbtmpCell");
        //    int count = IbatisHelper.ExecuteUpdate("UpdatetbCellByTmp", null);//更新的行数
        //    Debug.WriteLine("一共更新：" + count + "条数据");
        //}
        #endregion

        void writeFinalResultToDB(DataTable dt, string deletesqlID, string tbname)
        {
            IbatisHelper.ExecuteDelete(deletesqlID, null);
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
    }

    /// <summary>
    /// 单个小区信息
    /// </summary>
    class UseCell
    {
        public double x;
        public double y;
        public string btsname;
        public int eNodeB;
        public int cellID;
        public UseCell(double lx, double ly, string lbtsname, int leNodeB, int lcellID)
        {
            x = lx;
            y = ly;
            btsname = lbtsname;
            eNodeB = leNodeB;
            cellID = lcellID;
        }
    }
}
