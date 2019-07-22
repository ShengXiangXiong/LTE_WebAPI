using System;
using System.Collections;

using System.Linq;
using System.Text;
using System.Data;

using LTE.Geometric;
using LTE.GIS;
using LTE.DB;

namespace LTE.InternalInterference
{
    public enum CellType
    {
        GSM900,
        GSM1800,
    };

    /// <summary>
    /// 用于传递共享内存
    /// </summary>
    public struct CellInfoStruct
    {
        public Point SourcePoint;
        public string SourceName;
        public int eNodeB;
        public int CI;

        public double Azimuth;
        public double Inclination;
        public double RayAzimuth;
        public double RayInclination;

        public float directCoefficient;
        public float reflectCoefficient;
        public float diffracteCoefficient;
        public float diffracteCoefficient2;

        public CellType cellType;
        public int frequncy;
        public double EIRP;
    }

    public class CellInfo : SourceInfo
    {
        //小区需要
        public CellType cellType { get; set; }
        public int frequncy { get; set; }
        public double EIRP { get; set; }


        public CellInfo(string cellName, int enodeb, int ci, float directCoefficient, float reflectCoefficient, float diffracteCoefficient, float diffracteCoefficient2)
            : base(cellName, enodeb, ci, directCoefficient, reflectCoefficient, diffracteCoefficient, diffracteCoefficient2)
        {
            this.constructSourceInfo();
        }

        public CellInfo(string cellName, int enodeb, int ci)
            : this(cellName, enodeb, ci, 0, 1, 1, 1)
        {
        }

        public CellInfo()
        {
        }

        public CellInfo(CellInfo c)
            : base(c)
        {
            this.cellType = c.cellType;
            this.frequncy = c.frequncy;
            this.EIRP = c.EIRP;
        }

        public override SourceInfo clone()
        {
            CellInfo c = new CellInfo(this.SourceName, this.eNodeB, this.CI, this.directCoefficient, this.reflectCoefficient, this.diffracteCoefficient, this.diffracteCoefficient2);
            c.cellType = this.cellType;
            c.frequncy = c.frequncy;
            c.EIRP = this.EIRP;
            c.SourcePoint = this.SourcePoint.clone();
            c.Azimuth = this.Azimuth;
            c.Inclination = this.Inclination;
            c.RayAzimuth = this.RayAzimuth;
            c.RayInclination = this.RayInclination;
            return c;
        }

        public CellInfoStruct convertToStruct()
        {
            CellInfoStruct cs = new CellInfoStruct();
            cs.SourcePoint = this.SourcePoint.clone();
            cs.SourceName = this.SourceName;
            cs.eNodeB = this.eNodeB;
            cs.CI = this.CI;
            cs.Azimuth = this.Azimuth;
            cs.Inclination = this.Inclination;
            cs.RayAzimuth = this.RayAzimuth;
            cs.RayInclination = this.RayInclination;
            cs.diffracteCoefficient = this.diffracteCoefficient;
            cs.reflectCoefficient = this.reflectCoefficient;
            cs.diffracteCoefficient = this.diffracteCoefficient;
            cs.diffracteCoefficient2 = this.diffracteCoefficient2;
            return cs;
        }

        public static CellInfo convertFromStruct(CellInfoStruct cs)
        {
            CellInfo c = new CellInfo();
            c.SourcePoint = cs.SourcePoint.clone();
            c.SourceName = cs.SourceName;
            c.eNodeB = cs.eNodeB;
            c.CI = cs.CI;
            c.Azimuth = cs.Azimuth;
            c.Inclination = cs.Inclination;
            c.RayAzimuth = cs.RayAzimuth;
            c.RayInclination = cs.RayInclination;
            c.diffracteCoefficient = cs.diffracteCoefficient;
            c.reflectCoefficient = cs.reflectCoefficient;
            c.diffracteCoefficient = cs.diffracteCoefficient;
            c.diffracteCoefficient2 = cs.diffracteCoefficient2;
            return c;
        }

        /// <summary>
        /// 构造小区信息，其中小区的坐标信息中默认为投影坐标, 使用了arcgis
        /// </summary>
        internal override void constructSourceInfo()
        {
            DataTable cellInfoTable;
            switch (this.cellType)
            {
                case CellType.GSM900:                              //数据库
                case CellType.GSM1800:
                    {
                        Hashtable ht = new Hashtable();
                        //ht["eNodeB"] = this.eNodeB;
                        //ht["CI"] = this.CI;
                        ht["cellName"] = this.SourceName;
                        cellInfoTable = IbatisHelper.ExecuteQueryForDataTable("GetGSMCellInfo", ht);
                        if (cellInfoTable != null)
                        {
                            DataRow row = cellInfoTable.Rows[0];

                            this.Azimuth = double.Parse(row["Azimuth"].ToString());
                            this.Inclination = double.Parse(row["Tilt"].ToString());
                            this.Inclination = this.Inclination > 0 ? this.Inclination : 7;
                            this.EIRP = double.Parse(row["EIRP"].ToString());

                            double x, y, z;
                            x = double.Parse(row["x"].ToString());
                            y = double.Parse(row["y"].ToString());
                            z = double.Parse(row["AntHeight"].ToString());
                            double z1 = double.Parse(row["Altitude"].ToString());
                            this.SourcePoint = new Point(x, y, z + z1);

                            string cellType = row["NetType"].ToString();
                            if (cellType == "GSM900" || cellType == "GSM900小区")
                                this.cellType = CellType.GSM900;
                            if (cellType == "GSM1800" || cellType == "GSM1800小区")
                                this.cellType = CellType.GSM1800;

                            this.frequncy = Convert.ToInt32(row["EARFCN"]);
                        }

                        break;
                    }
                default:
                    break;

            }
        }

    }
}
