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
        //去掉引号函数
        private static string removeQuato(string str)
        {
            return str.Replace("\"", "");
        }
        //读取txt文件到datatable中
        public static DataTable readTxt(string path)
        {

            StreamReader sr = new StreamReader(path, Encoding.Default);
            string line;
            System.Data.DataTable exdt = new DataTable();
            line = sr.ReadLine();//读取第一行
            string[] attrs = line.Split('\t');
            for (int i = 0; i < attrs.Count(); i++)//将属性名添加到datatable中
                exdt.Columns.Add(removeQuato(attrs[i]), System.Type.GetType("System.String"));

            while ((line = sr.ReadLine()) != null)//添加数据
            {
                string[] values = line.Split('\t');
                DataRow row = exdt.NewRow();
                for (int i = 0; i < values.Count(); i++)
                {

                    //row[exdt.Columns[i].ColumnName] = removeQuato(values[i]);
                    string temp = removeQuato(values[i]); ;
                    if (temp.Count() == 0)
                    {
                        temp = "0";
                    }

                    row[exdt.Columns[i].ColumnName] = temp;
                }

                exdt.Rows.Add(row);

            }
            return exdt;
        }
    }
}
