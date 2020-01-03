using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/* Copyright (c) 2004-2005, Samuli Laine */
// Copyright (c) 2018-2019, 尹静萍 
// 文献：Samuli Laine, Samuel Siltanen, Tapio Lokki, Lauri Savioja. Accelerated beam tracing algorithm[J]. Applied Acoustics, 2009, 70(1): 172-181.
namespace LTE.Beam
{
    public class Beam
    {
        private Vector3 m_top;
        private Polygon m_polygon;
        private List<Vector4> m_pleqs;

        public Beam()
        {
            m_top = new Vector3();
            m_polygon = new Polygon();
            m_pleqs = new List<Vector4>();
        }

        public Beam(ref Vector3 top, ref Polygon polygon)
        {
            m_top = new Vector3(ref top);
            m_polygon = new Polygon(ref polygon);

            calculatePleqs();  // 得到 beam 的各面
        }

        public Beam(ref Beam beam)
        {
            m_top = new Vector3(ref beam.m_top);     // 源点的镜像
            m_polygon = new Polygon(ref beam.m_polygon); // 障碍物面
            m_pleqs = new List<Vector4>(beam.m_pleqs); // 各面的面方程
        }

        public Beam(Beam beam)
        {
            m_top = new Vector3(ref beam.m_top);     // 源点的镜像
            m_polygon = new Polygon(ref beam.m_polygon); // 障碍物面
            m_pleqs = new List<Vector4>(beam.m_pleqs); // 各面的面方程
        }

        public void opAssign(ref Beam beam)
        {
            m_top = new Vector3(ref beam.m_top);     // 源点的镜像
            m_polygon = new Polygon(ref beam.m_polygon); // 障碍物面
            m_pleqs = new List<Vector4>(beam.m_pleqs); // 各面的面方程
        }

        public Vector3 getTop() { return m_top; }
        public Polygon getPolygon() { return m_polygon; }
        public int numPleqs() { return (int)m_pleqs.Count(); }
        public Vector4 getPleq(int i) { return m_pleqs[i]; }

        public bool contains(ref Vector3 p)
        {
            for (int i = 0; i < numPleqs(); i++)
                if (Vector4.dot(ref p, getPleq(i)) < 0)
                    return false;
            return true;
        }
        //------------------------------------------------------------------------

        // 得到 beam 的各面，各面法向量向外
        public void calculatePleqs()
        {
            int n = m_polygon.numPoints();

            m_pleqs = new List<Vector4>(new Vector4[n + 1]);
            Vector3 p1 = m_polygon[n - 1];

            float sign = Vector4.dot(ref m_top, m_polygon.getPleq()) > 0 ? -1 : 1;  // -1: 虚拟点位于障碍物法向量一侧

            for (int i = 0; i < n; i++)
            {
                Vector3 p0 = p1;
                p1 = m_polygon[i];

                Vector4 plane = Polygon.getPlaneEquation(ref m_top, ref p0, ref p1);
                plane.normalize();
                m_pleqs[i + 1] = sign * plane;
            }
            m_pleqs[0] = sign * m_polygon.getPleq(); // 第一个面是障碍物面，法向量与障碍物面相反
        }
    }
}
