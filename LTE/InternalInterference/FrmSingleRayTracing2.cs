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
    public partial class FrmSingleRayTracing2 : FrmSingleRayTracing
    {
        internal double direction;
        internal double inclination;

        public FrmSingleRayTracing2() :base()
        {
            InitializeComponent();
            
        }
        public FrmSingleRayTracing2(string cellName):base()
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
            //CellInfo cellInfo = new CellInfo(this.cellName, this.cellType, this.frequncy, this.directCoefficient, this.reflectCoefficient, this.diffractCoefficient, this.diffractCoefficient2);
            //IbatisHelper.ExecuteDelete("deleteSpecifiedCelltbGrids", cellInfo.cellName);
            this.dt.Clear();
            //IGraphicsContainer3D container3D = GISMapApplication.Instance.GetLayer(LayerNames.Rays) as IGraphicsContainer3D;
            //container3D.DeleteAllElements();
            //OperateCoverGirdLayer operateGrid = new OperateCoverGirdLayer(LayerNames.AreaCoverGrids);
            //operateGrid.ClearLayer();
            RayTracing interAnalysis = new RayTracing(cellInfo, 4, 2, false);

            interAnalysis.SingleRayAnalysis(this);
            if (this.dt.Rows.Count == 0)
            {
                MessageBox.Show(this, "射线到达建筑物顶面或经过若干次反射超出地图范围以致无法到达地面。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            this.FillGrid();
            
           // OperateCoverGirdLayer operateGrid = new OperateCoverGirdLayer();
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
                MessageBox.Show(this,"请输入方位角","提示",MessageBoxButtons.OK,MessageBoxIcon.Information);
                return false;
            }
            if(this.textBox3.Text==string.Empty)
            {
                MessageBox.Show(this,"请输入下倾角","提示",MessageBoxButtons.OK,MessageBoxIcon.Information);
                return false;
            }
            this.cellName=textBox1.Text;
            try{double.TryParse(this.textBox2.Text,out this.direction);}
            catch
            {
                MessageBox.Show(this,"您输入的方位角格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox2.Focus();
                return false;
            }
            try{double.TryParse(this.textBox3.Text,out this.inclination);}
            catch
            {
                MessageBox.Show(this,"您输入的下倾角格式有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.textBox3.Focus();
                return false;
            }
            return true;
        }


    }
}
