using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/* Copyright (c) 2004-2005, Samuli Laine */
// Copyright (c) 2018-2019, 尹静萍 
// 文献：Samuli Laine, Samuel Siltanen, Tapio Lokki, Lauri Savioja. Accelerated beam tracing algorithm[J]. Applied Acoustics, 2009, 70(1): 172-181.
namespace LTE.Beam
{
    public class TempNode
    {
        public List<TempNode> m_children;    // 2 个子节点
        public int m_splitAxis;   // 将垂直于m_splitAxis轴，平行于另两个轴的平面作为分割面
        public float m_splitPos;    // 该分割面经过的的点
        public List<Polygon> m_polygons;    // 属于当前节点的多边形
        public int m_numPolygons;

        public TempNode()
        {
            m_splitAxis = -1;
            m_splitPos = 0;
            m_polygons = new List<Polygon>();
            m_numPolygons = 0;
            m_children = new List<TempNode>(new TempNode[2]);
        }

    };

    public class SortItem
    {
        public float v;
        public Polygon polygon;
        public uint iptr;

        public SortItem()
        {
            iptr = 0;
            v = 0;
            polygon = new Polygon();
        }

        public SortItem(ref SortItem s)
        {
            v = s.v;
            iptr = s.iptr;
            polygon = new Polygon(ref s.polygon);
        }

        public SortItem(SortItem s)
        {
            v = s.v;
            iptr = s.iptr;
            polygon = new Polygon(ref s.polygon);
        }
    };

    public class SortItemCompare : IComparer<SortItem>
    {
        public int Compare(SortItem a, SortItem b)
        {
            if (a.v < b.v)
                return 1;
            else if (a.v == b.v)
                if (a.iptr < b.iptr)
                    return 1;
            return 0;
        }
    }
    //------------------------------------------------------------------------

    public class KD
    {
        public TempNode m_hierarchy;   // KD 根节点
        public AABB m_aabb;

        int g_maxPolygonsInLeaf = 30;
        float EPS_RAY_ENDS = 1;
        float EPS_BOUNDING_BOX = 1;
        float EPS_POLY_BOX_OVERLAP = 1;
        float EPS_ISECT_POLYGON = 1e-5f;
        float EPS_DISTANCE = 1e-5f;

        static List<SortItem> g_items;
        static int g_totalPolys;
        static int g_maxDepth;
        static int g_numNodes;

        public KD()
        {
            g_items = new List<SortItem>();
            m_hierarchy = new TempNode();
            g_totalPolys = 0;
            g_maxDepth = 0;
            g_numNodes = 0;

            m_aabb = new AABB();
            m_aabb.m_mn = new Vector3(0, 0, 0);
            m_aabb.m_mx = new Vector3(0, 0, 0);
        }

        void swap(SortItem a, SortItem b)
        {
            SortItem tmp = new SortItem(ref a);
            a.polygon = b.polygon; a.v = b.v; a.iptr = b.iptr;
            b.polygon = tmp.polygon; b.v = tmp.v; b.iptr = tmp.iptr;
        }

        void swap(ref int a, ref int b)
        {
            int tmp = a;
            a = b;
            b = tmp;
        }

        public void insertionSort(ref List<SortItem> items, int start, int end)
        {
            for (int i = start; i < end - 1; i++)
            {
                int k = -1;
                float v = items[i].v;
                uint iptr = items[i].iptr;
                for (int j = i + 1; j < end; j++)
                {
                    if (items[j].v < v)
                    {
                        v = items[j].v;
                        k = j;
                        iptr = items[j].iptr;
                    }
                    else if (items[j].v == v)
                    {
                        if (items[j].iptr < iptr)
                        {
                            v = items[j].v;
                            k = j;
                            iptr = items[j].iptr;
                        }
                    }
                }
                if (k >= 0)
                    swap(items[i], items[k]);

                //for (int j = start; j < end; j++)
                //{
                //    Console.WriteLine("{0}，{1}，{2}", j, g_items[j].v, g_items[j].iptr);
                //}
                //Console.WriteLine();
            }
        }

        public int median3(int low, int high)
        {
            int l = low;
            int c = (high + low) >> 1;
            int h = high - 2;

            SortItem lv = g_items[l];
            SortItem cv = g_items[c];
            SortItem hv = g_items[h];

            if (hv.v < lv.v || (hv.v == lv.v && hv.iptr < lv.iptr)) { swap(ref l, ref h); swap(lv, hv); }
            if (cv.v < lv.v || (cv.v == lv.v && cv.iptr < lv.iptr)) { swap(ref l, ref c); swap(lv, cv); }
            if (hv.v < cv.v || (hv.v == cv.v && hv.iptr < cv.iptr)) { swap(ref c, ref h); swap(cv, hv); }

            return c;
        }

        int getPartition(int i, int j)
        {
            SortItem pivot = g_items[i];
            while (i < j)
            {
                while (i < j && g_items[i].v < pivot.v || (g_items[i].v == pivot.v && g_items[i].iptr < pivot.iptr))
                    i++;
                while (i < j && j >= 0 && pivot.v < g_items[j].v || (pivot.v == g_items[j].v && pivot.iptr < g_items[j].iptr))
                    j--;
                swap(g_items[i], g_items[j]);
            }
            swap(g_items[i], pivot);
            return i;
        }

        // 2018.12.17  改为非递归，避免栈溢出
        void quickSort(int low, int high)
        {
            int SWITCHPOINT = 2000000000;
            if ((high - low) <= SWITCHPOINT)  
            {
                insertionSort(ref g_items, low, high);
                return;
            }

            high -= 1;

            Stack<int> s = new Stack<int>();

            // 选则 pivot 
            int pivotIndex = median3(low, high);
            swap(g_items[high - 1], g_items[pivotIndex]);

            if (low < high)
            {
                int mid = getPartition(low, high);
                if (mid - 1 > low)
                {
                    s.Push(low);
                    s.Push(mid - 1);
                }
                if (mid + 1 < high)
                {
                    s.Push(mid + 1);
                    s.Push(high);
                }
                while (s.Count != 0)
                {
                    int qHeight = s.Peek();
                    s.Pop();
                    int pLow = s.Peek();
                    s.Pop();

                    int pqMid = getPartition(pLow, qHeight);
                    if (pqMid - 1 > pLow)
                    {
                        s.Push(pLow);
                        s.Push(pqMid - 1);
                    }
                    if (pqMid + 1 < qHeight)
                    {
                        s.Push(pqMid + 1);
                        s.Push(qHeight);
                    }
                }
            }
        }

        // 选则最好分割面
        int getOptimalSplitPlane(ref List<Polygon> polygons, int numPolygons, ref float bestSplitPos, ref AABB aabb)
        {
            int bestSplitAxis = -1;
            float bestCost = 0;

            for (int axis = 0; axis < 3; axis++)
            {
                int[] nextAxis = new int[] { 1, 2, 0, 1 };

                // 构建 item 数组
                int k = 0;
                for (int i = 0; i < numPolygons; i++)
                {
                    Polygon poly = polygons[i];
                    float mn = poly[0][axis];
                    float mx = mn;
                    for (int j = 1; j < poly.numPoints(); j++)  // 得到每个多边形的包围盒
                    {
                        mn = Math.Min(mn, poly[j][axis]);
                        mx = Math.Max(mx, poly[j][axis]);
                    }

                    g_items[k].v = mn;
                    g_items[k].polygon = polygons[i];
                    g_items[k].iptr = (uint)k;
                    g_items[k + 1].v = mx;
                    g_items[k + 1].iptr = (uint)k + 1; ;
                    k += 2;
                }

                //for (int i = 0; i < numPolygons * 2; i++)
                //{
                //    Console.WriteLine("{0}，{1}，{2}", i, g_items[i].v, g_items[i].iptr);
                //}
                //Console.WriteLine();

                // 排序
                quickSort(0, numPolygons * 2);

                //for (int i = 0; i < numPolygons * 2; i++)
                //{
                //    Console.WriteLine("{0}，{1}，{2}", i, g_items[i].v, g_items[i].iptr);
                //}
                //Console.WriteLine();
                //Console.WriteLine();

                // 区域
                int c1 = nextAxis[axis];
                int c2 = nextAxis[axis + 1];
                float areaConst = 2 * (aabb.m_mx[c1] - aabb.m_mn[c1]) * (aabb.m_mx[c2] - aabb.m_mn[c2]);
                float areaFactor = 2 * ((aabb.m_mx[c1] - aabb.m_mn[c1]) + (aabb.m_mx[c2] - aabb.m_mn[c2]));
                float boundLeft = aabb.m_mn[axis];
                float boundRight = aabb.m_mx[axis];

                // 遍历，寻找最小代价分割面
                float bestAxisCost = 0;
                float bestAxisSplit = 0;
                int leftPolys = 0;
                int rightPolys = numPolygons;
                int bothPolys = 0;
                for (int i = 0; i < numPolygons * 2; i++)
                {
                    SortItem it = g_items[i];

                    if (it.iptr % 2 == 0)
                    {
                        leftPolys++;
                        bothPolys++;
                    }

                    if (it.v >= boundRight)
                        break;

                    if (it.v > boundLeft)
                    {
                        float split = it.v;
                        float aLeft = areaConst + areaFactor * (split - boundLeft);
                        float aRight = areaConst + areaFactor * (boundRight - split);
                        float cost = aLeft * leftPolys + aRight * rightPolys;
                        if (cost < bestAxisCost || bestAxisCost == 0)
                        {
                            bestAxisCost = cost;
                            bestAxisSplit = split;
                        }
                    }
                    if (it.iptr % 2 == 1)
                    {
                        rightPolys--;
                        bothPolys--;
                    }
                }

                if ((bestAxisCost < bestCost || bestCost == 0) && bestAxisCost > 0)
                {
                    bestCost = bestAxisCost;
                    bestSplitPos = bestAxisSplit;
                    bestSplitAxis = axis;
                }
            }

            return bestSplitAxis;
        }

        //------------------------------------------------------------------------
        // 构建 KD 树
        //------------------------------------------------------------------------

        void swap(Polygon a, Polygon b)
        {
            Polygon tmp = new Polygon(ref a);
            a.opAssign(ref b);
            b.opAssign(ref tmp);
            //a.m_materialId = b.m_materialId; a.m_pleq = b.m_pleq; a.m_points = b.m_points; a.m_id = b.m_id; a.m_buildingID = b.m_buildingID; a.m_first = b.m_first;
            //b.m_materialId = tmp.m_materialId; b.m_pleq = tmp.m_pleq; b.m_points = tmp.m_points; b.m_id = tmp.m_id; b.m_buildingID = tmp.m_buildingID; b.m_first = tmp.m_first;
        }

        // 每个节点保存多边形数组，多边形数量，分割面，子节点指针
        public TempNode constructRecursive(ref List<Polygon> polygons, int numPolygons, ref AABB aabb)
        {
            // 叶子
            if (numPolygons <= g_maxPolygonsInLeaf)
            {
                g_totalPolys += numPolygons;
                TempNode n = new TempNode();
                n.m_numPolygons = numPolygons;
                if (numPolygons > 0)
                {
                    n.m_polygons = new List<Polygon>();
                    for (int i = 0; i < numPolygons; i++)
                        n.m_polygons.Add(new Polygon(polygons[i]));
                }
                return n;
            }

            // 寻找最佳分割面
            float splitPos = 0;
            int axis = getOptimalSplitPlane(ref polygons, numPolygons, ref splitPos, ref aabb);
            if (axis < 0)  // 不可分割，成为叶子
            {
                g_totalPolys += numPolygons;
                TempNode n = new TempNode();
                n.m_numPolygons = numPolygons;
                n.m_polygons = new List<Polygon>();
                for (int i = 0; i < polygons.Count; i++)
                    n.m_polygons.Add(new Polygon(polygons[i]));
                return n;
            }

            // 分割
            TempNode n1 = new TempNode();
            n1.m_splitAxis = axis;
            n1.m_splitPos = splitPos;
            n1.m_numPolygons = numPolygons;

            // 将多边形分类
            for (int c = 0; c < 2; c++) // 0，分割面左侧；1，分割面右侧
            {
                AABB aabb2 = new AABB(ref aabb);  // 父区域大小

                if (c == 0)
                    aabb2.m_mx[axis] = splitPos;
                else
                    aabb2.m_mn[axis] = splitPos;

                AABB aabbTest = new AABB(ref aabb2);  // 子区域大小

                // 每个维度扩大一些
                Vector3 v = new Vector3(EPS_POLY_BOX_OVERLAP, EPS_POLY_BOX_OVERLAP, EPS_POLY_BOX_OVERLAP);
                aabbTest.m_mn -= v;
                aabbTest.m_mx += v;

                // 每个多边形应该位于哪个子区域内
                int childPolys = 0;
                for (int i = 0; i < numPolygons; i++)
                {
                    Polygon poly = new Polygon(polygons[i]);	// 复制
                    AABB pbox = poly.getAABB();

                    bool overlap = false;
                    if (pbox.m_mn[axis] == splitPos && pbox.m_mx[axis] == splitPos)  // 本身是一个分割面
                        overlap = (c == 1); // 本身是分割面，属于右子节点
                    else
                    {
                        // 确定分割面
                        for (int j = 0; j < poly.numPoints(); j++)
                        {
                            float x = poly[j][axis];
                            if (c == 0 && x < splitPos)
                                overlap = true;  // 被包含在当前半区域内
                            if (c == 1 && x > splitPos)
                                overlap = true;
                        }
                    }

                    if (!overlap)
                        continue;

                    if (poly.clip(ref aabbTest) != Polygon.ClipResult.CLIP_VANISHED)
                    {
                        if (i != childPolys)
                            swap(polygons[i], polygons[childPolys]);
                        childPolys++;
                    }
                }

                n1.m_children[c] = constructRecursive(ref polygons, childPolys, ref aabb2);
            }

            return n1;
        }

        public int getDepth(TempNode n)
        {
            g_numNodes++;
            if (n.m_splitAxis < 0)
            {
                return 1;
            }
            int d0 = getDepth(n.m_children[0]);
            int d1 = getDepth(n.m_children[1]);
            if (d0 > d1)
                return d0 + 1;
            return d1 + 1;
        }

        // 创建 KD 树，得到 KD 树相关信息
        public void constructHierarchy(ref List<Polygon> polygons, int numPolygons)
        {
            g_totalPolys = 0;
            // 计算包围盒，构建排序数组
            g_items = new List<SortItem>();
            for (int i = 0; i < 2 * numPolygons; i++)
                g_items.Add(new SortItem());

            for (int i = 0; i < 3; i++)
                m_aabb.m_mn[i] = m_aabb.m_mx[i] = polygons[0][0][i];

            for (int i = 0; i < numPolygons; i++)
            {
                Polygon poly = polygons[i];
                for (int j = 0; j < poly.numPoints(); j++)
                    m_aabb.grow(poly[j]);
            }

            // 稍微扩大包围盒，把多边形的边界也包含进来
            Vector3 v = new Vector3(EPS_BOUNDING_BOX, EPS_BOUNDING_BOX, EPS_BOUNDING_BOX);
            m_aabb.m_mn -= v;
            m_aabb.m_mx += v;

            // 构建 KD 树
            m_hierarchy = constructRecursive(ref polygons, numPolygons, ref m_aabb);  // 根节点
            Console.WriteLine("场景中的障碍物面总数：{0}", g_totalPolys);

            // 计算最大深度
            g_numNodes = 0;

            g_maxDepth = getDepth(m_hierarchy);  // KD 树的最大深度
            Console.WriteLine("KD 树节点数: {0}", g_numNodes);
            Console.WriteLine("KD 树深度: {0}", g_maxDepth);

            // 清空
            g_items.Clear();
        }

        //------------------------------------------------------------------------
        // Ray cast helpers
        //------------------------------------------------------------------------
        Vector3 g_orig = new Vector3();
        Vector3 g_dest = new Vector3();
        Vector3 g_dir = new Vector3();
        Vector3 g_invdir = new Vector3();
        int[] g_dirsgn = new int[3];
        HashSet<int> g_foundPolygons = new HashSet<int>();

        Vector3 g_beamMid = new Vector3();
        Vector3 g_beamDiag = new Vector3();
        Beam g_beamBeam = new Beam();
        List<Polygon> g_beamResult;

        public void setupRayCast(ref Ray ray)
        {
            Vector3 ndir = EPS_RAY_ENDS * Vector3.normalize(ray.m_b - ray.m_a);  // 射线单位向量

            g_orig = ray.m_a + ndir;
            g_dest = ray.m_b - ndir;
            g_dir = g_dest - g_orig;

            g_invdir.set(1 / g_dir.x, 1 / g_dir.y, 1 / g_dir.z);
            g_dirsgn[0] = g_invdir[0] < 0 ? 1 : 0;
            g_dirsgn[1] = g_invdir[1] < 0 ? 1 : 0;
            g_dirsgn[2] = g_invdir[2] < 0 ? 1 : 0;
        }

        public float getSplitDistance(float splitPos, int axis)
        {
            return (splitPos - g_orig[axis]) * g_invdir[axis];
        }

        public void getEnterExitDistances(ref AABB aabb, ref float dEnter, ref float dExit)
        {
            float[] x = new float[2];
            float[] y = new float[2];
            float[] z = new float[2];

            // 进入和退出的距离
            x[0] = getSplitDistance(aabb.m_mn[0], 0);
            y[0] = getSplitDistance(aabb.m_mn[1], 1);
            z[0] = getSplitDistance(aabb.m_mn[2], 2);
            x[1] = getSplitDistance(aabb.m_mx[0], 0);
            y[1] = getSplitDistance(aabb.m_mx[1], 1);
            z[1] = getSplitDistance(aabb.m_mx[2], 2);

            int sx = g_dirsgn[0];
            int sy = g_dirsgn[1];
            int sz = g_dirsgn[2];

            // 进入和退出
            float mn0 = x[sx];
            float mx0 = x[sx ^ 1];
            float mn1 = y[sy];
            float mx1 = y[sy ^ 1];
            float mn2 = z[sz];
            float mx2 = z[sz ^ 1];

            // 得到最大进入距离和最小退出距离
            dEnter = mn0;
            if (mn1 > dEnter) dEnter = mn1;
            if (mn2 > dEnter) dEnter = mn2;
            dExit = mx0;
            if (mx1 < dExit) dExit = mx1;
            if (mx2 < dExit) dExit = mx2;
        }

        //------------------------------------------------------------------------
        // Ray casts
        //------------------------------------------------------------------------

        public bool isectPolygonsAny(ref List<Polygon> list, int numPolygons)
        {
            Ray ray = new Ray(ref g_orig, ref g_dest);
            while (numPolygons-- > 0)
            {
                Polygon poly = list[numPolygons];
                if (ray.intersect(ref poly))
                    return true;
            }
            return false;
        }

        public bool rayCastListAny(TempNode node, float dEnter, float dExit)
        {
            if (dEnter < 0) dEnter = 0;
            if (dExit > 1) dExit = 1;
            if (dEnter > dExit + EPS_DISTANCE) return false;

            if (node.m_splitAxis < 0)
            {
                // 叶子节点
                if (node.m_numPolygons > 0)
                {
                    if (isectPolygonsAny(ref node.m_polygons, node.m_numPolygons))
                        return true;
                }
                return false;
            }

            float d = getSplitDistance(node.m_splitPos, node.m_splitAxis);

            if (g_dirsgn[node.m_splitAxis] == 0)  // 正数
            {
                if (node.m_children[1] != null && d <= dExit + EPS_DISTANCE)
                {
                    float newEnter = dEnter;
                    if (d > newEnter)
                        newEnter = d;
                    if (rayCastListAny(node.m_children[1], newEnter, dExit))
                        return true;
                }

                if (node.m_children[0] != null && d >= dEnter - EPS_DISTANCE)
                {
                    if (d < dExit)
                        dExit = d;
                    if (rayCastListAny(node.m_children[0], dEnter, dExit))
                        return true;
                }
            }
            else
            {
                if (node.m_children[0] != null && d <= dExit + EPS_DISTANCE)
                {
                    float newEnter = dEnter;
                    if (d > newEnter)
                        newEnter = d;
                    if (rayCastListAny(node.m_children[0], newEnter, dExit))
                        return true;
                }

                if (node.m_children[1] != null && d >= dEnter - EPS_DISTANCE)
                {
                    if (d < dExit)
                        dExit = d;
                    if (rayCastListAny(node.m_children[1], dEnter, dExit))
                        return true;
                }
            }

            return false;
        }

        public bool rayCastAny(ref Ray ray)
        {
            setupRayCast(ref ray);
            float dEnter = 0, dExit = 0;
            getEnterExitDistances(ref m_aabb, ref dEnter, ref dExit);
            bool result = rayCastListAny(m_hierarchy, dEnter, dExit);
            return result;
        }

        //-------------------------- 2018.12.13

        public bool isectPolygonsAny(ref List<Polygon> list, int numPolygons, int polygonId, int prePolyId)
        {
            Ray ray = new Ray(ref g_orig, ref g_dest);
            while (numPolygons-- > 0)
            {
                Polygon poly = list[numPolygons];
                if (poly.m_id == polygonId || poly.m_id == prePolyId)
                    continue;
                if (ray.intersect(ref poly))
                    return true;
            }
            return false;
        }

        public bool rayCastListAny(TempNode node, float dEnter, float dExit, int polygonId, int prePolyId)
        {
            if (dEnter < 0) dEnter = 0;
            if (dExit > 1) dExit = 1;
            if (dEnter > dExit + EPS_DISTANCE) return false;

            if (node.m_splitAxis < 0)
            {
                // 叶子节点
                if (node.m_numPolygons > 0)
                {
                    if (isectPolygonsAny(ref node.m_polygons, node.m_numPolygons, polygonId, prePolyId))
                        return true;
                }
                return false;
            }

            float d = getSplitDistance(node.m_splitPos, node.m_splitAxis);

            if (g_dirsgn[node.m_splitAxis] == 0)  // 正数
            {
                if (node.m_children[1] != null && d <= dExit + EPS_DISTANCE)
                {
                    float newEnter = dEnter;
                    if (d > newEnter)
                        newEnter = d;
                    if (rayCastListAny(node.m_children[1], newEnter, dExit, polygonId, prePolyId))
                        return true;
                }

                if (node.m_children[0] != null && d >= dEnter - EPS_DISTANCE)
                {
                    if (d < dExit)
                        dExit = d;
                    if (rayCastListAny(node.m_children[0], dEnter, dExit, polygonId, prePolyId))
                        return true;
                }
            }
            else
            {
                if (node.m_children[0] != null && d <= dExit + EPS_DISTANCE)
                {
                    float newEnter = dEnter;
                    if (d > newEnter)
                        newEnter = d;
                    if (rayCastListAny(node.m_children[0], newEnter, dExit, polygonId, prePolyId))
                        return true;
                }

                if (node.m_children[1] != null && d >= dEnter - EPS_DISTANCE)
                {
                    if (d < dExit)
                        dExit = d;
                    if (rayCastListAny(node.m_children[1], dEnter, dExit, polygonId, prePolyId))
                        return true;
                }
            }

            return false;
        }

        public bool rayCastAny(ref Ray ray, int polygonId, int prePolyId)
        {
            setupRayCast(ref ray);
            float dEnter = 0, dExit = 0;
            getEnterExitDistances(ref m_aabb, ref dEnter, ref dExit);
            bool result = rayCastListAny(m_hierarchy, dEnter, dExit, polygonId, prePolyId);
            return result;
        }

        //-------------------------

        Vector3 g_intersectionPoint;

        public Polygon isectPolygons(ref List<Polygon> list, int numPolygons, float dEnter, float dExit)
        {
            Polygon res = null;
            float thigh = dExit + EPS_ISECT_POLYGON;
            float tlow = dEnter - EPS_ISECT_POLYGON;
            Ray ray = new Ray(ref g_orig, ref g_dest);

            while (numPolygons-- > 0)
            {
                Polygon poly = list[numPolygons];

                if (ray.intersect(ref poly))
                {
                    float t = -Vector4.dot(ref g_orig, poly.getPleq()) / Vector3.dot(ref g_dir, poly.getNormal());
                    if (t < tlow || t > thigh)
                        continue;

                    thigh = t;
                    res = poly;

                    g_intersectionPoint = g_orig + t * g_dir;
                }
            }

            return res;
        }

        public Polygon rayCastList(TempNode node, float dEnter, float dExit)
        {
            if (dEnter < 0) dEnter = 0;
            if (dExit > 1) dExit = 1;
            if (dEnter > dExit + EPS_DISTANCE) return null;

            if (node.m_splitAxis < 0)
            {
                // 叶子节点
                if (node.m_numPolygons > 0)
                {
                    Polygon poly = isectPolygons(ref node.m_polygons, node.m_numPolygons, dEnter, dEnter);
                    if (poly != null)
                        return poly;
                }
                return null;
            }

            float d = getSplitDistance(node.m_splitPos, node.m_splitAxis);

            if (g_dirsgn[node.m_splitAxis] == 0)  // 正数
            {
                if (node.m_children[1] != null && d <= dExit + EPS_DISTANCE)
                {
                    float newEnter = dEnter;
                    if (d > newEnter)
                        newEnter = d;
                    rayCastList(node.m_children[1], newEnter, dExit);
                }

                if (node.m_children[0] != null && d >= dEnter - EPS_DISTANCE)
                {
                    if (d < dExit)
                        dExit = d;
                    rayCastList(node.m_children[0], dEnter, dExit);
                }
            }
            else
            {
                if (node.m_children[0] != null && d <= dExit + EPS_DISTANCE)
                {
                    float newEnter = dEnter;
                    if (d > newEnter)
                        newEnter = d;
                    rayCastList(node.m_children[0], newEnter, dExit);
                }

                if (node.m_children[1] != null && d >= dEnter - EPS_DISTANCE)
                {
                    if (d < dExit)
                        dExit = d;
                    rayCastList(node.m_children[1], dEnter, dExit);
                }
            }
            return null;
        }

        public Polygon rayCast(ref Ray ray)
        {
            setupRayCast(ref ray);
            float dEnter = 0, dExit = 0;
            getEnterExitDistances(ref m_aabb, ref dEnter, ref dExit);
            Polygon result = rayCastList(m_hierarchy, dEnter, dExit);
            return result;
        }
        //------------------------------------------------------------------------

        //------------------------------------------------------------------------
        // Beam 碰撞检测
        //------------------------------------------------------------------------

        // 解释：http://old.cescg.org/CESCG-2002/DSykoraJJelinek/index.html
        //       https://sourceforge.net/p/gdalgorithms/mailman/message/9674912/
        // m 为 AABB 的中心
        // d 为 AABB 对角线向量的一半
        public static bool intersectAABBFrustum(ref Vector3 m,
                                                 ref Vector3 d,
                                                 ref Beam beam)
        {
            for (int i = 0; i < beam.numPleqs(); i++)
            {
                // NP 为 AABB 外接球的半径在 beam 某个面法向量上的投影
                // 平面法向量的绝对值(|a|, |b|, |c|)将该向量转换到第一象限，因此与 d 的点积总是正值
                float NP = d.x * Math.Abs(beam.getPleq(i).x) + d.y * Math.Abs(beam.getPleq(i).y) + d.z * Math.Abs(beam.getPleq(i).z);

                // MP 为球心到 beam 某个面的距离
                // 平面已经被归一化，所以分母为1
                float MP = m.x * beam.getPleq(i).x + m.y * beam.getPleq(i).y + m.z * beam.getPleq(i).z + beam.getPleq(i).w;                 

                // MP < -NP 说明当前区域位于对应平面的反向之外，即完全位于 beam 之外
                if ((MP + NP) < 0.0f)  
                    return false;
            }
            return true;
        }

        public void beamCastRecursive(TempNode node)
        {
            // beam 不会进入当前区域
            if (g_beamBeam.numPleqs() > 0 && !intersectAABBFrustum(ref g_beamMid, ref g_beamDiag, ref g_beamBeam))
                return;

            // 叶子
            if (node.m_splitAxis < 0)
            {
                for (int i = 0; i < node.m_numPolygons; i++)
                {
                    Polygon poly = node.m_polygons[i];
                    if (g_foundPolygons.Contains(poly.m_id))  // 已经保存过了
                        continue;

                    g_beamResult.Add(new Polygon(ref poly));
                    g_foundPolygons.Add(poly.m_id);
                }
                return;
            }

            // 递归
            int axis = node.m_splitAxis;
            float splitPos = node.m_splitPos;

            float om = g_beamMid[axis];
            float od = g_beamDiag[axis];

            g_beamMid[axis] = (float)0.5 * (om - od + splitPos);
            g_beamDiag[axis] = splitPos - g_beamMid[axis];
            beamCastRecursive(node.m_children[0]);

            g_beamMid[axis] = (float)0.5 * (om + od + splitPos);
            g_beamDiag[axis] = g_beamMid[axis] - splitPos;
            beamCastRecursive(node.m_children[1]);

            g_beamMid[axis] = om;
            g_beamDiag[axis] = od;
        }

        public void beamCast(ref Beam beam, ref List<Polygon> result)
        {
            // m_aabb 当前区域的包围盒
            g_beamMid = m_aabb.m_mn + m_aabb.m_mx;
            g_beamDiag = m_aabb.m_mx - m_aabb.m_mn;
            g_beamMid.opMultiplyAssign((float)0.5);  // 包围盒的中心
            g_beamDiag.opMultiplyAssign((float)0.5); // 包围盒的对角线向量的一半，与起点无关，m_aabb.mn + g_beamDiag = g_beamMid
            g_beamBeam = beam;
            g_beamResult = result;

            g_foundPolygons.Clear();
            beamCastRecursive(m_hierarchy);
        }

        public Vector3 getIntersectionPoint()
        {
            return g_intersectionPoint;
        }

    };
}
