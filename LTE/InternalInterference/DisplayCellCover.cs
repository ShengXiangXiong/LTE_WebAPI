using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LTE.InternalInterference
{
    public partial class DisplayCellCover : Form
    {
        private string cellName;
        private CellInfo cellInfo;

        public DisplayCellCover()
        {
            InitializeComponent();
        }

        private bool validateCell()
        {
            cellInfo = new CellInfo();

            if (this.CellName.Text == string.Empty)
            {
                MessageBox.Show(this, "请输入小区名称", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            this.cellName = CellName.Text.Trim();
            DataTable dt = DB.IbatisHelper.ExecuteQueryForDataTable("SingleGetCellType", this.cellName);
            if (dt.Rows.Count == 0)
            {
                MessageBox.Show(this, "您输入的小区名称有误，请重新输入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            cellInfo.SourceName = cellName;
            cellInfo.eNodeB = Convert.ToInt32(dt.Rows[0]["eNodeB"]);
            cellInfo.CI = Convert.ToInt32(dt.Rows[0]["CI"]);
            return true;
        }


        private void button1_Click(object sender, EventArgs e)
        {
            if (!validateCell())
                return;

            AnalysisEntry.DisplayAnalysis(cellInfo);
            MessageBox.Show("已呈现！");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!validateCell())
                return;

            AnalysisEntry.Display3DAnalysis(cellInfo);
            MessageBox.Show("已呈现！");
        }
    }
}
