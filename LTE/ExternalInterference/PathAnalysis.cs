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
using LTE.Utils;
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
        private int max_from;//栅格覆盖的最大起点数
        private const int ggridsize = 30;//射线映射栅格设置的栅格长宽大小
        private const int ggridVsize = 30;

       
        private double nata = 300.0 / (1805 + 0.2 * (63 - 511));
        private int kCons = 3;
        private Dictionary<string, double> cellPwr = new Dictionary<string, double>();//记录接收功率从大到小的路测点信息
        List<string> VitalMP;//接收功率从大到小的路测点cellid
        private DataTable tb = new DataTable();
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
            max_from = int.MinValue;

            Hashtable ht = new Hashtable();
            ht["fromName"] = this.virname;
            DataTable tbsourceInfo = IbatisHelper.ExecuteQueryForDataTable("GetSelectedPoint", ht);
            Dictionary<string, double> cellPwrtmp = new Dictionary<string, double>();
            double curMax = double.MinValue, nextMax = double.MinValue;
            //Debug.WriteLine("源点信息");
            foreach (DataRow tb in tbsourceInfo.Rows)
            {
                string cellid = Convert.ToString(tb["CI"]);
                double pwr = Convert.ToDouble(tb["ReceivePW"]);//单位是w
                cellPwrtmp.Add(cellid, pwr);
                if (curMax < pwr)
                {
                    curMax = pwr;
                }
            }
            VitalMP = new List<string>();
            for (int i = 0; i < cellPwrtmp.Count; i++)
            {
                nextMax = double.MinValue;
                bool isfind = false;
                foreach (KeyValuePair<string, double> kvp in cellPwrtmp)
                {
                    if (!isfind && kvp.Value == curMax)
                    {
                        VitalMP.Add(kvp.Key);
                        cellPwr.Add(kvp.Key, kvp.Value);
                    }
                    else if (isfind && kvp.Value == curMax)
                    {
                        if (++i < cellPwr.Count)//重复的情况
                        {
                            VitalMP.Add(kvp.Key);
                            cellPwr.Add(kvp.Key, kvp.Value);
                        }
                    }
                    else if (kvp.Value < curMax && kvp.Value > nextMax)
                    {
                        nextMax = kvp.Value;
                    }
                }
                curMax = nextMax;
            }

            tb.Columns.Add("Version");
            tb.Columns.Add("Longitude");
            tb.Columns.Add("Latitude");
            tb.Columns.Add("x");
            tb.Columns.Add("y");
            tb.Columns.Add("z");
            tb.Columns.Add("ansID");


        }


        public ResultRecord StartAnalysis(double ratioAP, double ratioP, double ratioAPW)
        {

            
            Clear();
            DateTime t1, t2, t3, t4;
            t1 = DateTime.Now;

            if (!DataHandler(3))
            {
                return new ResultRecord(false,"路径不完整，没有覆盖所有反向跟踪起点");
            }
           
            t3 = DateTime.Now;

            Debug.WriteLine("y原始栅格大小" + this.togrid.Count);

            this.GetGrid_From(togrid,10);

            // this.BalanceFiltPara(1000, 500, 80, 0.8, 0.80, 200);

            BalanceFiltParaS(500, 20, 0.9, 0.8, 200);
            SortByPalthloss();
            AverageGridPathloss(600);
            this.MergeGridPwr(this.togrid);

            //CompareWithAim();
            Debug.WriteLine("过滤发射源周围栅格后栅格大小 " + this.togrid.Count);
            //string aimGrid = EvaluateCombineAll(togrid, ratioAP, ratioP, ratioAPW,10);
            string aimGrid = EvaluateCombineAllNor(togrid, ratioAP, ratioP, ratioAPW, 10);
            
            Debug.WriteLine("AimGrid: " + aimGrid==""?"none":aimGrid);
            t4 = DateTime.Now;
            Debug.WriteLine("计算时长: " + (t4 - t1));
            //string aimGrid = EvaluateCombineALL(togrid, ratioAP, ratioP, ratioAPW);//得到当前占比最大的栅格
            if (aimGrid == null || aimGrid == "")
            {
                Debug.WriteLine("未找到合适位置");
                return new ResultRecord(false, "", "", 0, "未找到合适位置"); 
            }
            //TestPre();
            
            
            return showResult(aimGrid);
        }

        public Dictionary<string, List<GridInfo>> getTogrid()
        {
            if (togrid.Count == 0)
            {
                DataHandler(3);
            }
            return togrid;
        }

        private ResultRecord showResult(string ans)
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
            
            Debug.WriteLine("定位结果坐标：" + p.X + ",  " + p.Y + ",   " + p.Z);
            Debug.WriteLine("定位结果栅格：" + gx + ",  " + gy + ",   " + gz);
            LTE.Utils.PointConvertByProj.Instance.GetGeoPoint(p);
            double lon = p.X;
            double lat = p.Y;
            LTE.Utils.PointConvertByProj.Instance.GetProjectPoint(p);
            Hashtable ht = new Hashtable();
            ht["cellname"] = this.virname;
            DataTable source = IbatisHelper.ExecuteQueryForDataTable("GetVirSourceAllInfo", ht);
            Geometric.Point point;
            if (source==null || source.Rows.Count < 1)
            {
                Debug.WriteLine("tbRealSource中无数据");
                return new ResultRecord(true,lon,lat,string.Format("{0},{1},{2}", p.X, p.Y, p.Z), "", 0, "不存在指定的干扰源信息");
            }
            else
            {
                point = new Geometric.Point(Convert.ToDouble(source.Rows[0]["x"]), Convert.ToDouble(source.Rows[0]["y"]), Convert.ToDouble(source.Rows[0]["z"]));
                Grid3D tmp2 = new Grid3D();
                GridHelper.getInstance().PointXYZGrid(point, ref tmp2, ggridsize, ggridVsize);
                Console.WriteLine("干扰源点坐标：" + point.X + ",  " + point.Y + ",   " + point.Z);
                Debug.WriteLine(" 干扰源点栅格：" + tmp2.gxid + ",  " + tmp2.gyid + ",  " + tmp2.gzid);
                point.Z = 0;
                p.Z = 0;
                double dis = Geometric.Point.distance(point, p);

                #region 测试
                //test
                //if (dis >= 50) {
                //    double tarLon = Convert.ToDouble(source.Rows[0]["Longitude"]);
                //    double tarLat = Convert.ToDouble(source.Rows[0]["Latitude"]);
                //    lon = (lon + tarLon) / 2;
                //    lat = (lat + tarLat) / 2;
                //}
                #endregion

                Debug.WriteLine(" 定位精度：" + dis);
                return new ResultRecord(true, lon, lat, string.Format("{0},{1},{2}", p.X, p.Y, p.Z), "", 0, "不存在指定的干扰源信息");
                //presentGrid(tmp1);

            }
        }

        private ResultRecord showResultTest(string ans)
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
        #region 三维射线路径映射
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
            

            double reflectedR = 1;//反射系数
            double diffrctedR = 1;//绕射系数

            //获取tbsource 数据,存到字典 cellPwr 中

            //DataTable tbsourcePwr = IbatisHelper.ExecuteQueryForDataTable("GetSelectedPoint", ht);
            

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
        //private void DataHandler()
        //{
        //    //获取该virsources对应的反射点Cellid
        //    DateTime t1, t2, t3;

        //    foreach (DataRow tb in tbsourceInfo.Rows)
        //    {
        //        t1 = DateTime.Now;
        //        int cellid = Convert.ToInt32(tb["CI"]);
        //        double pwr = Convert.ToDouble(tb["ReceivePW"]);
        //        //对于每一个cellid，获取射线datatable,进行读取和映射处理
        //        List<PathInfo> curpaths = GetPathByBatch(cellid, pwr);
        //        t2 = DateTime.Now;
        //        int mod = 10000;
        //        int alllen = curpaths.Count;
        //        for (int i = 0; i < alllen; i += mod)
        //        {
        //            try
        //            {
        //                if (i + mod > alllen)
        //                {
        //                    this.NewUpdatPathToGridXY(curpaths, i, alllen);
        //                }
        //                else
        //                {
        //                    this.NewUpdatPathToGridXY(curpaths, i, i + mod);
        //                }
        //            }
        //            catch (Exception e)
        //            {
        //                Debug.WriteLine("已执行：" + i + "   发生错误：" + e.Message);
        //            }

        //        }
        //        curpaths.Clear();
        //        t3 = DateTime.Now;
        //        Debug.WriteLine("批次CI=:" + cellid + "   读取处理用时：" + (t2 - t1) + "   建立索引用时：" + (t3 - t2));
        //    }

        //}

        private bool DataHandler(int k)
        {
            //获取该virsources对应的反射点Cellid
            DateTime t1, t2, t3;
            
            //对前k个，读取的数据按照
            for (int i = 0; i < k; i++)
            {
                t1 = DateTime.Now;
                int cellid = Convert.ToInt32(VitalMP[i]);
                double pwr = cellPwr[VitalMP[i]];
                //对于每一个cellid，获取射线datatable,进行读取和映射处理
                List<PathInfo> curpaths = GetPathByBatch(cellid, pwr);
                if (curpaths == null || curpaths.Count < 2)
                {
                    return false;
                }
                t2 = DateTime.Now;
                int mod = 10000;
                int alllen = curpaths.Count;
                for (int j = 0; j < alllen; j += mod)
                {
                    try
                    {
                        if (j + mod > alllen)
                        {
                            this.NewUpdatPathToGridXYZ(curpaths, j, alllen);
                        }
                        else
                        {
                            this.NewUpdatPathToGridXYZ(curpaths, j, j + mod);
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
            for (int i = k; i < VitalMP.Count; i++)
            {
                t1 = DateTime.Now;
                int cellid = Convert.ToInt32(VitalMP[i]);
                double pwr = cellPwr[VitalMP[i]];
                //对于每一个cellid，获取射线datatable,进行读取和映射处理
                List<PathInfo> curpaths = GetPathByBatch(cellid, pwr);
                if (curpaths == null || curpaths.Count < 2)
                {
                    return false;
                }
                t2 = DateTime.Now;
                int mod = 10000;
                int alllen = curpaths.Count;
                for (int j = 0; j < alllen; j += mod)
                {
                    try
                    {
                        if (j + mod > alllen)
                        {
                            this.NewUpdatPathToGridXYZUV(curpaths, j, alllen);
                        }
                        else
                        {
                            this.NewUpdatPathToGridXYZUV(curpaths, j, j + mod);
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
            return true;
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

                //计算功率
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
        #endregion

        /*-----------------------------------------------建立索引--------------------------------------------------------------------*/
        #region 建立索引
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
                    if(!GridHelper.getInstance().PointXYZGrid(paths[i].rayStartPoint, ref grid3d, ggridsize, ggridVsize))
                    {
                        continue;
                    }
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

                    if(!GridHelper.getInstance().PointXYZGrid(tmp, ref grid3d, ggridsize, ggridVsize))
                    {
                        continue;
                    }
                    string key = String.Format("{0},{1},{2}", grid3d.gxid, grid3d.gyid, grid3d.gzid);
                    //因为
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
                            receivePwr = emit / (Math.Pow(nata / (4 * Math.PI), 2))  * Math.Pow(distance, 2);
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
                    if(!GridHelper.getInstance().PointXYZGrid(paths[i].rayStartPoint, ref grid3d, ggridsize, ggridVsize))
                    {
                        continue;
                    }
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

                    if(!GridHelper.getInstance().PointXYZGrid(tmp, ref grid3d, ggridsize, ggridVsize))
                    {
                        continue;
                    }
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
                    if (paths[i].rayEndPoint.Y - paths[i].rayStartPoint.Y < 0)
                    {
                        dir = -1;
                    }
                    else
                    {
                        dir = 1;
                    }
                    n = (int)Math.Abs(paths[i].rayEndPoint.Y - paths[i].rayStartPoint.Y) / ggridsize;
                }

                if (n == 0)
                {
                    //将大地坐标转为栅格坐标
                    //paths[i].rayStartPoint.Z = 0;
                    if(!GridHelper.getInstance().PointXYZGrid(paths[i].rayStartPoint, ref grid3d, ggridsize, ggridVsize))
                    {
                        continue;
                    }
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

                    if(!GridHelper.getInstance().PointXYZGrid(tmp, ref grid3d, ggridsize, ggridVsize))
                    {
                        continue;
                    }
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
                            receivePwr = emit / (Math.Pow(nata / (4 * Math.PI), 2)) * Math.Pow(distance, 2);
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


        public void NewUpdatPathToGridXYZ(List<PathInfo> paths, int li, int hi)
        {
            Grid3D grid3d = new Grid3D();
            Point tmp = new Point();
            Dictionary<string, List<GridInfo>> curgrid = new Dictionary<string, List<GridInfo>>();
            
            for (int i = li; i < hi; i++)
            {
                if (i % 10000 == 0)
                {
                    Debug.WriteLine("已执行" + i);
                }
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
                    if(!GridHelper.getInstance().PointXYZGrid(paths[i].rayStartPoint, ref grid3d, ggridsize, ggridVsize))
                    {
                        continue;
                    }
                    string key = String.Format("{0},{1},{2}", grid3d.gxid, grid3d.gyid, grid3d.gzid);

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
                //Debug.WriteLine("当前段" + n);
                for (int r = 0; r < n; r++)
                {

                    double xid, yid, zid;//为当前记录的小段大地坐标id

                    //路径传播方向
                    if (isAxisX)
                    {
                        xid = startx + r * ggridsize * dir;
                        yid = starty + r * ggridsize * dir * k;
                        zid = startz + r * ggridsize * kxy * dir;
                    }
                    else
                    {
                        xid = startx + r * ggridsize * dir / k;
                        yid = starty + r * ggridsize * dir;
                        zid = startz + r * ggridsize * kyz * dir;
                    }
                    tmp.X = xid;
                    tmp.Y = yid;
                    tmp.Z = zid;
                    //Debug.WriteLine()
                    if(!GridHelper.getInstance().PointXYZGrid(tmp, ref grid3d, ggridsize, ggridVsize))
                    {
                        continue;
                    }
                    string key = String.Format("{0},{1},{2}", grid3d.gxid, grid3d.gyid, grid3d.gzid);
                    double distance = Math.Sqrt(Math.Pow((xid-startx), 2) + Math.Pow(yid-starty, 2) + Math.Pow(zid-startz, 2)) + paths[i].distance;
                    
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
                            receivePwr = emit / (Math.Pow(nata / (4 * Math.PI), 2)) * Math.Pow(distance, 2);
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

        /// <summary>
        /// 对次重要的点进行的三维映射
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="li"></param>
        /// <param name="hi"></param>
        public void NewUpdatPathToGridXYZUV(List<PathInfo> paths, int li, int hi)
        {
            Grid3D grid3d = new Grid3D();
            Point tmp = new Point();
            Dictionary<string, List<GridInfo>> curgrid = new Dictionary<string, List<GridInfo>>();
            
            for (int i = li; i < hi; i++)
            {
                if (i % 10000 == 0)
                {
                    Debug.WriteLine("已执行" + i);
                }
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
                    if(!GridHelper.getInstance().PointXYZGrid(paths[i].rayStartPoint, ref grid3d, ggridsize, ggridVsize))
                    {
                        continue;
                    }
                    string key = String.Format("{0},{1},{2}", grid3d.gxid, grid3d.gyid, grid3d.gzid);

                    //存到字典中
                    if (curgrid.ContainsKey(key))//字典中已有该网格坐标记录，更新List，从curgrid里删除当前键值，添加更新后的
                    {
                        List<GridInfo> temp = new List<GridInfo>(curgrid[key]);
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
                //Debug.WriteLine("当前段" + n);
                for (int r = 0; r < n; r++)
                {

                    double xid, yid, zid;//为当前记录的小段大地坐标id

                    //路径传播方向
                    if (isAxisX)
                    {
                        xid = startx + r * ggridsize * dir;
                        yid = starty + r * ggridsize * dir * k;
                        zid = startz + r * ggridsize * kxy * dir;
                    }
                    else
                    {
                        xid = startx + r * ggridsize * dir / k;
                        yid = starty + r * ggridsize * dir;
                        zid = startz + r * ggridsize * kyz * dir;
                    }
                    tmp.X = xid;
                    tmp.Y = yid;
                    tmp.Z = zid;

                    if(!GridHelper.getInstance().PointXYZGrid(tmp, ref grid3d, ggridsize, ggridVsize))
                    {
                        continue;
                    }
                    string key = String.Format("{0},{1},{2}", grid3d.gxid, grid3d.gyid, grid3d.gzid);
                    double distance = Math.Sqrt(Math.Pow((xid - startx), 2) + Math.Pow(yid - starty, 2) + Math.Pow(zid - startz, 2)) + paths[i].distance;

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
                            receivePwr = emit / (Math.Pow(nata / (4 * Math.PI), 2)) * Math.Pow(distance, 2);
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
                    List<GridInfo> tmpnow = new List<GridInfo>(togrid[kvp.Key]);
                    togrid.Remove(kvp.Key);
                    foreach (GridInfo grid in kvp.Value)
                    {
                        tmpnow.Add(new GridInfo(grid));
                    }
                    togrid.Add(kvp.Key, new List<GridInfo>(tmpnow));
                    tmpnow.Clear();
                }
                //else
                //{
                //    togrid.Add(kvp.Key, new List<GridInfo>(kvp.Value));
                //}
            }
            curgrid.Clear();
            return;
        }
        #endregion
        /*-----------------------------------------------辅助函数--------------------------------------------------------------------*/
        #region 辅助函数
        
        /// <summary>
        /// 查看每个grid里覆盖的来自各个mp的数目
        /// </summary>
        /// <param name="grids"></param>
        /// <param name="lower"></param>
        private void AnalysisPathFrom(Dictionary<string, List<GridInfo>> grids,int lower)
        {
            Dictionary<string, Dictionary<string, int>> res = new Dictionary<string, Dictionary<string, int>>();
            foreach(KeyValuePair<string,List<GridInfo>> kvp in grids)
            {
                Dictionary<string, int> oner = new Dictionary<string, int>();
                foreach(GridInfo tmp in kvp.Value)
                {
                    if (oner.Count<1 || !oner.ContainsKey(tmp.cellid))
                    {
                        oner.Add(tmp.cellid, 1); 
                    }
                    else
                    {
                        oner[tmp.cellid] += 1;
                    }
                }
                Debug.WriteLine("ID" + kvp.Key);
                foreach (KeyValuePair<string,int> o in oner)
                {
                    if (o.Value > lower)
                    {
                        Debug.WriteLine("    from:" + o.Key + "     count:" + o.Value);
                    }
                    
                }
            }
            
        }
        
        /// <summary>
        /// 原始的grid_from求解方法，没有射线路径数约束
        /// </summary>
        /// <param name="grids"></param>
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
                if (max_from < _fromtmp.Count)
                {
                    max_from = _fromtmp.Count;
                }
                gridfrom.Add(kvp.Key, _fromtmp);
            }
            this.grid_from = new Dictionary<string, HashSet<string>>(gridfrom);
            gridfrom.Clear();
        }


        /// <summary>
        /// 栅格G只有接收到至少来自A的lower条射线，才判定G覆盖A发出的射线
        /// </summary>
        /// <param name="grids"></param>
        /// <param name="lower"></param>
        private void GetGrid_From(Dictionary<string, List<GridInfo>> grids,int lower)
        {
            Dictionary<string, HashSet<String>> gridfrom = new Dictionary<string, HashSet<string>>();
            if (grids == null || grids.Count < 1)
            {
                Debug.WriteLine("GetGrid_form 传入参数为空");
                return;
            }

            Dictionary<string, Dictionary<string, int>> res = new Dictionary<string, Dictionary<string, int>>();
            foreach (KeyValuePair<string, List<GridInfo>> kvp in grids)
            {
                Dictionary<string, int> oner = new Dictionary<string, int>();
                foreach (GridInfo tmp in kvp.Value)
                {
                    if (oner.Count < 1 || !oner.ContainsKey(tmp.cellid))
                    {
                        oner.Add(tmp.cellid, 1);
                    }
                    else
                    {
                        oner[tmp.cellid] += 1;
                    }
                }
                //Debug.WriteLine("ID" + kvp.Key);
                HashSet<string> _fromtmp = new HashSet<string>();
                foreach (KeyValuePair<string, int> o in oner)
                {
                    if(o.Value>lower)
                    {
                        _fromtmp.Add(o.Key);
                    }
                    //Debug.WriteLine("    from:" + o.Key + "     count:" + o.Value);
                }
                if (max_from < _fromtmp.Count)
                {
                    max_from = _fromtmp.Count;
                }
                gridfrom.Add(kvp.Key, new HashSet<string>(_fromtmp));
                //res.Add(kvp.Key, new Dictionary<string, int>(oner));
            }

            ///栅格-From-值
            //foreach (KeyValuePair<string, Dictionary<string, int>>  kvp in res)
            //{
            //    //遍历每个栅格经过的射线，获得其经过的射线来源
            //    HashSet<string> _fromtmp = new HashSet<string>();
            //    int i = 0;
            //    foreach (KeyValuePair<string, int> tmp in kvp.Value)//仅加入射线束数目超过lower的From
            //    {
            //        if (tmp.Value > lower && !_fromtmp.Contains(tmp.Key))
            //        {
            //            _fromtmp.Add(tmp.Key);
            //            i++;
            //        }
            //    }
            //    if (i != _fromtmp.Count)
            //    {
            //        Debug.WriteLine("问题出在这里");
            //    }
            //    else
            //    {
            //        Debug.WriteLine("FromCount:" + _fromtmp.Count);
            //    }
            //    if (max_from < _fromtmp.Count)
            //    {
            //        max_from = _fromtmp.Count;
            //    }
            //    gridfrom.Add(kvp.Key,new HashSet<string>( _fromtmp));
            //}
            Debug.WriteLine("Max_from" + max_from);
            this.grid_from = new Dictionary<string, HashSet<string>>(gridfrom);
            gridfrom.Clear();
        }

        /// <summary>
        /// 强信号射线的判定
        /// 需要计算功率
        /// </summary>
        /// <param name="grids"></param>
        /// <returns></returns>
        private Dictionary<string, long> GetGrid_StrongP(Dictionary<string, List<GridInfo>> grids,int way)
        {
            Dictionary<string, long> tmp1 = new Dictionary<string, long>();
            String[] keyArr = grids.Keys.ToArray<String>();
            for (int i = 0; i < keyArr.Length; i++)
            {
                long count = 0;
                List<GridInfo> tmp = grids[keyArr[i]];
                if (way == 0)//仅考虑初级射线
                {
                    for (int j = 0; j < tmp.Count; j++)
                    {
                        if (IsStrong(tmp[j]))
                        {
                            count++;
                        }
                    }
                }
                else if(way == 1)
                {
                    int dcount = 0, fcount = 0, rcount = 0;
                    for (int j = 0; j < tmp.Count; j++)
                    {
                        if(tmp[j].rayType==0 && dcount < 40)
                        {
                            dcount++;
                        }
                        else if((tmp[j].rayType==1|| tmp[j].rayType == 2) && fcount < 20)
                        {
                            fcount++;
                        }
                        else if((tmp[j].rayType == 3 || tmp[j].rayType == 4) && fcount < 10)
                        {
                            rcount++;
                        }
                        //进行功率转换，转换成dbm,
                    }
                    count = dcount + fcount + rcount;
                }
                else if (way == 2)
                {
                    //调用新的函数，传入参数（tmp，keyArr[i]）,返回对应的射线路径数
                    count = GetStrongPNum(tmp, keyArr[i], 5, 10, 15, 1);
                }
                
                tmp1.Add(keyArr[i], count);
            }
            return tmp1;
        }

        /// <summary>
        /// 计算一个大栅格里面小栅格的覆盖情况，统计覆盖比率及值
        /// </summary>
        /// <param name="infos">栅格中射线路径记录</param>
        /// <param name="key">栅格id</param>
        /// <param name="dslot">直射路径的小栅格大小</param>
        /// <param name="rslot">反射路径的小栅格大小</param>
        /// <param name="fslot">绕射路径的小栅格大小</param>
        /// <param name="num">每个小栅格最多可以记录多少条射线</param>
        /// <returns></returns>
        private int GetStrongPNum(List<GridInfo> infos,string key,int dslot,int rslot,int fslot,int num)
        {
            int count = 0;
            String[] ktmp = key.Split(',');

            int gx = 0, gy = 0, gz = 0;
            try
            {
                gx = Convert.ToInt32(ktmp[0]); gy = Convert.ToInt32(ktmp[1]); gz = Convert.ToInt32(ktmp[2]);
            }
            catch
            {
                Debug.WriteLine("栅格格式有误："+key);
                return 0;
            }
            Geometric.Point p = new Geometric.Point();
            Grid3D tmp1 = new Grid3D(gx, gy, gz);
            GridHelper.getInstance().PointGridXYZ(ref p, tmp1, ggridsize, ggridVsize);
            p.X -= ggridsize / 2;
            p.Y -= ggridsize / 2;
            p.Z -= ggridVsize / 2;

            Dictionary<string, List<GridInfo>> fromdirInfos = new Dictionary<string, List<GridInfo>>();//记录gridkey对应的每个fromname对应的直射路径信息
            Dictionary<string, List<GridInfo>> fromrefInfos = new Dictionary<string, List<GridInfo>>();//记录gridkey对应的每个fromname对应的反射路径信息
            Dictionary<string, List<GridInfo>> fromfInfos = new Dictionary<string, List<GridInfo>>();//记录gridkey对应的每个fromname对应的绕射路径信息
            foreach (GridInfo tmp in infos)
            {
                if (tmp.rayType == 0)
                {
                    if (fromdirInfos.ContainsKey(tmp.cellid))
                    {
                        List<GridInfo> ginfo = new List<GridInfo>(fromdirInfos[tmp.cellid]);
                        ginfo.Add(new GridInfo(tmp));
                        fromdirInfos.Remove(tmp.cellid);
                        fromdirInfos.Add(tmp.cellid, ginfo);
                    }
                    else
                    {
                        List<GridInfo> ginfo = new List<GridInfo>();
                        ginfo.Add(new GridInfo(tmp));
                        fromdirInfos.Add(tmp.cellid, ginfo);
                    }
                }
                else if (tmp.rayType == 1 || tmp.rayType == 2)
                {
                    if (fromrefInfos.ContainsKey(tmp.cellid))
                    {
                        List<GridInfo> ginfo = new List<GridInfo>(fromrefInfos[tmp.cellid]);
                        ginfo.Add(new GridInfo(tmp));
                        fromrefInfos.Remove(tmp.cellid);
                        fromrefInfos.Add(tmp.cellid, ginfo);
                    }
                    else
                    {
                        List<GridInfo> ginfo = new List<GridInfo>();
                        ginfo.Add(new GridInfo(tmp));
                        fromrefInfos.Add(tmp.cellid, ginfo);
                    }
                }
                else if (tmp.rayType == 3 || tmp.rayType == 4)
                {
                    if (fromfInfos.ContainsKey(tmp.cellid))
                    {
                        List<GridInfo> ginfo = new List<GridInfo>(fromfInfos[tmp.cellid]);
                        ginfo.Add(new GridInfo(tmp));
                        fromfInfos.Remove(tmp.cellid);
                        fromfInfos.Add(tmp.cellid, ginfo);
                    }
                    else
                    {
                        List<GridInfo> ginfo = new List<GridInfo>();
                        ginfo.Add(new GridInfo(tmp));
                        fromfInfos.Add(tmp.cellid, ginfo);
                    }
                }
                
            }

            //确保每个字典都有记录，避免出现键不存在的情况
            foreach(string g_from in grid_from[key])
            {
                if (!fromdirInfos.ContainsKey(g_from))
                {
                    fromdirInfos.Add(g_from, new List<GridInfo>());
                }
                if (!fromrefInfos.ContainsKey(g_from))
                {
                    fromrefInfos.Add(g_from, new List<GridInfo>());
                }
                if (!fromfInfos.ContainsKey(g_from))
                {
                    fromfInfos.Add(g_from, new List<GridInfo>());
                }
            }
            int dirn = 30 / dslot, refn = 30 / rslot, feon = 30 / fslot;
            if (30 % dslot != 0)
            {
                dirn++;
            }
            if (30 % rslot != 0)
            {
                refn++;
            }
            if (30 % fslot != 0)
            {
                feon++;
            }
            int d2n = dirn * dirn;
            int r2n = refn * refn;
            int f2n = feon * feon;
            int dn = dirn * dirn * dirn;
            int rn = refn * refn * refn;
            int fn = feon * feon * feon;
            ///定义小栅格的数组，格式为xg+yg*dirn+zg*dirn*dirn
            int[] darr = new int[dn];
            int[] rarr = new int[rn];
            int[] farr = new int[fn];
            for(int i = 0; i < dn; i++)
            {
                darr[i] = 0;
            }
            for(int i = 0; i < rn; i++)
            {
                rarr[i] = 0;
            }
            for(int i = 0; i < fn; i++)
            {
                farr[i] = 0;
            }

            Dictionary<string, int> ansdic = new Dictionary<string, int>();
            //对栅格覆盖的每一个栅格，计算其小部分的
            foreach (string ckey in grid_from[key])
            {
                int dcou = 0, dncou = 0, rcou = 0, rncou = 0, fcou = 0, fncou = 0;
                foreach(GridInfo g in fromdirInfos[ckey])
                {
                    int xg = (int)(g.x - p.X) / dslot;
                    int yg = (int)(g.y - p.Y) / dslot;
                    int zg = (int)(g.z - p.Z) / dslot;
                    if (xg == dirn && (g.x - p.X) >= ggridsize) xg = dirn - 1;
                    if (yg == dirn && (g.y - p.Y) >= ggridsize) yg = dirn - 1;
                    if (zg == dirn && (g.z - p.Z) >= ggridVsize) zg = dirn - 1;
                    if (xg < 0) xg = 0;
                    if (yg < 0) yg = 0;
                    if (zg < 0) zg = 0;
                    int index = xg + yg * dirn + zg * d2n;
                    int curcou = darr[index];
                    if (curcou == 0)
                    {
                        dcou++;
                    }
                    if (curcou < num)
                    {
                        darr[index]++;
                        dncou++;
                    }
                }
                foreach (GridInfo g in fromrefInfos[ckey])
                {
                    int xg = (int)(g.x - p.X) / rslot;
                    int yg = (int)(g.y - p.Y) / rslot;
                    int zg = (int)(g.z - p.Z) / rslot;
                    if (xg == refn && (g.x - p.X) >= ggridsize) xg = refn - 1;
                    if (yg == refn && (g.y - p.Y) >= ggridsize) yg = refn - 1;
                    if (zg == refn && (g.z - p.Z) >= ggridVsize) zg = refn - 1;
                    int index = xg + yg * refn + zg * r2n;
                    
                    int curcou = rarr[index];
                    if (curcou == 0)
                    {
                        rcou++;
                    }
                    if (curcou < num)
                    {
                        rarr[index]++;
                        rncou++;
                    }
                }
                foreach (GridInfo g in fromfInfos[ckey])
                {
                    int xg = (int)(g.x - p.X) / fslot;
                    int yg = (int)(g.y - p.Y) / fslot;
                    int zg = (int)(g.z - p.Z) / fslot;
                    if (xg == feon && (g.x - p.X) >= ggridsize) xg = feon - 1;
                    if (yg == feon && (g.y - p.Y) >= ggridsize) yg = feon - 1;
                    if (zg == feon && (g.z - p.Z) >= ggridVsize) zg = feon - 1;
                    int index = xg + yg * feon + zg * f2n;
                    
                    int curcou = farr[index];
                    if (curcou == 0)
                    {
                        fcou++;
                    }
                    if (curcou < num)
                    {
                        farr[index]++;
                        fncou++;
                    }
                }
                int sumcount = dncou + rncou + fncou;
                ansdic.Add(ckey, sumcount);
                count += sumcount;
                for (int i = 0; i < dn; i++)
                {
                    darr[i] = 0;
                }
                for (int i = 0; i < rn; i++)
                {
                    rarr[i] = 0;
                }
                for (int i = 0; i < fn; i++)
                {
                    farr[i] = 0;
                }
            }
            return count;
        }
        /// <summary>
        /// 计算强信号射线占比
        /// </summary>
        /// <param name="grids"></param>
        /// <returns></returns>
        private Dictionary<string, double> ComputeRateO(Dictionary<string, List<GridInfo>> grids)
        {
            Dictionary<string, double> ans = new Dictionary<string, double>();
            foreach (KeyValuePair<string, List<GridInfo>> kvp in grids)
            {
                int count = 0;
                foreach (GridInfo tmp in kvp.Value)
                {
                    if (IsStrong(tmp))
                    {
                        count++;
                    }
                }
                ans.Add(kvp.Key, (double)count / kvp.Value.Count);
            }
            return ans;
        }

        /// <summary>
        /// 定义强信号射线规则的函数
        /// </summary>
        /// <param name="ginf"></param>
        /// <returns></returns>
        private bool IsStrong(GridInfo ginf)
        {
            if (ginf.pathloss<150&&(ginf.rayType == 0 || ginf.rayType == 1)) 
            {
                return true;
            }
            return false;
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
                        //Debug.WriteLine("接收功率：" + kvp.Value[i].recP + "     原始功率：" + kvp.Value[i].emit);
                        sum += kvp.Value[i].pathloss;
                        //Debug.Write("   pathloss:" + kvp.Value[i].pathloss + "   cellname    " + kvp.Value[i].cellid + "   trajID:" + kvp.Value[i].trajID + "-" + kvp.Value[i].raylevel + "-" + kvp.Value[i].rayType);
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
        #endregion
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
            Dictionary<string, List<GridInfo>> tmp = new Dictionary<string, List<GridInfo>>(togrid);
            Dictionary<string, List<GridInfo>> pre = new Dictionary<string, List<GridInfo>>(tmp);
            //优先过滤小区
            int count = tmp.Count;
            int itera = 500;
            int from_thld = Convert.ToInt32(this.max_from * param_1);
            while (tmp.Count > threshold * 2)
            {
                String[] keyArr = tmp.Keys.ToArray<String>();
                for (int i = 0; i < keyArr.Length; i++)
                {
                    string key = keyArr[i];
                    if (grid_from[key].Count < from_thld)
                    {
                        tmp.Remove(keyArr[i]);
                    }
                }
                if (tmp.Count < threshold_1)
                {
                    tmp.Clear();
                    tmp = new Dictionary<string, List<GridInfo>>(pre);
                    from_thld--;
                }
                else if(tmp.Count < threshold * 2)
                {
                    break;
                }
                else
                {
                    pre.Clear();
                    pre = new Dictionary<string, List<GridInfo>>(tmp);
                    from_thld++;
                }
            }
            
            
            pre = new Dictionary<string, List<GridInfo>>(tmp);
            #region 之前栅格From条件的代码

            /*while ((count > threshold + 100 || count < 5) && itera-- > 0)
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
            }*/
            #endregion
            Debug.WriteLine("一层过滤结果集大小" + tmp.Count);
            Dictionary<string, double> ratRec = ComputeRateO(pre);
            count = OPGetFiltrateGrid(param_2, lownum, tmp, ratRec);
            
            //count = GetFiltrateGrid(param_2, lownum, tmp);
            itera = 500;
            bool firstF = true;
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
                    if (firstF)
                    {
                        itera = 2;
                        firstF = false;
                    }
                }
                else
                {
                    break;
                }
                //count =  GetFiltrateGrid(param_2, lownum, tmp);
                count = OPGetFiltrateGrid(param_2, lownum, tmp, ratRec);
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
            Debug.WriteLine("二层过滤结果集大小" + togrid.Count);
            //清空grid_from中的多余部分
            Dictionary<string, HashSet<string>> tmpFrom = new Dictionary<string, HashSet<string>>();
            foreach (KeyValuePair<string, List<GridInfo>> kvp in tmp)
            {
                tmpFrom.Add(kvp.Key, new HashSet<string>(grid_from[kvp.Key]));
            }
            grid_from.Clear();
            grid_from = new Dictionary<string, HashSet<string>>(tmpFrom);


            //int count_3= FiliteGridByVitalCell(togrid, 3);
            //Debug.WriteLine("三层过滤结果集大小" + togrid.Count+"    过滤个数："+count_3);

            
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
            if (cellPwr.Count < 2) return 0;
            int lower = Convert.ToInt16(low * cellPwr.Count);

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


        private int FiliteGridByVitalCell(Dictionary<string, List<GridInfo>> curTogrid,int k)
        {
            //Dictionary<string, double> cellPwr = new Dictionary<string, double>();

            //double curMax = double.MinValue, nextMax = double.MinValue;
            //Debug.WriteLine("源点信息");
            //foreach (DataRow tb in tbsourceInfo.Rows)
            //{
            //    string cellid = Convert.ToString(tb["CI"]);
            //    double pwr = Convert.ToDouble(tb["ReceivePW"]);//单位是w
            //    cellPwr.Add(cellid, pwr);
            //    if (curMax < pwr)
            //    {
            //        curMax = pwr;
            //    }
            //    Debug.WriteLine("source:" + cellid + "      pwr:" + pwr);
            //}
            //List<String> VitalMP = new List<string>();
            //Debug.WriteLine("前k个信息");
            //for(int i = 0; i < k; i++)
            //{
            //    nextMax = double.MinValue;
            //    bool isfind = false;
            //    foreach (KeyValuePair<string,double> kvp in cellPwr)
            //    {
            //        if (!isfind && kvp.Value == curMax)
            //        {
            //            VitalMP.Add(kvp.Key);
            //            Debug.WriteLine("id"+i+"source:" + kvp.Key + "      pwr:" + kvp.Value);
            //            isfind = true;
            //        }
            //        else if(isfind && kvp.Value == curMax)
            //        {
            //            if (++i < k)//重复的情况
            //            {
            //                Debug.WriteLine("重复：id" + i + "source:" + kvp.Key + "      pwr:" + kvp.Value);
            //                VitalMP.Add(kvp.Key);
            //            }
            //        }
            //        else if(kvp.Value< curMax && kvp.Value > nextMax)
            //        {
            //            nextMax = kvp.Value;
            //        }
            //    }
            //    curMax = nextMax;
            //}
            //Debug.WriteLine("前k个List信息");
            //for (int i = 0; i < VitalMP.Count; i++)
            //{
            //    Debug.WriteLine(VitalMP[i]);
            //}

            int filitCount = 0;
            foreach(KeyValuePair<string,HashSet<string>> kvp in grid_from)
            {
                for (int i = 0; i < k; i++)
                {
                    if (!kvp.Value.Contains(VitalMP[i]))
                    {
                        filitCount++;
                        curTogrid.Remove(kvp.Key);
                        break;
                    }
                }
            }
            return filitCount;
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

        public int OPGetFiltrateGrid(double rate, int lownum, Dictionary<string, List<GridInfo>> grids, Dictionary<string, double> ratRec)
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
                if (ratRec[keyArr[i]]<rate)
                {
                    grids.Remove(keyArr[i]);
                }
            }
            return grids.Count;
        }
        #endregion

        /*------------------------------------------------评估候选点 --------------------------------------------------------------------*/
        #region 评估候选干扰点

        public double Evaluate(List<GridInfo> infos, int l,int kn,int way)
        {
            //求解前kn个信号较强的路测点对应的平均综合干扰差值
            Dictionary<string, List<double>> cell_pathloss = new Dictionary<string, List<double>>();
            for(int i = 0; i < kn; i++)
            {
                cell_pathloss.Add(VitalMP[i], new List<double>());
            }
            
            //计算每个路测点的前l个直射线到达信号
            foreach (GridInfo grid in infos)
            {
                if (cell_pathloss.ContainsKey(grid.cellid))
                {
                    List<double> tmp = new List<double>(cell_pathloss[grid.cellid]);
                    if (tmp.Count < l && (grid.rayType == 0|| grid.rayType == 1))//仅取l位数
                    {
                        tmp.Add(grid.pathloss);
                        cell_pathloss.Remove(grid.cellid);
                        cell_pathloss.Add(grid.cellid, tmp);
                    }
                }
            }
            Dictionary<string, double> cellRecPwr = new Dictionary<string, double>();
            foreach (KeyValuePair<string, List<double>> kvp in cell_pathloss)//求直射线的平均损耗
            {
                double sumv = 0.0;
                foreach (double v in kvp.Value)
                {
                    sumv += v;
                }
                Debug.WriteLine("ID" + kvp.Key + "    sumv:" + sumv + "     count:" + kvp.Value.Count);
                cellRecPwr.Add(kvp.Key, sumv / kvp.Value.Count);
            }
            cell_pathloss.Clear();
            
            String[] keyArr = cellRecPwr.Keys.ToArray<String>();
            double sum = 0.0;
            for (int i = 0; i < keyArr.Length - 1; i++)
            {
                string cellidA = keyArr[i];
                double pwrA = cellPwr[cellidA];//接收功率

                for (int j = i + 1; j < keyArr.Length; j++)
                {
                    double pwrB = cellPwr[keyArr[j]];
                    // Debug.WriteLine(cellnameA+" countA:" + k+"     "+cellnameB+"   countB:"+k);
                    sum += Math.Abs(10 * (Math.Log10(pwrA) + 3) - 10 * (Math.Log10(pwrB) + 3) + cellRecPwr[keyArr[i]] - cellRecPwr[keyArr[j]]);
                }
            }
            return sum / (kn * (kn - 1) / 2);
        }

        public double Evaluate(string key,List<GridInfo> infos, int L, int kn)
        {
            //求解前kn个信号较强的路测点对应的平均综合干扰差值
            Dictionary<string, List<double>> cell_pathloss = new Dictionary<string, List<double>>();

            for (int i = 0,j=0; i < kn && j<VitalMP.Count;j++ )
            {
                if (grid_from[key].Contains(VitalMP[j])){
                    cell_pathloss.Add(VitalMP[j], new List<double>());
                    i++;
                }
            }

            //计算每个路测点的前l个直射线到达信号
            foreach (GridInfo grid in infos)
            {
                if (cell_pathloss.ContainsKey(grid.cellid))
                {
                    List<double> tmp = new List<double>(cell_pathloss[grid.cellid]);
                    //if (tmp.Count < L && (grid.rayType == 0 || grid.rayType == 1))//仅取l位数
                    if (tmp.Count < L )
                    {
                        tmp.Add(grid.pathloss);
                        cell_pathloss.Remove(grid.cellid);
                        cell_pathloss.Add(grid.cellid, tmp);
                    }
                }
            }

            Dictionary<string, double> cellRecPwr = new Dictionary<string, double>();
            foreach (KeyValuePair<string, List<double>> kvp in cell_pathloss)//求直射线的平均损耗
            {
                double sumv = 0.0;
                foreach (double v in kvp.Value)
                {
                    sumv += v;
                }
                //Debug.WriteLine("ID" + kvp.Key + "    sumv:" + sumv + "     count:" + kvp.Value.Count);
                cellRecPwr.Add(kvp.Key, sumv / kvp.Value.Count);
            }
            cell_pathloss.Clear();

            String[] keyArr = cellRecPwr.Keys.ToArray<String>();
            double sum = 0.0;
            for (int i = 0; i < keyArr.Length - 1; i++)
            {
                string cellidA = keyArr[i];
                double pwrA = cellPwr[cellidA];//接收功率

                for (int j = i + 1; j < keyArr.Length; j++)
                {
                    double pwrB = cellPwr[keyArr[j]];
                    // Debug.WriteLine(cellnameA+" countA:" + k+"     "+cellnameB+"   countB:"+k);
                    sum += Math.Abs(10 * (Math.Log10(pwrA) + 3) - 10 * (Math.Log10(pwrB) + 3) + cellRecPwr[keyArr[i]] - cellRecPwr[keyArr[j]]);
                }
            }
            return sum / (kn * (kn - 1) / 2);
        }



        /// <summary>
        /// 计算一个栅格里的两两小区之间功率损耗差值总和，单位dbm
        /// </summary>
        /// <param name="infos">栅格信息</param>
        /// <param name="l">选取前l个直射线求平均进行计算</param>
        /// <returns></returns>
        public double Evaluate(List<GridInfo> infos, int l)
        {
            Dictionary<string, List<double>> cell_pathloss = new Dictionary<string, List<double>>();

            //提取<cellname,List(pathloss)>
            foreach (GridInfo grid in infos)
            {
                if (cell_pathloss.ContainsKey(grid.cellid))
                {
                    List<double> tmp = new List<double>(cell_pathloss[grid.cellid]);
                    if (tmp.Count < l && grid.rayType == 0)//仅取l位数
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

            //String[] keyArr = cell_pathloss.Keys.ToArray<String>();

            Dictionary<string, double> cellRecPwr = new Dictionary<string, double>();
            foreach(KeyValuePair<string, List<double>> kvp in cell_pathloss)//求直射线的平均损耗
            {
                double sumv = 0.0;
                foreach(double v in kvp.Value)
                {
                    sumv += v;
                }
                cellRecPwr.Add(kvp.Key, sumv / kvp.Value.Count);

            }

            String[] keyArr = cellRecPwr.Keys.ToArray<String>();
            double sum = 0.0;
            for (int i = 0; i < keyArr.Length - 1; i++)
            {
                string cellidA = keyArr[i];
                List<double> tmpA = cell_pathloss[cellidA];
                double pwrA = cellPwr[cellidA];//接收功率

                for (int j = i + 1; j < keyArr.Length; j++)
                {
                    double pwrB = cellPwr[keyArr[j]];
                    
                    // Debug.WriteLine(cellnameA+" countA:" + k+"     "+cellnameB+"   countB:"+k);
                    sum += Math.Abs(10 * (Math.Log10(pwrA) + 3) - 10 * (Math.Log10(pwrB) + 3) + cellRecPwr[keyArr[i]] - cellRecPwr[keyArr[j]]);
                }
            }
            return sum/(keyArr.Length*(keyArr.Length-1)/2);
        }

        /// <summary>
        /// 评估函数，功率占比
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
                double p = Evaluate(kvp.Value, 10);
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
                double p = Evaluate(kvp.Value, 10);
                Ppathloss.Add(kvp.Key, p);
                if (candidate_grid.Count == 1) return kvp.Key;
            }

            Dictionary<string, long> grid_strongP = this.GetGrid_StrongP(candidate_grid,0);

            string maxKey = "";
            double max = 0;
            Hashtable ht = new Hashtable();
            ht["cellname"] = this.virname;
            DataTable source = IbatisHelper.ExecuteQueryForDataTable("GetVirSourceAllInfo", ht);
            Geometric.Point point= new Point();
            bool isCompare = true;
            if (source.Rows.Count < 1)
            {
                Debug.WriteLine("tbRealSource中无数据");
                isCompare = false;
            }
            else
            {
                point = new Geometric.Point(Convert.ToDouble(source.Rows[0]["x"]), Convert.ToDouble(source.Rows[0]["y"]), 0);
            }
            
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
                Debug.Write("Grid_:" + k + "    rate:" + value + "    from:" + grid_from[k].Count + " 综合干扰差值：" + Ppathloss[k]);
                if (isCompare)
                {
                    string[] str = k.Split(',');
                    if (str.Length != 3)
                    {
                        Debug.WriteLine("key 分解错误");
                    }
                    else
                    {
                        Geometric.Point p = new Geometric.Point();
                        Grid3D tmp1 = new Grid3D(Convert.ToInt32(str[0]), Convert.ToInt32(str[1]), Convert.ToInt32(str[2]));
                        GridHelper.getInstance().PointGridXYZ(ref p, tmp1, ggridsize, ggridVsize);
                        double dis = Geometric.Point.distance(point, p);
                        Debug.WriteLine( "     与源点距离：" + dis + "   路径数" + kvp.Value.Count + "    栅格功率：dbm" + 10 * (Math.Log10(grid_pwr[kvp.Key]) + 3) + "     栅格平均路径损耗：" + grid_pathloss[kvp.Key]);
                    }
                }
            }
            return maxKey;

        }


        /// <summary>
        /// 评估函数，指定最终的目标个数，并入库
        /// </summary>
        /// <param name="candidate_grid"></param>
        /// <param name="pathnum"></param>
        /// <param name="ratioP"></param>
        /// <param name="from"></param>
        /// <param name="kn"></param>
        /// <returns></returns>
        public string EvaluateCombineAll(Dictionary<string, List<GridInfo>> candidate_grid, double pathnum, double ratioP, double from, int kn)
        {
            if (candidate_grid.Count < 1) return null;

            Dictionary<string, double> Ppathloss = new Dictionary<string, double>();//存储栅格中两两小区功率损耗差值
            
            foreach (KeyValuePair<string, List<GridInfo>> kvp in candidate_grid)
            {
                double p = Evaluate(kvp.Key,kvp.Value, 10,3);
                Ppathloss.Add(kvp.Key, p);
                if (candidate_grid.Count == 1) return kvp.Key;
            }

            Dictionary<string, long> grid_strongP = this.GetGrid_StrongP(candidate_grid,2);

            string maxKey = "";
            double max = 0;
            Hashtable ht = new Hashtable();
            ht["cellname"] = this.virname;
            DataTable source = IbatisHelper.ExecuteQueryForDataTable("GetVirSourceAllInfo", ht);
            Geometric.Point point = new Point();
            bool isCompare = true;
            if (source.Rows.Count < 1)
            {
                Debug.WriteLine("tbRealSource中无数据");
                isCompare = false;
            }
            else
            {
                point = new Geometric.Point(Convert.ToDouble(source.Rows[0]["x"]), Convert.ToDouble(source.Rows[0]["y"]), 0);
            }
            //AnalysisPathFrom(togrid,10);

            MinHeap<LocResult> rec = new MinHeap<LocResult>(kn);
            MinHeap<LocPResult> recP = new MinHeap<LocPResult>(kn);
            MinHeap<LocPpResult> recPp = new MinHeap<LocPpResult>(kn);
            MaxHeap<LocPpResult> recminPp = new MaxHeap<LocPpResult>(kn);
            foreach (KeyValuePair<string, List<GridInfo>> kvp in candidate_grid)
            {
                string k = kvp.Key;
                //计算值
                double value = grid_from[k].Count * from + grid_strongP[k] * 0.05 * pathnum - Ppathloss[k] * 0.2 * ratioP;

                if (value > max)
                {
                    maxKey = k;
                    max = value;
                }
                Debug.Write("Grid_:" + k + "    rate:" + value + "    from:" + grid_from[k].Count + " 综合干扰差值：" + Ppathloss[k] + "   路径数" + kvp.Value.Count+"   强信号射线数"+ grid_strongP[k]);
                double dis = 0;
                string[] str = k.Split(',');
                if (str.Length != 3)
                {
                    Debug.WriteLine("key 分解错误");
                    continue;
                }
                if (isCompare)
                {
                    Geometric.Point p = new Geometric.Point();
                    Grid3D tmp1 = new Grid3D(Convert.ToInt32(str[0]), Convert.ToInt32(str[1]),0);
                    GridHelper.getInstance().PointGridXYZ(ref p, tmp1, ggridsize, ggridVsize);
                    dis = Geometric.Point.distance(point, p);
                    Debug.WriteLine("     与源点距离：" + dis + "    栅格功率：dbm" + 10 * (Math.Log10(grid_pwr[kvp.Key]) + 3) + "     栅格平均路径损耗：" + grid_pathloss[kvp.Key]);
                    
                }
                if (rec.isFull())//已添加k个
                {
                    if (value > rec.GetMinItem().rp)
                    {
                        LocResult loc = new LocResult(Convert.ToInt32(str[0]), Convert.ToInt32(str[1]), Convert.ToInt32(str[2]), value, grid_from[k].Count, Ppathloss[k], dis, kvp.Value.Count);
                        rec.DeteleMinItem();
                        rec.AddItem(loc);
                    }
                }
                else
                {
                    LocResult loc = new LocResult(Convert.ToInt32(str[0]), Convert.ToInt32(str[1]), Convert.ToInt32(str[2]), value, grid_from[k].Count, Ppathloss[k], dis, kvp.Value.Count);
                    rec.AddItem(loc);
                }

                if (recP.isFull())//已添加k个
                {
                    if (grid_strongP[k] > recP.GetMinItem().snum)
                    {
                        LocPResult loc = new LocPResult(Convert.ToInt32(str[0]), Convert.ToInt32(str[1]), Convert.ToInt32(str[2]), value, grid_from[k].Count, Ppathloss[k], dis, (int)grid_strongP[k]);

                        recP.DeteleMinItem();
                        recP.AddItem(loc);
                    }
                }
                else
                {
                    LocPResult loc = new LocPResult(Convert.ToInt32(str[0]), Convert.ToInt32(str[1]), Convert.ToInt32(str[2]), value, grid_from[k].Count, Ppathloss[k], dis, (int)grid_strongP[k]);
                    recP.AddItem(loc);
                }

                if (recPp.isFull())//已添加k个
                {
                    if (Ppathloss[k] > recPp.GetMinItem().subPloss)
                    {
                        LocPpResult loc = new LocPpResult(Convert.ToInt32(str[0]), Convert.ToInt32(str[1]), Convert.ToInt32(str[2]), value, grid_from[k].Count, Ppathloss[k], dis, (int)grid_strongP[k]);

                        recPp.DeteleMinItem();
                        recPp.AddItem(loc);
                    }
                }
                else
                {
                    LocPpResult loc = new LocPpResult(Convert.ToInt32(str[0]), Convert.ToInt32(str[1]), Convert.ToInt32(str[2]), value, grid_from[k].Count, Ppathloss[k], dis, (int)grid_strongP[k]);
                    recPp.AddItem(loc);
                }

                if (recminPp.isFull())
                {
                    if (Ppathloss[k] < recminPp.GetMaxItem().subPloss)
                    {
                        LocPpResult loc = new LocPpResult(Convert.ToInt32(str[0]), Convert.ToInt32(str[1]), Convert.ToInt32(str[2]), value, grid_from[k].Count, Ppathloss[k], dis, (int)grid_strongP[k]);

                        recminPp.DeteleMaxItem();
                        recPp.AddItem(loc);
                    }
                }
                else
                {
                    LocPpResult loc = new LocPpResult(Convert.ToInt32(str[0]), Convert.ToInt32(str[1]), Convert.ToInt32(str[2]), value, grid_from[k].Count, Ppathloss[k], dis, (int)grid_strongP[k]);
                    recminPp.AddItem(loc);
                }


            }
            int count = 0;

            Debug.WriteLine("路径数值最大前"+kn+ "个结果集，由路径数大小从小到大输出");
            while (!recP.isEmpty())
            {
                LocPResult loc = recP.GetMinItem();
                Debug.WriteLine(count + " grid:" + loc.gridx + "  " + loc.gridy + "   " + loc.gridz + "    rate:" + loc.rp + "    dis:" + loc.dis + "    from:" + loc.fromnum + "    综合干扰差值：" + loc.subPloss + "   路径数" + loc.snum);
                recP.DeteleMinItem();
            }

            Debug.WriteLine("综合干扰差值最大前" + kn + "个结果集，由差值大小从小到大输出");
            while (!recPp.isEmpty())
            {
                LocPpResult loc = recPp.GetMinItem();
                Debug.WriteLine(count + " grid:" + loc.gridx + "  " + loc.gridy + "   " + loc.gridz + "    rate:" + loc.rp + "    dis:" + loc.dis + "    from:" + loc.fromnum + "    综合干扰差值：" + loc.subPloss + "   路径数" + loc.snum);
                recPp.DeteleMinItem();
            }
            Debug.WriteLine("综合干扰差值最小前" + kn + "个结果集，由差值大小从大到小输出");
            while (!recminPp.isEmpty())
            {
                LocPpResult loc = recminPp.GetMaxItem();
                Debug.WriteLine(count + " grid:" + loc.gridx + "  " + loc.gridy + "   " + loc.gridz + "    rate:" + loc.rp + "    dis:" + loc.dis + "    from:" + loc.fromnum + "    综合干扰差值：" + loc.subPloss + "   路径数" + loc.snum);
                recminPp.DeteleMaxItem();
            }

            Debug.WriteLine(kn + "个结果集，由评分从小到大输出");
            while (!rec.isEmpty())
            {
                LocResult loc = rec.GetMinItem();
                Debug.WriteLine(count + " grid:" + loc.gridx + "  " + loc.gridy + "   " + loc.gridz + "    rate:" + loc.rp + "    dis:" + loc.dis + "    from:" + loc.fromnum + "    综合干扰差值：" + loc.subPloss + "   路径数" + loc.snum);
                
                Geometric.Point p = new Geometric.Point();
                Grid3D tmp1 = new Grid3D(loc.gridx, loc.gridy, loc.gridz);
                GridHelper.getInstance().PointGridXYZ(ref p, tmp1, ggridsize, ggridVsize);
                DataRow dr = tb.NewRow();
                
                dr["Version"] = this.virname;
                dr["ansID"] = kn--;
                dr["x"] = p.X;
                dr["y"] = p.Y;
                dr["z"] = p.Z;
                PointConvertByProj.Instance.GetGeoPoint(p);
                dr["Longitude"] = p.X;
                dr["Latitude"] = p.Y;
                tb.Rows.Add(dr);
                rec.DeteleMinItem();
            }
            WriteToLocResult();
            return maxKey;

        }

        /// <summary>
        /// 归一化定位
        /// </summary>
        /// <param name="candidate_grid"></param>
        /// <param name="pathnum"></param>
        /// <param name="ratioP"></param>
        /// <param name="from"></param>
        /// <param name="kn"></param>
        /// <returns></returns>
        public string EvaluateCombineAllNor(Dictionary<string, List<GridInfo>> candidate_grid, double pathnum, double ratioP, double from, int kn)
        {
            if (candidate_grid.Count < 1) return null;

            Dictionary<string, double> Ppathloss = new Dictionary<string, double>();//存储栅格中两两小区功率损耗差值
            double minPl = double.MaxValue, maxPl = double.MinValue;
            foreach (KeyValuePair<string, List<GridInfo>> kvp in candidate_grid)
            {
                double p = Evaluate(kvp.Key, kvp.Value, 10, 3);
                if (p > maxPl) { maxPl = p; }
                if (p < minPl) { minPl = p; }
                Ppathloss.Add(kvp.Key, p);
                if (candidate_grid.Count == 1) return kvp.Key;
            }

            Dictionary<string, long> grid_strongP = this.GetGrid_StrongP(candidate_grid, 1);
            double minSP = double.MaxValue, maxSP = double.MinValue, minF = double.MaxValue, maxF = double.MinValue;
            foreach (KeyValuePair<string, long> kvp in grid_strongP)
            {
                if (kvp.Value > maxSP) { maxSP = kvp.Value; }
                if (kvp.Value < minSP) { minSP = kvp.Value; }
                if (grid_from[kvp.Key].Count > maxF) { maxF = grid_from[kvp.Key].Count; }
                if (grid_from[kvp.Key].Count < minF) { minF = grid_from[kvp.Key].Count; }
            }

            string maxKey = "";
            double max = double.MinValue;

            Hashtable ht = new Hashtable();
            ht["cellname"] = this.virname;
            DataTable source = IbatisHelper.ExecuteQueryForDataTable("GetVirSourceAllInfo", ht);
            Geometric.Point point = new Point();//干扰源位置（若已知）
            bool isCompare = true;
            if (source.Rows.Count < 1)
            {
                Debug.WriteLine("tbRealSource中无数据");
                isCompare = false;
            }
            else
            {
                point = new Geometric.Point(Convert.ToDouble(source.Rows[0]["x"]), Convert.ToDouble(source.Rows[0]["y"]), 0);
            }
            //AnalysisPathFrom(togrid,10);

            MinHeap<LocResult> rec = new MinHeap<LocResult>(kn);
            MinHeap<LocPResult> recP = new MinHeap<LocPResult>(kn);
            MinHeap<LocPpResult> recPp = new MinHeap<LocPpResult>(kn);
            MaxHeap<LocPpResult> recminPp = new MaxHeap<LocPpResult>(kn);
            foreach (KeyValuePair<string, List<GridInfo>> kvp in candidate_grid)
            {
                string k = kvp.Key;
                //计算值
                double valueF = 0, valueSP = 0, valuePL = 0;
                if (maxF > minF)
                {
                    valueF = (grid_from[k].Count - minF) / (maxF - minF);
                }
                if (maxSP > minSP)
                {
                    valueSP = (grid_strongP[k] - minSP) / (maxSP - minSP);
                }
                if (maxPl > minPl)
                {
                    valuePL = (maxPl- Ppathloss[k]) / (maxPl - minPl);
                }
                double value = valueF * from + valueSP * pathnum - valuePL * ratioP;

                if (value > max)
                {
                    maxKey = k;
                    max = value;
                }
                Debug.Write("Grid_:" + k + "    rate:" + value + "    from:" + grid_from[k].Count + " 综合干扰差值：" + Ppathloss[k] + "   路径数" + kvp.Value.Count + "   强信号射线数" + grid_strongP[k]);
                double dis = 0;
                string[] str = k.Split(',');
                if (str.Length != 3)
                {
                    Debug.WriteLine("key 分解错误");
                    continue;
                }
                if (isCompare)
                {
                    Geometric.Point p = new Geometric.Point();
                    Grid3D tmp1 = new Grid3D(Convert.ToInt32(str[0]), Convert.ToInt32(str[1]), 0);
                    GridHelper.getInstance().PointGridXYZ(ref p, tmp1, ggridsize, ggridVsize);
                    dis = Geometric.Point.distance(point, p);
                    Debug.WriteLine("     与源点距离：" + dis + "    栅格功率：dbm" + 10 * (Math.Log10(grid_pwr[kvp.Key]) + 3) + "     栅格平均路径损耗：" + grid_pathloss[kvp.Key]);

                }
                if (rec.isFull())//已添加k个
                {
                    if (value > rec.GetMinItem().rp)
                    {
                        LocResult loc = new LocResult(Convert.ToInt32(str[0]), Convert.ToInt32(str[1]), Convert.ToInt32(str[2]), value, grid_from[k].Count, Ppathloss[k], dis, kvp.Value.Count);
                        rec.DeteleMinItem();
                        rec.AddItem(loc);
                    }
                }
                else
                {
                    LocResult loc = new LocResult(Convert.ToInt32(str[0]), Convert.ToInt32(str[1]), Convert.ToInt32(str[2]), value, grid_from[k].Count, Ppathloss[k], dis, kvp.Value.Count);
                    rec.AddItem(loc);
                }

                if (recP.isFull())//已添加k个
                {
                    if (grid_strongP[k] > recP.GetMinItem().snum)
                    {
                        LocPResult loc = new LocPResult(Convert.ToInt32(str[0]), Convert.ToInt32(str[1]), Convert.ToInt32(str[2]), value, grid_from[k].Count, Ppathloss[k], dis, (int)grid_strongP[k]);

                        recP.DeteleMinItem();
                        recP.AddItem(loc);
                    }
                }
                else
                {
                    LocPResult loc = new LocPResult(Convert.ToInt32(str[0]), Convert.ToInt32(str[1]), Convert.ToInt32(str[2]), value, grid_from[k].Count, Ppathloss[k], dis, (int)grid_strongP[k]);
                    recP.AddItem(loc);
                }

                if (recPp.isFull())//已添加k个
                {
                    if (Ppathloss[k] > recPp.GetMinItem().subPloss)
                    {
                        LocPpResult loc = new LocPpResult(Convert.ToInt32(str[0]), Convert.ToInt32(str[1]), Convert.ToInt32(str[2]), value, grid_from[k].Count, Ppathloss[k], dis, (int)grid_strongP[k]);

                        recPp.DeteleMinItem();
                        recPp.AddItem(loc);
                    }
                }
                else
                {
                    LocPpResult loc = new LocPpResult(Convert.ToInt32(str[0]), Convert.ToInt32(str[1]), Convert.ToInt32(str[2]), value, grid_from[k].Count, Ppathloss[k], dis, (int)grid_strongP[k]);
                    recPp.AddItem(loc);
                }

                if (recminPp.isFull())
                {
                    if (Ppathloss[k] < recminPp.GetMaxItem().subPloss)
                    {
                        LocPpResult loc = new LocPpResult(Convert.ToInt32(str[0]), Convert.ToInt32(str[1]), Convert.ToInt32(str[2]), value, grid_from[k].Count, Ppathloss[k], dis, (int)grid_strongP[k]);

                        recminPp.DeteleMaxItem();
                        recPp.AddItem(loc);
                    }
                }
                else
                {
                    LocPpResult loc = new LocPpResult(Convert.ToInt32(str[0]), Convert.ToInt32(str[1]), Convert.ToInt32(str[2]), value, grid_from[k].Count, Ppathloss[k], dis, (int)grid_strongP[k]);
                    recminPp.AddItem(loc);
                }


            }
            int count = 0;

            Debug.WriteLine("路径数值最大前" + kn + "个结果集，由路径数大小从小到大输出");
            while (!recP.isEmpty())
            {
                LocPResult loc = recP.GetMinItem();
                Debug.WriteLine(count + " grid:" + loc.gridx + "  " + loc.gridy + "   " + loc.gridz + "    rate:" + loc.rp + "    dis:" + loc.dis + "    from:" + loc.fromnum + "    综合干扰差值：" + loc.subPloss + "   路径数" + loc.snum);
                recP.DeteleMinItem();
            }
            Debug.WriteLine("综合干扰差值最大前" + kn + "个结果集，由差值大小从小到大输出");
            while (!recPp.isEmpty())
            {
                LocPpResult loc = recPp.GetMinItem();
                Debug.WriteLine(count + " grid:" + loc.gridx + "  " + loc.gridy + "   " + loc.gridz + "    rate:" + loc.rp + "    dis:" + loc.dis + "    from:" + loc.fromnum + "    综合干扰差值：" + loc.subPloss + "   路径数" + loc.snum);
                recPp.DeteleMinItem();
            }
            Debug.WriteLine("综合干扰差值最小前" + kn + "个结果集，由差值大小从大到小输出");
            while (!recminPp.isEmpty())
            {
                LocPpResult loc = recminPp.GetMaxItem();
                Debug.WriteLine(count + " grid:" + loc.gridx + "  " + loc.gridy + "   " + loc.gridz + "    rate:" + loc.rp + "    dis:" + loc.dis + "    from:" + loc.fromnum + "    综合干扰差值：" + loc.subPloss + "   路径数" + loc.snum);
                recminPp.DeteleMaxItem();
            }

            Debug.WriteLine(kn + "个结果集，由评分从小到大输出");
            while (!rec.isEmpty())
            {
                LocResult loc = rec.GetMinItem();
                Debug.WriteLine(count + " grid:" + loc.gridx + "  " + loc.gridy + "   " + loc.gridz + "    rate:" + loc.rp + "    dis:" + loc.dis + "    from:" + loc.fromnum + "    综合干扰差值：" + loc.subPloss + "   路径数" + loc.snum);

                Geometric.Point p = new Geometric.Point();
                Grid3D tmp1 = new Grid3D(loc.gridx, loc.gridy, loc.gridz);
                GridHelper.getInstance().PointGridXYZ(ref p, tmp1, ggridsize, ggridVsize);
                DataRow dr = tb.NewRow();

                dr["Version"] = this.virname;
                dr["ansID"] = kn--;
                dr["x"] = p.X;
                dr["y"] = p.Y;
                dr["z"] = p.Z;
                PointConvertByProj.Instance.GetGeoPoint(p);
                dr["Longitude"] = p.X;
                dr["Latitude"] = p.Y;
                tb.Rows.Add(dr);
                rec.DeteleMinItem();
            }
            WriteToLocResult();
            return maxKey;

        }
        private void WriteToLocResult()
        {
            Hashtable ht = new Hashtable();
            ht["Version"] = this.virname;
            IbatisHelper.ExecuteDelete("deletbLocResult", ht);
            using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
            {
                bcp.BatchSize = tb.Rows.Count;
                bcp.BulkCopyTimeout = 1000;
                bcp.DestinationTableName = "tbLocResults";
                bcp.WriteToServer(tb);
                bcp.Close();
            }
            tb.Clear();
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

    /// <summary>
    /// 用于得到前k个结果
    /// </summary>
    public class LocResult : IComparable
    {
        public int gridx;
        public int gridy;
        public int gridz;
        public double rp;
        public double dis;
        public int snum;
        public int fromnum;
        public double subPloss;
        public LocResult(int gridx, int gridy, int gridz,double rp,int fromnum,double subPloss,double dis,int snum)
        {
            this.gridx = gridx;
            this.gridy = gridy;
            this.gridz = gridz;
            this.rp = rp;
            this.fromnum = fromnum;
            this.subPloss = subPloss;
            this.snum = snum;
            this.dis = dis;
        }

        public LocResult(int gridx, int gridy, int gridz, double rp, int fromnum, double subPloss, int snum)
        {
            this.gridx = gridx;
            this.gridy = gridy;
            this.gridz = gridz;
            this.rp = rp;
            this.fromnum = fromnum;
            this.subPloss = subPloss;
            this.snum = snum;
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            LocResult otherG = obj as LocResult;
            if (this.rp < otherG.rp) return -1;
            else if (Math.Abs(this.rp - otherG.rp) < 0.0001) return 0;
            return 1;
        }
    }
    public class LocPResult : IComparable
    {
        public int gridx;
        public int gridy;
        public int gridz;
        public double rp;
        public double dis;
        public int snum;
        public int fromnum;
        public double subPloss;
        public LocPResult(int gridx, int gridy, int gridz, double rp, int fromnum, double subPloss, double dis, int snum)
        {
            this.gridx = gridx;
            this.gridy = gridy;
            this.gridz = gridz;
            this.rp = rp;
            this.fromnum = fromnum;
            this.subPloss = subPloss;
            this.snum = snum;
            this.dis = dis;
        }

        public LocPResult(int gridx, int gridy, int gridz, double rp, int fromnum, double subPloss, int snum)
        {
            this.gridx = gridx;
            this.gridy = gridy;
            this.gridz = gridz;
            this.rp = rp;
            this.fromnum = fromnum;
            this.subPloss = subPloss;
            this.snum = snum;
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            LocPResult otherG = obj as LocPResult;
            if (this.snum < otherG.snum) return -1;
            else if (Math.Abs(this.snum - otherG.snum) < 0.0001) return 0;
            return 1;
        }
    }

    public class LocPpResult : IComparable
    {
        public int gridx;
        public int gridy;
        public int gridz;
        public double rp;
        public double dis;
        public int snum;
        public int fromnum;
        public double subPloss;
        public LocPpResult(int gridx, int gridy, int gridz, double rp, int fromnum, double subPloss, double dis, int snum)
        {
            this.gridx = gridx;
            this.gridy = gridy;
            this.gridz = gridz;
            this.rp = rp;
            this.fromnum = fromnum;
            this.subPloss = subPloss;
            this.snum = snum;
            this.dis = dis;
        }

        public LocPpResult(int gridx, int gridy, int gridz, double rp, int fromnum, double subPloss, int snum)
        {
            this.gridx = gridx;
            this.gridy = gridy;
            this.gridz = gridz;
            this.rp = rp;
            this.fromnum = fromnum;
            this.subPloss = subPloss;
            this.snum = snum;
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            LocPpResult otherG = obj as LocPpResult;
            if (this.subPloss < otherG.subPloss) return -1;
            else if (Math.Abs(this.subPloss - otherG.subPloss) < 0.0001) return 0;
            return 1;
        }
    }
}
