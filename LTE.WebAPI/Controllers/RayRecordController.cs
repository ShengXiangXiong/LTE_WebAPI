using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LTE.WebAPI.Attributes;
using LTE.WebAPI.Models;

namespace LTE.WebAPI.Controllers
{
    public class RayRecordController : ApiController
    {
        /// <summary>
        /// 记录用于干扰定位的射线
        /// </summary>
        /// <param name="ray">界面输入参数</param>
        /// <returns></returns>
        [HttpPost]
        [TaskLoadInfo(taskName = "射线记录Loc", type = TaskType.RayRecordLoc)]
        public Result PostRayRecordLoc([FromBody]RayLocRecordModel ray)
        {
            Result res = ray.RecordRayLoc(1);
            return res;
        }

        /// <summary>
        /// 记录用于系数校正的射线
        /// </summary>
        /// <param name="ray">界面输入参数</param>
        /// <returns></returns>
        [HttpPost]
        [TaskLoadInfo(taskName = "射线记录Adj", type = TaskType.RayRecordAdj)]
        public Result PostRayRecordAdj([FromBody]RayRecordAdjModel ray)
        {   
            return ray.rayRecord();
        }

        /// <summary>
        /// 记录用于系数校正的射线(批量模式),生成【在tbRayAdjRange范围内所有路测数据对应的主小区&&小区在最大范围内】的小区轨迹
        /// 目前还要求小区满足路测数>500且在筛选出的排序列表上下界范围内的小区。
        /// </summary>
        /// <param name="ray">界面输入参数</param>
        /// <returns></returns>
        [HttpPost]
        public Result PostRayRecordAdjBatchMode([FromBody]RayRecordAdjModel ray)
        {
            ////更新数据库中待生成轨迹所选区域范围,经纬度，todo
            //double areaMaxLon = 118.979600;
            //double areaMaxLat = 32.373100;
            //double areaMinLon = 118.564100;
            //double areaMinLat = 31.863000; 
            //Hashtable paramHt = new Hashtable();
            //paramHt["areaMaxLon"] = areaMaxLon;
            //paramHt["areaMaxLat"] = areaMaxLat;
            //paramHt["areaMinLon"] = areaMinLon;
            //paramHt["areaMinLat"] = areaMinLat;


            //cells获得tbRayAdjRange范围内所有路测数据的主小区
            DataTable cellsTb = DB.IbatisHelper.ExecuteQueryForDataTable("getAreaDTMainCellName", null);
            //获得在最大范围内的小区名称集合，必须使用经纬度范围，因为若超出经纬度范围小区表内小区投影坐标为null。
            DataTable cellsInMaxArea= DB.IbatisHelper.ExecuteQueryForDataTable("getCellsInMaxArea", null);
            HashSet<string> cellsInMaxAreaSet = new HashSet<string>();
            for (int i = 0; i < cellsInMaxArea.Rows.Count; ++i) {
                cellsInMaxAreaSet.Add(cellsInMaxArea.Rows[i]["CellName"].ToString());
            }

            //cells中最终存放范围内所有路测对应的主小区&&在tbGridRange表的最大范围内的小区名称
            List<string> cells = new List<string>();//范围内所有路测数据的主小区名称
            for (int i = 0; i < cellsTb.Rows.Count; ++i)
            {
                string cellName = cellsTb.Rows[i][0].ToString();
                if (cellName != "" && cellName != null && cellsInMaxAreaSet.Contains(cellName))
                {
                    cells.Add(cellsTb.Rows[i][0].ToString());
                }
            }



            //得到一个字典：每个小区的路测数据条数
            Dictionary<string, long> dtCntOfCellDic= new Dictionary<string, long>();//存储每个小区的路测数据条数
            DataTable dtDataCntOfCellTb = DB.IbatisHelper.ExecuteQueryForDataTable("getDtCntOfCell", null);
            for (int i = 0; i < dtDataCntOfCellTb.Rows.Count; ++i)
            {
                string cellName = dtDataCntOfCellTb.Rows[i][0].ToString();
                long dtCntOfCell = long.Parse( dtDataCntOfCellTb.Rows[i][1].ToString());
                dtCntOfCellDic[cellName] = dtCntOfCell;
            }

            Result result = null;

            // 跑上界和下界之间的小区
            int lowBound = 0;   
            int highBound = 132; 
            for (int i=lowBound; i<=highBound;i++) {//计算每个小区的轨迹，每个小区开三个子进程              
                int dtCntThreshold = 500;//如果小区路测数量小于dtCntThreshold，就不生成该小区轨迹
                if (dtCntOfCellDic[cells[i]] < dtCntThreshold) {
                    continue;
                }
                ray.cellName = cells[i];
                result = ray.rayRecord();
            }
            return result;
        }
    }
}
