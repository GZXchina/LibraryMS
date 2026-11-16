using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LibraryMS.Common;
using LibraryMS.Models;

namespace LibraryMS.DAL
{
    public class ReaderDao
    {
        private const string SelectColumns =
            "r.ReaderId, r.Name, r.Gender, r.Phone, r.CategoryId, c.CategoryName, " +
            "c.MaxBorrowCount, c.MaxBorrowDays, r.Status, r.RegisterDate";

        private const string SelectFrom =
            "FROM dbo.Readers r " +
            "JOIN dbo.ReaderCategory c ON c.CategoryId = r.CategoryId ";

        // 表提示必须紧跟表名，不能拼在 JOIN 之后
        private const string SelectFromLocked =
            "FROM dbo.Readers r WITH (UPDLOCK, HOLDLOCK) " +
            "JOIN dbo.ReaderCategory c ON c.CategoryId = r.CategoryId ";

        public Reader GetById(string readerId)
        {
            using (SqlConnection conn = DbHelper.CreateConnection())
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, null,
                "SELECT " + SelectColumns + " " + SelectFrom + "WHERE r.ReaderId = @rid"))
            {
                cmd.Parameters.Add("@rid", SqlDbType.Char, 10).Value = readerId;
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? Map(reader) : null;
                }
            }
        }

        /// <summary>
        /// 借书/注销事务内锁定读者行（UPDLOCK + HOLDLOCK），
        /// 使同一读者的并发借书/注销串行化，防止超额借阅。
        /// </summary>
        public Reader LockWithCategory(SqlConnection conn, SqlTransaction tran, string readerId)
        {
            const string sql = "SELECT " + SelectColumns + " " + SelectFromLocked +
                "WHERE r.ReaderId = @rid";
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, tran, sql))
            {
                cmd.Parameters.Add("@rid", SqlDbType.Char, 10).Value = readerId;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? Map(reader) : null;
                }
            }
        }

        public List<Reader> GetAll()
        {
            return Query("SELECT " + SelectColumns + " " + SelectFrom + "ORDER BY r.ReaderId", null);
        }

        public List<Reader> Search(string keyword)
        {
            string sql = "SELECT " + SelectColumns + " " + SelectFrom +
                "WHERE r.ReaderId LIKE @like OR r.Name LIKE @like OR r.Phone LIKE @like " +
                "ORDER BY r.ReaderId";
            return Query(sql, "%" + BookDao.EscapeLike(keyword) + "%");
        }

        private static List<Reader> Query(string sql, string likeValue)
        {
            var list = new List<Reader>();
            using (SqlConnection conn = DbHelper.CreateConnection())
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, null, sql))
            {
                if (likeValue != null)
                {
                    cmd.Parameters.Add("@like", SqlDbType.NVarChar, 200).Value = likeValue;
                }
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

        public void Insert(SqlConnection conn, SqlTransaction tran, Reader reader)
        {
            const string sql =
                "INSERT INTO dbo.Readers (ReaderId, Name, Gender, Phone, CategoryId, Status, RegisterDate) " +
                "VALUES (@rid, @n, @g, @ph, @cat, N'正常', CAST(GETDATE() AS DATE))";
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, tran, sql))
            {
                cmd.Parameters.Add("@rid", SqlDbType.Char, 10).Value = reader.ReaderId;
                cmd.Parameters.Add("@n", SqlDbType.NVarChar, 20).Value = reader.Name;
                cmd.Parameters.Add("@g", SqlDbType.NChar, 1).Value = reader.Gender;
                cmd.Parameters.Add("@ph", SqlDbType.VarChar, 20).Value =
                    (object)reader.Phone ?? DBNull.Value;
                cmd.Parameters.Add("@cat", SqlDbType.Int).Value = reader.CategoryId;
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 生成读者编号：R + 类别号2位 + 年份4位 + 年内序号3位。
        /// 必须在事务内调用：UPDLOCK+HOLDLOCK 防并发重号。
        /// </summary>
        public string GenerateReaderId(SqlConnection conn, SqlTransaction tran, int categoryId, int year)
        {
            const string sql =
                "SELECT ISNULL(MAX(CAST(SUBSTRING(ReaderId, 8, 3) AS INT)), 0) + 1 " +
                "FROM dbo.Readers WITH (UPDLOCK, HOLDLOCK) " +
                "WHERE CategoryId = @cat AND SUBSTRING(ReaderId, 4, 4) = @year";

            int sequence;
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, tran, sql))
            {
                cmd.Parameters.Add("@cat", SqlDbType.Int).Value = categoryId;
                cmd.Parameters.Add("@year", SqlDbType.VarChar, 4).Value = year.ToString("D4");
                sequence = Convert.ToInt32(cmd.ExecuteScalar());
            }

            if (sequence > 999)
            {
                throw new BizException("E2004", "该类别本年度序号已用尽（最多 999 人）。");
            }

            return "R" + categoryId.ToString("D2") + year.ToString("D4") + sequence.ToString("D3");
        }

        public int CountActiveBorrows(SqlConnection conn, SqlTransaction tran, string readerId)
        {
            const string sql =
                "SELECT COUNT(*) FROM dbo.Borrows WHERE ReaderId = @rid AND ReturnDate IS NULL";
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, tran, sql))
            {
                cmd.Parameters.Add("@rid", SqlDbType.Char, 10).Value = readerId;
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public int CountOverdue(SqlConnection conn, SqlTransaction tran, string readerId)
        {
            const string sql =
                "SELECT COUNT(*) FROM dbo.Borrows " +
                "WHERE ReaderId = @rid AND ReturnDate IS NULL AND DueDate < CAST(GETDATE() AS DATE)";
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, tran, sql))
            {
                cmd.Parameters.Add("@rid", SqlDbType.Char, 10).Value = readerId;
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public int SetStatus(string readerId, string status)
        {
            const string sql = "UPDATE dbo.Readers SET Status = @st WHERE ReaderId = @rid";
            using (SqlConnection conn = DbHelper.CreateConnection())
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, null, sql))
            {
                cmd.Parameters.Add("@st", SqlDbType.NChar, 2).Value = status;
                cmd.Parameters.Add("@rid", SqlDbType.Char, 10).Value = readerId;
                conn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>注销事务内更新状态（与锁读者行同一事务）。</summary>
        public int SetStatus(SqlConnection conn, SqlTransaction tran, string readerId, string status)
        {
            const string sql = "UPDATE dbo.Readers SET Status = @st WHERE ReaderId = @rid";
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, tran, sql))
            {
                cmd.Parameters.Add("@st", SqlDbType.NChar, 2).Value = status;
                cmd.Parameters.Add("@rid", SqlDbType.Char, 10).Value = readerId;
                return cmd.ExecuteNonQuery();
            }
        }

        public int CountAll()
        {
            return ScalarInt("SELECT COUNT(*) FROM dbo.Readers");
        }

        public int CountActive()
        {
            return ScalarInt("SELECT COUNT(*) FROM dbo.Readers WHERE Status = N'正常'");
        }

        private static int ScalarInt(string sql)
        {
            using (SqlConnection conn = DbHelper.CreateConnection())
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, null, sql))
            {
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static Reader Map(SqlDataReader reader)
        {
            return new Reader
            {
                ReaderId = Convert.ToString(reader["ReaderId"]),
                Name = Convert.ToString(reader["Name"]),
                Gender = Convert.ToString(reader["Gender"]),
                Phone = reader["Phone"] == DBNull.Value ? null : Convert.ToString(reader["Phone"]),
                CategoryId = Convert.ToInt32(reader["CategoryId"]),
                CategoryName = Convert.ToString(reader["CategoryName"]),
                MaxBorrowCount = Convert.ToInt32(reader["MaxBorrowCount"]),
                MaxBorrowDays = Convert.ToInt32(reader["MaxBorrowDays"]),
                Status = Convert.ToString(reader["Status"]),
                RegisterDate = Convert.ToDateTime(reader["RegisterDate"])
            };
        }
    }
}
