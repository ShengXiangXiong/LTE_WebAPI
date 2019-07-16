using System;
using System.Collections.Generic;
using System.Text;

namespace LTE.GIS
{
    /// <summary>
    /// 图层名称，用常量表示
    /// </summary>
    public struct LayerNames
    {
        public const string Building = "建筑物";
        public const string Building1 = "建筑物海拔";
        public const string SmoothBuildingVertex = "建筑物底边平滑";

        public const string TIN = "地形TIN";  // 地形
        public const string TIN1 = "地形TIN面";  // 地形
        /// <summary>
        /// GSM900基站
        /// </summary>
        public const string GSM900BTS = "GSM900基站";

        /// <summary>
        /// GSM900小区
        /// </summary>
        public const string GSM900Cell = "GSM900小区";

        /// <summary>
        /// GSM1800基站
        /// </summary>
        public const string GSM1800BTS = "GSM1800基站";

        /// <summary>
        /// GSM1800小区
        /// </summary>
        public const string GSM1800Cell = "GSM1800小区";


        /// <summary>
        /// 扫频
        /// </summary>
        public const string RoadTest = "路测";

        /// <summary>
        /// 底面
        /// </summary>
        public const string Projecton = "buildings_Project";
        /// <summary>
        /// 建筑物
        /// </summary>
        public const string Buildings = "buildings";
        /// <summary>
        /// 覆盖
        /// </summary>
        //public const string CoverGrids = "覆盖";
        /// <summary>
        /// 立体覆盖
        /// </summary>
        //public const string CoverGrid3Ds = "立体覆盖";
        /// <summary>
        /// 区域覆盖
        /// </summary>
        public const string AreaCoverGrids = "区域覆盖";
        /// <summary>
        /// 区域立体覆盖
        /// </summary>
        public const string AreaCoverGrid3Ds = "区域立体覆盖";
        /// <summary>
        /// 干扰
        /// </summary>
        public const string InterferenceGrids = "干扰";
        /// <summary>
        /// 地面网格
        /// </summary>
        public const string GroundGrids = "地面网格";
        /// <summary>
        /// 射线
        /// </summary>
        public const string Rays = "射线";

        public const string Street = "sub交通线";

        public const string MStreet = "主道路";

        /// <summary>
        /// TD路测
        /// </summary>
        public const string TDDriverTest = "TD路测";

        public const string Weak = "弱覆盖点";
        public const string Excessive = "过覆盖点";
        public const string Overlapped = "重叠覆盖点";
        public const string PCImod3 = "PCI模3对打点";
        public const string PCIconfusion = "PCI混淆点";
        public const string PCIconflict = "PCI冲突点";

        public const string InfSource = "网外干扰源";
    }
}
