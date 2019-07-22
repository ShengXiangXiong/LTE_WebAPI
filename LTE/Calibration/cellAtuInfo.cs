using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LTE.Calibration
{
    //以CellID为主小区的路测数据
    public class cellAtuInfo
    {
        public string CellID;                       // 路测点的主小区
        public double AllRsrp;                      // 路测点内的路测点rsrp叠加
        public double maxRSRP;                      // 路测点内的最大RSRP
        public int NumofAtu;                        // 路测点内的路测数量
        public double avgRSRP;                      // 路测点内的平均RSRP
    }
}
