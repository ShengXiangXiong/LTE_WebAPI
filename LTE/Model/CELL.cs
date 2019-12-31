using System;
namespace LTE.Model
{
    /// <summary>
    /// 实体类CELL 。(属性说明自动提取数据库字段的描述信息)
    /// </summary>
    [Serializable]
    public class CELL
    {
        public CELL()
        { }
        #region
        private int? _id;

        public int? ID
        {
            get { return _id; }
            set { _id = value; }
        }
        private string _cellName;

        public string CellName
        {
            get { return _cellName; }
            set { _cellName = value; }
        }
        private string _btsName;

        public string BtsName
        {
            get { return _btsName; }
            set { _btsName = value; }
        }
        private decimal? _longitude;

        public decimal? Longitude
        {
            get { return _longitude; }
            set { _longitude = value; }
        }
        private decimal? _latitude;

        public decimal? Latitude
        {
            get { return _latitude; }
            set { _latitude = value; }
        }
        private decimal? _x;

        public decimal? x
        {
            get { return _x; }
            set { _x = value; }
        }
        private decimal? _y;

        public decimal? y
        {
            get { return _y; }
            set { _y = value; }
        }
        private decimal? _altitude;

        public decimal? Altitude
        {
            get { return _altitude; }
            set { _altitude = value; }
        }
        private decimal? _antHeight;

        public decimal? AntHeight
        {
            get { return _antHeight; }
            set { _antHeight = value; }
        }
        private double? _azimuth;

        public double? Azimuth
        {
            get { return _azimuth; }
            set { _azimuth = value; }
        }
        private double? _mechTilt;

        public double? MechTilt
        {
            get { return _mechTilt; }
            set { _mechTilt = value; }
        }
        private double? _elecTilt;

        public double? ElecTilt
        {
            get { return _elecTilt; }
            set { _elecTilt = value; }
        }
        private double? _tilt;

        public double? Tilt
        {
            get { return _tilt; }
            set { _tilt = value; }
        }
        private double? _coverageRadius;

        public double? CoverageRadius
        {
            get { return _coverageRadius; }
            set { _coverageRadius = value; }
        }
        private double? _feederLength;

        public double? FeederLength
        {
            get { return _feederLength; }
            set { _feederLength = value; }
        }
        private double? _eirp;

        public double? EIRP
        {
            get { return _eirp; }
            set { _eirp = value; }
        }
        private string _pathlossMode;

        public string PathlossMode
        {
            get { return _pathlossMode; }
            set { _pathlossMode = value; }
        }
        private string _coverageType;

        public string CoverageType
        {
            get { return _coverageType; }
            set { _coverageType = value; }
        }
        private string _netType;

        public string NetType
        {
            get { return _netType; }
            set { _netType = value; }
        }
        private string _comments;

        public string Comments
        {
            get { return _comments; }
            set { _comments = value; }
        }
        private int? _eNodeB;

        public int? eNodeB
        {
            get { return _eNodeB; }
            set { _eNodeB = value; }
        }
        private int? _ci;

        public int? CI
        {
            get { return _ci; }
            set { _ci = value; }
        }
        private string _cellNameChs;

        public string CellNameChs
        {
            get { return _cellNameChs; }
            set { _cellNameChs = value; }
        }
        private int? _earfcn;

        public int? EARFCN
        {
            get { return _earfcn; }
            set { _earfcn = value; }
        }
        private int? _pci;

        public int? PCI
        {
            get { return _pci; }
            set { _pci = value; }
        }

        private double? _KDis;
        public double? KDis
        {
            get { return _KDis; }
            set { _KDis = value; }
        }

        private double? _MaxDis;
        public double? MaxDis
        {
            get { return _MaxDis; }
            set { _MaxDis = value; }
        }
        private double? _MinDis;
        public double? MinDis
        {
            get { return _MinDis; }
            set { _MinDis = value; }
        }

        #endregion

    }
}

