using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Windows.Forms;

using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.esriSystem;

namespace LTE.GIS
{
    /// <summary>
    /// 地图空间操作
    /// </summary>
    public class MapWorkSpace
    {
        /// <summary>
        /// 加载默认的地图空间
        /// </summary>
        /// <param name="sceneControl"></param>
        public void LoadDefaultWorkSpace(ISceneControl sceneControl)
        {
            if (sceneControl != null)
            {
                string defaultWorkSpacePath = GetDefaultWorkSpacePath();
                sceneControl.LoadSxFile(defaultWorkSpacePath);

            }

        }

        /// <summary>
        /// 加载用户的地图空间
        /// </summary>
        /// <param name="sceneControl"></param>
        public void LoadUserWorkSpace(ISceneControl sceneControl)
        {
            if (sceneControl != null)
            {
                string userWorkSpacePath = GetUserWorkSpacePath();
                if (!File.Exists(userWorkSpacePath))
                {
                    LoadDefaultWorkSpace(sceneControl);

                    //创建一个新的地图文档实例
                }
                else
                {
                    sceneControl.LoadSxFile(userWorkSpacePath);
                }
            }
        }
        /// <summary>
        /// 加载默认的地图空间
        /// </summary>
        /// <returns></returns>
        string GetDefaultWorkSpacePath()
        {
            string defaultWorkSpacePath = string.Empty;
            defaultWorkSpacePath = System.AppDomain.CurrentDomain.BaseDirectory; 
            CreatePath(defaultWorkSpacePath);
            defaultWorkSpacePath = defaultWorkSpacePath + @"..\..\MapFiles\default.sxd";
            if (!File.Exists(defaultWorkSpacePath))
            {
                throw new Exception("发生错误,原因为：默认的地图空间不存在！");
            }
            return defaultWorkSpacePath;
        }

        public static IFeatureWorkspace getWorkSpace()
        {
            // 运行时环境的绑定
            ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.EngineOrDesktop);

            // AO的许可级别
            IAoInitialize aoInitialize = new AoInitializeClass();
            esriLicenseStatus licenseStatus = aoInitialize.Initialize(esriLicenseProductCode.esriLicenseProductCodeStandard);
            if (licenseStatus != esriLicenseStatus.esriLicenseCheckedOut)
            {
                Console.WriteLine("Unable to check-out an ArcInfo license, error code is {0}", licenseStatus);
                return null;
            }

            string path = System.AppDomain.CurrentDomain.BaseDirectory + "../LTE/MapFiles";
            IWorkspaceFactory workspaceFactory = new ESRI.ArcGIS.DataSourcesFile.ShapefileWorkspaceFactoryClass();
            IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspaceFactory.OpenFromFile(path, 0);
            return featureWorkspace;
        }

        /// <summary>
        /// 获取用户的地图空间路径
        /// </summary>
        /// <returns></returns>
        string GetUserWorkSpacePath()
        {
            string userWorkSpacePath = string.Empty;
            userWorkSpacePath = System.IO.Directory.GetCurrentDirectory();
            CreatePath(userWorkSpacePath);
            userWorkSpacePath = userWorkSpacePath + @"workspace.sxd";
            return userWorkSpacePath;

        }


        /// <summary>
        /// 创建路径
        /// </summary>
        /// <param name="path"></param>
        void CreatePath(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}
