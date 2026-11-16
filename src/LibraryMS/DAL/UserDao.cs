using System;
using System.Data;
using System.Data.SqlClient;
using LibraryMS.Models;

namespace LibraryMS.DAL
{
    public class UserDao
    {
        private const string SelectColumns =
            "UserId, Username, PasswordHash, PasswordSalt, UserRole, ReaderId";

        public User FindByUsername(string username)
        {
            const string sql = "SELECT " + SelectColumns + " FROM dbo.Users WHERE Username = @u";
            using (SqlConnection conn = DbHelper.CreateConnection())
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, null, sql))
            {
                cmd.Parameters.Add("@u", SqlDbType.NVarChar, 20).Value = username;
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? Map(reader) : null;
                }
            }
        }

        public User FindById(int userId)
        {
            const string sql = "SELECT " + SelectColumns + " FROM dbo.Users WHERE UserId = @id";
            using (SqlConnection conn = DbHelper.CreateConnection())
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, null, sql))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = userId;
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? Map(reader) : null;
                }
            }
        }

        public User FindByReaderId(string readerId)
        {
            const string sql = "SELECT " + SelectColumns + " FROM dbo.Users WHERE ReaderId = @rid";
            using (SqlConnection conn = DbHelper.CreateConnection())
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, null, sql))
            {
                cmd.Parameters.Add("@rid", SqlDbType.Char, 10).Value = readerId;
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? Map(reader) : null;
                }
            }
        }

        public int CountAdmins()
        {
            const string sql = "SELECT COUNT(*) FROM dbo.Users WHERE UserRole = 'Admin'";
            using (SqlConnection conn = DbHelper.CreateConnection())
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, null, sql))
            {
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public bool UsernameExists(string username)
        {
            const string sql = "SELECT COUNT(*) FROM dbo.Users WHERE Username = @u";
            using (SqlConnection conn = DbHelper.CreateConnection())
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, null, sql))
            {
                cmd.Parameters.Add("@u", SqlDbType.NVarChar, 20).Value = username;
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        /// <summary>在调用方事务内创建用户，返回 UserId。</summary>
        public int Create(SqlConnection conn, SqlTransaction tran, string username,
                          byte[] passwordHash, byte[] passwordSalt, string role, string readerId)
        {
            const string sql =
                "INSERT INTO dbo.Users (Username, PasswordHash, PasswordSalt, UserRole, ReaderId) " +
                "VALUES (@u, @h, @s, @role, @rid); " +
                "SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using (SqlCommand cmd = DbHelper.CreateCommand(conn, tran, sql))
            {
                cmd.Parameters.Add("@u", SqlDbType.NVarChar, 20).Value = username;
                cmd.Parameters.Add("@h", SqlDbType.VarBinary, 32).Value = passwordHash;
                cmd.Parameters.Add("@s", SqlDbType.VarBinary, 16).Value = passwordSalt;
                cmd.Parameters.Add("@role", SqlDbType.VarChar, 10).Value = role;
                cmd.Parameters.Add("@rid", SqlDbType.Char, 10).Value =
                    (object)readerId ?? DBNull.Value;
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void UpdatePassword(int userId, byte[] passwordHash, byte[] passwordSalt)
        {
            const string sql =
                "UPDATE dbo.Users SET PasswordHash = @h, PasswordSalt = @s WHERE UserId = @id";
            using (SqlConnection conn = DbHelper.CreateConnection())
            using (SqlCommand cmd = DbHelper.CreateCommand(conn, null, sql))
            {
                cmd.Parameters.Add("@h", SqlDbType.VarBinary, 32).Value = passwordHash;
                cmd.Parameters.Add("@s", SqlDbType.VarBinary, 16).Value = passwordSalt;
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = userId;
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private static User Map(SqlDataReader reader)
        {
            return new User
            {
                UserId = reader.GetInt32(0),
                Username = reader.GetString(1),
                PasswordHash = (byte[])reader[2],
                PasswordSalt = (byte[])reader[3],
                UserRole = reader.GetString(4),
                ReaderId = reader.IsDBNull(5) ? null : reader.GetString(5)
            };
        }
    }
}
