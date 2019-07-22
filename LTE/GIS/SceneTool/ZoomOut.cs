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
using System.Drawing;
using ESRI.ArcGIS.Analyst3D;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.ADF.CATIDs;
using System.Runtime.InteropServices;

namespace LTE.GIS
{
	[ClassInterface(ClassInterfaceType.None)]
	[Guid("3D3C8C68-7B65-4441-B614-792D5606FF8D")]

	public sealed class ZoomOut : BaseTool
	{
		[DllImport("user32")] public static extern int ReleaseCapture(int hwnd); 
		[DllImport("user32")] public static extern int SetCapture(int hwnd); 
		[DllImport("user32")] public static extern int GetCapture(int fuFlags);
		[DllImport("user32")] public static extern int GetWindowRect(int hwnd, ref Rectangle lpRect);

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
		private long m_lMouseX, m_lMouseY;
		private bool m_bInUse;
		private Pen m_pen; 
		private Brush m_brush;
		private Graphics myGraphics;

		public ZoomOut()
		{
			base.m_category = "Sample_SceneControl(C#)";
			base.m_caption = "Zoom Out";
			base.m_toolTip = "Zoom Out";
			base.m_name = "Sample_SceneControl(C#)/Zoom Out";
			base.m_message = "Zooms in out the scene";

			//Load resources
			string[] res = GetType().Assembly.GetManifestResourceNames();
			if(res.GetLength(0) > 0)
			{
				base.m_bitmap = new System.Drawing.Bitmap(GetType().Assembly.GetManifestResourceStream("LTE.GIS.SceneTool.zoomout.bmp"));
			}
			m_pCursor = new System.Windows.Forms.Cursor(GetType().Assembly.GetManifestResourceStream("LTE.GIS.SceneTool.ZOOMOUT.CUR"));
		
			m_pSceneHookHelper = new SceneHookHelperClass ();
		}

		~ZoomOut()
		{
			m_pSceneHookHelper = null;
			m_pCursor = null;
			m_pen.Dispose();
			m_brush.Dispose();
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
			myGraphics = Graphics.FromHdc((IntPtr)m_pSceneHookHelper.ActiveViewer.hDC);
			m_brush = new SolidBrush(Color.Transparent);  //hollow brush
			m_pen = new System.Drawing.Pen(Color.Black, 2); //A solid, width of 2 black pen
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

		public override void OnKeyDown(int keyCode, int Shift)
		{
			if(m_bInUse == true)
			{
				if(keyCode == 27) //If ESC was pressed 
				{
					//Redraw the scene viewer
					ISceneViewer pSceneViewer = (ISceneViewer) m_pSceneHookHelper.ActiveViewer;
					pSceneViewer.Redraw(true);

					ReleaseCapture(m_pSceneHookHelper.ActiveViewer.hWnd);

					m_bInUse = false;
				}
			}
		}

		public override void OnMouseDown(int Button, int Shift, int X, int Y)
		{
			//Initialize mouse coordinates
			m_bInUse = true;
			m_lMouseX = X;
			m_lMouseY = Y;

			IEnvelope pEnvelope;

			//Initialize envelope
			CreateEnvelope(X, Y, out pEnvelope);

			//Get the scene viewer
			ISceneViewer pSceneViewer = (ISceneViewer) m_pSceneHookHelper.ActiveViewer;

			SetCapture(m_pSceneHookHelper.ActiveViewer.hWnd);
		}

		public override void OnMouseMove(int Button, int Shift, int X, int Y)
		{
			if(!m_bInUse) return;

			IEnvelope pEnvelope;
			
			//Draw rectangle on the device
			CreateEnvelope(X, Y, out pEnvelope);
			DrawRectangle(pEnvelope);
		}

		public override void OnMouseUp(int Button, int Shift, int X, int Y)
		{
			if(!m_bInUse) return;

			if(GetCapture(m_pSceneHookHelper.ActiveViewer.hWnd) != 0)
				ReleaseCapture(m_pSceneHookHelper.ActiveViewer.hWnd);

			//Get the scene viewer's camera
			ICamera pCamera = (ICamera) m_pSceneHookHelper.Camera;

			//Get the scene graph
			ISceneGraph pSceneGraph = (ISceneGraph) m_pSceneHookHelper.SceneGraph;

			//Create envelope
			IEnvelope pEnvelope;
			CreateEnvelope(X, Y, out pEnvelope);

			IPoint pPoint;
			object pOwner, pObject;

			if(pEnvelope.Width == 0 || pEnvelope.Height == 0)
			{
				//Translate screen coordinates into a 3D point
				pSceneGraph.Locate(pSceneGraph.ActiveViewer, X, Y, esriScenePickMode.esriScenePickAll, true,
					out pPoint, out pOwner, out pObject);

				//Set camera target and zoom in
				pCamera.Target = pPoint;
				pCamera.Zoom(1.3333333333333);
			}
			else
			{
				//Get dimension of the scene viewer window
				Rectangle rect;
				rect = new Rectangle();

				if(GetWindowRect(m_pSceneHookHelper.ActiveViewer.hWnd, ref rect) == 0) return;
					
				//If perspective (3D) view
				if(pCamera.ProjectionType == esri3DProjectionType.esriPerspectiveProjection)
				{
					double dWidth, dHeight;

					dWidth = Math.Abs(rect.Right - rect.Left) * (Math.Abs(rect.Right - rect.Left) / pEnvelope.Width);
					dHeight = Math.Abs(rect.Top - rect.Bottom) * (Math.Abs(rect.Top - rect.Bottom) / pEnvelope.Height);

					pPoint = new PointClass();
					pPoint.PutCoords(pEnvelope.XMin + (pEnvelope.Width / 2), pEnvelope.YMin + (pEnvelope.Height / 2));

					//Redimension envelope based on scene viewer dimensions
					pEnvelope.XMin = pPoint.X - (dWidth / 2);
					pEnvelope.YMin = pPoint.Y - (dHeight / 2);
					pEnvelope.Width = dWidth;
					pEnvelope.Height = dHeight;

					//Zoom camera to the envelope
					pCamera.ZoomToRect(pEnvelope);
				}
				else
				{
					//Translate screen coordinates into a 3D point
					pSceneGraph.Locate(pSceneGraph.ActiveViewer, (int) (pEnvelope.XMin + (pEnvelope.Width / 2)), 
						(int) (pEnvelope.YMin + (pEnvelope.Height / 2)), esriScenePickMode.esriScenePickAll, true, 
						out pPoint, out pOwner, out pObject);

					//Set camera target
					pCamera.Target = pPoint;

					double dx, dy;
					dx = pEnvelope.Width;
					dy = pEnvelope.Height;

					//Determine zoom factor
					if(dx > 0 && dy >0)
					{
						dx = Math.Abs(rect.Right - rect.Left) / dx;
						dy = Math.Abs(rect.Top - rect.Bottom) / dy;
						
						if(dx<dy)
							pCamera.Zoom(dx);
						else
							pCamera.Zoom(dy);
					}
					else
						pCamera.Zoom(1.3333333333333);
				}
			}
			
			//Redraw the scene viewer
			ISceneViewer pSceneViewer = (ISceneViewer) m_pSceneHookHelper.ActiveViewer;
			pSceneViewer.Redraw(true);

			m_bInUse = false;
		}

		public void CreateEnvelope(int X, int Y, out IEnvelope pEnvelope)
		{
			//Create envelope based upon the initial
			//and current mouse coordinates
			pEnvelope = new EnvelopeClass();
			if((double)m_lMouseX <= (double)X)
			{
				pEnvelope.XMin = (double) m_lMouseX;
				pEnvelope.XMax = (double) X;
			}
			else
			{
				pEnvelope.XMin = (double) X;
				pEnvelope.XMax = (double) m_lMouseX;
			}

			if((double) m_lMouseY <= (double) Y)
			{
				pEnvelope.YMin = (double) m_lMouseY;
				pEnvelope.YMax = (double) Y;
			}
			else
			{
				pEnvelope.YMin = (double) Y;
				pEnvelope.YMax = (double) m_lMouseY;
			}
		}

		public void DrawRectangle(IEnvelope pEnvelope)
		{
			//Get the scene viewer
			ISceneViewer pSceneViewer = (ISceneViewer) m_pSceneHookHelper.ActiveViewer;
			
			//Redraw the rectangle
			pSceneViewer.Redraw(true);
			
			//GDI+ call to fill a rectangle with a hollow brush
			myGraphics.FillRectangle(m_brush, (int)pEnvelope.XMin, (int) pEnvelope.YMin,
				(int) pEnvelope.Width, (int) pEnvelope.Height);

			//GDI+ call to draw a rectangle with a specified pen 
			myGraphics.DrawRectangle(m_pen, (int)pEnvelope.XMin, (int) pEnvelope.YMin,
				(int) pEnvelope.Width, (int) pEnvelope.Height);
		}
	}
}
