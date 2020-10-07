using System;
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

namespace LTE.WebAPI.Controllers
{
    public class ModelDataGenController : ApiController
    {
        [AllowAnonymous]
        [TaskLoadInfo(taskName = "干扰区域数据仿真——模拟路测点生成", type = TaskType.DataMock)]
        public Result roadPointMockGen([FromBody]DataRange dataRange)
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
                    cellRays[cnt].calc();
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

            double maxBh = 100;//最大建筑物高度
            int radius = 2000;//干扰源覆盖半径
            String tarBaseName = "测试干扰源基站_";
            List<CELL> cells = new List<CELL>();
            int batch = 200;
            int cnt = 0;

            //计算测试数据总数
            int lx = (int)Math.Ceiling((pMax.X - pMin.X) / dataRange.tarGridX);
            int ly = (int)Math.Ceiling((pMax.Y - pMin.Y) / dataRange.tarGridY);
            int lz = (int)Math.Ceiling(maxBh/ dataRange.tarGridH)-1;
            long uidBatch = long.Parse((lx*ly*lz).ToString());
            String dbName = "CELL";
            int initOff = 50000;
            int uid = (int)UIDHelper.GenUIdByRedis(dbName,uidBatch)+initOff;

            for (double x = pMin.X; x < pMax.X; x += dataRange.tarGridX)
            {
                for (double y = pMin.Y; y < pMax.Y; y+=dataRange.tarGridY)
                {
                    for (double z = 30; z < maxBh; z+=30)
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
            IbatisHelper.ExecuteInsert("CELL_BatchInsert", cells);

            return res;
        }



        public void WriteDt(DataRange dataRange)
        {
            RedisMq.subscriber.Subscribe("cover2db_finish", (channel, message) => {

                Hashtable ht = new Hashtable();
                DataTable dtable = new DataTable();

                ht["eNodeB"] = message;
                DataTable dt = DB.IbatisHelper.ExecuteQueryForDataTable("qureyMockDT", ht);
                dtable.Columns.Add("x", System.Type.GetType("System.Decimal"));
                dtable.Columns.Add("y", System.Type.GetType("System.Decimal"));
                dtable.Columns.Add("Lon", System.Type.GetType("System.Decimal"));
                dtable.Columns.Add("Lat", System.Type.GetType("System.Decimal"));
                dtable.Columns.Add("RSRP", System.Type.GetType("System.Double"));
                dtable.Columns.Add("InfName", System.Type.GetType("System.String"));
                dtable.Columns.Add("DtType", System.Type.GetType("System.String"));

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    var row = dt.Rows[i];
                    int gxid = (int)row["GXID"];
                    int gyid = (int)row["GYID"];
                    double rsrp = (double)row["ReceivedPowerdbm"];
                    Point geo = GridHelper.getInstance().GridToGeo(gxid, gyid);
                    Point proj = GridHelper.getInstance().GridToXY(gxid, gyid);
                    DataRow thisrow = dtable.NewRow();
                    thisrow["x"] = proj.X;
                    thisrow["y"] = proj.Y;
                    thisrow["Lon"] = geo.X;
                    thisrow["Lat"] = geo.Y;
                    thisrow["RSRP"] = rsrp;
                    thisrow["InfName"] = dataRange.infAreaId;
                    thisrow["DtType"] = "mock";
                    dtable.Rows.Add(thisrow);
                }
                DataUtil.BCPDataTableImport(dtable, "tbUINTF");
            });
        }
        

    }
}
