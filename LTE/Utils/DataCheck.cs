using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LTE.DB;

namespace LTE.Utils
{
    public class DataCheck
    {
        /// <summary>
        /// 判断初始化工作是否完成，完成后才可进行系数校正射线生成等工作
        /// </summary>
        /// <returns></returns>
        public static bool checkInitFinished(){
            DataTable resultTb = IbatisHelper.ExecuteQueryForDataTable("getInitZeroStateCnt",null);
            double zeroStateCnt = double.Parse(resultTb.Rows[0][0].ToString());
            return zeroStateCnt == 0;
        }
    }
}
