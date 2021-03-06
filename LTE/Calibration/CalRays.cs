﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using LTE.Geometric;

using LTE.DB;
using LTE.InternalInterference.Grid;
using LTE.InternalInterference;

using System.IO;
//using Excel = Microsoft.Office.Interop.Excel;
using Point = LTE.Geometric.Point;

namespace LTE.Calibration
{
    public class CalRays
    {
        public CalRays()
        {

        }

        //轨迹数据字典
        // key："cellid,gxid,gyid"
        // value: TrajInfo{ key: "trajID", value: List<NodeInfo> }
        public static Dictionary<string, TrajInfo> buildingGrids(ref DataTable tb)
        {
            Dictionary<string, TrajInfo> rayDic = new Dictionary<string, TrajInfo>();

            //DataTable tb = new DataTable();
            //tb = IbatisHelper.ExecuteQueryForDataTable("getRays", null);

            double h = (int)GridHelper.getInstance().getGHeight();
            if (tb.Rows.Count < 1)
            {
                return rayDic;
            }
            else
            {
                for (int i = 0; i < tb.Rows.Count; i++)
                {
                    int cellID = Convert.ToInt32(tb.Rows[i]["cellID"].ToString());
                    int Gxid = Convert.ToInt32(tb.Rows[i]["gxid"].ToString());
                    int Gyid = Convert.ToInt32(tb.Rows[i]["gyid"].ToString());
                    int trajID = Convert.ToInt32(tb.Rows[i]["trajID"].ToString());
                    double EmitPwrW = Convert.ToDouble(tb.Rows[i]["emitPwrW"].ToString());
                    int rayType = Convert.ToInt32(tb.Rows[i]["rayType"].ToString());
                    int rayLevel = Convert.ToInt32(tb.Rows[i]["rayLevel"].ToString());
                    double distance = Convert.ToDouble(tb.Rows[i]["distance"].ToString());
                    double Angle = Convert.ToDouble(tb.Rows[i]["angle"].ToString());
                    double attenuation = Convert.ToDouble(tb.Rows[i]["attenuation"].ToString());
                    double RecePwrW = Convert.ToDouble(tb.Rows[i]["recePwrW"].ToString());
                    string scen = tb.Rows[i]["proportion"].ToString();
                    int ptScen = Convert.ToInt32(tb.Rows[i]["endPointScen"].ToString());

                    //射线段读入内存结构时，将射线段的起点和终点也读进来，jinhj
                    double pointOfIncidenceX= Convert.ToDouble(tb.Rows[i]["rayStartPointX"].ToString());
                    double pointOfIncidenceY = Convert.ToDouble(tb.Rows[i]["rayStartPointY"].ToString());
                    double pointOfIncidenceZ = Convert.ToDouble(tb.Rows[i]["rayStartPointZ"].ToString());
                    Point pointOfIncidence = new Point(pointOfIncidenceX, pointOfIncidenceY, pointOfIncidenceZ);

                    double crossPointX = Convert.ToDouble(tb.Rows[i]["rayEndPointX"].ToString());
                    double crossPointY = Convert.ToDouble(tb.Rows[i]["rayEndPointY"].ToString());
                    double crossPointZ = Convert.ToDouble(tb.Rows[i]["rayEndPointZ"].ToString());
                    Point crossPoint = new Point(crossPointX, crossPointY, crossPointZ);


                    RayType rayT = new RayType();
                    switch (rayType)
                    {
                        case 0:
                            rayT = RayType.Direction;
                            break;
                        case 1:
                            rayT = RayType.VReflection;
                            break;
                        case 2:
                            rayT = RayType.HReflection;
                            break;
                        case 3:
                            rayT = RayType.HDiffraction;
                            break;
                        case 4:
                            rayT = RayType.VDiffraction;
                            break;
                    }

                    // //射线段读入内存结构时，将射线段的起点和终点也读进来，jinhj
                    NodeInfo ni = new NodeInfo(cellID, Gxid, Gyid, trajID, rayT, distance, Angle, attenuation, RecePwrW, pointOfIncidence, crossPoint);

                    // 射线经过各场景的距离
                    string[] scenArr = scen.Split(';');
                    int n = scenArr.Count();
                    ni.trajScen = new double[n];
                    for (int j = 0; j < n; j++)
                        ni.trajScen[j] = Convert.ToDouble(scenArr[j]) * distance;
                    ni.endPointScen = ptScen;

                    string key = string.Format("{0},{1},{2}", cellID, Gxid, Gyid);
                    if (rayDic.Keys.Contains(key))  // 存在 rayDic[key]
                    {
                        if (rayDic[key].traj.Keys.Contains(trajID))  // 存在 rayDic[key].traj[trajID]
                        {
                            rayDic[key].traj[trajID].rayList.Add(ni);
                            if (EmitPwrW != 0)    // 射线轨迹的第一段
                                rayDic[key].traj[trajID].emitPwrW = EmitPwrW;
                            if (ni.recePwr != 0)  // 射线轨迹的最后一段
                            {
                                rayDic[key].traj[trajID].recePwrW = ni.recePwr;
                                rayDic[key].sumReceivePwrW += ni.recePwr;
                            }
                        }
                        else
                        {
                            RayInfo rays = new RayInfo();
                            rays.rayList.Add(ni);
                            rayDic[key].traj[trajID] = rays;
                            if (EmitPwrW != 0)    // 射线轨迹的第一段
                                rayDic[key].traj[trajID].emitPwrW = EmitPwrW;
                            if (ni.recePwr != 0)  // 射线轨迹的最后一段
                            {
                                rayDic[key].traj[trajID].recePwrW = ni.recePwr;
                                rayDic[key].sumReceivePwrW += ni.recePwr;
                            }
                        }
                    }
                    else
                    {
                        RayInfo rays = new RayInfo();
                        rays.rayList.Add(ni);
                        TrajInfo ti = new TrajInfo();
                        ti.traj[trajID] = rays;
                        rayDic[key] = ti;
                        if (EmitPwrW != 0)    // 射线轨迹的第一段
                            rayDic[key].traj[trajID].emitPwrW = EmitPwrW;
                        if (ni.recePwr != 0)  // 射线轨迹的最后一段
                        {
                            rayDic[key].traj[trajID].recePwrW = ni.recePwr;
                            rayDic[key].sumReceivePwrW += ni.recePwr;
                        }
                    }
                }
            }

            foreach (string key in rayDic.Keys)
                rayDic[key].sumPwrDbm = ConvertUtil.convertw2dbm(rayDic[key].sumReceivePwrW);

            return rayDic;
        }

        //路测数据字典,by jinhj
        //key：“cellid,gxid,gyid”
        //value: List<double> cellIdList
        public static Dictionary<string, List<double>> getMeaPwrFromtbDT(ref DataTable dtTb)
        {
            Dictionary<string, List<double>> dtDic = new Dictionary<string, List<double>>();
            for (int i = 0; i < dtTb.Rows.Count; i++) {
                //拼接key
                if (dtTb.Rows[i]["x"] == System.DBNull.Value || dtTb.Rows[i]["y"] == System.DBNull.Value) {
                    continue;
                }
                double x = double.Parse(dtTb.Rows[i]["x"].ToString());
                double y = double.Parse(dtTb.Rows[i]["y"].ToString());
                int gxid = -1;
                int gyid = -1;
                GridHelper.getInstance().XYToGGrid(x, y, ref gxid, ref gyid);
                string cellId = dtTb.Rows[i]["CI"].ToString();
                string key = string.Format("{0},{1},{2}", cellId, gxid, gyid);

                //if (key == "35653633,4045,4427") {
                //    int a = 1;
                //}

                //路测接收信号强度放入value
                double pwrDbm = double.Parse(dtTb.Rows[i]["RSRP"].ToString());
                if (!dtDic.ContainsKey(key)) {
                    dtDic[key] = new List<double>();
                }
                dtDic[key].Add(pwrDbm);
            }
            return dtDic;
        }

        //路测数据字典,by jinhj
        //key：“cellid,gxid,gyid”
        //value: List<DTInfo> cellIdList
        public static Dictionary<string, List<DTInfo>> getDTInfoFromtbDT(ref DataTable dtTb)
        {
            Dictionary<string, List<DTInfo>> dtDic = new Dictionary<string, List<DTInfo>>();
            for (int i = 0; i < dtTb.Rows.Count; i++)
            {
                //拼接key
                if (dtTb.Rows[i]["x"] == System.DBNull.Value || dtTb.Rows[i]["y"] == System.DBNull.Value)
                {
                    continue;
                }
                double x = double.Parse(dtTb.Rows[i]["x"].ToString());
                double y = double.Parse(dtTb.Rows[i]["y"].ToString());
                int gxid = -1;
                int gyid = -1;
                GridHelper.getInstance().XYToGGrid(x, y, ref gxid, ref gyid);
                string cellId = dtTb.Rows[i]["CI"].ToString();
                string key = string.Format("{0},{1},{2}", cellId, gxid, gyid);

                //if (key == "35653633,4045,4427") {
                //    int a = 1;
                //}

                //路测接收信号强度放入value
                double pwrDbm = double.Parse(dtTb.Rows[i]["RSRP"].ToString());
                double distance = double.Parse(dtTb.Rows[i]["SCell_Dist"].ToString());
                DTInfo dtInfo = new DTInfo(pwrDbm,distance);

                if (!dtDic.ContainsKey(key))
                {
                    dtDic[key] = new List<DTInfo>();
                }
                dtDic[key].Add(dtInfo);
            }
            return dtDic;
        }

        // 模拟路测，是根据射线轨迹得到的路测加上随机扰动的结果
        // coef：第一维为场景，第二维为各校正系数，依次为直射、反射、绕射
        public static Dictionary<string, List<double>> getMeaPwr(ref Dictionary<string, TrajInfo> rayDic, int scenNum)
        {
            int cnt = rayDic.Count;  // 路测点数量
            Dictionary<string, List<double>> meaPwrNopt = new Dictionary<string, List<double>>();  // 模拟的真实路测
            double[] BigObj = new double[cnt];  // 整体目标
            double[] SmaObj = new double[cnt];  // 局部目标
            RandomToND rtnd = new RandomToND();
            rtnd.RandomTo(ref BigObj, ref SmaObj);  // 随机扰动
            
            int indexB = 0;

            foreach (string key in rayDic.Keys)
            {
                meaPwrNopt[key] = new List<double>();
                meaPwrNopt[key].Add(rayDic[key].sumPwrDbm + BigObj[indexB++]);
            }

            return meaPwrNopt;
        }
    }
}
