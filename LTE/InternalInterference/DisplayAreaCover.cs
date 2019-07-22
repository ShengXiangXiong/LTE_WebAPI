using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LTE.GIS;

namespace LTE.InternalInterference
{
    public partial class DisplayAreaCover : Form
    {
        public DisplayAreaCover()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int mingxid , maxgxid, mingyid, maxgyid;
            mingxid = maxgxid = mingyid = maxgyid = -1;
            if ( ! valideInput(ref mingxid, ref maxgxid, ref mingyid, ref maxgyid) )
            {
                MessageBox.Show("边界网格输入有误！");
                return;
            }
            OperateCoverGirdLayer operateGrid = new OperateCoverGirdLayer(LayerNames.AreaCoverGrids);
            operateGrid.ClearLayer();
            operateGrid.constuctAreaGrids(mingxid, mingyid, maxgxid, maxgyid);
            MessageBox.Show("已呈现！");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int mingxid, maxgxid, mingyid, maxgyid;
            mingxid = maxgxid = mingyid = maxgyid = -1;
            if (!valideInput(ref mingxid, ref maxgxid, ref mingyid, ref maxgyid))
            {
                MessageBox.Show("边界网格输入有误！");
                return;
            }

            OperateCoverGird3DLayer operateGrid3d = new OperateCoverGird3DLayer(LayerNames.AreaCoverGrid3Ds);
            operateGrid3d.ClearLayer();
            operateGrid3d.constuctAreaGrid3Ds(mingxid, mingyid, maxgxid, maxgyid);
            MessageBox.Show("已呈现！");
        }

        private bool valideInput(ref int mingxid, ref int maxgxid, ref int mingyid, ref int maxgyid)
        {
            if ( this.textBox1.Text != "" && this.textBox2.Text != "" && this.textBox3.Text != "" && this.textBox4.Text != "")
            {
                try{
                    mingxid = Convert.ToInt32(this.textBox1.Text);
                    maxgxid = Convert.ToInt32(this.textBox2.Text);
                    mingyid = Convert.ToInt32(this.textBox3.Text);
                    maxgyid = Convert.ToInt32(this.textBox4.Text);
                }
                catch(Exception e)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
