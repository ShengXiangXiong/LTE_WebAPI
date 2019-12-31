using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Windows.Forms;
using System.Collections;

namespace LTE.DataOperate
{
    public class ExportToExcel
    {
        public void DataTableToTxtExcel(System.Data.DataTable dt)
        {
            SaveFileDialog fileSaver = new SaveFileDialog();
            fileSaver.AddExtension = true;
            fileSaver.DefaultExt = "xls";
            fileSaver.DereferenceLinks = true;
            fileSaver.Filter = "Excel(*.xls)|*.xls|All File(*.*)|*.*";
            fileSaver.Title = "请选择 Excel 文件输出路径!";
            fileSaver.OverwritePrompt = true;
            fileSaver.ShowHelp = false;
            fileSaver.Title = "另存为";
            if (fileSaver.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string filePath = fileSaver.FileName;
                    ExportExcel(dt, filePath);
                }
                catch (Exception ex)
                {
                    //MessageBox.Show("保存文件失败。可能是磁盘空间不足或者文件访问冲突。错误信息：" + ex.Message, "错误");
                }
            }
        }


        public  bool ExportExcel(DataTable dt, string fileName)
        {
            if (dt.Rows.Count == 0)
            {
                //System.Windows.Forms.MessageBox.Show("没有需要导出的数据，将不保存文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            try
            {
                FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                StringBuilder sb;
                string content;
                byte[] bytes;
                sb = new StringBuilder();
                ArrayList filteredFields = new ArrayList();
                filteredFields.Add("RB_FID");
                filteredFields.Add("设施唯一标识");
                filteredFields.Add("组件唯一标识");
                filteredFields.Add("组件锁");
                filteredFields.Add("版本号");
                filteredFields.Add("修改用户ID");
                filteredFields.Add("RB_CID");
                filteredFields.Add("任务ID");
                filteredFields.Add("文件ID");
                filteredFields.Add("序号");

                //写入数据的列名，作为单独一行
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    if (!filteredFields.Contains(dt.Columns[i].ColumnName.ToUpper()))
                    {
                        sb = sb.Append(dt.Columns[i].ColumnName + "\t");
                    }
                }
                sb = sb.Append("\r\n");
                content = sb.ToString();
                bytes = Encoding.GetEncoding("gb2312").GetBytes(content);
                fs.Write(bytes, 0, bytes.Length);
                //按行写入数据
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    sb = new StringBuilder();
                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        if (!filteredFields.Contains(dt.Columns[j].ColumnName.ToUpper()))
                        {
                            sb = sb.Append(dt.Rows[i][j].ToString().Replace("\r", "  ").Replace("\n", "  ") + "\t");
                        }
                    }
                    sb = sb.Append("\r\n");
                    content = sb.ToString();
                    bytes = Encoding.GetEncoding("gb2312").GetBytes(content);
                    fs.Write(bytes, 0, bytes.Length);
                }
                try
                {
                    fs.Flush();
                }
                catch//(System.IO.IOException ioe)
                {
                    return false;
                }
                finally
                {
                    fs.Close();
                }
                //System.Windows.Forms.MessageBox.Show("导出成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (Exception e)
            {
                //MessageBox.Show("保存文件失败。可能是磁盘空间不足或者文件访问冲突。错误信息：" + e.Message, "错误");
                return false;
            }

        }
    }
}
