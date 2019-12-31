using System;
namespace LTE.Model
{
    /// <summary>
    /// CELL
    /// </summary>
    [Serializable]
    public class PropertyCELL
    {
        public PropertyCELL()
        { }
        #region Model
        private int _id;
        private string _cellname;
        private string _cellnamechs;
        private int _lac;
        private int _ci;

        private int? _EARFCN;//主频
        private int? _EIRP;
        //private string _btsname;
        //private string _bsc;
        //private string _msc;
        //private string _vendor;
        private decimal? _longitude;
        private decimal? _latitude;
        private double? _azimuth;//方向角
        private decimal? _antheight;
        private double? _tilt;
        private double? _radius;  // 覆盖半径
        //private string _freqband;
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
        public int? EARFCN
        {
            set { _EARFCN = value; }
            get { return _EARFCN; }
        }

        /// <summary>
        /// 
        /// </summary>
        public int? EIRP
        {
            set { _EIRP = value; }
            get { return _EIRP; }
        }

        ///// <summary>
        ///// 
        ///// </summary>
        //public string 基站名称
        //{
        //    set { _bts_name = value; }
        //    get { return _bts_name; }
        //}
        /// <summary>
        /// 
        /// </summary>
        //public string BSC
        //{
        //    set { _bsc_name = value; }
        //    get { return _bsc_name; }
        //}
        ///// <summary>
        ///// 
        ///// </summary>
        //public string MSC
        //{
        //    set { _msc_name = value; }
        //    get { return _msc_name; }
        //}
        ///// <summary>
        ///// 
        ///// </summary>
        //public string 厂家
        //{
        //    set { _vendor_name = value; }
        //    get { return _vendor_name; }
        //}
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
        public decimal? AntHeight
        {
            set { _antheight = value; }
            get { return _antheight; }
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
        public double? Radius
        {
            set { _radius = value; }
            get { return _radius; }
        }
        ///// <summary>
        ///// 
        ///// </summary>
        //public string 频段
        //{
        //    set { _freqband = value; }
        //    get { return _freqband; }
        //}
        #endregion Model

    }
}

