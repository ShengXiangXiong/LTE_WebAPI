using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LTE.GIS
{
    /// <summary>
    /// 右边的控件
    /// </summary>
    public partial class RightControl : UserControl
    {
        #region 私有变量
        private string type = null;
        private static RightControl instance = null; //当前对象的实例
        private static System.Object m_syncObject = new System.Object();   // 同步对象
        #endregion 私有变量
        #region 公共属性

        /// <summary>
        /// 当前实例对象
        /// </summary>
        public static RightControl Instance
        {
            get
            {
                //if (instance == null)
                //{
                //    lock (m_syncObject)
                //    {
                        if (instance == null)
                        {
                            instance = new RightControl();
                        }
                    //}

                //}
                return instance;
            }
        }

        /// <summary>
        /// 干扰分类(GSM或TD)
        /// </summary>
        public string Type
        {
            get { return type; }
            set { type = value; }
        }
        #endregion 公共属性

        public RightControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 清空所有的控件
        /// </summary>
        public void ClearAllControl()
        {
            foreach(Control control in this.Controls)
            {
                this.Controls.Remove(control);
                this.Refresh();
            }
           

        }

        /// <summary>
        /// 添加新的控件
        /// </summary>
        /// <param name="control"></param>
        public void AddControl(Control control)
        {
            control.Dock = DockStyle.Top;
            control.Width = this.Width;
            this.Controls.Add(control);

        }

        private void RightControl_Load(object sender, EventArgs e)
        {

        }


    }
}
