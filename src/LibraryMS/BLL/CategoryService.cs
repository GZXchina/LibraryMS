using System.Collections.Generic;
using LibraryMS.Common;
using LibraryMS.DAL;
using LibraryMS.Models;

namespace LibraryMS.BLL
{
    public class CategoryService
    {
        private readonly CategoryDao _dao = new CategoryDao();

        public List<ReaderCategory> GetAll()
        {
            return _dao.GetAll();
        }

        public ReaderCategory GetById(int categoryId)
        {
            return _dao.GetById(categoryId);
        }

        public int Add(string name, int maxBorrowCount, int maxBorrowDays)
        {
            name = ValidateName(name);
            ValidateLimits(maxBorrowCount, maxBorrowDays);

            if (_dao.NameExists(name, 0))
            {
                throw new BizException("E4002", "类别名称已存在。");
            }

            return _dao.Insert(new ReaderCategory
            {
                CategoryName = name,
                MaxBorrowCount = maxBorrowCount,
                MaxBorrowDays = maxBorrowDays
            });
        }

        public void Update(int categoryId, string name, int maxBorrowCount, int maxBorrowDays)
        {
            ReaderCategory existing = _dao.GetById(categoryId);
            if (existing == null)
            {
                throw new BizException("E4003", "读者类别不存在。");
            }

            name = ValidateName(name);
            ValidateLimits(maxBorrowCount, maxBorrowDays);

            if (_dao.NameExists(name, categoryId))
            {
                throw new BizException("E4002", "类别名称已存在。");
            }

            existing.CategoryName = name;
            existing.MaxBorrowCount = maxBorrowCount;
            existing.MaxBorrowDays = maxBorrowDays;
            _dao.Update(existing);
        }

        private static string ValidateName(string name)
        {
            name = (name ?? string.Empty).Trim();
            if (name.Length == 0 || name.Length > 20)
            {
                throw new BizException("E4004", "类别名称须为 1-20 个字符。");
            }
            return name;
        }

        private static void ValidateLimits(int maxBorrowCount, int maxBorrowDays)
        {
            if (maxBorrowCount < 1 || maxBorrowCount > 50)
            {
                throw new BizException("E4005", "可借数量须为 1-50。");
            }
            if (maxBorrowDays < 1 || maxBorrowDays > 365)
            {
                throw new BizException("E4006", "可借天数须为 1-365。");
            }
        }
    }
}
