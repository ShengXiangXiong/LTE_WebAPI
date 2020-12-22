﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Http;
using System.Web.Http;
using LTE.WebAPI.Models;
using LTE.WebAPI.Attributes;
using LTE.WebAPI.Utils;
using LTE.Geometric;
using LTE.Model;
using LTE.DB;
using System.Collections;
using System.Data;
using System.Threading.Tasks;
using System.Threading;
using LTE.InternalInterference.Grid;
using LTE.Utils;
using StackExchange.Redis;
using System.Diagnostics;
using System.Data.SqlClient;
using LTE.ExternalInterference.Struct;
using LTE.InternalInterference;
using LTE.ExternalInterference;

namespace LTE.WebAPI.Controllers
{
    public class ModelDataGenController : ApiController
    {
        private int canGridL = 90;
        private int canGridW = 90;
        private int canGridH = 30;

        [AllowAnonymous]
        [TaskLoadInfo(taskName = "干扰区域数据仿真——模拟路测点生成", type = TaskType.DataMock)]
        public Result RoadPointMockGen([FromBody]DataRange dataRange)
        {
            List<CellRayTracingModel> cellRays = interfeCellGen(dataRange);
            WriteDt(dataRange);
            int cnt = 0;
            LoadInfo loadInfo = new LoadInfo();
            loadInfo.count = cellRays.Count;
            loadInfo.loadCreate();

            RedisMq.subscriber.Subscribe("rayTrace_finish", (channel, message) => 
            {
                if (++cnt < cellRays.Count)
                {
                    loadInfo.cnt = cnt;
                    loadInfo.loadUpdate();
                    Task.Run(() =>
                    {
                        cellRays[cnt].calc();
                    });
                }
                else
                {
                    loadInfo.finish = true;
                    loadInfo.loadUpdate();
                }
            });
            cellRays[0].calc();
            //foreach (var ray in cellRays)
            //{
            //    ray.calc();
            //}

            Result res = new Result(true,"区域数据仿真已提交");
            return res;
        }
        
        public List<CellRayTracingModel> interfeCellGen(DataRange dataRange)
        {
            List<CellRayTracingModel> res = new List<CellRayTracingModel>();
            Point pMin = new Point();
            pMin.X = dataRange.minLongitude;
            pMin.Y = dataRange.minLatitude;
            pMin.Z = 0;
            LTE.Utils.PointConvertByProj.Instance.GetProjectPoint(pMin);

            Point pMax = new Point();
            pMax.X = dataRange.maxLongitude;
            pMax.Y = dataRange.maxLatitude;
            pMax.Z = 0;
            LTE.Utils.PointConvertByProj.Instance.GetProjectPoint(pMax);

            double maxBh = 90;//最大建筑物高度
            int radius = 1200;//干扰源覆盖半径
            String tarBaseName = "测试干扰源基站_";
            List<CELL> cells = new List<CELL>();
            int batch = 10;
            int cnt = 0;

            //计算测试数据总数
            int lx = (int)Math.Ceiling((pMax.X - pMin.X) / dataRange.tarGridX);
            int ly = (int)Math.Ceiling((pMax.Y - pMin.Y) / dataRange.tarGridY);
            int lz = (int)Math.Ceiling(maxBh/ dataRange.tarGridH);
            long uidBatch = long.Parse((lx*ly*lz).ToString());
            String dbName = "CELL";
            int initOff = 1500000; 
            int uid = (int)UIDHelper.GenUIdByRedis(dbName,uidBatch)+initOff;

            for (double x = pMin.X; x < pMax.X; x += dataRange.tarGridX)
            {
                for (double y = pMin.Y; y < pMax.Y; y+=dataRange.tarGridY)
                {
                    for (double z = 30; z <= maxBh; z+=30)
                    {
                        cnt++;
                        Random r = new Random(uid);
                        CELL cELL = new CELL();
                        cELL.ID = uid;
                        cELL.CellName = tarBaseName + uid;
                        cELL.Altitude = 13;
                        cELL.AntHeight = (decimal)z;
                        cELL.x = (decimal)x;
                        cELL.y = (decimal)y;
                        cELL.CI = uid;
                        cELL.eNodeB = uid;
                        cELL.EIRP = 32;
                        cELL.Azimuth = 0;
                        cELL.Tilt = r.Next(4,16);   //下倾角范围4~16之间随机取
                        cELL.EARFCN = 63;
                        cELL.CoverageRadius = radius;
                        cells.Add(cELL);

                        CellRayTracingModel rayCell = new CellRayTracingModel();
                        rayCell.cellName = cELL.CellName;
                        rayCell.reflectionNum = 3;
                        rayCell.diffPointsMargin = 5;
                        rayCell.diffractionNum = 2;
                        rayCell.threadNum = 3;
                        rayCell.incrementAngle = 180;
                        rayCell.computeIndoor = false;
                        rayCell.computeDiffrac = true;
                        rayCell.distance = radius;
                        res.Add(rayCell);

                        uid++;
                    }
                    if (res.Count >= batch)
                    {
                        IbatisHelper.ExecuteInsert("CELL_BatchInsert", cells);
                        cells.Clear();
                    }
                }
            }
            if (cells.Count > 0)
            {
                IbatisHelper.ExecuteInsert("CELL_BatchInsert", cells);
            }

            return res;
        }
        
        public void WriteDt(DataRange dataRange)
        {
            RedisMq.subscriber.Subscribe("cover2db_finish", (channel, message) => {

                Hashtable ht = new Hashtable();
                DataTable dtable = new DataTable();
                
                //数据模拟阶段,选取top k
                int sRec = 2000 * 2000;
                int k = sRec / (canGridL * canGridW);

                ht["eNodeB"] = message;
                ht["k"] = k;
                DataTable dt = DB.IbatisHelper.ExecuteQueryForDataTable("qureyMockDT", ht);
                dtable.Columns.Add("ID", System.Type.GetType("System.Int32"));
                dtable.Columns.Add("x", System.Type.GetType("System.Decimal"));
                dtable.Columns.Add("y", System.Type.GetType("System.Decimal"));
                //dtable.Columns.Add("Lon", System.Type.GetType("System.Decimal"));
                //dtable.Columns.Add("Lat", System.Type.GetType("System.Decimal"));
                dtable.Columns.Add("RSRP", System.Type.GetType("System.Double"));
                dtable.Columns.Add("InfName", System.Type.GetType("System.String"));
                //dtable.Columns.Add("DtType", System.Type.GetType("System.String"));

                int initOff = 5000;
                int uid = (int)UIDHelper.GenUIdByRedis("DT", dt.Rows.Count) + initOff;
                string infName = dataRange.infAreaId + "_" + message;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    var row = dt.Rows[i];
                    int gxid = (int)row["GXID"];
                    int gyid = (int)row["GYID"];
                    double rsrp = (double)row["ReceivedPowerdbm"];
                    Point geo = GridHelper.getInstance().GridToGeo(gxid, gyid);
                    Point proj = GridHelper.getInstance().GridToXY(gxid, gyid);
                    DataRow thisrow = dtable.NewRow();
                    thisrow["ID"] = uid+i;
                    thisrow["x"] = proj.X;
                    thisrow["y"] = proj.Y;
                    //thisrow["Lon"] = geo.X;
                    //thisrow["Lat"] = geo.Y;
                    thisrow["RSRP"] = rsrp;
                    thisrow["InfName"] = infName;
                    //thisrow["DtType"] = "mock";
                    dtable.Rows.Add(thisrow);
                }
                //DataUtil.BCPDataTableImport(dtable, "tbUINTF");
                SelectDT(infName, dtable);
            });
        }
        [AllowAnonymous]
        public void Wdt() {
            Hashtable ht = new Hashtable();
            DataTable dtable = new DataTable();
            dtable.Columns.Add("ID", System.Type.GetType("System.Int32"));
            dtable.Columns.Add("x", System.Type.GetType("System.Decimal"));
            dtable.Columns.Add("y", System.Type.GetType("System.Decimal"));
            dtable.Columns.Add("Lon", System.Type.GetType("System.Decimal"));
            dtable.Columns.Add("Lat", System.Type.GetType("System.Decimal"));
            dtable.Columns.Add("RSRP", System.Type.GetType("System.Double"));
            dtable.Columns.Add("InfName", System.Type.GetType("System.String"));
            dtable.Columns.Add("DtType", System.Type.GetType("System.String"));

            for (int vir = 50369; vir <= 50458; vir++)
            {
                dtable.Clear();
                ht["eNodeB"] = vir;
                DataTable dt = DB.IbatisHelper.ExecuteQueryForDataTable("qureyMockDT", ht);
                int initOff = 5000;
                int uid = (int)UIDHelper.GenUIdByRedis("DT", dt.Rows.Count) + initOff;

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    var row = dt.Rows[i];
                    int gxid = (int)row["GXID"];
                    int gyid = (int)row["GYID"];
                    double rsrp = (double)row["ReceivedPowerdbm"];
                    Point geo = GridHelper.getInstance().GridToGeo(gxid, gyid);
                    Point proj = GridHelper.getInstance().GridToXY(gxid, gyid);
                    DataRow thisrow = dtable.NewRow();
                    thisrow["ID"] = uid + i;
                    thisrow["x"] = proj.X;
                    thisrow["y"] = proj.Y;
                    thisrow["Lon"] = geo.X;
                    thisrow["Lat"] = geo.Y;
                    thisrow["RSRP"] = rsrp;
                    thisrow["InfName"] = "v1" + "_" + vir;
                    thisrow["DtType"] = "mock";
                    dtable.Rows.Add(thisrow);
                }
                DataUtil.BCPDataTableImport(dtable, "tbUINTF");
            }

            
        }


        [AllowAnonymous]
        public void RoadPointSelect()
        {
            string pre = "v1";
            //for (int i = 50369; i <= 50458; i++)
            //{
            //    SelectDT(pre + "_" + i);
            //}
            //SelectDT(pre + "_" + 1505669);
        }

        [AllowAnonymous]
        public void SelectDT(string InfName,DataTable dt) {
            //筛选信号值前k个的点
            //Hashtable ht = new Hashtable();
            //ht["InfName"] = InfName;
            //DataTable dt = IbatisHelper.ExecuteQueryForDataTable("queryDTRange",ht);
            //double minX = Convert.ToDouble(dt.Rows[0]["minX"]);
            //double maxX = Convert.ToDouble(dt.Rows[0]["maxX"]);
            //double minY = Convert.ToDouble(dt.Rows[0]["minY"]);
            //double maxY = Convert.ToDouble(dt.Rows[0]["maxY"]);
            //int sRec = (int)((maxX - minX) * (maxY - minY));

            //ht["k"] = k;
            //dt = IbatisHelper.ExecuteQueryForDataTable("queryTopKDT", ht);

            //定义候选点数据结构
            DataTable canGrid = new DataTable();
            canGrid.Columns.Add("fromName");
            canGrid.Columns.Add("CI");
            canGrid.Columns.Add("x");
            canGrid.Columns.Add("y");
            canGrid.Columns.Add("ReceivePW");
            canGrid.Columns.Add("Azimuth");
            canGrid.Columns.Add("Distance");
            //用于记录路测点已经覆盖过的栅格
            HashSet<string> vis = new HashSet<string>();
            List<Point> ps = new List<Point>();

            double grade = 0.001;//选点等级，每隔0.1%减少一定候选栅格
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                int sigma = (int)Math.Floor((double)i/dt.Rows.Count/ grade) + 1;
                sigma = 1;
                Point p = new Point(Convert.ToDouble(dt.Rows[i]["x"]), Convert.ToDouble(dt.Rows[i]["y"]),0);
                Grid3D grid3d = new Grid3D();
                if (!GridHelper.getInstance().PointXYZGrid(p, ref grid3d, canGridL*sigma, canGridH*sigma))
                {
                    continue;
                }
                string key = grid3d.gxid + "_" + grid3d.gyid;
                if (vis.Contains(key))
                {
                    //该栅格已经存在候选路测点，则跳过
                    continue;
                }

                //选点距离约束，至少间隔30m,防止边界邻点出现
                double thDis = 30;
                bool near = false;
                foreach (var gs in ps)
                {
                    double ddis = distanceXY(p.X, p.Y, gs.X,gs.Y);
                    if (ddis < thDis)
                    {
                        near = true;
                        break;
                    }
                }

                if (near)
                {
                    continue;
                }
                vis.Add(key);
                ps.Add(p);
                DataRow thisrow = canGrid.NewRow();
                thisrow["fromName"] = InfName;
                thisrow["x"] = dt.Rows[i]["x"];
                thisrow["y"] = dt.Rows[i]["y"];
                thisrow["ReceivePW"] = Math.Pow(10, (Convert.ToDouble(dt.Rows[i]["RSRP"]) / 10 - 3));
                thisrow["CI"] = dt.Rows[i]["ID"];
                canGrid.Rows.Add(thisrow);
                
            }
            //计算路测点起始参数
            ComputeInitParams(canGrid);
            //入库
            Hashtable ht1 = new Hashtable();
            ht1["fromName"] = InfName;
            IbatisHelper.ExecuteDelete("deletetbRayLoc", ht1);
            IbatisHelper.ExecuteDelete("deletbSelectedPoint", ht1);
            WriteDataToBase(canGrid,100, "tbSelectedPoints");
            Task.Run(() =>
            {
                TarjGen(InfName);
            });
        }
        private void ComputeInitParams(DataTable dtinfo)
        {
            double avgx = 0, avgy = 0;
            for (int i = 0; i < dtinfo.Rows.Count; i++)
            {
                double x = Convert.ToDouble(dtinfo.Rows[i]["x"]);
                double y = Convert.ToDouble(dtinfo.Rows[i]["y"]);
                avgx += x;
                avgy += y;
            }
            avgx /= dtinfo.Rows.Count;
            avgy /= dtinfo.Rows.Count;
            Debug.WriteLine("路测中点:x" + avgx + "路测中点:x" + avgy);
            Geometric.Point endavg = new Geometric.Point(avgx, avgy, 0);

            double minx = double.MaxValue, miny = double.MaxValue, maxx = double.MinValue, maxy = double.MinValue;
            for (int i = 0; i < dtinfo.Rows.Count; i++)
            {
                //Debug.WriteLine(i);
                double x = Convert.ToDouble(dtinfo.Rows[i]["x"]);
                double y = Convert.ToDouble(dtinfo.Rows[i]["y"]);
                Geometric.Point start = new Geometric.Point(x, y, 0);

                double aziavg = LTE.Geometric.GeometricUtilities.getPolarCoord(start, endavg).theta / Math.PI * 180;
                aziavg = GeometricUtilities.ConvertGeometricArithmeticAngle(aziavg + 1);
                //Debug.WriteLine("路测中点计算角度:" + aziavg);
                dtinfo.Rows[i]["Azimuth"] = aziavg;
                dtinfo.Rows[i]["Distance"] = distanceXY(start.X, start.Y, endavg.X, endavg.Y) + 300;
            }
        }
        private double distanceXY(double x, double y, double ex, double ey)
        {
            double deteX = Math.Pow((x - ex), 2);
            double deteY = Math.Pow((y - ey), 2);
            double distance = Math.Sqrt(deteX + deteY);
            return distance;
        }
        public void WriteDataToBase(DataTable tb,int batchSize,string destinationTableName)
        {
            using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
            {
                bcp.BatchSize = batchSize;
                bcp.BulkCopyTimeout = 1000;
                bcp.DestinationTableName = destinationTableName;
                bcp.WriteToServer(tb);
                bcp.Close();
            }
        }


        [AllowAnonymous]
        public void TarjBatchGen()
        {
            string pre = "v1";
            for (int i = 50369; i <= 50458; i++)
            {
                TarjGen(pre + "_" + i);
            }
        }

        public void TarjGen(string InfName)
        {
            RayLocRecordModel rayLoc = new RayLocRecordModel();
            rayLoc.virsource = InfName;
            rayLoc.incrementAngle = 50;
            rayLoc.reflectionNum = 3;
            rayLoc.diffractionNum = 2;
            rayLoc.sideSplitUnit = 3;
            rayLoc.RecordRayLoc();
        }


        [AllowAnonymous]
        public void FeatureBatchGen()
        {
            string pre = "v1";
            for (int i = 50369; i <= 50458; i++)
            {
                Tarj2Grid(pre + "_" + i);
            }
        }

        /// <summary>
        /// 反向跟踪生成统计栅格
        /// </summary>
        /// <param name="virName"></param>
        public void Tarj2Grid(string virName)
        {
            PathAnalysis pa = new PathAnalysis(virName);
            Dictionary<string, List<GridInfo>> togrid = pa.getTogrid();
            Tarj2GridFeature(togrid, virName);
        }

        /// <summary>
        /// 对统计栅格按照训练特征进行统计并入库
        /// </summary>
        /// <param name="togrid"></param>
        /// <param name="inf_name"></param>
        public void Tarj2GridFeature(Dictionary<string, List<GridInfo>> togrid,string inf_name) {

            System.Data.DataTable tb = new System.Data.DataTable();
            tb.Columns.Add("gxid");   
            tb.Columns.Add("gyid");  
            tb.Columns.Add("gzid");  
            tb.Columns.Add("inf_name");     //此版本数据对应的干扰源名称
            tb.Columns.Add("direct_num");   //直射数（不同路测点发出的）
            tb.Columns.Add("reflect_num");  //反射数（所有轨迹）
            tb.Columns.Add("difract_num");  //绕射数（所有轨迹）
            tb.Columns.Add("recp");         //信号接收强度
            tb.Columns.Add("recp_var");     //信号接收总方差
            tb.Columns.Add("direct_var"); //信号接收直射方差
            tb.Columns.Add("reflect_var"); //信号接收反射方差
            tb.Columns.Add("diffract_var"); //信号接收绕射方差
            tb.Columns.Add("rp_num");       //路测点覆盖数目
            tb.Columns.Add("dis");       //距离干扰源的距离的平方

            //获取干扰源的位置
            int id = int.Parse(inf_name.Split('_')[1]);
            Hashtable ht = new Hashtable();
            ht["id"] = id;
            DataTable dt = IbatisHelper.ExecuteQueryForDataTable("queryCellPosById", ht);
            double x = double.Parse(dt.Rows[0]["x"].ToString());
            double y = double.Parse(dt.Rows[0]["y"].ToString());
            double h = double.Parse(dt.Rows[0]["h"].ToString());
            Grid3D tarGrid = new Grid3D();
            GridHelper.getInstance().PointXYZToAccGrid(new Point(x, y, h), ref tarGrid);

            foreach (var item in togrid)
            {
                System.Data.DataRow thisrow = tb.NewRow();
                thisrow["inf_name"] = inf_name;
                string[] strs = item.Key.Split(',');
                thisrow["gxid"] = int.Parse(strs[0]);
                thisrow["gyid"] = int.Parse(strs[1]);
                thisrow["gzid"] = int.Parse(strs[2]);

                int dx = int.Parse(strs[0]) - tarGrid.gxid;
                int dy = int.Parse(strs[1]) - tarGrid.gyid;
                int dz = int.Parse(strs[2]) - tarGrid.gzid;
                thisrow["dis"] = Math.Pow(dx, 2)+ Math.Pow(dy, 2)+ Math.Pow(dz, 2);

                //某一个栅格的统计信息
                Dictionary<string, List<GridInfo>> dic = new Dictionary<string, List<GridInfo>>();

                HashSet<string> directRay = new HashSet<string>();

                int directNum = 0;
                int reflectNUm = 0;
                int difractNum = 0;
                double recp = int.MinValue;
                double recpVar = 0;
                double directVar = 0;
                double reflectVar = 0;
                double diffractVar = 0;

                List<double> recpLs = new List<double>();
                List<double> directRecp = new List<double>();
                List<double> reflectRecp = new List<double>();
                List<double> diffractRecp = new List<double>();
                
                foreach (GridInfo gr in item.Value)
                {
                    
                    //只统计不同路测点的直射数目
                    if (gr.rayType == 0)
                    {
                        directRay.Add(gr.cellid);
                    }
                    if (gr.rayType ==1 || gr.rayType == 2)
                    {
                        reflectNUm++;
                    }
                    if(gr.rayType == 3 || gr.rayType == 4)
                    {
                        difractNum++;
                    }
                    if (!dic.ContainsKey(gr.cellid))
                    {
                        dic[gr.cellid] = new List<GridInfo>();
                    }
                    dic[gr.cellid].Add(gr);
                }

                directNum = directRay.Count;
                thisrow["direct_num"] = directNum;
                thisrow["reflect_num"] = reflectNUm;
                thisrow["difract_num"] = difractNum;
                thisrow["rp_num"] = dic.Keys.Count;

                double distinctRefNum = 0;
                double distinctDiffraNum = 0;

                foreach (var grs in dic)
                {
                    grs.Value.Sort((ox, oy) =>
                    {
                        if (ox is null)
                            throw new ArgumentNullException(nameof(ox));
                        if (oy is null)
                            throw new ArgumentNullException(nameof(oy));
                        if(double.IsNaN(ox.recP)|| double.IsNaN(oy.recP))
                        {
                            throw new ArgumentException(nameof(ox) + " can't contain NaNs.");
                        }
                        int p1 = ox.rayType - oy.rayType;
                        int p2 = ox.raylevel - oy.raylevel;
                        double temp = oy.recP - ox.recP;
                        int p3 = 0;
                        if (temp < 0)
                        {
                            p3 = -1;
                        }
                        if (temp > 0)
                        {
                            p3 = 1;
                        }
                        return p1 != 0 ? p1 : (p2 != 0 ? p2 : p3);
                    });
                    
                    double recpTemp = grs.Value[0].recP;
                    recpLs.Add(recpTemp);
                    recpVar += recpTemp;

                    recp = Math.Max(recpTemp, recp);
                    int type = grs.Value[0].rayType;
                    if (type == 0)
                    {
                        directVar += recpTemp;
                        directRecp.Add(recpTemp);
                    }
                    if (type == 1 || type == 2)
                    {
                        distinctRefNum++;
                        reflectVar += recpTemp;
                        reflectRecp.Add(recpTemp);
                    }
                    if(type == 3 || type == 4)
                    {
                        distinctDiffraNum++;
                        diffractVar += recpTemp;
                        diffractRecp.Add(recpTemp);
                    }
                }
                double aveRecp = recpVar / dic.Keys.Count;
                double aveDirectRecp = directVar / directNum;
                double aveReflectRecp = reflectVar / distinctRefNum;
                double aveDiffractRecp = diffractVar / distinctDiffraNum;

                recpVar = 0;
                reflectVar = 0;
                diffractVar = 0;
                foreach (var rtRecp in recpLs)
                {
                    recpVar += Math.Pow(rtRecp-aveRecp, 2);
                }
                foreach (var rtRecp in directRecp)
                {
                    directVar += Math.Pow(rtRecp - aveDirectRecp, 2);
                }
                foreach (var rtRecp in reflectRecp)
                {
                    reflectVar += Math.Pow(rtRecp - aveReflectRecp, 2);
                }
                foreach (var rtRecp in diffractRecp)
                {
                    diffractVar += Math.Pow(rtRecp - aveDiffractRecp, 2);
                }
                thisrow["recp"] = aveRecp;
                thisrow["recp_var"] = recpVar;
                thisrow["direct_var"] = directVar;
                thisrow["reflect_var"] = reflectVar;
                thisrow["diffract_var"] = diffractVar;
                tb.Rows.Add(thisrow);
            }
            string desTbName = "tbGridFeature";
            //WriteDataToBase(tb, 100, desTbName);
            DataUtil.BCPDataTableImport(tb, desTbName);
        }

        

    }
}
