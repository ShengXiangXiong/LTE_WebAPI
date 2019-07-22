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
    public class FrmSingleRayTracing : Form
    {
        public string cellName;
        protected string cellType;
        protected DataTable dt;
        public FrmSingleRayTracing()
        {
            dt = new DataTable();
            dt.Columns.Add("小区名称");
            dt.Columns.Add("EIRP");
            dt.Columns.Add("射线途经距离");
            dt.Columns.Add("接收功率");
            dt.Columns.Add("路径损耗");
        }

        public void AddRow(string cellname, double EIRP, double distance, double recePower, double pathLoss)
        {
            DataRow row = this.dt.NewRow();
            row[0] = cellname;
            row[1] = EIRP;
            row[2] = distance;
            row[3] = recePower;
            row[4] = pathLoss;
            this.dt.Rows.Add(row);
        }

        protected bool validateCell()
        {
            object o = IbatisHelper.ExecuteScalar("SingleGetCellType", this.cellName);
            if (o == null)
            {
                MessageBox.Show(this, "您输入的小区名称有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            this.cellType = o.ToString();
            return true;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // FrmSingleRayTracing
            // 
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Name = "FrmSingleRayTracing";
            this.Load += new System.EventHandler(this.FrmSingleRayTracing_Load);
            this.ResumeLayout(false);

        }

        private void FrmSingleRayTracing_Load(object sender, EventArgs e)
        {

        }

    }
}

