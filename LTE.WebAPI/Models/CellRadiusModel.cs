using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LTE.DB;
using LTE.Geometric;
using System.Data;
using System.IO;
using System.Data.SqlClient;
using System.Collections;
using LTE.Model;

namespace LTE.WebAPI.Models
{
    // 小区理论覆盖半径计算
    public class CellRadiusModel
    {
        public Result calcRadius()
        {
            #region 读数据
            //DataTable tb = IbatisHelper.ExecuteQueryForDataTable("getAllCells", null);
            //if(tb.Rows.Count < 1)
            //    return new Result(false, "无小区数据");
            //List<Cell> cells = new List<Cell>();
            //Dictionary<string, int> cellCnt = new Dictionary<string, int>();

            //foreach (DataRow dataRow in tb.Rows)
            //{
            //    Cell cell = new Cell();
            //    cell.BtsName = dataRow["BtsName"].ToString();

            //    if (cellCnt.ContainsKey(cell.BtsName))
            //    {
            //        cellCnt[cell.BtsName]++;
            //    }
            //    else
            //        cellCnt[cell.BtsName] = 1;

            //    cell.id = int.Parse(dataRow["id"].ToString());
            //    cell.CellName = dataRow["CellName"].ToString();
            //    cell.Longitude = double.Parse(dataRow["Longitude"].ToString());
            //    cell.Latitude = double.Parse(dataRow["Latitude"].ToString());
            //    cell.Altitude = float.Parse(dataRow["Altitude"].ToString());
            //    cell.x = float.Parse(dataRow["x"].ToString());
            //    cell.y = float.Parse(dataRow["y"].ToString());
            //    cell.AntHeight = float.Parse(dataRow["AntHeight"].ToString());
            //    cell.Azimuth = float.Parse(dataRow["Azimuth"].ToString());
            //    cell.MechTilt = float.Parse(dataRow["MechTilt"].ToString());
            //    cell.ElecTilt = float.Parse(dataRow["ElecTilt"].ToString());
            //    cell.Tilt = float.Parse(dataRow["Tilt"].ToString());
            //    cell.FeederLength = float.Parse(dataRow["FeederLength"].ToString());
            //    cell.EIRP = float.Parse(dataRow["EIRP"].ToString());
            //    cell.PathlossMode = dataRow["PathlossMode"].ToString();
            //    cell.CoverageType = dataRow["CoverageType"].ToString();
            //    cell.NetType = dataRow["NetType"].ToString();
            //    cell.Comments = dataRow["Comments"].ToString();
            //    cell.eNodeB = int.Parse(dataRow["eNodeB"].ToString());
            //    cell.CI = int.Parse(dataRow["CI"].ToString());
            //    cell.CellNameChs = dataRow["CellNameChs"].ToString();
            //    cell.EARFCN = int.Parse(dataRow["EARFCN"].ToString());
            //    cell.PCI = int.Parse(dataRow["PCI"].ToString());

            //    cells.Add(cell);
            //}
            #endregion

            # region 计算理论覆盖半径
            ///*
            //StreamWriter sw = File.CreateText("cellRadius.txt");
            //StreamWriter sw1 = File.CreateText("nearCellRadius.txt");

            IList<CELL> cells = IbatisHelper.ExecuteQueryForList<CELL>("CELL_SelectAll", null);
            Dictionary<string, int> cellCnt = new Dictionary<string, int>();
            Dictionary<string, double> radius = new Dictionary<string, double>();
            foreach(CELL a in cells)
            {
                //if(cellCnt[a.BtsName])
                if (cellCnt.ContainsKey(a.BtsName))
                {
                    cellCnt[a.BtsName]++;
                }
                else
                {
                    cellCnt[a.BtsName] = 1;
                }
            }
            IList<CELL> res = new List<CELL>();
            for (int i = 0; i < cells.Count; i++)
            {
                //if (radius.ContainsKey(cells[i].BtsName))
                //{
                //    cells[i].CoverageRadius = radius[cells[i].BtsName];
                //    continue;
                //}
                //else
                {
                    double angle = 0; // 覆盖角度
                    if (cellCnt[cells[i].BtsName] == 1)
                        angle = 360;
                    else if (cellCnt[cells[i].BtsName] == 2)
                        angle = 180;
                    else  //当扇区数大于2时，小区的覆盖角度由公式计算得出
                        angle = 1.35 * 360 / cellCnt[cells[i].BtsName];

                    // 计算其最近N个邻区的平均距离、计算最近邻区距离
                    double minDis = 0;

                    double avgDis = nearCellCovR(10000, ref cells, i, 4, 1, ref minDis);//, sw1);

                    double antVAngle = getVAngle("", cells[i].Tilt.Value); //记录垂直功率角

                    double upper = 0, main = 0, lower = 0; //记录算出来的覆盖半径外，覆盖半径中，覆盖半径内的值

                    //fix bug when the cell's altitude is null due to out of range
                    if(cells[i].Altitude is null) { cells[i].Altitude = 0; }

                    cmptCovR(Convert.ToDouble(cells[i].AntHeight + cells[i].Altitude), cells[i].Tilt.Value, antVAngle, ref upper, ref main, ref lower); //返回计算出来的值

                    double outCellCover, inCellCover;
                    if (upper == Double.MaxValue)
                    {
                        outCellCover = -1;
                    }
                    else
                    {
                        outCellCover = upper;
                    }
                    if (main == Double.MaxValue)
                    {
                        inCellCover = -1;
                    }
                    else
                    {
                        inCellCover = main;
                    }

                    cells[i].CoverageRadius = (float)cmptRadius(cells[i].Tilt.Value, cells[i].AntHeight.Value + cells[i].Altitude.Value, outCellCover, inCellCover, minDis, avgDis);
                    radius[cells[i].BtsName] = cells[i].CoverageRadius.Value;
                    res.Add(cells[i]);
                    //sw.WriteLine("{0} {1} {2}", cells[i].id, cells[i].BtsName, cells[i].CoverageRadius);
                    //sw.WriteLine("{0}", (int)cells[i].CoverageRadius);
                    if (res.Count > 100)
                    {
                        IbatisHelper.ExecuteUpdate("CELLBatchUpdateCoverageRadius", res);
                        res.Clear();
                    }
                }
            }
            //sw.Close();
            //sw1.Close();
            //*/
            #endregion

            #region 计算每个小区覆盖方向的180度范围内，最近5个小区的距离、平均距离
            /*
            StreamWriter sw3 = File.CreateText("c:/3.txt");
            StreamWriter sw4 = File.CreateText("c:/4.txt");
            Dictionary<string, double> radius1 = new Dictionary<string, double>();

            for (int i = 0; i < cells.Count; i++)
            {
                if (radius1.ContainsKey(cells[i].BtsName))
                {
                    continue;
                }
                else
                {
                    double avgDis = inCoverCells(ref cells, i, 5, sw3);
                    radius1[cells[i].BtsName] = avgDis;

                    //sw.WriteLine("{0} {1} {2}", cells[i].id, cells[i].BtsName, cells[i].CoverageRadius);
                    sw4.WriteLine("{0}", (int)avgDis);
                }
            }
            sw3.Close();
            sw4.Close();
             */
            #endregion

            #region 写数据
            /////*
            //System.Data.DataTable dtable = new System.Data.DataTable();
            //dtable.Columns.Add("id");
            //dtable.Columns.Add("CellName");
            //dtable.Columns.Add("BtsName");
            //dtable.Columns.Add("Longitude");
            //dtable.Columns.Add("Latitude");
            //dtable.Columns.Add("x");
            //dtable.Columns.Add("y");
            //dtable.Columns.Add("Altitude");
            //dtable.Columns.Add("AntHeight");
            //dtable.Columns.Add("Azimuth");
            //dtable.Columns.Add("MechTilt");
            //dtable.Columns.Add("ElecTilt");
            //dtable.Columns.Add("Tilt");
            //dtable.Columns.Add("CoverageRadius");
            //dtable.Columns.Add("FeederLength");
            //dtable.Columns.Add("EIRP");
            //dtable.Columns.Add("PathlossMode");
            //dtable.Columns.Add("CoverageType");
            //dtable.Columns.Add("NetType");
            //dtable.Columns.Add("Comments");
            //dtable.Columns.Add("eNodeB");
            //dtable.Columns.Add("CI");
            //dtable.Columns.Add("CellNameChs");
            //dtable.Columns.Add("EARFCN");
            //dtable.Columns.Add("PCI");

            //for (int i = 0; i < cells.Count; i++)
            //{
            //    System.Data.DataRow thisrow = dtable.NewRow();

            //    thisrow["id"] = cells[i].ID;
            //    thisrow["CellName"] = cells[i].CellName;
            //    thisrow["BtsName"] = cells[i].BtsName;
            //    thisrow["Longitude"] = cells[i].Longitude;
            //    thisrow["Latitude"] = cells[i].Latitude;
            //    thisrow["x"] = cells[i].x;
            //    thisrow["y"] = cells[i].y;
            //    thisrow["Altitude"] = cells[i].Altitude;
            //    thisrow["AntHeight"] = cells[i].AntHeight;
            //    thisrow["Azimuth"] = cells[i].Azimuth;
            //    thisrow["MechTilt"] = cells[i].MechTilt;
            //    thisrow["ElecTilt"] = cells[i].ElecTilt;
            //    thisrow["Tilt"] = cells[i].Tilt;
            //    thisrow["CoverageRadius"] = cells[i].CoverageRadius;
            //    thisrow["FeederLength"] = cells[i].FeederLength;
            //    thisrow["EIRP"] = cells[i].EIRP;
            //    thisrow["PathlossMode"] = cells[i].PathlossMode;
            //    thisrow["CoverageType"] = cells[i].CoverageType;
            //    thisrow["NetType"] = cells[i].NetType;
            //    thisrow["Comments"] = cells[i].Comments;
            //    thisrow["eNodeB"] = cells[i].eNodeB;
            //    thisrow["CI"] = cells[i].CI;
            //    thisrow["CellNameChs"] = cells[i].CellNameChs;
            //    thisrow["EARFCN"] = cells[i].EARFCN;
            //    thisrow["PCI"] = cells[i].PCI;
            //    dtable.Rows.Add(thisrow);
            //}

            //// 删除旧的建筑物网格
            //IbatisHelper.ExecuteDelete("deleteAllCells", null);

            //using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
            //{
            //    bcp.BatchSize = dtable.Rows.Count;
            //    bcp.BulkCopyTimeout = 1000;
            //    bcp.DestinationTableName = "CELL";
            //    bcp.WriteToServer(dtable);
            //    bcp.Close();
            //}
            //dtable.Clear();
            // */
            #endregion
            IbatisHelper.ExecuteUpdate("CELLBatchUpdateCoverageRadius", res);
            return new Result(true);
        }

        // 判断angle是否在from和to的角度内，二者有可能大小相反，大小相反的情况是角度位于x轴两侧
        private bool isInRange(double angle, double from, double to)
        {
            return from < to ? (angle > from && angle < to) : (angle < to || angle > from);
        }

        // 第k个小区覆盖方向的180度范围内，最近n个小区的距离、平均距离 
        private double inCoverCells(ref List<Cell2> cells, int k, int n, StreamWriter sw)
        {
            double fromAngle = cells[k].Azimuth - 90;
            double toAngle = cells[k].Azimuth + 90;
            double from = GeometricUtilities.ConvertGeometricArithmeticAngle(toAngle + 1);
            double to = GeometricUtilities.ConvertGeometricArithmeticAngle(fromAngle - 1);
            from = GeometricUtilities.GetRadians(from);
            to = GeometricUtilities.GetRadians(to);

            List<Pair> near = new List<Pair>();
            HashSet<string> hs = new HashSet<string>();
            hs.Add(cells[k].BtsName);

            for (int i = 0; i < cells.Count; i++)
            {
                if (hs.Contains(cells[i].BtsName))
                    continue;

                // 不在180度的覆盖范围内
                LTE.Geometric.Point source = new LTE.Geometric.Point(cells[k].x, cells[k].y, 0);
                LTE.Geometric.Point p = new LTE.Geometric.Point(cells[i].x, cells[i].y, 0);
                Polar pr = GeometricUtilities.getPolarCoord(source, p);
                if (!isInRange(pr.theta, from, to))
                    continue;

                hs.Add(cells[i].BtsName);
                double dis1 = dis(cells[i].x, cells[i].y, cells[k].x, cells[k].y);

                Pair p1 = new Pair(i, dis1);
                near.Add(p1);
            }

            near.Sort(new PairCompare());

            double avgDis = 0;
            for (int i = 0; i < near.Count && i < n; i++)
            {
                avgDis += near[i].dis;
                //sw.WriteLine("{0} {1} {2}", cells[near[i].id].id, cells[near[i].id].BtsName, near[i].dis);
                sw.Write("{0}\t", (int)near[i].dis);
            }
            sw.WriteLine();

            avgDis = avgDis / Math.Min(near.Count, n); //覆盖角度内与前n个基站之间的距离

            return avgDis;
        }

        double dis(double x, double y, double x1, double y1)
        {
            return Math.Sqrt(Math.Pow(x - x1, 2) + Math.Pow(y - y1, 2));
        }

        // 位于maxDis范围内的n个最近邻区的平均距离
        // k：小区下标
        // m：倍数
        double nearCellCovR(double maxDis, ref IList<CELL> cells, int k, int n, int m, ref double minDis)//, StreamWriter sw)
        {
            double fromAngle = cells[k].Azimuth.Value - 90;
            double toAngle = cells[k].Azimuth.Value + 90;
            double from = GeometricUtilities.ConvertGeometricArithmeticAngle(toAngle + 1);
            double to = GeometricUtilities.ConvertGeometricArithmeticAngle(fromAngle - 1);
            from = GeometricUtilities.GetRadians(from);
            to = GeometricUtilities.GetRadians(to);

            List<Pair> near = new List<Pair>();
            HashSet<string> hs = new HashSet<string>();
            hs.Add(cells[k].BtsName);

            for (int i = 0; i < cells.Count; i++)
            {
                if (hs.Contains(cells[i].BtsName))
                    continue;

                hs.Add(cells[i].BtsName);

                // 不在180度的覆盖范围内
                LTE.Geometric.Point source = new LTE.Geometric.Point(Convert.ToDouble(cells[k].x), Convert.ToDouble(cells[k].y), 0);
                LTE.Geometric.Point p = new LTE.Geometric.Point(Convert.ToDouble(cells[i].x), Convert.ToDouble(cells[i].y), 0);
                Polar pr = GeometricUtilities.getPolarCoord(source, p);
                if (!isInRange(pr.theta, from, to))
                    continue;

                double dis1 = dis(Convert.ToDouble(cells[i].x), Convert.ToDouble(cells[i].y), Convert.ToDouble(cells[k].x), Convert.ToDouble(cells[k].y));
                if (dis1 > maxDis)
                    continue;

                Pair p1 = new Pair(i, dis1);
                near.Add(p1);
            }

            near.Sort(new PairCompare());
            if (near.Count == 0)
            {
                minDis = 3000;
                return 3000;
            }

            minDis = near[0].dis;

            double avgDis = 0;
            for (int i = 0; i < near.Count && i < n; i++)
            {
                avgDis += near[i].dis;
                //sw.WriteLine("{0} {1} {2}", cells[near[i].id].id, cells[near[i].id].BtsName, near[i].dis);
                //sw.Write("{0}\t", (int)near[i].dis);
            }
            //sw.WriteLine();

            avgDis = avgDis / Math.Min(near.Count, n) * m; //覆盖角度内与前N个基站之间的距离求平均在乘以m倍

            return avgDis;
        }

        // 获取某种类型基站的垂直半功率角 todo
        double getVAngle(String AntModel, double tilt)
        {
            if (tilt < 3 && tilt >= 0)
            {
                return tilt;
            }
            else
            {
                return 6;
            }
        }

        ///<summary>
        ///计算指定小区的覆盖半径
        ///<param name="AntHeight">天线高度</para>
        ///<param name="Tilt">下倾角度</para>
        ///<param name="AntVerAglP">垂直半功率角</para>
        ///<param name="Upper3dbR">向上衰减3db后的覆盖距离</para>
        ///<param name="MainR">没有衰减的覆盖距离</para>
        ///<param name="Lower3dbR">向下衰减3db后的覆盖距离</para>
        ///</summary>
        void cmptCovR(double AntHeight, double Tilt, double AntVerAglP, ref double Upper3dbR, ref double MainR, ref double Lower3dbR)
        {
            Upper3dbR = MainR = Lower3dbR = 0;
            if (Tilt < 0 || Tilt >= 90)
            {
                //MessageBox::Show("天线下倾角不合理");
                Upper3dbR = -1;
                MainR = -1;
                return;
            }
            ///////////////Upper////////////////////
            if (Tilt == 0 || (Tilt > 0 && AntVerAglP / 2 >= Tilt))
            {
                Upper3dbR = Double.MaxValue;
            }
            else if (Tilt > 0 && AntVerAglP / 2 < Tilt)
            {
                Upper3dbR = AntHeight / (Math.Tan((Tilt - AntVerAglP / 2) * Math.PI / 180));
            }
            ////////////////////////////////////////


            //////////////Main/////////////////////
            if (Tilt == 0)
            {
                MainR = Double.MaxValue;
            }
            else if (Tilt > 0)
            {
                MainR = AntHeight / (Math.Tan((Tilt) * Math.PI / 180));
            }
            ////////////////////////////////////////


            ////////////////lower///////////////////
            if (Tilt + AntVerAglP / 2 == 0)
            {
                Lower3dbR = Double.MaxValue;
            }
            else if (Tilt + AntVerAglP / 2 >= 90)
            {
                Lower3dbR = Double.MinValue;
            }
            else if ((Tilt + AntVerAglP / 2 > 0) && (Tilt + AntVerAglP / 2 < 90))
            {
                Lower3dbR = AntHeight / (Math.Tan((Tilt + AntVerAglP / 2) * Math.PI / 180));
            }
            ////////////////////////////////////////
        }

        ///<summary>
        ///计算指定小区的覆盖半径radius
        ///<param name="Tilt">下倾角度</para>
        ///<param name="AntHeight">天线高度</para>
        ///<param name="altitude">海拔</para>
        ///<param name="OutCellCover">覆盖半径外</para>
        ///<param name="InCellCover">覆盖半径中</para>
        ///<param name="NeighborCellDis">最近邻区距离</para>
        ///<param name="NeighborCellAvgDis">最近邻区平均距离</para>
        ///</summary>
        double cmptRadius(double Tilt, Decimal Height, double OutCellCover, double InCellCover, double NeighborCellDis, double NeighborCellAvgDis)
        {
            double radius = 0; //小区的覆盖半径
            if (Tilt <= 6 && Height <= 12)
            {//a
                if (OutCellCover > 0)
                {//*“覆盖半径外”有值
                    radius = (Math.Max(NeighborCellDis, 50.0) + Math.Max(OutCellCover, 50.0)) / 2;
                }
                else if (InCellCover > 0)
                {//*“覆盖半径外”无值、但“覆盖半径中”有值
                    radius = (Math.Max(NeighborCellDis, 50.0) + Math.Max(InCellCover, 50.0) * 2) / 2;
                }
                else
                {//*“覆盖半径外”、“覆盖半径中”均无值
                    radius = Math.Max(NeighborCellDis, 50.0);
                }
            }//a
            if (Tilt <= 15 && Tilt > 6 && Height <= 12)
            {//b
                if (InCellCover > 0)
                {//*“覆盖半径中”有值
                    if (OutCellCover < 0)//覆盖半径外无值
                    {
                        radius = (Math.Max(NeighborCellDis, 50.0) + Math.Max(InCellCover, 50.0) * 2) / 2;
                    }
                    else
                    {//*覆盖半径外有值,保证覆盖半径中的2倍不超过覆盖半径外
                        radius = (Math.Max(NeighborCellDis, 50.0) + Math.Min(OutCellCover, Math.Max(InCellCover, 50.0) * 2)) / 2;
                    }
                }
                else
                { //*“覆盖半径中”无值
                    radius = Math.Max(NeighborCellDis, 50.0);
                }
            }//b
            if (Tilt > 15 && Height <= 12)
            {//c
                if (InCellCover > 0)
                { //*“覆盖半径中”有值
                    radius = (Math.Max(NeighborCellDis, 50.0) + Math.Max(InCellCover, 50.0)) / 2;
                }
                else
                {//*“覆盖半径中”无值
                    radius = Math.Max(NeighborCellDis, 50.0);
                }
            }//c
            if (Tilt <= 6 && (Height <= 40 && Height > 12))
            {//d
                if (OutCellCover > 0)
                {//*“覆盖半径外”有值
                    radius = ((Math.Max(NeighborCellDis, 50.0) + NeighborCellAvgDis) / 2 + Math.Max(OutCellCover, 50.0)) / 2;
                }
                else if (InCellCover > 0)
                {//*“覆盖半径外”无值、但“覆盖半径中”有值
                    radius = ((Math.Max(NeighborCellDis, 50.0) + NeighborCellAvgDis) / 2 + Math.Max(InCellCover, 50.0) * 2) / 2;
                }
                else
                { //*“覆盖半径外”、“覆盖半径中”均无值
                    radius = (Math.Max(NeighborCellDis, 50.0) + NeighborCellAvgDis) / 2;
                }
            }//d
            if (Tilt <= 15 && Tilt > 6 && Height <= 40 && Height > 12)
            {//e
                if (InCellCover > 0)
                {//*“覆盖半径中”有值
                    if (OutCellCover <= 0)
                    {//覆盖半径外无值
                        radius = ((Math.Max(NeighborCellDis, 50.0) + NeighborCellAvgDis) / 2 + Math.Max(InCellCover, 50.0) * 2) / 2;
                    }
                    else
                    {//*覆盖半径外、覆盖半径中有值,保证覆盖半径中的2倍不超过覆盖半径外
                        radius = ((Math.Max(NeighborCellDis, 50.0) + NeighborCellAvgDis) / 2 + Math.Min(OutCellCover, Math.Max(InCellCover, 50.0) * 2)) / 2;
                    }
                }
                else
                {//*“覆盖半径中”无值
                    radius = (Math.Max(NeighborCellDis, 50.0) + NeighborCellAvgDis) / 2;
                }
            }//e
            if (Tilt > 15 && Height <= 40 && Height > 12)
            {//f
                if (InCellCover > 0)
                { //*“覆盖半径中”有值
                    radius = ((Math.Max(NeighborCellDis, 50.0) + NeighborCellAvgDis) / 2 + Math.Max(InCellCover, 50.0)) / 2;
                }
                else
                {//*“覆盖半径中”无值
                    radius = (Math.Max(NeighborCellDis, 50.0) + NeighborCellAvgDis) / 2;
                }
            }//f
            if (Tilt <= 6 && Height > 40)
            {//g
                if (OutCellCover > 0)
                {//*“覆盖半径外”有值
                    radius = (Math.Max(NeighborCellAvgDis, 50.0) + Math.Max(OutCellCover, 50.0)) / 2;
                }
                else if (InCellCover > 0)
                {//*“覆盖半径外”无值、但“覆盖半径中”有值
                    radius = (Math.Max(NeighborCellAvgDis, 50.0) + Math.Max(InCellCover, 50.0) * 2) / 2;
                }
                else
                {//*“覆盖半径外”、“覆盖半径中”均无值
                    radius = Math.Max(NeighborCellAvgDis, 50.0);
                }
            }//g
            if (Tilt <= 15 && Tilt > 6 && Height > 40)
            {//h
                if (InCellCover > 0)
                { //*“覆盖半径中”有值
                    if (OutCellCover <= 0)
                    {//覆盖半径外无值
                        radius = (Math.Max(NeighborCellAvgDis, 50.0) + Math.Max(InCellCover, 50.0) * 2) / 2;
                    }
                    else
                    { //*覆盖半径外、覆盖半径中有值,保证覆盖半径中的2倍不超过覆盖半径外
                        radius = (Math.Max(NeighborCellAvgDis, 50.0) + Math.Min(OutCellCover, Math.Max(InCellCover, 50.0) * 2)) / 2;
                    }
                }
                else
                { //*“覆盖半径中”无值
                    radius = Math.Max(NeighborCellAvgDis, 50.0);
                }
            }//h
            if (Tilt > 15 && Height > 40)
            {//i
                if (InCellCover > 0)
                {//*“覆盖半径中”有值
                    radius = (Math.Max(NeighborCellAvgDis, 50.0) + Math.Max(InCellCover, 50.0)) / 2;
                }
                else
                {//*“覆盖半径中”无值
                    radius = Math.Max(NeighborCellAvgDis, 50.0);
                }
            }//i
            //String config1 = "MaxRadius";
            //ReadXML^ readConfig1 = gcnew ReadXML(filepath,config1);
            //double MaxRadius = Convert::ToDouble(readConfig1->configvalue);
            //if(radius > MaxRadius)
            //{
            //    radius = MaxRadius;
            //}
            return radius;
        }
    
    }

    class Cell2
    {
        public int id;
        public string CellName;
        public string BtsName;
        public double Longitude;
        public double Latitude;
        public float x;
        public float y;
        public float Altitude;
        public float AntHeight;
        public float Azimuth;
        public float MechTilt;
        public float ElecTilt;
        public float Tilt;
        public float CoverageRadius;
        public float FeederLength;
        public float EIRP;
        public string PathlossMode;
        public string CoverageType;
        public string NetType;
        public string Comments;
        public int eNodeB;
        public int CI;
        public string CellNameChs;
        public int EARFCN;
        public int PCI;
    };

    class Pair
    {
        public int id;
        public double dis;

        public Pair(int id1, double dis1)
        {
            this.id = id1;
            this.dis = dis1;
        }
    };

    class PairCompare : IComparer<Pair>
    {
        public int Compare(Pair a, Pair b)
        {
            return a.dis.CompareTo(b.dis);
        }
    }
}