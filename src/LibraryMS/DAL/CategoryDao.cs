using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LibraryMS.Models;

namespace LibraryMS.DAL
{
    public class CategoryDao
    {
        public List<ReaderCategory> GetAll()
        {
            const string sql =
                "SELECT CategoryId, CategoryName, MaxBorrowCount, MaxBorrowDays " +
                "FROM dbo.ReaderCategory ORDER BY CategoryId";
            var list = new List<ReaderCategory>();
            using (SqlConnection conn = DbHelper.CreateConnection())
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, null, sql))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(Map(reader));
                    }
                }
            }
            return list;
        }

        public ReaderCategory GetById(int categoryId)
        {
            const string sql =
                "SELECT CategoryId, CategoryName, MaxBorrowCount, MaxBorrowDays " +
                "FROM dbo.ReaderCategory WHERE CategoryId = @id";
            using (SqlConnection conn = DbHelper.CreateConnection())
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, null, sql))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = categoryId;
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? Map(reader) : null;
                }
            }
        }

        public bool NameExists(string categoryName, int excludeId)
        {
            const string sql =
                "SELECT COUNT(*) FROM dbo.ReaderCategory WHERE CategoryName = @n AND CategoryId <> @id";
            using (SqlConnection conn = DbHelper.CreateConnection())
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, null, sql))
            {
                cmd.Parameters.Add("@n", SqlDbType.NVarChar, 20).Value = categoryName;
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = excludeId;
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        /// <summary>新增类别：类别号在事务内取 MAX+1，避免并发重复。</summary>
        public int Insert(ReaderCategory category)
        {
            using (SqlConnection conn = DbHelper.CreateConnection())
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        int newId;
                        using (SqlCommand cmd = DbHelper.CreateCommand(conn, tran,
                            "SELECT ISNULL(MAX(CategoryId), 0) + 1 FROM dbo.ReaderCategory WITH (UPDLOCK, HOLDLOCK)"))
                        {
                            newId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        if (newId > 99)
                        {
                            throw new Common.BizException("E4001", "类别号已用尽（最多 99 个类别）。");
                        }

                        using (SqlCommand cmd = DbHelper.CreateCommand(conn, tran,
                            "INSERT INTO dbo.ReaderCategory (CategoryId, CategoryName, MaxBorrowCount, MaxBorrowDays) " +
                            "VALUES (@id, @n, @c, @d)"))
                        {
                            cmd.Parameters.Add("@id", SqlDbType.Int).Value = newId;
                            cmd.Parameters.Add("@n", SqlDbType.NVarChar, 20).Value = category.CategoryName;
                            cmd.Parameters.Add("@c", SqlDbType.Int).Value = category.MaxBorrowCount;
                            cmd.Parameters.Add("@d", SqlDbType.Int).Value = category.MaxBorrowDays;
                            cmd.ExecuteNonQuery();
                        }

                        tran.Commit();
                        return newId;
                    }
                    catch
                    {
                        try { tran.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }

        public void Update(ReaderCategory category)
        {
            const string sql =
                "UPDATE dbo.ReaderCategory SET CategoryName = @n, MaxBorrowCount = @c, MaxBorrowDays = @d " +
                "WHERE CategoryId = @id";
            using (SqlConnection conn = DbHelper.CreateConnection())
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, null, sql))
            {
                cmd.Parameters.Add("@n", SqlDbType.NVarChar, 20).Value = category.CategoryName;
                cmd.Parameters.Add("@c", SqlDbType.Int).Value = category.MaxBorrowCount;
                cmd.Parameters.Add("@d", SqlDbType.Int).Value = category.MaxBorrowDays;
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = category.CategoryId;
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private static ReaderCategory Map(SqlDataReader reader)
        {
            return new ReaderCategory
            {
                CategoryId = reader.GetInt32(0),
                CategoryName = reader.GetString(1),
                MaxBorrowCount = reader.GetInt32(2),
                MaxBorrowDays = reader.GetInt32(3)
            };
        }
    }
}
