using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using LibraryMS.Common;
using LibraryMS.DAL;
using LibraryMS.Models;

namespace LibraryMS.BLL
{
    /// <summary>
    /// 认证与账号业务：管理员初始化、登录、读者注册、开通账号、修改密码。
    /// 所有 SQL 由 DAL 参数化执行；密码使用 PBKDF2 哈希存储。
    /// </summary>
    public class AuthService
    {
        private readonly UserDao _userDao = new UserDao();
        private readonly ReaderDao _readerDao = new ReaderDao();
        private readonly CategoryDao _categoryDao = new CategoryDao();

        public bool AdminExists()
        {
            return _userDao.CountAdmins() > 0;
        }

        public void RegisterAdmin(string username, string password)
        {
            username = ValidateUsername(username);
            ValidatePassword(password);

            if (_userDao.UsernameExists(username))
            {
                throw new BizException("E3001", "用户名已存在，请更换。");
            }

            byte[] hash;
            byte[] salt;
            CryptoUtil.CreatePasswordHash(password, out hash, out salt);

            using (SqlConnection conn = DbHelper.CreateConnection())
            {
                conn.Open();
                try
                {
                    _userDao.Create(conn, null, username, hash, salt, "Admin", null);
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 2601 || ex.Number == 2627)
                    {
                        throw new BizException("E3001", "用户名已存在，请更换。", ex);
                    }
                    throw new BizException("E9000", "管理员创建失败。", ex);
                }
            }
        }

        /// <summary>统一登录入口：失败消息不区分"用户不存在/密码错误"，防止账号枚举。</summary>
        public User Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            {
                throw new BizException("E3002", "用户名或密码错误。");
            }

            User user = _userDao.FindByUsername(username.Trim());
            if (user == null || !CryptoUtil.Verify(password, user.PasswordHash, user.PasswordSalt))
            {
                throw new BizException("E3002", "用户名或密码错误。");
            }

            if (user.UserRole == "Reader")
            {
                if (string.IsNullOrEmpty(user.ReaderId))
                {
                    throw new BizException("E3002", "用户名或密码错误。");
                }
                Reader reader = _readerDao.GetById(user.ReaderId);
                if (reader == null)
                {
                    throw new BizException("E1001", "读者编号不存在。");
                }
                if (!reader.IsActive)
                {
                    throw new BizException("E1002", "该读者账号已注销，无法登录。");
                }
            }

            return user;
        }

        /// <summary>
        /// 读者自助注册：同一事务内创建读者记录 + 用户账号。
        /// 编号由 ReaderDao 在事务内用 UPDLOCK+HOLDLOCK 生成，避免并发重号。
        /// </summary>
        public Reader RegisterReader(string name, string gender, string phone, int categoryId,
                                     string username, string password)
        {
            username = ValidateUsername(username);
            ValidatePassword(password);

            name = (name ?? string.Empty).Trim();
            if (name.Length == 0 || name.Length > 20)
            {
                throw new BizException("E2001", "姓名须为 1-20 个字符。");
            }
            if (gender != "男" && gender != "女")
            {
                throw new BizException("E2002", "性别只能为男或女。");
            }
            phone = (phone ?? string.Empty).Trim();
            if (phone.Length > 20)
            {
                throw new BizException("E2005", "联系电话不能超过 20 个字符。");
            }

            ReaderCategory category = _categoryDao.GetById(categoryId);
            if (category == null)
            {
                throw new BizException("E2003", "读者类别不存在。");
            }
            if (_userDao.UsernameExists(username))
            {
                throw new BizException("E3001", "用户名已存在，请更换。");
            }

            byte[] hash;
            byte[] salt;
            CryptoUtil.CreatePasswordHash(password, out hash, out salt);

            using (SqlConnection conn = DbHelper.CreateConnection())
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        string readerId = _readerDao.GenerateReaderId(conn, tran, categoryId, DateTime.Now.Year);

                        var reader = new Reader
                        {
                            ReaderId = readerId,
                            Name = name,
                            Gender = gender,
                            Phone = phone.Length == 0 ? null : phone,
                            CategoryId = categoryId,
                            Status = "正常",
                            RegisterDate = DateTime.Today
                        };

                        _readerDao.Insert(conn, tran, reader);
                        _userDao.Create(conn, tran, username, hash, salt, "Reader", readerId);
                        tran.Commit();

                        reader.CategoryName = category.CategoryName;
                        reader.MaxBorrowCount = category.MaxBorrowCount;
                        reader.MaxBorrowDays = category.MaxBorrowDays;
                        return reader;
                    }
                    catch (SqlException ex)
                    {
                        SafeRollback(tran);
                        if (ex.Number == 2601 || ex.Number == 2627)
                        {
                            throw new BizException("E3001", "用户名或读者编号已存在，请重试。", ex);
                        }
                        throw new BizException("E9000", "注册失败，数据已回滚。", ex);
                    }
                    catch (BizException)
                    {
                        SafeRollback(tran);
                        throw;
                    }
                    catch
                    {
                        SafeRollback(tran);
                        throw;
                    }
                }
            }
        }

        /// <summary>管理员为已有读者开通登录账号。</summary>
        public void CreateAccountForReader(string readerId, string username, string password)
        {
            readerId = (readerId ?? string.Empty).Trim();
            username = ValidateUsername(username);
            ValidatePassword(password);

            Reader reader = _readerDao.GetById(readerId);
            if (reader == null)
            {
                throw new BizException("E1001", "读者编号不存在。");
            }
            if (!reader.IsActive)
            {
                throw new BizException("E1002", "该读者已注销，不能开通账号。");
            }
            if (_userDao.FindByReaderId(readerId) != null)
            {
                throw new BizException("E3004", "该读者已存在账号。");
            }
            if (_userDao.UsernameExists(username))
            {
                throw new BizException("E3001", "用户名已存在，请更换。");
            }

            byte[] hash;
            byte[] salt;
            CryptoUtil.CreatePasswordHash(password, out hash, out salt);

            using (SqlConnection conn = DbHelper.CreateConnection())
            {
                conn.Open();
                try
                {
                    _userDao.Create(conn, null, username, hash, salt, "Reader", readerId);
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 2601 || ex.Number == 2627)
                    {
                        throw new BizException("E3001", "用户名或读者账号已存在。", ex);
                    }
                    throw new BizException("E9000", "账号创建失败。", ex);
                }
            }
        }

        public void ChangePassword(int userId, string oldPassword, string newPassword)
        {
            User user = _userDao.FindById(userId);
            if (user == null)
            {
                throw new BizException("E3005", "用户不存在。");
            }
            if (!CryptoUtil.Verify(oldPassword, user.PasswordHash, user.PasswordSalt))
            {
                throw new BizException("E3006", "原密码错误。");
            }

            ValidatePassword(newPassword);
            if (newPassword == oldPassword)
            {
                throw new BizException("E3007", "新密码不能与原密码相同。");
            }

            byte[] hash;
            byte[] salt;
            CryptoUtil.CreatePasswordHash(newPassword, out hash, out salt);
            _userDao.UpdatePassword(userId, hash, salt);
        }

        public static string ValidateUsername(string username)
        {
            username = (username ?? string.Empty).Trim();
            if (!Regex.IsMatch(username, @"^[A-Za-z0-9_]{3,20}$"))
            {
                throw new BizException("E3010", "用户名须为 3-20 位字母、数字或下划线。");
            }
            return username;
        }

        public static void ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 6 || password.Length > 32)
            {
                throw new BizException("E3011", "密码长度须为 6-32 位。");
            }
        }

        private static void SafeRollback(SqlTransaction tran)
        {
            try { tran.Rollback(); } catch { }
        }
    }
}
