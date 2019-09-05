namespace LTE.WebAPI.Models
{
	#region CELL
	/// <summary>
	/// 数据库 [NJCover3D] 中表 [dbo.CELL] 的实体类.
	/// </summary>
	/// 创 建 人: {xsx}
	/// 创建日期: 2019/9/2
	/// 修 改 人:
	/// 修改日期:
	/// 修改内容:
	/// 版    本: 1.0.0
	using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    [Table("CELL")]
    public partial class CELL
    {
        // Instantiate empty CELL for inserting
        public CELL() { }

        public override string ToString()
        {
            return "ID:" + ID + " CellName:" + CellName + " BtsName:" + BtsName + " Longitude:" + Longitude + " Latitude:" + Latitude + " x:" + x + " y:" + y + " Altitude:" + Altitude + " AntHeight:" + AntHeight + " Azimuth:" + Azimuth + " MechTilt:" + MechTilt + " ElecTilt:" + ElecTilt + " Tilt:" + Tilt + " CoverageRadius:" + CoverageRadius + " FeederLength:" + FeederLength + " EIRP:" + EIRP + " PathlossMode:" + PathlossMode + " CoverageType:" + CoverageType + " NetType:" + NetType + " Comments:" + Comments + " eNodeB:" + eNodeB + " CI:" + CI + " CellNameChs:" + CellNameChs + " EARFCN:" + EARFCN + " PCI:" + PCI;
        }

        #region Public Properties
        public int? ID { get; set; }

        [StringLength(50)]
        public string CellName { get; set; }

        [StringLength(50)]
        public string BtsName { get; set; }

        public decimal Longitude { get; set; }

        public decimal Latitude { get; set; }

        public decimal x { get; set; }

        public decimal y { get; set; }

        public decimal Altitude { get; set; }

        public decimal AntHeight { get; set; }

        public double Azimuth { get; set; }

        public double MechTilt { get; set; }

        public double ElecTilt { get; set; }

        public double Tilt { get; set; }

        public double CoverageRadius { get; set; }

        public double FeederLength { get; set; }

        public double EIRP { get; set; }

        [StringLength(50)]
        public string PathlossMode { get; set; }

        [StringLength(50)]
        public string CoverageType { get; set; }

        [StringLength(50)]
        public string NetType { get; set; }

        [StringLength(50)]
        public string Comments { get; set; }

        public int? eNodeB { get; set; }

        public int? CI { get; set; }

        [StringLength(50)]
        public string CellNameChs { get; set; }

        public int? EARFCN { get; set; }

        public int? PCI { get; set; }
        #endregion
    }
    #endregion
}
