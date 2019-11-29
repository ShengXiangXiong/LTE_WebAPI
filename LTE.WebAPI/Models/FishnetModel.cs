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
    public class FishnetModel
    {
        public Result makeFishnet()
        {
            DataTable dt1 = DB.IbatisHelper.ExecuteQueryForDataTable("GetGridRangeState", null);  // Ibatis 数据访问，判断用户是否提供了网格范围
            if (dt1.Rows[0][0].ToString() == "0")
            { return new Result(false, "用户未提供区域范围"); }
        //    else
        //    { return new Result(true, "用户提供区域范围"); }



            DataTable dt2 = DB.IbatisHelper.ExecuteQueryForDataTable("GetFishnetShpState", null);  // Ibatis 数据访问，判断用户是否做了渔网图层，做了则删除它
            if (dt2.Rows[0][0].ToString() == "1")//存在渔网图层
            {
                /*
                string[] de = new string[5];//聚类图层，建筑物叠加图层，水面叠加图层，草地叠加图层，渔网图层及label图层
                DataTable dt3 = DB.IbatisHelper.ExecuteQueryForDataTable("GetGrassOverlayPosition", null);  // Ibatis 数据访问,得到草地叠加图层文件位置
                de[0] = dt3.Rows[0][0].ToString();

                DataTable dt4 = DB.IbatisHelper.ExecuteQueryForDataTable("GetBuildingOverlayPosition", null);  // Ibatis 数据访问,得到建筑物叠加图层文件位置
                de[1] = dt4.Rows[0][0].ToString();

                DataTable dt5 = DB.IbatisHelper.ExecuteQueryForDataTable("GetWaterOverlayPosition", null);  // Ibatis 数据访问,得到水面叠加图层文件位置
                de[2] = dt5.Rows[0][0].ToString();

                DataTable dt6 = DB.IbatisHelper.ExecuteQueryForDataTable("GetClusterPosition", null);  // Ibatis 数据访问,得到聚类图层文件位置
                de[3] = dt6.Rows[0][0].ToString();

                DataTable dt7 = DB.IbatisHelper.ExecuteQueryForDataTable("GetFishnetPosition", null);  // Ibatis 数据访问,得到渔网图层文件位置
                de[4] = dt7.Rows[0][0].ToString();

                string filepath;
                for(int i=0;i<3;i++)//删除建筑物叠加图层，水面叠加图层，草地叠加图层
                {
                    filepath = de[i];
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
                            if (name == file.Name.Substring(0, file.Name.LastIndexOf('.')) || name+".shp"== file.Name.Substring(0, file.Name.LastIndexOf('.')))
                            {
                                file.Delete();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return new Result(false, ex.ToString());
                    }
                }
                for (int i=3;i<5;i++)
                {
                    filepath = de[i];//删除聚类图层，渔网图层及label图层
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
                    }
                }*/
                try//删除加速场景表，重置前提条件表
                {
                    IbatisHelper.ExecuteDelete("DeleteFishnet", null);
                    IbatisHelper.ExecuteUpdate("UpdatetbDependTableDuetoFishnet", null);
                }
                catch (Exception ex)
                { return new Result(false, ex.ToString()); }

            }

           
            

            try
            {
                GisClient.Result res = GisClient.ServiceApi.getGisLayerService().makeFishnet();
                if (res.Ok)
                {
                    //更新tbDependTabled的FishnetShp
                    IbatisHelper.ExecuteUpdate("UpdatetbDependTableFishnetShp", null);
                    return new Result(true, res.Msg);
                }
                else
                {
                    return new Result(false,res.Msg);
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
