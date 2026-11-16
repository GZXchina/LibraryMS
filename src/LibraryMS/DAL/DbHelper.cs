using System;
using System.Configuration;
using System.Data.SqlClient;

namespace LibraryMS.DAL
{
    /// <summary>
    /// 数据访问基础设施：连接串读取、连接创建、命令创建。
    /// 所有 SQL 一律使用 SqlParameter 参数化，禁止字符串拼接。
    /// </summary>
    public static class DbHelper
    {
        public static string ConnectionString
        {
            get
            {
                ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings["LibraryDB"];
                if (settings == null)
                {
                    throw new InvalidOperationException("未找到名为 LibraryDB 的连接字符串，请检查 App.config。");
                }
                return settings.ConnectionString;
            }
        }

        public static SqlConnection CreateConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        public static SqlCommand CreateCommand(SqlConnection conn, SqlTransaction tran, string sql)
        {
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandTimeout = 5;
            if (tran != null)
            {
                cmd.Transaction = tran;
            }
            return cmd;
        }

        /// <summary>启动自检：连接数据库并确认 LibraryDB 可访问。</summary>
        public static void TestConnection()
        {
            using (SqlConnection conn = CreateConnection())
            {
                conn.Open();
            }
        }
    }
}
