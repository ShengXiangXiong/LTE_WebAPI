using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.SqlClient;
using GisClient;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using LTE.DB;
using System.Xml;
using System.Diagnostics;
using System.Collections;

namespace LTE.WebAPI.Models
{
    public class ScenePartModel
    {
        public bool addornot(double yes, double no, int x, int y, int kind, int k, int xmax, int ymax, short[,] a, short[,] b, double threshold)//是否符合要求
        {
            int i, j;
            for (i = x - k; i <= x + k; i++)
            {
                for (j = y - k; j <= y + k; j++)
                {
                    if (i >= 0 && i <= xmax && j >= 0 && j <= ymax && b[i, j] == 0)
                    {
                        if (a[i, j] == kind)
                        { yes++; }
                        else
                        { no++; }
                    }
                }
            }
            if (yes / (yes + no) >= threshold)
            { return true; }
            else
            { return false; }
        }
        public bool isedge(int x, int y, int xmax, int ymax, short[,] b)//是否是边界点
        {
            int num = 0;
            if (y + 1 <= ymax && b[x, y + 1] == 0)
            { num++; }
            if (x + 1 <= xmax && b[x + 1, y] == 0)
            { num++; }
            if (x - 1 >= 0 && b[x - 1, y] == 0)
            { num++; }
            if (y - 1 >= 0 && b[x, y - 1] == 0)
            { num++; }
            if (num >= 2)
            { return true; }
            else
            { return false; }
        }
        public bool isedge1(int x, int y, int xmax, int ymax, short[,] a)//建筑物扩展中是否是边界点
        {
            int num = 0;
            if (y + 1 <= ymax && a[x, y + 1] == 0)
            { num++; }
            if (x + 1 <= xmax && a[x + 1, y] == 0)
            { num++; }
            if (x - 1 >= 0 && a[x - 1, y] == 0)
            { num++; }
            if (y - 1 >= 0 && a[x, y - 1] == 0)
            { num++; }
            if (num >= 1)
            { return true; }
            else
            { return false; }
        }
        public void vic(int xseed, int yseed, int xmax, int ymax, ref short[,] a, ref short[,] b, ref short[,] tmp, ref int[] sernum, ref HashSet<int> meetcluster, int k, int area, ref int[,] c, int ilength, int jlength, double threshold)//由种子来对某一种类型进行扩展
        {
            int i, j, x, y;
            short kind;
            double yes, no;
            string s;
            int clunum = 0;
            int currentarea = 0;//当前面积
            kind = a[xseed, yseed];
            if (b[xseed, yseed] != 0)
            { return; }
            if (tmp[xseed, yseed] != 0)
            { return; }
            for (i = 1; i < 100000; i++)
            {
                if (sernum[i] != 1)
                { clunum = i; break; }
            }
            sernum[i] = 1;
            meetcluster.Add(clunum);
            Queue<string> q = new Queue<string>();
            yes = 0; no = 0;
            s = xseed.ToString() + "," + yseed.ToString();//种子入队列
            q.Enqueue(s);
            while (q.Count != 0)
            {
                s = q.Dequeue();
                string[] sArray = Regex.Split(s, ",", RegexOptions.IgnoreCase); s = "";
                x = Convert.ToInt32(sArray[0].ToString());
                y = Convert.ToInt32(sArray[1].ToString());
                if (addornot(yes, no, x, y, kind, k, xmax, ymax, a, b, threshold))
                {
                    for (i = x - k; i <= x + k; i++)
                    {
                        for (j = y - k; j <= y + k; j++)
                        {
                            if (i >= 0 && i <= xmax && j >= 0 && j <= ymax)
                            {
                                if (b[i, j] == 0)
                                {
                                    if (a[i, j] == kind)
                                    { yes++; }
                                    else { no++; }
                                    b[i, j] = kind; currentarea++;
                                }
                                else
                                {
                                    if (b[i, j] == kind && c[i, j] != 0)//属于这个类型 且不属于该簇的
                                    {
                                        if (!meetcluster.Contains(c[i, j]))
                                        { meetcluster.Add(c[i, j]); }
                                    }
                                }
                            }
                        }
                    }
                }
                string[] candidate = new string[4];//下一个扩展点的筛选,计算曼哈顿距离
                string stmp;
                int[] dis = new int[4];
                int cnum = 0, i1, j1, dtmp;
                if (x + k >= 0 && x + k <= xmax && y + k >= 0 && y + k <= ymax && isedge(x + k, y + k, xmax, ymax, b) && addornot(yes, no, x + k, y + k, kind, k, xmax, ymax, a, b, threshold))
                {
                    string s2 = (x + k).ToString() + "," + (y + k).ToString();
                    dis[cnum] = System.Math.Abs(x + k - xseed) + System.Math.Abs(y + k - yseed); candidate[cnum] = s2; cnum++;
                }
                if (x + k >= 0 && x + k <= xmax && y - k >= 0 && y - k <= ymax && isedge(x + k, y - k, xmax, ymax, b) && addornot(yes, no, x + k, y - k, kind, k, xmax, ymax, a, b, threshold))
                {
                    string s3 = (x + k).ToString() + "," + (y - k).ToString();
                    dis[cnum] = System.Math.Abs(x + k - xseed) + System.Math.Abs(y - k - yseed); candidate[cnum] = s3; cnum++;
                }
                if (x - k >= 0 && x - k <= xmax && y - k >= 0 && y - k <= ymax && isedge(x - k, y - k, xmax, ymax, b) && addornot(yes, no, x - k, y - k, kind, k, xmax, ymax, a, b, threshold))
                {
                    string s4 = (x - k).ToString() + "," + (y - k).ToString();
                    dis[cnum] = System.Math.Abs(x - k - xseed) + System.Math.Abs(y - k - yseed); candidate[cnum] = s4; cnum++;
                }
                if (x - k >= 0 && x - k <= xmax && y + k >= 0 && y + k <= ymax && isedge(x - k, y + k, xmax, ymax, b) && addornot(yes, no, x - k, y + k, kind, k, xmax, ymax, a, b, threshold))
                {
                    string s5 = (x - k).ToString() + "," + (y + k).ToString();
                    dis[cnum] = System.Math.Abs(x - k - xseed) + System.Math.Abs(y + k - yseed); candidate[cnum] = s5; cnum++;
                }
                for (i1 = 0; i1 < cnum - 1; i1++)
                {
                    for (j1 = 0; j1 < cnum - i1 - 1; j1++)
                    {
                        if (dis[j1] > dis[j1 + 1])
                        {
                            dtmp = dis[j1]; dis[j1] = dis[j1 + 1]; dis[j1 + 1] = dtmp;
                            stmp = candidate[j1]; candidate[j1] = candidate[j1 + 1]; candidate[j1 + 1] = stmp;
                        }
                    }
                }
                for (i1 = 0; i1 < cnum; i1++)
                { q.Enqueue(candidate[i1]); }
            }
            if (currentarea < area)
            {
                for (i = 0; i <= xmax; i++)
                {
                    for (j = 0; j <= ymax; j++)
                    { b[i, j] = tmp[i, j]; }
                }
                sernum[clunum] = 0;
            }
            else
            {
                for (i = 0; i <= xmax; i++)
                {
                    for (j = 0; j <= ymax; j++)
                    { tmp[i, j] = b[i, j]; }
                }
                for (i = 0; i <= xmax; i++)
                {
                    for (j = 0; j <= ymax; j++)
                    {
                        if (tmp[i, j] == kind && c[i, j] == 0)
                        { c[i, j] = clunum; }
                    }
                }
                foreach (var n in meetcluster)//修正簇编号
                {
                    if (n < clunum)
                    { clunum = n; }
                }
                foreach (var n in meetcluster)
                {
                    for (i = 0; i <= xmax; i++)
                    {
                        for (j = 0; j <= ymax; j++)
                        {
                            if (c[i, j] == n)
                            { c[i, j] = clunum; }
                        }
                    }
                }
                foreach (var n in meetcluster)//回收簇编号
                {
                    if (n != clunum)
                    { sernum[n] = 0; }
                }
            }
        }
        public Result part()
        {
            DataTable dt11 = DB.IbatisHelper.ExecuteQueryForDataTable("GetBuilding_overlayState", null);  // Ibatis 数据访问，判断用户是否做了叠加分析
            if (dt11.Rows[0][0].ToString() == "0")
            { return new Result(false, "用户未进行建筑物叠加分析"); }
            DataTable dt12 = DB.IbatisHelper.ExecuteQueryForDataTable("GetWater_overlayState", null);  // Ibatis 数据访问，判断用户是否做了叠加分析
            if (dt12.Rows[0][0].ToString() == "0")
            { return new Result(false, "用户未进行水面叠加分析"); }
            DataTable dt13 = DB.IbatisHelper.ExecuteQueryForDataTable("GetGrass_overlayState", null);  // Ibatis 数据访问，判断用户是否做了叠加分析
            if (dt13.Rows[0][0].ToString() == "0")
            { return new Result(false, "用户未进行草地叠加分析"); }

            DataTable dt23 = DB.IbatisHelper.ExecuteQueryForDataTable("GetClustertoDBState", null);  // Ibatis 数据访问，判断用户是否做了聚类分析，做了则删除它
            if (dt23.Rows[0][0].ToString() == "1")//做了聚类分析
            {
                try//更新加速场景表，前提条件表
                {
                    IbatisHelper.ExecuteDelete("UpdatetbDependTableDuetoClustertoDB", null);
                    IbatisHelper.ExecuteUpdate("UpdatetbAccelerateGridSceneDuetoClustertoDB", null);
                }
                catch (Exception ex)
                { return new Result(false, ex.ToString()); }
            }

            try
            {
                string xmlpath = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "cluster.xml";//xml文件位置
                //读xml文件获取参数
                XDocument document = XDocument.Load(xmlpath);
                XElement root = document.Root;
                XElement ele = root.Element("parameter");
                XElement para = ele.Element("range");
                int range = Convert.ToInt32(para.Value.ToString());//每次栅格扩展的个数,为了避免扩张时出现空隙，range为奇数
                para = ele.Element("threshold");
                double threshold = Convert.ToDouble(para.Value.ToString());//簇的密度阈值
                para = ele.Element("area");
                int area = Convert.ToInt32(para.Value.ToString());//簇最小的栅格数量
                para = ele.Element("building_threshold");
                double building_threshold = Convert.ToDouble(para.Value.ToString());//建筑物栅格密度阈值
                para = ele.Element("buildingseed_threshold");
                double buildingseed_threshold = Convert.ToDouble(para.Value.ToString());//建筑物扩展种子栅格密度阈值
                para = ele.Element("buildingheight_threshold");
                double buildingheight_threshold = Convert.ToDouble(para.Value.ToString());//建筑物栅格向外扩展高度阈值
                para = ele.Element("water_threshold");
                double water_threshold = Convert.ToDouble(para.Value.ToString());//水面栅格密度阈值
                para = ele.Element("waterseed_threshold");
                double waterseed_threshold = Convert.ToDouble(para.Value.ToString());//水面扩展种子栅格密度阈值
                para = ele.Element("grass_threshold");
                double grass_threshold = Convert.ToDouble(para.Value.ToString());//草地栅格密度阈值
                para = ele.Element("grassseed_threshold");
                double grassseed_threshold = Convert.ToDouble(para.Value.ToString());//草地扩展种子栅格密度阈值

                DataTable dt1;
                try
                {
                    dt1 = DB.IbatisHelper.ExecuteQueryForDataTable("GetFishnetRange", null);  // Ibatis 数据访问，得到目标区域范围

                }
                catch (Exception ex11)
                {
                    return new Result(false, "11111" + ex11.ToString());
                }

                string minx_text = dt1.Rows[0][0].ToString(),
                          miny_text = dt1.Rows[0][1].ToString(),
                          maxx_text = dt1.Rows[0][2].ToString(),
                          maxy_text = dt1.Rows[0][3].ToString(),
                          gridsize_text = dt1.Rows[0][4].ToString();
                double minx = double.Parse(minx_text);//最大最小大地坐标
                double miny = double.Parse(miny_text);
                double maxx = double.Parse(maxx_text);
                double maxy = double.Parse(maxy_text);
                double cellsize = double.Parse(gridsize_text);//栅格边长


                int ilength = (int)((maxy - miny) / cellsize), jlength = (int)((maxx - minx) / cellsize);
                int xmax = ilength - 1, ymax = jlength - 1;//xmax是i的上界,ymax是j的上界

                short[,] a = new short[ilength, jlength];//目标区域矩阵
                short[,] b = new short[ilength, jlength];//结果数据
                short[,] tmp = new short[ilength, jlength];//临时存放矩阵
                int[,] c = new int[ilength, jlength];//序号矩阵
                int[,] bseed = new int[900000, 2];//建筑物种子
                int[,] wseed = new int[900000, 2];//水面种子
                int[,] gseed = new int[900000, 2];//草地种子
                int k = range / 2;//栅格范围
                int[] sernum = new int[100000];//序号的有效性，0可用，1不可用，从1开始
                HashSet<int> meetcluster = new HashSet<int>();
                int[] x = new int[4] { 0, 0, 1, -1 };//x的增量,用于小面积去除
                int[] y = new int[4] { 1, -1, 0, 0 };//y的增量,用于小面积去除

                int i, j, num, seednum = 0, seednum1 = 0, seednum2 = 0;
                double ratio;
                for (i = 0; i <= xmax; i++)
                {
                    for (j = 0; j <= ymax; j++)
                    { a[i, j] = 0; b[i, j] = 0; tmp[i, j] = 0; c[i, j] = 0; }
                }
                for (i = 0; i < 100000; i++)
                { sernum[i] = 0; }

                int pre, next;//a[pre,next]
                DataTable dt2 = DB.IbatisHelper.ExecuteQueryForDataTable("GetBuildingRatio", null);  // Ibatis 数据访问,找出建筑物栅格放入数组a
                for (i = 0; i < dt2.Rows.Count; i++)
                {
                    pre = Convert.ToInt32(dt2.Rows[i][1].ToString());
                    next = Convert.ToInt32(dt2.Rows[i][0].ToString());
                    ratio = Convert.ToDouble(dt2.Rows[i][2].ToString());
                    if (ratio > building_threshold)//建筑物栅格密度阈值
                    { a[pre, next] = 1; }
                    if (seednum == 0 && ratio > buildingseed_threshold)//建筑扩展种子栅格密度阈值
                    { bseed[seednum, 0] = pre; bseed[seednum, 1] = next; seednum++; }
                    if (ratio > buildingseed_threshold && seednum < 900000 && seednum != 0)//控制数量
                    {
                        if (!(pre >= bseed[seednum - 1, 0] - 5 && pre <= bseed[seednum - 1, 0] + 5 &&
                              next >= bseed[seednum - 1, 1] - 5 && next <= bseed[seednum - 1, 1] + 5))
                        { bseed[seednum, 0] = pre; bseed[seednum, 1] = next; seednum++; }
                    }
                }

                Hashtable ht = new Hashtable();//建筑物扩展
                ht["height"] = buildingheight_threshold;
                ht["minGXID"] = minx;
                ht["maxGXID"] = maxx;
                ht["minGYID"] = miny;
                ht["maxGYID"] = maxy;
                DataTable dt3 = DB.IbatisHelper.ExecuteQueryForDataTable("GetBuildingHeight", ht);  // Ibatis 数据访问

                string s;
                int error = 0;
                HashSet<string> h = new HashSet<string>();
                Queue<string> q = new Queue<string>();
                for (i = 0; i < dt3.Rows.Count; i++)
                {
                    next = Convert.ToInt32((Convert.ToDouble(dt3.Rows[i][1].ToString()) - minx) / cellsize);
                    pre = Convert.ToInt32((Convert.ToDouble(dt3.Rows[i][2].ToString()) - miny) / cellsize);
                    s = pre.ToString() + "," + next.ToString();
                    q.Enqueue(s);
                    int sem = 1;
                    while (q.Count != 0)
                    {
                        s = q.Dequeue(); sem--;
                        string[] sArray = Regex.Split(s, ",", RegexOptions.IgnoreCase);
                        pre = Convert.ToInt32(sArray[0].ToString());
                        next = Convert.ToInt32(sArray[1].ToString());
                        if (!h.Contains(s) && isedge1(pre, next, xmax, ymax, a) && a[pre, next] == 1)
                        { h.Add(s); }
                        if (a[pre, next] == 1)//上下8个点,x上界是j的上界ymax
                        {
                            s = (pre - 1).ToString() + "," + (next + 1).ToString();
                            if (pre - 1 >= 0 && next + 1 <= ymax && a[pre - 1, next + 1] == 1 && isedge1(pre - 1, next + 1, xmax, ymax, a) && !h.Contains(s))//不越界+是建筑物+是边界点+不在hashset中
                            { q.Enqueue(s); sem++; }
                            s = (pre).ToString() + "," + (next + 1).ToString();
                            if (next + 1 <= ymax && a[pre, next + 1] == 1 && isedge1(pre, next + 1, xmax, ymax, a) && !h.Contains(s))
                            { q.Enqueue(s); sem++; }
                            s = (pre + 1).ToString() + "," + (next + 1).ToString();
                            if (pre + 1 <= xmax && next + 1 <= ymax && a[pre + 1, next + 1] == 1 && isedge1(pre + 1, next + 1, xmax, ymax, a) && !h.Contains(s))
                            { q.Enqueue(s); sem++; }
                            s = (pre + 1).ToString() + "," + (next).ToString();
                            if (pre + 1 <= xmax && a[pre + 1, next] == 1 && isedge1(pre + 1, next, xmax, ymax, a) && !h.Contains(s))
                            { q.Enqueue(s); sem++; }
                            s = (pre + 1).ToString() + "," + (next - 1).ToString();
                            if (pre + 1 <= xmax && next - 1 >= 0 && a[pre + 1, next - 1] == 1 && isedge1(pre + 1, next - 1, xmax, ymax, a) && !h.Contains(s))
                            { q.Enqueue(s); sem++; }
                            s = (pre).ToString() + "," + (next - 1).ToString();
                            if (next - 1 >= 0 && a[pre, next - 1] == 1 && isedge1(pre, next - 1, xmax, ymax, a) && !h.Contains(s))
                            { q.Enqueue(s); sem++; }
                            s = (pre - 1).ToString() + "," + (next - 1).ToString();
                            if (pre - 1 >= 0 && next - 1 >= 0 && a[pre - 1, next - 1] == 1 && isedge1(pre - 1, next - 1, xmax, ymax, a) && !h.Contains(s))
                            { q.Enqueue(s); sem++; }
                            s = (pre - 1).ToString() + "," + (next).ToString();
                            if (pre - 1 >= 0 && a[pre - 1, next] == 1 && isedge1(pre - 1, next, xmax, ymax, a) && !h.Contains(s))
                            { q.Enqueue(s); sem++; }
                        }
                        else
                        {
                            error++;
                        }
                        if (q.Count > 100)
                        { q.Clear(); break; }
                    }
                    foreach (var n in h)
                    {

                        string[] sArray = Regex.Split(n, ",", RegexOptions.IgnoreCase);
                        pre = Convert.ToInt32(sArray[0].ToString());
                        next = Convert.ToInt32(sArray[1].ToString());
                        for (int i1 = Math.Max(0, pre - 1); i1 <= Math.Min(xmax, pre + 1); i1++)
                        {
                            for (int j1 = Math.Max(0, next - 1); j1 <= Math.Min(ymax, next + 1); j1++)
                            { a[i1, j1] = 1; }
                        }
                    }
                    h.Clear();
                }

                DataTable dt4 = DB.IbatisHelper.ExecuteQueryForDataTable("GetWaterRatio", null);  // Ibatis 数据访问,找出水面栅格放入数组a
                for (i = 0; i < dt4.Rows.Count; i++)
                {
                    pre = Convert.ToInt32(dt4.Rows[i][1].ToString());
                    next = Convert.ToInt32(dt4.Rows[i][0].ToString());
                    ratio = Convert.ToDouble(dt4.Rows[i][2].ToString());
                    if (ratio > water_threshold)//水面栅格密度阈值
                    { a[pre, next] = 2; }
                    if (ratio > waterseed_threshold && seednum1 < 900000)//水面扩展种子栅格密度阈值,且
                    { wseed[seednum1, 0] = pre; wseed[seednum1, 1] = next; seednum1++; }
                }

                DataTable dt5 = DB.IbatisHelper.ExecuteQueryForDataTable("GetGrassRatio", null);  // Ibatis 数据访问,找出草地栅格放入数组a
                for (i = 0; i < dt5.Rows.Count; i++)
                {
                    pre = Convert.ToInt32(dt5.Rows[i][1].ToString());
                    next = Convert.ToInt32(dt5.Rows[i][0].ToString());
                    ratio = Convert.ToDouble(dt5.Rows[i][2].ToString());
                    if (ratio > grass_threshold)//草地栅格密度阈值
                    { a[pre, next] = 3; }
                    if (ratio > grassseed_threshold && seednum2 < 900000)//草地扩展种子栅格密度阈值
                    { gseed[seednum2, 0] = pre; gseed[seednum2, 1] = next; seednum2++; }
                }

                //3种地物的聚类
                for (i = 0; i < seednum; i++)
                {

                    meetcluster.Clear();
                    vic(bseed[i, 0], bseed[i, 1], xmax, ymax, ref a, ref b, ref tmp, ref sernum, ref meetcluster, k, area, ref c, ilength, jlength, threshold);
                }

                for (i = 0; i < seednum1; i++)
                {
                    meetcluster.Clear();
                    vic(wseed[i, 0], wseed[i, 1], xmax, ymax, ref a, ref b, ref tmp, ref sernum, ref meetcluster, k, area, ref c, ilength, jlength, threshold);
                }
                for (i = 0; i < seednum2; i++)
                {

                    meetcluster.Clear();
                    vic(gseed[i, 0], gseed[i, 1], xmax, ymax, ref a, ref b, ref tmp, ref sernum, ref meetcluster, k, area, ref c, ilength, jlength, threshold);
                }

                int jjj = 0;//已经标记序号的格子数
                meetcluster.Clear();//已经分配的簇号
                int jj1 = 0, jj2 = 0;//jj1：聚类为空地的格子数，jj2：最大簇号
                for (i = 0; i <= xmax; i++)
                {
                    for (j = 0; j <= ymax; j++)
                    {
                        if (c[i, j] != 0)
                        { jjj++; }
                        if (tmp[i, j] != 0)
                        { jj1++; }
                        if (c[i, j] > jj2)
                        { jj2 = c[i, j]; }
                        if (c[i, j] != 0 && !meetcluster.Contains(c[i, j]))
                        { meetcluster.Add(c[i, j]); }
                    }
                }
                int nextclunum = jj2 + 1;

                short[,] vis = new short[ilength, jlength];//标记是否访问过
                short[,] vis1 = new short[ilength, jlength];
                int gnum = 0;//空地个数
                short firstmeetscene = 0;
                int firstmeetid = 0, firstmeetsem = 0;
                for (i = 0; i <= xmax; i++)
                {
                    for (j = 0; j <= ymax; j++)
                    { vis[i, j] = 0; vis1[i, j] = 0; }
                }
                for (i = 0; i <= xmax; i++)
                {
                    for (j = 0; j <= ymax; j++)
                    {
                        if (c[i, j] == 0 && tmp[i, j] == 0)
                        {
                            firstmeetsem = 0;
                            s = ""; gnum = 1;
                            int count = 0;
                            int i1, j1;
                            Queue<string> q1 = new Queue<string>();
                            s = i.ToString() + "," + j.ToString();
                            q1.Enqueue(s); count++;
                            vis1[i, j] = 1;
                            while (q1.Count != 0)
                            {
                                s = q1.Dequeue(); count--;
                                string[] sArray = Regex.Split(s, ",", RegexOptions.IgnoreCase);
                                i1 = Convert.ToInt32(sArray[0].ToString());
                                j1 = Convert.ToInt32(sArray[1].ToString());
                                //    c[i1, j1] = nextclunum;
                                int newx, newy;
                                for (int i11 = 0; i11 < 4; i11++) //该位置的相邻的4个元素
                                {
                                    newx = i1 + x[i11];
                                    newy = j1 + y[i11];
                                    if (newx < 0 || newx > xmax || newy < 0 || newy > ymax) continue; //越界的坐标，直接跳过
                                    if (firstmeetsem == 0 && vis1[newx, newy] == 0 && c[newx, newy] != 0)//碰到的第一个簇
                                    {
                                        firstmeetscene = tmp[newx, newy];
                                        firstmeetid = c[newx, newy];
                                        firstmeetsem = 1;
                                    }
                                    if (vis1[newx, newy] == 0 && c[newx, newy] == 0) //没有被访问且该元素值为1
                                    {
                                        s = newx.ToString() + "," + newy.ToString();
                                        q1.Enqueue(s); count++;
                                        vis1[newx, newy] = 1; gnum++;
                                    }

                                }
                                if (gnum >= area)
                                { break; }
                            }
                            if (gnum >= area)
                            {
                                s = "";
                                int gcount = 0;
                                int gi1, gj1;
                                Queue<string> gq1 = new Queue<string>();
                                s = i.ToString() + "," + j.ToString();
                                gq1.Enqueue(s); gcount++;
                                vis[i, j] = 1;
                                while (gq1.Count != 0)
                                {
                                    s = gq1.Dequeue(); gcount--;
                                    string[] sArray = Regex.Split(s, ",", RegexOptions.IgnoreCase);
                                    gi1 = Convert.ToInt32(sArray[0].ToString());
                                    gj1 = Convert.ToInt32(sArray[1].ToString());
                                    c[gi1, gj1] = nextclunum;
                                    int newx, newy;
                                    for (int i11 = 0; i11 < 4; i11++) //该位置的相邻的4个元素
                                    {
                                        newx = gi1 + x[i11];
                                        newy = gj1 + y[i11];
                                        if (newx < 0 || newx > xmax || newy < 0 || newy > ymax) continue; //越界的坐标，直接跳过
                                        if (vis[newx, newy] == 0 && c[newx, newy] == 0) //没有被访问且该元素值为1
                                        {
                                            s = newx.ToString() + "," + newy.ToString();
                                            gq1.Enqueue(s); gcount++;
                                            vis[newx, newy] = 1;
                                        }
                                    }
                                }
                                nextclunum++;
                            }
                            else
                            {
                                s = "";
                                int pcount = 0;
                                int pi1, pj1;
                                Queue<string> pq1 = new Queue<string>();
                                s = i.ToString() + "," + j.ToString();
                                pq1.Enqueue(s); pcount++;
                                vis[i, j] = 1;
                                while (pq1.Count != 0)
                                {
                                    s = pq1.Dequeue(); pcount--;
                                    string[] sArray = Regex.Split(s, ",", RegexOptions.IgnoreCase);
                                    pi1 = Convert.ToInt32(sArray[0].ToString());
                                    pj1 = Convert.ToInt32(sArray[1].ToString());
                                    c[pi1, pj1] = firstmeetid;
                                    tmp[pi1, pj1] = firstmeetscene;
                                    int newx, newy;
                                    for (int i11 = 0; i11 < 4; i11++) //该位置的相邻的4个元素
                                    {
                                        newx = pi1 + x[i11];
                                        newy = pj1 + y[i11];
                                        if (newx < 0 || newx > xmax || newy < 0 || newy > ymax) continue; //越界的坐标，直接跳过
                                        if (vis[newx, newy] == 0 && c[newx, newy] == 0) //没有被访问且该元素值为1
                                        {
                                            s = newx.ToString() + "," + newy.ToString();
                                            pq1.Enqueue(s); pcount++;
                                            vis[newx, newy] = 1;
                                        }
                                    }

                                }
                            }

                        }

                    }
                }
                
                //制作字典
                int rownumber = xmax + 1, columnnumber = ymax + 1;
                Dictionary<int, int> myDictionary = new Dictionary<int, int>();
                for (i = 0; i < rownumber; i++)
                {
                    for (j = 0; j < columnnumber; j++)
                    {
                        int gridID = i * columnnumber + j;
                        myDictionary.Add(gridID, tmp[i, j]);
                    }
                }

                try//批量更新
                {
                    DataTable dt = new DataTable();
                    dt.Columns.Add("x", Type.GetType("System.Int32"));
                    dt.Columns.Add("y", Type.GetType("System.Int32"));
                    dt.Columns.Add("scene", Type.GetType("System.Byte"));
                    dt.Columns.Add("clusterid", Type.GetType("System.Int32"));
                    foreach (var item in myDictionary.Keys)
                    {
                        dt.Rows.Add(new object[] { (item % (ymax + 1)).ToString(), (item / (ymax + 1)).ToString(), myDictionary[item].ToString(), c[item / columnnumber, item % columnnumber].ToString() });
                        if (dt.Rows.Count > 5000)
                        {
                            using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                            {
                                bcp.BatchSize = dt.Rows.Count;
                                bcp.BulkCopyTimeout = 1000;
                                bcp.DestinationTableName = "tbAccelerateGridSceneTmpCluster";
                                bcp.WriteToServer(dt);
                                bcp.Close();
                            }
                            dt.Clear();
                        }
                    }
                    using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                    {
                        bcp.BatchSize = dt.Rows.Count;
                        bcp.BulkCopyTimeout = 1000;
                        bcp.DestinationTableName = "tbAccelerateGridSceneTmpCluster";
                        bcp.WriteToServer(dt);
                        bcp.Close();
                    }
                    dt.Clear();
                    IbatisHelper.ExecuteUpdate("UpdatetbAccelerateGridClusterByTmp", null);
                    IbatisHelper.ExecuteDelete("DeletetbAccelerateGridSceneTmpCluster", null);
                }
                catch (Exception ex2)
                {
                    return new Result(false, ex2.ToString());
                }


                //更新tbDependTabled的ClustertoDB
                IbatisHelper.ExecuteUpdate("UpdatetbDependTableClustertoDB", null);
                return new Result(true, "场景划分完成");
            }
            catch (Exception ex)
            {
                return new Result(false, ex.ToString());
            }
        }
        //之前的场景划分
        /*
        // 对均匀栅格进行粗略的场景划分，后续应采用张明明的改进方式：通过聚类的方式对场景进行自动划分
        // 主要用于多场景系数校正
        public static Result part()
        {
            #region 之前的场景划分
                //DataTable tb = LTE.DB.IbatisHelper.ExecuteQueryForDataTable("getRays", null);

                //for (int i = 0; i < tb.Rows.Count; i++)
                //{
                //    // 三个场景：y < 3545765，3545765 < y < 3547435，y > 3547435
                //    double y = Convert.ToDouble(tb.Rows[i]["rayStartPointY"]);
                //    if (y < 3545765)
                //        tb.Rows[i]["startPointScen"] = 0;
                //    else if (y > 3545765 && y < 3547435)
                //        tb.Rows[i]["startPointScen"] = 1;
                //    else
                //        tb.Rows[i]["startPointScen"] = 2;

                //    double y1 = Convert.ToDouble(tb.Rows[i]["rayEndPointY"]);
                //    if (y1 < 3545765)
                //        tb.Rows[i]["endPointScen"] = 0;
                //    else if (y1 > 3545765 && y1 < 3547435)
                //        tb.Rows[i]["endPointScen"] = 1;
                //    else
                //        tb.Rows[i]["endPointScen"] = 2;

                //    // 计算穿过场景的比例
                //    double scene1 = 3545765, sceneLen2 = 3547435 - 3545765, scene3 = 3547435;
                //    double yMin = 0, yMax = 0;
                //    if (y < y1)
                //    {
                //        yMin = y;
                //        yMax = y1;
                //    }
                //    else
                //    {
                //        yMin = y1;
                //        yMax = y;
                //    }
                //    double yLen = yMax - yMin;
                //    double proportion1 = 0, proportion2 = 0, proportion3 = 0;
                //    if (yMin < scene1 && yMax > scene3)  // 覆盖3个场景
                //    {
                //        double yLen1 = scene1 - yMin;
                //        double yLen3 = yMax - scene3;
                //        proportion1 = yLen1 / yLen;
                //        proportion2 = sceneLen2 / yLen;
                //        proportion3 = yLen3 / yLen;
                //    }
                //    else if (yMin < scene1 && yMax > scene1 && yMax <= scene3) // 覆盖前2个场景
                //    {
                //        double yLen1 = scene1 - yMin;
                //        double yLen2 = yMax - scene1;
                //        proportion1 = yLen1 / yLen;
                //        proportion2 = yLen2 / yLen;
                //    }
                //    else if (yMin >= scene1 && yMin < scene3 && yMax > scene3) // 覆盖后2个场景
                //    {
                //        double xLen2 = scene3 - yMin;
                //        double xLen3 = yMax - scene3;
                //        proportion2 = xLen2 / yLen;
                //        proportion3 = xLen3 / yLen;
                //    }
                //    else if (yMin < scene1)  // 只覆盖第1个场景
                //    {
                //        proportion1 = 1;
                //    }
                //    else if (yMin >= scene1 && yMax <= scene3)
                //    {
                //        proportion2 = 1;
                //    }
                //    else if (yMax > scene3)
                //    {
                //        proportion3 = 1;
                //    }
                //    tb.Rows[i]["proportion"] = string.Format("{0:N3};{1:N3};{2:N3}", proportion1, proportion2, proportion3);
                //}

                //LTE.DB.IbatisHelper.ExecuteDelete("DeleteRays", null);

                //using (System.Data.SqlClient.SqlBulkCopy bcp = new System.Data.SqlClient.SqlBulkCopy(LTE.DB.DataUtil.ConnectionString))
                //{
                //    bcp.BatchSize = tb.Rows.Count;
                //    bcp.BulkCopyTimeout = 1000;
                //    bcp.DestinationTableName = "tbRayAdj";
                //    bcp.WriteToServer(tb);
                //    bcp.Close();

                //}
                //tb.Clear();
                #endregion

            int maxGxid = 0, maxGyid = 0;
            InternalInterference.Grid.GridHelper.getInstance().getMaxAccGridXY(ref maxGxid, ref maxGyid);

            DataTable tb = new DataTable();
            tb = DB.IbatisHelper.ExecuteQueryForDataTable("getAGridZ", null);
            if(tb.Rows.Count < 1)
            {
                return new Result(false, "无均匀栅格！");
            }

            int minGzid = Convert.ToInt32(tb.Rows[0][0]);
            int maxGzid = Convert.ToInt32(tb.Rows[0][1]);

            try
            {
                DB.IbatisHelper.ExecuteDelete("DeleteAccrelateGridScene", null);
            }
            catch(Exception e)
            {
                return new Result(false, e.ToString());
            }

            System.Data.DataTable dtable = new System.Data.DataTable();
            dtable.Columns.Add("GXID");
            dtable.Columns.Add("GYID");
            dtable.Columns.Add("GZID");
            dtable.Columns.Add("Scene");

            double oX = 0, oY = 0;
            InternalInterference.Grid.GridHelper.getInstance().getOriginXY(ref oX, ref oY);
            double len = InternalInterference.Grid.GridHelper.getInstance().getAGridSize();

            // 三个场景：y < 3545765，3545765 < y < 3547435，y > 3547435
            int scen1 = (int)((3545765 - oY) / len);
            int scen2 = (int)((3547435 - oY) / len);

            for (int x = 0; x <= maxGxid; x++)
            {
                for (int y = 0; y <= maxGyid; y++)
                {
                    for (int z = minGzid; z <= maxGzid; z++)
                    {
                        System.Data.DataRow thisrow = dtable.NewRow();
                        thisrow["GXID"] = x;
                        thisrow["GYID"] = y;
                        thisrow["GZID"] = z;

                        if (y < scen1)
                            thisrow["Scene"] = (byte)0;
                        else if (y < scen2)
                            thisrow["Scene"] = (byte)1;
                        else
                            thisrow["Scene"] = (byte)2;

                        dtable.Rows.Add(thisrow);
                    }
                }

                if (dtable.Rows.Count > 50000)
                {
                    using (System.Data.SqlClient.SqlBulkCopy bcp = new System.Data.SqlClient.SqlBulkCopy(DB.DataUtil.ConnectionString))
                    {
                        bcp.BatchSize = dtable.Rows.Count;
                        bcp.BulkCopyTimeout = 1000;
                        bcp.DestinationTableName = "tbAccelerateGridScene";
                        bcp.WriteToServer(dtable);
                        bcp.Close();
                    }
                    dtable.Clear();
                }
            }

            // 最后一批
            if (dtable.Rows.Count > 0)
            {
                using (System.Data.SqlClient.SqlBulkCopy bcp = new System.Data.SqlClient.SqlBulkCopy(DB.DataUtil.ConnectionString))
                {
                    bcp.BatchSize = dtable.Rows.Count;
                    bcp.BulkCopyTimeout = 1000;
                    bcp.DestinationTableName = "tbAccelerateGridScene";
                    bcp.WriteToServer(dtable);
                    bcp.Close();
                }
                dtable.Clear();
            }

            return new Result(true);
        }
        */
    }
}