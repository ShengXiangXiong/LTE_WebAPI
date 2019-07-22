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
        #region Model
        private int _id;
        private string _cellname;
        private string _cellnamechs;
        private int _lac;
        private int _ci;

        private string _btsname;
        private string _bsc;
        private string _msc;
        private string _vendor;
        private int? _EARFCN;
        private string _tchno;
        private int? _bsic;
        private int? _trxnumber;

        private decimal? _btslongitude;
        private decimal? _btslatitude;
        private decimal? _longitude;
        private decimal? _latitude;
        private decimal? _x;
        private decimal? _y;
        private int? _antfloor;
        private string _btssite;
        private decimal? _btsaltitude;
        private string _admiRegion;
        private decimal? _antheight;
        private double? _azimuth;
        private double? _mechtilt;
        private double? _electilt;
        private double? _tilt;
        private double? _bspwrb;
        private double? _bspwrt;
        private double? _feederlength;
        private double? _eirp;
        private double? _radius;
        private string _antname;
        private string _pathlossmodel;
        private string _coveragetype;
        private string _nettype;
        private string _geoscenario;
        private string _comments;
        private string _rsite;
        /// <summary>
        /// 
        /// </summary>
        public int ID
        {
            set { _id = value; }
            get { return _id; }
        }
        /// <summary>
        /// 
        /// </summary>
        public string CellName
        {
            set { _cellname = value; }
            get { return _cellname; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string CellNameChs
        {
            set { _cellnamechs = value; }
            get { return _cellnamechs; }
        }
        /// <summary>
        /// 
        /// </summary>
        public int eNodeB
        {
            set { _lac = value; }
            get { return _lac; }
        }
        /// <summary>
        /// 
        /// </summary>
        public int CI
        {
            set { _ci = value; }
            get { return _ci; }
        }
        /// <summary>
        /// 
        /// </summary>
        public string BtsName
        {
            set { _btsname = value; }
            get { return _btsname; }
        }
        /// <summary>
        /// 
        /// </summary>
        public string Bsc
        {
            set { _bsc = value; }
            get { return _bsc; }
        }
        /// <summary>
        /// 
        /// </summary>
        public string Msc
        {
            set { _msc = value; }
            get { return _msc; }
        }
        /// <summary>
        /// 
        /// </summary>
        public string Vendor
        {
            set { _vendor = value; }
            get { return _vendor; }
        }
        /// <summary>
        /// 
        /// </summary>
        public int? EARFCN
        {
            set { _EARFCN = value; }
            get { return _EARFCN; }
        }
        /// <summary>
        /// 
        /// </summary>
        public string TCHNO
        {
            set { _tchno = value; }
            get { return _tchno; }
        }
        /// <summary>
        /// 
        /// </summary>
        public int? BSIC
        {
            set { _bsic = value; }
            get { return _bsic; }
        }
        /// <summary>
        /// 
        /// </summary>
        public int? TrxNumber
        {
            set { _trxnumber = value; }
            get { return _trxnumber; }
        }
        /// <summary>
        /// 
        /// </summary>
        public decimal? BTSLongitude
        {
            set { _btslongitude = value; }
            get { return _btslongitude; }
        }
        /// <summary>
        /// 
        /// </summary>
        public decimal? BTSLatitude
        {
            set { _btslatitude = value; }
            get { return _btslatitude; }
        }
        /// <summary>
        /// 
        /// </summary>
        public decimal? Longitude
        {
            set { _longitude = value; }
            get { return _longitude; }
        }
        /// <summary>
        /// 
        /// </summary>
        public decimal? Latitude
        {
            set { _latitude = value; }
            get { return _latitude; }
        }

        public decimal? x
        {
            set { _x = value; }
            get { return _x; }
        }

        public decimal? y
        {
            set { _y = value; }
            get { return _y; }
        }
        /// <summary>
        /// 
        /// </summary>
        public int? AntFloor
        {
            set { _antfloor = value; }
            get { return _antfloor; }
        }
        /// <summary>
        /// 
        /// </summary>
        public string BTSSite
        {
            set { _btssite = value; }
            get { return _btssite; }
        }
        /// <summary>
        /// 
        /// </summary>
        public decimal? BTSAltitude
        {
            set { _btsaltitude = value; }
            get { return _btsaltitude; }
        }
        /// <summary>
        /// 
        /// </summary>
        public string AdmiRegion
        {
            set { _admiRegion = value; }
            get { return _admiRegion; }
        }
        /// <summary>
        /// 
        /// </summary>
        public decimal? AntHeight
        {
            set { _antheight = value; }
            get { return _antheight; }
        }
        /// <summary>
        /// 
        /// </summary>
        public double? Azimuth
        {
            set { _azimuth = value; }
            get { return _azimuth; }
        }
        /// <summary>
        /// 
        /// </summary>
        public double? MechTilt
        {
            set { _mechtilt = value; }
            get { return _mechtilt; }
        }
        /// <summary>
        /// 
        /// </summary>
        public double? ElecTilt
        {
            set { _electilt = value; }
            get { return _electilt; }
        }
        /// <summary>
        /// 
        /// </summary>
        public double? Tilt
        {
            set { _tilt = value; }
            get { return _tilt; }
        }
        /// <summary>
        /// 
        /// </summary>
        public double? KDWRB
        {
            set { _bspwrb = value; }
            get { return _bspwrb; }
        }
        /// <summary>
        /// 
        /// </summary>
        public double? KDWRT
        {
            set { _bspwrt = value; }
            get { return _bspwrt; }
        }
        /// <summary>
        /// 
        /// </summary>
        public double? FeederLength
        {
            set { _feederlength = value; }
            get { return _feederlength; }
        }
        /// <summary>
        /// 
        /// </summary>
        public double? EIRP
        {
            set { _eirp = value; }
            get { return _eirp; }
        }

        /// <summary>
        /// 
        /// </summary>
        public double? CoverageRadius
        {
            set { _radius = value; }
            get { return _radius; }
        }
        /// <summary>
        /// 
        /// </summary>
        public string AntName
        {
            set { _antname = value; }
            get { return _antname; }
        }
        /// <summary>
        /// 
        /// </summary>
        public string PathlossModel
        {
            set { _pathlossmodel = value; }
            get { return _pathlossmodel; }
        }
        /// <summary>
        /// 
        /// </summary>
        public string CoverageType
        {
            set { _coveragetype = value; }
            get { return _coveragetype; }
        }
        /// <summary>
        /// 
        /// </summary>
        public string NetType
        {
            set { _nettype = value; }
            get { return _nettype; }
        }
        /// <summary>
        /// 
        /// </summary>
        public string GeoScenario
        {
            set { _geoscenario = value; }
            get { return _geoscenario; }
        }
        /// <summary>
        /// 
        /// </summary>
        public string Comments
        {
            set { _comments = value; }
            get { return _comments; }
        }
        /// <summary>
        /// 
        /// </summary>
        public string rsite
        {
            set { _rsite = value; }
            get { return _rsite; }
        }
        #endregion Model

    }
}

