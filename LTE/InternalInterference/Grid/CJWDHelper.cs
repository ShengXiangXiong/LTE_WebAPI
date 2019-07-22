using System;
using System.Data;
using LTE.DB;
using ESRI.ArcGIS.Geometry;
using LTE.InternalInterference.Grid;

namespace LTE.InternalInterference
{
	/// <summary>
	/// CJWDHelper 的摘要说明。
	/// 已知点A 和 点B的经纬度，求他们的距离和点B相对于点A的方位
	///  已知点A经纬度，根据B点据A点的距离，和方位，求B点的经纬度
	/// </summary>
	public class CJWDHelper
	{
        public CJWDHelper()
        {
            //
            // TODO: 在此处添加构造函数逻辑
            //
        }
        
		//! 计算点A 和 点B的经纬度，求他们的距离和点B相对于点A的方位
		/*! 
		  * \param A A点经纬度
		  * \param B B点经纬度
		  * \param angle B相对于A的方位, 不需要返回该值，则将其设为空
		  * \return A点B点的距离
		  */
		public static double distance(JWD A, JWD B, ref double angle)
		{
			double dx = (B.m_RadLo - A.m_RadLo) * A.Ed;
			double dy = (B.m_RadLa - A.m_RadLa) * A.Ec;
			double outresult = Math.Sqrt(dx * dx + dy * dy);
  
			if( angle != 390)
			{
				if(dx==0.0)
					angle=0;
				else if(dy==0.0)
					angle=90;
				else
					angle = Math.Atan(Math.Abs(dx/dy))*180/Math.PI;
				// 判断象限
				double dLo = B.m_Longitude - A.m_Longitude;
				double dLa = B.m_Latitude - A.m_Latitude;
   
				if(dLo > 0 && dLa <= 0) 
				{
					angle = (90 - angle) + 90;
				}
				else if(dLo <= 0 && dLa < 0) 
				{
					angle = angle + 180;
				}
				else if(dLo < 0 && dLa >= 0) 
				{
					angle = (90 - angle) + 270;
				}
			}
			return outresult/1000;
		}
        /// <summary>
        /// 返回两点球面距离
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns>返回两点距离，单位千米</returns>
        public static double distance(JWD A, JWD B)
        {
            double dx = (B.m_RadLo - A.m_RadLo) * A.Ed;
            double dy = (B.m_RadLa - A.m_RadLa) * A.Ec;
            double outresult = Math.Sqrt(dx * dx + dy * dy);

            return outresult / 1000;
        }
		//! 计算点A 和 点B的经纬度，求他们的距离和点B相对于点A的方位
		/*! 
		  * \param longitude1 A点经度
		  * \param latitude1 A点纬度
		  * \param longitude2 B点经度
		  * \param latitude2 B点纬度
		  * \param angle B相对于A的方位, 不需要返回该值，则将其设为空
		  * \return A点B点的距离
		  */
		public static double distance(
			double longitude1, double latitude1,
			double longitude2, double latitude2, 
			ref double angle)
		{
			JWD A=new JWD(longitude1,latitude1);
			JWD B=new JWD(longitude2,latitude2);
			return distance(A, B,ref angle);
		}


        public static double distance(
            double longitude1, double latitude1,
            double longitude2, double latitude2)
        {
            JWD A = new JWD(longitude1, latitude1);
            JWD B = new JWD(longitude2, latitude2);
            return distance(A, B);
        }
		//! 已知点A经纬度，根据B点据A点的距离，和方位，求B点的经纬度， 正北方向是0度
		/*!
		  * \param A 已知点A
		  * \param distance B点到A点的距离 
		  * \param angle B点相对于A点的方位
		  * \return B点的经纬度坐标
		  */
		public static JWD GetJWDB(JWD A, double distance, double angle)
		{ 
			double dx = distance*1000 * Math.Sin(angle * Math.PI /180);
			double dy = distance*1000 * Math.Cos(angle * Math.PI /180);
  
			//double dx = (B.m_RadLo - A.m_RadLo) * A.Ed;
			//double dy = (B.m_RadLa - A.m_RadLa) * A.Ec;
			double BJD = (dx/A.Ed + A.m_RadLo) * 180/Math.PI;
			double BWD = (dy/A.Ec + A.m_RadLa) * 180/Math.PI;
			JWD B=new JWD(BJD, BWD);
			return B;
		}
		//! 已知点A经纬度，根据B点据A点的距离，和方位，求B点的经纬度,正北方向为0度
		/*!
		  * \param longitude 已知点A经度
		  * \param latitude 已知点A纬度
		  * \param distance B点到A点的距离 
		  * \param angle B点相对于A点的方位
		  * \return B点的经纬度坐标
		  */
		public static JWD GetJWDB(double longitude, double latitude, double distance, double angle)
		{ 
			JWD A=new JWD(longitude,latitude);
			return GetJWDB(A, distance, angle);
		}
	
	}
}
