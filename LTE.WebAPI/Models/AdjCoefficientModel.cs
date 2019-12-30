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
using LTE.Model;

namespace LTE.WebAPI.Models
{
    public class AdjCoefficientModel
    {
        public Result AdjCoefficient()
        {
            int cnt = 0;
            //初始化进度信息
            LoadInfo loadInfo = new LoadInfo();
            loadInfo.count = 1;
            loadInfo.loadCreate();

            DataTable dt11 = DB.IbatisHelper.ExecuteQueryForDataTable("GetClusterShpState", null);  // Ibatis 数据访问，判断用户是否做了图层生成
            if (dt11.Rows[0][0].ToString() == "0")
            { return new Result(false, "用户未进行图层生成"); }

            DataTable dt23 = DB.IbatisHelper.ExecuteQueryForDataTable("GetAdjcoefficienttoDBState", null);  // Ibatis 数据访问，判断用户是否做了矫正系数，做了则删除它
            if (dt23.Rows[0][0].ToString() == "1")//做了矫正系数
            {
                try//更新矫正系数表，前提条件表
                {
                    IbatisHelper.ExecuteDelete("deleteAdjcoefficient", null);
                    IbatisHelper.ExecuteUpdate("UpdatetbDependTableDuetoAdjcoefficienttoDB", null);
                }
                catch (Exception ex)
                { return new Result(false, ex.ToString()); }
            }

            string xmlpath = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "coefficient.xml";//xml文件位置
                                                                                                             //读xml文件获取参数
            XDocument document = XDocument.Load(xmlpath);
            XElement root = document.Root;
            XElement ele = root.Element("parameter"); 
            double DirectCoefficient_Scene0, ReflectCoefficientA_Scene0, ReflectCoefficientB_Scene0, ReflectCoefficientC_Scene0, DiffracteCoefficientA_Scene0, DiffracteCoefficientB_Scene0, DiffracteCoefficientC_Scene0;
            double DirectCoefficient_Scene1, ReflectCoefficientA_Scene1, ReflectCoefficientB_Scene1, ReflectCoefficientC_Scene1, DiffracteCoefficientA_Scene1, DiffracteCoefficientB_Scene1, DiffracteCoefficientC_Scene1;
            double DirectCoefficient_Scene2, ReflectCoefficientA_Scene2, ReflectCoefficientB_Scene2, ReflectCoefficientC_Scene2, DiffracteCoefficientA_Scene2, DiffracteCoefficientB_Scene2, DiffracteCoefficientC_Scene2;
            double DirectCoefficient_Scene3, ReflectCoefficientA_Scene3, ReflectCoefficientB_Scene3, ReflectCoefficientC_Scene3, DiffracteCoefficientA_Scene3, DiffracteCoefficientB_Scene3, DiffracteCoefficientC_Scene3;
            //scene0
            XElement para = ele.Element("DirectCoefficient_Scene0");
            DirectCoefficient_Scene0 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("ReflectCoefficientA_Scene0");
            ReflectCoefficientA_Scene0 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("ReflectCoefficientB_Scene0");
            ReflectCoefficientB_Scene0 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("ReflectCoefficientC_Scene0");
            ReflectCoefficientC_Scene0 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("DiffracteCoefficientA_Scene0");
            DiffracteCoefficientA_Scene0 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("DiffracteCoefficientB_Scene0");
            DiffracteCoefficientB_Scene0 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("DiffracteCoefficientC_Scene0");
            DiffracteCoefficientC_Scene0 = Convert.ToDouble(para.Value.ToString());
            //scene1
            para = ele.Element("DirectCoefficient_Scene1");
            DirectCoefficient_Scene1 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("ReflectCoefficientA_Scene1");
            ReflectCoefficientA_Scene1 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("ReflectCoefficientB_Scene1");
            ReflectCoefficientB_Scene1 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("ReflectCoefficientC_Scene1");
            ReflectCoefficientC_Scene1 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("DiffracteCoefficientA_Scene1");
            DiffracteCoefficientA_Scene1 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("DiffracteCoefficientB_Scene1");
            DiffracteCoefficientB_Scene1 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("DiffracteCoefficientC_Scene1");
            DiffracteCoefficientC_Scene1 = Convert.ToDouble(para.Value.ToString());
            //scene2
            para = ele.Element("DirectCoefficient_Scene2");
            DirectCoefficient_Scene2 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("ReflectCoefficientA_Scene2");
            ReflectCoefficientA_Scene2 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("ReflectCoefficientB_Scene2");
            ReflectCoefficientB_Scene2 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("ReflectCoefficientC_Scene2");
            ReflectCoefficientC_Scene2 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("DiffracteCoefficientA_Scene2");
            DiffracteCoefficientA_Scene2 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("DiffracteCoefficientB_Scene2");
            DiffracteCoefficientB_Scene2 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("DiffracteCoefficientC_Scene2");
            DiffracteCoefficientC_Scene2 = Convert.ToDouble(para.Value.ToString());
            //scene3
            para = ele.Element("DirectCoefficient_Scene3");
            DirectCoefficient_Scene3 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("ReflectCoefficientA_Scene3");
            ReflectCoefficientA_Scene3 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("ReflectCoefficientB_Scene3");
            ReflectCoefficientB_Scene3 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("ReflectCoefficientC_Scene3");
            ReflectCoefficientC_Scene3 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("DiffracteCoefficientA_Scene3");
            DiffracteCoefficientA_Scene3 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("DiffracteCoefficientB_Scene3");
            DiffracteCoefficientB_Scene3 = Convert.ToDouble(para.Value.ToString());
            para = ele.Element("DiffracteCoefficientC_Scene3");
            DiffracteCoefficientC_Scene3 = Convert.ToDouble(para.Value.ToString());


            DataTable dt = new DataTable();//入库
            dt.Columns.Add("scene", Type.GetType("System.Byte"));
            dt.Columns.Add("DirectCoefficient", Type.GetType("System.Double"));
            dt.Columns.Add("ReflectCoefficientA", Type.GetType("System.Double"));
            dt.Columns.Add("ReflectCoefficientB", Type.GetType("System.Double"));
            dt.Columns.Add("ReflectCoefficientC", Type.GetType("System.Double"));
            dt.Columns.Add("DiffracteCoefficientA", Type.GetType("System.Double"));
            dt.Columns.Add("DiffracteCoefficientB", Type.GetType("System.Double"));
            dt.Columns.Add("DiffracteCoefficientC", Type.GetType("System.Double"));

            dt.Rows.Add(new object[] { "0", DirectCoefficient_Scene0.ToString(), ReflectCoefficientA_Scene0.ToString(), ReflectCoefficientB_Scene0.ToString(), ReflectCoefficientC_Scene0.ToString(), DiffracteCoefficientA_Scene0.ToString(), DiffracteCoefficientB_Scene0.ToString(), DiffracteCoefficientC_Scene0.ToString() });
            dt.Rows.Add(new object[] { "1", DirectCoefficient_Scene1.ToString(), ReflectCoefficientA_Scene1.ToString(), ReflectCoefficientB_Scene1.ToString(), ReflectCoefficientC_Scene1.ToString(), DiffracteCoefficientA_Scene1.ToString(), DiffracteCoefficientB_Scene1.ToString(), DiffracteCoefficientC_Scene1.ToString() });
            dt.Rows.Add(new object[] { "2", DirectCoefficient_Scene2.ToString(), ReflectCoefficientA_Scene2.ToString(), ReflectCoefficientB_Scene2.ToString(), ReflectCoefficientC_Scene2.ToString(), DiffracteCoefficientA_Scene2.ToString(), DiffracteCoefficientB_Scene2.ToString(), DiffracteCoefficientC_Scene2.ToString() });
            dt.Rows.Add(new object[] { "3", DirectCoefficient_Scene3.ToString(), ReflectCoefficientA_Scene3.ToString(), ReflectCoefficientB_Scene3.ToString(), ReflectCoefficientC_Scene3.ToString(), DiffracteCoefficientA_Scene3.ToString(), DiffracteCoefficientB_Scene3.ToString(), DiffracteCoefficientC_Scene3.ToString() });

            try
            {
                using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
                {
                    bcp.BatchSize = dt.Rows.Count;
                    bcp.BulkCopyTimeout = 1000;
                    bcp.DestinationTableName = "tbAdjtest";
                    bcp.WriteToServer(dt);
                    bcp.Close();
                }
                IbatisHelper.ExecuteDelete("UpdatetbDependTableAdjcoefficienttoDB", null);
                cnt++;
                loadInfo.cnt = cnt;
                loadInfo.loadUpdate();
                return new Result(true,"成功");
            }
            catch (Exception e)
            { return new Result(false,"失败"); }

        }
    }
}