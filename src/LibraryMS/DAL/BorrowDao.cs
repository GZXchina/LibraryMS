using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LibraryMS.Models;

namespace LibraryMS.DAL
{
    public class BorrowDao
    {
        private const string ViewColumns =
            "BorrowId, ReaderId, ReaderName, CategoryName, BookId, BookTitle, Author, " +
            "BorrowDate, DueDate, ReturnDate, Status, OverdueDays";

        /// <summary>
        /// 在借书事务内插入借阅记录：应还日期由数据库时间计算（DATEADD），避免应用机时钟漂移。
        /// </summary>
        public BorrowResult Insert(SqlConnection conn, SqlTransaction tran,
                                   string readerId, int bookId, int maxBorrowDays)
        {
            const string sql =
                "INSERT INTO dbo.Borrows (ReaderId, BookId, DueDate) " +
                "OUTPUT INSERTED.BorrowId, INSERTED.DueDate " +
                "VALUES (@rid, @bid, DATEADD(DAY, @days, CAST(GETDATE() AS DATE)))";

            using (SqlCommand cmd = DbHelper.CreateCommand(conn, tran, sql))
            {
                cmd.Parameters.Add("@rid", SqlDbType.Char, 10).Value = readerId;
                cmd.Parameters.Add("@bid", SqlDbType.Int).Value = bookId;
                cmd.Parameters.Add("@days", SqlDbType.Int).Value = maxBorrowDays;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new InvalidOperationException("借阅记录写入失败。");
                    }
                    return new BorrowResult
                    {
                        BorrowId = Convert.ToInt32(reader["BorrowId"]),
                        ReaderId = readerId,
                        DueDate = Convert.ToDateTime(reader["DueDate"])
                    };
                }
            }
        }

        /// <summary>还书事务内锁定借阅行并读取图书信息与超期天数。</summary>
        public BorrowReturnInfo GetForReturn(SqlConnection conn, SqlTransaction tran, int borrowId)
        {
            const string sql =
                "SELECT b.BorrowId, b.BookId, bk.Title, b.DueDate, b.ReturnDate, " +
                "       DATEDIFF(DAY, b.DueDate, CAST(GETDATE() AS DATE)) AS DaysOverdue " +
                "FROM dbo.Borrows b WITH (UPDLOCK, HOLDLOCK) " +
                "JOIN dbo.Books bk ON bk.BookId = b.BookId " +
                "WHERE b.BorrowId = @id";

            using (SqlCommand cmd = DbHelper.CreateCommand(conn, tran, sql))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = borrowId;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    return new BorrowReturnInfo
                    {
                        BorrowId = Convert.ToInt32(reader["BorrowId"]),
                        BookId = Convert.ToInt32(reader["BookId"]),
                        BookTitle = Convert.ToString(reader["Title"]),
                        DueDate = Convert.ToDateTime(reader["DueDate"]),
                        ReturnDate = reader["ReturnDate"] == DBNull.Value
                            ? (DateTime?)null
                            : Convert.ToDateTime(reader["ReturnDate"]),
                        DaysOverdue = Convert.ToInt32(reader["DaysOverdue"])
                    };
                }
            }
        }

        /// <summary>条件更新：仅未归还记录可被更新，保证重复还书/并发双还的幂等性。</summary>
        public int MarkReturned(SqlConnection conn, SqlTransaction tran, int borrowId)
        {
            const string sql =
                "UPDATE dbo.Borrows SET ReturnDate = CAST(GETDATE() AS DATE) " +
                "WHERE BorrowId = @id AND ReturnDate IS NULL";
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, tran, sql))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = borrowId;
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>借阅记录查询：读者编号 / 关键字 / 状态 三个可选条件。</summary>
        public List<BorrowRecord> Query(string readerId, string keyword, string status)
        {
            string sql =
                "SELECT " + ViewColumns + " FROM dbo.V_BorrowDetails " +
                "WHERE (@rid IS NULL OR ReaderId = @rid) " +
                "  AND (@kw IS NULL OR BookTitle LIKE @like OR ReaderName LIKE @like OR ReaderId LIKE @like) " +
                "  AND (@st IS NULL OR Status = @st) " +
                "ORDER BY BorrowId DESC";

            var list = new List<BorrowRecord>();
            using (SqlConnection conn = DbHelper.CreateConnection())
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, null, sql))
            {
                cmd.Parameters.Add("@rid", SqlDbType.Char, 10).Value =
                    string.IsNullOrWhiteSpace(readerId) ? (object)DBNull.Value : readerId.Trim();
                cmd.Parameters.Add("@kw", SqlDbType.NVarChar, 50).Value =
                    string.IsNullOrWhiteSpace(keyword) ? (object)DBNull.Value : keyword.Trim();
                cmd.Parameters.Add("@like", SqlDbType.NVarChar, 200).Value =
                    string.IsNullOrWhiteSpace(keyword)
                        ? (object)DBNull.Value
                        : "%" + BookDao.EscapeLike(keyword.Trim()) + "%";
                cmd.Parameters.Add("@st", SqlDbType.NVarChar, 10).Value =
                    string.IsNullOrWhiteSpace(status) ? (object)DBNull.Value : status;

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

        public List<BorrowRecord> GetByReader(string readerId)
        {
            return Query(readerId, null, null);
        }

        /// <summary>某读者尚未归还的记录（用于按读者还书）。</summary>
        public List<BorrowRecord> GetUnreturnedByReader(string readerId)
        {
            const string sql =
                "SELECT " + ViewColumns + " FROM dbo.V_BorrowDetails " +
                "WHERE ReaderId = @rid AND ReturnDate IS NULL ORDER BY DueDate";

            var list = new List<BorrowRecord>();
            using (SqlConnection conn = DbHelper.CreateConnection())
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, null, sql))
            {
                cmd.Parameters.Add("@rid", SqlDbType.Char, 10).Value = readerId;
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

        private static BorrowRecord Map(SqlDataReader reader)
        {
            return new BorrowRecord
            {
                BorrowId = Convert.ToInt32(reader["BorrowId"]),
                ReaderId = Convert.ToString(reader["ReaderId"]),
                ReaderName = Convert.ToString(reader["ReaderName"]),
                CategoryName = Convert.ToString(reader["CategoryName"]),
                BookId = Convert.ToInt32(reader["BookId"]),
                BookTitle = Convert.ToString(reader["BookTitle"]),
                Author = Convert.ToString(reader["Author"]),
                BorrowDate = Convert.ToDateTime(reader["BorrowDate"]),
                DueDate = Convert.ToDateTime(reader["DueDate"]),
                ReturnDate = reader["ReturnDate"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(reader["ReturnDate"]),
                Status = Convert.ToString(reader["Status"]),
                OverdueDays = reader["OverdueDays"] == DBNull.Value
                    ? (int?)null
                    : Convert.ToInt32(reader["OverdueDays"])
            };
        }
    }
}
