using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using System.Data.OleDb;

namespace LTE.DataOperate
{
    class ImportExcel
    {

        private string path;
        public ImportExcel(string _path)
        {
            path = _path;
        }
        //将excel文件中的内容导入到datatable
        public DataTable ExcelToDS()
        {
            DataTable tables = new DataTable();

            string conStr = "";
            string filePath = path;
            int index = filePath.LastIndexOf('.');
            //获取文件扩展名
            string extendedName = filePath.Substring(index + 1, filePath.Length - index - 1);

            //判断文件是否正确
            if (extendedName == "")
            {
                //Console.WriteLine("请选择文件");
                MessageBox.Show("请选择文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return null;
            }
            else if (!(extendedName.Equals("xls") || extendedName.Equals("xlsx")))
            {
                //Console.WriteLine("文件格式错误");
                MessageBox.Show("文件格式错误", "警告", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return null;
            }
            else
            {
                //Console.WriteLine(filePath);
                //格式正确
                if (extendedName.Equals("xls"))
                    conStr = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + filePath + ";Extended Properties=\"Excel 8.0;HDR=YES;IMEX=1\"";
                else
                    conStr = "Provider=Microsoft.ACE.OLEDB.12.0;" + "Data Source=" + filePath + ";" + ";Extended Properties=\"Excel 12.0;HDR=YES;IMEX=1\"";
            }

            //Console.WriteLine(conStr);

            try
            {
                //类似于获取数据库连接
                OleDbConnection conn = new OleDbConnection(conStr);
                //打开
                conn.Open();

                //得到数据
                tables = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new Object[] { });
                if (tables.Rows.Count == 0)
                {
                    //Console.WriteLine("文件中没有可用表");
                    MessageBox.Show("文件中没有可用表", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }

                //读取数据
                string firstTableName = tables.Rows[0]["TABLE_NAME"].ToString();
                OleDbCommand cmd = new OleDbCommand("select * from [" + firstTableName + "]", conn);
                OleDbDataAdapter apt = new OleDbDataAdapter(cmd);
                DataTable dt = new DataTable();
                dt.Clear();
                apt.Fill(dt);
                if (dt.Rows.Count < 1)
                {
                    Console.WriteLine("Excel表中没有数据");
                    MessageBox.Show("Excel表中没有数据", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }

                conn.Close();
                return dt;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }

        }

    }
}
