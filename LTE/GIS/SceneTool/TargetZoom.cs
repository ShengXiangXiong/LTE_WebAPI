// Copyright 2008 ESRI
// 
// All rights reserved under the copyright laws of the United States
// and applicable international laws, treaties, and conventions.
// 
// You may freely redistribute and use this sample code, with or
// without modification, provided you include the original copyright
// notice and use restrictions.
// 
// See use restrictions at <your ArcGIS install location>/developerkit/userestrictions.txt.
// 

using System;
using ESRI.ArcGIS.Analyst3D;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.ADF.CATIDs;
using System.Runtime.InteropServices;

namespace LTE.GIS
{
	[ClassInterface(ClassInterfaceType.None)]
	[Guid("6104E022-3859-4d89-9BEF-984375BA41F9")]

	public sealed class TargetZoom : BaseTool
	{

        #region COM Registration Function(s)
        [ComRegisterFunction()]
        [ComVisible(false)]
        static void RegisterFunction(Type registerType)
        {
            // Required for ArcGIS Component Category Registrar support
            ArcGISCategoryRegistration(registerType);

            //
            // TODO: Add any COM registration code here
            //
        }

        [ComUnregisterFunction()]
        [ComVisible(false)]
        static void UnregisterFunction(Type registerType)
        {
            // Required for ArcGIS Component Category Registrar support
            ArcGISCategoryUnregistration(registerType);

            //
            // TODO: Add any COM unregistration code here
            //
        }

        #region ArcGIS Component Category Registrar generated code
        /// <summary>
        /// Required method for ArcGIS Component Category registration -
        /// Do not modify the contents of this method with the code editor.
        /// </summary>
        private static void ArcGISCategoryRegistration(Type registerType)
        {
            string regKey = string.Format("HKEY_CLASSES_ROOT\\CLSID\\{{{0}}}", registerType.GUID);
            ControlsCommands.Register(regKey);

        }
        /// <summary>
        /// Required method for ArcGIS Component Category unregistration -
        /// Do not modify the contents of this method with the code editor.
        /// </summary>
        private static void ArcGISCategoryUnregistration(Type registerType)
        {
            string regKey = string.Format("HKEY_CLASSES_ROOT\\CLSID\\{{{0}}}", registerType.GUID);
            ControlsCommands.Unregister(regKey);

        }

        #endregion
        #endregion

		private System.Windows.Forms.Cursor m_pCursor;
		private ISceneHookHelper m_pSceneHookHelper;

		public TargetZoom()
		{
			base.m_category = "Sample_SceneControl(C#)";
			base.m_caption = "Target Zoom";
			base.m_toolTip = "Zoom to Target";
			base.m_name = "Sample_SceneControl(C#)/TargetZoom";
			base.m_message = "Zoom to selected target";

			//Load resources
			string[] res = GetType().Assembly.GetManifestResourceNames();
			if(res.GetLength(0) > 0)
			{
				base.m_bitmap = new System.Drawing.Bitmap(GetType().Assembly.GetManifestResourceStream("LTE.GIS.SceneTool.TargetZoom.bmp"));
			}
			m_pCursor = new System.Windows.Forms.Cursor(GetType().Assembly.GetManifestResourceStream("LTE.GIS.SceneTool.targetzoom.cur"));
		
			m_pSceneHookHelper = new SceneHookHelperClass ();
		}

		~TargetZoom()
		{
			m_pSceneHookHelper = null;
			m_pCursor = null;
		}
	
		public override bool Enabled
		{
			get
			{
				if(m_pSceneHookHelper.Scene == null)
					return false;
				else
					return true;
			}
		}
	
		public override void OnCreate(object hook)
		{
			m_pSceneHookHelper.Hook = hook;
		}
	
		public override int Cursor
		{
			get
			{
				return m_pCursor.Handle.ToInt32();
			}
		}
	
		public override bool Deactivate()
		{
			return true;
		}
	
		public override void OnMouseUp(int Button, int Shift, int X, int Y)
		{
			//Get the scene graph
			ISceneGraph pSceneGraph = (ISceneGraph) m_pSceneHookHelper.SceneGraph;

			IPoint pNewTgt;
			object pOwner, pObject;

			//Translate screen coordinates into a 3D point
			pSceneGraph.Locate(pSceneGraph.ActiveViewer, X, Y, esriScenePickMode.esriScenePickAll, true,
				out pNewTgt, out pOwner, out pObject);

			if(pNewTgt == null) return;

			//Get the scene viewer's camera
			ICamera pCamera = (ICamera) m_pSceneHookHelper.Camera;

			//If orthographic (2D) type
			if(pCamera.ProjectionType == esri3DProjectionType.esriOrthoProjection )
			{
				//Set the camera's new target and zoom in
				pCamera.Target = pNewTgt;
				pCamera.Zoom(0.25);

				//Redraw the scene viewer
				pSceneGraph.ActiveViewer.Redraw(true);
			}
			else
			{
				//Get the camera's old target and observer
				IPoint pOldTgt = (IPoint) pCamera.Target;
				IPoint pOldObs = (IPoint) pCamera.Observer;

				//Set the camera's new target and get the new observer
				pCamera.Target = pNewTgt;
				pCamera.PolarUpdate(0.1, 0, 0, true);
 
				IPoint pNewObs = (IPoint) pCamera.Observer;

				//Get the duration in seconds of last redraw
				//and the average number of frames per second
				double dlastFrameDuration, dMeanFrameRate;
				pSceneGraph.GetDrawingTimeInfo(out dlastFrameDuration, out dMeanFrameRate);

				if(dlastFrameDuration < 0.01)
					dlastFrameDuration = 0.01;

				int iSteps;
				iSteps = (int) (2 / dlastFrameDuration);
				if(iSteps < 1)
					iSteps = 1;

				if(iSteps > 60)
					iSteps = 60;

				double dxObs, dyObs, dzObs;
				double dxTgt, dyTgt, dzTgt;

				dxObs = (pNewObs.X - pOldObs.X) / iSteps;
				dyObs = (pNewObs.Y - pOldObs.Y) / iSteps;
				dzObs = (pNewObs.Z - pOldObs.Z) / iSteps;

				dxTgt = (pNewTgt.X - pOldTgt.X) / iSteps;
				dyTgt = (pNewTgt.Y - pOldTgt.Y) / iSteps;
				dzTgt = (pNewTgt.Z - pOldTgt.Z) / iSteps;

				//Loop through each step moving the camera's observer and target from the
				//old positions to the new positions, refreshing the scene viewer each time
				for(int i=0; i < iSteps; i++)
				{
					pNewObs.X = pOldObs.X + (i * dxObs);
					pNewObs.Y = pOldObs.Y + (i * dyObs);
					pNewObs.Z = pOldObs.Z + (i * dzObs);

					pNewTgt.X = pOldTgt.X + (i * dxTgt);
					pNewTgt.Y = pOldTgt.Y + (i * dyTgt);
					pNewTgt.Z = pOldTgt.Z + (i * dzTgt);

					pCamera.Observer = pNewObs;
					pCamera.Target = pNewTgt;
					pSceneGraph.ActiveViewer.Redraw(true);
				}
			}
		}
	}
}
