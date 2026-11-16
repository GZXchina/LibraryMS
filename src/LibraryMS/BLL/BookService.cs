using System.Collections.Generic;
using LibraryMS.Common;
using LibraryMS.DAL;
using LibraryMS.Models;

namespace LibraryMS.BLL
{
    public class BookService
    {
        private readonly BookDao _dao = new BookDao();

        public int AddBook(string title, string author, string publisher, decimal price)
        {
            title = (title ?? string.Empty).Trim();
            author = (author ?? string.Empty).Trim();
            publisher = (publisher ?? string.Empty).Trim();

            if (title.Length == 0 || title.Length > 100)
            {
                throw new BizException("E5001", "图书名称须为 1-100 个字符。");
            }
            if (author.Length == 0 || author.Length > 50)
            {
                throw new BizException("E5002", "作者须为 1-50 个字符。");
            }
            if (publisher.Length > 50)
            {
                throw new BizException("E5003", "出版社不能超过 50 个字符。");
            }
            if (price < 0 || price > 99999999.99m)
            {
                throw new BizException("E5004", "单价须在 0 - 99999999.99 之间。");
            }

            return _dao.Insert(new Book
            {
                Title = title,
                Author = author,
                Publisher = publisher.Length == 0 ? null : publisher,
                Price = price
            });
        }

        public Book GetById(int bookId)
        {
            return _dao.GetById(bookId);
        }

        public List<Book> GetAll()
        {
            return _dao.GetAll();
        }

        public List<Book> Search(string keyword)
        {
            keyword = (keyword ?? string.Empty).Trim();
            if (keyword.Length == 0)
            {
                return _dao.GetAll();
            }
            return _dao.Search(keyword);
        }
    }
}
