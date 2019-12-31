using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LTE.Model
{
    [DefaultProperty("ReceivedPowerdbm")]
    
    public class PropertyGrid
    {
        //public int GXID { get; set; }
        //public int GYID { get; set; }
        //public float CLong { get; set; }
        //public float CLat { get; set; }
        //public float MinLong { get; set; }
        //public float MinLat { get; set; }
        //public float MaxLong { get; set; }
        //public float MaxLat { get; set; }

        //public string cellName { get; set; }
        //public float BTSGridDistance { get; set; }
        //public float FieldIntensity { get; set; }
        //public float ReceivedPower_W { get; set; }
        //public float ReceivedPower_dbm { get; set; }
        //public float PathLoss { get; set; }

        //public int DirectPwrNum { get; set; }
        //public float DirectPwrW { get; set; }
        //public float MaxDirectPwrW { get; set; }

        //public int RefPwrNum { get; set; }
        //public float RefPwrW { get; set; }
        //public float MaxRefPwrW { get; set; }

        //public int DiffNum { get; set; }
        //public float DiffPwrW { get; set; }
        //public float MaxDiffPwrW { get; set; }

        //-----------------------------变量为英文-----------------------
        //[Category("小区信息"), ReadOnly(true)]
        //public string cellName { get; set; }

        //[Category("网格信息"), ReadOnly(true)]
        //public int GXID { get; set; }
        //[Category("网格信息"), ReadOnly(true)]
        //public int GYID { get; set; }
        //[Category("网格信息"), ReadOnly(true)]
        //public double CLong { get; set; }
        //[Category("网格信息"), ReadOnly(true)]
        //public double CLat { get; set; }
        //[Category("网格信息"), ReadOnly(true)]
        //public double MinLong { get; set; }
        //[Category("网格信息"), ReadOnly(true)]
        //public double MinLat { get; set; }
        //[Category("网格信息"), ReadOnly(true)]
        //public double MaxLong { get; set; }
        //[Category("网格信息"), ReadOnly(true)]
        //public double MaxLat { get; set; }
        //[Category("网格信息"), ReadOnly(true)]
        //public double BTSGridDistance { get; set; }


        //[Category("统计"), ReadOnly(true),Browsable(false)]
        //public double FieldIntensity { get; set; }
        //[Category("统计"), ReadOnly(true)]
        //public double ReceivedPower_W { get; set; }
        //[Category("统计"), ReadOnly(true)]
        //public double ReceivedPower_dbm { get; set; }
        //[Category("统计"), ReadOnly(true)]
        //public double PathLoss { get; set; }

        //[Category("直射"), ReadOnly(true)]
        //public int DirectPwrNum { get; set; }
        //[Category("直射"), ReadOnly(true)]
        //public double DirectPwrW { get; set; }
        //[Category("直射"), ReadOnly(true)]
        //public double MaxDirectPwrW { get; set; }

        //[Category("反射"), ReadOnly(true)]
        //public int RefPwrNum { get; set; }
        //[Category("反射"), ReadOnly(true)]
        //public double RefPwrW { get; set; }
        //[Category("反射"), ReadOnly(true)]
        //public double MaxRefPwrW { get; set; }

        //[Category("绕射"), ReadOnly(true)]
        //public int DiffNum { get; set; }
        //[Category("绕射"), ReadOnly(true)]
        //public double DiffPwrW { get; set; }
        //[Category("绕射"), ReadOnly(true)]
        //public double MaxDiffPwrW { get; set; }


        ////-------------------------------变量为英文，添加中文显示属性--------------------------------
        //[Category("小区信息"), ReadOnly(true),DisplayName("小区名称")]
        //public string cellName { get; set; }

        //[Category("网格信息"), ReadOnly(true), DisplayName("网格X标识")]
        //public int GXID { get; set; }
        //[Category("网格信息"), ReadOnly(true), DisplayName("网格Y标识")]
        //public int GYID { get; set; }
        //[Category("网格信息"), ReadOnly(true), DisplayName("网格中心经度")]
        //public double CLong { get; set; }
        //[Category("网格信息"), ReadOnly(true), DisplayName("网格中心纬度")]
        //public double CLat { get; set; }
        //[Category("网格信息"), ReadOnly(true), DisplayName("最小经度")]
        //public double MinLong { get; set; }
        //[Category("网格信息"), ReadOnly(true), DisplayName("最小纬度")]
        //public double MinLat { get; set; }
        //[Category("网格信息"), ReadOnly(true), DisplayName("最大经度")]
        //public double MaxLong { get; set; }
        //[Category("网格信息"), ReadOnly(true), DisplayName("最大纬度")]
        //public double MaxLat { get; set; }
        //[Category("网格信息"), ReadOnly(true), DisplayName("网格小区间距")]
        //public double BTSGridDistance { get; set; }


        //[Category("统计"), ReadOnly(true), Browsable(false), DisplayName("场强")]
        //public double FieldIntensity { get; set; }
        //[Category("统计"), ReadOnly(true), DisplayName("接收功率W")]
        //public double ReceivedPower_W { get; set; }
        //[Category("统计"), ReadOnly(true), DisplayName("接收功率dbm")]
        //public double ReceivedPower_dbm { get; set; }
        //[Category("统计"), ReadOnly(true), DisplayName("路径损耗")]
        //public double PathLoss { get; set; }

        //[Category("直射"), ReadOnly(true), DisplayName("直射数")]
        //public int DirectPwrNum { get; set; }
        //[Category("直射"), ReadOnly(true), DisplayName("平均直射接收功率")]
        //public double DirectPwrW { get; set; }
        //[Category("直射"), ReadOnly(true), DisplayName("最大直射接收功率")]
        //public double MaxDirectPwrW { get; set; }


        //[Category("反射"), ReadOnly(true), DisplayName("反射数")]
        //public int RefPwrNum { get; set; }
        //[Category("反射"), ReadOnly(true), DisplayName("平均反射接收功率")]
        //public double RefPwrW { get; set; }
        //[Category("反射"), ReadOnly(true), DisplayName("最大反射接收功率")]
        //public double MaxRefPwrW { get; set; }
        //[Category("反射"), ReadOnly(true), DisplayName("反射建筑物标识")]
        //public string RefBuildingID { get; set; }

        //[Category("绕射"), ReadOnly(true), DisplayName("绕射数")]
        //public int DiffNum { get; set; }
        //[Category("绕射"), ReadOnly(true), DisplayName("平均绕射接收功率")]
        //public double DiffPwrW { get; set; }
        //[Category("绕射"), ReadOnly(true), DisplayName("最大绕射接收功率")]
        //public double MaxDiffPwrW { get; set; }
        //[Category("绕射"), ReadOnly(true), DisplayName("绕射建筑物标识")]
        //public string DiffBuildingID { get; set; }

        //-------------------------------变量为英文，添加中文显示属性,对分类排序--------------------------------
        [Category("a.网格信息"), ReadOnly(true), DisplayName("小区名称")]
        public string CellName { get; set; }
        [Category("a.网格信息"), ReadOnly(true), DisplayName("小区eNodeB")]
        public int eNodeB { get; set; }
        [Category("a.网格信息"), ReadOnly(true), DisplayName("小区CI")]
        public int CI { get; set; }
        [Category("a.网格信息"), ReadOnly(true), DisplayName("网格X标识")]
        public int GXID { get; set; }
        [Category("a.网格信息"), ReadOnly(true), DisplayName("网格Y标识")]
        public int GYID { get; set; }
        [Category("a.网格信息"), ReadOnly(true), DisplayName("网格L标识")]
        public int Level { get; set; }
        [Category("a.网格信息"), ReadOnly(true), DisplayName("网格高度")]
        public double Height { get; set; }
        [Category("a.网格信息"), ReadOnly(true), DisplayName("网格中心经度")]
        public double CLong { get; set; }
        [Category("a.网格信息"), ReadOnly(true), DisplayName("网格中心纬度")]
        public double CLat { get; set; }
        [Category("a.网格信息"), ReadOnly(true), DisplayName("最小经度")]
        public double MinLong { get; set; }
        [Category("a.网格信息"), ReadOnly(true), DisplayName("最小纬度")]
        public double MinLat { get; set; }
        [Category("a.网格信息"), ReadOnly(true), DisplayName("最大经度")]
        public double MaxLong { get; set; }
        [Category("a.网格信息"), ReadOnly(true), DisplayName("最大纬度")]
        public double MaxLat { get; set; }
        [Category("a.网格信息"), ReadOnly(true), DisplayName("网格小区间距")]
        public double BTSGridDistance { get; set; }


        [Category("b.统计"), ReadOnly(true), Browsable(false), DisplayName("场强")]
        public double FieldIntensity { get; set; }
        [Category("b.统计"), ReadOnly(true), DisplayName("接收功率W")]
        public double ReceivedPowerW { get; set; }
        [Category("b.统计"), ReadOnly(true), DisplayName("接收功率dbm")]
        public double ReceivedPowerdbm { get; set; }
        [Category("b.统计"), ReadOnly(true), DisplayName("路径损耗")]
        public double PathLoss { get; set; }

        [Category("c.直射"), ReadOnly(true), DisplayName("直射数")]
        public int DirectPwrNum { get; set; }
        [Category("c.直射"), ReadOnly(true), DisplayName("平均直射接收功率")]
        public double DirectPwrW { get; set; }
        [Category("c.直射"), ReadOnly(true), DisplayName("最大直射接收功率")]
        public double MaxDirectPwrW { get; set; }


        [Category("d.反射"), ReadOnly(true), DisplayName("反射数")]
        public int RefPwrNum { get; set; }
        [Category("d.反射"), ReadOnly(true), DisplayName("平均反射接收功率")]
        public double RefPwrW { get; set; }
        [Category("d.反射"), ReadOnly(true), DisplayName("最大反射接收功率")]
        public double MaxRefPwrW { get; set; }
        [Category("d.反射"), ReadOnly(true), DisplayName("反射建筑物标识")]
        public string RefBuildingID { get; set; }

        [Category("e.绕射"), ReadOnly(true), DisplayName("绕射数")]
        public int DiffNum { get; set; }
        [Category("e.绕射"), ReadOnly(true), DisplayName("平均绕射接收功率")]
        public double DiffPwrW { get; set; }
        [Category("e.绕射"), ReadOnly(true), DisplayName("最大绕射接收功率")]
        public double MaxDiffPwrW { get; set; }
        [Category("e.绕射"), ReadOnly(true), DisplayName("绕射建筑物标识")]
        public string DiffBuildingID { get; set; }
    }
}
