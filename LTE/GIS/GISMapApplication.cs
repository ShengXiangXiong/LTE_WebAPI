using System;
using System.Collections.Generic;
using System.Text;

using System.Windows.Forms;

using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Analyst3D;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;

namespace LTE.GIS
{
    /// <summary>
    /// GIS的入口操作(采用单例模式) 
    /// </summary>
    public class GISMapApplication
    {
        #region 私有字段
        private static GISMapApplication instance = null; //当前对象的实例
        private AxSceneControl m_axSceneControl = null;

        public AxSceneControl AxSceneControl
        {
            get { return m_axSceneControl; }
            set { m_axSceneControl = value; }
        }
        private ISceneControl m_sceneControl = null;
        private ISceneGraph m_SceneGraph = null;
        private static System.Object m_syncObject = new System.Object();   // 同步对象
        #endregion 私有字段

        #region 属性

        /// <summary>
        /// 当前实例对象(单例模式)
        /// </summary>
        public static GISMapApplication Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (m_syncObject)
                    {
                        if (instance == null)
                        {
                            instance = new GISMapApplication();
                        }
                    }

                }
                return instance;
            }
        }

        /// <summary>
        /// 地图控件的引用
        /// </summary>
        public ISceneControl SceneControl
        {
            get { return m_sceneControl; }
            set { m_sceneControl = value; }
        }
        /// <summary>
        /// 图形接口
        /// </summary>
        public ISceneGraph SceneGraph
        {
            get { return m_SceneGraph; }
        }
        private IScene m_Scene = null;

        /// <summary>
        /// 场景
        /// </summary>
        public IScene Scene
        {
            get { return m_Scene; }
            set { m_Scene = value; }
        }

        private ISceneViewer m_SceneViewer = null;

        /// <summary>
        /// 活动视图
        /// </summary>
        public ISceneViewer SceneViewer
        {
            get { return m_SceneViewer; }
            set { m_SceneViewer = value; }
        }

        private ICamera m_Camera = null;

        /// <summary>
        /// 快照
        /// </summary>
        public ICamera Camera
        {
            get { return m_Camera; }
            set { m_Camera = value; }
        }



        #endregion 属性

        #region 公共方法

        /// <summary>
        /// 初始化的一些方法
        /// </summary>
        public void Init(AxSceneControl axSceneControl)
        {
            if (axSceneControl != null)
            {
                try
                {
                    this.m_axSceneControl = axSceneControl;
                    m_sceneControl = axSceneControl.Object as ISceneControl;
                    m_SceneGraph = m_sceneControl.SceneGraph;
                    m_Scene = m_sceneControl.Scene;
                    m_SceneViewer = m_SceneGraph.ActiveViewer;
                    m_Camera = m_sceneControl.Camera;
                }
                catch (Exception e)
                {

                }
            }
        }

        /// <summary>
        /// 加载三维地图的地图空间
        /// </summary>
        /// <param name="sceneControl"></param>
        public void LoadUserMapWorkSpace()
        {
            if (SceneControl != null)
            {

                MapWorkSpace workSpace = new MapWorkSpace();
                workSpace.LoadDefaultWorkSpace(SceneControl);
            }

        }


        /// <summary>
        /// 打开工作空间
        /// </summary>
        public void OpenWorkSpace()
        {
            if (this.m_sceneControl != null)
            {
                ICommand command = new ControlsSceneOpenDocCommand();
                command.OnCreate(SceneControl);
                SceneControl.CurrentTool = command as ESRI.ArcGIS.SystemUI.ITool;
                command.OnClick();
            }
        }


        /// <summary>
        /// 加载图层控件
        /// </summary>
        /// <returns></returns>
        public AxTOCControl LoadaxTOCControl()
        {
            if (this.SceneControl != null)
            {
                AxTOCControl control = new AxTOCControl();
                control.SetBuddyControl(m_sceneControl);
                return control;
            }

            return null;

        }

        /// <summary>
        /// 添加图层
        /// </summary>
        /// <param name="layer"></param>
        public void AddLayer(ILayer layer)
        {
            if (layer != null)
                this.m_Scene.AddLayer(layer, false);
        }
        /// <summary>
        /// 获取指定名称的图层
        /// </summary>
        /// <param name="layerName"></param>
        /// <returns></returns>
        public ILayer GetLayer(string layerName)
        {
            ILayer layer = null;
            if (this.m_sceneControl != null)
            {
                int n = this.m_sceneControl.Scene.LayerCount;
                for (int i = 0; i < this.m_sceneControl.Scene.LayerCount; i++)
                {
                    layer = this.SceneControl.Scene.get_Layer(i);
                    if (layer is IGroupLayer)
                    {
                        ICompositeLayer pGroupLayer = layer as ICompositeLayer;

                        IFeatureLayer pFeatLayer = null;
                        for (int j = 0; j < pGroupLayer.Count; j++)
                        {
                            if (pGroupLayer.get_Layer(j) is IFeatureLayer)
                            {
                                pFeatLayer = pGroupLayer.get_Layer(j) as IFeatureLayer;

                                if (pFeatLayer.Name.ToUpper() == layerName.ToUpper())
                                {
                                    return pFeatLayer;
                                }
                            }
                        }
                    }
                    else if (layer.Name.ToUpper() == layerName.ToUpper())
                    {
                        return layer;
                    }

                }
                if (layerName == LayerNames.Rays)
                {
                    IGraphicsContainer3D raysGraphicsContainer3D = GraphicsLayer3DUtilities.ConstructGraphicsLayer3D(LayerNames.Rays);
                    AddLayer(raysGraphicsContainer3D as ILayer);
                    return raysGraphicsContainer3D as ILayer;
                }
            }
            return null;

        }
        /// <summary>
        /// 以指定矩形缩放地图
        /// </summary>
        /// <param name="envelope"></param>
        public void FullExtent(IEnvelope envelope)
        {
            //Position the camera to see the full extent of the scene graph
            //pCamera.SetDefaultsMBB(pSceneGraph.Extent);
            this.m_Camera.SetDefaultsMBB(envelope);

            //Redraw the scene viewer
            this.m_SceneViewer.Redraw(true);
        }
        /// <summary>
        /// 刷新整个视图
        /// </summary>
        public void RefreshViewers()
        {
            if (this.m_sceneControl != null)
            {
                m_sceneControl.SceneGraph.RefreshViewers();
            }
        }

        /// <summary>
        /// 刷新指定图层
        /// </summary>
        /// <param name="pObject"></param>
        public void RefreshLayer(object pObject)
        {
            if (this.m_sceneControl == null)
                return;

            ISceneGraph pSceneGragh = m_sceneControl.SceneGraph;
            pSceneGragh.Invalidate(pObject, true, false);
            pSceneGragh.ActiveViewer.Redraw(true);
            pSceneGragh.RefreshViewers();
        }
        /// <summary>
        /// 以指定几何体选择图元
        /// </summary>
        /// <param name="pGeometry"></param>
        /// <param name="ShiftKey"></param>
        /// <param name="justOne"></param>
        public void SelectByShape(IGeometry pGeometry, bool ShiftKey, bool justOne)
        {
            if (this.m_Scene == null)
                return;
            //Get a selection environment
            ISelectionEnvironment pSelectionEnv;
            pSelectionEnv = new SelectionEnvironmentClass();

            if (ShiftKey)
                pSelectionEnv.CombinationMethod = ESRI.ArcGIS.Carto.esriSelectionResultEnum.esriSelectionResultAdd;
            else
                pSelectionEnv.CombinationMethod = ESRI.ArcGIS.Carto.esriSelectionResultEnum.esriSelectionResultNew;

            //Select by Shape
            this.m_Scene.SelectByShape(pGeometry, pSelectionEnv, justOne);

            //Refresh the scene viewer
            RefreshViewers();
        }
        /// <summary>
        /// 鼠标的滚轮放大/缩小操作
        /// </summary>
        public void SceneControlMouseWheel(MouseEventArgs e)
        {
            if (this.m_sceneControl == null)
                return;

            double scale = -0.2;
            if (e.Delta < 0) scale = 0.2;
            ICamera pCamera = SceneControl.Camera;
            IPoint pPtObs = pCamera.Observer;
            IPoint pPtTar = pCamera.Target;
            pPtObs.X += (pPtObs.X - pPtTar.X) * scale;
            pPtObs.Y += (pPtObs.Y - pPtTar.Y) * scale;
            pPtObs.Z += (pPtObs.Z - pPtTar.Z) * scale;
            pCamera.Observer = pPtObs;
            RefreshViewers();

        }

        /// <summary>
        /// 清除所选的图形
        /// </summary>
        public void ClearSelection()
        {
            if (this.m_sceneControl == null)
                return;

            m_sceneControl.Scene.ClearSelection();
        }



        /// <summary>
        /// 激活地图窗口。
        /// </summary>
        public void ActivateMapForm()
        {
            Form mapForm = this.m_axSceneControl.FindForm();
            if (mapForm != null)
            {
                mapForm.Activate();
            }
        }

        #endregion 公共方法

    }
}
