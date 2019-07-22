namespace LTE.GIS
{
    partial class GISForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GISForm));
            this.pnlTool = new System.Windows.Forms.Panel();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.Navigation = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.ZoomInOut = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.TargetCenter = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.ZoomIn = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.zoomout = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.narrow = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this.expand = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            this.pan = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
            this.fullextent = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
            this.SelectFeatures = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
            this.select = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator12 = new System.Windows.Forms.ToolStripSeparator();
            this.open = new System.Windows.Forms.ToolStripButton();
            this.pnlMap = new System.Windows.Forms.Panel();
            this.pnlRight = new System.Windows.Forms.Panel();
            this.axLicenseControl1 = new ESRI.ArcGIS.Controls.AxLicenseControl();
            this.pnlLeft = new System.Windows.Forms.Panel();
            this.axTOCControl1 = new ESRI.ArcGIS.Controls.AxTOCControl();
            this.axSceneControl1 = new ESRI.ArcGIS.Controls.AxSceneControl();
            this.axLicenseControl2 = new ESRI.ArcGIS.Controls.AxLicenseControl();
            this.pnlTool.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.pnlMap.SuspendLayout();
            this.pnlRight.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.axLicenseControl1)).BeginInit();
            this.pnlLeft.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.axTOCControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.axSceneControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.axLicenseControl2)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlTool
            // 
            this.pnlTool.Controls.Add(this.toolStrip1);
            this.pnlTool.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTool.Location = new System.Drawing.Point(0, 0);
            this.pnlTool.Margin = new System.Windows.Forms.Padding(4);
            this.pnlTool.Name = "pnlTool";
            this.pnlTool.Size = new System.Drawing.Size(957, 38);
            this.pnlTool.TabIndex = 0;
            this.pnlTool.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlTool_Paint);
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Navigation,
            this.toolStripSeparator1,
            this.ZoomInOut,
            this.toolStripSeparator3,
            this.TargetCenter,
            this.toolStripSeparator4,
            this.ZoomIn,
            this.toolStripSeparator5,
            this.zoomout,
            this.toolStripSeparator6,
            this.narrow,
            this.toolStripSeparator7,
            this.expand,
            this.toolStripSeparator8,
            this.pan,
            this.toolStripSeparator9,
            this.fullextent,
            this.toolStripSeparator10,
            this.SelectFeatures,
            this.toolStripSeparator11,
            this.select,
            this.toolStripSeparator12,
            this.open});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(957, 31);
            this.toolStrip1.TabIndex = 2;
            this.toolStrip1.Text = "toolStrip1";
            this.toolStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.toolStrip1_ItemClicked);
            // 
            // Navigation
            // 
            this.Navigation.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.Navigation.Image = ((System.Drawing.Image)(resources.GetObject("Navigation.Image")));
            this.Navigation.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.Navigation.Name = "Navigation";
            this.Navigation.Size = new System.Drawing.Size(28, 28);
            this.Navigation.Text = "Navigation";
            this.Navigation.Click += new System.EventHandler(this.Navigation_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 31);
            // 
            // ZoomInOut
            // 
            this.ZoomInOut.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.ZoomInOut.Image = ((System.Drawing.Image)(resources.GetObject("ZoomInOut.Image")));
            this.ZoomInOut.Name = "ZoomInOut";
            this.ZoomInOut.Size = new System.Drawing.Size(28, 28);
            this.ZoomInOut.Text = "ZoomInOut";
            this.ZoomInOut.Click += new System.EventHandler(this.ZoomInOut_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 31);
            // 
            // TargetCenter
            // 
            this.TargetCenter.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.TargetCenter.Image = ((System.Drawing.Image)(resources.GetObject("TargetCenter.Image")));
            this.TargetCenter.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.TargetCenter.Name = "TargetCenter";
            this.TargetCenter.Size = new System.Drawing.Size(28, 28);
            this.TargetCenter.Text = "TargetCenter";
            this.TargetCenter.Click += new System.EventHandler(this.TargetCenter_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(6, 31);
            // 
            // ZoomIn
            // 
            this.ZoomIn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.ZoomIn.Image = ((System.Drawing.Image)(resources.GetObject("ZoomIn.Image")));
            this.ZoomIn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ZoomIn.Name = "ZoomIn";
            this.ZoomIn.Size = new System.Drawing.Size(28, 28);
            this.ZoomIn.Text = "ZoomIn";
            this.ZoomIn.Click += new System.EventHandler(this.ZoomIn_Click);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(6, 31);
            // 
            // zoomout
            // 
            this.zoomout.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.zoomout.Image = global::LTE.Properties.Resources.zoomout;
            this.zoomout.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.zoomout.Name = "zoomout";
            this.zoomout.Size = new System.Drawing.Size(28, 28);
            this.zoomout.Text = "zoomout";
            this.zoomout.Click += new System.EventHandler(this.zoomout_Click);
            // 
            // toolStripSeparator6
            // 
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            this.toolStripSeparator6.Size = new System.Drawing.Size(6, 31);
            // 
            // narrow
            // 
            this.narrow.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.narrow.Image = ((System.Drawing.Image)(resources.GetObject("narrow.Image")));
            this.narrow.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.narrow.Name = "narrow";
            this.narrow.Size = new System.Drawing.Size(28, 28);
            this.narrow.Text = "narrow";
            this.narrow.Click += new System.EventHandler(this.narrow_Click);
            // 
            // toolStripSeparator7
            // 
            this.toolStripSeparator7.Name = "toolStripSeparator7";
            this.toolStripSeparator7.Size = new System.Drawing.Size(6, 31);
            // 
            // expand
            // 
            this.expand.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.expand.Image = ((System.Drawing.Image)(resources.GetObject("expand.Image")));
            this.expand.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.expand.Name = "expand";
            this.expand.Size = new System.Drawing.Size(28, 28);
            this.expand.Text = "expand";
            this.expand.Click += new System.EventHandler(this.expand_Click);
            // 
            // toolStripSeparator8
            // 
            this.toolStripSeparator8.Name = "toolStripSeparator8";
            this.toolStripSeparator8.Size = new System.Drawing.Size(6, 31);
            // 
            // pan
            // 
            this.pan.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.pan.Image = ((System.Drawing.Image)(resources.GetObject("pan.Image")));
            this.pan.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.pan.Name = "pan";
            this.pan.Size = new System.Drawing.Size(28, 28);
            this.pan.Text = "pan";
            this.pan.Click += new System.EventHandler(this.pan_Click);
            // 
            // toolStripSeparator9
            // 
            this.toolStripSeparator9.Name = "toolStripSeparator9";
            this.toolStripSeparator9.Size = new System.Drawing.Size(6, 31);
            // 
            // fullextent
            // 
            this.fullextent.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.fullextent.Image = ((System.Drawing.Image)(resources.GetObject("fullextent.Image")));
            this.fullextent.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.fullextent.Name = "fullextent";
            this.fullextent.Size = new System.Drawing.Size(28, 28);
            this.fullextent.Text = "fullextent";
            this.fullextent.Click += new System.EventHandler(this.fullextent_Click);
            // 
            // toolStripSeparator10
            // 
            this.toolStripSeparator10.Name = "toolStripSeparator10";
            this.toolStripSeparator10.Size = new System.Drawing.Size(6, 31);
            // 
            // SelectFeatures
            // 
            this.SelectFeatures.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.SelectFeatures.Image = ((System.Drawing.Image)(resources.GetObject("SelectFeatures.Image")));
            this.SelectFeatures.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.SelectFeatures.Name = "SelectFeatures";
            this.SelectFeatures.Size = new System.Drawing.Size(28, 28);
            this.SelectFeatures.Text = "SelectFeatures";
            this.SelectFeatures.Click += new System.EventHandler(this.SelectFeatures_Click);
            // 
            // toolStripSeparator11
            // 
            this.toolStripSeparator11.Name = "toolStripSeparator11";
            this.toolStripSeparator11.Size = new System.Drawing.Size(6, 31);
            // 
            // select
            // 
            this.select.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.select.Image = ((System.Drawing.Image)(resources.GetObject("select.Image")));
            this.select.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.select.Name = "select";
            this.select.Size = new System.Drawing.Size(28, 28);
            this.select.Text = "select";
            this.select.Click += new System.EventHandler(this.select_Click);
            // 
            // toolStripSeparator12
            // 
            this.toolStripSeparator12.Name = "toolStripSeparator12";
            this.toolStripSeparator12.Size = new System.Drawing.Size(6, 31);
            // 
            // open
            // 
            this.open.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.open.Image = ((System.Drawing.Image)(resources.GetObject("open.Image")));
            this.open.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.open.Name = "open";
            this.open.Size = new System.Drawing.Size(28, 28);
            this.open.Text = "open";
            this.open.Click += new System.EventHandler(this.open_Click);
            // 
            // pnlMap
            // 
            this.pnlMap.Controls.Add(this.pnlRight);
            this.pnlMap.Controls.Add(this.pnlLeft);
            this.pnlMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMap.Location = new System.Drawing.Point(0, 38);
            this.pnlMap.Margin = new System.Windows.Forms.Padding(4);
            this.pnlMap.Name = "pnlMap";
            this.pnlMap.Size = new System.Drawing.Size(957, 493);
            this.pnlMap.TabIndex = 1;
            // 
            // pnlRight
            // 
            this.pnlRight.Controls.Add(this.axLicenseControl2);
            this.pnlRight.Controls.Add(this.axSceneControl1);
            this.pnlRight.Controls.Add(this.axLicenseControl1);
            this.pnlRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlRight.Location = new System.Drawing.Point(195, 0);
            this.pnlRight.Margin = new System.Windows.Forms.Padding(4);
            this.pnlRight.Name = "pnlRight";
            this.pnlRight.Size = new System.Drawing.Size(762, 493);
            this.pnlRight.TabIndex = 5;
            // 
            // axLicenseControl1
            // 
            this.axLicenseControl1.Enabled = true;
            this.axLicenseControl1.Location = new System.Drawing.Point(346, 149);
            this.axLicenseControl1.Margin = new System.Windows.Forms.Padding(4);
            this.axLicenseControl1.Name = "axLicenseControl1";
            this.axLicenseControl1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axLicenseControl1.OcxState")));
            this.axLicenseControl1.Size = new System.Drawing.Size(32, 32);
            this.axLicenseControl1.TabIndex = 1;
            // 
            // pnlLeft
            // 
            this.pnlLeft.Controls.Add(this.axTOCControl1);
            this.pnlLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlLeft.Location = new System.Drawing.Point(0, 0);
            this.pnlLeft.Margin = new System.Windows.Forms.Padding(4);
            this.pnlLeft.Name = "pnlLeft";
            this.pnlLeft.Size = new System.Drawing.Size(195, 493);
            this.pnlLeft.TabIndex = 6;
            // 
            // axTOCControl1
            // 
            this.axTOCControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axTOCControl1.Location = new System.Drawing.Point(0, 0);
            this.axTOCControl1.Margin = new System.Windows.Forms.Padding(4);
            this.axTOCControl1.Name = "axTOCControl1";
            this.axTOCControl1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axTOCControl1.OcxState")));
            this.axTOCControl1.Size = new System.Drawing.Size(195, 493);
            this.axTOCControl1.TabIndex = 3;
            this.axTOCControl1.OnMouseDown += new ESRI.ArcGIS.Controls.ITOCControlEvents_Ax_OnMouseDownEventHandler(this.axTOCControl1_OnMouseDown);
            // 
            // axSceneControl1
            // 
            this.axSceneControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axSceneControl1.Location = new System.Drawing.Point(0, 0);
            this.axSceneControl1.Margin = new System.Windows.Forms.Padding(4);
            this.axSceneControl1.Name = "axSceneControl1";
            this.axSceneControl1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axSceneControl1.OcxState")));
            this.axSceneControl1.Size = new System.Drawing.Size(762, 493);
            this.axSceneControl1.TabIndex = 2;
            this.axSceneControl1.OnMouseDown += new ESRI.ArcGIS.Controls.ISceneControlEvents_Ax_OnMouseDownEventHandler(this.axSceneControl1_OnMouseDown);
            this.axSceneControl1.OnMouseUp += new ESRI.ArcGIS.Controls.ISceneControlEvents_Ax_OnMouseUpEventHandler(this.axSceneControl1_OnMouseUp);
            this.axSceneControl1.OnMouseMove += new ESRI.ArcGIS.Controls.ISceneControlEvents_Ax_OnMouseMoveEventHandler(this.axSceneControl1_OnMouseMove);
            // 
            // axLicenseControl2
            // 
            this.axLicenseControl2.Enabled = true;
            this.axLicenseControl2.Location = new System.Drawing.Point(229, 456);
            this.axLicenseControl2.Name = "axLicenseControl2";
            this.axLicenseControl2.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axLicenseControl2.OcxState")));
            this.axLicenseControl2.Size = new System.Drawing.Size(32, 32);
            this.axLicenseControl2.TabIndex = 3;
            // 
            // GISForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(957, 531);
            this.Controls.Add(this.pnlMap);
            this.Controls.Add(this.pnlTool);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "GISForm";
            this.Text = "地图窗口";
            this.Activated += new System.EventHandler(this.GISForm_Activated);
            this.Deactivate += new System.EventHandler(this.GISForm_Deactivate);
            this.pnlTool.ResumeLayout(false);
            this.pnlTool.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.pnlMap.ResumeLayout(false);
            this.pnlRight.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.axLicenseControl1)).EndInit();
            this.pnlLeft.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.axTOCControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.axSceneControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.axLicenseControl2)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlTool;
        private System.Windows.Forms.Panel pnlMap;
        private ESRI.ArcGIS.Controls.AxLicenseControl axLicenseControl1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton ZoomInOut;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripButton TargetCenter;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripButton ZoomIn;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripButton zoomout;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        private System.Windows.Forms.ToolStripButton narrow;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
        private System.Windows.Forms.ToolStripButton expand;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        private System.Windows.Forms.ToolStripButton pan;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
        private System.Windows.Forms.ToolStripButton fullextent;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator10;
        private System.Windows.Forms.ToolStripButton SelectFeatures;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator11;
        private System.Windows.Forms.ToolStripButton select;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator12;
        private System.Windows.Forms.ToolStripButton open;
        private ESRI.ArcGIS.Controls.AxTOCControl axTOCControl1;
        private System.Windows.Forms.Panel pnlRight;
        private System.Windows.Forms.Panel pnlLeft;
        private System.Windows.Forms.ToolStripButton Navigation;
        private ESRI.ArcGIS.Controls.AxSceneControl axSceneControl1;
        private ESRI.ArcGIS.Controls.AxLicenseControl axLicenseControl2;
    }
}

