using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LTE.DB;
using System.Data;

namespace LTE.InternalInterference.Grid
{
    /// <summary>
    /// 网格立体覆盖类，用于记录射线跟踪结果，与数据库交互
    /// </summary>
    public class GridCover
    {
        private DataTable groundCover;
        private DataTable buildingCover;
        public int ng, nb;
        private static GridCover instance = null;

        public static GridCover getInstance()
        {
            if (instance == null)
            {
                instance = new GridCover();
            }
            return instance;
        }

        public GridCover()
        {
            ng = nb = 0;
            DataColumn[] keys = new DataColumn[4];
            keys[0] = new DataColumn("GXID", System.Type.GetType("System.Int16"));
            keys[1] = new DataColumn("GYID", System.Type.GetType("System.Int16"));
            keys[2] = new DataColumn("eNodeB", System.Type.GetType("System.Int32"));
            keys[3] = new DataColumn("CI", System.Type.GetType("System.Int32"));

            this.groundCover = new DataTable();
            this.groundCover.Columns.Add(keys[0]);
            this.groundCover.Columns.Add(keys[1]);
            this.groundCover.Columns.Add(keys[2]);
            this.groundCover.Columns.Add(keys[3]);
            this.groundCover.Columns.Add("FieldIntensity", System.Type.GetType("System.Double"));
            this.groundCover.Columns.Add("DirectPwrNum", System.Type.GetType("System.Int32"));
            this.groundCover.Columns.Add("DirectPwrW", System.Type.GetType("System.Double"));
            this.groundCover.Columns.Add("MaxDirectPwrW", System.Type.GetType("System.Double"));
            this.groundCover.Columns.Add("RefPwrNum", System.Type.GetType("System.Int32"));
            this.groundCover.Columns.Add("RefPwrW", System.Type.GetType("System.Double"));
            this.groundCover.Columns.Add("MaxRefPwrW", System.Type.GetType("System.Double"));
            this.groundCover.Columns.Add("RefBuildingID", System.Type.GetType("System.String"));
            this.groundCover.Columns.Add("DiffNum", System.Type.GetType("System.Int32"));
            this.groundCover.Columns.Add("DiffPwrW", System.Type.GetType("System.Double"));
            this.groundCover.Columns.Add("MaxDiffPwrW", System.Type.GetType("System.Double"));
            this.groundCover.Columns.Add("DiffBuildingID", System.Type.GetType("System.String"));
            this.groundCover.Columns.Add("BTSGridDistance", System.Type.GetType("System.Double"));
            this.groundCover.Columns.Add("ReceivedPowerW", System.Type.GetType("System.Double"));
            this.groundCover.Columns.Add("ReceivedPowerdbm", System.Type.GetType("System.Double"));
            this.groundCover.Columns.Add("PathLoss", System.Type.GetType("System.Double"));
            this.groundCover.PrimaryKey = keys;

            keys = new DataColumn[5];
            keys[0] = new DataColumn("GXID", System.Type.GetType("System.Int16"));
            keys[1] = new DataColumn("GYID", System.Type.GetType("System.Int16"));
            keys[2] = new DataColumn("Level", System.Type.GetType("System.Byte"));
            keys[3] = new DataColumn("eNodeB", System.Type.GetType("System.Int32"));
            keys[4] = new DataColumn("CI", System.Type.GetType("System.Int32"));

            this.buildingCover = new DataTable();
            this.buildingCover.Columns.Add(keys[0]);
            this.buildingCover.Columns.Add(keys[1]);
            this.buildingCover.Columns.Add(keys[2]);
            this.buildingCover.Columns.Add(keys[3]);
            this.buildingCover.Columns.Add(keys[4]);
            this.buildingCover.Columns.Add("FieldIntensity", System.Type.GetType("System.Double"));
            this.buildingCover.Columns.Add("DirectPwrNum", System.Type.GetType("System.Int32"));
            this.buildingCover.Columns.Add("DirectPwrW", System.Type.GetType("System.Double"));
            this.buildingCover.Columns.Add("MaxDirectPwrW", System.Type.GetType("System.Double"));
            this.buildingCover.Columns.Add("RefPwrNum", System.Type.GetType("System.Int32"));
            this.buildingCover.Columns.Add("RefPwrW", System.Type.GetType("System.Double"));
            this.buildingCover.Columns.Add("MaxRefPwrW", System.Type.GetType("System.Double"));
            this.buildingCover.Columns.Add("RefBuildingID", System.Type.GetType("System.String"));
            this.buildingCover.Columns.Add("DiffNum", System.Type.GetType("System.Int32"));
            this.buildingCover.Columns.Add("DiffPwrW", System.Type.GetType("System.Double"));
            this.buildingCover.Columns.Add("MaxDiffPwrW", System.Type.GetType("System.Double"));
            this.buildingCover.Columns.Add("DiffBuildingID", System.Type.GetType("System.String"));
            this.buildingCover.Columns.Add("TransNum", System.Type.GetType("System.Int32"));
            this.buildingCover.Columns.Add("TransPwrW", System.Type.GetType("System.Double"));
            this.buildingCover.Columns.Add("MaxTransPwrW", System.Type.GetType("System.Double"));
            this.buildingCover.Columns.Add("TransmitBuildingID", System.Type.GetType("System.String"));
            this.buildingCover.Columns.Add("BTSGridDistance", System.Type.GetType("System.Double"));
            this.buildingCover.Columns.Add("ReceivedPowerW", System.Type.GetType("System.Double"));
            this.buildingCover.Columns.Add("ReceivedPowerdbm", System.Type.GetType("System.Double"));
            this.buildingCover.Columns.Add("PathLoss", System.Type.GetType("System.Double"));
            this.buildingCover.PrimaryKey = keys;
        }

        public DataRow getGCNR()
        {
            return groundCover.NewRow();
        }

        public DataRow getBCNR()
        {
            return buildingCover.NewRow();
        }

        public void addGC(DataRow dr)
        {
            //lock (syncGC)
            {
                groundCover.Rows.Add(dr);
            }
        }

        public void addBC(DataRow dr)
        {
            //lock (syncBC)
            {
                buildingCover.Rows.Add(dr);
            }
        }

        public DataRow[] getGCByGrid(String condition)
        {
            DataRow[] ret;
            //lock (syncGC)
            {
                ret = groundCover.Select(condition);
            }
            return ret;
        }

        public DataRow[] getBCByGrid(String condition)
        {
            DataRow[] ret;
            //lock (syncBC)
            {
                ret = buildingCover.Select(condition);
            }
            return ret;
        }

        public void deleteGroundCover(Hashtable ht)
        {
            IbatisHelper.ExecuteDelete("deleteSpecifiedCelltbGrids", ht);
        }

        public void deleteBuildingCover(Hashtable ht)
        {
            IbatisHelper.ExecuteDelete("deleteSpecifiedCelltbBuildingGrid3Ds", ht);
        }

        public void wirteGroundCover(Hashtable ht)
        {
            //IbatisHelper.ExecuteDelete("deleteSpecifiedCelltbGrids", ht);
            DataUtil.BCPDataTableImport(this.groundCover, "tbGridPathloss");
            this.groundCover.Clear();
        }

        public void writeBuildingCover(Hashtable ht)
        {
            //IbatisHelper.ExecuteDelete("deleteSpecifiedCelltbBuildingGrid3Ds", ht);
            DataUtil.BCPDataTableImport(this.buildingCover, "tbBuildingGridPathloss");
            this.buildingCover.Clear();
        }

        public void convertToDt(Dictionary<string, GridStrength> GridStrengths)
        {
            DataRow dr;
            GridStrength gs;
            foreach (KeyValuePair<string, GridStrength> kv in GridStrengths)
            {
                gs = kv.Value;
                if (gs.Level == 0)
                {//地面栅格
                    ng++;
                    dr = groundCover.NewRow();
                    dr["GXID"] = gs.GXID;
                    dr["GYID"] = gs.GYID;
                    dr["eNodeB"] = gs.eNodeB;
                    dr["CI"] = gs.CI;
                    dr["DirectPwrNum"] = gs.DirectNum;
                    dr["DirectPwrW"] = gs.DirectPwrW;
                    dr["MaxDirectPwrW"] = gs.MaxDirectPwrW;
                    dr["RefPwrNum"] = gs.RefNum;
                    dr["RefPwrW"] = gs.RefPwrW;
                    dr["MaxRefPwrW"] = gs.MaxRefPwrW;
                    dr["RefBuildingID"] = gs.RefBuildingID;
                    dr["DiffNum"] = gs.DiffNum;
                    dr["DiffPwrW"] = gs.DiffPwrW;
                    dr["MaxDiffPwrW"] = gs.MaxDiffPwrW;
                    dr["DiffBuildingID"] = gs.DiffBuildingID;
                    dr["BTSGridDistance"] = gs.BTSGridDistance;
                    dr["ReceivedPowerW"] = gs.ReceivedPowerW;
                    dr["ReceivedPowerdbm"] = gs.ReceivedPowerdbm;
                    dr["PathLoss"] = gs.PathLoss;

                    groundCover.Rows.Add(dr);
                }
                else
                {//建筑物栅格
                    nb++;
                    dr = buildingCover.NewRow();
                    dr["GXID"] = gs.GXID;
                    dr["GYID"] = gs.GYID;
                    dr["Level"] = gs.Level;
                    dr["eNodeB"] = gs.eNodeB;
                    dr["CI"] = gs.CI;
                    dr["DirectPwrNum"] = gs.DirectNum;
                    dr["DirectPwrW"] = gs.DirectPwrW;
                    dr["MaxDirectPwrW"] = gs.MaxDirectPwrW;
                    dr["RefPwrNum"] = gs.RefNum;
                    dr["RefPwrW"] = gs.RefPwrW;
                    dr["MaxRefPwrW"] = gs.MaxRefPwrW;
                    dr["RefBuildingID"] = gs.RefBuildingID;
                    dr["DiffNum"] = gs.DiffNum;
                    dr["DiffPwrW"] = gs.DiffPwrW;
                    dr["MaxDiffPwrW"] = gs.MaxDiffPwrW;
                    dr["DiffBuildingID"] = gs.DiffBuildingID;
                    dr["TransNum"] = gs.TransNum;
                    dr["TransPwrW"] = gs.TransPwrW;
                    dr["MaxTransPwrW"] = gs.MaxTransPwrW;
                    dr["TransmitBuildingID"] = gs.TransmitBuildingID;
                    dr["BTSGridDistance"] = gs.BTSGridDistance;
                    dr["ReceivedPowerW"] = gs.ReceivedPowerW;
                    dr["ReceivedPowerdbm"] = gs.ReceivedPowerdbm;
                    dr["PathLoss"] = gs.PathLoss;

                    buildingCover.Rows.Add(dr);
                }
            }
        }

        public void clearGround()
        {
            this.groundCover.Clear();
        }

        public void clearBuilding()
        {
            this.buildingCover.Clear();
        }
        
    }

}
