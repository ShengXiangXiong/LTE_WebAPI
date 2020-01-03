using System;
using GisClient;
using System.Data;
using LTE.DB;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTE.WebAPI.Models
{
    public class BuildingOverlayModel
    {
        public Result overlaybuilding()
        {
            DataTable dt1 = DB.IbatisHelper.ExecuteQueryForDataTable("GetFishnetShpState", null);  // Ibatis 数据访问，判断用户是否做了渔网生成
            if (dt1.Rows[0][0].ToString() == "0")
            { return new Result(false, "用户未进行渔网生成"); }
            //     else
            //     { return new Result(true, "用户未进行渔网生成"); }

            DataTable dt2 = DB.IbatisHelper.ExecuteQueryForDataTable("GetBuilding_overlayState", null);  // Ibatis 数据访问，判断用户是否做了建筑物叠加分析，做了则删除它
            if (dt2.Rows[0][0].ToString() == "1")//存在建筑物叠加图层
            {
                /*
                string[] de = new string[2];//聚类图层，建筑物叠加图层
               
                DataTable dt3 = DB.IbatisHelper.ExecuteQueryForDataTable("GetBuildingOverlayPosition", null);  // Ibatis 数据访问,得到建筑物叠加图层文件位置
                de[0] = dt3.Rows[0][0].ToString();

                DataTable dt4 = DB.IbatisHelper.ExecuteQueryForDataTable("GetClusterPosition", null);  // Ibatis 数据访问,得到聚类图层文件位置
                de[1] = dt4.Rows[0][0].ToString();

                string filepath;
                
                    filepath = de[0];//删除建筑物叠加图层
                    try
                    {
                        //    File.Delete(filepath);
                        string fileName = System.IO.Path.GetFileName(filepath);
                        string[] a = fileName.Split('.');
                        string name = a[0];
                        string b = "\\" + name + ".shp";
                        filepath = filepath.Replace(b, "");
                        DirectoryInfo Folder = new DirectoryInfo(filepath);
                        foreach (FileInfo file in Folder.GetFiles())
                        {
                            if (name == file.Name.Substring(0, file.Name.LastIndexOf('.')) || name + ".shp" == file.Name.Substring(0, file.Name.LastIndexOf('.')))
                            {
                                file.Delete();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return new Result(false, ex.ToString());
                    }
                
               
                    filepath = de[1];//删除聚类图层
                    try
                    {
                        //    File.Delete(filepath);
                        string fileName = System.IO.Path.GetFileName(filepath);
                        string[] a = fileName.Split('.');
                        string name = a[0];
                        string b = "\\" + name + ".shp";
                        filepath = filepath.Replace(b, "");
                        DirectoryInfo Folder = new DirectoryInfo(filepath);
                        foreach (FileInfo file in Folder.GetFiles())
                        {
                            if (name == file.Name.Substring(0, file.Name.LastIndexOf('.')) || name + ".shp" == file.Name.Substring(0, file.Name.LastIndexOf('.')))
                            {
                                file.Delete();
                            }
                        }
                        name = name + "_label";
                        foreach (FileInfo file in Folder.GetFiles())
                        {
                            if (name == file.Name.Substring(0, file.Name.LastIndexOf('.')) || name + ".shp" == file.Name.Substring(0, file.Name.LastIndexOf('.')))
                            {
                                file.Delete();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return new Result(false, ex.ToString());
                    }*/
                try//更新加速场景表，前提条件表
                {
                    IbatisHelper.ExecuteDelete("UpdatetbDependTableDuetoBuilding_overlay", null);
                    IbatisHelper.ExecuteUpdate("UpdatetbAccelerateGridSceneDuetoBuilding_overlay", null);
                }
                catch (Exception ex)
                { return new Result(false, ex.ToString()); }


            }

            




            try
            {
                GisClient.Result res = GisClient.ServiceApi.getGisLayerService().overlaybuilding();
                if (res.Ok)
                {
                    //更新tbDependTabled的Building_overlay
                    IbatisHelper.ExecuteUpdate("UpdatetbDependTableBuilding_overlay", null);
                    return new Result(true, res.Msg);
                }
                else
                {
                    return new Result(false, res.Msg);
                }
            }
            catch (Exception e)
            {
                return new Result(false, "远程调用失败" + e);
            }
            finally
            {
                ServiceApi.CloseConn();
            }
        }
    }
}