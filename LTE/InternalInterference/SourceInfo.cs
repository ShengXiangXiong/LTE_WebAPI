using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

using LTE.Geometric;
using LTE.GIS;
using LTE.DB;

namespace LTE.InternalInterference
{
    /// <summary>
    /// 射线跟踪始发点小区信息
    /// </summary>
    public class SourceInfo
    {
        public Point SourcePoint { get; set; }
        public string SourceName { get; set; }
        public int eNodeB { get; set; }
        public int CI { get; set; }

        public double Azimuth { get; set; }
        public double Inclination { get; set; }
        public double RayAzimuth { get; set; }
        public double RayInclination { get; set; }

        public float directCoefficient{get;set;}
        public float reflectCoefficient{get;set;}
        public float diffracteCoefficient{get;set;}
        public float diffracteCoefficient2{get;set;} //菲涅尔绕射校正系数

        public SourceInfo(string SourceName, int enodeb, int ci, float directCoefficient, float reflectCoefficient, float diffracteCoefficient, float diffracteCoefficient2)
        {
            this.SourceName = SourceName;
            this.eNodeB = enodeb;
            this.CI = ci;
            this.directCoefficient = directCoefficient;
            this.reflectCoefficient = reflectCoefficient;
            this.diffracteCoefficient = diffracteCoefficient;
            this.diffracteCoefficient2 = diffracteCoefficient2;
        }

        public SourceInfo()
        {
        }

        public SourceInfo(SourceInfo s)
        {
            this.SourcePoint = s.SourcePoint.clone();
            this.SourceName = s.SourceName;
            this.eNodeB = s.eNodeB;
            this.CI = s.CI;
            this.Azimuth = s.Azimuth;
            this.Inclination = s.Inclination;
            this.RayAzimuth = s.RayAzimuth;
            this.RayInclination = s.RayInclination;
            this.diffracteCoefficient = s.diffracteCoefficient;
            this.reflectCoefficient = s.reflectCoefficient;
            this.diffracteCoefficient = s.diffracteCoefficient;
            this.diffracteCoefficient2 = s.diffracteCoefficient2;
        }

        public virtual SourceInfo clone()
        {
            SourceInfo s = new SourceInfo(this.SourceName, this.eNodeB, this.CI, this.diffracteCoefficient, this.reflectCoefficient, this.diffracteCoefficient, this.diffracteCoefficient2);
            s.SourcePoint = this.SourcePoint.clone();
            s.Azimuth = this.Azimuth;
            s.Inclination = this.Inclination;
            s.RayAzimuth = this.RayAzimuth;
            s.RayInclination = this.RayInclination;
            return s;
        }

        internal virtual void constructSourceInfo()
        {
        }

    }
}
