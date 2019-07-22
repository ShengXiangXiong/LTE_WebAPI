using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Reflection;

using LTE.InternalInterference;
using LTE.GIS;
using LTE.Calibration;

namespace LTE
{
    public partial class MainForm : Form
    {
        private FlowLayoutPanel flowLayoutPanel1;
        private FlowLayoutPanel flowLayoutPanel3;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem 定义网格ToolStripMenuItem;
        private ToolStripMenuItem 刷新图层ToolStripMenuItem;
        private ToolStripMenuItem 射线追踪ToolStripMenuItem;
        private ToolStripContainer toolStripContainer1;
        private FlowLayoutPanel flowLayoutPanel2;
        private FlowLayoutPanel flowLayoutPanel5;
        private FlowLayoutPanel flowLayoutPanel6;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private FlowLayoutPanel flowLayoutPanel7;
        private ToolStripMenuItem 定义网格ToolStripMenuItem1;
        private ToolStripMenuItem 小区图层ToolStripMenuItem;
        private ToolStripMenuItem 小区覆盖半径计算ToolStripMenuItem;
        private Panel panel1;
        private FlowLayoutPanel flowLayoutPanel4;
        private ToolStripMenuItem 单射线跟踪ToolStripMenuItem;
        private ToolStripMenuItem 小区覆盖分析ToolStripMenuItem;
        private ToolStripMenuItem 区域覆盖呈现ToolStripMenuItem;
        private ToolStripMenuItem 干扰源定位ToolStripMenuItem;
        private ToolStripMenuItem 射线记录ToolStripMenuItem;
        private TabControl tabControl2;
        private TabPage tabPage1;
        private ToolStripMenuItem 覆盖缺陷分析ToolStripMenuItem;
        private ToolStripMenuItem 建筑物平滑ToolStripMenuItem;
        private ToolStripMenuItem 指定地面交点ToolStripMenuItem;
        private ToolStripMenuItem 指定射线方向ToolStripMenuItem;
        private ToolStripMenuItem 小区覆盖呈现ToolStripMenuItem;
        private ToolStripMenuItem 路测刷新ToolStripMenuItem;
        private ToolStripMenuItem 虚拟路测ToolStripMenuItem;
        private ToolStripMenuItem 调试ToolStripMenuItem;
        private ToolStripMenuItem 小区覆盖ToolStripMenuItem;
        private ToolStripMenuItem 射线记录ToolStripMenuItem1;
        private ToolStripMenuItem 实际路测导入ToolStripMenuItem;
        private ToolStripMenuItem demoToolStripMenuItem;
        private ToolStripMenuItem 多条线ToolStripMenuItem;
        private ToolStripMenuItem 一条线ToolStripMenuItem;
        private ToolStripMenuItem 系数校正ToolStripMenuItem;
        private ToolStripMenuItem 路测数据预处理ToolStripMenuItem1;
        private ToolStripMenuItem 系数校正ToolStripMenuItem1;
        private ToolStripMenuItem 场景划分ToolStripMenuItem;
        private ToolStripMenuItem 场景划分ToolStripMenuItem1;
        private ToolStripMenuItem 网外干扰源ToolStripMenuItem;
        private ToolStripMenuItem 扇区ToolStripMenuItem;
        private ToolStripMenuItem 多个扇区ToolStripMenuItem;
        private ToolStripMenuItem tINToolStripMenuItem1;
        private ToolStripMenuItem 建筑物底边平滑ToolStripMenuItem;
        private ToolStripMenuItem 建筑物ToolStripMenuItem;
        login.Login loginForm;

        public MainForm(login.Login loginForm)
        {
            InitializeComponent();
            this.loginForm = loginForm;
        }

        private void axTOCControl1_OnMouseDown(object sender, ESRI.ArcGIS.Controls.ITOCControlEvents_OnMouseDownEvent e)
        {

        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {

        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.定义网格ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.建筑物平滑ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.定义网格ToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.小区覆盖半径计算ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.虚拟路测ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.实际路测导入ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.场景划分ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.射线追踪ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.单射线跟踪ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.指定地面交点ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.指定射线方向ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.小区覆盖分析ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.射线记录ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.调试ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.小区覆盖ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.射线记录ToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.刷新图层ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.建筑物底边平滑ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.小区图层ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.小区覆盖呈现ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.区域覆盖呈现ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.路测刷新ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.网外干扰源ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tINToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.覆盖缺陷分析ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.干扰源定位ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.系数校正ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.场景划分ToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.路测数据预处理ToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.系数校正ToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.demoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.一条线ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.多条线ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.扇区ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.多个扇区ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel5 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel6 = new System.Windows.Forms.FlowLayoutPanel();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.flowLayoutPanel7 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel4 = new System.Windows.Forms.FlowLayoutPanel();
            this.tabControl2 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.建筑物ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.flowLayoutPanel1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.flowLayoutPanel3.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.flowLayoutPanel6.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.flowLayoutPanel4.SuspendLayout();
            this.tabControl2.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.menuStrip1);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(1201, 40);
            this.flowLayoutPanel1.TabIndex = 0;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.定义网格ToolStripMenuItem,
            this.射线追踪ToolStripMenuItem,
            this.刷新图层ToolStripMenuItem,
            this.覆盖缺陷分析ToolStripMenuItem,
            this.干扰源定位ToolStripMenuItem,
            this.系数校正ToolStripMenuItem,
            this.demoToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(710, 28);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // 定义网格ToolStripMenuItem
            // 
            this.定义网格ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.建筑物平滑ToolStripMenuItem,
            this.定义网格ToolStripMenuItem1,
            this.小区覆盖半径计算ToolStripMenuItem,
            this.虚拟路测ToolStripMenuItem,
            this.实际路测导入ToolStripMenuItem,
            this.场景划分ToolStripMenuItem});
            this.定义网格ToolStripMenuItem.Name = "定义网格ToolStripMenuItem";
            this.定义网格ToolStripMenuItem.Size = new System.Drawing.Size(66, 24);
            this.定义网格ToolStripMenuItem.Text = "预处理";
            // 
            // 建筑物平滑ToolStripMenuItem
            // 
            this.建筑物平滑ToolStripMenuItem.Name = "建筑物平滑ToolStripMenuItem";
            this.建筑物平滑ToolStripMenuItem.Size = new System.Drawing.Size(228, 24);
            this.建筑物平滑ToolStripMenuItem.Text = "建筑物底边平滑";
            this.建筑物平滑ToolStripMenuItem.Click += new System.EventHandler(this.建筑物平滑ToolStripMenuItem_Click);
            // 
            // 定义网格ToolStripMenuItem1
            // 
            this.定义网格ToolStripMenuItem1.Name = "定义网格ToolStripMenuItem1";
            this.定义网格ToolStripMenuItem1.Size = new System.Drawing.Size(228, 24);
            this.定义网格ToolStripMenuItem1.Text = "网格划分";
            this.定义网格ToolStripMenuItem1.Click += new System.EventHandler(this.定义网格ToolStripMenuItem1_Click);
            // 
            // 小区覆盖半径计算ToolStripMenuItem
            // 
            this.小区覆盖半径计算ToolStripMenuItem.Name = "小区覆盖半径计算ToolStripMenuItem";
            this.小区覆盖半径计算ToolStripMenuItem.Size = new System.Drawing.Size(228, 24);
            this.小区覆盖半径计算ToolStripMenuItem.Text = "小区理论覆盖半径计算";
            this.小区覆盖半径计算ToolStripMenuItem.Click += new System.EventHandler(this.小区覆盖半径计算ToolStripMenuItem_Click);
            // 
            // 虚拟路测ToolStripMenuItem
            // 
            this.虚拟路测ToolStripMenuItem.Name = "虚拟路测ToolStripMenuItem";
            this.虚拟路测ToolStripMenuItem.Size = new System.Drawing.Size(228, 24);
            this.虚拟路测ToolStripMenuItem.Text = "虚拟路测生成";
            this.虚拟路测ToolStripMenuItem.Click += new System.EventHandler(this.虚拟路测ToolStripMenuItem_Click);
            // 
            // 实际路测导入ToolStripMenuItem
            // 
            this.实际路测导入ToolStripMenuItem.Name = "实际路测导入ToolStripMenuItem";
            this.实际路测导入ToolStripMenuItem.Size = new System.Drawing.Size(228, 24);
            this.实际路测导入ToolStripMenuItem.Text = "数据导入";
            this.实际路测导入ToolStripMenuItem.Click += new System.EventHandler(this.实际路测导入ToolStripMenuItem_Click);
            // 
            // 场景划分ToolStripMenuItem
            // 
            this.场景划分ToolStripMenuItem.Name = "场景划分ToolStripMenuItem";
            this.场景划分ToolStripMenuItem.Size = new System.Drawing.Size(228, 24);
            // 
            // 射线追踪ToolStripMenuItem
            // 
            this.射线追踪ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.单射线跟踪ToolStripMenuItem,
            this.小区覆盖分析ToolStripMenuItem,
            this.射线记录ToolStripMenuItem,
            this.调试ToolStripMenuItem});
            this.射线追踪ToolStripMenuItem.Name = "射线追踪ToolStripMenuItem";
            this.射线追踪ToolStripMenuItem.Size = new System.Drawing.Size(81, 24);
            this.射线追踪ToolStripMenuItem.Text = "覆盖分析";
            // 
            // 单射线跟踪ToolStripMenuItem
            // 
            this.单射线跟踪ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.指定地面交点ToolStripMenuItem,
            this.指定射线方向ToolStripMenuItem});
            this.单射线跟踪ToolStripMenuItem.Name = "单射线跟踪ToolStripMenuItem";
            this.单射线跟踪ToolStripMenuItem.Size = new System.Drawing.Size(168, 24);
            this.单射线跟踪ToolStripMenuItem.Text = " 单射线跟踪";
            // 
            // 指定地面交点ToolStripMenuItem
            // 
            this.指定地面交点ToolStripMenuItem.Name = "指定地面交点ToolStripMenuItem";
            this.指定地面交点ToolStripMenuItem.Size = new System.Drawing.Size(168, 24);
            this.指定地面交点ToolStripMenuItem.Text = "指定地面交点";
            this.指定地面交点ToolStripMenuItem.Click += new System.EventHandler(this.指定地面交点ToolStripMenuItem_Click);
            // 
            // 指定射线方向ToolStripMenuItem
            // 
            this.指定射线方向ToolStripMenuItem.Name = "指定射线方向ToolStripMenuItem";
            this.指定射线方向ToolStripMenuItem.Size = new System.Drawing.Size(168, 24);
            this.指定射线方向ToolStripMenuItem.Text = "指定射线方向";
            this.指定射线方向ToolStripMenuItem.Click += new System.EventHandler(this.指定射线方向ToolStripMenuItem_Click);
            // 
            // 小区覆盖分析ToolStripMenuItem
            // 
            this.小区覆盖分析ToolStripMenuItem.Name = "小区覆盖分析ToolStripMenuItem";
            this.小区覆盖分析ToolStripMenuItem.Size = new System.Drawing.Size(168, 24);
            this.小区覆盖分析ToolStripMenuItem.Text = "小区覆盖计算";
            this.小区覆盖分析ToolStripMenuItem.Click += new System.EventHandler(this.小区覆盖分析ToolStripMenuItem_Click);
            // 
            // 射线记录ToolStripMenuItem
            // 
            this.射线记录ToolStripMenuItem.Name = "射线记录ToolStripMenuItem";
            this.射线记录ToolStripMenuItem.Size = new System.Drawing.Size(168, 24);
            this.射线记录ToolStripMenuItem.Text = "射线记录";
            this.射线记录ToolStripMenuItem.Click += new System.EventHandler(this.射线记录ToolStripMenuItem_Click);
            // 
            // 调试ToolStripMenuItem
            // 
            this.调试ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.小区覆盖ToolStripMenuItem,
            this.射线记录ToolStripMenuItem1});
            this.调试ToolStripMenuItem.Name = "调试ToolStripMenuItem";
            this.调试ToolStripMenuItem.Size = new System.Drawing.Size(168, 24);
            this.调试ToolStripMenuItem.Text = "调试";
            // 
            // 小区覆盖ToolStripMenuItem
            // 
            this.小区覆盖ToolStripMenuItem.Name = "小区覆盖ToolStripMenuItem";
            this.小区覆盖ToolStripMenuItem.Size = new System.Drawing.Size(138, 24);
            this.小区覆盖ToolStripMenuItem.Text = "小区覆盖";
            this.小区覆盖ToolStripMenuItem.Click += new System.EventHandler(this.小区覆盖ToolStripMenuItem_Click);
            // 
            // 射线记录ToolStripMenuItem1
            // 
            this.射线记录ToolStripMenuItem1.Name = "射线记录ToolStripMenuItem1";
            this.射线记录ToolStripMenuItem1.Size = new System.Drawing.Size(138, 24);
            this.射线记录ToolStripMenuItem1.Text = "射线记录";
            this.射线记录ToolStripMenuItem1.Click += new System.EventHandler(this.射线记录ToolStripMenuItem1_Click);
            // 
            // 刷新图层ToolStripMenuItem
            // 
            this.刷新图层ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.建筑物ToolStripMenuItem,
            this.建筑物底边平滑ToolStripMenuItem,
            this.tINToolStripMenuItem1,
            this.小区图层ToolStripMenuItem,
            this.小区覆盖呈现ToolStripMenuItem,
            this.区域覆盖呈现ToolStripMenuItem,
            this.路测刷新ToolStripMenuItem,
            this.网外干扰源ToolStripMenuItem});
            this.刷新图层ToolStripMenuItem.Name = "刷新图层ToolStripMenuItem";
            this.刷新图层ToolStripMenuItem.Size = new System.Drawing.Size(81, 24);
            this.刷新图层ToolStripMenuItem.Text = "图层刷新";
            // 
            // 建筑物底边平滑ToolStripMenuItem
            // 
            this.建筑物底边平滑ToolStripMenuItem.Name = "建筑物底边平滑ToolStripMenuItem";
            this.建筑物底边平滑ToolStripMenuItem.Size = new System.Drawing.Size(183, 24);
            this.建筑物底边平滑ToolStripMenuItem.Text = "建筑物底边平滑";
            this.建筑物底边平滑ToolStripMenuItem.Click += new System.EventHandler(this.建筑物底边平滑ToolStripMenuItem_Click);
            // 
            // 小区图层ToolStripMenuItem
            // 
            this.小区图层ToolStripMenuItem.Name = "小区图层ToolStripMenuItem";
            this.小区图层ToolStripMenuItem.Size = new System.Drawing.Size(183, 24);
            this.小区图层ToolStripMenuItem.Text = "小区";
            this.小区图层ToolStripMenuItem.Click += new System.EventHandler(this.小区图层ToolStripMenuItem_Click);
            // 
            // 小区覆盖呈现ToolStripMenuItem
            // 
            this.小区覆盖呈现ToolStripMenuItem.Name = "小区覆盖呈现ToolStripMenuItem";
            this.小区覆盖呈现ToolStripMenuItem.Size = new System.Drawing.Size(183, 24);
            this.小区覆盖呈现ToolStripMenuItem.Text = "小区覆盖";
            this.小区覆盖呈现ToolStripMenuItem.Click += new System.EventHandler(this.小区覆盖呈现ToolStripMenuItem_Click);
            // 
            // 区域覆盖呈现ToolStripMenuItem
            // 
            this.区域覆盖呈现ToolStripMenuItem.Name = "区域覆盖呈现ToolStripMenuItem";
            this.区域覆盖呈现ToolStripMenuItem.Size = new System.Drawing.Size(183, 24);
            this.区域覆盖呈现ToolStripMenuItem.Text = "区域覆盖";
            this.区域覆盖呈现ToolStripMenuItem.Click += new System.EventHandler(this.区域覆盖呈现ToolStripMenuItem_Click);
            // 
            // 路测刷新ToolStripMenuItem
            // 
            this.路测刷新ToolStripMenuItem.Name = "路测刷新ToolStripMenuItem";
            this.路测刷新ToolStripMenuItem.Size = new System.Drawing.Size(183, 24);
            this.路测刷新ToolStripMenuItem.Text = "路测";
            this.路测刷新ToolStripMenuItem.Click += new System.EventHandler(this.路测刷新ToolStripMenuItem_Click);
            // 
            // 网外干扰源ToolStripMenuItem
            // 
            this.网外干扰源ToolStripMenuItem.Name = "网外干扰源ToolStripMenuItem";
            this.网外干扰源ToolStripMenuItem.Size = new System.Drawing.Size(183, 24);
            this.网外干扰源ToolStripMenuItem.Text = "网外干扰源";
            this.网外干扰源ToolStripMenuItem.Click += new System.EventHandler(this.网外干扰源ToolStripMenuItem_Click);
            // 
            // tINToolStripMenuItem1
            // 
            this.tINToolStripMenuItem1.Name = "tINToolStripMenuItem1";
            this.tINToolStripMenuItem1.Size = new System.Drawing.Size(183, 24);
            this.tINToolStripMenuItem1.Text = "TIN";
            this.tINToolStripMenuItem1.Click += new System.EventHandler(this.tINToolStripMenuItem1_Click);
            // 
            // 覆盖缺陷分析ToolStripMenuItem
            // 
            this.覆盖缺陷分析ToolStripMenuItem.Name = "覆盖缺陷分析ToolStripMenuItem";
            this.覆盖缺陷分析ToolStripMenuItem.Size = new System.Drawing.Size(111, 24);
            this.覆盖缺陷分析ToolStripMenuItem.Text = "网内干扰分析";
            this.覆盖缺陷分析ToolStripMenuItem.Click += new System.EventHandler(this.覆盖缺陷分析ToolStripMenuItem_Click);
            // 
            // 干扰源定位ToolStripMenuItem
            // 
            this.干扰源定位ToolStripMenuItem.Name = "干扰源定位ToolStripMenuItem";
            this.干扰源定位ToolStripMenuItem.Size = new System.Drawing.Size(96, 24);
            this.干扰源定位ToolStripMenuItem.Text = "干扰源定位";
            this.干扰源定位ToolStripMenuItem.Click += new System.EventHandler(this.干扰源定位ToolStripMenuItem_Click);
            // 
            // 系数校正ToolStripMenuItem
            // 
            this.系数校正ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.场景划分ToolStripMenuItem1,
            this.路测数据预处理ToolStripMenuItem1,
            this.系数校正ToolStripMenuItem1});
            this.系数校正ToolStripMenuItem.Name = "系数校正ToolStripMenuItem";
            this.系数校正ToolStripMenuItem.Size = new System.Drawing.Size(81, 24);
            this.系数校正ToolStripMenuItem.Text = "系数校正";
            // 
            // 场景划分ToolStripMenuItem1
            // 
            this.场景划分ToolStripMenuItem1.Name = "场景划分ToolStripMenuItem1";
            this.场景划分ToolStripMenuItem1.Size = new System.Drawing.Size(183, 24);
            this.场景划分ToolStripMenuItem1.Text = "场景划分";
            this.场景划分ToolStripMenuItem1.Click += new System.EventHandler(this.场景划分ToolStripMenuItem1_Click);
            // 
            // 路测数据预处理ToolStripMenuItem1
            // 
            this.路测数据预处理ToolStripMenuItem1.Name = "路测数据预处理ToolStripMenuItem1";
            this.路测数据预处理ToolStripMenuItem1.Size = new System.Drawing.Size(183, 24);
            this.路测数据预处理ToolStripMenuItem1.Text = "路测数据预处理";
            this.路测数据预处理ToolStripMenuItem1.Click += new System.EventHandler(this.路测数据预处理ToolStripMenuItem1_Click);
            // 
            // 系数校正ToolStripMenuItem1
            // 
            this.系数校正ToolStripMenuItem1.Name = "系数校正ToolStripMenuItem1";
            this.系数校正ToolStripMenuItem1.Size = new System.Drawing.Size(183, 24);
            this.系数校正ToolStripMenuItem1.Text = "系数校正";
            this.系数校正ToolStripMenuItem1.Click += new System.EventHandler(this.系数校正ToolStripMenuItem1_Click);
            // 
            // demoToolStripMenuItem
            // 
            this.demoToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.一条线ToolStripMenuItem,
            this.多条线ToolStripMenuItem,
            this.扇区ToolStripMenuItem,
            this.多个扇区ToolStripMenuItem});
            this.demoToolStripMenuItem.Name = "demoToolStripMenuItem";
            this.demoToolStripMenuItem.Size = new System.Drawing.Size(94, 24);
            this.demoToolStripMenuItem.Text = "绘图demo";
            this.demoToolStripMenuItem.Click += new System.EventHandler(this.demoToolStripMenuItem_Click);
            // 
            // 一条线ToolStripMenuItem
            // 
            this.一条线ToolStripMenuItem.Name = "一条线ToolStripMenuItem";
            this.一条线ToolStripMenuItem.Size = new System.Drawing.Size(138, 24);
            this.一条线ToolStripMenuItem.Text = "一条线";
            this.一条线ToolStripMenuItem.Click += new System.EventHandler(this.一条线ToolStripMenuItem_Click);
            // 
            // 多条线ToolStripMenuItem
            // 
            this.多条线ToolStripMenuItem.Name = "多条线ToolStripMenuItem";
            this.多条线ToolStripMenuItem.Size = new System.Drawing.Size(138, 24);
            this.多条线ToolStripMenuItem.Text = "多条线";
            this.多条线ToolStripMenuItem.Click += new System.EventHandler(this.多条线ToolStripMenuItem_Click);
            // 
            // 扇区ToolStripMenuItem
            // 
            this.扇区ToolStripMenuItem.Name = "扇区ToolStripMenuItem";
            this.扇区ToolStripMenuItem.Size = new System.Drawing.Size(138, 24);
            this.扇区ToolStripMenuItem.Text = "扇区";
            this.扇区ToolStripMenuItem.Click += new System.EventHandler(this.扇区ToolStripMenuItem_Click);
            // 
            // 多个扇区ToolStripMenuItem
            // 
            this.多个扇区ToolStripMenuItem.Name = "多个扇区ToolStripMenuItem";
            this.多个扇区ToolStripMenuItem.Size = new System.Drawing.Size(138, 24);
            this.多个扇区ToolStripMenuItem.Text = "多个扇区";
            this.多个扇区ToolStripMenuItem.Click += new System.EventHandler(this.多个扇区ToolStripMenuItem_Click);
            // 
            // flowLayoutPanel3
            // 
            this.flowLayoutPanel3.Controls.Add(this.panel1);
            this.flowLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Right;
            this.flowLayoutPanel3.Location = new System.Drawing.Point(1016, 40);
            this.flowLayoutPanel3.Name = "flowLayoutPanel3";
            this.flowLayoutPanel3.Size = new System.Drawing.Size(185, 583);
            this.flowLayoutPanel3.TabIndex = 2;
            // 
            // panel1
            // 
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(179, 578);
            this.panel1.TabIndex = 0;
            // 
            // toolStripContainer1
            // 
            // 
            // toolStripContainer1.ContentPanel
            // 
            this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(1201, 630);
            this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer1.Name = "toolStripContainer1";
            this.toolStripContainer1.Size = new System.Drawing.Size(1201, 655);
            this.toolStripContainer1.TabIndex = 0;
            this.toolStripContainer1.Text = "toolStripContainer1";
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Controls.Add(this.flowLayoutPanel5);
            this.flowLayoutPanel2.Controls.Add(this.flowLayoutPanel6);
            this.flowLayoutPanel2.Controls.Add(this.flowLayoutPanel7);
            this.flowLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(0, 623);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(1201, 32);
            this.flowLayoutPanel2.TabIndex = 1;
            // 
            // flowLayoutPanel5
            // 
            this.flowLayoutPanel5.Location = new System.Drawing.Point(3, 3);
            this.flowLayoutPanel5.Name = "flowLayoutPanel5";
            this.flowLayoutPanel5.Size = new System.Drawing.Size(282, 100);
            this.flowLayoutPanel5.TabIndex = 0;
            // 
            // flowLayoutPanel6
            // 
            this.flowLayoutPanel6.Controls.Add(this.statusStrip1);
            this.flowLayoutPanel6.Location = new System.Drawing.Point(291, 3);
            this.flowLayoutPanel6.Name = "flowLayoutPanel6";
            this.flowLayoutPanel6.Size = new System.Drawing.Size(590, 100);
            this.flowLayoutPanel6.TabIndex = 1;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 0);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(17, 22);
            this.statusStrip1.TabIndex = 0;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(0, 17);
            // 
            // flowLayoutPanel7
            // 
            this.flowLayoutPanel7.Location = new System.Drawing.Point(3, 109);
            this.flowLayoutPanel7.Name = "flowLayoutPanel7";
            this.flowLayoutPanel7.Size = new System.Drawing.Size(400, 100);
            this.flowLayoutPanel7.TabIndex = 2;
            // 
            // flowLayoutPanel4
            // 
            this.flowLayoutPanel4.Controls.Add(this.tabControl2);
            this.flowLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel4.Location = new System.Drawing.Point(0, 40);
            this.flowLayoutPanel4.Name = "flowLayoutPanel4";
            this.flowLayoutPanel4.Size = new System.Drawing.Size(1016, 583);
            this.flowLayoutPanel4.TabIndex = 3;
            this.flowLayoutPanel4.Paint += new System.Windows.Forms.PaintEventHandler(this.flowLayoutPanel4_Paint);
            // 
            // tabControl2
            // 
            this.tabControl2.Controls.Add(this.tabPage1);
            this.tabControl2.Location = new System.Drawing.Point(3, 3);
            this.tabControl2.Name = "tabControl2";
            this.tabControl2.SelectedIndex = 0;
            this.tabControl2.Size = new System.Drawing.Size(1013, 580);
            this.tabControl2.TabIndex = 1;
            // 
            // tabPage1
            // 
            this.tabPage1.Location = new System.Drawing.Point(4, 25);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(1005, 551);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "地图";
            this.tabPage1.UseVisualStyleBackColor = true;
            this.tabPage1.Click += new System.EventHandler(this.tabPage1_Click);
            // 
            // 建筑物ToolStripMenuItem
            // 
            this.建筑物ToolStripMenuItem.Name = "建筑物ToolStripMenuItem";
            this.建筑物ToolStripMenuItem.Size = new System.Drawing.Size(183, 24);
            this.建筑物ToolStripMenuItem.Text = "建筑物";
            this.建筑物ToolStripMenuItem.Click += new System.EventHandler(this.建筑物ToolStripMenuItem_Click);
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(1201, 655);
            this.Controls.Add(this.flowLayoutPanel4);
            this.Controls.Add(this.flowLayoutPanel3);
            this.Controls.Add(this.flowLayoutPanel2);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.toolStripContainer1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "LTE 网优综合分析系统";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.flowLayoutPanel3.ResumeLayout(false);
            this.toolStripContainer1.ResumeLayout(false);
            this.toolStripContainer1.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel6.ResumeLayout(false);
            this.flowLayoutPanel6.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.flowLayoutPanel4.ResumeLayout(false);
            this.tabControl2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.loginForm.Close();
            Application.Exit();
        }

        #region 加载地图、属性控件
        public int[] s = { 0, 0};         //用来记录form是否打开过

         //在选项卡中生成窗体
        public void GenerateForm(string form, object sender)
        {
            // 反射生成窗体
            switch (form)
            {
                case "GISForm":
                    LTE.GIS.GISForm fm = new LTE.GIS.GISForm(this);
 
                    //设置窗体没有边框 加入到选项卡中
                    fm.FormBorderStyle = FormBorderStyle.None;
                    fm.TopLevel = false;
                    fm.Parent = ((TabControl)sender).SelectedTab;
                    fm.ControlBox = false;
                    fm.Dock = DockStyle.Fill;
                    fm.Show();
                    s[((TabControl)sender).SelectedIndex] = 1;
                    break;
                default :
                    break;
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // 窗体居中
            int x = (System.Windows.Forms.SystemInformation.WorkingArea.Width - this.Size.Width) / 2;
            int y = (System.Windows.Forms.SystemInformation.WorkingArea.Height - this.Size.Height) / 2;
            this.StartPosition = FormStartPosition.Manual; //窗体的位置由Location属性决定
            this.Location = (Point)new Size(x, y);         //窗体的起始位置为(x,y)
            
            //初始打开时就加载GISForm
            string formClass = "GISForm";
            GenerateForm(formClass, tabControl2);

            // 加载右侧属性控件
            this.panel1.Controls.Add(LTE.GIS.PropertyGridControl.Instance);
        }
        #endregion

        public void SetStatus(string msg)
        {
            this.toolStripStatusLabel1.Text = msg;
        }

        private void 定义网格ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            LTE.InternalInterference.FrmGrid frmGrid = new InternalInterference.FrmGrid();
            frmGrid.Show();
        }

        private void 小区图层ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LTE.GIS.RefreshLayerClass refreshLayer = new LTE.GIS.RefreshLayerClass();
            refreshLayer.RefreshGSMLayers();
            MessageBox.Show("小区图层刷新完毕", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void 小区覆盖半径计算ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CellRadius r = new CellRadius();
            r.calcRadius();
            MessageBox.Show("小区覆盖半径计算结束！");
        }

        private void drawCoverageSectors(LTE.Geometric.Point s, double distance, double fromAngle, double toAngle, int sectors)
        {
            double sectorAngle = (toAngle - fromAngle + 360) % 360;
            double angle = sectorAngle / sectors;
            for (int r = 0; r < sectors; r++)
            {
                double from = (fromAngle + r * angle);
                double to = (fromAngle + (r + 1) * angle);
                InterferenceFeatureLayerAnalysis.drawSector(s, from, to, distance);
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            tabControl2.Width = this.flowLayoutPanel4.Width;
            tabControl2.Height = this.flowLayoutPanel4.Height;
        }

        private void 小区覆盖范围ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void flowLayoutPanel4_Paint(object sender, PaintEventArgs e)
        {

        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void 射线记录ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LTE.InternalInterference.RayRecord frm = new LTE.InternalInterference.RayRecord();
            frm.Show();
        }

        private void 模拟路测ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void 建筑物平滑ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SmoothBuildingVertex smooth = new SmoothBuildingVertex();
            smooth.smoothBuildingPoints();
            MessageBox.Show("完成！");
        }

        private void 小区覆盖分析ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmCellRayTracing frm = new FrmCellRayTracing();
            frm.Show();
        }

        private void 指定地面交点ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmSingleRayTracing1 frm = new FrmSingleRayTracing1();
            frm.Show();
        }

        private void 指定射线方向ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmSingleRayTracing2 frm = new FrmSingleRayTracing2();
            frm.Show();
        }

        private void 区域覆盖呈现ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DisplayAreaCover displayArea = new DisplayAreaCover();
            displayArea.Show();
        }

        private void 小区覆盖呈现ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DisplayCellCover displayCell = new DisplayCellCover();
            displayCell.Show();
        }

        private void 覆盖缺陷分析ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AreaCoverDefect displayarea = new AreaCoverDefect();
            displayarea.Show();
        }

        private void 路测刷新ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 绘制路测数据
            OperateDTLayer layer = new OperateDTLayer();
            layer.ClearLayer();
            layer.constuctDTGrids();
            MessageBox.Show("完成！");
        }

        private void 初始区域ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void 干扰源定位ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InfLocate infArea = new InfLocate();
            infArea.Show();
        }

        private void 虚拟路测ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 注意：确保已完成区域覆盖计算

            // 每条路径上的点序列，确保每两个点之间的路径接近于直线
            double[] xx1 = { 667987, 667596, 667172, 667001 };
            double[] yy1 = { 3545330, 3545454, 3545668, 3545775 };
            double[] xx2 = { 667001, 667063, 667090, 667260, 667264, 667296, 667338, 667299, 667192 };
            double[] yy2 = { 3545775, 3546033, 3546242, 3546968, 3547139, 3547208, 3547361, 3547476, 3547626 };
            double[] xx3 = { 667192, 667291, 667663, 667881, 668101, 668244, 669079 };
            double[] yy3 = { 3547626, 3547600,                              3547590, 3547618, 3547609, 3547594, 3547436 };
            double[] xx4 = { 669079, 668983 };
            double[] yy4 = { 3547436, 3546849 };

            List<List<double>> vx = new List<List<double>>();
            List<List<double>> vy = new List<List<double>>();

            vx.Add(new List<double>(xx1));
            vx.Add(new List<double>(xx2));
            vx.Add(new List<double>(xx3));
            vx.Add(new List<double>(xx4));
            vy.Add(new List<double>(yy1));
            vy.Add(new List<double>(yy2));
            vy.Add(new List<double>(yy3));
            vy.Add(new List<double>(yy4));

            // 起始序号
            int id = 0;
            int roadid = 0;

            // 虚拟路测路径生成
            InfLocate.DTPathGen(ref vx, ref vy, ref id, ref roadid);

            // 虚拟路测场强填写
            InfLocate.DTStrength();

            MessageBox.Show("完成！");
        }

        private void 小区覆盖ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LTE.InternalInterference.Debug.FrmCellRayTracingDebug frm = new LTE.InternalInterference.Debug.FrmCellRayTracingDebug();
            frm.Show();
        }

        private void 射线记录ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            LTE.InternalInterference.Debug.RayRecordDebug frm = new LTE.InternalInterference.Debug.RayRecordDebug();
            frm.Show();
        }

        private void 实际路测导入ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataImport di = new DataImport();
            di.Show();
        }

        private void demoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void 多条线ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<ESRI.ArcGIS.Geometry.IPoint> linePoints = new List<ESRI.ArcGIS.Geometry.IPoint>();

            ESRI.ArcGIS.Geometry.IPoint p = GeometryUtilities.ConstructPoint3D(668656, 3546108, 0);
            ESRI.ArcGIS.Geometry.IPoint p1 = GeometryUtilities.ConstructPoint3D(669079, 3545579, 0);
            ESRI.ArcGIS.Geometry.IPoint p2 = GeometryUtilities.ConstructPoint3D(669069, 3545979, 0);

            linePoints.Add(p);
            linePoints.Add(p1);
            linePoints.Add(p2);

            ESRI.ArcGIS.Carto.IGraphicsLayer pGraphicsLayer = (GISMapApplication.Instance.Scene as ESRI.ArcGIS.Carto.IBasicMap).BasicGraphicsLayer;
            DrawUtilities.DrawLine(pGraphicsLayer as ESRI.ArcGIS.Analyst3D.IGraphicsContainer3D, linePoints);

            MessageBox.Show("完成");
        }

        private void 一条线ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ESRI.ArcGIS.Geometry.IPoint p1 = GeometryUtilities.ConstructPoint3D(668656, 3546108, 0);
            ESRI.ArcGIS.Geometry.IPoint p2 = GeometryUtilities.ConstructPoint3D(669079, 3545579, 0);
            DrawUtilities.DrawLine(p1, p2, 255, 0, 0);
            MessageBox.Show("完成");
        }

        private void 路测数据预处理ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            getATUData gA = new getATUData();

            Dictionary<string, AtuInfo> AtuDic = new Dictionary<string, AtuInfo>();
            gA.getPreAtu(ref AtuDic);
            gA.ATUData2SQL(ref AtuDic);
            MessageBox.Show("ATUData已写入！");
        }

        // 该过程需几分钟~十几分钟
        private void 系数校正ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            CalibrationForm cf = new CalibrationForm();
            cf.Show();
        }

        // 对均匀栅格进行粗略的场景划分，后续应该通过聚类的方式对场景进行自动划分
        private void 场景划分ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            #region 之前的场景划分
            //DataTable tb = LTE.DB.IbatisHelper.ExecuteQueryForDataTable("getRays", null);

            //for (int i = 0; i < tb.Rows.Count; i++)
            //{
            //    // 三个场景：y < 3545765，3545765 < y < 3547435，y > 3547435
            //    double y = Convert.ToDouble(tb.Rows[i]["rayStartPointY"]);
            //    if (y < 3545765)
            //        tb.Rows[i]["startPointScen"] = 0;
            //    else if (y > 3545765 && y < 3547435)
            //        tb.Rows[i]["startPointScen"] = 1;
            //    else
            //        tb.Rows[i]["startPointScen"] = 2;

            //    double y1 = Convert.ToDouble(tb.Rows[i]["rayEndPointY"]);
            //    if (y1 < 3545765)
            //        tb.Rows[i]["endPointScen"] = 0;
            //    else if (y1 > 3545765 && y1 < 3547435)
            //        tb.Rows[i]["endPointScen"] = 1;
            //    else
            //        tb.Rows[i]["endPointScen"] = 2;

            //    // 计算穿过场景的比例
            //    double scene1 = 3545765, sceneLen2 = 3547435 - 3545765, scene3 = 3547435;
            //    double yMin = 0, yMax = 0;
            //    if (y < y1)
            //    {
            //        yMin = y;
            //        yMax = y1;
            //    }
            //    else
            //    {
            //        yMin = y1;
            //        yMax = y;
            //    }
            //    double yLen = yMax - yMin;
            //    double proportion1 = 0, proportion2 = 0, proportion3 = 0;
            //    if (yMin < scene1 && yMax > scene3)  // 覆盖3个场景
            //    {
            //        double yLen1 = scene1 - yMin;
            //        double yLen3 = yMax - scene3;
            //        proportion1 = yLen1 / yLen;
            //        proportion2 = sceneLen2 / yLen;
            //        proportion3 = yLen3 / yLen;
            //    }
            //    else if (yMin < scene1 && yMax > scene1 && yMax <= scene3) // 覆盖前2个场景
            //    {
            //        double yLen1 = scene1 - yMin;
            //        double yLen2 = yMax - scene1;
            //        proportion1 = yLen1 / yLen;
            //        proportion2 = yLen2 / yLen;
            //    }
            //    else if (yMin >= scene1 && yMin < scene3 && yMax > scene3) // 覆盖后2个场景
            //    {
            //        double xLen2 = scene3 - yMin;
            //        double xLen3 = yMax - scene3;
            //        proportion2 = xLen2 / yLen;
            //        proportion3 = xLen3 / yLen;
            //    }
            //    else if (yMin < scene1)  // 只覆盖第1个场景
            //    {
            //        proportion1 = 1;
            //    }
            //    else if (yMin >= scene1 && yMax <= scene3)
            //    {
            //        proportion2 = 1;
            //    }
            //    else if (yMax > scene3)
            //    {
            //        proportion3 = 1;
            //    }
            //    tb.Rows[i]["proportion"] = string.Format("{0:N3};{1:N3};{2:N3}", proportion1, proportion2, proportion3);
            //}

            //LTE.DB.IbatisHelper.ExecuteDelete("DeleteRays", null);

            //using (System.Data.SqlClient.SqlBulkCopy bcp = new System.Data.SqlClient.SqlBulkCopy(LTE.DB.DataUtil.ConnectionString))
            //{
            //    bcp.BatchSize = tb.Rows.Count;
            //    bcp.BulkCopyTimeout = 1000;
            //    bcp.DestinationTableName = "tbRayAdj";
            //    bcp.WriteToServer(tb);
            //    bcp.Close();

            //}
            //tb.Clear();
            #endregion

            int maxGxid = 0, maxGyid = 0;
            InternalInterference.Grid.GridHelper.getInstance().getMaxAccGridXY(ref maxGxid, ref maxGyid);

            DataTable tb = new DataTable();
            tb = DB.IbatisHelper.ExecuteQueryForDataTable("getAGridZ", null);
            int minGzid = Convert.ToInt32(tb.Rows[0][0]);
            int maxGzid = Convert.ToInt32(tb.Rows[0][1]);

            DB.IbatisHelper.ExecuteDelete("DeleteAccrelateGridScene", null);

            System.Data.DataTable dtable = new System.Data.DataTable();
            dtable.Columns.Add("GXID");
            dtable.Columns.Add("GYID");
            dtable.Columns.Add("GZID");
            dtable.Columns.Add("Scene");

            double oX = 0, oY = 0;
            InternalInterference.Grid.GridHelper.getInstance().getOriginXY(ref oX, ref oY);
            double len = InternalInterference.Grid.GridHelper.getInstance().getAGridSize();

            // 三个场景：y < 3545765，3545765 < y < 3547435，y > 3547435
            int scen1 = (int)((3545765 - oY) / len);
            int scen2 = (int)((3547435 - oY) / len);

            for (int x = 0; x <= maxGxid; x++)
            {
                for (int y = 0; y <= maxGyid; y++)
                {
                    for (int z = minGzid; z <= maxGzid; z++)
                    {
                        System.Data.DataRow thisrow = dtable.NewRow();
                        thisrow["GXID"] = x;
                        thisrow["GYID"] = y;
                        thisrow["GZID"] = z;

                        if (y < scen1)
                            thisrow["Scene"] = (byte)0;
                        else if (y < scen2)
                            thisrow["Scene"] = (byte)1;
                        else
                            thisrow["Scene"] = (byte)2;

                        dtable.Rows.Add(thisrow);
                    }
                }

                if (dtable.Rows.Count > 50000)
                {
                    using (System.Data.SqlClient.SqlBulkCopy bcp = new System.Data.SqlClient.SqlBulkCopy(DB.DataUtil.ConnectionString))
                    {
                        bcp.BatchSize = dtable.Rows.Count;
                        bcp.BulkCopyTimeout = 1000;
                        bcp.DestinationTableName = "tbAccelerateGridScene";
                        bcp.WriteToServer(dtable);
                        bcp.Close();
                    }
                    dtable.Clear();
                }
            }

            // 最后一批
            if (dtable.Rows.Count > 0)
            {
                using (System.Data.SqlClient.SqlBulkCopy bcp = new System.Data.SqlClient.SqlBulkCopy(DB.DataUtil.ConnectionString))
                {
                    bcp.BatchSize = dtable.Rows.Count;
                    bcp.BulkCopyTimeout = 1000;
                    bcp.DestinationTableName = "tbAccelerateGridScene";
                    bcp.WriteToServer(dtable);
                    bcp.Close();
                }
                dtable.Clear();
            }

            MessageBox.Show("完成");
        }

        private void 网外干扰源ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OperateInterferenceLocLayer operateInf = new OperateInterferenceLocLayer();
            operateInf.ClearLayer();
            operateInf.constuctGrid3Ds();
            MessageBox.Show("已呈现！");
        }

        private void 扇区ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LTE.Geometric.Point source = new LTE.Geometric.Point(666454, 3551046, 0);
            double fromAngle = 159;
            double toAngle = 185;
            double distance = 500;
            LTE.InternalInterference.InterferenceFeatureLayerAnalysis.drawSector(source, fromAngle, toAngle, distance);
        }

        private void 多个扇区ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LTE.Geometric.Point source = new LTE.Geometric.Point(666454, 3551046, 0);
            double fromAngle = 55;
            double toAngle = 185;
            int sectors = 5;  // 分 5 批计算
            double distance = 500;

            double sectorAngle = (toAngle - fromAngle + 360) % 360;
            double angle = sectorAngle / sectors;
            for (int r = 0; r < sectors; r++)
            {
                double from = (fromAngle + r * angle);
                double to = (fromAngle + (r + 1) * angle);
                InterferenceFeatureLayerAnalysis.drawSector(source, from, to, distance);
            }
        }

        // 2019.5.30 地形
        private void tINToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            // TIN 轮廓
            LTE.GIS.OperateTINLayer TINLayer = new LTE.GIS.OperateTINLayer(LayerNames.TIN);
            TINLayer.ClearLayer();
            TINLayer.constuctTIN();

            // TIN 面
            LTE.GIS.OperateTINLayer TINLayer1 = new LTE.GIS.OperateTINLayer(LayerNames.TIN1);
            TINLayer1.ClearLayer();
            TINLayer1.constuctTIN1();
            MessageBox.Show("TIN 图层刷新完毕", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // 2019.6.11 建筑物底边平滑结果
        private void 建筑物底边平滑ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LTE.GIS.OperateSmoothBuildingLayer buildLayer = new LTE.GIS.OperateSmoothBuildingLayer();
            buildLayer.ClearLayer();
            buildLayer.constuctBuildingVertex();
            MessageBox.Show("平滑后的建筑物底边图层刷新完毕", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void 建筑物ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LTE.GIS.OperateBuildingLayer buildLayer = new LTE.GIS.OperateBuildingLayer(LayerNames.Building);
            buildLayer.ClearLayer();
            buildLayer.constuctBuilding();

            LTE.GIS.OperateBuildingLayer buildLayer1 = new LTE.GIS.OperateBuildingLayer(LayerNames.Building1);
            buildLayer1.ClearLayer();
            buildLayer1.constuctBuilding1();
            MessageBox.Show("建筑物图层刷新完毕", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
