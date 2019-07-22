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
using System.Data.SqlClient;
using LTE.DataOperate;
using LTE.DB;
using ESRI.ArcGIS.Geometry;
using LTE.GIS;

namespace LTE.InternalInterference
{
    public partial class DataImport : Form
    {
        private ArrayList attrName = new ArrayList();//数据库属性列表
        private DataTable exdt;//存放从excel或者txt中读取的数据

        public DataImport()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)//窗口加载时添加combobox中的值以及左边listbox中的值
        {
            ArrayList tableNames = IbatisHelper.getTableNames();//获取数据库中的表名
            if (tableNames == null)
                return;
            for (int i = 0; i < tableNames.Count; i++)//添加数据库名到左边combobox中
            {
                selectTableName.Items.Add(tableNames[i]);
                selectTableName.SelectedIndex = 0;
            }
            this.lstShow1.Items.Clear();
            attrName = IbatisHelper.getAttrName(selectTableName.SelectedItem.ToString());//将指定表的属性添加到左边的列表中
            for (int i = 0; i < attrName.Count; i++)
                this.lstShow1.Items.Add(attrName[i]);

        }

        private void button4_Click(object sender, EventArgs e)//获取excel或者txt表格的按钮
        {

            if (txtFile.Text == "")
            {
                MessageBox.Show("请选择文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                int index = txtFile.Text.LastIndexOf('.');
                //获取文件扩展名
                string extendedName = txtFile.Text.Substring(index + 1, txtFile.Text.Length - index - 1);
                Console.WriteLine(txtFile.Text);
                //判断文件是否正确
                if (extendedName == "")
                {
                    MessageBox.Show("请选择文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (extendedName.Equals("csv"))//导入csv文件格式
                {
                    exdt = CSVFileHelper.OpenCSV(txtFile.Text);
                }
                else if (!(extendedName.Equals("txt")))//导入excel格式的数据
                {
                    ImportExcel excel = new ImportExcel(txtFile.Text);
                    exdt = excel.ExcelToDS();
                }
                else//导入txt格式的数据
                {
                    exdt = ImportTxt.readTxt(txtFile.Text);
                }
                if (exdt == null)
                    return;
                //返回的DataTable对象不为空
                this.lstShow2.Items.Clear();
                for (int i = 0; i < exdt.Columns.Count; i++)
                {
                    this.lstShow2.Items.Add(exdt.Columns[i].ColumnName);

                }
            }
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                txtFile.Text = openFileDialog1.FileName;
            }
        }

        private void openFileDialog1_FileOk_1(object sender, CancelEventArgs e)
        {
            lstShow2.Items.Clear();
        }


        private void btnImport_Click(object sender, EventArgs e)//导入按钮
        {
            if (write_excel_data_2_db(this.selectTableName.SelectedItem.ToString()))
                MessageBox.Show("导入数据成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        private void lstShow1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstShow1.SelectedIndex != -1)
            {
                if (lstShow2.SelectedIndex != -1)
                    lstShow2.SetSelected(lstShow2.SelectedIndex, false);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (this.lstShow1.SelectedIndex > 0)
                up_Event(lstShow1);
            else if (this.lstShow2.SelectedIndex > 0)
                up_Event(lstShow2);
        }


        private void up_Event(ListBox listBox)
        {
            string temp = listBox.SelectedItem.ToString();
            Console.WriteLine(temp);
            listBox.Items[listBox.SelectedIndex] = listBox.Items[listBox.SelectedIndex - 1].ToString();
            listBox.SelectedIndex = listBox.SelectedIndex - 1;
            listBox.Items[listBox.SelectedIndex] = temp;
        }


        private void button2_Click(object sender, EventArgs e)
        {
            if ((lstShow1.SelectedIndex < lstShow1.Items.Count - 1) && (lstShow1.SelectedIndex != -1))
                down_Event(lstShow1);
            else if ((lstShow2.SelectedIndex < lstShow2.Items.Count - 1) && (lstShow2.SelectedIndex != -1))
                down_Event(lstShow2);
        }

        private void down_Event(ListBox listBox)
        {
            string temp = listBox.SelectedItem.ToString();
            Console.WriteLine(temp);
            listBox.Items[listBox.SelectedIndex] = listBox.Items[listBox.SelectedIndex + 1].ToString();
            listBox.SelectedIndex = listBox.SelectedIndex + 1;
            listBox.Items[listBox.SelectedIndex] = temp;
        }


        private void button3_Click(object sender, EventArgs e)//remove按钮
        {
            int index = this.lstShow2.SelectedIndex;
            this.lstShow2.Items.Remove(this.lstShow2.SelectedItem);
            Console.WriteLine(this.lstShow2.Items);
            if (index > 0)
                this.lstShow2.SetSelected(index - 1, true);
        }

        /// <summary>
        /// 导入时生成XY平面坐标系
        /// </summary>
        /// <param name="b"></param>
        /// <param name="exdt"></param>
        private void addXY(ListBox b, DataTable exdt)
        {
            b.Items.Add("x");
            b.Items.Add("y");
            exdt.Columns.Add("x", typeof(double));
            exdt.Columns.Add("y", typeof(double));
            foreach (DataRow dr in exdt.Rows)
            {
                //dr["DATE"] = time;
                IPoint p = new PointClass();
                p.X = (double)dr["Longitude"];
                p.Y = (double)dr["Latitude"];
                p = PointConvert.Instance.GetProjectPoint(p);
                dr["x"] = p.X;
                dr["y"] = p.Y;
            }
        }

        /// <summary>
        /// 字段名检查
        /// </summary>
        /// <param name="b1"></param>
        /// <param name="b2"></param>
        /// <returns>与数据库不一致的字段名</returns>
        private HashSet<String> checkFieldName(ListBox b1, ListBox b2)
        {
            HashSet<String> s1 = new HashSet<string>();
            HashSet<String> s2 = new HashSet<string>();
            for (int ccc = 0; ccc < b1.Items.Count; ccc++)
            {
                s1.Add(b1.Items[ccc].ToString().Trim());
            }
            for (int ccc = 0; ccc < b2.Items.Count; ccc++)
            {
                s2.Add(b2.Items[ccc].ToString().Trim());
            }
            s2.ExceptWith(s1);
            return s2;
        }
        //将datatable中的数据写入数据库函数
        private bool write_excel_data_2_db(string _tableName)
        {

            if (_tableName == "CELL")
            {
                addXY(lstShow2, exdt);
            }
            HashSet<String> s = checkFieldName(lstShow1, lstShow2);
            if (s.Count > 0)
            {
                StringBuilder sss = new StringBuilder();
                foreach (string str in s.ToList())
                {
                    sss.Append(str + " ");
                }
                MessageBox.Show("数据字段名不一致" + sss);
                return false;
            }
            if (lstShow1.Items.Count > lstShow2.Items.Count)//对日期专门处理
            {
                //Console.WriteLine("date ruin");

                ////lstShow2.Items.Add("DATE");
                //lstShow2.Items.Add("x");
                //lstShow2.Items.Add("y");
                ////string time = dateTimePicker.Text.ToString();
                ////string time = dateTimePicker.Value.ToString("yyyyMMdd");
                ////exdt.Columns.Add("DATE", typeof(string));
                //exdt.Columns.Add("x", typeof(double));
                //exdt.Columns.Add("y", typeof(double));
                //foreach (DataRow dr in exdt.Rows)
                //{
                //    //dr["DATE"] = time;
                //    IPoint p = new PointClass();
                //    p.X = (double)dr["Longitude"];
                //    p.Y = (double)dr["Latitude"];
                //    p = PointConvert.Instance.GetProjectPoint(p);
                //    dr["x"] = p.X;
                //    dr["y"] = p.Y;
                //}
                //if (_tableName == "CELL")
                //{
                //    addXY(lstShow2, exdt);
                //}
                //HashSet<String> s = checkFieldName(lstShow1, lstShow2);
                //if (s.Count > 0)
                //{
                //    throw new Exception("数据字段名不一致"+s.ToList().ToString());
                //}
            }
            if (lstShow1.Items.Count < lstShow2.Items.Count)
            {
                MessageBox.Show("属性设置不合理，请重新设计", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            else
            {
                try
                {
                    SqlBulkCopy bulkCopy = new SqlBulkCopy(DataUtil.ConnectionString);
                    bulkCopy.DestinationTableName = _tableName;
                    for (int ccc = 0; ccc < lstShow2.Items.Count; ccc++)
                    {
                        bulkCopy.ColumnMappings.Add(this.lstShow2.Items[ccc].ToString().Trim(), this.lstShow2.Items[ccc].ToString().Trim());
                    }
                    while (exdt.Rows.Count > 0)
                    {
                        bulkCopy.WriteToServer(exdt);
                        exdt.Clear();//防止gc来不及回收，造成下一次读取的内存不足
                        exdt = ImportTxt.readTxt(txtFile.Text);
                    }
                    ImportTxt.closeReader();
                    //bulkCopy.WriteToServer(exdt);
                    bulkCopy.Close();
                    bulkCopy = null;

                    //exdt.Clear();

                    return true;

                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("数据库插入失败，请重试", "警告", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return false;
                }
                finally
                {

                }
            }
        }

        private void lstShow2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstShow2.SelectedIndex != -1)
            {
                if (lstShow1.SelectedIndex != -1)
                    lstShow1.SetSelected(lstShow1.SelectedIndex, false);
            }
        }

        private void selectTableName_SelectedIndexChanged(object sender, EventArgs e)//combobox选定内容改变促发的事件
        {
            this.lstShow1.Items.Clear();
            attrName = IbatisHelper.getAttrName(selectTableName.SelectedItem.ToString());//将指定表的属性添加到左边的列表中
            for (int i = 0; i < attrName.Count; i++)
            {
                this.lstShow1.Items.Add(attrName[i]);
            }

        }

    }
}
