using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LTE.Calibration
{
    //用于存储atu路测数据
    public class AtuInfo
    {
        public double gxid;                                // 路测点坐标
        public double gyid;
        public Dictionary<string, cellAtuInfo> CellATU;     // 某主小区内的路测点
    }
}
