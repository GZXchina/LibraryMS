using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LibraryMS.Models;

namespace LibraryMS.DAL
{
    public class BookDao
    {
        private const string SelectColumns =
            "BookId, Title, Author, Publisher, Price, IsAvailable, RegisterDate";

        public int Insert(Book book)
        {
            const string sql =
                "INSERT INTO dbo.Books (Title, Author, Publisher, Price) " +
                "OUTPUT INSERTED.BookId VALUES (@t, @a, @p, @price)";
            using (SqlConnection conn = DbHelper.CreateConnection())
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, null, sql))
            {
                cmd.Parameters.Add("@t", SqlDbType.NVarChar, 100).Value = book.Title;
                cmd.Parameters.Add("@a", SqlDbType.NVarChar, 50).Value = book.Author;
                cmd.Parameters.Add("@p", SqlDbType.NVarChar, 50).Value =
                    (object)book.Publisher ?? DBNull.Value;
                cmd.Parameters.Add("@price", SqlDbType.Decimal).Value = book.Price;
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public Book GetById(int bookId)
        {
            const string sql = "SELECT " + SelectColumns + " FROM dbo.Books WHERE BookId = @id";
            using (SqlConnection conn = DbHelper.CreateConnection())
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, null, sql))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = bookId;
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? Map(reader) : null;
                }
            }
        }

        public List<Book> GetAll()
        {
            const string sql = "SELECT " + SelectColumns + " FROM dbo.Books ORDER BY BookId";
            return Query(sql, null);
        }

        public List<Book> Search(string keyword)
        {
            const string sql =
                "SELECT " + SelectColumns + " FROM dbo.Books " +
                "WHERE Title LIKE @like OR Author LIKE @like OR Publisher LIKE @like " +
                "ORDER BY BookId";
            return Query(sql, "%" + EscapeLike(keyword) + "%");
        }

        private static List<Book> Query(string sql, string likeValue)
        {
            var list = new List<Book>();
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

        /// <summary>在借书事务内抢占图书：仅当在馆时置为借出，返回受影响行数。</summary>
        public int Claim(SqlConnection conn, SqlTransaction tran, int bookId)
        {
            const string sql =
                "UPDATE dbo.Books SET IsAvailable = 0 WHERE BookId = @id AND IsAvailable = 1";
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, tran, sql))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = bookId;
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>在还书事务内同步在馆状态。</summary>
        public int SetAvailability(SqlConnection conn, SqlTransaction tran, int bookId, bool available)
        {
            string sql = available
                ? "UPDATE dbo.Books SET IsAvailable = 1 WHERE BookId = @id AND IsAvailable = 0"
                : "UPDATE dbo.Books SET IsAvailable = 0 WHERE BookId = @id AND IsAvailable = 1";
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, tran, sql))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = bookId;
                return cmd.ExecuteNonQuery();
            }
        }

        public int CountAll()
        {
            return ScalarInt("SELECT COUNT(*) FROM dbo.Books");
        }

        public int CountAvailable()
        {
            return ScalarInt("SELECT COUNT(*) FROM dbo.Books WHERE IsAvailable = 1");
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

        /// <summary>转义 LIKE 通配符，防止用户输入 % _ [ 干扰匹配（仍走参数化）。</summary>
        public static string EscapeLike(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
        }

        private static Book Map(SqlDataReader reader)
        {
            return new Book
            {
                BookId = Convert.ToInt32(reader["BookId"]),
                Title = Convert.ToString(reader["Title"]),
                Author = Convert.ToString(reader["Author"]),
                Publisher = reader["Publisher"] == DBNull.Value ? null : Convert.ToString(reader["Publisher"]),
                Price = Convert.ToDecimal(reader["Price"]),
                IsAvailable = Convert.ToBoolean(reader["IsAvailable"]),
                RegisterDate = Convert.ToDateTime(reader["RegisterDate"])
            };
        }
    }
}
