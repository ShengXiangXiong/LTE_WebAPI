using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections;

using IBatisNet.DataMapper.Configuration;
using IBatisNet.DataMapper.Configuration.Statements;
using IBatisNet.DataMapper;
using IBatisNet.DataMapper.Exceptions;
using IBatisNet.Common.Pagination;
using IBatisNet.DataMapper.MappedStatements;
using IBatisNet.Common;
using IBatisNet.DataMapper.Scope;
using System.Windows;

namespace LTE.DB
{
    /// <summary>
    /// Ibatis数据访问
    /// </summary>
    public class IbatisHelper
    {
        //private  ISqlMapper sqlMap = Mapper(); //sqlMaper引用

        /// <summary>
        /// 获得IBatis的sqlmapper对象
        /// </summary>
        /// <returns></returns>
        public static ISqlMapper Mapper()
        {
            ISqlMapper sqlMap = IBatisNet.DataMapper.Mapper.Instance();
            if (!sqlMap.IsSessionStarted)
            {
                sqlMap.OpenConnection();
            }

            return sqlMap;


            //不使用默认配置文件,用下面方法
            //DomSqlMapBuilder builder = new DomSqlMapBuilder();
            //string path = AppDomain.CurrentDomain.BaseDirectory + "..\\Bin"; //更改路径
            //ISqlMapper sqlMap = builder.Configure(path);
            //if (!sqlMap.IsSessionStarted)
            //{
            //    sqlMap.OpenConnection();
            //}
            //return sqlMap;
        }

        // 获取表名
        public static ArrayList getTableNames()
        {
            ArrayList tableNames = new ArrayList();
            DataTable dt = IbatisHelper.ExecuteQueryForDataTable("GetTableNames", null);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                tableNames.Add(dt.Rows[i][0].ToString());
            }
            
            return tableNames;

        }

        // 获取表中的属性名
        public static ArrayList getAttrName(string tableName)//返回数据库中的属性名
        {
            ArrayList attrName = new ArrayList();

            Hashtable ht = new Hashtable();
            ht["tableName"] = tableName;
            DataTable dt = IbatisHelper.ExecuteQueryForDataTable("GetAttrNames", ht);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                attrName.Add(dt.Rows[i][0].ToString());
            }

            return attrName;
        }

        /// <summary>
        /// 执行添加
        /// </summary>
        /// <param name="statementName">操作名</param>
        /// <param name="parameterObject">参数</param>
        public static object ExecuteInsert(string statementName, object parameterObject)
        {
            ISqlMapper sqlMap = Mapper();
            object obj = null;
            try
            {
                //分布式事务操作方法
                //using (TransactionScope tx = new TransactionScope())
                //{
                if (!sqlMap.IsSessionStarted)
                {
                    sqlMap.OpenConnection();
                }
                obj = sqlMap.Insert(statementName, parameterObject);
                sqlMap.CloseConnection();
                //    tx.Complete();
                //}

            }
            catch (Exception e)
            {

                throw new DataMapperException("Error executing query '" + statementName + "' for insert.  Cause: " + e.Message, e);
            }

            return obj;


        }

        /// <summary>
        /// 执行修改
        /// </summary>
        /// <param name="statementName">操作名</param>
        /// <param name="parameterObject">参数，里面存放的是一个具体对象的list集合（key:对象名，value:对象list）</param>
        /// <returns>返回影响行数</returns>
        public static int ExecuteUpdate(string statementName, object parameterObject)
        {
            ISqlMapper sqlMap = Mapper();
            int number = 0;
            try
            {
                //分布式事务操作方法
                //using (TransactionScope tx = new TransactionScope())
                //{
                if (!sqlMap.IsSessionStarted)
                {
                    sqlMap.OpenConnection();
                }
                number = sqlMap.Update(statementName, parameterObject);
                sqlMap.CloseConnection();
                //    tx.Complete();
                //}
            }
            catch (Exception e)
            {
                throw new DataMapperException("Error executing query '" + statementName + "' for update.  Cause: " + e.Message, e);
            }
            return number;
        }


        /// <summary>
        /// 执行删除
        /// </summary>
        /// <param name="statementName">操作名</param>
        /// <param name="parameterObject">参数</param>
        /// <returns>返回影响行数</returns>
        public static int ExecuteDelete(string statementName, object parameterObject = null)
        {
            ISqlMapper sqlMap = Mapper();
            int number = 0;
            try
            {
                //分布式事务操作方法
                //using (TransactionScope tx = new TransactionScope())
                //{
                if (!sqlMap.IsSessionStarted)
                {
                    sqlMap.OpenConnection();
                }

                number = sqlMap.Delete(statementName, parameterObject);
                //    tx.Complete();
                //}
            }
            catch (Exception e)
            {
                throw new DataMapperException("Error executing query '" + statementName + "' for delete.  Cause: " + e.Message, e);
            }
            return number;
        }

        /// <summary>
        /// 无条件执行SQL语句
        /// </summary>
        public static void ExecuteNonQuery(string sql)
        {
            IDbCommand command = new System.Data.SqlClient.SqlCommand
            {
                CommandText = sql,

                //command.Connection = DataUtil.GetConnection();
                CommandTimeout = 0
            };

            using (command.Connection)
            {
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 无条件执行SQL语句,注:不支持#变量替换
        /// </summary>
        /// <param name="statementName">iBatis命令名称</param>
        /// <param name="parameterObject">指定的参数</param>
        public static void ExecuteNonQuery(string statementName, object parameterObject)
        {
            IDbCommand command = GetCommand(statementName, parameterObject);
            //command.Connection = DataUtil.GetConnection();
            command.CommandTimeout = 0;
            using (command.Connection)
            {
                command.ExecuteNonQuery();
            }
        }


        /// <summary>
        /// 执行SQL语句并将结果返回第一行第一列,注:不支持#变量替换
        /// </summary>
        /// <param name="statementName">iBatis命令名称</param>
        /// <param name="parameterObject">指定的参数</param>
        /// <returns>返回第一行第一列的值</returns>
        public static object ExecuteScalar(string statementName, object parameterObject)
        {
            IDbCommand command = GetCommand(statementName, parameterObject);
            command.Connection = DataUtil.GetConnection();
            command.CommandTimeout = 0;
            using (command.Connection)
            {
                return command.ExecuteScalar();
            }
        }

        /// <summary>
        /// 得到列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="statementName">操作名称，对应xml中的Statement的id</param>
        /// <param name="parameterObject">参数</param>
        /// <returns></returns>
        public static IList<T> ExecuteQueryForList<T>(string statementName, object parameterObject)
        {
            ISqlMapper sqlMap = Mapper();
            try
            {
                return sqlMap.QueryForList<T>(statementName, parameterObject);
            }
            catch (Exception e)
            {
                throw new DataMapperException("Error executing query '" + statementName + "' for list.  Cause: " + e.Message, e);
            }
        }


        /// <summary>
        /// 得到指定数量的记录数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="statementName"></param>
        /// <param name="parameterObject">参数</param>
        /// <param name="skipResults">跳过的记录数</param>
        /// <param name="maxResults">最大返回的记录数</param>
        /// <returns></returns>
        public static IList<T> ExecuteQueryForList<T>(string statementName, object parameterObject, int skipResults, int maxResults)
        {
            ISqlMapper sqlMap = Mapper();
            try
            {
                return sqlMap.QueryForList<T>(statementName, parameterObject, skipResults, maxResults);
            }
            catch (Exception e)
            {
                throw new DataMapperException("Error executing query '" + statementName + "' for list.  Cause: " + e.Message, e);
            }
        }

        /// <summary>
        /// 得到分页的列表
        /// </summary>
        /// <param name="statementName">操作名称</param>
        /// <param name="parameterObject">参数</param>
        /// <param name="pageSize">每页记录数</param>
        /// <returns></returns>
        /* public static IPaginatedList ExecuteQueryForPaginatedList(string statementName, object parameterObject, int pageSize)
         {
             ISqlMapper sqlMap = Mapper();
             try
             {
                 return sqlMap.QueryForPaginatedList(statementName, parameterObject, pageSize);
             }
             catch (Exception e)
             {
                 throw new DataMapperException("Error executing query '" + statementName + "' for paginated list.  Cause: " + e.Message, e);
             }

         }*/

        /// <summary>
        /// 查询得到对象的一个实例
        /// </summary>
        /// <typeparam name="T">对象type</typeparam>
        /// <param name="statementName">操作名</param>
        /// <param name="parameterObject">参数</param>
        /// <returns></returns>
        public static T ExecuteQueryForObject<T>(string statementName, object parameterObject)
        {
            ISqlMapper sqlMap = Mapper();
            try
            {
                return sqlMap.QueryForObject<T>(statementName, parameterObject);
            }
            catch (Exception e)
            {
                throw new DataMapperException("Error executing query '" + statementName + "' for object.  Cause: " + e.Message, e);
            }
        }

        /// <summary>
        /// 查询得到对象的一个实例,注:不支持#变量替换
        /// </summary>
        /// <typeparam name="T">对象type</typeparam>
        /// <param name="statementName">操作名</param>
        /// <param name="parameterObject">参数</param>
        /// <returns></returns>
        public static object ExecuteQueryForObject(string statementName, object parameterObject)
        {
            ISqlMapper sqlMap = Mapper();
            try
            {
                return sqlMap.QueryForObject(statementName, parameterObject);
            }
            catch (Exception e)
            {
                throw new DataMapperException("Error executing query '" + statementName + "' for object.  Cause: " + e.Message, e);
            }
        }

        /// <summary>
        /// 通用的以DataTable的方式得到Select的结果(xml文件中参数要使用$标记的占位参数. 注:不支持#变量替换)
        /// </summary>
        /// <param name="statementName">语句ID</param>
        /// <param name="paramObject">语句所需要的参数</param>
        /// <returns>得到的DataTable</returns>
        public static DataTable ExecuteQueryForDataTable(string statementName, object paramObject)
        {
            ISqlMapper sqlMap = Mapper();
            DataSet ds = new DataSet();


            bool isSessionLocal = false;
            IDalSession session = sqlMap.LocalSession;
            if (session == null)
            {
                session = new SqlMapSession(sqlMap);
                session.OpenConnection();
                isSessionLocal = true;
            }


            IDbCommand cmd = GetCommand(statementName, paramObject);
            cmd.CommandTimeout = 0;

            try
            {
                cmd.Connection = session.Connection;
                IDbDataAdapter adapter = session.CreateDataAdapter(cmd);
                adapter.Fill(ds);
            }
            catch (Exception e)
            {
                throw new DataMapperException("Error executing query '" + statementName + "' for object.  Cause: " + e.Message, e);
            }
            finally
            {
                if (isSessionLocal)
                {
                    session.CloseConnection();
                }
            }

            return ds.Tables[0];

        }

        /// <summary>
        /// 用的以DataSet的方式得到Select的结果(xml文件中参数要使用$标记的占位参数. 注:不支持#变量替换)
        /// </summary>
        /// <param name="statementName"></param>
        /// <param name="paramObject"></param>
        /// <returns></returns>
        public static DataSet ExecuteQueryForDataSet(string statementName, object paramObject)
        {
            ISqlMapper sqlMap = Mapper();
            DataSet ds = new DataSet();


            bool isSessionLocal = false;
            IDalSession session = sqlMap.LocalSession;
            if (session == null)
            {
                session = new SqlMapSession(sqlMap);
                session.OpenConnection();
                isSessionLocal = true;
            }


            IDbCommand cmd = GetCommand(statementName, paramObject);
            cmd.CommandTimeout = 0;

            try
            {
                cmd.Connection = session.Connection;

                IDbDataAdapter adapter = session.CreateDataAdapter(cmd);
                adapter.Fill(ds);
            }
            catch (Exception e)
            {
                throw new DataMapperException("Error executing query '" + statementName + "' for object.  Cause: " + e.Message, e);
            }
            finally
            {
                if (isSessionLocal)
                {
                    session.CloseConnection();
                }
            }

            return ds;

        }
        /// <summary>
        /// 通用的以DataTable的方式得到Select的结果(xml文件中参数要使用$标记的占位参数,注:不支持#变量替换)
        /// </summary>
        /// <param name="statementName">语句ID</param>
        /// <param name="paramObject">语句所需要的参数</param>
        /// <param name="htOutPutParameter">Output参数值哈希表</param>
        /// <returns>得到的DataTable</returns>
        public static DataTable ExecuteQueryForDataTable(string statementName, object paramObject, out Hashtable htOutPutParameter)
        {
            ISqlMapper sqlMap = Mapper();
            DataSet ds = new DataSet();
            bool isSessionLocal = false;
            IDalSession session = sqlMap.LocalSession;
            if (session == null)
            {
                session = new SqlMapSession(sqlMap);
                session.OpenConnection();
                isSessionLocal = true;
            }

            IDbCommand cmd = GetCommand(statementName, paramObject);
            cmd.CommandTimeout = 0;

            try
            {
                cmd.Connection = session.Connection;
                IDbDataAdapter adapter = session.CreateDataAdapter(cmd);
                adapter.Fill(ds);
            }
            catch (Exception e)
            {
                throw new DataMapperException("Error executing query '" + statementName + "' for object.  Cause: " + e.Message, e);
            }
            finally
            {
                if (isSessionLocal)
                {
                    session.CloseConnection();
                }
            }

            htOutPutParameter = new Hashtable();
            foreach (IDataParameter parameter in cmd.Parameters)
            {
                if (parameter.Direction == ParameterDirection.Output)
                {
                    htOutPutParameter[parameter.ParameterName] = parameter.Value;
                }
            }


            return ds.Tables[0];

        }


        /// <summary>
        /// 根据iBatis命令名称和指定的参数获得一个数据库命令对象
        /// </summary>
        /// <param name="statementName">操作名</param>
        /// <param name="parameterObject">参数</param>
        /// <returns>数据库命令对象</returns>
        public static System.Data.IDbCommand GetCommand(string statementName, object parameterObject)
        {
            ISqlMapper sqlMap = Mapper();
            IMappedStatement mapStatement = sqlMap.GetMappedStatement(statementName);
            IStatement statement = mapStatement.Statement;
            if (!sqlMap.IsSessionStarted)
            {
                sqlMap.OpenConnection();
            }
            RequestScope request = statement.Sql.GetRequestScope(mapStatement, parameterObject, sqlMap.LocalSession);
            mapStatement.PreparedCommand.Create(request, sqlMap.LocalSession, statement, parameterObject);
            IDbCommand command = new System.Data.SqlClient.SqlCommand
            {
                CommandTimeout = 0,
                CommandText = request.IDbCommand.CommandText
            };
            return command;
        }

        /// <summary>
        /// 获取SQL查询语句(不支持#变量替换)
        /// </summary>
        /// <param name="statementName">iBatis命令名称</param>
        /// <param name="parameterObject">指定的参数</param>
        /// <returns>返回SQL的查询语句</returns>
        public static string GetSql(string statementName, object parameterObject)
        {
            ISqlMapper sqlMap = Mapper();
            IMappedStatement mapStatement = sqlMap.GetMappedStatement(statementName);
            IStatement statement = mapStatement.Statement;
            RequestScope request = statement.Sql.GetRequestScope(mapStatement, parameterObject, new SqlMapSession(sqlMap));
            return request.PreparedStatement.PreparedSql;
        }

    }
}
