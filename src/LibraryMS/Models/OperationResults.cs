using System;

namespace LibraryMS.Models
{
    public class BorrowResult
    {
        public int BorrowId { get; set; }
        public string ReaderId { get; set; }
        public string ReaderName { get; set; }
        public string BookTitle { get; set; }
        public DateTime DueDate { get; set; }
    }

    public class ReturnResult
    {
        public int BorrowId { get; set; }
        public string BookTitle { get; set; }
        public bool AlreadyReturned { get; set; }
        public int OverdueDays { get; set; }
    }

    /// <summary>还书事务中读取的借阅行（含数据库计算的超期天数）。</summary>
    public class BorrowReturnInfo
    {
        public int BorrowId { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public int DaysOverdue { get; set; }
    }

    public class OverviewStats
    {
        public int TotalBooks { get; set; }
        public int AvailableBooks { get; set; }
        public int TotalReaders { get; set; }
        public int ActiveReaders { get; set; }
        public int TotalBorrows { get; set; }
        public int ActiveBorrows { get; set; }
        public int OverdueBorrows { get; set; }
    }

    public class BookStat
    {
        public string Title { get; set; }
        public int BorrowCount { get; set; }
    }

    public class CategoryStat
    {
        public string CategoryName { get; set; }
        public int ReaderCount { get; set; }
        public int BorrowCount { get; set; }
    }
}
