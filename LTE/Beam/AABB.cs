using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/* Copyright (c) 2004-2005, Samuli Laine */
// Copyright (c) 2018-2019, 尹静萍 
// 文献：Samuli Laine, Samuel Siltanen, Tapio Lokki, Lauri Savioja. Accelerated beam tracing algorithm[J]. Applied Acoustics, 2009, 70(1): 172-181.
namespace LTE.Beam
{
    public class AABB
    {
        public Vector3 m_mn;
        public Vector3 m_mx;

        public AABB() { m_mn = new Vector3(0, 0, 0); m_mx = new Vector3(0, 0, 0); }
        public AABB(ref Vector3 mn, ref Vector3 mx) { m_mn = new Vector3(ref mn); m_mx = new Vector3(ref mx); }
        public AABB(ref AABB aabb) { m_mn = new Vector3(ref aabb.m_mn); m_mx = new Vector3(ref aabb.m_mx); }
        public AABB(AABB aabb) { m_mn = new Vector3(ref aabb.m_mn); m_mx = new Vector3(ref aabb.m_mx); }
        public void opAssign(ref AABB aabb) { m_mn = aabb.m_mn; m_mx = aabb.m_mx; }

        public void grow(Vector3 p)
        {
            for (int j = 0; j < 3; j++)
            {
                if (p[j] < m_mn[j]) m_mn[j] = p[j];
                if (p[j] > m_mx[j]) m_mx[j] = p[j];
            }
        }

        public bool overlaps(ref AABB o)
        {
            return (m_mn.x < o.m_mx.x && m_mx.x > o.m_mn.x &&
                m_mn.y < o.m_mx.y && m_mx.y > o.m_mn.y &&
                m_mn.z < o.m_mx.z && m_mx.z > o.m_mn.z);
        }

        public bool contains(ref Vector3 p)
        {
            return (p.x > m_mn.x && p.x < m_mx.x &&
                p.y > m_mn.y && p.y < m_mx.y &&
                p.z > m_mn.z && p.z < m_mx.z);
        }
    };
}
