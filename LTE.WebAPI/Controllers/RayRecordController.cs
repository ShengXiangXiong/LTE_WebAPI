using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LTE.Model;
using LTE.Utils;
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
            Result res = ray.RecordRayLoc(load:true);
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

        ///// <summary>
        ///// 记录用于系数校正的射线(批量模式),生成【在tbRayAdjRange范围内所有路测数据对应的主小区&&小区在最大范围内】的小区轨迹
        ///// 目前还要求小区满足路测数>500且在筛选出的排序列表上下界范围内的小区。
        ///// </summary>
        ///// <param name="ray">界面输入参数</param>
        ///// <returns></returns>
        //[HttpPost]
        //[TaskLoadInfo(taskName = "射线记录Adj", type = TaskType.RayRecordAdjBatchMode)]
        //public Result PostRayRecordAdjBatchMode([FromBody]RayRecordAdjBatchModeModel rayBatchMode)
        //{
        //    return rayBatchMode.rayAdjRecordBatch();
        //}

        [HttpPost]
        [TaskLoadInfo(taskName = "系数校正射线记录", type = TaskType.RayRecordAdjBatchMode)]
        public Result PostRayRecordAdjBatchMode([FromBody]RayRecordAdjModel ray)
        {
            // 初始化进度信息
            LoadInfo loadInfo = new LoadInfo();

            try
            {
                //初始条件检查
                if (!DataCheck.checkInitFinished())
                {
                    loadInfo.breakdown = true;
                    loadInfo.loadBreakDown();
                    return new Result(false, "系数校正射线记录失败，请先完成场景建模。");
                }

                //cells获得tbRayAdjRange范围内所有路测数据的主小区,按小区名称排序
                DataTable cellsTb = DB.IbatisHelper.ExecuteQueryForDataTable("getAreaDTMainCellName", null);
                //获得在最大范围内,并且Tilt、Azimuth非空的小区名称集合，必须使用经纬度范围，因为若超出经纬度范围小区表内小区投影坐标为null
                DataTable cellsInMaxArea = DB.IbatisHelper.ExecuteQueryForDataTable("getCellsInMaxArea", null);
                HashSet<string> cellsInMaxAreaSet = new HashSet<string>();
                for (int i = 0; i < cellsInMaxArea.Rows.Count; ++i)
                {
                    cellsInMaxAreaSet.Add(cellsInMaxArea.Rows[i]["CellName"].ToString());
                }
                //获得所有tbRayAdj表中轨迹的小区，按小区名称排序
                DataTable rayAdjCellsTb = DB.IbatisHelper.ExecuteQueryForDataTable("getRayAdjCellNames", null);
                HashSet<string> rayAdjCells = new HashSet<string>();
                for (int i = 0; i < rayAdjCellsTb.Rows.Count; ++i)
                {
                    rayAdjCells.Add(rayAdjCellsTb.Rows[i]["CellName"].ToString());
                }
                //获得每个小区对应的理论覆盖半径
                Dictionary<string, double> cellCoverageRadius = new Dictionary<string, double>();
                for (int i = 0; i < cellsInMaxArea.Rows.Count; ++i)
                {
                    cellCoverageRadius[cellsInMaxArea.Rows[i]["CellName"].ToString()] = double.Parse(cellsInMaxArea.Rows[i]["CoverageRadius"].ToString());
                }

                //cells中最终存放范围内所有路测对应的主小区&&在tbGridRange表的最大范围内的小区名称 &&不在tbRayAdj表中轨迹的小区(即略过已生成过轨迹的小区)
                List<string> cells = new List<string>();//范围内所有路测数据的主小区名称
                for (int i = 0; i < cellsTb.Rows.Count; ++i)
                {
                    string cellName = cellsTb.Rows[i][0].ToString();
                    if (cellName != "" && cellName != null && cellsInMaxAreaSet.Contains(cellName) && !rayAdjCells.Contains(cellName))
                    {
                        cells.Add(cellsTb.Rows[i][0].ToString());
                    }
                }


                //得到一个字典：每个小区的路测数据条数
                Dictionary<string, long> dtCntOfCellDic = new Dictionary<string, long>();//存储每个小区的路测数据条数
                DataTable dtDataCntOfCellTb = DB.IbatisHelper.ExecuteQueryForDataTable("getDtCntOfCell", null);
                for (int i = 0; i < dtDataCntOfCellTb.Rows.Count; ++i)
                {
                    string cellName = dtDataCntOfCellTb.Rows[i][0].ToString();
                    long dtCntOfCell = long.Parse(dtDataCntOfCellTb.Rows[i][1].ToString());
                    dtCntOfCellDic[cellName] = dtCntOfCell;
                }

                Result result = null;

                // 跑上界和下界之间的小区
                int lowBound = 0;
                int highBound = cells.Count;

                // 初始化进度信息
                loadInfo.loadCountAdd(cells.Count);
                string fail = "";
                for (int i = lowBound; i < highBound; i++)//计算每个小区的轨迹，每个小区开三个子进程       
                {
                    int dtCntThreshold = 500;//如果小区路测数量小于dtCntThreshold，就不生成该小区轨迹
                    if (dtCntOfCellDic[cells[i]] < dtCntThreshold)
                    {
                        continue;
                    }

                    ray.cellName = cells[i];
                    if (cellCoverageRadius.ContainsKey(ray.cellName))
                    {//若Cell表该小区有理论覆盖半径，则采用理论覆盖半径
                        ray.distance = cellCoverageRadius[ray.cellName];
                    }
                    result = ray.rayRecord();//todo 区分两者result

                    //处理进度信息
                    if (result.ok)
                    {
                        loadInfo.loadHashAdd(1);
                    }
                    else
                    {
                        fail += ray.cellName + "\t";
                    }
                }

                //完成后的返回结果判断
                if (loadInfo.cnt < loadInfo.count)
                {
                    loadInfo.breakdown = true;
                    loadInfo.loadBreakDown();
                    return new Result(false, fail + "系数校正射线计算失败");
                }
                loadInfo.loadFinish();
                return new Result(true, "系数校正射线计算完成");
            }
            catch (Exception)
            {
                loadInfo.breakdown = true;
                loadInfo.loadBreakDown();
                return new Result(false, "系数校正射线计算失败");
            }
        }
    }
}
