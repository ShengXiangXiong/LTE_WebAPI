using LTE.DB;
using LTE.Geometric;
using LTE.InternalInterference;
using LTE.InternalInterference.Grid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using LTE.ExternalInterference.Struct;
namespace LTE.ExternalInterference
{
    public class PathAnalysis
    {
        private Dictionary<string, List<GridInfo>> togrid;
        private string virname;

        private Dictionary<string, double> grid_pwr;//栅格接收功率
        private Dictionary<string, double> grid_pathloss;//栅格平均损耗
        /// <summary>
        /// 存储栅格-路径起点CI信息
        /// </summary>
        private Dictionary<string, HashSet<String>> grid_from;
        private const int ggridsize = 30;//射线映射栅格设置的栅格长宽大小
        private const int ggridVsize = 3;

        private DataTable tbsourceInfo;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">指定当前测试的干扰源点</param>
        public PathAnalysis(string name)
        {
            this.virname = name;
            //pathList = new List<PathInfo>();//数据库tbrayLoc数据
            togrid = new Dictionary<string, List<GridInfo>>();//映射的栅格数据

            grid_pwr = new Dictionary<string, double>();
            grid_pathloss = new Dictionary<string, double>();
            grid_from = new Dictionary<string, HashSet<string>>();
            Hashtable ht = new Hashtable();
            ht["fromName"] = this.virname;
            tbsourceInfo = IbatisHelper.ExecuteQueryForDataTable("GetSelectedPoint", ht);
        }


        public ResultRecord StartAnalysis(double ratioAP, double ratioP, double ratioAPW)
        {
            Clear();
            DateTime t1, t2, t3, t4;
            t1 = DateTime.Now;

            //DataHandler();
            List<PathInfo> paths = this.GetPathByD();

            if (paths == null || paths.Count == 0)
            {
                Debug.WriteLine("tbRayLoc表中没有数据");
                return new ResultRecord(false,"","",0, "没有路径数据");
            }

            //Debug.WriteLine(paths.Count);
            t2 = DateTime.Now;
            //映射栅格
            int mod = 10000;
            int alllen = paths.Count;

            for (int i = 0; i < alllen; i += mod)
            {
                try
                {
                    if (i + mod > alllen)
                    {
                        this.NewUpdatPathToGridXY(paths, i, alllen);
                    }
                    else
                    {
                        this.NewUpdatPathToGridXY(paths, i, i + mod);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("已执行：" + i + "   发生错误：" + e.Message);
                }
                //GC.Collect();

            }

            //this.PathToGrid3D();
            t3 = DateTime.Now;

            Debug.WriteLine("y原始栅格大小" + this.togrid.Count);
            FiltrateGrid();//过滤不合理的togrid栅格
            Debug.WriteLine("删除发射源周围栅格后栅格大小 " + this.togrid.Count);

            this.GetGrid_From(togrid);

            // this.BalanceFiltPara(1000, 500, 80, 0.8, 0.80, 200);

            BalanceFiltParaS(500, 5, 0.7, 0.9, 200);
            SortByPalthloss();
            AverageGridPathloss(600);
            this.MergeGridPwr(this.togrid);

            CompareWithAim();
            Debug.WriteLine("过滤发射源周围栅格后栅格大小 " + this.togrid.Count);
            string aimGrid = EvaluateCombineAll(togrid, ratioAP, ratioP, ratioAPW);
            //string aimGrid = EvaluateCombineALL(togrid, ratioAP, ratioP, ratioAPW);//得到当前占比最大的栅格
            if (aimGrid == null || aimGrid == "")
            {
                Debug.WriteLine("未找到合适位置");
                return new ResultRecord(false, "", "", 0, "未找到合适位置"); 
            }
            //TestPre();
            Debug.WriteLine("AimGrid: " + aimGrid);
            
            //presentRacing(togrid[aimGrid], 1000);
            // Debug.WriteLine("");
            //Clear();
            return showResult(aimGrid);
        }

        private ResultRecord showResult(string ans)
        {
            Hashtable ht = new Hashtable();
            ht["cellname"] = this.virname;
            DataTable source = IbatisHelper.ExecuteQueryForDataTable("GetVirSourceAllInfo", ht);
            Geometric.Point point;
            if (source.Rows.Count < 1)
            {
                Debug.WriteLine("tbRealSource中无数据");
                return new ResultRecord(false, "", "", 0, "不存在指定的干扰源信息");
            }
            else
            {
                String[] tmp = ans.Split(',');

                int gx = 0, gy = 0, gz = 0;
                try
                {
                    gx = Convert.ToInt32(tmp[0]); gy = Convert.ToInt32(tmp[1]); gz = Convert.ToInt32(tmp[2]);
                }
                catch
                {
                    Debug.WriteLine(ans);
                }
                Geometric.Point p = new Geometric.Point();
                Grid3D tmp1 = new Grid3D(gx, gy, gz);
                GridHelper.getInstance().PointGridXYZ(ref p, tmp1, ggridsize, ggridVsize);

                Console.WriteLine("定位结果坐标：" + p.X + ",  " + p.Y + ",   " + p.Z);
                Console.WriteLine("定位结果栅格：" + gx + ",  " + gy + ",   " + gz);
                point = new Geometric.Point(Convert.ToDouble(source.Rows[0]["x"]), Convert.ToDouble(source.Rows[0]["y"]), Convert.ToDouble(source.Rows[0]["z"]));
                Grid3D tmp2 = new Grid3D();
                GridHelper.getInstance().PointXYZGrid(point, ref tmp2, ggridsize, ggridVsize);
                Console.WriteLine("干扰源点坐标：" + point.X + ",  " + point.Y + ",   " + point.Z);
                Debug.WriteLine(" 干扰源点栅格：" + tmp2.gxid + ",  " + tmp2.gyid + ",  " + tmp2.gzid);
                point.Z = 0;
                p.Z = 0;
                double dis = Geometric.Point.distance(point, p);
                Debug.WriteLine(" 定位精度：" + dis);
                return new ResultRecord(true, string.Format("{0},{1},{2}", p.X, p.Y, p.Z), string.Format("{0},{1},{2}", point.X, point.Y, point.Z), dis, "成功定位");
                //presentGrid(tmp1);

            }
        }

        /*-----------------------------------------------获取原始路径数据--------------------------------------------------------------------*/
        public List<PathInfo> GetPathByD()
        {
            DateTime t1, t2, t3;
            t1 = DateTime.Now;
            List<PathInfo> raypath = new List<PathInfo>();
            Hashtable ht = new Hashtable();
            ht["fromName"] = this.virname;
            DataTable pathTemp = IbatisHelper.ExecuteQueryForDataTable("GetRayLoc", ht);
            if (pathTemp.Rows.Count < 2)
            {
                return raypath;
            }
            t2 = DateTime.Now;

            Debug.WriteLine(pathTemp.Rows.Count);
            double nata = 300.0 / (1805 + 0.2 * (893 - 511));

            double reflectedR = 1;//反射系数
            double diffrctedR = 1;//绕射系数

            //获取tbsource 数据,存到字典 cellPwr 中

            //DataTable tbsourcePwr = IbatisHelper.ExecuteQueryForDataTable("GetSelectedPoint", ht);
            Dictionary<string, double> cellPwr = new Dictionary<string, double>();
            foreach (DataRow tb in tbsourceInfo.Rows)
            {
                string cellid = Convert.ToString(tb["CI"]);
                double pwr = Convert.ToDouble(tb["ReceivePW"]);//单位是w
                cellPwr.Add(cellid, pwr);
            }

            //用于记录反推接收功率
            double distance = 0;
            double receivePwr = 0;
            double emit = 0;
            int length = pathTemp.Rows.Count;
            int trajID;//路径标识
            short rayLevel;
            double rayStartPointX;//路径起点x地理位置
            double rayStartPointY;//路径起点y地理位置
            double rayStartPointZ;//路径起点y地理位置
            double rayEndPointX;//路径终点x地理位置
            double rayEndPointY;//路径终点y地理位置
            double rayEndPointZ;//路径终点y地理位置
            int rayType;//路径类型
            for (int i = 0; i < length; i++)
            {
                string cellid = pathTemp.Rows[i]["cellID"].ToString();
                trajID = Convert.ToInt32(pathTemp.Rows[i]["trajID"].ToString());//路径标识
                rayLevel = Convert.ToInt16(pathTemp.Rows[i]["rayLevel"].ToString());
                rayStartPointX = Convert.ToDouble(pathTemp.Rows[i]["rayStartPointX"].ToString());//路径起点x地理位置
                rayStartPointY = Convert.ToDouble(pathTemp.Rows[i]["rayStartPointY"].ToString());//路径起点y地理位置
                rayStartPointZ = Convert.ToDouble(pathTemp.Rows[i]["rayStartPointZ"].ToString());//路径起点y地理位置
                rayEndPointX = Convert.ToDouble(pathTemp.Rows[i]["rayEndPointX"].ToString());//路径终点x地理位置
                rayEndPointY = Convert.ToDouble(pathTemp.Rows[i]["rayEndPointY"].ToString());//路径终点y地理位置
                rayEndPointZ = Convert.ToDouble(pathTemp.Rows[i]["rayEndPointZ"].ToString());//路径终点y地理位置
                rayType = Convert.ToInt16(pathTemp.Rows[i]["rayType"].ToString());//路径类型

                PathInfo path = new PathInfo(cellid, trajID, rayLevel, rayStartPointX, rayStartPointY, rayStartPointZ, rayEndPointX, rayEndPointY, rayEndPointZ, rayType);
                if (rayLevel == 0)
                {
                    path.sourceEmit = cellPwr[cellid];
                    path.distance = 0;
                    path.emit = cellPwr[cellid];//本段起点功率


                    emit = cellPwr[cellid];//存储路径起点功率
                                           //emit = Math.Pow(10, (emitDbm / 10 - 3));

                    //更新一些系数比如，distance，初始化 amendCoeDis，reflectedR，diffrctedR
                    distance = Point.distance(path.rayStartPoint, path.rayEndPoint);
                    reflectedR = 1;//反射系数
                    diffrctedR = 1;//绕射系数

                }
                else
                {
                    path.distance = distance;
                    path.sourceEmit = emit;
                    if (rayType == 1 || rayType == 2) //反射
                    {
                        double Attenuation = Convert.ToDouble(pathTemp.Rows[i]["attenuation"].ToString()); // 用于系数校正
                        reflectedR *= Attenuation;

                    }
                    else if (rayType == 3 || rayType == 4) //绕射
                    {
                        double Attenuation = Convert.ToDouble(pathTemp.Rows[i]["attenuation"].ToString()); // 用于系数校正
                        diffrctedR *= Attenuation;
                    }

                    receivePwr = emit / (Math.Pow(nata / (4 * Math.PI), 2) * Math.Pow(reflectedR, 2) * Math.Pow(diffrctedR, 2)) * Math.Pow(distance, 2);

                    //计算当前路段的发射功率，并更新distance，amendCoeDis，reflectedR，diffrctedR
                    path.emit = receivePwr;
                    //path.emit = 10 * (Math.Log10(receivePwr) + 3);

                    distance += Point.distance(path.rayStartPoint, path.rayEndPoint);
                }

                raypath.Add(path);
                path = null;
            }
            pathTemp.Clear();
            t3 = DateTime.Now;
            Debug.WriteLine(String.Format("读取数据库：{0}s, 处理数据：{1}", t2 - t1, t3 - t2));
            return raypath;
        }

        /// <summary>
        /// 分批读取数据库数据并建立索引
        /// </summary>
        private void DataHandler()
        {
            //获取该virsources对应的反射点Cellid
            DateTime t1, t2, t3;

            foreach (DataRow tb in tbsourceInfo.Rows)
            {
                t1 = DateTime.Now;
                int cellid = Convert.ToInt32(tb["CI"]);
                double pwr = Convert.ToDouble(tb["ReceivePW"]);
                //对于每一个cellid，获取射线datatable,进行读取和映射处理
                List<PathInfo> curpaths = GetPathByBatch(cellid, pwr);
                t2 = DateTime.Now;
                int mod = 10000;
                int alllen = curpaths.Count;
                for (int i = 0; i < alllen; i += mod)
                {
                    try
                    {
                        if (i + mod > alllen)
                        {
                            this.NewUpdatPathToGridXY(curpaths, i, alllen);
                        }
                        else
                        {
                            this.NewUpdatPathToGridXY(curpaths, i, i + mod);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("已执行：" + i + "   发生错误：" + e.Message);
                    }

                }
                curpaths.Clear();
                t3 = DateTime.Now;
                Debug.WriteLine("批次CI=:" + cellid + "   读取处理用时：" + (t2 - t1) + "   建立索引用时：" + (t3 - t2));
            }

        }
        public List<PathInfo> GetPathByBatch(int ci, double pwr)
        {
            DateTime t1, t2, t3;
            t1 = DateTime.Now;
            List<PathInfo> raypath = new List<PathInfo>();
            Hashtable ht = new Hashtable();
            ht["CI"] = ci;
            DataTable pathTemp = IbatisHelper.ExecuteQueryForDataTable("GetRaylocByCI", ht);
            if (pathTemp.Rows.Count < 2)
            {
                return raypath;
            }
            t2 = DateTime.Now;

            Debug.WriteLine(pathTemp.Rows.Count);
            double nata = 300.0 / (1805 + 0.2 * (893 - 511));

            double reflectedR = 1;//反射系数
            double diffrctedR = 1;//绕射系数


            //用于记录反推接收功率
            double distance = 0;
            double receivePwr = 0;
            double emit = 0;
            int length = pathTemp.Rows.Count;
            int trajID;//路径标识
            short rayLevel;
            double rayStartPointX;//路径起点x地理位置
            double rayStartPointY;//路径起点y地理位置
            double rayStartPointZ;//路径起点y地理位置
            double rayEndPointX;//路径终点x地理位置
            double rayEndPointY;//路径终点y地理位置
            double rayEndPointZ;//路径终点y地理位置
            int rayType;//路径类型
            for (int i = 0; i < length; i++)
            {
                string cellid = pathTemp.Rows[i]["cellID"].ToString();
                trajID = Convert.ToInt32(pathTemp.Rows[i]["trajID"].ToString());//路径标识
                rayLevel = Convert.ToInt16(pathTemp.Rows[i]["rayLevel"].ToString());
                rayStartPointX = Convert.ToDouble(pathTemp.Rows[i]["rayStartPointX"].ToString());//路径起点x地理位置
                rayStartPointY = Convert.ToDouble(pathTemp.Rows[i]["rayStartPointY"].ToString());//路径起点y地理位置
                rayStartPointZ = Convert.ToDouble(pathTemp.Rows[i]["rayStartPointZ"].ToString());//路径起点y地理位置
                rayEndPointX = Convert.ToDouble(pathTemp.Rows[i]["rayEndPointX"].ToString());//路径终点x地理位置
                rayEndPointY = Convert.ToDouble(pathTemp.Rows[i]["rayEndPointY"].ToString());//路径终点y地理位置
                rayEndPointZ = Convert.ToDouble(pathTemp.Rows[i]["rayEndPointZ"].ToString());//路径终点y地理位置
                rayType = Convert.ToInt16(pathTemp.Rows[i]["rayType"].ToString());//路径类型

                PathInfo path = new PathInfo(cellid, trajID, rayLevel, rayStartPointX, rayStartPointY, rayStartPointZ, rayEndPointX, rayEndPointY, rayEndPointZ, rayType);
                if (rayLevel == 0)
                {
                    path.sourceEmit = pwr;
                    path.distance = 0;
                    path.emit = pwr;//本段起点功率

                    emit = pwr;//存储路径起点功率
                               //emit = Math.Pow(10, (emitDbm / 10 - 3));

                    //更新一些系数比如，distance，初始化 amendCoeDis，reflectedR，diffrctedR
                    distance = Point.distance(path.rayStartPoint, path.rayEndPoint);
                    reflectedR = 1;//反射系数
                    diffrctedR = 1;//绕射系数

                }
                else
                {
                    path.distance = distance;
                    path.sourceEmit = emit;
                    if (rayType == 1 || rayType == 2) //反射
                    {
                        double Attenuation = Convert.ToDouble(pathTemp.Rows[i]["attenuation"].ToString()); // 用于系数校正
                        reflectedR *= Attenuation;

                    }
                    else if (rayType == 3 || rayType == 4) //绕射
                    {
                        double Attenuation = Convert.ToDouble(pathTemp.Rows[i]["attenuation"].ToString()); // 用于系数校正
                        diffrctedR *= Attenuation;
                    }

                    receivePwr = emit / (Math.Pow(nata / (4 * Math.PI), 2) * Math.Pow(reflectedR, 2) * Math.Pow(diffrctedR, 2)) * Math.Pow(distance, 2);

                    //计算当前路段的发射功率，并更新distance，amendCoeDis，reflectedR，diffrctedR
                    path.emit = receivePwr;
                    //path.emit = 10 * (Math.Log10(receivePwr) + 3);

                    distance += Point.distance(path.rayStartPoint, path.rayEndPoint);
                }

                raypath.Add(path);
                path = null;
            }
            pathTemp.Clear();
            t3 = DateTime.Now;
            Debug.WriteLine(String.Format("读取数据库：{0}s, 处理数据：{1}", t2 - t1, t3 - t2));
            return raypath;
        }

        /*-----------------------------------------------建立索引--------------------------------------------------------------------*/
        #region 修改内存版本
        public void NewPathToGrid(List<PathInfo> paths, int li, int hi)
        {
            Grid3D grid3d = new Grid3D();
            Point tmp = new Point();
            Dictionary<string, List<GridInfo>> curgrid = new Dictionary<string, List<GridInfo>>();
            for (int i = li; i < hi; i++)
            {
                bool SminE = true;//起点x坐标小于终点x坐标，则为true
                if (paths[i].rayEndPoint.X - paths[i].rayStartPoint.X < 0) SminE = false;

                //double emitDbm =  paths[i].emit;
                //double emit = Math.Pow(10, emitDbm / 10 - 3);

                double sourceEmit = paths[i].sourceEmit;//反向发射源的功率
                double emit = paths[i].emit; //当前路段的接收功率
                string CellID = paths[i].CellID;
                int trajID = paths[i].trajID;
                int rayLevel = paths[i].rayLevel;
                int rayType = paths[i].rayType;

                int n = (int)Math.Abs(paths[i].rayEndPoint.X - paths[i].rayStartPoint.X) / ggridsize;//每段路径分成的栅格小段
                //int m = (int)Math.Abs(paths[i].rayEndPoint.Z - paths[i].rayStartPoint.Z) / ggridVsize;//每段路径分成的栅格小段
                if (n == 0)
                {
                    //将大地坐标转为栅格坐标
                    GridHelper.getInstance().PointXYZGrid(paths[i].rayStartPoint, ref grid3d, ggridsize, ggridVsize);
                    string key = String.Format("{0},{1},{2}", grid3d.gxid, grid3d.gyid, grid3d.gzid);

                    //存到字典中
                    if (curgrid.ContainsKey(key))//字典中已有该网格坐标记录，更新List，从curgrid里删除当前键值，添加更新后的
                    {
                        List<GridInfo> temp = curgrid[key];
                        temp.Add(new GridInfo(CellID, trajID, rayLevel, rayType, paths[i].rayStartPoint.X, paths[i].rayStartPoint.Y, paths[i].rayStartPoint.Z, emit, sourceEmit));
                        curgrid.Remove(key);
                        curgrid.Add(key, temp);
                        curgrid[key].Add(new GridInfo(CellID, trajID, rayLevel, rayType, paths[i].rayStartPoint.X, paths[i].rayStartPoint.Y, paths[i].rayStartPoint.Z, emit, sourceEmit));
                    }
                    else//字典中没有该网格坐标记录，添加
                    {
                        List<GridInfo> temp = new List<GridInfo>
                        {
                            new GridInfo(CellID, trajID, rayLevel, rayType,  paths[i].rayStartPoint.X,  paths[i].rayStartPoint.Y,  paths[i].rayStartPoint.Z, emit, sourceEmit)
                        };
                        curgrid.Add(key, temp);
                    }
                }
                //将每个小段记录到栅格中
                for (int r = 0; r < n; r++)
                {


                    double xid, yid, zid;//为当前记录的小段大地坐标id

                    //路径传播方向
                    if (SminE)
                    {
                        xid = paths[i].rayStartPoint.X + r * ggridsize;
                        yid = paths[i].rayStartPoint.Y + r * ggridsize * paths[i].k1;
                        zid = paths[i].rayStartPoint.Z + r * ggridVsize * paths[i].k2;
                    }
                    else
                    {
                        xid = paths[i].rayStartPoint.X - r * ggridsize;
                        yid = paths[i].rayStartPoint.Y - r * ggridsize * paths[i].k1;
                        zid = paths[i].rayStartPoint.Z - r * ggridVsize * paths[i].k2;
                    }

                    //Point tmp = new Point(xid, yid, zid);
                    tmp.X = xid;
                    tmp.Y = yid;
                    tmp.Z = zid;

                    GridHelper.getInstance().PointXYZGrid(tmp, ref grid3d, ggridsize, ggridVsize);
                    string key = String.Format("{0},{1},{2}", grid3d.gxid, grid3d.gyid, grid3d.gzid);
                    double distance = Math.Sqrt(Math.Pow(r * ggridsize, 2) + Math.Pow(r * ggridsize * paths[i].k1, 2) + Math.Pow(r * ggridVsize * paths[i].k2, 2)) + paths[i].distance;


                    double receivePwr;

                    //存到字典中
                    if (r == 0)
                    {
                        receivePwr = emit;
                    }
                    else
                    {
                        if (paths[i].distance < 0.1)//如果为第一个栅格，则直接赋值当前路段接收功率
                        {
                            receivePwr = emit;
                        }
                        else
                        {
                            receivePwr = Math.Pow(distance, 2) * emit / Math.Pow(paths[i].distance, 2); //根据公式求解
                        }

                    }

                    if (curgrid.ContainsKey(key))//字典中已有该网格坐标记录，更新List，从curgrid里删除当前键值，添加更新后的
                    {
                        List<GridInfo> temp = curgrid[key];
                        temp.Add(new GridInfo(CellID, trajID, rayLevel, rayType, xid, yid, zid, receivePwr, sourceEmit));
                        //temp.Add(new GridInfo(CellName, trajID, rayLevel, rayType, xid, yid, zid, 10 * (Math.Log10(receivePwr) + 3), sourceEmit));
                        curgrid.Remove(key);
                        curgrid.Add(key, temp);
                        //curgrid[key].Add(new GridInfo(CellID, trajID, rayLevel, rayType, paths[i].rayStartPoint.X, paths[i].rayStartPoint.Y, paths[i].rayStartPoint.Z, emit, sourceEmit));
                    }
                    else//字典中没有该网格坐标记录，添加
                    {
                        List<GridInfo> temp = new List<GridInfo>();
                        temp.Add(new GridInfo(CellID, trajID, rayLevel, rayType, xid, yid, zid, receivePwr, sourceEmit));
                        //temp.Add(new GridInfo(CellName, trajID, rayLevel, rayType, xid, yid, zid, 10 * (Math.Log10(receivePwr) + 3), sourceEmit));
                        curgrid.Add(key, temp);

                    }
                }

            }
            foreach (KeyValuePair<string, List<GridInfo>> kvp in curgrid)
            {
                if (togrid.ContainsKey(kvp.Key))
                {
                    togrid[kvp.Key].Union(new List<GridInfo>(kvp.Value));
                }
                else
                {
                    togrid.Add(kvp.Key, new List<GridInfo>(kvp.Value));
                }
            }
            curgrid.Clear();
            return;
        }
        #endregion


        #region 只考虑平面2D,修改内存版本
        public void NewPathToGridXY(List<PathInfo> paths, int li, int hi)
        {
            Grid3D grid3d = new Grid3D();
            Point tmp = new Point();
            Dictionary<string, List<GridInfo>> curgrid = new Dictionary<string, List<GridInfo>>();
            for (int i = li; i < hi; i++)
            {
                bool SminE = true;//起点x坐标小于终点x坐标，则为true
                if (paths[i].rayEndPoint.X - paths[i].rayStartPoint.X < 0) SminE = false;

                double sourceEmit = paths[i].sourceEmit;//反向发射源的功率
                double emit = paths[i].emit; //当前路段的接收功率
                string CellID = paths[i].CellID;
                int trajID = paths[i].trajID;
                int rayLevel = paths[i].rayLevel;
                int rayType = paths[i].rayType;

                int n = (int)Math.Abs(paths[i].rayEndPoint.X - paths[i].rayStartPoint.X) / ggridsize;//每段路径分成的栅格小段
                //int m = (int)Math.Abs(paths[i].rayEndPoint.Z - paths[i].rayStartPoint.Z) / ggridVsize;//每段路径分成的栅格小段
                if (n == 0)
                {
                    //将大地坐标转为栅格坐标
                    //paths[i].rayStartPoint.Z = 0;
                    GridHelper.getInstance().PointXYZGrid(paths[i].rayStartPoint, ref grid3d, ggridsize, ggridVsize);
                    string key = String.Format("{0},{1},{2}", grid3d.gxid, grid3d.gyid, 0);

                    //存到字典中
                    if (curgrid.ContainsKey(key))//字典中已有该网格坐标记录，更新List，从curgrid里删除当前键值，添加更新后的
                    {
                        List<GridInfo> temp = curgrid[key];
                        temp.Add(new GridInfo(CellID, trajID, rayLevel, rayType, paths[i].rayStartPoint.X, paths[i].rayStartPoint.Y, paths[i].rayStartPoint.Z, emit, sourceEmit));
                        curgrid.Remove(key);
                        curgrid.Add(key, new List<GridInfo>(temp));
                        temp.Clear();
                        //curgrid[key].Add(new GridInfo(CellID, trajID, rayLevel, rayType, paths[i].rayStartPoint.X, paths[i].rayStartPoint.Y, paths[i].rayStartPoint.Z, emit, sourceEmit));
                    }
                    else//字典中没有该网格坐标记录，添加
                    {
                        List<GridInfo> temp = new List<GridInfo>
                        {
                            new GridInfo(CellID, trajID, rayLevel, rayType,  paths[i].rayStartPoint.X,  paths[i].rayStartPoint.Y,  paths[i].rayStartPoint.Z, emit, sourceEmit)
                        };
                        curgrid.Add(key, new List<GridInfo>(temp));
                        temp.Clear();
                    }
                }
                //将每个小段记录到栅格中
                for (int r = 0; r < n; r++)
                {

                    double xid, yid, zid;//为当前记录的小段大地坐标id

                    //路径传播方向
                    if (SminE)
                    {
                        xid = paths[i].rayStartPoint.X + r * ggridsize;
                        yid = paths[i].rayStartPoint.Y + r * ggridsize * paths[i].k1;
                        zid = paths[i].rayStartPoint.Z + r * ggridVsize * paths[i].k2;
                    }
                    else
                    {
                        xid = paths[i].rayStartPoint.X - r * ggridsize;
                        yid = paths[i].rayStartPoint.Y - r * ggridsize * paths[i].k1;
                        zid = paths[i].rayStartPoint.Z - r * ggridVsize * paths[i].k2;
                    }

                    //Point tmp = new Point(xid, yid, zid);
                    tmp.X = xid;
                    tmp.Y = yid;
                    tmp.Z = zid;

                    GridHelper.getInstance().PointXYZGrid(tmp, ref grid3d, ggridsize, ggridVsize);
                    string key = String.Format("{0},{1},{2}", grid3d.gxid, grid3d.gyid, 0);
                    double distance = Math.Sqrt(Math.Pow(r * ggridsize, 2) + Math.Pow(r * ggridsize * paths[i].k1, 2) + Math.Pow(r * ggridVsize * paths[i].k2, 2)) + paths[i].distance;


                    double receivePwr;

                    //存到字典中
                    if (r == 0)
                    {
                        receivePwr = emit;
                    }
                    else
                    {
                        if (paths[i].distance < 0.1)//如果为第一个栅格，则直接赋值当前路段接收功率
                        {
                            receivePwr = emit;
                        }
                        else
                        {
                            receivePwr = Math.Pow(distance, 2) * emit / Math.Pow(paths[i].distance, 2); //根据公式求解
                        }

                    }

                    if (curgrid.ContainsKey(key))//字典中已有该网格坐标记录，更新List，从curgrid里删除当前键值，添加更新后的
                    {
                        List<GridInfo> temp = new List<GridInfo>(curgrid[key]);
                        temp.Add(new GridInfo(CellID, trajID, rayLevel, rayType, xid, yid, zid, receivePwr, sourceEmit));
                        //temp.Add(new GridInfo(CellName, trajID, rayLevel, rayType, xid, yid, zid, 10 * (Math.Log10(receivePwr) + 3), sourceEmit));
                        curgrid.Remove(key);
                        curgrid.Add(key, new List<GridInfo>(temp));
                        temp.Clear();
                        //curgrid[key].Add(new GridInfo(CellID, trajID, rayLevel, rayType, paths[i].rayStartPoint.X, paths[i].rayStartPoint.Y, paths[i].rayStartPoint.Z, emit, sourceEmit));
                    }
                    else//字典中没有该网格坐标记录，添加
                    {
                        List<GridInfo> temp = new List<GridInfo>();
                        temp.Add(new GridInfo(CellID, trajID, rayLevel, rayType, xid, yid, zid, receivePwr, sourceEmit));
                        //temp.Add(new GridInfo(CellName, trajID, rayLevel, rayType, xid, yid, zid, 10 * (Math.Log10(receivePwr) + 3), sourceEmit));
                        curgrid.Add(key, new List<GridInfo>(temp));
                        temp.Clear();

                    }
                }

            }
            foreach (KeyValuePair<string, List<GridInfo>> kvp in curgrid)
            {
                if (togrid.ContainsKey(kvp.Key))
                {
                    List<GridInfo> tmpnow = togrid[kvp.Key];
                    togrid.Remove(kvp.Key);
                    foreach (GridInfo grid in kvp.Value)
                    {
                        tmpnow.Add(new GridInfo(grid));
                    }
                    togrid.Add(kvp.Key, new List<GridInfo>(tmpnow));
                    tmpnow.Clear();
                }
                else
                {
                    togrid.Add(kvp.Key, new List<GridInfo>(kvp.Value));
                }
            }
            curgrid.Clear();
            return;
        }
        #endregion

        /// <summary>
        /// 根据x，y走向，确定遍历方向
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="li"></param>
        /// <param name="hi"></param>
        public void NewUpdatPathToGridXY(List<PathInfo> paths, int li, int hi)
        {
            Grid3D grid3d = new Grid3D();
            Point tmp = new Point();
            Dictionary<string, List<GridInfo>> curgrid = new Dictionary<string, List<GridInfo>>();

            for (int i = li; i < hi; i++)
            {
                bool isAxisX = true;//起点x坐标小于终点x坐标，则为true
                int dir = 1;
                if (paths[i].rayEndPoint.X - paths[i].rayStartPoint.X < 0) dir = -1;

                //double emitDbm =  paths[i].emit;
                //double emit = Math.Pow(10, emitDbm / 10 - 3);

                double sourceEmit = paths[i].sourceEmit;//反向发射源的功率
                double emit = paths[i].emit; //当前路段的接收功率
                string CellID = paths[i].CellID;
                int trajID = paths[i].trajID;
                int rayLevel = paths[i].rayLevel;
                int rayType = paths[i].rayType;
                double k = paths[i].k1;
                int n = (int)Math.Abs(paths[i].rayEndPoint.X - paths[i].rayStartPoint.X) / ggridsize;//每段路径分成的栅格小段
                if (Math.Abs(k) > 1)
                {
                    isAxisX = false;
                    if (paths[i].rayEndPoint.Y - paths[i].rayStartPoint.Y < 0) dir = -1;
                    n = (int)Math.Abs(paths[i].rayEndPoint.Y - paths[i].rayStartPoint.Y) / ggridsize;
                }

                if (n == 0)
                {
                    //将大地坐标转为栅格坐标
                    //paths[i].rayStartPoint.Z = 0;
                    GridHelper.getInstance().PointXYZGrid(paths[i].rayStartPoint, ref grid3d, ggridsize, ggridVsize);
                    string key = String.Format("{0},{1},{2}", grid3d.gxid, grid3d.gyid, 0);

                    //存到字典中
                    if (curgrid.ContainsKey(key))//字典中已有该网格坐标记录，更新List，从curgrid里删除当前键值，添加更新后的
                    {
                        List<GridInfo> temp = curgrid[key];
                        temp.Add(new GridInfo(CellID, trajID, rayLevel, rayType, paths[i].rayStartPoint.X, paths[i].rayStartPoint.Y, paths[i].rayStartPoint.Z, emit, sourceEmit));
                        curgrid.Remove(key);
                        curgrid.Add(key, new List<GridInfo>(temp));
                        temp.Clear();
                        //curgrid[key].Add(new GridInfo(CellID, trajID, rayLevel, rayType, paths[i].rayStartPoint.X, paths[i].rayStartPoint.Y, paths[i].rayStartPoint.Z, emit, sourceEmit));
                    }
                    else//字典中没有该网格坐标记录，添加
                    {
                        List<GridInfo> temp = new List<GridInfo>
                        {
                            new GridInfo(CellID, trajID, rayLevel, rayType,  paths[i].rayStartPoint.X,  paths[i].rayStartPoint.Y,  paths[i].rayStartPoint.Z, emit, sourceEmit)
                        };
                        curgrid.Add(key, new List<GridInfo>(temp));
                        temp.Clear();
                    }
                }
                //将每个小段记录到栅格中
                double startx = paths[i].rayStartPoint.X;
                double starty = paths[i].rayStartPoint.Y;
                double startz = paths[i].rayStartPoint.Z;
                double kxy = paths[i].k2;
                double kyz = paths[i].k3;
                for (int r = 0; r < n; r++)
                {

                    double xid, yid, zid;//为当前记录的小段大地坐标id

                    //路径传播方向
                    if (isAxisX)
                    {
                        xid = startx + r * ggridsize * dir;
                        yid = starty + r * ggridsize * dir * k;
                        zid = startz + r * ggridVsize * kxy * dir;
                    }
                    else
                    {
                        xid = startx + r * ggridsize * dir / k;
                        yid = starty + r * ggridsize * dir;
                        zid = startz + r * ggridVsize * kyz * dir;
                    }
                    tmp.X = xid;
                    tmp.Y = yid;
                    tmp.Z = zid;

                    GridHelper.getInstance().PointXYZGrid(tmp, ref grid3d, ggridsize, ggridVsize);
                    string key = String.Format("{0},{1},{2}", grid3d.gxid, grid3d.gyid, 0);
                    double distance = Math.Sqrt(Math.Pow(r * ggridsize, 2) + Math.Pow(r * ggridsize * paths[i].k1, 2) + Math.Pow(r * ggridVsize * paths[i].k2, 2)) + paths[i].distance;


                    double receivePwr;

                    //存到字典中
                    if (r == 0)
                    {
                        receivePwr = emit;
                    }
                    else
                    {
                        if (paths[i].distance < 0.1)//如果为第一个栅格，则直接赋值当前路段接收功率
                        {
                            receivePwr = emit;
                        }
                        else
                        {
                            receivePwr = Math.Pow(distance, 2) * emit / Math.Pow(paths[i].distance, 2); //根据公式求解
                        }

                    }

                    if (curgrid.ContainsKey(key))//字典中已有该网格坐标记录，更新List，从curgrid里删除当前键值，添加更新后的
                    {
                        List<GridInfo> temp = new List<GridInfo>(curgrid[key]);
                        temp.Add(new GridInfo(CellID, trajID, rayLevel, rayType, xid, yid, zid, receivePwr, sourceEmit));
                        //temp.Add(new GridInfo(CellName, trajID, rayLevel, rayType, xid, yid, zid, 10 * (Math.Log10(receivePwr) + 3), sourceEmit));
                        curgrid.Remove(key);
                        curgrid.Add(key, new List<GridInfo>(temp));
                        temp.Clear();
                        //curgrid[key].Add(new GridInfo(CellID, trajID, rayLevel, rayType, paths[i].rayStartPoint.X, paths[i].rayStartPoint.Y, paths[i].rayStartPoint.Z, emit, sourceEmit));
                    }
                    else//字典中没有该网格坐标记录，添加
                    {
                        List<GridInfo> temp = new List<GridInfo>();
                        temp.Add(new GridInfo(CellID, trajID, rayLevel, rayType, xid, yid, zid, receivePwr, sourceEmit));
                        //temp.Add(new GridInfo(CellName, trajID, rayLevel, rayType, xid, yid, zid, 10 * (Math.Log10(receivePwr) + 3), sourceEmit));
                        curgrid.Add(key, new List<GridInfo>(temp));
                        temp.Clear();

                    }
                }

            }
            foreach (KeyValuePair<string, List<GridInfo>> kvp in curgrid)
            {
                if (togrid.ContainsKey(kvp.Key))
                {
                    List<GridInfo> tmpnow = togrid[kvp.Key];
                    togrid.Remove(kvp.Key);
                    foreach (GridInfo grid in kvp.Value)
                    {
                        tmpnow.Add(new GridInfo(grid));
                    }
                    togrid.Add(kvp.Key, new List<GridInfo>(tmpnow));
                    tmpnow.Clear();
                }
                else
                {
                    togrid.Add(kvp.Key, new List<GridInfo>(kvp.Value));
                }
            }
            curgrid.Clear();
            return;
        }

        /*-----------------------------------------------辅助函数--------------------------------------------------------------------*/
        private void GetGrid_From(Dictionary<string, List<GridInfo>> grids)
        {
            Dictionary<string, HashSet<String>> gridfrom = new Dictionary<string, HashSet<string>>();
            if (grids == null || grids.Count < 1)
            {
                Debug.WriteLine("GetGrid_form 传入参数为空");
                return;
            }
            foreach (KeyValuePair<string, List<GridInfo>> kvp in grids)
            {
                //遍历每个栅格经过的射线，获得其经过的射线来源
                HashSet<string> _fromtmp = new HashSet<string>();
                foreach (GridInfo tmp in kvp.Value)
                {
                    if (_fromtmp == null || !_fromtmp.Contains(tmp.cellid))
                    {
                        _fromtmp.Add(tmp.cellid);
                    }
                }
                gridfrom.Add(kvp.Key, _fromtmp);
            }
            this.grid_from = new Dictionary<string, HashSet<string>>(gridfrom);
            gridfrom.Clear();
        }

        private Dictionary<string, long> GetGrid_StrongP(Dictionary<string, List<GridInfo>> grids)
        {
            Dictionary<string, long> tmp1 = new Dictionary<string, long>();
            String[] keyArr = grids.Keys.ToArray<String>();
            for (int i = 0; i < keyArr.Length; i++)
            {
                long count = 0;
                List<GridInfo> tmp = grids[keyArr[i]];
                for (int j = 0; j < tmp.Count; j++)
                {
                    if (tmp[j].rayType == 0 || tmp[j].rayType == 1 || tmp[j].rayType == 2)
                    {
                        count++;
                    }
                }
                tmp1.Add(keyArr[i], count);
            }
            return tmp1;
        }


        /// <summary>
        /// 基于GridInfo采用的是单位w， 加权统计栅格接收功率
        /// </summary>
        /// <param name="grids"></param>
        /// <returns></returns>
        public Dictionary<string, double> MergeGridPwr(Dictionary<string, List<GridInfo>> grids)
        {

            String[] keyArr = grids.Keys.ToArray<String>();
            for (int i = 0; i < keyArr.Length; i++)
            {
                string key = keyArr[i];
                double emitpw = 0.0;

                //for(int j = 0; j < grids[key].Count; j++)
                //{
                //    GridInfo tmp = grids[key][j];
                //    double emitDbm = tmp.recP;
                //    if (tmp.rayType == 0)
                //    {
                //        emitpw += Math.Pow(10, emitDbm / 10 - 3); 
                //    }
                //    else if(tmp.rayType == 1 || tmp.rayType == 2)
                //    {
                //        emitpw += Math.Pow(10, emitDbm / 10 - 3)*0.8;
                //    }
                //    else
                //    {
                //        emitpw += Math.Pow(10, emitDbm / 10 - 3) * 0.5;
                //    }
                //    emitpw += Math.Pow(10, emitDbm / 10 - 3); ;
                //}


                for (int j = 0; j < grids[key].Count; j++)
                {
                    GridInfo tmp = grids[key][j];
                    double emit = tmp.recP;
                    if (tmp.rayType == 0)
                    {
                        emitpw += emit;
                    }
                    else if (tmp.rayType == 1 || tmp.rayType == 2)
                    {
                        emitpw += emit * 0.8;
                    }
                    else
                    {
                        emitpw += emit * 0.5;
                    }

                }
                grid_pwr.Add(key, emitpw);
            }

            return grid_pwr;
        }

        /// <summary>
        ///计算栅格平均场强
        /// </summary>
        /// <param name="up"></param>
        public void AverageGridPathloss(int up)
        {
            foreach (KeyValuePair<string, List<GridInfo>> kvp in togrid)
            {

                int len = kvp.Value.Count;

                if (len > up)
                {
                    len = up;
                }
                double sum = 0;
                // Debug.WriteLine("   id  : " + kvp.Key);
                for (int i = 0; i < len; i++)
                {
                    if (kvp.Value[i].pathloss < 120)
                    {
                        sum += kvp.Value[i].pathloss;
                        //Debug.Write("   pathloss:" + kvp.Value[i].pathloss + "   cellname    " + kvp.Value[i].cellname + "   trajID:" + kvp.Value[i].trajID + "-" + kvp.Value[i].raylevel + "-" + kvp.Value[i].rayType);
                    }
                    else
                    {
                        len = i;
                        break;
                    }
                }
                //Debug.WriteLine("\t");
                // Debug.Write("长度" + len + "    总和" + sum);
                sum /= len;
                //Debug.WriteLine("    平均" + sum);
                grid_pathloss.Add(kvp.Key, sum);
            }
        }

        /// <summary>
        /// 将每个栅格的记录值按照路径损耗从小到大排列
        /// </summary>
        public void SortByPalthloss()
        {
            foreach (KeyValuePair<string, List<GridInfo>> kvp in togrid)
            {
                kvp.Value.Sort();
            }
        }

        private double GetMin(double x, double y, double z)
        {
            if (x < y)
            {
                if (x < z)
                {
                    return x;
                }
            }
            else
            {
                if (y < z)
                {
                    return y;
                }
            }
            return z;
        }

        private void Clear()
        {
            togrid.Clear();
            grid_from.Clear();
            grid_pwr.Clear();
            grid_pathloss.Clear();
        }

        /*------------------------------------------------筛选得到候选点  具体三种过程实现--------------------------------------------------------------------*/

        #region  筛选掉不符合条件的栅格，减少计算量

        /// <summary>
        /// 
        /// </summary>
        /// <param name="threshold">筛选后栅格剩下最大值</param>
        /// <param name="threshold_1">筛选后栅格剩下最小值</param>
        /// <param name="threshold_2"></param>
        /// <param name="param_1">经过小区所占比</param>
        /// <param name="param_2">直反射线所占比</param>
        /// <param name="lownum">通过栅格最小射线</param>
        public void BalanceFiltPara(int threshold, int threshold_1, int threshold_2, double param_1, double param_2, int lownum)
        {
            bool flag = false; //是否得到合适的值
            //int threshold = 300, threshold_1 = 100, threshold_2 = 5;
            //double param_1 = 0.6, param_2 = 0.95;
            //int lownum = 200;
            int countflag = 0;
            Dictionary<string, List<GridInfo>> pre = new Dictionary<string, List<GridInfo>>(togrid);
            Dictionary<string, List<GridInfo>> tmp = new Dictionary<string, List<GridInfo>>(togrid);
            while (!flag)
            {
                countflag++;
                if (countflag > 1000)
                {
                    //Debug.WriteLine("未找到合适的，出现死循环，可更改参数/阈值 重新计算");
                    break;
                }

                int count = this.GetFiltrateGrid(param_2, lownum, tmp);
                if (count > threshold)//通过栅格射线数以及射线比例筛选 所得数目过多
                {
                    if (param_2 > 0.98)
                    {
                        lownum += 50;
                    }
                    else
                    {
                        param_2 += 0.05;
                    }
                    pre.Clear();
                    pre = new Dictionary<string, List<GridInfo>>(tmp);
                    continue;
                }
                else if (count < threshold_1) //通过栅格射线数以及射线比例筛选 所得数目过少，导致没数据
                {
                    param_2 -= 0.1;
                    lownum -= 50;
                    tmp.Clear();
                    tmp = new Dictionary<string, List<GridInfo>>(pre);
                    continue;
                }
                count = FiltrateGridByCell(param_1, tmp);
                if (count < 1)
                {
                    param_1 -= 0.05;
                    tmp.Clear();
                    tmp = new Dictionary<string, List<GridInfo>>(pre);
                    continue;
                }
                else if (count > threshold_2)
                {
                    param_1 += 0.05;
                    pre.Clear();
                    pre = new Dictionary<string, List<GridInfo>>(tmp);
                    continue;
                }
                flag = true;
            }
            if (tmp.Count > 1 && tmp.Count < threshold_2)
            {
                this.togrid = new Dictionary<string, List<GridInfo>>(tmp);
                tmp.Clear();
                pre.Clear();
            }
            else
            {
                this.togrid = new Dictionary<string, List<GridInfo>>(pre);
                tmp.Clear();
                pre.Clear();
            }

        }

        /// <summary>
        /// 串行，优先进行from
        /// </summary>
        /// <param name="threshold">筛选后栅格剩下最大值SMax</param>
        /// <param name="threshold_1">筛选后栅格剩下最小值SMin</param>
        /// <param name="threshold_2"></param>
        /// <param name="param_1">经过小区所占比</param>
        /// <param name="param_2">直反射线所占比</param>
        /// <param name="lownum">通过栅格最小射线</param>
        public void BalanceFiltParaS(int threshold, int threshold_1, double param_1, double param_2, int lownum)
        {
            Dictionary<string, List<GridInfo>> pre = new Dictionary<string, List<GridInfo>>(togrid);
            Dictionary<string, List<GridInfo>> tmp = new Dictionary<string, List<GridInfo>>(togrid);

            //优先过滤小区
            int count = tmp.Count;
            int itera = 500;
            while ((count > threshold + 100 || count < 5) && itera-- > 0)
            {
                count = this.FiltrateGridByCell(param_1, tmp);
                if (count > threshold + 100)
                {
                    param_1 += 0.05;
                    pre.Clear();
                    pre = new Dictionary<string, List<GridInfo>>(tmp);
                }
                else if (count < 5)
                {
                    tmp.Clear();
                    param_1 -= 0.02;
                    tmp = new Dictionary<string, List<GridInfo>>(pre);
                    itera = 2;
                }
                else
                {
                    break;
                }
            }
            if (count > threshold + 100 || count < 5)
            {
                tmp.Clear();
                tmp = new Dictionary<string, List<GridInfo>>(pre);
            }
            else
            {
                pre.Clear();
                pre = new Dictionary<string, List<GridInfo>>(tmp);
            }

            count = GetFiltrateGrid(param_2, lownum, tmp);
            itera = 500;
            while ((count > threshold || count < threshold_1) && itera-- > 0)
            {
                if (count > threshold)
                {
                    param_2 += 0.05;
                    pre.Clear();
                    pre = new Dictionary<string, List<GridInfo>>(tmp);
                }
                else if (count < threshold_1)
                {
                    tmp.Clear();
                    param_2 -= 0.02;
                    tmp = new Dictionary<string, List<GridInfo>>(pre);
                    itera = 2;
                }
                else
                {
                    break;
                }
                count = count = GetFiltrateGrid(param_2, lownum, tmp);
            }
            if (count > threshold || count < threshold_1)
            {
                tmp.Clear();
                tmp = new Dictionary<string, List<GridInfo>>(pre);
            }
            else
            {
                pre.Clear();
                pre = new Dictionary<string, List<GridInfo>>(tmp);
            }
            this.togrid = new Dictionary<string, List<GridInfo>>(tmp);
            Dictionary<string, HashSet<string>> tmpFrom = new Dictionary<string, HashSet<string>>();
            foreach (KeyValuePair<string, List<GridInfo>> kvp in tmp)
            {
                tmpFrom.Add(kvp.Key, new HashSet<string>(grid_from[kvp.Key]));
            }
            grid_from.Clear();
            grid_from = new Dictionary<string, HashSet<string>>(tmpFrom);
        }
        /// <summary>
        /// 从映射栅格中删除反向发射点周边
        /// </summary>
        public void FiltrateGrid()
        {
            //DataTable tbsource = IbatisHelper.ExecuteQueryForDataTable("GetSelectedPoint", null);
            const int xadj = 4, yadj = 4, zadj = 30;//删除范围（4*栅格大小，4*栅格大小，30*栅格大小）的栅格
            foreach (DataRow row in tbsourceInfo.Rows)
            {
                Geometric.Point point = new Geometric.Point(Convert.ToDouble(row["x"]), Convert.ToDouble(row["y"]), 0);
                Grid3D tmp = new Grid3D();
                GridHelper.getInstance().PointXYZGrid(point, ref tmp, ggridsize, ggridVsize);//此处应该有设置映射栅格大小设置
                for (int i = 0; i < zadj; i++)
                {
                    for (int k = -3; k < xadj; k++)
                    {
                        for (int k1 = -3; k1 < yadj; k1++)
                        {
                            string key = String.Format("{0},{1},{2}", tmp.gxid + k, tmp.gyid + k1, i);
                            if (togrid.ContainsKey(key))
                            {
                                togrid.Remove(key);
                            }
                        }

                    }

                }
            }
        }


        /// <summary>
        /// 筛选掉只包含来自少数反射点的射线的栅格
        /// </summary>
        /// <param name="low">栅格最少经过总反射源点发来射线的比例</param>
        /// <param name="curTogrid"></param>
        /// <returns></returns>
        public int FiltrateGridByCell(double low, Dictionary<string, List<GridInfo>> curTogrid)
        {
            //DataTable source = IbatisHelper.ExecuteQueryForDataTable("GetSelectedPoint", null);
            int lower = Convert.ToInt16(low * tbsourceInfo.Rows.Count);

            String[] keyArr = curTogrid.Keys.ToArray<String>();
            for (int i = 0; i < keyArr.Length; i++)
            {
                string key = keyArr[i];
                if (grid_from[key].Count < lower)
                {
                    curTogrid.Remove(keyArr[i]);
                }
            }
            return curTogrid.Count;
        }

        /// <summary>
        /// 通过直反射线比例以及射线数目控制
        /// </summary>
        /// <param name="rate">为限制的直射反射射线占比</param>
        /// <param name="lownum">限制的射线条数最低值</param>
        /// <param name="grids">传递grids</param>
        /// <returns></returns>
        public int GetFiltrateGrid(double rate, int lownum, Dictionary<string, List<GridInfo>> grids)
        {
            String[] keyArr = grids.Keys.ToArray<String>();
            for (int i = 0; i < keyArr.Length; i++)
            {
                int count = 0;
                List<GridInfo> tmp = grids[keyArr[i]];
                if (tmp.Count < lownum) //有效射线低于某值，则可以不考虑
                {
                    grids.Remove(keyArr[i]);
                    continue;
                }
                for (int j = 0; j < tmp.Count; j++)
                {
                    if (tmp[j].rayType == 0 || tmp[j].rayType == 1 || tmp[j].rayType == 2)
                    {
                        count++;
                    }
                }
                if (count < tmp.Count * rate)
                {
                    grids.Remove(keyArr[i]);
                }
            }
            return grids.Count;
        }


        #endregion

        /*------------------------------------------------评估候选点 --------------------------------------------------------------------*/
        #region 评估候选干扰点
        /// <summary>
        /// 计算一个栅格里的两两小区之间功率损耗差值总和
        /// </summary>
        /// <param name="infos">栅格信息</param>
        /// <param name="l">选取前l个进行计算</param>
        /// <returns></returns>
        public double Evaluate(List<GridInfo> infos, int l)
        {
            Dictionary<string, List<double>> cell_pathloss = new Dictionary<string, List<double>>();

            //提取<cellname,List(pathloss)>
            foreach (GridInfo grid in infos)
            {
                if (cell_pathloss.ContainsKey(grid.cellid))
                {
                    List<double> tmp = cell_pathloss[grid.cellid];
                    if (tmp.Count < l)//仅取l位数
                    {
                        tmp.Add(grid.pathloss);
                        cell_pathloss.Remove(grid.cellid);
                        cell_pathloss.Add(grid.cellid, tmp);
                    }

                }
                else
                {
                    List<double> tmp = new List<double>
                    {
                        grid.pathloss
                    };
                    cell_pathloss.Add(grid.cellid, tmp);
                }
            }

            //Hashtable ht = new Hashtable();
            //ht["fromName"] = this.virname;
            ///获取多个反向发射点的接收功率
            //DataTable tbsourcePwr = IbatisHelper.ExecuteQueryForDataTable("GetSelectedPoint", null);
            Dictionary<string, double> cellPwr = new Dictionary<string, double>();
            foreach (DataRow tb in tbsourceInfo.Rows)
            {
                string cellid = Convert.ToString(tb["CI"]);
                double pwr = Convert.ToDouble(tb["ReceivePW"]);
                cellPwr.Add(cellid, pwr);
            }

            String[] keyArr = cell_pathloss.Keys.ToArray<String>();

            double sum = 0.0;
            for (int i = 0; i < keyArr.Length - 1; i++)
            {
                string cellidA = keyArr[i];
                List<double> tmpA = cell_pathloss[cellidA];
                double pwrA = cellPwr[cellidA];//接收功率

                for (int j = i + 1; j < keyArr.Length; j++)
                {
                    string cellnameB = keyArr[j];
                    List<double> tmpB = cell_pathloss[cellnameB];

                    //同步对数
                    int k = 0;
                    if (tmpA.Count > tmpB.Count)
                    {
                        k = tmpB.Count;
                    }
                    else
                    {
                        k = tmpA.Count;
                    }

                    //求tmp平均
                    double averA = 0.0;
                    for (int r = 0; r < k; r++)
                    {
                        averA += tmpA[r];
                    }
                    averA /= k;

                    double pwrB = cellPwr[cellnameB];
                    double averB = 0.0;
                    for (int r = 0; r < k; r++)
                    {
                        averB += tmpB[r];
                    }
                    averB /= k;

                    // Debug.WriteLine(cellnameA+" countA:" + k+"     "+cellnameB+"   countB:"+k);
                    sum += Math.Abs(10 * (Math.Log10(pwrA) + 3) - 10 * (Math.Log10(pwrA) + 3) + averA - averB);
                }
            }
            return sum;
        }

        /// <summary>
        /// 评估函数
        /// </summary>
        /// <param name="candidate_grid"></param>
        /// <param name="ratioAP">平均损耗</param>
        /// <param name="ratioP">小区间两两损耗差值</param>
        /// <param name="ratioAPW">接收功率</param>
        /// <returns></returns>
        public string EvaluateCombineALL(Dictionary<string, List<GridInfo>> candidate_grid, double ratioAP, double ratioP, double ratioAPW)
        {
            if (candidate_grid.Count < 1) return null;

            List<GridInfo> ans = new List<GridInfo>();
            double LowAP = 0, HighAP = 0, LowP = 0, HighP = 0, LowApw = 0, HighApw = 0;
            Dictionary<string, double> Ppathloss = new Dictionary<string, double>();//存储栅格中两两小区功率损耗差值
            foreach (KeyValuePair<string, List<GridInfo>> kvp in candidate_grid)
            {
                double p = Evaluate(kvp.Value, 20);
                Ppathloss.Add(kvp.Key, p);
                if (candidate_grid.Count == 1) return kvp.Key;
            }

            //Dictionary<string, long> grid_strongP = this.GetGrid_StrongP(candidate_grid);

            #region 找到最大最小值，用于归一化
            int count = 0;
            foreach (KeyValuePair<string, List<GridInfo>> kvp in candidate_grid)
            {
                string k = kvp.Key;
                if (count == 0)
                {

                    LowAP = grid_pathloss[k];
                    HighAP = grid_pathloss[k];
                    LowP = Ppathloss[k];
                    HighP = Ppathloss[k];
                    LowApw = grid_pwr[k] / candidate_grid[k].Count;
                    HighApw = grid_pwr[k] / candidate_grid[k].Count;
                    count++;
                }
                else
                {
                    if (grid_pathloss[k] < LowAP)
                    {
                        LowAP = grid_pathloss[k];
                    }
                    else if (HighAP < grid_pathloss[k])
                    {
                        HighAP = grid_pathloss[k];
                    }

                    if (LowP > Ppathloss[k])
                    {
                        LowP = Ppathloss[k];
                    }
                    else if (HighP < Ppathloss[k])
                    {
                        HighP = Ppathloss[k];
                    }

                    if (LowApw > grid_pwr[k] / candidate_grid[k].Count)
                    {
                        LowApw = grid_pwr[k] / candidate_grid[k].Count;
                    }
                    else if (HighApw < grid_pwr[k] / candidate_grid[k].Count)
                    {
                        HighApw = grid_pwr[k] / candidate_grid[k].Count;
                    }
                }
            }
            #endregion

            LowAP = HighAP - LowAP;
            LowP = HighP - LowP;
            HighApw = HighApw - LowApw;
            Debug.WriteLine("LowAP:" + LowAP + "    LowP:" + LowP + "    HighApw:" + HighApw);

            Dictionary<string, double> Grid_rate = new Dictionary<string, double>();
            foreach (KeyValuePair<string, List<GridInfo>> kvp in candidate_grid)
            {
                string k = kvp.Key;
                double rate = (HighAP - grid_pathloss[k]) / LowAP * ratioAP + (HighP - Ppathloss[k]) / LowP * ratioP + (grid_pwr[k] / candidate_grid[k].Count - LowApw) / HighApw * ratioAPW + grid_from[k].Count * 0.1;
                Grid_rate.Add(k, rate);
                Debug.WriteLine("Grid_:" + k + "    rate:" + rate + "    from:" + grid_from[k].Count);
            }

            string maxKey = "";
            double max = 0;
            foreach (KeyValuePair<string, double> kvp in Grid_rate)
            {
                if (kvp.Value > max)
                {
                    maxKey = kvp.Key;
                    max = kvp.Value;
                }
            }
            return maxKey;

        }
        /// <summary>
        /// 评估函数
        /// </summary>
        /// <param name="candidate_grid"></param>
        /// <param name="ratioAP">平均损耗</param>
        /// <param name="ratioP">小区间两两损耗差值</param>
        /// <param name="ratioAPW">接收功率</param>
        /// <returns></returns>
        public string EvaluateCombineAll(Dictionary<string, List<GridInfo>> candidate_grid, double pathnum, double ratioP, double from)
        {
            if (candidate_grid.Count < 1) return null;

            Dictionary<string, double> Ppathloss = new Dictionary<string, double>();//存储栅格中两两小区功率损耗差值
            foreach (KeyValuePair<string, List<GridInfo>> kvp in candidate_grid)
            {
                double p = Evaluate(kvp.Value, 20);
                Ppathloss.Add(kvp.Key, p);
                if (candidate_grid.Count == 1) return kvp.Key;
            }

            Dictionary<string, long> grid_strongP = this.GetGrid_StrongP(candidate_grid);



            string maxKey = "";
            double max = 0;
            foreach (KeyValuePair<string, List<GridInfo>> kvp in candidate_grid)
            {
                string k = kvp.Key;
                //计算值
                double value = grid_from[k].Count * from + grid_strongP[k] * 0.002 * pathnum - Ppathloss[k] * 0.005 * ratioP;

                if (value > max)
                {
                    maxKey = k;
                    max = value;
                }
                Debug.WriteLine("Grid_:" + k + "    rate:" + value + "    from:" + grid_from[k].Count + " 综合干扰差值：" + Ppathloss[k]);
            }
            return maxKey;

        }
        public void CompareWithAim()
        {
            //获取实际的当前定位干扰源点信息
            Hashtable ht = new Hashtable();
            ht["cellname"] = this.virname;
            DataTable source = IbatisHelper.ExecuteQueryForDataTable("GetVirSourceAllInfo", ht);
            Geometric.Point point;
            if (source.Rows.Count < 1)
            {
                Debug.WriteLine("tbRealSource中无数据");
                return;
            }
            else
            {
                Debug.WriteLine("实际干扰点个数共有" + source.Rows.Count);
                for (int i = 0; i < source.Rows.Count; i++)
                {
                    point = new Geometric.Point(Convert.ToDouble(source.Rows[0]["x"]), Convert.ToDouble(source.Rows[0]["y"]), 0);
                    Grid3D tmp = new Grid3D();
                    GridHelper.getInstance().PointXYZGrid(point, ref tmp, ggridsize, ggridVsize);
                    Debug.WriteLine(" 干扰点  grid坐标   " + tmp.gxid + "   " + tmp.gyid + "   " + tmp.gzid);
                    foreach (KeyValuePair<string, List<GridInfo>> kvp in togrid)
                    {
                        string[] str = kvp.Key.Split(',');
                        if (str.Length != 3)
                        {
                            Debug.WriteLine("key 分解错误");
                        }
                        else
                        {
                            Geometric.Point p = new Geometric.Point();

                            Grid3D tmp1 = new Grid3D(Convert.ToInt32(str[0]), Convert.ToInt32(str[1]), Convert.ToInt32(str[2]));
                            GridHelper.getInstance().PointGridXYZ(ref p, tmp1, ggridsize, ggridVsize);
                            //GridHelper.getInstance().getGridToXY(Convert.ToInt32(str[0]), Convert.ToInt32(str[1]), ref xid, ref yid);
                            double dis = Geometric.Point.distance(point, p);
                            Debug.WriteLine("   grid坐标   " + str[0] + "   " + str[1] + "   " + str[2] + "   " + kvp.Value.Count + "     与源点距离：" + dis + "    栅格功率：dbm" + 10 * (Math.Log10(grid_pwr[kvp.Key]) + 3) + "     栅格平均路径损耗：" + grid_pathloss[kvp.Key]);
                            //Debug.WriteLine("评估值：" + "   30:" + Evaluate(kvp.Value, 30) + "   10:" + Evaluate(kvp.Value, 10) + "   5:" + Evaluate(kvp.Value, 5) + "   2:" + Evaluate(kvp.Value, 2));
                        }

                    }
                }
            }
        }
        #endregion

    }
}
