using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using LTE.DB;
using LTE.Geometric;
//using ReflectionCoefficient;
using LTE.InternalInterference.Grid;

namespace LTE.InternalInterference
{
    /// <summary>
    /// 用于射线跟踪场强计算合并，取代原有类GridStrengthPwr
    /// </summary>
    public class CalcGridStrength
    {
        public CellInfo cellInfo;
        private Dictionary<string, GridStrength> gridStrengths;

        //多径修正参数
        private double alfa1 = 0.4;  //反射
        private double alfa2 = 0.4;  //绕射
        private double alfa3 = 0.4;  //透射

        public double maxDistGround;  // 射线所达到的地面最远距离
        public double maxDistBuild;   // 射线所达到的建筑物最远距离
        public double gx, gy;
        public double bx, by;
        public double dbm;
        public int scenNum = 3;

        public CalcGridStrength(CellInfo sourceInfo, Dictionary<string, GridStrength> gs)
        {
            this.cellInfo = sourceInfo;
            this.gridStrengths = gs;
            
            maxDistGround = 0;
            maxDistBuild = 0;
            scenNum = AdjCoeffHelper.getInstance().getSceneNum();
        }

        public CalcGridStrength(CellInfo sourceInfo)
        {
            this.cellInfo = sourceInfo;

            maxDistGround = 0;
            maxDistBuild = 0;
            scenNum = AdjCoeffHelper.getInstance().getSceneNum();
        }

        /// <summary>
        /// 获取射线发射出来的功率（dbm）
        /// </summary>
        /// <param name="rayAzimuth"></param> 
        /// <param name="rayInclination"></param>
        /// <returns></returns>
        public double calcDbmPt(double rayAzimuth, double rayInclination)
        {
            return dbmPt(cellInfo.EIRP, Math.Abs(cellInfo.Azimuth - rayAzimuth), Math.Abs(cellInfo.Inclination - rayInclination));
        }

        /// <summary>
        /// 计算射线接收强度（除去透射）
        /// </summary>
        /// <param name="rayAzimuth">初始射线方位角</param>
        /// <param name="rayInclination">初始射线下倾角</param>
        /// <param name="nodeInfo"></param>
        /// <returns>接收功率，路径长度</returns>
        public double[] calcRayStrength(double rayAzimuth, double rayInclination, ref List<NodeInfo> rays)
        {
            //if (this.scenNum > 0)
            //{
                // 直接使用多场景校正系数 
                return calcRayStrengthAdj(rayAzimuth, rayInclination, ref rays);
            //}
            //else  // 使用界面输入的校正系数
            //{
            //    //计算dbmPt
            //    double[] ret = new double[3];

            //    double distance = 0;
            //    double reflectedR = 1;//反射系数
            //    double diffrctedR = 1;//绕射系数

            //    double amendCoeDis = cellInfo.directCoefficient;//直射校正系数 大东裕DB 0.341712610980973  公安局2 0.606110707653544
            //    double amendCoeRef = cellInfo.reflectCoefficient;//反射校正系数
            //    double amendCoeDif = cellInfo.diffracteCoefficient;//绕射校正系数

            //    double nata = 0;
            //    if (cellInfo.cellType == CellType.GSM1800)
            //    {
            //        //f(n) = 1805 + 0.2*(n－511) MHz
            //        nata = 300.0 / (1805 + 0.2 * (cellInfo.frequncy - 511));
            //    }
            //    else
            //    {
            //        //f(n) = 935 + 0.2n MHz
            //        nata = 300.0 / (935 + 0.2 * cellInfo.frequncy);
            //    }

            //    double dbmPt = calcDbmPt(rayAzimuth, rayInclination);  //定向
            //    if (rays.Count > 0 && cellInfo.SourcePoint.Z < 1.1)
            //        dbmPt = cellInfo.EIRP;
            //    //double dbmPt = calcDbmPt(cellInfo.Azimuth, rayInclination);    //全向

            //    double wPt = convertdbm2w(dbmPt);

            //    int length = rays.Count;
            //    NodeInfo ray;

            //    for (int i = 0; i < length; i++)
            //    {
            //        ray = rays[i];
            //        distance += ray.Distance;
            //        if (ray.rayType == RayType.HReflection || ray.rayType == RayType.VReflection) //反射
            //        {
            //            //反射系数是平方后的结果？todo
            //            // 弧度
            //            ray.attenuation = reflectCoefficient(ray.Angle) * amendCoeRef; // 用于系数校正
            //            reflectedR *= ray.attenuation;

            //        }
            //        else if (ray.rayType == RayType.HDiffraction || ray.rayType == RayType.VDiffraction) //绕射
            //        {
            //            ray.attenuation = reflectCoefficient(ray.Angle) * amendCoeDif;// 暂时使用计算反射系数的函数 代替计算绕射系数的函数
            //            //ray.attenuation = diffractCoefficient(ray.Angle) * amendCoeDif; // 用于系数校正
            //            diffrctedR *= ray.attenuation;
            //        }
            //        else
            //            ray.attenuation = 1;
            //        //透射另行计算
            //    }

            //    double receivePwr = 0;
            //    receivePwr = Math.Pow(nata / (4 * Math.PI), 2) * (wPt / Math.Pow(distance, (2 + amendCoeDis))) * Math.Pow(reflectedR, 2) * Math.Pow(diffrctedR, 2);
            //    ret[0] = receivePwr;
            //    ret[1] = distance;
            //    ret[2] = wPt;
            //    return ret;
            //}
        }

        // 多场景校正系数
        public double[] calcRayStrengthAdj(double rayAzimuth, double rayInclination, ref List<NodeInfo> rays)
        {
            //计算发射功率p0
            double dbmPt = calcDbmPt(rayAzimuth, rayInclination);  //定向
            if (rays.Count > 0 && cellInfo.SourcePoint.Z < 1.1)
                dbmPt = cellInfo.EIRP;
            double wPt = convertdbm2w(dbmPt);

            //获得校正系数矩阵
            double[,] coef = AdjCoeffHelper.getInstance().getCoeff();
            
            //计算接收功率
            RayInfo rayInfo = new RayInfo(rays, wPt);
            rayInfo.calcRecePwrW(ref coef, this.scenNum, cellInfo.frequncy);

            double[] ret = new double[3];
            ret[0] = rayInfo.recePwrW;
            ret[1] = rayInfo.distance;
            ret[2] = wPt;
            return ret;
        }

        /// <summary>
        /// 计算射线接收强度（除去透射），二次投射
        /// </summary>
        /// <param name="rayAzimuth">初始射线方位角</param>
        /// <param name="rayInclination">初始射线下倾角</param>
        /// <param name="nodeInfo"></param>
        /// <returns>接收功率，路径长度</returns>
        public double[] calcRayStrength(ref List<NodeInfo> rays, double emitPwr)
        {
            //计算dbmPt
            double[] ret = new double[3];

            double distance = 0;
            double reflectedR = 1;//反射系数
            double diffrctedR = 1;//绕射系数

            double amendCoeDis = cellInfo.directCoefficient;//直射校正系数 大东裕DB 0.341712610980973  公安局2 0.606110707653544
            double amendCoeRef = cellInfo.reflectCoefficient;//反射校正系数
            double amendCoeDif = cellInfo.diffracteCoefficient;//绕射校正系数

            double nata = 0;
            if (cellInfo.cellType == CellType.GSM1800)
            {
                //f(n) = 1805 + 0.2*(n－511) MHz
                nata = 300.0 / (1805 + 0.2 * (cellInfo.frequncy - 511));
            }
            else
            {
                //f(n) = 935 + 0.2n MHz
                nata = 300.0 / (935 + 0.2 * cellInfo.frequncy);
            }

            double wPt = convertdbm2w(emitPwr);

            int length = rays.Count;
            NodeInfo ray;

            for (int i = 0; i < length; i++)
            {
                ray = rays[i];
                distance += ray.Distance;
                if (ray.rayType == RayType.HReflection || ray.rayType == RayType.VReflection) //反射
                {
                    //反射系数是平方后的结果？todo
                    // 弧度
                    ray.attenuation = reflectCoefficient(ray.Angle) * amendCoeRef; // 用于系数校正
                    reflectedR *= ray.attenuation;

                }
                else if (ray.rayType == RayType.HDiffraction || ray.rayType == RayType.VDiffraction) //绕射
                {
                    ray.attenuation = reflectCoefficient(ray.Angle) * amendCoeDif;//暂时用反射系数代替
                    //ray.attenuation = diffractCoefficient(ray.Angle) * amendCoeDif; // 用于系数校正
                    diffrctedR *= ray.attenuation;
                }
                else
                    ray.attenuation = 1;
                //透射另行计算
            }

            double receivePwr = 0;
            receivePwr = Math.Pow(nata / (4 * Math.PI), 2) * (wPt / Math.Pow(distance, (2 + amendCoeDis))) * Math.Pow(reflectedR, 2) * Math.Pow(diffrctedR, 2);
            ret[0] = receivePwr;
            ret[1] = distance;
            ret[2] = wPt;
            return ret;
        }

        public double calcDirectRayStrength(double fromX, double fromY, double fromZ, double toX, double toY, double toZ, double EIRP, double nata)
        {
            double rayAzimuth, rayInclination;
            LTE.Geometric.Point SourcePoint = new Point(fromX, fromY, fromZ);
            LTE.Geometric.Point crossWithGround = new Point(toX, toY, toZ);
            double distance = Math.Sqrt(Math.Pow(fromX - toX, 2) + Math.Pow(fromY - toY, 2) + Math.Pow(fromZ - toZ, 2));

            //double L = 32.45 + 20 * Math.Log(nata) + 20 * Math.Log(distance/1000.0);
            //return EIRP - L;

            GeometricUtilities.getAzimuth_Inclination(SourcePoint, crossWithGround, out rayAzimuth, out rayInclination);

            double amendCoeDis = 0.3;  //直射校正系数
            double dbmPt1 = dbmPt(EIRP, 0, Math.Abs(rayInclination));
            //double dbmPt1 = EIRP;
            double wPt = convertdbm2w(dbmPt1);
            nata = 300.0 / (1805 + 0.2 * (nata - 511));
            double receivePwr = Math.Pow(nata / (4 * Math.PI), 2) * (wPt / Math.Pow(distance, (2 + amendCoeDis)));
            double ReceivedPower_dbm = convertw2dbm(receivePwr);

            return ReceivedPower_dbm;
        }

        /// <summary>
        /// 计算室外接收场强和路径损耗
        /// </summary>
        /// <param name="rayAzimuth">初始射线方位角</param>
        /// <param name="rayInclination">初始射线下倾角</param>
        /// <param name="rays"></param>
        /// <param name="isT">初级射线是否与建筑物相交</param>
        public void CalcAndMergeGGridStrength(double rayAzimuth, double rayInclination, List<NodeInfo> rays, bool isT)
        {
            double[] receiveList = calcRayStrength(rayAzimuth, rayInclination, ref rays);

            double ReceivedPwr = receiveList[0];
            double ReceivedPower_dbm = convertw2dbm(ReceivedPwr);
            if (isT) // 如果是穿过建筑物后到达地面或楼顶
            {
                ReceivedPower_dbm -= 18;// transmissionLoss(0);
                ReceivedPwr = convertdbm2w(ReceivedPower_dbm);
            }
            //场强太弱，认为对栅格接收场强无影响，不合并
            if (ReceivedPower_dbm < -130)
            {
                return;
            }
            //Console.WriteLine("ReceivedPower_dbm >= -130");

            Point t_p = rays[rays.Count - 1].CrossPoint;

            int gxid = -1, gyid = -1;
            if (!GridHelper.getInstance().XYToGGrid(t_p.X, t_p.Y, ref gxid, ref gyid))
                return;

            if (Math.Abs(t_p.Z) < 1)
                mergeGridStrength(gxid, gyid, 0, t_p, rays, receiveList[2], ReceivedPwr, isT);
            else
            {
                int gzid = (int)(t_p.Z / GridHelper.getInstance().getGHeight()) + 1;
                mergeGridStrength(gxid, gyid, gzid, t_p, rays, receiveList[2], ReceivedPwr, isT);
            }
        }

        /// <summary>
        /// 计算室外接收场强和路径损耗，二次投射
        /// </summary>
        /// <param name="rayAzimuth">初始射线方位角</param>
        /// <param name="rayInclination">初始射线下倾角</param>
        /// <param name="rays"></param>
        /// <param name="isT">初级射线是否与建筑物相交</param>
        public void CalcAndMergeGGridStrength(List<NodeInfo> rays, double emitPwr)
        {
            double[] receiveList = calcRayStrength(ref rays, emitPwr);

            double ReceivedPwr = receiveList[0];
            double ReceivedPower_dbm = convertw2dbm(ReceivedPwr);

            //场强太弱，认为对栅格接收场强无影响，不合并
            if (ReceivedPower_dbm < -130)
            {
                return;
            }
            //Console.WriteLine("ReceivedPower_dbm >= -130");

            Point t_p = rays[rays.Count - 1].CrossPoint;

            int gxid = -1, gyid = -1;
            if (!GridHelper.getInstance().XYToGGrid(t_p.X, t_p.Y, ref gxid, ref gyid))
                return;

            if (Math.Abs(t_p.Z) < 1)
                mergeGridStrength(gxid, gyid, 0, t_p, rays, receiveList[2], ReceivedPwr, false);
            else
            {
                int gzid = (int)Math.Ceiling(t_p.Z / GridHelper.getInstance().getGHeight()) + 1;
                mergeGridStrength(gxid, gyid, gzid, t_p, rays, receiveList[2], ReceivedPwr, false);
            }
        }

        /// <summary>
        /// 计算小区室内传播路径损耗
        /// </summary>
        /// <param name="indoorstart">室内入射点</param>
        /// <param name="indoorend">结束点，有可能不在室内</param>
        public void CalcAndMergeBGridStrength(double rayAzimuth, double rayInclination, List<NodeInfo> rays, Point indoorstart, Point indoorend)
        {
            NodeInfo ray = rays.Last();
            int buildingID = ray.buildingID;

            double[] receiveList = calcRayStrength(rayAzimuth, rayInclination, ref rays);
            double dbminner = convertw2dbm(receiveList[0]) - transmissionLoss(0);

            if (dbminner < -130)
            {
                return;
            }

            double pwr = 0;

            // 室内
            Grid3D curGrid3D;
            double dis = 0.0;
            double dbmgrid = 0.0;

            LineCrossGrid3D linecrossgrid3d = new LineCrossGrid3D(indoorstart, indoorend);

            while ((curGrid3D = linecrossgrid3d.getNextCrossGrid3D(ref dis)) != null)
            {
                if (!BuildingGrid3D.isBuildingExistGrid3D(buildingID, curGrid3D.gxid, curGrid3D.gyid, curGrid3D.gzid))
                {
                    break;
                }
                dbmgrid = dbminner - distanceLoss(receiveList[1], dis);
                if (dbmgrid < -130)
                {
                    return;
                }
                pwr = convertdbm2w(dbmgrid);

                mergeIndoorGridStrength(curGrid3D, rays, pwr, buildingID);
            }
        }

        /*
        /// <summary>
        /// 计算建筑物外表面传播路径损耗
        /// </summary>
        /// <param name="indoorstart">室内入射点</param>
        /// <param name="indoorend">结束点，有可能不在室内</param>
        public void CalcAndMergeBGridStrengthOut(double rayAzimuth, double rayInclination, List<NodeInfo> rays, Point indoorstart, Point indoorend)
        {
            double[] receiveList = calcRayStrength(rayAzimuth, rayInclination, rays);

            double Pwr = receiveList[0];
            double ReceivedPower_dbm = convertw2dbm(Pwr);
            //场强太弱，认为对栅格接收场强无影响，不合并
            if (ReceivedPower_dbm < -130)
            {
                return;
            }

            Point t_p = rays[rays.Count - 1].CrossPoint;

            int gxid = -1, gyid = -1;
            if (!GridHelper.getInstance().XYToGGrid(t_p.X, t_p.Y, ref gxid, ref gyid))
                return;
            double h = GridHelper.getInstance().getGHeight();
            int gzid = (int)Math.Ceiling(t_p.Z / h);
            mergeGridStrength(gxid, gyid, gzid, rays, Pwr);
        }
        */

        /// <summary>
        /// 合并多任务计算后的场强结果，结果保存在预先设定的GridStrength结构内
        /// </summary>
        /// <param name="strengthList"></param>
        public Dictionary<string, GridStrength> MergeMultipleTaskStrength(List<GridStrength> strengthList)
        {
            Dictionary<string, GridStrength> ret = new Dictionary<string, GridStrength>();
            GridStrength tgs, ogs;
            string key;
            for (int c = 0; c < strengthList.Count; c++)
            {
                tgs = strengthList[c];
                key = String.Format("{0},{1},{2}", tgs.GXID, tgs.GYID, tgs.Level);

                if (!ret.ContainsKey(key))
                {
                    ret.Add(key, tgs);
                }
                else
                {
                    ogs = ret[key];
                    ogs.RefBuildingID += tgs.RefBuildingID;
                    //ogs.DiffBuildingID += tgs.DiffBuildingID;
                    //ogs.TransmitBuildingID += tgs.TransmitBuildingID;

                    ogs.DirectNum += tgs.DirectNum;
                    ogs.DirectPwrW += tgs.DirectPwrW;
                    if (ogs.MaxDirectPwrW < tgs.MaxDirectPwrW)
                    {
                        ogs.MaxDirectPwrW = tgs.MaxDirectPwrW;
                    }

                    ogs.RefNum += tgs.RefNum;
                    ogs.RefPwrW += tgs.RefPwrW;
                    if (ogs.MaxRefPwrW < tgs.MaxRefPwrW)
                    {
                        ogs.MaxRefPwrW = tgs.MaxRefPwrW;
                    }

                    ogs.DiffNum += tgs.DiffNum;
                    ogs.DiffPwrW += tgs.DiffPwrW;
                    if (ogs.MaxDiffPwrW < tgs.MaxDiffPwrW)
                    {
                        ogs.MaxDiffPwrW = tgs.MaxDiffPwrW;
                    }

                    ogs.TransNum += tgs.TransNum;
                    ogs.TransPwrW += tgs.TransPwrW;
                    if (ogs.MaxTransPwrW < tgs.MaxTransPwrW)
                    {
                        ogs.MaxTransPwrW = tgs.MaxTransPwrW;
                    }

                    //dictionary不能自动更新
                    ret[key] = ogs;
                }
            }
            int lac = this.cellInfo.eNodeB, ci = this.cellInfo.CI;
            Point s = this.cellInfo.SourcePoint;
            Vector3D tv;
            double eirp = this.cellInfo.EIRP, Azimuth = this.cellInfo.Azimuth, Inclination = this.cellInfo.Inclination, GPtDbm;

            //string path = @"f:\tt.txt";
            //StreamWriter sw = File.CreateText(path);
            foreach (var k in ret.Keys.ToArray())
            {
                ogs = ret[k];
                ogs.eNodeB = lac;
                ogs.CI = ci;
                ogs.FieldIntensity = 0;
                double p = ogs.DirectPwrW + ogs.DiffPwrW + ogs.RefPwrW + ogs.TransPwrW;
                if (p > 0)
                    ogs.ReceivedPowerW = p;

                ogs.ReceivedPowerdbm = convertw2dbm(ogs.ReceivedPowerW);

                //sw.Write("{0} {1} -- ", ogs.ReceivedPowerW, ogs.ReceivedPowerdbm);

                //反射、绕射建筑物id去重
                ogs.RefBuildingID = DistinctStringArray(ogs.RefBuildingID.Split(';'));
                //ogs.DiffBuildingID = DistinctStringArray(ogs.DiffBuildingID.Split(';'));
                //ogs.TransmitBuildingID = DistinctStringArray(ogs.TransmitBuildingID.Split(';'));

                if (ogs.GCenter == null)
                {
                    ret[k] = ogs;
                    continue;
                }
                //计算BTSGridDistance，PathLoss
                tv = Vector3D.constructVector(s, ogs.GCenter);
                ogs.BTSGridDistance = Math.Sqrt(Math.Pow(s.X - ogs.GCenter.X, 2) + Math.Pow(s.Y - ogs.GCenter.Y, 2) + Math.Pow(s.Z - ogs.GCenter.Z, 2));
                //Console.WriteLine("{0} {1} {2}", tv.XComponent, tv.YComponent, tv.ZComponent);
                //Console.WriteLine("{0} {1} {2} {3} {4} {5}", (int)Math.Round(Math.Abs(cellInfo.Azimuth - tv.Azimuth)) % 360, (int)Math.Round(Math.Abs(cellInfo.Inclination - tv.Inclination)) % 360, tv.Azimuth, tv.Inclination, cellInfo.Azimuth, cellInfo.Inclination);
                GPtDbm = calcDbmPt(tv.Azimuth, tv.Inclination);
                ogs.PathLoss = ogs.ReceivedPowerdbm - GPtDbm;

                // 2017.6.14添加，为查看射线最远的地方
                if (ogs.Level == 0)
                {
                    if (ogs.ReceivedPowerdbm > -110 && ogs.BTSGridDistance > maxDistGround)
                    {
                        maxDistGround = ogs.BTSGridDistance;
                        this.gx = ogs.GCenter.X;
                        this.gy = ogs.GCenter.Y;
                        this.dbm = ogs.ReceivedPowerdbm;
                    }
                }
                else if (tv.Magnitude2D > maxDistBuild)
                {
                    maxDistBuild = tv.Magnitude2D;
                    this.bx = ogs.GCenter.X;
                    this.by = ogs.GCenter.Y;
                }

                //dictionary 不能自动更新
                ret[k] = ogs;
            }
            //sw.Close();
            return ret;
        }

        /// <summary>
        /// 字符串数组去重
        /// </summary>
        /// <param name="strArr"></param>
        /// <returns></returns>
        public string DistinctStringArray(string[] strArr)
        {
            return String.Join(";", strArr.Distinct().ToArray());
        }

        
        // Pwr0  发射功率，单位w
        // Pwr1  接收功率，单位w
        // isT   是否穿过建筑物
        // ci    小区标识
        public void mergeGridStrength(int gxid, int gyid, int gzid, Point p, List<NodeInfo> rays, double Pwr0, double Pwr1, bool isT)
        {
            GridStrength gs;
            string key = String.Format("{0},{1},{2}", gxid, gyid, gzid);

            if (this.gridStrengths.ContainsKey(key))
            {
                gs = this.gridStrengths[key];

                // 当前射线存在绕射和反射
                if (rays.Count > 0)
                {
                    updateBuildingID(ref gs, rays);
                    NodeInfo ray = rays[rays.Count - 1];
                    if (ray.rayType == RayType.HReflection || ray.rayType == RayType.VReflection)
                    {
                        gs.RefNum += 1;
                        gs.RefPwrW += Pwr1;
                        gs.MaxRefPwrW = Math.Max(gs.MaxRefPwrW, Pwr1);
                    }
                    else if (ray.rayType == RayType.HDiffraction || ray.rayType == RayType.VDiffraction)
                    {
                        gs.DiffNum += 1;
                        gs.DiffPwrW += Pwr1;
                        gs.MaxDiffPwrW = Math.Max(gs.MaxDiffPwrW, Pwr1);
                    }

                    //反射、绕射建筑物id去重
                    gs.RefBuildingID = DistinctStringArray(gs.RefBuildingID.Split(';'));
                    gs.DiffBuildingID = DistinctStringArray(gs.DiffBuildingID.Split(';'));
                }
                else if (rays.Count == 1)
                {
                    //当前射线是直射
                    gs.DirectNum += 1;
                    gs.DirectPwrW += Pwr1;
                    gs.MaxDirectPwrW = Math.Max(gs.MaxDirectPwrW, Pwr1);
                }

                this.gridStrengths[key] = gs;  // 更新
            }
            else
            {
                gs = new GridStrength();
                gs.GXID = gxid;
                gs.GYID = gyid;
                gs.Level = gzid;
                gs.eNodeB = this.cellInfo.eNodeB;
                gs.CI = this.cellInfo.CI;
                gs.RefBuildingID = "";
                gs.DiffBuildingID = "";
                gs.TransmitBuildingID = "";
                gs.GCenter = p;
                gs.TransPwrW = gs.RefPwrW = gs.DiffPwrW = gs.DirectPwrW = gs.ReceivedPowerW = 0;
                gs.TransNum = gs.RefNum = gs.DiffNum = gs.DirectNum = 0;

                //当前射线存在绕射和反射
                if (rays.Count > 1)
                {
                    updateBuildingID(ref gs, rays);

                    NodeInfo ray = rays[rays.Count - 1];
                    if (ray.rayType == RayType.HReflection || ray.rayType == RayType.VReflection)
                    {
                        gs.RefNum = 1;
                        gs.ReceivedPowerW = gs.MaxRefPwrW = gs.RefPwrW = Pwr1;
                    }
                    else if (ray.rayType == RayType.HDiffraction || ray.rayType == RayType.VDiffraction)
                    {
                        gs.DiffNum = 1;
                        gs.ReceivedPowerW = gs.MaxDiffPwrW = gs.DiffPwrW = Pwr1;
                    }
                }
                else if (rays.Count == 1)
                {
                    //当前射线是直射
                    gs.DirectNum = 1;
                    gs.ReceivedPowerW = gs.MaxDirectPwrW = gs.DirectPwrW = Pwr1;
                }

                this.gridStrengths.Add(key, gs);
            }
        }

        /// <summary>
        /// 合并室内栅格的场强
        /// </summary>
        /// <param name="curGrid3D">入地点栅格</param>
        /// <param name="rays"></param>
        /// <param name="Pwr"></param>
        /// <param name="buildingID"></param>
        public void mergeIndoorGridStrength(Grid3D curGrid3D, List<NodeInfo> rays, double Pwr, int buildingID)
        {
            GridStrength gs;
            string key = String.Format("{0},{1},{2}", curGrid3D.gxid, curGrid3D.gyid, curGrid3D.gzid);

            if (this.gridStrengths.ContainsKey(key))
            {
                gs = this.gridStrengths[key];
                gs.TransmitBuildingID += buildingID.ToString();
                updateBuildingID(ref gs, rays);
                gs.TransNum += 1;
                gs.TransPwrW += Pwr;
                gs.MaxTransPwrW = Math.Max(gs.MaxTransPwrW, Pwr);

                this.gridStrengths[key] = gs;
            }
            else
            {
                gs = new GridStrength();
                gs.GXID = curGrid3D.gxid;
                gs.GYID = curGrid3D.gyid;
                gs.Level = curGrid3D.gzid;
                gs.RefBuildingID = "";
                gs.DiffBuildingID = "";
                gs.TransmitBuildingID = buildingID.ToString();
                gs.GCenter = GroundGrid.getBGridCenter(curGrid3D.gxid, curGrid3D.gyid, curGrid3D.gzid);

                if (gs.GCenter == null)
                {
                    gs.GCenter = new Point(-1, -1, -1);
                }

                updateBuildingID(ref gs, rays);

                gs.TransNum = 1;
                gs.ReceivedPowerW = gs.MaxTransPwrW = gs.TransPwrW = Pwr;

                this.gridStrengths.Add(key, gs);
            }
        }

        /// <summary>
        /// 更新栅格的反射、绕射建筑物ID
        /// </summary>
        /// <param name="gs"></param>
        /// <param name="rays"></param>
        public void updateBuildingID(ref GridStrength gs, List<NodeInfo> rays)
        {
            NodeInfo ray;
            for (int i = 1; i < rays.Count; i++)
            {
                ray = rays[i];
                if (ray.rayType == RayType.HReflection || ray.rayType == RayType.VReflection) //反射
                {
                    //最终合并时去重，此处不去重
                    gs.RefBuildingID += ";" + rays[i - 1].buildingID;
                }
                else if (ray.rayType == RayType.HDiffraction || ray.rayType == RayType.VDiffraction) //绕射
                {
                    gs.DiffBuildingID += ";" + rays[i - 1].buildingID;
                }
            }
        }

        /// <summary>
        /// 小区的不同方向的损耗
        /// </summary>
        /// <param name="halfangle">半功率角</param>
        /// <param name="mjangle">与天线方位角的夹角</param>
        /// <param name="AddValue">添加0度时的增益值----现在还未使用-目前为0</param>
        /// <returns></returns>
        public double LossAdd(double halfangle, double mjangle, double AddValue)
        {
            if (mjangle == 0)
                return 0;
            else if (mjangle > 0 && mjangle <= halfangle / 2)
            {
                double loss = -mjangle * 3 / (halfangle / 2);
                return loss;
            }
            else if (mjangle > halfangle / 2 && mjangle <= halfangle)
            {
                double loss = -3 - (mjangle - halfangle / 2) * 7 / (halfangle / 2);
                return loss;
            }
            else if (mjangle > halfangle && mjangle <= 90)
            {
                double loss = -10 - (mjangle - halfangle) * 5 / (90 - halfangle);
                return loss;
            }
            else if (mjangle > 90 && mjangle <= 110)
            {
                double loss = -15 - (mjangle - 90) * 5 / (110 - 90);
                return loss;
            }
            else if (mjangle > 110 && mjangle <= 140)
            {
                double loss = -20 - (mjangle - 110) * 5 / (140 - 110);
                return loss;
            }
            else if (mjangle > 140 && mjangle <= 150)
            {
                double loss = -25 - (mjangle - 140) * 5 / (150 - 140);
                return loss;
            }
            else
            {
                double loss = -30 - (mjangle - 150) * 5 / (180 - 150);
                return loss;
            }
        }

        public double dbmPt(double EIRP, double mjDir, double mjTilt)
        {
            //夹角度数为小数时，按比例取大于它和小于它的整度数对应的损耗
            //dbmPt = EiRP－ HLoss(Dir,θ) － VLoss(Tilt, Φ)
            Hashtable paramTable = new Hashtable();
            paramTable["gainType"] = "KRE738819_902";
            paramTable["direction"] = 0; // 0对应HLoss
            paramTable["degree"] = (int)Math.Floor(mjDir) % 360;
            double HLoss1 = Convert.ToDouble(IbatisHelper.ExecuteQueryForDataTable("getLoss", paramTable).Rows[0]["Loss"]);
            paramTable["degree"] = (int)Math.Ceiling(mjDir) % 360;
            double HLoss2= Convert.ToDouble(IbatisHelper.ExecuteQueryForDataTable("getLoss", paramTable).Rows[0]["Loss"]);
            double w1 = Math.Abs(mjDir - Math.Floor(mjDir) % 360);
            double HLoss = HLoss1 * w1 + HLoss2 * (1 - w1);

            paramTable["direction"] = 1; // 1对应VLoss.
            paramTable["degree"] = (int)Math.Floor(mjTilt) % 360;
            double VLoss1 = Convert.ToDouble(IbatisHelper.ExecuteQueryForDataTable("getLoss", paramTable).Rows[0]["Loss"]);
            paramTable["degree"] = (int)Math.Ceiling(mjTilt) % 360;
            double VLoss2 = Convert.ToDouble(IbatisHelper.ExecuteQueryForDataTable("getLoss", paramTable).Rows[0]["Loss"]);
            double w2 =Math.Abs(mjTilt-Math.Floor(mjTilt)%360);
            double VLoss = VLoss1 * w2 + VLoss2 * (1 - w2);


            //旧版代码，夹角度数为小数时采用随机取整
            ////dbmPt = EiRP－ HLoss(Dir,θ) － VLoss(Tilt, Φ)
            //Hashtable paramTable = new Hashtable();
            //paramTable["gainType"] = "KRE738819_902";
            //paramTable["direction"] = 0; // 0对应HLoss
            //paramTable["degree"] = (int)Math.Round(mjDir) % 360;
            //double HLoss = Convert.ToDouble(IbatisHelper.ExecuteQueryForDataTable("getLoss", paramTable).Rows[0]["Loss"]);

            //paramTable["direction"] = 1; // 1对应VLoss.
            //paramTable["degree"] = (int)Math.Round(mjTilt) % 360;
            //double VLoss = Convert.ToDouble(IbatisHelper.ExecuteQueryForDataTable("getLoss", paramTable).Rows[0]["Loss"]);

            //旧版代码，从内存读取
            //AbstrGain abstrGain = GainFactory.GetabstractGain("KRE738819_902");
            //double[] HAGain = abstrGain.GetHAGain();
            //double[] VAGain = abstrGain.GetVAGain();
            //double HLoss = HAGain[(int)Math.Round(mjDir) % 360];
            //double VLoss = VAGain[(int)Math.Round(mjTilt) % 360];

            double dbmPt = EIRP - HLoss - VLoss - 2;

            return dbmPt;
        }

        public double dbmPt(double EIRP, double mjDir, double mjTilt, out double HLoss, out double VLoss)
        {
            //dbmPt = EiRP－ HLoss(Dir,θ) － VLoss(Tilt, Φ)
            Hashtable paramTable = new Hashtable();
            paramTable["gainType"] = "KRE738819_902";
            paramTable["direction"] = 0; // 0对应HLoss
            paramTable["degree"] = (int)mjDir % 360;
            HLoss = Convert.ToDouble( IbatisHelper.ExecuteQueryForDataTable("getLoss", paramTable).Rows[0]["Loss"]);

            paramTable["direction"] = 1; // 1对应VLoss.
            paramTable["degree"] = (int)mjTilt % 360;
            VLoss = Convert.ToDouble(IbatisHelper.ExecuteQueryForDataTable("getLoss", paramTable).Rows[0]["Loss"]);

            //旧版代码，从内存读取
            //AbstrGain abstrGain = GainFactory.GetabstractGain("KRE738819_902");
            //double[] HAGain = abstrGain.GetHAGain();
            //double[] VAGain = abstrGain.GetVAGain();
            ////double HLoss = HAGain[(int)Math.Round(mjDir)];
            ////double VLoss = VAGain[(int)Math.Round(mjTilt)];
            //HLoss = HAGain[(int)mjDir % 360];
            //VLoss = VAGain[(int)mjTilt % 360];

            //double dbmPt = EIRP - HAGain[(int)Math.Round(mjDir)] - VAGain[(int)Math.Round(mjTilt)] - 2;
            double dbmPt = EIRP - HLoss - VLoss - 2;
            return dbmPt;
        }

        public double convertdbm2w(double dbmPt)
        {
            return Math.Pow(10, (dbmPt / 10 - 3));
        }

        public double convertw2dbm(double w)
        {
            return 10 * (Math.Log10(w) + 3);
        }

        /// <summary>
        /// 透射损耗15dbm, 与角度和材质有关，此处认为与反射损耗一致
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public double transmissionLoss(double angle)
        {
            return 13.0;
        }

        /// <summary>
        /// 室内传播距离损耗,值为正, 当直射校正系数为0.3时，deltaDis / baseDis = 0.01时，约损耗0.1db，比值变化不大时，损耗可看做线性关系。
        /// </summary>
        /// <param name="dis"></param>
        /// <returns></returns>
        public double distanceLoss(double baseDis, double deltaDis)
        {
            return Math.Log10(1 + deltaDis / baseDis) * 10 * (2 + cellInfo.directCoefficient);
        }

        //反射系数
        public double reflectCoefficient(double angle)
        {
            angle = GeometricUtilities.GetDegrees(angle);
            Reflection_vh rf = new Reflection_vh();//水平方向系数
            ArrayList reflectCoe = rf.GetReflection(5, 0.01, angle);//第一个是垂直系数 第二个是水平系数

            double return_reflectCoe = Math.Pow(Math.Cos(angle) * (double)reflectCoe[1], 2) + Math.Pow(Math.Sin(angle) * (double)reflectCoe[0], 2);

            return return_reflectCoe > 1 ? 1 : return_reflectCoe;
            //return 0.5;
        }

        //绕射系数
        public double diffractCoefficient(double angle)
        {
            angle = GeometricUtilities.GetDegrees(angle);
            Reflection_vh rv = new Reflection_vh();
            //double return_Coe = 0;
            //double Coe22 = rv.Getangel22(angle);
            //double Coe248 = rv.Getangel248(angle);
            //double return_Coe = (Math.Abs(angle - 22) * Coe248 + Math.Abs(angle - 248) * Coe22) / (Math.Abs(angle - 22) + Math.Abs(angle - 248));
            //double Uv = Math.Pow(10, return_Coe / 10);

            double Coe75 = rv.Getangel75(angle);
            double Coe135 = rv.Getangel135(angle);
            double return_Coe = (Math.Abs(angle - 135) * Coe75 + Math.Abs(angle - 75) * Coe135) / (Math.Abs(angle - 135) + Math.Abs(angle - 75));
            double Uv = Math.Pow(return_Coe, 2);

            return Uv > 1 ? 1 : Uv;
        }

        #region beam 相关

        /// <summary>
        /// 计算射线接收强度（除去透射），用于 beam
        /// </summary>
        /// <param name="rayAzimuth">初始射线方位角</param>
        /// <param name="rayInclination">初始射线下倾角</param>
        /// <param name="nodeInfo"></param>
        /// <returns>接收功率，路径长度</returns>
        public double[] calcRayStrengthBeam(double rayAzimuth, double rayInclination, ref List<Beam.NodeInfo> rays)
        {
            //计算dbmPt
            double[] ret = new double[3];

            double distance = 0;
            double reflectedR = 1;//反射系数
            double diffrctedR = 1;//绕射系数

            double amendCoeDis = cellInfo.directCoefficient;//直射校正系数 大东裕DB 0.341712610980973  公安局2 0.606110707653544
            double amendCoeRef = cellInfo.reflectCoefficient;//反射校正系数
            double amendCoeDif = cellInfo.diffracteCoefficient;//绕射校正系数

            double nata = 0;
            if (cellInfo.cellType == CellType.GSM1800)
            {
                //f(n) = 1805 + 0.2*(n－511) MHz
                nata = 300.0 / (1805 + 0.2 * (cellInfo.frequncy - 511));
            }
            else
            {
                //f(n) = 935 + 0.2n MHz
                nata = 300.0 / (935 + 0.2 * cellInfo.frequncy);
            }

            double dbmPt = calcDbmPt(rayAzimuth, rayInclination);  //定向

            double wPt = convertdbm2w(dbmPt);
            int length = rays.Count;
            Beam.NodeInfo ray;

            for (int i = 0; i < length; i++)
            {
                distance += rays[i].Distance;
                if (rays[i].rayType == Beam.RayType.HReflection || rays[i].rayType == Beam.RayType.VReflection) //反射
                {
                    //反射系数是平方后的结果？todo
                    // 弧度
                    rays[i].attenuation = reflectCoefficient(rays[i].Angle) * amendCoeRef; // 用于系数校正
                    reflectedR *= rays[i].attenuation;

                }
                else if (rays[i].rayType == Beam.RayType.HDiffraction || rays[i].rayType == Beam.RayType.VDiffraction) //绕射
                {
                    rays[i].attenuation = diffractCoefficient(rays[i].Angle) * amendCoeDif; // 用于系数校正
                    diffrctedR *= rays[i].attenuation;
                }
                else
                    rays[i].attenuation = 1;
                //透射另行计算
            }

            double receivePwr = 0;
            receivePwr = Math.Pow(nata / (4 * Math.PI), 2) * (wPt / Math.Pow(distance, (2 + amendCoeDis))) * Math.Pow(reflectedR, 2) * Math.Pow(diffrctedR, 2);

            ret[0] = receivePwr;
            ret[1] = distance;
            ret[2] = wPt;
            return ret;
        }

        /// <summary>
        /// 2018.12.04
        /// 计算室外接收场强和路径损耗
        /// </summary>
        /// <param name="rayAzimuth">初始射线方位角</param>
        /// <param name="rayInclination">初始射线下倾角</param>
        /// <param name="rays"></param>
        /// <param name="isT">初级射线是否与建筑物相交</param>
        public void CalcAndMergeGGridStrengthBeam(double rayAzimuth, double rayInclination, ref List<Beam.NodeInfo> rays)
        {
            double[] receiveList = calcRayStrengthBeam(rayAzimuth, rayInclination, ref rays);

            double ReceivedPwr = receiveList[0];
            double ReceivedPower_dbm = convertw2dbm(ReceivedPwr);

            //场强太弱，认为对栅格接收场强无影响，不合并
            if (ReceivedPower_dbm < -130)
            {
                return;
            }
            //Console.WriteLine("ReceivedPower_dbm >= -130");

            Beam.Point t_p = rays[rays.Count - 1].CrossPoint;
            Geometric.Point t_p1 = new Geometric.Point(t_p.m_position.x, t_p.m_position.y, t_p.m_position.z);

            int gxid = -1, gyid = -1;
            if (!GridHelper.getInstance().XYToGGrid(t_p.m_position.x, t_p.m_position.y, ref gxid, ref gyid))
                return;

            if (Math.Abs(t_p1.Z) < 1)
                mergeGridStrengthBeam(gxid, gyid, 0, t_p1, ref rays, receiveList[2], ReceivedPwr);
            else
            {
                int gzid = (int)Math.Ceiling(t_p.m_position.z / GridHelper.getInstance().getGHeight()) + 1;
                mergeGridStrengthBeam(gxid, gyid, gzid, t_p1, ref rays, receiveList[2], ReceivedPwr);
            }
        }

        // 2018.12.04
        // Pwr0  发射功率，单位w
        // Pwr1  接收功率，单位w
        // isT   是否穿过建筑物
        // ci    小区标识
        public void mergeGridStrengthBeam(int gxid, int gyid, int gzid, Point p, ref List<Beam.NodeInfo> rays, double Pwr0, double Pwr1)
        {
            GridStrength gs;
            string key = String.Format("{0},{1},{2}", gxid, gyid, gzid);

            if (this.gridStrengths.ContainsKey(key))
            {
                gs = this.gridStrengths[key];

                // 当前射线存在反射
                if (rays.Count > 1)
                {
                    updateBuildingIDBeam(ref gs, rays);

                    gs.RefNum += 1;
                    gs.RefPwrW += Pwr1;
                    gs.MaxRefPwrW = Math.Max(gs.MaxRefPwrW, Pwr1);

                    //反射建筑物id去重
                    gs.RefBuildingID = DistinctStringArray(gs.RefBuildingID.Split(';'));
                    gs.DiffBuildingID = DistinctStringArray(gs.DiffBuildingID.Split(';'));
                }
                else if (rays.Count == 1)
                {
                    //当前射线是直射
                    gs.DirectNum += 1;
                    gs.DirectPwrW += Pwr1;
                    gs.MaxDirectPwrW = Math.Max(gs.MaxDirectPwrW, Pwr1);
                }

                gs.ReceivedPowerW += Pwr1;

                this.gridStrengths[key] = gs;  // 更新
            }
            else  // 没有出现在栅格中
            {
                gs = new GridStrength();
                gs.GXID = gxid;
                gs.GYID = gyid;
                gs.Level = gzid;
                gs.eNodeB = this.cellInfo.eNodeB;
                gs.CI = this.cellInfo.CI;
                gs.RefBuildingID = "";
                gs.DiffBuildingID = "";
                gs.TransmitBuildingID = "";
                gs.GCenter = p;
                gs.TransPwrW = gs.RefPwrW = gs.DiffPwrW = gs.DirectPwrW = gs.ReceivedPowerW = 0;
                gs.TransNum = gs.RefNum = gs.DiffNum = gs.DirectNum = 0;

                //当前射线存在反射
                if (rays.Count > 1)
                {
                    updateBuildingIDBeam(ref gs, rays);

                    gs.RefNum = 1;
                    gs.ReceivedPowerW = gs.MaxRefPwrW = gs.RefPwrW = Pwr1;
                }
                else if (rays.Count == 1)
                {
                    //当前射线是直射
                    gs.DirectNum = 1;
                    gs.ReceivedPowerW = gs.MaxDirectPwrW = gs.DirectPwrW = Pwr1;
                }

                this.gridStrengths.Add(key, gs);
            }
        }

        /// <summary>
        /// 2018.12.04
        /// 更新栅格的反射、绕射建筑物ID
        /// </summary>
        /// <param name="gs"></param>
        /// <param name="rays"></param>
        public void updateBuildingIDBeam(ref GridStrength gs, List<Beam.NodeInfo> rays)
        {
            Beam.NodeInfo ray;
            for (int i = 0; i < rays.Count; i++)
            {
                ray = rays[i];
                if (ray.rayType == Beam.RayType.HReflection || ray.rayType == Beam.RayType.VReflection) //反射
                {
                    //最终合并时去重，此处不去重
                    gs.RefBuildingID += ";" + ray.buildingID;
                }
                else if (ray.rayType == Beam.RayType.HDiffraction || ray.rayType == Beam.RayType.VDiffraction) //绕射
                {
                    gs.DiffBuildingID += ";" + ray.buildingID;
                }
            }
        }

        #endregion

    }
}
