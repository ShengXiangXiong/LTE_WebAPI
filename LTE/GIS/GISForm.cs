using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Collections;

using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Analyst3D;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;

using LTE.InternalInterference;
using LTE.InternalInterference.Grid;

namespace LTE.GIS
{
    public partial class GISForm : Form
    {
        #region 私有变量

        private ESRI.ArcGIS.Controls.ISceneControl m_sceneControl = null;
        private ISceneGraph pSceneGraph = null;
        private ISceneViewer pViewer = null;
        private ICommand command = null;
        private bool isActive = false;//当前窗体是否激活
        private string statusMsg = "";//状态栏信息
        private bool isMouseDown = false;//是否鼠标按下
        private ContextMenuStrip contextMenu = null;

        #endregion 私有变量

        public MainForm mainForm;

        public GISForm(MainForm mForm)
        {
            ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.EngineOrDesktop); 
            InitializeComponent();
            axTOCControl1.SetBuddyControl(axSceneControl1);
            this.mainForm = mForm;
            InitInfo();
            this.contextMenu = this.getMapContextMenuStrip();
        }

        private ContextMenuStrip getMapContextMenuStrip()
        {
            ContextMenuStrip contextMenu = new ContextMenuStrip();

            ToolStripMenuItem query0 = new ToolStripMenuItem("单射线追踪");

            ContextMenuStrip contextMenu0 = new ContextMenuStrip();
            ToolStripMenuItem query00 = new ToolStripMenuItem("指定射线方向");
            query00.Click += new EventHandler(SingleRayTracing_Direction);
            ToolStripMenuItem query01 = new ToolStripMenuItem("指定地面交点");
            query01.Click += new EventHandler(SingleRayTracing_CrossPoint);
            contextMenu0.Items.Add(query01);
            contextMenu0.Items.Add(query00);
            query0.DropDown = contextMenu0;

            ToolStripMenuItem query1 = new ToolStripMenuItem("小区覆盖计算");
            query1.Click += new EventHandler(this.RayTracing);
            ToolStripMenuItem query2 = new ToolStripMenuItem("小区覆盖呈现");
            query2.Click += new EventHandler(this.DisplayRayTracing);
            ToolStripMenuItem query3 = new ToolStripMenuItem("小区立体覆盖呈现");
            query3.Click += new EventHandler(this.Display3DRayTracing);
            ToolStripMenuItem query4 = new ToolStripMenuItem("区域覆盖呈现");
            query4.Click += new EventHandler(ShowAreaCover);
            ToolStripMenuItem query7 = new ToolStripMenuItem("区域覆盖缺陷分析");
            query7.Click += new EventHandler(ShowAreaCoverDefect);
            ToolStripMenuItem query5 = new ToolStripMenuItem("射线记录");
            query5.Click += new EventHandler(this.RayRecord);

            ToolStripMenuItem query6 = new ToolStripMenuItem("调试");
            ContextMenuStrip contextMenu6 = new ContextMenuStrip();
            ToolStripMenuItem query60 = new ToolStripMenuItem("小区覆盖计算");
            query60.Click += new EventHandler(this.RayTracingDebug);
            ToolStripMenuItem query61 = new ToolStripMenuItem("射线生成并记录");
            query61.Click += new EventHandler(this.RayRecordDebug);
            contextMenu6.Items.Add(query60);
            contextMenu6.Items.Add(query61);
            query6.DropDown = contextMenu6;

            contextMenu.Items.Add(query0);
            contextMenu.Items.Add(query1);
            contextMenu.Items.Add(query2);
            contextMenu.Items.Add(query3);
            contextMenu.Items.Add(query4);
            contextMenu.Items.Add(query7);
            contextMenu.Items.Add(query5);
            contextMenu.Items.Add(query6);

            return contextMenu;
        }

        private void SingleRayTracing_Direction(object sender, EventArgs e)
        {
            CellInfo cellInfo = AnalysisEntry.getCellInfo();
            if (cellInfo != null)
            {
                FrmSingleRayTracing2 frm = new FrmSingleRayTracing2(cellInfo.SourceName);
                frm.Show();
            }
            else
            {
                FrmSingleRayTracing2 frm = new FrmSingleRayTracing2();
                frm.Show();
            }
        }

        private void SingleRayTracing_CrossPoint(object sender, EventArgs e)
        {
            CellInfo cellInfo = AnalysisEntry.getCellInfo();
            if (cellInfo != null)
            {
                FrmSingleRayTracing1 frm = new FrmSingleRayTracing1(cellInfo.SourceName);
                frm.Show();
            }
            else
            {
                FrmSingleRayTracing1 frm = new FrmSingleRayTracing1();
                frm.Show();
            }
        }

        private void RayTracing(object sender, EventArgs e)
        {
            CellInfo cellInfo = AnalysisEntry.getCellInfo();
            if (cellInfo == null)
            {
                FrmCellRayTracing frm = new FrmCellRayTracing();
                frm.Show();
            }
            else
            {
                AnalysisEntry entry = new AnalysisEntry();
                entry.ExcuteAnalysis(cellInfo);
            }
        }

        private void RayTracingDebug(object sender, EventArgs e)
        {
            CellInfo cellInfo = AnalysisEntry.getCellInfo();
            if (cellInfo == null)
            {
                LTE.InternalInterference.Debug.FrmCellRayTracingDebug frm = new LTE.InternalInterference.Debug.FrmCellRayTracingDebug();
                frm.Show();
            }
            else
            {
                LTE.InternalInterference.Debug.FrmCellRayTracingDebug frm = new LTE.InternalInterference.Debug.FrmCellRayTracingDebug(cellInfo.SourceName, cellInfo.eNodeB, cellInfo.CI);
                frm.Show();
            }
        }

        private void RayRecordDebug(object sender, EventArgs e)
        {
            CellInfo cellInfo = AnalysisEntry.getCellInfo();
            if (cellInfo == null)
            {
                LTE.InternalInterference.Debug.RayRecordDebug frm = new LTE.InternalInterference.Debug.RayRecordDebug();
                frm.Show();
            }
            else
            {
                LTE.InternalInterference.Debug.RayRecordDebug frm = new LTE.InternalInterference.Debug.RayRecordDebug(cellInfo.SourceName, cellInfo.eNodeB, cellInfo.CI);
                frm.Show();
            }
        }

        private void RayRecord(object sender, EventArgs e)
        {
            CellInfo cellInfo = AnalysisEntry.getCellInfo();
            if (cellInfo == null)
            {
                LTE.InternalInterference.RayRecord frm = new LTE.InternalInterference.RayRecord();
                frm.Show();
            }
            else
            {
                LTE.InternalInterference.RayRecord frm = new LTE.InternalInterference.RayRecord(cellInfo.SourceName, cellInfo.eNodeB, cellInfo.CI);
                frm.Show();
            }
        }

        private void DisplayRayTracing(object sender, EventArgs e)
        {
            CellInfo cellInfo = AnalysisEntry.getCellInfo();
            if (cellInfo == null)
            {
                DisplayCellCover displayCell = new DisplayCellCover();
                displayCell.Show();
            }
            else
            {
                AnalysisEntry.DisplayAnalysis(cellInfo);
                //GISLocate.Instance.LocateToPoint(cellInfo.SourcePoint);
                MessageBox.Show("已呈现！");
            }
        }

        private void Display3DRayTracing(object sender, EventArgs e)
        {
            CellInfo cellInfo = AnalysisEntry.getCellInfo();
            if (cellInfo == null)
            {
                DisplayCellCover displayCell = new DisplayCellCover();
                displayCell.Show();
            }
            else
            {
                AnalysisEntry.Display3DAnalysis(cellInfo);
                //GISLocate.Instance.LocateToPoint(cellInfo.SourcePoint);
                MessageBox.Show("已呈现！");
            }
        }

        private void ShowAreaCover(object sender, EventArgs e)
        {
            DisplayAreaCover displayarea = new DisplayAreaCover();
            displayarea.Show();
        }

        private void ShowAreaCoverDefect(object sender, EventArgs e)
        {
            AreaCoverDefect displayarea = new AreaCoverDefect();
            displayarea.Show();
        }

        /// <summary>
        /// 初始化信息
        /// </summary>
        private void InitInfo()
        {
            pSceneGraph = axSceneControl1.SceneGraph;
            pViewer = axSceneControl1.SceneViewer;
            //this.Icon = LTE.Enos.EnosApplication.GetIcon(23);
            this.FormClosing += new FormClosingEventHandler(GISForm_FormClosing);
            this.MouseWheel += new MouseEventHandler(GISForm_MouseWheel);
            // 取得 SceneControl 的引用 
            m_sceneControl = (ISceneControl)this.axSceneControl1.Object;
            InitMap(this.axSceneControl1);

        }

        /// <summary>
        /// 鼠标的滚轮操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GISForm_MouseWheel(object sender, MouseEventArgs e)
        {
            System.Drawing.Point pSceLoc = this.PointToScreen(this.axSceneControl1.Location);
            System.Drawing.Point Pt = this.PointToScreen(e.Location);
            if (Pt.X < pSceLoc.X || Pt.X > pSceLoc.X + this.axSceneControl1.Width || Pt.Y < pSceLoc.Y || Pt.Y > pSceLoc.Y + this.axSceneControl1.Height)
            {
                return;
            }
            else
            {
                GISMapApplication.Instance.SceneControlMouseWheel(e);
            }
        }

        /// <summary>
        /// 窗体关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GISForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 用户不可以关闭地图窗口
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// 初始化地图的一些信息
        /// </summary>
        /// <param name="m_axSceneControl">地图控件的引用</param>
        private void InitMap(AxSceneControl m_axSceneControl)
        {
            if (m_axSceneControl == null)
                return;

            GISMapApplication gisMapApp = GISMapApplication.Instance;
            gisMapApp.Init(m_axSceneControl);
            gisMapApp.LoadUserMapWorkSpace();

        }

        /// <summary>
        /// 导航
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Navigation_Click(object sender, EventArgs e)
        {
            ICommand command = new ControlsSceneNavigateToolClass();
            command.OnCreate(m_sceneControl);
            this.axSceneControl1.CurrentTool = command as ESRI.ArcGIS.SystemUI.ITool;

        }

        ///// <summary>
        ///// 飞行
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        private void fly_Click(object sender, EventArgs e)
        {
            command = new ControlsSceneFlyTool();
            command.OnCreate(m_sceneControl);
            this.axSceneControl1.CurrentTool = command as ESRI.ArcGIS.SystemUI.ITool;

        }

        /// <summary>
        /// 放大/缩小
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ZoomInOut_Click(object sender, EventArgs e)
        {
            command = new ControlsSceneZoomInOutTool();
            command.OnCreate(m_sceneControl);
            this.axSceneControl1.CurrentTool = command as ESRI.ArcGIS.SystemUI.ITool;

        }

        /// <summary>
        /// 以某点居中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TargetCenter_Click(object sender, EventArgs e)
        {
            command = new ControlsSceneTargetCenterTool();
            command.OnCreate(m_sceneControl);
            this.axSceneControl1.CurrentTool = command as ESRI.ArcGIS.SystemUI.ITool;
        }

        /// <summary>
        /// 放大
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ZoomIn_Click(object sender, EventArgs e)
        {
            command = new ControlsSceneZoomInTool();
            command.OnCreate(m_sceneControl);
            this.axSceneControl1.CurrentTool = command as ESRI.ArcGIS.SystemUI.ITool;
        }

        /// <summary>
        /// 缩小
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void zoomout_Click(object sender, EventArgs e)
        {
            command = new ControlsSceneZoomOutTool();
            command.OnCreate(m_sceneControl);
            this.axSceneControl1.CurrentTool = command as ESRI.ArcGIS.SystemUI.ITool;
        }

        /// <summary>
        /// 缩小一倍
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void narrow_Click(object sender, EventArgs e)
        {
            command = new ControlsSceneNarrowFOVCommand();
            command.OnCreate(m_sceneControl);
            this.axSceneControl1.CurrentTool = command as ESRI.ArcGIS.SystemUI.ITool;
            command.OnClick();

        }

        /// <summary>
        /// 放大一倍
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void expand_Click(object sender, EventArgs e)
        {
            command = new ControlsSceneExpandFOVCommand();
            command.OnCreate(m_sceneControl);
            this.axSceneControl1.CurrentTool = command as ESRI.ArcGIS.SystemUI.ITool;
            command.OnClick();
        }

        /// <summary>
        /// 移动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pan_Click(object sender, EventArgs e)
        {
            command = new ControlsScenePanTool();
            command.OnCreate(m_sceneControl);
            this.axSceneControl1.CurrentTool = command as ESRI.ArcGIS.SystemUI.ITool;
        }

        /// <summary>
        /// 全部显示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fullextent_Click(object sender, EventArgs e)
        {
            command = new ControlsSceneFullExtentCommand();
            command.OnCreate(m_sceneControl);
            this.axSceneControl1.CurrentTool = command as ESRI.ArcGIS.SystemUI.ITool;
            command.OnClick();
        }

        /// <summary>
        /// 选择图元
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectFeatures_Click(object sender, EventArgs e)
        {
            command = new LTE.GIS.SelectFeatures();//new ControlsSceneSelectFeaturesToolClass(); 
            command.OnCreate(m_sceneControl);
            this.axSceneControl1.CurrentTool = command as ESRI.ArcGIS.SystemUI.ITool;
        }

        /// <summary>
        /// 选择
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void select_Click(object sender, EventArgs e)
        {
            command = new LTE.GIS.SceneTool.SelectGraphics();//(new ControlsSceneSelectGraphicsTool();
            command.OnCreate(m_sceneControl);
            this.axSceneControl1.CurrentTool = command as ESRI.ArcGIS.SystemUI.ITool;
            command.OnClick();
        }

        /// <summary>
        /// 打开工作空间
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void open_Click(object sender, EventArgs e)
        {
            command = new ControlsSceneOpenDocCommand();
            command.OnCreate(m_sceneControl);
            this.axSceneControl1.CurrentTool = command as ESRI.ArcGIS.SystemUI.ITool;
            command.OnClick();
        }

        private void GISForm_Activated(object sender, EventArgs e)
        {
            this.isActive = true;
            SetStatus();

        }

        private void GISForm_Deactivate(object sender, EventArgs e)
        {
            this.isActive = false;
        }

        /// <summary>
        /// 设置窗体状态
        /// </summary>
        private void SetStatus()
        {
            //if (isActive)
            {
                this.mainForm.SetStatus(this.statusMsg);
            }
        }

        /// <summary>
        /// 鼠标移动事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void axSceneControl1_OnMouseMove(object sender, ISceneControlEvents_OnMouseMoveEvent e)
        {
            //if (isActive && isMouseDown)
            if (isMouseDown)
            {
                IPoint pPnt = new PointClass();
                object pOwner;
                object pObject;
                pSceneGraph.Locate(pViewer, e.x, e.y, esriScenePickMode.esriScenePickGeography, true, out pPnt, out pOwner, out pObject);
                if (pPnt == null)
                    statusMsg = "当前坐标";
                else
                {
                    double x = pPnt.X, y = pPnt.Y;
                    IPoint pGeoPoint = LTE.GIS.PointConvert.Instance.GetGeoPoint(pPnt);

                    int gxid = 0, gyid = 0;
                    GridHelper.getInstance().XYToGGrid(x, y, ref gxid, ref gyid);

                    statusMsg = "当前坐标: 经度=" + Math.Round(pGeoPoint.X, 5) + "  纬度=" + Math.Round(pGeoPoint.Y, 5) + "  高度=" + Math.Round(pGeoPoint.Z, 3) + "  x=" + Math.Round(x, 3) + "  y=" + Math.Round(y, 3) + "  gxid=" + gxid + "  gyid=" + gyid;
                }
                //MessageBox.Show(this.statusMsg);
                SetStatus();
            }
        }

        private void axSceneControl1_OnMouseDown(object sender, ISceneControlEvents_OnMouseDownEvent e)
        {
            isMouseDown = true;

            if (this.axSceneControl1.CurrentTool is SelectFeatures || this.axSceneControl1.CurrentTool is SelectGraphics)
            {
                IPoint pPoint;
                object pOwner, pObject;

                //地图点击操作
                axSceneControl1.SceneGraph.Locate(pViewer, e.x, e.y, esriScenePickMode.esriScenePickGeography, true, out pPoint, out pOwner, out pObject);
                if (pOwner is IFeatureLayer)
                {
                    IFeatureLayer pFeatureLayer = (IFeatureLayer)pOwner;
                    if (pObject is IFeature)
                    {
                        IFeature iFeature = (IFeature)pObject;
                        //给属性赋值
                        object cellName = FeatureIdentity.GetFeatureInfo(pFeatureLayer.Name, iFeature);

                        GISLocate.Instance.HandlerFeatureData(pFeatureLayer.Name, iFeature);

                        //FlashMethod.FlashGeometry(iFeature.Shape, pSceneGraph);
                    }

                }

            }

        }

        private void axSceneControl1_OnMouseUp(object sender, ISceneControlEvents_OnMouseUpEvent e)
        {
            if (e.button == 2)
            {
                //ContextMenuStrip contextMenu = this.getMapContextMenuStrip();
                // 显示右键快捷菜单
                this.contextMenu.Show(this.axSceneControl1, new System.Drawing.Point(e.x, e.y));
            }
        }

        private void axTOCControl1_OnMouseDown(object sender, ESRI.ArcGIS.Controls.ITOCControlEvents_OnMouseDownEvent e)
        {

        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void pnlTool_Paint(object sender, PaintEventArgs e)
        {

        }

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }


    }
}
