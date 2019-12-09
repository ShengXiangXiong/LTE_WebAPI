using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/* Copyright (c) 2004-2005, Samuli Laine */
// Copyright (c) 2018-2019, 尹静萍 
// 文献：Samuli Laine, Samuel Siltanen, Tapio Lokki, Lauri Savioja. Accelerated beam tracing algorithm[J]. Applied Acoustics, 2009, 70(1): 172-181.
namespace LTE.Beam
{
    public class Ray
    {
        public Vector3 m_a;   // 起点
        public Vector3 m_b;   // 终点

        public Ray() { }
        public Ray(ref Vector3 a, ref Vector3 b) { m_a = new Vector3(ref a); m_b = new Vector3(ref b); }
        public Ray(Vector3 a, Vector3 b) { m_a = new Vector3(ref a); m_b = new Vector3(ref b); }
        public Ray(ref Ray ray) { m_a = new Vector3(ref ray.m_a); m_b = new Vector3(ref ray.m_b); }
        public void opAssign(ref Ray ray) { m_a = new Vector3(ref ray.m_a); m_b = new Vector3(ref ray.m_b); }


        public bool intersect(ref Polygon polygon)
        {
            // 判断点与面的位置
            float s0 = Vector4.dot(ref m_a, polygon.getPleq());
            float s1 = Vector4.dot(ref m_b, polygon.getPleq());

            if (s0 * s1 >= 0)  // 如果在面的同一侧，不可能有交点
                return false;

            int n = polygon.numPoints();

            Vector3 dir = m_b - m_a;
            Vector3 eb = polygon[n - 1] - m_a;
            float sign = 0;
            for (int i = 0; i < n; i++)
            {
                Vector3 ea = new Vector3(ref eb);
                eb = polygon[i] - m_a;

                float det = Vector3.dot(ref dir, Vector3.cross(ref ea, ref eb));

                if (sign == 0)  // 第一次
                    sign = det;
                else if (det * sign < 0)  // 不可能碰撞
                    return false;
            }

            return (sign != 0);
        }

        public bool intersectExt(ref Polygon polygon)
        {
            int n = polygon.numPoints();

            Vector3 dir = m_b - m_a;
            Vector3 eb = polygon[n - 1] - m_a;
            float sign = 0;
            for (int i = 0; i < n; i++)
            {
                Vector3 ea = new Vector3(ref eb);
                eb = polygon[i] - m_a;

                float det = Vector3.dot(ref dir, Vector3.cross(ref ea, ref eb));

                if (sign == 0)
                    sign = det;
                else if (det * sign < 0)
                    return false;
            }

            return (sign != 0);
        }

        public static Vector3 intersect(ref Ray ray, ref Vector4 pleq)
        {
            float s0 = Vector4.dot(ref ray.m_a, ref pleq);
            float s1 = Vector4.dot(ref ray.m_b, ref pleq);

            return ray.m_a + (s0 / (s0 - s1)) * (ray.m_b - ray.m_a);
        }

        // 2018.12.04
        public static NodeInfo intersect(ref Ray ray, ref Vector4 pleq, out Vector3 isect)
        {
            float s0 = Vector4.dot(ref ray.m_a, ref pleq);
            float s1 = Vector4.dot(ref ray.m_b, ref pleq);
            isect = ray.m_a + (s0 / (s0 - s1)) * (ray.m_b - ray.m_a);

            Point crossWithSidePlane = new Point(isect.x, isect.y, isect.z);
            Vector3 normal = new Vector3(pleq.x, pleq.y, pleq.z);
            Vector3 dir = ray.m_b - ray.m_a;
            RayType rayType;

            if (Math.Abs(pleq.x) < 0.000001 && Math.Abs(pleq.y) < 0.000001)
                rayType = RayType.HReflection;
            else
                rayType = RayType.VReflection;
            NodeInfo rayInfo = new NodeInfo(new Point(ray.m_a.x, ray.m_a.y, ray.m_a.z), crossWithSidePlane, null, null, 0, 0, normal, rayType, Vector3.getAngle(ref dir, ref normal) - Math.PI / 2.0);

            return rayInfo;
        }
        
        // 2018.12.04
        public static NodeInfo oneRay(Vector3 pt, Vector3 pt1, RayType rayType)
        {
            Vector3 normal = new Vector3(0, 0, 1);
            Vector3 dir = pt1 - pt;
            NodeInfo ray = new NodeInfo(new Point(pt.x, pt.y, pt.z), new Point(pt1.x, pt1.y, pt1.z), null, null, 0, 0, normal, rayType, Vector3.getAngle(ref dir, ref normal) - Math.PI / 2.0);
            return ray;
        }

        // 2018.12.11
        public static NodeInfo createRay(ref Ray ray, ref Vector4 pleq)
        {
            Vector3 dir = ray.m_b - ray.m_a;

            RayType rayType;
            double angle;
            if (Math.Abs(pleq.x) < 0.000001 && Math.Abs(pleq.y) < 0.000001)
                rayType = RayType.HReflection;
            else
                rayType = RayType.VReflection;

            Vector3 normal = new Vector3(pleq.x, pleq.y, pleq.z);
            angle = Vector3.getAngle(ref dir, ref normal) - Math.PI / 2.0;

            NodeInfo rayInfo = new NodeInfo(new Point(ray.m_a.x, ray.m_a.y, ray.m_a.z), new Point(ray.m_b.x, ray.m_b.y, ray.m_b.z), null, null, 0, 0, normal, rayType, angle);

            return rayInfo;
        }

        // 2018.12.11
        public static NodeInfo createRay(ref Ray ray)
        {
            Vector3 dir = ray.m_b - ray.m_a;

            RayType rayType = RayType.Direction;
            Vector3 normal = new Vector3(0, 0, 1);
            double angle = Vector3.getAngle(ref dir, ref normal) - Math.PI / 2.0;

            NodeInfo rayInfo = new NodeInfo(new Point(ray.m_a.x, ray.m_a.y, ray.m_a.z), new Point(ray.m_b.x, ray.m_b.y, ray.m_b.z), null, null, 0, 0, normal, rayType, angle);

            return rayInfo;
        }
    }
}
