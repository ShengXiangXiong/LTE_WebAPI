using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using LTE.DB;

namespace LTE.InternalInterference
{
    class AdjCoeffHelper
    {
        private static double[,] coeff;
        private static int scenNum;

        private static AdjCoeffHelper instance = null;
        private static object syncRoot = new object();

        public static AdjCoeffHelper getInstance()
        {
            if (instance == null)
            {
                lock (syncRoot)
                {
                    if (instance == null)
                    {
                        instance = new AdjCoeffHelper();
                        Init();
                    }
                }
            }
            return instance;
        }

        private static bool Init()
        {
            DataTable tb = IbatisHelper.ExecuteQueryForDataTable("getAdjCoeff", null);
            scenNum = tb.Rows.Count;

            if (scenNum < 1)
            {
                scenNum = 0;
                return false;
            }

            coeff = new double[scenNum, 3];

            for (int i = 0; i < scenNum; i++)
            {
                coeff[i, 0] = Convert.ToDouble(tb.Rows[i]["DirectCoefficient"].ToString());
                coeff[i, 1] = Convert.ToDouble(tb.Rows[i]["ReflectCoefficient"].ToString());
                coeff[i, 2] = Convert.ToDouble(tb.Rows[i]["DiffracteCoefficient"].ToString());
            }
            return true;
        }

        public double[,] getCoeff()
        {
            return coeff;
        }

        public int getSceneNum()
        {
            return scenNum;
        }
    }

    class AdjCoeffHelper1
    {
        private static int scenNum;

        private static AdjCoeffHelper1 instance = null;
        private static object syncRoot = new object();

        public static AdjCoeffHelper1 getInstance()
        {
            if (instance == null)
            {
                lock (syncRoot)
                {
                    if (instance == null)
                    {
                        instance = new AdjCoeffHelper1();
                        Init();
                    }
                }
            }
            return instance;
        }

        private static bool Init()
        {
            DataTable tb = IbatisHelper.ExecuteQueryForDataTable("getAdjCoeff1", null);

            if (tb.Rows.Count < 1)
            {
                scenNum = 0;
                return false;
            }

            scenNum = Convert.ToInt32(tb.Rows[0][0].ToString()) + 1;
            return true;
        }


        public int getSceneNum()
        {
            return scenNum;
        }
    }
}
