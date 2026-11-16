using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using LibraryMS.Common;
using LibraryMS.DAL;
using LibraryMS.Models;

namespace LibraryMS.BLL
{
    /// <summary>
    /// 借书 / 还书核心业务。
    /// 借书：锁定读者行 → 校验状态/上限/超期 → 条件抢占图书 → 插入借阅（同一事务）。
    /// 还书：锁定借阅行 → 条件更新（幂等）→ 恢复图书在馆（同一事务）。
    /// 任何异常均回滚，保证图书状态与借阅记录一致。
    /// </summary>
    public class BorrowService
    {
        private readonly BookDao _bookDao = new BookDao();
        private readonly ReaderDao _readerDao = new ReaderDao();
        private readonly BorrowDao _borrowDao = new BorrowDao();

        public BorrowResult BorrowBook(string readerId, int bookId)
        {
            readerId = (readerId ?? string.Empty).Trim();
            if (readerId.Length == 0)
            {
                throw new BizException("E1001", "读者编号不能为空。");
            }

            Book book = _bookDao.GetById(bookId);
            if (book == null)
            {
                throw new BizException("E1003", "书号不存在。");
            }

            using (SqlConnection conn = DbHelper.CreateConnection())
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. 锁定读者行（与注销流程共用行锁，保证业务串行）
                        Reader reader = _readerDao.LockWithCategory(conn, tran, readerId);
                        if (reader == null)
                        {
                            throw new BizException("E1001", "读者编号不存在。");
                        }
                        if (!reader.IsActive)
                        {
                            throw new BizException("E1002", "该读者已注销，不可借书。");
                        }

                        // 2. 可借数量上限
                        int activeCount = _readerDao.CountActiveBorrows(conn, tran, readerId);
                        if (activeCount >= reader.MaxBorrowCount)
                        {
                            throw new BizException("E1005",
                                string.Format("已达可借数量上限（{0} 本），请先归还。", reader.MaxBorrowCount));
                        }

                        // 3. 超期未还则禁止再借（已确认规则 A1）
                        int overdueCount = _readerDao.CountOverdue(conn, tran, readerId);
                        if (overdueCount > 0)
                        {
                            throw new BizException("E1006",
                                string.Format("有 {0} 本图书已超期未还，请先归还后再借。", overdueCount));
                        }

                        // 4. 条件抢占图书：仅当在馆时置为借出（并发安全）
                        int claimed = _bookDao.Claim(conn, tran, bookId);
                        if (claimed == 0)
                        {
                            throw new BizException("E1004", "该书当前不在馆（已被借出）。");
                        }

                        // 5. 插入借阅记录（应还日期由数据库计算）
                        BorrowResult result = _borrowDao.Insert(conn, tran, readerId, bookId, reader.MaxBorrowDays);
                        result.BookTitle = book.Title;
                        result.ReaderName = reader.Name;

                        tran.Commit();
                        return result;
                    }
                    catch
                    {
                        SafeRollback(tran);
                        throw;
                    }
                }
            }
        }

        public ReturnResult ReturnBook(int borrowId)
        {
            if (borrowId <= 0)
            {
                throw new BizException("E2003", "借阅号须为正整数。");
            }

            using (SqlConnection conn = DbHelper.CreateConnection())
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. 锁定借阅行
                        BorrowReturnInfo info = _borrowDao.GetForReturn(conn, tran, borrowId);
                        if (info == null)
                        {
                            throw new BizException("E2003", "借阅记录不存在。");
                        }

                        // 2. 已归还 → 幂等返回（并发双还同样安全）
                        if (info.ReturnDate.HasValue)
                        {
                            tran.Rollback();
                            return new ReturnResult
                            {
                                BorrowId = borrowId,
                                BookTitle = info.BookTitle,
                                AlreadyReturned = true
                            };
                        }

                        // 3. 条件更新：仅未归还记录可更新
                        int updated = _borrowDao.MarkReturned(conn, tran, borrowId);
                        if (updated == 0)
                        {
                            tran.Rollback();
                            return new ReturnResult
                            {
                                BorrowId = borrowId,
                                BookTitle = info.BookTitle,
                                AlreadyReturned = true
                            };
                        }

                        // 4. 恢复图书在馆状态（同一事务，失败即回滚）
                        _bookDao.SetAvailability(conn, tran, info.BookId, true);

                        tran.Commit();
                        return new ReturnResult
                        {
                            BorrowId = borrowId,
                            BookTitle = info.BookTitle,
                            AlreadyReturned = false,
                            OverdueDays = info.DaysOverdue > 0 ? info.DaysOverdue : 0
                        };
                    }
                    catch
                    {
                        SafeRollback(tran);
                        throw;
                    }
                }
            }
        }

        public List<BorrowRecord> QueryRecords(string readerId, string keyword, string status)
        {
            return _borrowDao.Query(readerId, keyword, status);
        }

        public List<BorrowRecord> GetByReader(string readerId)
        {
            return _borrowDao.GetByReader((readerId ?? string.Empty).Trim());
        }

        public List<BorrowRecord> GetUnreturnedByReader(string readerId)
        {
            return _borrowDao.GetUnreturnedByReader((readerId ?? string.Empty).Trim());
        }

        private static void SafeRollback(SqlTransaction tran)
        {
            try { tran.Rollback(); } catch { }
        }
    }
}
