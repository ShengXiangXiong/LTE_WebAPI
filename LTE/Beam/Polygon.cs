using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

/* Copyright (c) 2004-2005, Samuli Laine */
// Copyright (c) 2018-2019, 尹静萍 
// 文献：Samuli Laine, Samuel Siltanen, Tapio Lokki, Lauri Savioja. Accelerated beam tracing algorithm[J]. Applied Acoustics, 2009, 70(1): 172-181.
namespace LTE.Beam
{
    public class Polygon
    {
        public List<Vector3> m_points;
        public Vector4 m_pleq;    // 平面方程
        public uint m_materialId;
        public int m_id;
        public int m_buildingID;  // 2018.12.04
        public bool m_first;      // 2018.12.11  初级 beam 底面
        public bool m_ground;     // 2019.6.17  是否为地形

        public Polygon()
        {
            m_points = new List<Vector3>();
            m_pleq = new Vector4(0, 0, 0, 0);
            m_materialId = 0;
            m_id = -1;
        }

        public Polygon(ref Polygon p)
        {
            m_points = new List<Vector3>(p.m_points);
            m_pleq = new Vector4(ref p.m_pleq);
            m_materialId = p.m_materialId;
            m_id = p.m_id;
            m_buildingID = p.m_buildingID;
            m_first = p.m_first;
            m_ground = p.m_ground; // 地形
        }

        public Polygon(Polygon p)
        {
            m_points = new List<Vector3>(p.m_points);
            m_pleq = new Vector4(ref p.m_pleq);
            m_materialId = p.m_materialId;
            m_id = p.m_id;
            m_buildingID = p.m_buildingID;
            m_first = p.m_first;
            m_ground = p.m_ground; // 地形
        }

        public Polygon(ref List<Vector3> points, int id, int bid, bool f, bool ground)
        {
            m_points = new List<Vector3>(points);
            m_materialId = 0;
            m_pleq = new Vector4();
            m_id = id;
            m_buildingID = bid;
            m_first = f;
            m_ground = ground; // 地形

            calculatePleq();
        }

        public Polygon(Vector3[] points, int numPoints, int id, int bid, bool f, bool ground)
        {
            m_points = new List<Vector3>(new Vector3[numPoints]);
            m_materialId = 0;
            m_pleq = new Vector4();
            m_id = id;
            m_buildingID = bid;
            m_first = f;
            m_ground = ground;  // 地形

            for (int i = 0; i < numPoints; i++)
                m_points[i] = points[i];

            calculatePleq();
        }

        public Polygon(ref List<Vector3> points, int numPoints, ref Vector4 pleq, int id, int bid)
        {
            m_points = new List<Vector3>(new Vector3[numPoints]);
            m_pleq = new Vector4(ref pleq);
            m_materialId = 0;
            m_id = id;
            m_buildingID = bid;

            for (int i = 0; i < numPoints; i++)
                m_points[i] = points[i];
        }

        public Polygon(ref List<Vector3> points, int numPoints, ref Vector4 pleq, uint materialId, int id, int bid)
        {
            m_points = new List<Vector3>(new Vector3[numPoints]);
            m_pleq = new Vector4(ref pleq);
            m_materialId = materialId;
            m_id = id;
            m_buildingID = bid;

            for (int i = 0; i < numPoints; i++)
                m_points[i] = points[i];
        }

        public Polygon(ref List<Vector3> points, uint materialId, int id, int bid)
        {
            m_points = new List<Vector3>(points);
            m_materialId = materialId;
            m_pleq = new Vector4();
            m_id = id;
            m_buildingID = bid;

            calculatePleq();
        }

        public void opAssign(ref Polygon p)
        {
            m_points = new List<Vector3>(p.m_points);
            m_pleq = new Vector4(ref p.m_pleq);
            m_materialId = p.m_materialId;
            m_id = p.m_id;
            m_buildingID = p.m_buildingID;
            m_first = p.m_first;
            m_ground = p.m_ground;  // 地形
        }

        public enum ClipResult
        {
            CLIP_CLIPPED,
            CLIP_ORIGINAL,
            CLIP_VANISHED
        };

        public uint getMaterialId() { return m_materialId; }
        public void setMaterialId(uint id) { m_materialId = id; }
        public int numPoints() { return m_points.Count; }
        public Vector3 this[int i] { get { return (i >= m_points.Count || m_points[i] == null) ? null : (Vector3)m_points[i]; } }
        public Vector4 getPleq() { return m_pleq; }
        public Vector3 getNormal() { return new Vector3(ref m_pleq); }

        //------------------------------------------------------------------------

        public static Vector4 getPlaneEquation(ref Vector3 a, ref Vector3 b, ref Vector3 c)
        {
            float x1 = b.x - a.x;
            float y1 = b.y - a.y;
            float z1 = b.z - a.z;
            float x2 = c.x - a.x;
            float y2 = c.y - a.y;
            float z2 = c.z - a.z;
            float nx = (y1 * z2) - (z1 * y2);
            float ny = (z1 * x2) - (x1 * z2);
            float nz = (x1 * y2) - (y1 * x2);

            return new Vector4(nx, ny, nz, -(a.x * nx + a.y * ny + a.z * nz));
        }

        public float getNonPlanarity()
        {
            float err = 0;
            for (int i = 0; i < numPoints(); i++)
                err = Math.Max(err, Vector4.dot(m_points[i], ref m_pleq));

            return err;
        }

        public float getArea()
        {
            int n = numPoints();

            Vector3 sum = new Vector3(0, 0, 0);
            for (int i = 0; i < n - 2; i++)
            {
                Vector3 v0 = m_points[0];
                Vector3 v1 = m_points[i + 1];
                Vector3 v2 = m_points[i + 2];

                sum += Vector3.cross(v1 - v0, v2 - v0);
            }

            return (float)0.5 * (float)sum.length();
        }

        public AABB getAABB()
        {
            AABB aabb = new AABB();
            if (0 == numPoints())
                return aabb;

            aabb.m_mn = new Vector3(m_points[0]);
            aabb.m_mx = new Vector3(m_points[0]);
            for (int i = 1; i < numPoints(); i++)
                aabb.grow(m_points[i]);

            return aabb;
        }

        //----------------------------------------------------------------

        // 点是否在3D平面内  奇偶法  2018.12.13
        public bool isInPolygon(ref List<Vector3> points, float x, float y, float z, int type)
        {
            bool okPlane = false;

            float err = 0.5F;

            if (type == 0)  // xy
            {
                for (int ii = 0, jj = points.Count - 1; ii < points.Count; jj = ii++)
                {
                    if ((points[ii].y > y) != (points[jj].y > y))
                    {
                        double tmp1 = Math.Round((points[jj].x - points[ii].x) * (y - points[ii].y) / (points[jj].y - points[ii].y) + points[ii].x, 3);

                        if (Math.Abs(x - tmp1) < err)
                        {
                            okPlane = true;
                            break;
                        }
                        else if (x < tmp1)
                        {
                            okPlane = !okPlane;
                        }
                    }
                }
            }
            else if (type == 1) // yz
            {
                for (int ii = 0, jj = points.Count - 1; ii < points.Count; jj = ii++)
                {
                    if ((points[ii].z > z) != (points[jj].z > z))
                    {
                        double tmp1 = Math.Round((points[jj].y - points[ii].y) * (z - points[ii].z) / (points[jj].z - points[ii].z) + points[ii].y, 3);

                        if (Math.Abs(y - tmp1) < err)
                        {
                            okPlane = true;
                            break;
                        }
                        else if (y < tmp1)
                        {
                            okPlane = !okPlane;
                        }
                    }
                }
            }
            else  // xz
            {
                for (int ii = 0, jj = points.Count - 1; ii < points.Count; jj = ii++)
                {
                    if ((points[ii].z > z) != (points[jj].z > z))
                    {
                        double tmp1 = Math.Round((points[jj].x - points[ii].x) * (z - points[ii].z) / (points[jj].z - points[ii].z) + points[ii].x, 3);

                        if (Math.Abs(x - tmp1) < err)
                        {
                            okPlane = true;
                            break;
                        }
                        else if (x < tmp1)
                        {
                            okPlane = !okPlane;
                        }
                    }
                }
            }

            return okPlane;
        }

        // 得到位于平面 plane 内的点 2018.12.17
        public List<Vector3> getInerPoints(float interval, ref List<Vector3> plane)
        {
            List<Vector3> pts = new List<Vector3>();

            AABB aabb = getAABB();

            if (Math.Abs(aabb.m_mn.z - aabb.m_mx.z) < 0.1)  // 水平面
            {
                for (float x = aabb.m_mn.x + 1; x < aabb.m_mx.x - 1; x += interval)
                {
                    for (float y = aabb.m_mn.y + 1; y < aabb.m_mx.y - 1; y += interval)
                    {
                        if (isInPolygon(ref plane, x, y, 0, 0))
                        {
                            pts.Add(new Vector3(x, y, plane[0].z));
                        }
                    }
                }
            }
            else  // 侧面
            {
                Vector3 s = m_points[0];
                Vector3 t = m_points[1];

                double len = Math.Sqrt(Math.Pow(s.x - t.x, 2) + Math.Pow(s.y - t.y, 2));  // 线段长度

                LTE.Geometric.Vector2D a = new LTE.Geometric.Vector2D(s.x, s.y);
                LTE.Geometric.Vector2D b = new LTE.Geometric.Vector2D(t.x, t.y);
                LTE.Geometric.Line2D line = new LTE.Geometric.Line2D(a, b);

                // 粗略认为面被切割前后一样，更细致的检查交给射线跟踪
                if (plane.Count == 4)
                {
                    for (float i = 1; i < len - 1; i += interval)  // line = S + Ct
                    {
                        Vector3 p = new Vector3();
                        p.x = (float)line.S.x + i * (float)line.C.x;
                        p.y = (float)line.S.y + i * (float)line.C.y;

                        for (float h = aabb.m_mn.z + 1; h < aabb.m_mx.z - 1; h += interval)
                        {
                            p.z = h;
                            pts.Add(new Vector3(ref p));
                        }
                    }
                }
                else // 被切割后的面更小一些
                {
                    for (float i = 1; i < len - 1; i += interval)  // line = S + Ct
                    {
                        Vector3 p = new Vector3();
                        p.x = (float)line.S.x + i * (float)line.C.x;
                        p.y = (float)line.S.y + i * (float)line.C.y;

                        for (float h = aabb.m_mn.z + 1; h < aabb.m_mx.z - 1; h += interval)
                        {
                            p.z = h;
                            if (isInPolygon(ref plane, p.x, p.y, h, 1) && isInPolygon(ref plane, p.x, p.y, h, 2))
                                pts.Add(new Vector3(ref p));
                        }
                    }
                }
            }
            return pts;
        }

        //----------------------------------------------------------------------------

        // 计算平面方程 Ax + By + Cz + D = 0
        public void calculatePleq()
        {
            int n = numPoints();

            // 计算
            Vector3 normalSum = new Vector3(0, 0, 0);
            for (int i = 0; i < n - 2; i++)
            {
                Vector3 v0 = m_points[0];
                Vector3 v1 = m_points[i + 1];
                Vector3 v2 = m_points[i + 2];

                Vector3 v12 = v1 - v0;
                Vector3 v20 = v2 - v0;
                normalSum += Vector3.cross(ref v12, ref v20);
            }

            // 使用具有最大叉乘结果的三角形
            float bestMagnitude = 0;
            m_pleq.set(0, 0, 0, 0);

            for (int i = 0; i < n - 2; i++)
                for (int j = i + 1; j < n - 1; j++)
                    for (int k = j + 1; k < n; k++)
                    {
                        Vector3 v0 = m_points[i];
                        Vector3 v1 = m_points[j];
                        Vector3 v2 = m_points[k];

                        Vector4 pleq = getPlaneEquation(ref v0, ref v1, ref v2);
                        float mag = pleq.x * pleq.x + pleq.y * pleq.y + pleq.z * pleq.z;

                        if (mag > bestMagnitude)
                        {
                            bestMagnitude = mag;
                            m_pleq = pleq;
                        }
                    }

            if (bestMagnitude == 0)
                return;

            // 归一化和矫正
            Vector3 normal = new Vector3(m_pleq.x, m_pleq.y, m_pleq.z);
            if (Vector3.dot(ref normal, ref normalSum) < 0)
                m_pleq.opNegative();
            m_pleq *= 1 / normal.length();
        }

        public List<Vector3>[] s_clipBuffer = new List<Vector3>[2];

        public ClipResult clipInner(ref List<Vector3> inPoints, int numInPoints,
                                   ref List<Vector3> outPoints, ref int numOutPoints,
                                   ref Vector4 pleq)
        {
            numOutPoints = 0;
            if (numInPoints == 0)
                return ClipResult.CLIP_VANISHED;

            ClipResult result = ClipResult.CLIP_ORIGINAL;
            Vector3 a;
            Vector3 b = inPoints[numInPoints - 1];
            float sa = 0;
            float sb = Vector4.dot(ref b, ref pleq);

            for (int i = 0; i < numInPoints; i++)  // 障碍物
            {
                a = b;
                b = inPoints[i];
                sa = sb;
                sb = Vector4.dot(ref b, ref pleq);
                bool na = sa < 0;
                bool nb = sb < 0;

                if (!na && !nb)
                {
                    outPoints[numOutPoints++] = b;
                    continue;
                }

                result = ClipResult.CLIP_CLIPPED;
                if (na && nb)
                    continue;

                float cval = sa / (sa - sb);
                Vector3 c = a + cval * (b - a);

                outPoints[numOutPoints++] = c;

                if (na)
                    outPoints[numOutPoints++] = b;
            }

            if (numOutPoints == 0)
                return ClipResult.CLIP_VANISHED;
            return result;
        }

        public ClipResult clipInner(ref List<Vector3> inPoints, int numInPoints,
                                    ref List<Vector3> outPoints, ref int numOutPoints,
                                    Vector4 pleq)
        {
            numOutPoints = 0;
            if (numInPoints == 0)
                return ClipResult.CLIP_VANISHED;

            ClipResult result = ClipResult.CLIP_ORIGINAL;
            Vector3 a;
            Vector3 b = inPoints[numInPoints - 1];
            float sa = 0;
            float sb = Vector4.dot(ref b, ref pleq);

            for (int i = 0; i < numInPoints; i++)  // 障碍物
            {
                a = b;
                b = inPoints[i];
                sa = sb;
                sb = Vector4.dot(ref b, ref pleq);
                bool na = sa < 0;
                bool nb = sb < 0;

                if (!na && !nb)
                {
                    outPoints[numOutPoints++] = b;
                    continue;
                }

                result = ClipResult.CLIP_CLIPPED;
                if (na && nb)
                    continue;

                float cval = sa / (sa - sb);
                Vector3 c = a + cval * (b - a);

                outPoints[numOutPoints++] = c;

                if (na)
                    outPoints[numOutPoints++] = b;
            }

            if (numOutPoints == 0)
                return ClipResult.CLIP_VANISHED;
            return result;
        }

        //------------------------------------------------------------------------

        ClipResult clip(ref Vector4 pleq)
        {
            int n = m_points.Count();

            if (s_clipBuffer[0] == null || (int)s_clipBuffer[0].Count() < n * 2)
            {
                s_clipBuffer[0] = new List<Vector3>();
                s_clipBuffer[1] = new List<Vector3>();
                for (int i = 0; i < n * 2; i++)
                {
                    Vector3 v = new Vector3();
                    Vector3 v1 = new Vector3();
                    s_clipBuffer[0].Add(v);
                    s_clipBuffer[1].Add(v1);
                }
            }

            int clippedVertexCount = 0;
            ClipResult result = clipInner(
                ref m_points, m_points.Count(),
                ref s_clipBuffer[0], ref clippedVertexCount,
                ref pleq);

            for (int i = 0; i < clippedVertexCount; i++)
                m_points.Add(new Vector3(s_clipBuffer[0][i]));

            return result;
        }

        //------------------------------------------------------------------------

        public ClipResult clip(ref AABB aabb)
        {
            bool clipped = false;

            for (int axis = 0; axis < 3; axis++)
                for (int dir = 0; dir < 2; dir++)
                {
                    Vector4 pleq = new Vector4(0, 0, 0, 0);
                    pleq[axis] = dir == 0 ? 1 : -1;
                    pleq.w = -pleq[axis] * (dir == 0 ? aabb.m_mn[axis] : aabb.m_mx[axis]);

                    ClipResult res = clip(ref pleq);
                    if (res == ClipResult.CLIP_VANISHED)
                        return ClipResult.CLIP_VANISHED;
                    else if (res == ClipResult.CLIP_CLIPPED)
                        clipped = true;
                }

            if (clipped)
                return ClipResult.CLIP_CLIPPED;

            return ClipResult.CLIP_ORIGINAL;
        }
        //------------------------------------------------------------------------

        public ClipResult clip(ref Beam beam)
        {
            int m = numPoints();
            if (m == 0)
                return ClipResult.CLIP_VANISHED;

            int n = beam.numPleqs();
            if (n == 0)
                return ClipResult.CLIP_ORIGINAL;

            ClipResult result = ClipResult.CLIP_ORIGINAL;

            if (s_clipBuffer[0] == null || (int)s_clipBuffer[0].Count < (n + m) * 2)
            {
                s_clipBuffer[0] = new List<Vector3>();
                s_clipBuffer[1] = new List<Vector3>();

                for (int i = 0; i < (n + m) * 2; i++)
                {
                    s_clipBuffer[0].Add(new Vector3());
                    s_clipBuffer[1].Add(new Vector3());
                }
            }

            int clippedVertices = 0;
            ClipResult res = clipInner(
                    ref m_points, m_points.Count,
                    ref s_clipBuffer[0], ref clippedVertices,
                    beam.getPleq(0));
            if (res == ClipResult.CLIP_VANISHED)
                return ClipResult.CLIP_VANISHED;
            else if (res == ClipResult.CLIP_CLIPPED)
                result = ClipResult.CLIP_CLIPPED;

            List<Vector3> clipSource = s_clipBuffer[0];  // 新的障碍物面
            List<Vector3> clipTarget = s_clipBuffer[1];

            for (int i = 1; i < n; i++)  // 对beam的每一个面
            {
                int newClippedVertices = 0;
                res = clipInner(
                        ref clipSource, clippedVertices,
                        ref clipTarget, ref newClippedVertices,
                        beam.getPleq(i));

                clippedVertices = newClippedVertices;
                List<Vector3> tmp = clipSource;
                clipSource = clipTarget;
                clipTarget = tmp;

                if (res == ClipResult.CLIP_VANISHED)
                    return ClipResult.CLIP_VANISHED;
                else if (res == ClipResult.CLIP_CLIPPED)
                    result = ClipResult.CLIP_CLIPPED;
            }

            m_points = new List<Vector3>(new Vector3[clippedVertices]);
            for (int i = 0; i < clippedVertices; i++)
                m_points[i] = clipSource[i];

            return result;
        }
    };
}