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
    public partial class PropertyGridControl : UserControl
    {
        private static PropertyGridControl instance = null; //当前对象的实例
        private static System.Object m_syncObject = new System.Object();   // 同步对象

        public PropertyGridControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 当前实例对象
        /// </summary>
        public static PropertyGridControl Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (m_syncObject)
                    {
                        if (instance == null)
                        {
                            instance = new PropertyGridControl();
                        }
                    }

                }
                return instance;
            }
        }

        /// <summary>
        /// 给属性赋值
        /// </summary>
        /// <param name="o"></param>
        public void SetObject(object obj)
        {
            if (this.propertyGrid1.InvokeRequired)
                this.propertyGrid1.BeginInvoke(new SetObjectHander(this.SetObject), obj);
            else
            {
                this.propertyGrid1.SelectedObject = obj;
            }
        }

        private delegate void SetObjectHander(object obj);

        private void propertyGrid1_Click(object sender, EventArgs e)
        {

        }

    }
}
