using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using LTE.Geometric;
using LTE.GIS;
using LTE.DB;
using LTE.InternalInterference.Grid;
using ESRI.ArcGIS.Geometry;

namespace LTE.InternalInterference
{
    /// <summary>
    /// 划分地面网格
    /// </summary>
    public partial class FrmGrid : Form
    {
        private string name;
        private double minLongitude;
        private double minLatitude;
        private double maxLongitude;
        private double maxLatitude;
        private double sideLength;
        //给定地区的最大固定范围的经纬度
        private double fixMinLongitude=118;
        private double fixMaxLongitude=120;
        private double fixMinLatitude=31;
        private double fixMaxLatitude=33;

        public FrmGrid()
        {
            InitializeComponent();
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
            if (e.KeyChar >= '0' && e.KeyChar <= '9' || e.KeyChar == '\b' || e.KeyChar == '.')
                e.Handled = false;
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
            if (e.KeyChar >= '0' && e.KeyChar <= '9' || e.KeyChar == '\b' || e.KeyChar == '.')
                e.Handled = false;
        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
            if (e.KeyChar >= '0' && e.KeyChar <= '9' || e.KeyChar == '\b' || e.KeyChar == '.')
                e.Handled = false;
        }
        private void textBox4_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
            if (e.KeyChar >= '0' && e.KeyChar <= '9' || e.KeyChar == '\b' || e.KeyChar == '.')
                e.Handled = false;
        }

        private void textBox5_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
            if (e.KeyChar >= '0' && e.KeyChar <= '9' || e.KeyChar == '\b' || e.KeyChar == '.')
                e.Handled = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!validateInput())
                return;
            this.ConstructGrid(this.minLongitude, this.minLatitude, this.maxLongitude, this.maxLatitude, this.sideLength);
            MessageBox.Show(this, "重定义网格已完成，请执行刷新网格图层", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 2019.05.29 ShengXiang.Xiong
        /// 修改均匀栅格划分的范围，使其与DEM数据的栅格范围一致
        /// fix*表示该地区确定的经纬度范围，minX等表示选择栅格划分的范围，gridLength表示栅格大小
        /// </summary>
        /// <param name="minX"></param>
        /// <param name="minY"></param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        /// <param name="gridLength"></param>
        public double[] tinAlignment(double minX, double minY, double maxX, double maxY, double gridLength)
        {
            ESRI.ArcGIS.Geometry.IPoint pMin = new ESRI.ArcGIS.Geometry.PointClass();
            pMin.X = this.fixMinLongitude;
            pMin.Y = this.fixMinLatitude;
            pMin.Z = 0;
            PointConvert.Instance.GetProjectPoint(pMin);

            ESRI.ArcGIS.Geometry.IPoint pMax = new ESRI.ArcGIS.Geometry.PointClass();
            pMax.X = this.fixMaxLongitude;
            pMax.Y = this.fixMaxLatitude;
            pMax.Z = 0;
            PointConvert.Instance.GetProjectPoint(pMax);


            
            //小数未对齐
            minX = minX-(minX - pMin.X) % gridLength;
            maxX = maxX + (pMax.X - maxX) % gridLength;
            minY = minY - (minY - pMin.Y) % gridLength;
            maxY = maxY + (pMax.Y - maxY) % gridLength;

            //小数对齐
            //minX = minX - (minX - pMin.X) % gridLength;
            //maxX = maxX + (pMax.X - maxX) % gridLength;
            //minY = minY - (minY - pMin.Y) % gridLength;
            //maxY = maxY + (pMax.Y - maxY) % gridLength;

            //double dx = Math.Abs(maxX - minX);
            //double dy = Math.Abs(maxY - minY);
            //int cnty = Convert.ToInt32(Math.Ceiling(dy / gridLength));
            //int cntx = Convert.ToInt32(Math.Ceiling(dx / gridLength));

            return new double[4]{minX,minY,maxX,maxY};
        }

        private Boolean validateInput()
        {
            if (this.textBox6.Text == string.Empty)
            {
                MessageBox.Show(this, "请输入网格名称", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (this.textBox1.Text == string.Empty)
            {
                MessageBox.Show(this, "请输入最小经度", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (this.textBox2.Text == string.Empty)
            {
                MessageBox.Show(this, "请输入最小维度", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (this.textBox3.Text == string.Empty)
            {
                MessageBox.Show(this, "请输入最大经度", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (this.textBox4.Text == string.Empty)
            {
                MessageBox.Show(this, "请输入最大维度", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (this.textBox5.Text == string.Empty)
            {
                MessageBox.Show(this, "请输入网格边长", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            this.name = this.textBox6.Text.Trim();

            try { double.TryParse(this.textBox1.Text, out this.minLongitude); }
            catch
            {
                MessageBox.Show(this, "您输入的最小经度格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox1.Focus();
                return false;
            }
            try { double.TryParse(this.textBox2.Text, out this.minLatitude); }
            catch
            {
                MessageBox.Show(this, "您输入的最小维度格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox2.Focus();
                return false;
            }
            try { double.TryParse(this.textBox3.Text, out this.maxLongitude); }
            catch
            {
                MessageBox.Show(this, "您输入的最大经度格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox3.Focus();
                return false;
            }
            try { double.TryParse(this.textBox4.Text, out this.maxLatitude); }
            catch
            {
                MessageBox.Show(this, "您输入的最大维度格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox4.Focus();
                return false;
            }
            try { double.TryParse(this.textBox5.Text, out this.sideLength); }
            catch
            {
                MessageBox.Show(this, "您输入的网格边长格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox1.Focus();
                return false;
            }

            return true;
        }
        #region 旧方法, 20140520之前的
        //public void ConstructGrid(double minlongi, double minlati, double maxlongi, double maxlati, double perL)
        //{
        //    System.Data.DataTable dtable = new System.Data.DataTable();
        //    dtable.Columns.Add("GXID");
        //    dtable.Columns.Add("GYID");
        //    dtable.Columns.Add("CLong");
        //    dtable.Columns.Add("CLat");
        //    dtable.Columns.Add("MaxLong");
        //    dtable.Columns.Add("MaxLat");
        //    dtable.Columns.Add("MinLong");
        //    dtable.Columns.Add("MinLat");
        //    dtable.Columns.Add("ObjID2DBuilding");
        //    dtable.Columns.Add("ObjID3DBuilding");
        //    double angle1 = 0;
        //    double angle2 = 0;
        //    double distance1 = CJWDHelper.distance(minlongi, minlati, minlongi, maxlati, ref angle1);
        //    double distance2 = CJWDHelper.distance(minlongi, minlati, maxlongi, minlati, ref angle2);
        //    int disi = Convert.ToInt16(Math.Floor(distance1 / perL)) + 1;
        //    int disj = Convert.ToInt16(Math.Floor(distance2 / perL)) + 1;
        //    //int maxi = Math.Max(disi,disj);
        //    //int mini = Math.Max(disi,disj);
        //    double longi = minlongi;
        //    double lati = maxlati;
        //    int gxid = 0, gyid = 0;
        //    double maxlatiperl = lati;
        //    double minlongtperl = longi;
        //    double gridMinLongi = 0, gridMinLati = 0, gridMaxLongi = 0, gridMaxLati = 0;
        //    double CLongi = 0, CLati = 0;
        //    for (int i = 1; i <= disi; i++)
        //    {
        //        JWD jwd1 = CJWDHelper.GetJWDB(longi, lati, perL * i, 180);
        //        gridMaxLati = maxlatiperl;
        //        gridMinLati = jwd1.m_Latitude;
        //        //gridMinLongi=longi;
        //        maxlatiperl = jwd1.m_Latitude;
        //        minlongtperl = longi;
        //        if (gridMinLati < minlati)
        //            gridMinLati = minlati;
        //        for (int j = 1; j <= disj; j++)
        //        {
        //            gxid = j - 1;
        //            gyid = i - 1;
        //            JWD jwd = CJWDHelper.GetJWDB(longi, lati, perL * j, 90);
        //            gridMinLongi = minlongtperl;
        //            gridMaxLongi = jwd.m_Longitude;
        //            minlongtperl = jwd.m_Longitude;
        //            CLongi = (gridMaxLongi + gridMinLongi) / 2;
        //            CLati = (gridMinLati + gridMaxLati) / 2;
        //            if (gridMaxLongi > maxlongi)
        //                gridMaxLongi = maxlongi;
        //            System.Data.DataRow thisrow = dtable.NewRow();
        //            thisrow["GXID"] = gxid;
        //            thisrow["GYID"] = gyid;
        //            thisrow["CLong"] = CLongi;
        //            thisrow["CLat"] = CLati;
        //            thisrow["MaxLong"] = gridMaxLongi;
        //            thisrow["MaxLat"] = gridMaxLati;
        //            thisrow["MinLong"] = gridMinLongi;
        //            thisrow["MinLat"] = gridMinLati;
        //            thisrow["ObjID2DBuilding"] = 0;
        //            thisrow["ObjID3DBuilding"] = 0;
        //            dtable.Rows.Add(thisrow);
        //            //Console.WriteLine("("+gxid+","+gyid+")   "+gridMinLongi+","+gridMinLati+","+gridMaxLongi+","+gridMaxLati+","+CLongi+","+CLati);
        //        }
        //        using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
        //        {
        //            //SqlConnection.ClearAllPools();
        //            //int batchs = Math.Floor(Convert.ToDouble(sectcount / 500)) * 500;
        //            //batchs = 500;
        //            bcp.BatchSize = 100;
        //            bcp.BulkCopyTimeout = 5000000;
        //            bcp.DestinationTableName = "tbGrid";
        //            bcp.WriteToServer(dtable);
        //            bcp.Close();
        //        }
        //        dtable.Clear();
        //    }
        //}
        #endregion

        /// <summary>
        /// 以矩形区域左下角为原点构造网格，以投影坐标方式划分，基于地形
        /// </summary>
        /// <param name="minlng"></param>
        /// <param name="minlat"></param>
        /// <param name="maxlng"></param>
        /// <param name="maxlat"></param>
        /// <param name="gridlength">整数，5m,10m,20m,30m...，单位公里</param>
        public void ConstructGrid(double minlng, double minlat, double maxlng, double maxlat, double gridlength)
        {
            // 经纬度转投影坐标
            ESRI.ArcGIS.Geometry.IPoint pMin = new ESRI.ArcGIS.Geometry.PointClass();
            pMin.X = minlng;
            pMin.Y = minlat;
            pMin.Z = 0;
            PointConvert.Instance.GetProjectPoint(pMin);

            ESRI.ArcGIS.Geometry.IPoint pMax = new ESRI.ArcGIS.Geometry.PointClass();
            pMax.X = maxlng;
            pMax.Y = maxlat;
            pMax.Z = 0;
            PointConvert.Instance.GetProjectPoint(pMax);

            ////与Tin数据栅格对齐 2019.5.30
            //double[] tinAlignmentData = tinAlignment(pMin.X, pMin.Y, pMax.X, pMax.Y, 30);
            //pMin.X = tinAlignmentData[0];
            //pMin.Y = tinAlignmentData[1];
            //pMax.X = tinAlignmentData[2];
            //pMax.Y = tinAlignmentData[3];

            // 最大栅格和均匀栅格
            //2019.07.20 方便后面计算，统一以m为单位
            pMax.X = Convert.ToInt32(Math.Ceiling(pMax.X));
            pMax.Y = Convert.ToInt32(Math.Ceiling(pMax.Y));
            pMin.X = Convert.ToInt32(Math.Ceiling(pMin.X));
            pMin.Y = Convert.ToInt32(Math.Ceiling(pMin.Y));
            double dx = Math.Abs(pMax.X - pMin.X);
            double dy = Math.Abs(pMax.Y - pMin.Y);
            int maxgxid = Convert.ToInt32(Math.Ceiling(dx / gridlength));
            int maxgyid = Convert.ToInt32(Math.Ceiling(dy / gridlength));
            int maxAgxid = Convert.ToInt16(Math.Ceiling(dx / 30.0));
            int maxAgyid = Convert.ToInt16(Math.Ceiling(dy / 30.0));
            pMax.X = pMin.X + maxAgxid * 30;
            pMax.Y = pMin.Y + maxAgyid * 30;



            // 2019.6.11 地图范围
            //calcRange(minlng, minlat, maxlng, maxlat, pMin.X, pMin.Y, pMax.X, pMax.Y, maxAgxid, maxAgyid, maxgxid, maxgyid, gridlength);

            // 2019.5.28 地形和地形所在的加速栅格
            //calcTIN(pMin.X, pMin.Y, pMax.X, pMax.Y, 30, maxAgxid, maxAgyid);

            // 2019.6.5 得到建筑物海拔，基于地形
            calcBuildingAltitude((int)pMin.X, (int)pMin.Y, (int)pMax.X, (int)pMax.Y, 0, 0, maxAgxid, maxAgyid, 0, 0, maxgxid, maxgyid);

            // 2019.6.11 建筑物所在的加速栅格，基于地形
            calcAcclerateBuilding(pMin.X, pMin.Y, pMax.X, pMax.Y, 30, maxAgxid, maxAgyid);

            // 2019.6.11 建筑物表面栅格，基于地形
            calcBuildingGrids(pMin.X, pMin.Y, pMax.X, pMax.Y, gridlength, maxgxid, maxgyid);

            // 2019.6.11 地面栅格
            //calcGroundGrid(pMin.X, pMin.Y, maxgxid, maxgyid, gridlength);

            MessageBox.Show("网格划分结束！");
        }

        // 2018.6.11 计算建筑物所在的栅格，基于地形 
        bool calcBuildingGrids(double minX, double minY, double maxX, double maxY, double gridsize, int maxgxid, int maxgyid)
        {
            // 删除旧的建筑物网格
            IbatisHelper.ExecuteDelete("DeleteBuildingGrid", null);

            double err = GridHelper.getInstance().getGGridSize() / 2 + 1;
            int pageindex = 0;
            int pagesize = 10000;
            Hashtable ht = new Hashtable();
            ht["pageindex"] = pageindex;
            ht["pagesize"] = pagesize;
            DataTable tb = IbatisHelper.ExecuteQueryForDataTable("getBuildingVertexSmooth", ht);
            double h = (int)GridHelper.getInstance().getGHeight();

            System.Data.DataTable tb1 = new System.Data.DataTable();
            tb1.Columns.Add("BuildingID");
            tb1.Columns.Add("GXID");
            tb1.Columns.Add("GYID");
            tb1.Columns.Add("GZID");

            if (tb.Rows.Count < 1)
            {
                return false;
            }
            while(tb.Rows.Count>0)
            {
                //System.Data.DataTable tb1 = new System.Data.DataTable();
                //tb1.Columns.Add("BuildingID");
                //tb1.Columns.Add("GXID");
                //tb1.Columns.Add("GYID");
                //tb1.Columns.Add("GZID");
                List<BuildingVertex> tmp = new List<BuildingVertex>();

                BuildingVertex bv = new BuildingVertex();
                bv.bid = Convert.ToInt32(tb.Rows[0]["BuildingID"].ToString());
                bv.vx = Convert.ToDouble(tb.Rows[0]["VertexX"].ToString());
                bv.vy = Convert.ToDouble(tb.Rows[0]["VertexY"].ToString());
                bv.altitude = Convert.ToDouble(tb.Rows[0]["BAltitude"].ToString());  // 地形
                bv.vz = bv.altitude + Convert.ToDouble(tb.Rows[0]["Bheight"].ToString()); // 地形
                bv.vid = Convert.ToInt32(tb.Rows[0]["VIndex"].ToString());
                int lastid = bv.bid;
                tmp.Add(bv);

                double pMargin = gridsize;
                double dh = GridHelper.getInstance().getGHeight();

                for (int i = 1; i < tb.Rows.Count; i++)
                {
                    BuildingVertex bv1 = new BuildingVertex();
                    bv1.bid = Convert.ToInt32(tb.Rows[i]["BuildingID"].ToString());
                    bv1.vx = Convert.ToDouble(tb.Rows[i]["VertexX"].ToString());
                    bv1.vy = Convert.ToDouble(tb.Rows[i]["VertexY"].ToString());
                    bv1.altitude = Convert.ToDouble(tb.Rows[i]["BAltitude"].ToString()); // 地形
                    bv1.vz = bv1.altitude + Convert.ToDouble(tb.Rows[i]["Bheight"].ToString()); // 地形
                    bv1.vid = Convert.ToInt32(tb.Rows[i]["VIndex"].ToString());
                    
                    if (i == tb.Rows.Count - 1 || bv1.bid != lastid)
                    {
                        double maxx = tmp[0].vx, maxy = tmp[0].vy, minx = tmp[0].vx, miny = tmp[0].vy;
                        for (int j = 1; j < tmp.Count; ++j)
                        {
                            if (tmp[j].vx > maxx)
                                maxx = tmp[j].vx;
                            if (tmp[j].vx < minx)
                                minx = tmp[j].vx;
                            if (tmp[j].vy > maxy)
                                maxy = tmp[j].vy;
                            if (tmp[j].vy < miny)
                                miny = tmp[j].vy;
                        }

                        int gzidBase = (int)(tmp[0].altitude / GridHelper.getInstance().getGHeight());  // 基于地形，海拔处的栅格高度
                        if (gzidBase < 0)
                            gzidBase = 1;
                        int gzid = (int)(tmp[0].vz / GridHelper.getInstance().getGHeight()) + 1;  

                        int minGxid = 0, minGyid = 0, maxGxid = 0, maxGyid = 0;
                        GridHelper.getInstance().XYToGGrid(minx, miny, ref minGxid, ref minGyid);
                        GridHelper.getInstance().XYToGGrid(maxx, maxy, ref maxGxid, ref maxGyid);
                        if (minGxid == -1 || minGyid == -1)
                            continue;

                        double x = 0, y = 0, z = 0;
                        for (int j = minGxid; j <= maxGxid; j++)
                        {
                            for (int k = minGyid; k <= maxGyid; k++)
                            {
                                GridHelper.getInstance().GridToXYZ(j, k, 0, ref x, ref y, ref z);

                                #region 点是否在平面或边上
                                bool okPlane = false;
                                bool okEdge = false;

                                double tx = Math.Round(x, 3);
                                for (int ii = 0, jj = tmp.Count - 1; ii < tmp.Count; jj = ii++)
                                {
                                    if ((tmp[ii].vy > y) != (tmp[jj].vy > y))
                                    {
                                        double tmp1 = Math.Round((tmp[jj].vx - tmp[ii].vx) * (y - tmp[ii].vy) / (tmp[jj].vy - tmp[ii].vy) + tmp[ii].vx, 3);

                                        if (Math.Abs(tx - tmp1) < err)
                                        {
                                            okPlane = true;
                                            okEdge = true;
                                            break;
                                        }
                                        else if (tx < tmp1)
                                        {
                                            okPlane = !okPlane;
                                        }
                                    }
                                }

                                if (okPlane)
                                {
                                    // 建筑物顶面
                                    System.Data.DataRow thisrow = tb1.NewRow();
                                    thisrow["BuildingID"] = tmp[0].bid;
                                    thisrow["GXID"] = j;
                                    thisrow["GYID"] = k;
                                    thisrow["GZID"] = gzid;
                                    tb1.Rows.Add(thisrow);

                                    thisrow = tb1.NewRow();
                                    thisrow["BuildingID"] = tmp[0].bid;
                                    thisrow["GXID"] = j;
                                    thisrow["GYID"] = k;
                                    thisrow["GZID"] = 1;
                                    tb1.Rows.Add(thisrow);

                                    // 建筑物侧面
                                    if (okEdge)
                                    {
                                        for (int zid = gzidBase; zid < gzid; zid++) // 基于地形，海拔以下没有侧面栅格
                                        {
                                            thisrow = tb1.NewRow();
                                            thisrow["BuildingID"] = tmp[0].bid;
                                            thisrow["GXID"] = j;
                                            thisrow["GYID"] = k;
                                            thisrow["GZID"] = zid;
                                            tb1.Rows.Add(thisrow);
                                        }
                                    }
                                }
                                #endregion
                            }
                        }

                        lastid = bv1.bid;
                        tmp.Clear();
                    }
                    tmp.Add(bv1);
                    if (tb1.Rows.Count >= 5000)
                    {
                        using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                        {
                            bcp.BatchSize = tb1.Rows.Count;
                            bcp.BulkCopyTimeout = 1000;
                            bcp.DestinationTableName = "tbBuildingGrid3D";
                            bcp.WriteToServer(tb1);
                            bcp.Close();
                        }
                        tb1.Clear();
                    }
                }

                // 最后一批
                using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                {
                    bcp.BatchSize = tb1.Rows.Count;
                    bcp.BulkCopyTimeout = 1000;
                    bcp.DestinationTableName = "tbBuildingGrid3D";
                    bcp.WriteToServer(tb1);
                    bcp.Close();
                }
                tb1.Clear();
                ht["pageindex"] = ++pageindex;
                tb = IbatisHelper.ExecuteQueryForDataTable("getBuildingVertexSmooth", ht);
            }

            return true;
        }

        // 2019.6.5 根据地形得到建筑物海拔
        void calcBuildingAltitude(int minX, int minY, int maxX, int maxY,
            int minAxid, int minAyid, int maxAxid, int maxAyid,
            int mingxid, int mingyid, int maxgxid, int maxgyid)
        {
            //2019.07.20 分页操作，防止内存溢出
            int pageSize = 500;
            int minAgxid = minAxid;
            //int minAgyid = minAyid;
            int maxAgxid = Math.Min(maxAxid, minAxid + pageSize);
            //int maxAgyid = Math.Min(maxAyid, minAxid + pageSize);

            while (minAgxid <= maxAxid)
            {
                int minAgyid = 0;
                int maxAgyid = Math.Min(maxAyid, minAgyid + pageSize);
                while (minAgyid <= maxAyid)
                {
                    //根据均匀栅格网格id获得平面坐标范围(注意坐标代表的是左下角的id，所以求范围时，min用原始坐标id计算即可，而max就得在原始坐标的基础上+1计算才行)
                    int minGX = minX + minAgxid * 30;
                    int minGY = minY + minAgyid * 30;
                    int maxGX = minX + (maxAgxid+1) * 30;
                    int maxGY = minY + (maxAgyid+1) * 30;

                    // 读取范围内的加速栅格信息
                    AccelerateStruct.setAccGridRange(minAgxid, minAgyid, maxAgxid, maxAgyid);
                    //AccelerateStruct.constructAccelerateStructAltitude();
                    AccelerateStruct.constructGridTin();

                    // 读取范围内的地形 TIN
                    TINInfo.setBound(minx: minGX, miny:minGY, maxx:maxGX, maxy:maxGY);
                    TINInfo.constructTINVertex();

                    // 读取范围内的建筑物中心点
                    //BuildingGrid3D.setGGridRange(mingxid, mingyid, maxgxid, maxgyid);
                    //BuildingGrid3D.constructBuildingCenter();
                    BuildingGrid3D.constructBuildingCenterByArea(minGx:minGX,minGy:minGY,maxGx:maxGX,maxGy:maxGY);

                    // 计算建筑物底面中心的海拔
                    Dictionary<int, double> altitude = new Dictionary<int, double>();
                    foreach (var build in BuildingGrid3D.buildingCenter)
                    {
                        int bid = build.Key;

                        // 建筑物底面中心所在的 TIN
                        Grid3D agridid = new Grid3D();
                        Geometric.Point center = new Geometric.Point(BuildingGrid3D.buildingCenter[bid].X, BuildingGrid3D.buildingCenter[bid].Y, 0);
                        bool ok = GridHelper.getInstance().PointXYZToAccGrid(center, ref agridid);  // 建筑物底面中心所在的均匀栅格
                        if (!ok)
                        {
                            altitude[bid] = 0;
                            continue;
                        }
                        string key = string.Format("{0},{1},{2}", agridid.gxid, agridid.gyid, agridid.gzid);
                        List<int> TINs = AccelerateStruct.gridTIN[key];

                        // 建筑物底面中心的海拔
                        for (int i = 0; i < TINs.Count; i++)
                        {
                            List<Geometric.Point> pts = TINInfo.getTINVertex(TINs[i]);

                            if (TINs[i] == 173047)
                            {
                                Console.WriteLine(TINInfo.getTINVertex(TINs[i]));
                            }

                            if (pts.Count < 3)
                                MessageBox.Show("TIN 数据出错！");


                            bool inTIN = Geometric.PointHeight.isInside(pts[0], pts[1], pts[2],
                                BuildingGrid3D.buildingCenter[bid].X, BuildingGrid3D.buildingCenter[bid].Y);

                            if (inTIN) // 位于当前 TIN 三角形内
                            {
                                double alt = Geometric.PointHeight.getPointHeight(pts[0], pts[1], pts[2],
                                    BuildingGrid3D.buildingCenter[bid].X, BuildingGrid3D.buildingCenter[bid].Y);
                                altitude[bid] = alt;
                                break;
                            }
                        }
                    }

                    // 更新数据库
                    //TODO: 部分更新building数据表
                    Hashtable ht = new Hashtable();
                    ht["minGX"] = minGX;
                    ht["maxGX"] = maxGX;
                    ht["minGY"] = minGY;
                    ht["maxGY"] = maxGY;
                    System.Data.DataTable tb = IbatisHelper.ExecuteQueryForDataTable("getBuildingByArea", ht);
                    //System.Data.DataTable tb = IbatisHelper.ExecuteQueryForDataTable("getBuilding", null);
                    for (int i = 0; i < tb.Rows.Count; i++)
                    {
                        int bid = Convert.ToInt32(tb.Rows[i]["BuildingID"].ToString());
                        if (!altitude.Keys.Contains(bid))
                            continue;
                        tb.Rows[i]["BAltitude"] = altitude[bid];
                    }
                    //IbatisHelper.ExecuteDelete("DeleteBuilding", null);  // 删除旧的
                    IbatisHelper.ExecuteDelete("DeleteBuildingByArea", ht);

                    using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))  // 写入新的
                    {
                        bcp.BatchSize = tb.Rows.Count;
                        bcp.BulkCopyTimeout = 1000;
                        bcp.DestinationTableName = "tbBuilding";
                        bcp.WriteToServer(tb);

                        bcp.Close();
                    }
                    tb.Clear();

                    AccelerateStruct.clearAccelerateStruct();
                    TINInfo.clear();
                    BuildingGrid3D.clearBuildingData();
                    minAgyid = maxAgyid + 1;
                    maxAgyid = Math.Min(maxAgyid + pageSize, maxAyid);
                }
                minAgxid = maxAgxid + 1;
                maxAgxid = Math.Min(maxAgxid + pageSize, maxAxid);
            }

          
        }


        // 2019.6.5 根据地形得到建筑物海拔
        //void calcBuildingAltitude(double minX, double minY, double maxX, double maxY, 
        //    int minAgxid, int minAgyid, int maxAgxid, int maxAgyid,
        //    int mingxid, int mingyid, int maxgxid, int maxgyid)
        //{
        //    // 读取范围内的加速栅格信息
        //    AccelerateStruct.setAccGridRange(minAgxid, minAgyid, maxAgxid, maxAgyid);
        //    AccelerateStruct.constructAccelerateStructAltitude();

        //    // 读取范围内的地形 TIN
        //    TINInfo.setBound(minX, minY, maxX, maxY);
        //    TINInfo.constructTINVertex();

        //    // 读取范围内的建筑物中心点
        //    BuildingGrid3D.setGGridRange(mingxid, mingyid, maxgxid, maxgyid);
        //    BuildingGrid3D.constructBuildingCenter();

        //    // 计算建筑物底面中心的海拔
        //    Dictionary<int, double> altitude = new Dictionary<int, double>();
        //    foreach(var build in BuildingGrid3D.buildingCenter)
        //    {
        //        int bid = build.Key;

        //        // 建筑物底面中心所在的 TIN
        //        Grid3D agridid = new Grid3D();
        //        Geometric.Point center = new Geometric.Point(BuildingGrid3D.buildingCenter[bid].X, BuildingGrid3D.buildingCenter[bid].Y, 0);
        //        bool ok = GridHelper.getInstance().PointXYZToAccGrid(center, ref agridid);  // 建筑物底面中心所在的均匀栅格
        //        if (!ok)
        //        {
        //            altitude[bid] = 0;
        //            continue;
        //        }
        //        string key = string.Format("{0},{1},{2}", agridid.gxid, agridid.gyid, agridid.gzid);
        //        List<int> TINs = AccelerateStruct.gridTIN[key];

        //        // 建筑物底面中心的海拔
        //        for(int i=0; i<TINs.Count; i++)
        //        {
        //            List<Geometric.Point> pts = TINInfo.getTINVertex(TINs[i]);

        //            if (pts.Count < 3)
        //                MessageBox.Show("TIN 数据出错！");


        //            bool inTIN = Geometric.PointHeight.isInside(pts[0], pts[1], pts[2],
        //                BuildingGrid3D.buildingCenter[bid].X, BuildingGrid3D.buildingCenter[bid].Y);

        //            if (inTIN) // 位于当前 TIN 三角形内
        //            {
        //                double alt = Geometric.PointHeight.getPointHeight(pts[0], pts[1], pts[2],
        //                    BuildingGrid3D.buildingCenter[bid].X, BuildingGrid3D.buildingCenter[bid].Y);
        //                altitude[bid] = alt;
        //                break;
        //            }
        //        }
        //    }

        //    // 更新数据库
        //    System.Data.DataTable tb = IbatisHelper.ExecuteQueryForDataTable("getBuilding", null);
        //    for (int i = 0; i < tb.Rows.Count; i++)
        //    {
        //        int bid = Convert.ToInt32(tb.Rows[i]["BuildingID"].ToString());
        //        if (!altitude.Keys.Contains(bid))
        //            continue;
        //        tb.Rows[i]["BAltitude"] = altitude[bid];
        //    }
        //    IbatisHelper.ExecuteDelete("DeleteBuilding", null);  // 删除旧的
        //    using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))  // 写入新的
        //    {
        //        bcp.BatchSize = tb.Rows.Count;
        //        bcp.BulkCopyTimeout = 1000;
        //        bcp.DestinationTableName = "tbBuilding";
        //        bcp.WriteToServer(tb);

        //        bcp.Close();
        //    }
        //    tb.Clear();

        //    AccelerateStruct.clearAccelerateStruct();
        //    TINInfo.clear();
        //    BuildingGrid3D.clearBuildingData();
        //}

        // 2019.5.28 记录每个均匀栅格内有哪些地形 TIN 三角形
        /// <summary>
        /// 2019.7.21 xsx 修改源代码逻辑，增添分页操作，按栅格面积分页，解决数据完整性问题
        /// </summary>
        /// <param name="minX"></param>
        /// <param name="minY"></param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        /// <param name="agridsize"></param>
        /// <param name="maxAxid"></param>
        /// <param name="maxAyid"></param>
        /// <returns></returns>
        bool calcTIN(double minsX, double minsY, double maxsX, double maxsY, double agridsize, int maxAxid, int maxAyid)
        {
            Hashtable ht = new Hashtable();
            ht["minX"] = minsX;
            ht["maxX"] = maxsX;
            ht["minY"] = minsY;
            ht["maxY"] = maxsY;
            // 从原始数据中读取区域范围内的地形 TIN 最低点
            DataTable tbHeight = IbatisHelper.ExecuteQueryForDataTable("GetMinHeight", ht);
            if (tbHeight.Rows.Count < 1)
            {
                MessageBox.Show("没有 TIN 数据！");
                return false;
            }
            double minHeight = Convert.ToDouble(tbHeight.Rows[0][0]);

            //用于记录TIN加速栅格
            System.Data.DataTable tb1 = new System.Data.DataTable();
            tb1.Columns.Add("GXID");
            tb1.Columns.Add("GYID");
            tb1.Columns.Add("GZID");
            tb1.Columns.Add("TINID");

            //IbatisHelper.ExecuteDelete("DeleteTIN", null);
            //IbatisHelper.ExecuteDelete("DeleteAccrelateTIN", null);
            IbatisHelper.ExecuteDelete("TruncateTIN", null);
            IbatisHelper.ExecuteDelete("TruncateAccelerateGridTIN", null);

            //for(int minAgxid=0, maxAgxid = minAgxid+pageSize ; minAgxid <= maxAgxid ; minAgxid += maxAgxid + 1 , maxAgxid = Math.Min( maxAxid , maxAgxid + pageSize))
            //    for(int minAgyid=0, maxAgyid = minAgyid+pageSize ; minAgyid <= maxAgyid ; minAgyid += maxAgyid + 1 , maxAgyid = Math.Min(maxAyid, maxAgyid + pageSize))
            //    {

            //    }
            //2019.07.20 分页操作，防止内存泄漏
            int pageSize = 500;
            int minAgxid = 0;
            int maxAgxid = Math.Min(maxAxid, minAgxid + pageSize);
            while (minAgxid <= maxAxid)
            {
                int minAgyid = 0;
                int maxAgyid = Math.Min(maxAyid, minAgyid + pageSize);
                while (minAgyid <= maxAyid)
                {
                    //根据均匀栅格网格id获得平面坐标范围（注意坐标代表的是左下角的id，所以求范围时，min用原始坐标id计算即可，而max就得在原始坐标的基础上+1计算才行）
                    double minX = minsX + minAgxid * 30;
                    double minY = minsY + minAgyid * 30;
                    double maxX = minsX + (maxAgxid+1) * 30;
                    double maxY = minsY + (maxAgyid+1) * 30;
                    ht["minX"] = minX;
                    ht["minY"] = minY;
                    ht["maxX"] = maxX;
                    ht["maxY"] = maxY;

                    //int pageindex = 0;
                    //int pagesize = 10000;
                    //ht["pageindex"] = pageindex;
                    //ht["pagesize"] = pagesize;

                    // 从原始数据中读取区域范围内的地形 TIN，更新局部数据，以最低点高度为基准
                    DataTable tb = IbatisHelper.ExecuteQueryForDataTable("GetTINVertexOriginal", ht);
                    if (tb.Rows.Count < 1)
                    {
                        continue;//说明在本区域的tin数据为空
                        //MessageBox.Show("TIN 数据为空");
                        //return false;
                    }

                    for (int i = 0; i < tb.Rows.Count; i++)
                    {
                        tb.Rows[i]["VertexHeight"] = Convert.ToDouble(tb.Rows[i]["VertexHeight"]) - minHeight;
                    }

                    using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                    {
                        bcp.BatchSize = tb.Rows.Count;
                        bcp.BulkCopyTimeout = 1000;
                        bcp.DestinationTableName = "tbTIN";
                        bcp.WriteToServer(tb);
                        bcp.Close();
                    }

                    //HashSet<string> set = new HashSet<string>();
                    int id=0;
                    //int lastid = 0;

                    for (int i = 0; i < tb.Rows.Count; i += 3)
                    {
                        // 得到 TIN 三角形的轴对齐包围盒
                        double minTINx = double.MaxValue;
                        double minTINy = double.MaxValue;
                        double maxTINx = double.MinValue;
                        double maxTINy = double.MinValue;
                        double maxTINz = double.MinValue;

                        for (int j = i; j < i + 3; j++)
                        {
                            id = Convert.ToInt32(tb.Rows[j]["TINID"].ToString());
                            double x = Convert.ToDouble(tb.Rows[j]["VertexX"].ToString());
                            double y = Convert.ToDouble(tb.Rows[j]["VertexY"].ToString());
                            double z = Convert.ToDouble(tb.Rows[j]["VertexHeight"].ToString());

                            if (x < minTINx) minTINx = x;
                            if (y < minTINy) minTINy = y;
                            if (x > maxTINx) maxTINx = x;
                            if (y > maxTINy) maxTINy = y;
                            if (z > maxTINz) maxTINz = z;
                        }
                        ////2019.07.18 xsx 及时清理掉set中以前的数据，防止内存泄漏，因为数据是按照TINID顺序处理的，所以如果当前处理的ID和以前的ID不同的话，必然不会重复，也就是说set中只存当前TINID的数据
                        //if (id != lastid)
                        //{
                        //    set.Clear();
                        //    lastid = id;
                        //}

                        //以左下角作为网格id坐标
                        int minGxid = Convert.ToInt32(Math.Ceiling((minTINx - minsX) / agridsize)) - 1;
                        int minGyid = Convert.ToInt32(Math.Ceiling((minTINy - minsY) / agridsize)) - 1;
                        int maxGxid = Convert.ToInt32(Math.Ceiling((maxTINx - minsX) / agridsize)) - 1;
                        int maxGyid = Convert.ToInt32(Math.Ceiling((maxTINy - minsY) / agridsize)) - 1;
                        int maxGzid = Convert.ToInt32(Math.Ceiling(maxTINz / agridsize)) - 1;

                        // TIN 三角形跨越的均匀栅格
                        
                        //范围修正，舍去TIN超过给定栅格范围的数据
                        minGxid = Math.Max(minGxid, minAgxid);
                        minGyid = Math.Max(minGyid, minAgyid);
                        maxGxid = Math.Min(maxGxid, maxAgxid);
                        maxGyid = Math.Min(maxGyid, maxAgyid);

                        for (int j = minGxid; j <= maxGxid; j++)
                        {
                            for (int k = minGyid; k <= maxGyid; k++)
                            {
                                for (int h = 0; h <= maxGzid; h++)
                                {
                                    //string key = string.Format("{0},{1},{2},{3}", j, k, h + 1, id);
                                    //if (set.Contains(key))
                                    //    continue;
                                    //set.Add(key);

                                    System.Data.DataRow thisrow = tb1.NewRow();
                                    thisrow["GXID"] = j;
                                    thisrow["GYID"] = k;
                                    thisrow["GZID"] = h + 1;
                                    thisrow["TINID"] = id;
                                    tb1.Rows.Add(thisrow);
                                }
                            }
                        }

                        if (tb1.Rows.Count > 5000)
                        {
                            using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                            {
                                bcp.BatchSize = tb1.Rows.Count;
                                bcp.BulkCopyTimeout = 1000;
                                bcp.DestinationTableName = "tbAccelerateGridTIN";
                                bcp.WriteToServer(tb1);
                                bcp.Close();
                                tb1.Rows.Clear();
                            }
                        }
                    }

                    // 最后一批
                    using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                    {
                        bcp.BatchSize = tb1.Rows.Count;
                        bcp.BulkCopyTimeout = 1000;
                        bcp.DestinationTableName = "tbAccelerateGridTIN";
                        bcp.WriteToServer(tb1);
                        bcp.Close();
                        tb1.Rows.Clear();
                    }
                    minAgyid = maxAgyid + 1;
                    maxAgyid = Math.Min(maxAgyid + pageSize, maxAyid);
                }
                minAgxid = maxAgxid + 1;
                maxAgxid = Math.Min(maxAgxid + pageSize, maxAxid);
            }
            return true;

        }

        // 计算建筑物所在的加速网格  2019.6.13 改
        bool calcAcclerateBuilding(double minX, double minY, double maxX, double maxY, double agridsize, int maxAgxid, int maxAgyid)
        {
            DataTable tb = IbatisHelper.ExecuteQueryForDataTable("getBuildingVertexSmooth1", null);

            if (tb.Rows.Count < 1)
            {
                return false;
            }
            else
            {
                IbatisHelper.ExecuteDelete("DeleteBuildingAccrelate1", null);

                HashSet<string> set = new HashSet<string>();
                System.Data.DataTable tb1 = new System.Data.DataTable();
                tb1.Columns.Add("GXID");
                tb1.Columns.Add("GYID");
                tb1.Columns.Add("GZID");
                tb1.Columns.Add("BuildingID");

                for (int i = 0; i < tb.Rows.Count; i++)
                {
                    // 得到建筑物底面的轴对齐包围盒
                    int id = Convert.ToInt32(tb.Rows[i]["BuildingID"].ToString());
                    double height = Convert.ToDouble(tb.Rows[i]["Bheight"].ToString());
                    double altitude = Convert.ToDouble(tb.Rows[i]["BAltitude"].ToString());  // 基于地形
                    double x = Convert.ToDouble(tb.Rows[i]["minX"].ToString());
                    double y = Convert.ToDouble(tb.Rows[i]["minY"].ToString());
                    double x1 = Convert.ToDouble(tb.Rows[i]["maxX"].ToString());
                    double y1 = Convert.ToDouble(tb.Rows[i]["maxY"].ToString());

                    // 建筑物底面跨越的均匀栅格
                    int minGxid = Convert.ToInt32(Math.Ceiling((x - minX) / agridsize)) - 1;
                    int minGyid = Convert.ToInt32(Math.Ceiling((y - minY) / agridsize)) - 1;
                    int maxGxid = Convert.ToInt32(Math.Ceiling((x1 - minX) / agridsize)) - 1;
                    int maxGyid = Convert.ToInt32(Math.Ceiling((y1 - minY) / agridsize)) - 1;
                    int maxGzid = Convert.ToInt32(Math.Ceiling((height + altitude) / agridsize)) - 1;

                    bool ok = (x >= minX || x1 <= maxX || y >= minY || y1 <= maxY);

                    if (minGxid < 0)
                    {
                        if (ok)
                            minGxid = 0;
                        else
                            continue;
                    }
                    if (minGyid < 0)
                    {
                        if (ok)
                            minGyid = 0;
                        else
                            continue;
                    }
                    if (maxGxid > maxAgxid)
                    {
                        if (ok)
                            maxGxid = maxAgxid;
                        else
                            continue;
                    }
                    if (maxGyid > maxAgyid)
                    {
                        if (ok)
                            maxGyid = maxAgyid;
                        else
                            continue;
                    }

                    if (maxGzid > 2)
                        maxGzid = 2;

                    for (int j = minGxid; j <= maxGxid; j++)
                    {
                        for (int k = minGyid; k <= maxGyid; k++)
                        {
                            for (int h = 0; h <= maxGzid; h++)
                            {
                                string key = string.Format("{0},{1},{2},{3}", j, k, h + 1, id);
                                if (set.Contains(key))
                                    continue;
                                set.Add(key);

                                System.Data.DataRow thisrow = tb1.NewRow();
                                thisrow["GXID"] = j;
                                thisrow["GYID"] = k;
                                thisrow["GZID"] = h + 1;
                                thisrow["BuildingID"] = id;
                                tb1.Rows.Add(thisrow);
                            }
                        }
                    }

                    if (tb1.Rows.Count > 5000)
                    {
                        using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                        {
                            bcp.BatchSize = tb1.Rows.Count;
                            bcp.BulkCopyTimeout = 1000;
                            bcp.DestinationTableName = "tbAccelerateGridBuilding";
                            bcp.WriteToServer(tb1);
                            bcp.Close();
                            tb1.Clear();
                        }
                    }
                }

                // 写入数据库
                using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                {
                    bcp.BatchSize = tb1.Rows.Count;
                    bcp.BulkCopyTimeout = 1000;
                    bcp.DestinationTableName = "tbAccelerateGridBuilding";
                    bcp.WriteToServer(tb1);
                    bcp.Close();
                    tb1.Clear();
                }

                return true;
            }
        }

        // 地图范围
        void calcRange(double minlng, double minlat, double maxlng, double maxlat,
                        double minX, double minY, double maxX, double maxY,
                        int maxAgxid, int maxAgyid, int maxgxid, int maxgyid, double gridlength)
        {
            Hashtable ht = new Hashtable();
            ht["id"] = 1;
            ht["AreaMinLong"] = minlng;
            ht["AreaMinLat"] = minlat;
            ht["AreaMaxLong"] = maxlng;
            ht["AreaMaxLat"] = maxlat;
            ht["GGridSize"] = Convert.ToByte(gridlength);
            ht["MaxGGXID"] = maxgxid - 1;
            ht["MaxGGYID"] = maxgyid - 1;

            // 2017.4.28 添加
            ht["AreaMinX"] = minX;
            ht["AreaMinY"] = minY;
            ht["AreaMaxX"] = maxX;
            ht["AreaMaxY"] = maxY;
            ht["GHeight"] = 3;
            ht["GBaseHeight"] = 1.5;
            ht["AGridSize"] = 30;
            ht["AGridVSize"] = 30;
            ht["MaxAGXID"] = maxAgxid - 1;
            ht["MaxAGYID"] = maxAgyid - 1;

            // 暂时不用，当截取地图中一小部分时用到
            ht["MinX"] = minX;
            ht["MinY"] = minY;
            ht["MinGGXID"] = 0;
            ht["MinGGYID"] = 0;
            ht["MinAGXID"] = 0;
            ht["MinAGYID"] = 0;
            IbatisHelper.ExecuteInsert("insertGridRange", ht);
        }

        // 地面网格
        void calcGroundGrid(double minX, double minY, int maxgxid, int maxgyid, double gridlength)
        {
            IbatisHelper.ExecuteDelete("DeleteGroundGrids", null);

            System.Data.DataTable dtable = new System.Data.DataTable();
            dtable.Columns.Add("GXID");
            dtable.Columns.Add("GYID");
            dtable.Columns.Add("CLong");
            dtable.Columns.Add("CLat");
            dtable.Columns.Add("MinLong");
            dtable.Columns.Add("MinLat");
            dtable.Columns.Add("MaxLong");
            dtable.Columns.Add("MaxLat");
            dtable.Columns.Add("CX");
            dtable.Columns.Add("CY");
            dtable.Columns.Add("MinX");
            dtable.Columns.Add("MinY");
            dtable.Columns.Add("MaxX");
            dtable.Columns.Add("MaxY");
            dtable.Columns.Add("Dem");

            double gminX, gminY, gmaxX, gmaxY, gcX, gcY;
            gminX = minX;
            gminY = minY;

            ESRI.ArcGIS.Geometry.IPoint p1 = new ESRI.ArcGIS.Geometry.PointClass();
            ESRI.ArcGIS.Geometry.IPoint p2 = new ESRI.ArcGIS.Geometry.PointClass();
            ESRI.ArcGIS.Geometry.IPoint p3 = new ESRI.ArcGIS.Geometry.PointClass();
            p1.Z = 0;
            p2.Z = 0;
            p3.Z = 0;
            //  地面栅格
            for (int x = 0; x < maxgxid; x++)
            {
                gminY = minY;
                gmaxX = gminX + gridlength;
                gcX = (gminX + gmaxX) / 2.0;
                p1.X = gminX;
                p2.X = gmaxX;
                p3.X = gcX;

                for (int y = 0; y < maxgyid; y++)
                {
                    gmaxY = gminY + gridlength;
                    gcY = (gminY + gmaxY) / 2.0;

                    p1.X = gminX;
                    p2.X = gmaxX;
                    p3.X = gcX;
                    p1.Y = gminY;
                    p2.Y = gmaxY;
                    p3.Y = gcY;
                    PointConvert.Instance.GetGeoPoint(p1);
                    PointConvert.Instance.GetGeoPoint(p2);
                    PointConvert.Instance.GetGeoPoint(p3);

                    System.Data.DataRow thisrow = dtable.NewRow();
                    thisrow["GXID"] = x;
                    thisrow["GYID"] = y;
                    thisrow["CLong"] = p3.X;
                    thisrow["CLat"] = p3.Y;
                    thisrow["MinLong"] = p1.X;
                    thisrow["MinLat"] = p1.Y;
                    thisrow["MaxLong"] = p2.X;
                    thisrow["MaxLat"] = p2.Y;
                    thisrow["CX"] = gcX;
                    thisrow["CY"] = gcY;
                    thisrow["MinX"] = gminX;
                    thisrow["MinY"] = gminY;
                    thisrow["MaxX"] = gmaxX;
                    thisrow["MaxY"] = gmaxY;
                    thisrow["Dem"] = 0;
                    dtable.Rows.Add(thisrow);
                    gminY = gmaxY;
                }
                gminX = gmaxX;

                // 将地面栅格分批写入数据库
                if (dtable.Rows.Count > 50000)
                {
                    using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                    {
                        bcp.BatchSize = dtable.Rows.Count;
                        bcp.BulkCopyTimeout = 1000;
                        bcp.DestinationTableName = "tbGridDem";
                        bcp.WriteToServer(dtable);
                        bcp.Close();
                    }
                    dtable.Clear();
                }
            }
            // 最后一批地面栅格
            using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
            {
                bcp.BatchSize = dtable.Rows.Count;
                bcp.BulkCopyTimeout = 1000;
                bcp.DestinationTableName = "tbGridDem";
                bcp.WriteToServer(dtable);
                bcp.Close();
            }
            dtable.Clear();
        }

        

        class BuildingVertex
        {
            public int bid;
            public double vx, vy, vz;
            public int vid;
            public double altitude;
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }
    }
}
