using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Analyst3D;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;

using LTE.GIS;
using LTE.Geometric;
using LTE.DB;

namespace LTE.InternalInterference
{
    public partial class FrmSingleRayTracing1 : FrmSingleRayTracing
    {
        //经纬度
        internal double longitude;
        internal double latitude;
        //如地点大地坐标
        internal double x;
        internal double y;
        internal double z;

        public FrmSingleRayTracing1():base()
        {
            InitializeComponent();
        }
        public FrmSingleRayTracing1(string cellName):base()
        {
            InitializeComponent();
            this.textBox1.Text = cellName;
        }
      
        private void FillGrid()
        {
            this.dataGridView1.DataSource=this.dt;
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
            if (e.KeyChar >= '0' && e.KeyChar <= '9' || e.KeyChar == '\b'|| e.KeyChar=='.')
                e.Handled = false;
        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
            if (e.KeyChar >= '0' && e.KeyChar <= '9' || e.KeyChar == '\b' || e.KeyChar == '.')
                e.Handled = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!validateInput())
                return;
            if (!validateCell())
                return;
            
            CellInfo cellInfo = new CellInfo(this.cellName, 0, 0);
            IPoint p = GeometryUtilities.ConstructPoint3D(this.longitude, this.latitude, 0);
            PointConvert.Instance.GetProjectPoint(p);
            this.x = p.X;
            this.y = p.Y;
            this.z = p.Z;

            //IPoint p1 = GeometryUtilities.ConstructPoint3D(666784.023, 3551085.291, 0);
            //PointConvert.Instance.GetGeoPoint(p1);
            //double x = p1.X;
            //double y = p1.Y;
            //double z = p1.Z;

            //this.x = 666784.023;    // 反射  南京 和会街变_2
            //this.y = 3551085.291;
            //this.z = 0;

            //this.x = 666745.82;    // 水平绕射  南京 和会街变_2
            //this.y = 3551068.64;
            //this.z = 21;

            //this.x = 666719.68;      // 垂直绕射  南京 和会街变_2
            //this.y = 3551065.16;
            //this.z = 9;

            //this.x = 666509.233;    // 反射  南京 和会街变_2
            //this.y = 3550989.244;
            //this.z = 0;

            //IbatisHelper.ExecuteDelete("deleteSpecifiedCelltbGrids", cellInfo.CellName);
            this.dt.Clear();
            //IGraphicsContainer3D container3D = GISMapApplication.Instance.GetLayer(LayerNames.Rays) as IGraphicsContainer3D;
            //container3D.DeleteAllElements();
            //OperateCoverGirdLayer operateGrid = new OperateCoverGirdLayer();
            //operateGrid.ClearLayer();
            RayTracing interAnalysis = new RayTracing(cellInfo, 3, 2, false);

            interAnalysis.SingleRayAnalysis(this);

            if (this.dt.Rows.Count == 0)
            {
                MessageBox.Show(this, "射线到达建筑物顶面或经过若干次反射超出地图范围以致无法到达地面。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            this.FillGrid();
            
            //OperateCoverGirdLayer operateGrid = new OperateCoverGirdLayer();
            //operateGrid.displayCellGrids(this.cellName);
        }

        private Boolean validateInput()
        {
            if(this.textBox1.Text==string.Empty)
            {
                MessageBox.Show(this,"请输入小区名称","提示",MessageBoxButtons.OK,MessageBoxIcon.Information);
                return false;
            }
            if(this.textBox2.Text==string.Empty)
            {
                MessageBox.Show(this,"请输入经度","提示",MessageBoxButtons.OK,MessageBoxIcon.Information);
                this.textBox2.Focus();
                return false;
            }
            if(this.textBox3.Text==string.Empty)
            {
                MessageBox.Show(this,"请输入纬度","提示",MessageBoxButtons.OK,MessageBoxIcon.Information);
                this.textBox3.Focus();
                return false;
            }
            this.cellName=textBox1.Text;
            try{this.longitude = double.Parse(this.textBox2.Text);}
            catch
            {
                MessageBox.Show(this,"您输入的经度格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox2.Focus();
                return false;
            }
            /*
            if (this.longitude < 113.307800 || this.longitude > 113.372296)
            {
                MessageBox.Show(this, "您输入的经纬度超出了地图范围（113.307800<经度<113.372296,23.111300<纬度<23.156352），请重新输入。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox2.Focus();
                return false;
            }
            */
            try{this.latitude = double.Parse(this.textBox3.Text);}
            
            catch
            {
                MessageBox.Show(this,"您输入的纬度格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox3.Focus();
                return false;
            }
            /*
            if (this.latitude < 23.111300 || this.latitude > 23.156352)
            {
                MessageBox.Show(this, "您输入的经纬度超出了地图范围（113.307800<经度<113.372296,23.111300<纬度<23.156352），请重新输入。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox3.Focus();
                return false;
            }
            //if (this.longitude<113.36897||this.longitude>113.40273||this.latitude<22.5093||this.latitude>22.53625)
            //{
            //    MessageBox.Show(this, "您输入的经纬度超出了地图范围（113.36897<经度<113.40273,22.5093<纬度<22.53625），请重新输入。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return false;
            //}
             */
            return true;
        }


    }
}
