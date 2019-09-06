using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoAPI.CoordinateSystems;
using GeoAPI.CoordinateSystems.Transformations;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace LTE.GIS
{   
    /// <summary>
    /// 使用proj.net库进行二维坐标的转换，比AE提供的接口转换速度更快,by JinHaijia.
    /// </summary>
    public class PointConvertByProj
    {
        private static CoordinateTransformationFactory pCTFAC = null;
        private static IProjectedCoordinateSystem pPCS = null;
        private static GeographicCoordinateSystem pGCS = null;

        private static readonly object obj = new object();
        private static readonly object syncPrj = new object();
        private static readonly object syncGeo = new object();
        private static PointConvertByProj instance = null;


        /// <summary>
        /// 当前对象的实例
        /// </summary>
        public static PointConvertByProj Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (obj)
                    {
                        if (instance == null)
                        {
                            instance = new PointConvertByProj();
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
            pCTFAC = new CoordinateTransformationFactory();
            pPCS = ProjectedCoordinateSystem.WGS84_UTM(50, true);
            pGCS = GeographicCoordinateSystem.WGS84;

        }
        /// <summary>
        /// 将经纬度点转换为某投影下的坐标点
        /// </summary>
        /// <param name="point">经纬度点</param>
        /// <returns>某投影下的坐标点</returns>
        /// <remarks>其中pPoint的X是经度，Y是纬度，单位都是度，千万不要搞错，否则转换出来的值是不对的或转换不出值</remarks>
        public LTE.Geometric.Point GetProjectPoint(LTE.Geometric.Point point)
        {
            lock (syncPrj)
            {
                double[] p = new double[2];
                p[0] = point.X;
                p[1] = point.Y;
                ICoordinateTransformation trans = pCTFAC.CreateFromCoordinateSystems(pGCS, pPCS);
                p = trans.MathTransform.Transform(p);
                point.X = p[0];
                point.Y = p[1];
            }
            return point;
        }

        ///<summary>
        /// 将点转换为经纬度点
        /// </summary>
        /// <param name="point">平面坐标点</param>
        ///<returns>经纬度点</returns>
        ///<remarks></remarks>
        public LTE.Geometric.Point GetGeoPoint(LTE.Geometric.Point point)
        {
            lock (syncGeo)
            {
                double[] p = new double[2];
                p[0] = point.X;
                p[1] = point.Y;
                ICoordinateTransformation trans = pCTFAC.CreateFromCoordinateSystems(pPCS, pGCS);
                p = trans.MathTransform.Transform(p);
                point.X = p[0];
                point.Y = p[1];
            }
            return point;
        }

    }
}
