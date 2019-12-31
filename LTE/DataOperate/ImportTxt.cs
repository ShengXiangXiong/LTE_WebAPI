using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;

namespace LTE.DataOperate
{

    class ImportTxt
    {
        //此类的静态变量会在整个应用中不会被引用的时候才会被销毁，所以操作完完毕得及时清除掉不用的数据
        private static StreamReader sr = null;
        private static DataTable exdt = null;
        //去掉引号函数
        private static string removeQuato(string str)
        {
            return str.Replace("\"", "");
        }
        //读取txt文件到datatable中

        /// <summary>
        /// 初始化类静态变量，当
        /// </summary>
        /// <param name="path"></param>
        private static void initReader(string path)
        {
            sr = new StreamReader(path, Encoding.Default);
            exdt = new DataTable();
        }

        public static DataTable readTxt(string path, int batchSize = 100000)
        {
            string line;
            //sr为null时表明此文件流是一个新文件流，需重新生成reader和字段名，否则跳过继续读取下一批
            if (sr == null)
            {
                initReader(path);
                line = sr.ReadLine();//读取第一行
                                     //string[] attrs = line.Split('\t');
                string[] attrs = line.Split(new char[] { '\t', ',', ' ' });
                //for (int i = 0; i < attrs.Count(); i++)//将属性名添加到datatable中
                //    exdt.Columns.Add(removeQuato(attrs[i]), System.Type.GetType("System.String"));

                for (int i = 0; i < attrs.Count(); i++)//将属性名添加到datatable中
                    exdt.Columns.Add(removeQuato(attrs[i]), typeof(double));
            }
            exdt.Rows.Clear();//清除上一批数据，保留字段名
            while ((line = sr.ReadLine()) != null && batchSize > 0)//添加数据
            {
                string[] values = line.Split(new char[] { '\t', ',', ' ' });
                DataRow row = exdt.NewRow();
                for (int i = 0; i < values.Count(); i++)
                {

                    //row[exdt.Columns[i].ColumnName] = removeQuato(values[i]);
                    string temp = removeQuato(values[i]); ;
                    if (temp.Count() == 0)
                    {
                        temp = "0";
                    }

                    row[exdt.Columns[i].ColumnName] = System.Convert.ToDouble(temp);
                }
                exdt.Rows.Add(row);
                batchSize--;
            }
            //if (exdt.Rows.Count == 0 || exdt.Rows.Count<batchSize)
            //{
            //    //防止用户忘记关闭
            //    closeReader();
            //}
            return exdt;
        }
        public static void closeReader()
        {
            if (sr != null)
            {
                sr.Close();
                sr = null;
                exdt.Clear();
                exdt = null;
            }
        }
    }
}
