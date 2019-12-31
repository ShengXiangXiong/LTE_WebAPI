using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/* Copyright (c) 2004-2005, Samuli Laine */
// Copyright (c) 2018-2019, 尹静萍 
// 文献：Samuli Laine, Samuel Siltanen, Tapio Lokki, Lauri Savioja. Accelerated beam tracing algorithm[J]. Applied Acoustics, 2009, 70(1): 172-181.
namespace LTE.Beam
{
    public class Point
    {
        public Vector3 m_position;

        public Point()
        {
            m_position = new Vector3(0, 0, 0);
        }

        public Point(float x, float y, float z)
        {
            m_position = new Vector3(x, y, z);
        }

        public Point(ref Point s)
        {
            m_position = new Vector3(ref s.m_position);
        }

        public Point(Point s)
        {
            m_position = new Vector3(ref s.m_position);
        }

        public void opAssign(ref Point s)
        {
            m_position = new Vector3(ref s.m_position);
        }


        public Vector3 getPosition() { return m_position; }
        public void setPosition(Vector3 position) { m_position = new Vector3(ref position); }
    }
}
