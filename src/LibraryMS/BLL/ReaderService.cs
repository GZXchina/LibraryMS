using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using LibraryMS.Common;
using LibraryMS.DAL;
using LibraryMS.Models;

namespace LibraryMS.BLL
{
    public class ReaderService
    {
        private readonly ReaderDao _readerDao = new ReaderDao();
        private readonly CategoryDao _categoryDao = new CategoryDao();

        public List<ReaderCategory> GetCategories()
        {
            return _categoryDao.GetAll();
        }

        public Reader GetById(string readerId)
        {
            return _readerDao.GetById((readerId ?? string.Empty).Trim());
        }

        public List<Reader> GetAll()
        {
            return _readerDao.GetAll();
        }

        public List<Reader> Search(string keyword)
        {
            keyword = (keyword ?? string.Empty).Trim();
            if (keyword.Length == 0)
            {
                return _readerDao.GetAll();
            }
            return _readerDao.Search(keyword);
        }

        /// <summary>管理员录入读者：编号在同一事务内生成，保证唯一。</summary>
        public Reader AddReader(string name, string gender, string phone, int categoryId)
        {
            name = ValidateName(name);
            ValidateGender(gender);
            phone = NormalizePhone(phone);

            ReaderCategory category = _categoryDao.GetById(categoryId);
            if (category == null)
            {
                throw new BizException("E2003", "读者类别不存在。");
            }

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
                            Phone = phone,
                            CategoryId = categoryId,
                            CategoryName = category.CategoryName,
                            MaxBorrowCount = category.MaxBorrowCount,
                            MaxBorrowDays = category.MaxBorrowDays,
                            Status = "正常",
                            RegisterDate = DateTime.Today
                        };

                        _readerDao.Insert(conn, tran, reader);
                        tran.Commit();
                        return reader;
                    }
                    catch (SqlException ex)
                    {
                        SafeRollback(tran);
                        if (ex.Number == 2601 || ex.Number == 2627)
                        {
                            throw new BizException("E2006", "读者编号冲突，请重试。", ex);
                        }
                        throw new BizException("E9000", "读者录入失败，数据已回滚。", ex);
                    }
                    catch
                    {
                        SafeRollback(tran);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 注销读者：锁定读者行 → 校验在借数量 → 更新状态。
        /// 与借书事务使用同一行锁，避免"注销同时借出"的竞态。
        /// </summary>
        public void Cancel(string readerId)
        {
            readerId = (readerId ?? string.Empty).Trim();
            if (readerId.Length == 0)
            {
                throw new BizException("E1001", "读者编号不能为空。");
            }

            using (SqlConnection conn = DbHelper.CreateConnection())
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        Reader reader = _readerDao.LockWithCategory(conn, tran, readerId);
                        if (reader == null)
                        {
                            throw new BizException("E1001", "读者编号不存在。");
                        }
                        if (!reader.IsActive)
                        {
                            throw new BizException("E1002", "该读者已注销。");
                        }

                        int activeBorrows = _readerDao.CountActiveBorrows(conn, tran, readerId);
                        if (activeBorrows > 0)
                        {
                            throw new BizException("E1007",
                                string.Format("该读者尚有 {0} 本图书未归还，无法注销。", activeBorrows));
                        }

                        _readerDao.SetStatus(conn, tran, readerId, "注销");
                        tran.Commit();
                    }
                    catch
                    {
                        SafeRollback(tran);
                        throw;
                    }
                }
            }
        }

        private static string ValidateName(string name)
        {
            name = (name ?? string.Empty).Trim();
            if (name.Length == 0 || name.Length > 20)
            {
                throw new BizException("E2001", "姓名须为 1-20 个字符。");
            }
            return name;
        }

        private static void ValidateGender(string gender)
        {
            if (gender != "男" && gender != "女")
            {
                throw new BizException("E2002", "性别只能为男或女。");
            }
        }

        private static string NormalizePhone(string phone)
        {
            phone = (phone ?? string.Empty).Trim();
            if (phone.Length > 20)
            {
                throw new BizException("E2005", "联系电话不能超过 20 个字符。");
            }
            return phone.Length == 0 ? null : phone;
        }

        private static void SafeRollback(SqlTransaction tran)
        {
            try { tran.Rollback(); } catch { }
        }
    }
}
