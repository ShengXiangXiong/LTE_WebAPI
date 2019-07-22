using System;
using System.Collections.Generic;
using System.Text;
using ESRI.ArcGIS.Geometry;

namespace LTE.GIS
{
    /// <summary>
    /// 坐标转换方法
    /// </summary>
    public class PointConvert
    {

        private static ISpatialReferenceFactory pSRF = null;
        private static ISpatialReference pGCS = null;
        private static ISpatialReference pPCS = null;

        private static readonly object obj = new object();
        private static readonly object syncPrj = new object();
        private static readonly object syncGeo = new object();
        private static PointConvert instance = null;


        /// <summary>
        /// 当前对象的实例
        /// </summary>
        public static PointConvert Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (obj)
                    {
                        if (instance == null)
                        {
                            instance = new PointConvert();
                            InitInfo();
                        }
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// 初始化常用的信息
        /// </summary>
        private static void InitInfo()
        {
            pSRF = new SpatialReferenceEnvironment();
            pGCS = pSRF.CreateGeographicCoordinateSystem((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);
            pPCS = pSRF.CreateProjectedCoordinateSystem((int)esriSRProjCSType.esriSRProjCS_WGS1984UTM_50N);
            //
            //(int)esriSRProjCSType.esriSRProjCS_Beijing1954GK_23N
            
        }
        /// <summary>
        /// 将经纬度点转换为某投影下的坐标点
        /// </summary>
        /// <param name="pPoint">经纬度点</param>
        /// <returns>某投影下的坐标点</returns>
        /// <remarks>其中pPoint的X是经度，Y是纬度，单位都是度，千万不要搞错，否则转换出来的值是不对的或转换不出值</remarks>
        public  IPoint GetProjectPoint(IPoint pPoint)
        {
            lock (syncPrj)
            {
                pPoint.SpatialReference = pGCS;
                pPoint.Project(pPCS);
            }
            return pPoint;
        }

        ///<summary>
        /// 将点转换为经纬度点
        /// </summary>
        /// <param name="pPoint">平面坐标点</param>
        ///<returns>经纬度点</returns>
        ///<remarks></remarks>
        public IPoint GetGeoPoint(IPoint pPoint)
        {
            //IPoint pProPoint = new PointClass();
            //pProPoint.PutCoords(pPoint.X, pPoint.Y);
            lock (syncGeo)
            {
                pPoint.SpatialReference = pPCS;
                pPoint.Project(pGCS);
            }
            return pPoint;
        }


    }
}
