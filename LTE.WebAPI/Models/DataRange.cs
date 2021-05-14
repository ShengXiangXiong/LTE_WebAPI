using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LTE.WebAPI.Models
{
    public class DataRange
    {
        //此次区域数据仿真的版本ID
        public String infAreaId;
        //经纬度范围
        public double minLongitude;
        public double maxLongitude;
        public double minLatitude;
        public double maxLatitude;
        //干扰栅格长、宽、高
        public double tarGridX;
        public double tarGridY;
        public double tarGridH;
    }
    public class AreaSplitRange
    {
        //经纬度范围
        public double minLongitude;
        public double maxLongitude;
        public double minLatitude;
        public double maxLatitude;
        //区域划分栅格边长
        public double tarGridL;
    }
}