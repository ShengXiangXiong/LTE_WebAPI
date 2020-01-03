using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTE.Calibration
{
    class ConvertUtil
    {
        //功率单位：w转换为dbm
        public static double convertw2dbm(double w)
        {
            return 10 * (Math.Log10(w) + 3);
        }

        //功率单位：dbm转换为w
        public static double convertdbm2w(double dbm)
        {
            return Math.Pow(10, (dbm / 10 - 3));
        }
    }
}
