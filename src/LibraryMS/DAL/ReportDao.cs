using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LibraryMS.Models;

namespace LibraryMS.DAL
{
    public class ReportDao
    {
        public OverviewStats GetOverview()
        {
            const string sql =
                "SELECT " +
                " (SELECT COUNT(*) FROM dbo.Books) AS TotalBooks, " +
                " (SELECT COUNT(*) FROM dbo.Books WHERE IsAvailable = 1) AS AvailableBooks, " +
                " (SELECT COUNT(*) FROM dbo.Readers) AS TotalReaders, " +
                " (SELECT COUNT(*) FROM dbo.Readers WHERE Status = N'正常') AS ActiveReaders, " +
                " (SELECT COUNT(*) FROM dbo.Borrows) AS TotalBorrows, " +
                " (SELECT COUNT(*) FROM dbo.Borrows WHERE ReturnDate IS NULL) AS ActiveBorrows, " +
                " (SELECT COUNT(*) FROM dbo.Borrows WHERE ReturnDate IS NULL " +
                "        AND DueDate < CAST(GETDATE() AS DATE)) AS OverdueBorrows";

            using (SqlConnection conn = DbHelper.CreateConnection())
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, null, sql))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    return new OverviewStats
                    {
                        TotalBooks = Convert.ToInt32(reader["TotalBooks"]),
                        AvailableBooks = Convert.ToInt32(reader["AvailableBooks"]),
                        TotalReaders = Convert.ToInt32(reader["TotalReaders"]),
                        ActiveReaders = Convert.ToInt32(reader["ActiveReaders"]),
                        TotalBorrows = Convert.ToInt32(reader["TotalBorrows"]),
                        ActiveBorrows = Convert.ToInt32(reader["ActiveBorrows"]),
                        OverdueBorrows = Convert.ToInt32(reader["OverdueBorrows"])
                    };
                }
            }
        }

        /// <summary>借阅次数最多的图书 Top N（单次分组查询，避免 N+1）。</summary>
        public List<BookStat> GetTopBooks(int topN)
        {
            const string sql =
                "SELECT TOP (@n) BookTitle AS Title, COUNT(*) AS BorrowCount " +
                "FROM dbo.V_BorrowDetails " +
                "GROUP BY BookTitle ORDER BY COUNT(*) DESC, BookTitle";

            var list = new List<BookStat>();
            using (SqlConnection conn = DbHelper.CreateConnection())
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, null, sql))
            {
                cmd.Parameters.Add("@n", SqlDbType.Int).Value = topN;
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new BookStat
                        {
                            Title = Convert.ToString(reader["Title"]),
                            BorrowCount = Convert.ToInt32(reader["BorrowCount"])
                        });
                    }
                }
            }
            return list;
        }

        public List<CategoryStat> GetCategoryStats()
        {
            const string sql =
                "SELECT rc.CategoryName, " +
                " (SELECT COUNT(*) FROM dbo.Readers r WHERE r.CategoryId = rc.CategoryId) AS ReaderCount, " +
                " (SELECT COUNT(*) FROM dbo.Borrows b " +
                "    JOIN dbo.Readers r2 ON r2.ReaderId = b.ReaderId " +
                "  WHERE r2.CategoryId = rc.CategoryId) AS BorrowCount " +
                "FROM dbo.ReaderCategory rc ORDER BY rc.CategoryId";

            var list = new List<CategoryStat>();
            using (SqlConnection conn = DbHelper.CreateConnection())
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, null, sql))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new CategoryStat
                        {
                            CategoryName = Convert.ToString(reader["CategoryName"]),
                            ReaderCount = Convert.ToInt32(reader["ReaderCount"]),
                            BorrowCount = Convert.ToInt32(reader["BorrowCount"])
                        });
                    }
                }
            }
            return list;
        }

        public List<BorrowRecord> GetOverdueList()
        {
            const string sql =
                "SELECT BorrowId, ReaderId, ReaderName, CategoryName, BookId, BookTitle, Author, " +
                "BorrowDate, DueDate, ReturnDate, Status, OverdueDays " +
                "FROM dbo.V_BorrowDetails WHERE Status = N'超期' ORDER BY DueDate";

            var list = new List<BorrowRecord>();
            using (SqlConnection conn = DbHelper.CreateConnection())
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, null, sql))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new BorrowRecord
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
                        });
                    }
                }
            }
            return list;
        }
    }
}
