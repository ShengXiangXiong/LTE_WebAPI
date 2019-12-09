using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace LTE.DB
{
    // 数据库连接相关的2个dll、3份配置文件：
    // IBatisNet.Common.dll、IBatisNet.DataMapper.dll
    // providers.config、SqlMap.config、DataServiceSqlMap.xml

    public class DataUtil
    {
        /// <summary>
        /// 链接数据库字符串
        /// 数据库用户名、密码：SqlMap.config
        /// </summary>
        public static string ConnectionString = "Data Source=10.103.252.26;Initial Catalog=NJCover3D;user id=sa;password=123456";

        ///<summary>
        /// 获取连接
        /// </summary>
        /// <returns></returns>
        public static System.Data.IDbConnection GetConnection()
        {
            try
            {
                System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(ConnectionString);
                conn.Open();
                return conn;
            }
            catch (System.Data.SqlClient.SqlException sqlExp)
            {
                //没有可用的数据库连接，可能因为网络不通或者网络中断等
                throw new Exception(sqlExp.Message);
            }
            catch (System.Exception exp)
            {
                //连接池用完，或者因数据库连接不可用，或者因连接超时等，都可引起异常
                throw new Exception(exp.Message);
            }
        }

        /// <summary>
        /// BCP导入数据库
        /// </summary>
        /// <param name="dataTable">DataTable</param>
        /// <param name="tableName">数据库中对应的表名</param>
        public static void BCPDataTableImport(DataTable dataTable, string tableName)
        {
            using (SqlBulkCopy bcp = new SqlBulkCopy(DataUtil.ConnectionString))
            {
                bcp.DestinationTableName = tableName;
                bcp.BulkCopyTimeout = 600;

                try
                {
                    bcp.WriteToServer(dataTable);
                }
                catch (Exception e)
                {
                    //throw new Exception("写入数据库失败!");
                    int cnt = dataTable.Rows.Count, mid = cnt >> 1;
                    Console.WriteLine("bulk copy to table {0} ({1} data) failed: {2}", tableName, cnt, e.Message);
                    if (cnt == 1)
                    {
                        return;
                    }
                    Console.WriteLine("trying bulk copy in 2 batches ({0} data) ...", cnt);
                    DataTable table = dataTable.Clone();
                    for (int i = 0; i < mid; i++)
                    {
                        table.ImportRow(dataTable.Rows[i]);
                    }
                    BCPDataTableImport(table, tableName);
                    table.Clear();
                    for (int i = mid; i < cnt; i++)
                    {
                        table.ImportRow(dataTable.Rows[i]);
                    }
                    BCPDataTableImport(table, tableName);
                    table.Clear();
                    Console.WriteLine("bulk copy done ({0} data).", cnt);
                }
                finally
                {
                    bcp.Close();
                }
            }

        }
    }
}
