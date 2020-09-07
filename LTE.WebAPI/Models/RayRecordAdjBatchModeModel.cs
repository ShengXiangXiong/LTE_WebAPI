using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace LTE.WebAPI.Models
{
    public class RayRecordAdjBatchModeModel
    {
        /// <summary>
        /// 小区覆盖半径
        /// </summary>
        public double distance { get; set; }

        /// <summary>
        /// 以方位角为中心，两边扩展的角度
        /// </summary>
        public double incrementAngle { get; set; }

        /// <summary>
        /// 线程个数
        /// </summary>
        public int threadNum { get; set; }

        /// <summary>
        /// 反射次数
        /// </summary>
        public int reflectionNum { get; set; }

        /// <summary>
        /// 绕射次数
        /// </summary>
        public int diffractionNum { get; set; }

        /// <summary>
        /// 建筑物棱边绕射点间隔
        /// </summary>
        public double diffPointsMargin { get; set; }

        /// <summary>
        /// 计算立体覆盖
        /// </summary>
        public bool computeIndoor { get; set; }

        /// <summary>
        /// 计算棱边绕射
        /// </summary>
        public bool computeDiffrac { get; set; }

        /// <summary>
        /// 直射校正系数
        /// </summary>
        public float directCoeff { get; set; }

        /// <summary>
        /// 反射校正系数
        /// </summary>
        public float reflectCoeff { get; set; }

        /// <summary>
        /// 绕射校正系数
        /// </summary>
        public float diffractCoeff { get; set; }

        /// <summary>
        /// 菲涅尔绕射校正系数
        /// </summary>
        public float diffractCoeff2 { get; set; }

        /// <summary>
        /// tbRayAdjRange范围
        /// </summary>
        public double minLongitude { get; set; }
        public double minLatitude { get; set; }
        public double maxLongitude { get; set; }
        public double maxLatitude { get; set; }

        public Result rayAdjRecordBatch() {
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
            for (int i = lowBound; i < highBound; i++)
            {//计算每个小区的轨迹，每个小区开三个子进程              
                int dtCntThreshold = 500;//如果小区路测数量小于dtCntThreshold，就不生成该小区轨迹
                if (dtCntOfCellDic[cells[i]] < dtCntThreshold)
                {
                    continue;
                }

                //RayRecordAdjModel ray = new RayRecordAdjModel(this);
                //ray.cellName = cells[i];
                //if (cellCoverageRadius.ContainsKey(ray.cellName))
                //{//若Cell表该小区有理论覆盖半径，则采用理论覆盖半径
                //    ray.distance = cellCoverageRadius[ray.cellName];
                //}
                //result = ray.rayRecord();//todo 区分两者result
            }
            return result;
        }
    }
}