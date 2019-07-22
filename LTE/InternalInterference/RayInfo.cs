using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

using LTE.InternalInterference;


namespace LTE.InternalInterference
{
    // 用于系数校正
    // 路径上的所有射线信息
    public class RayInfo
    {
        public double emitPwrW;                   // 发射功率
        public double recePwrW;                   // 接收功率
        public List<NodeInfo> rayList; 

        public RayInfo()
        {
            rayList = new List<NodeInfo>();
        }
    }
}
