using System;

namespace LibraryMS.Models
{
    public class BorrowRecord
    {
        public int BorrowId { get; set; }
        public string ReaderId { get; set; }
        public string ReaderName { get; set; }
        public string CategoryName { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; }
        public string Author { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Status { get; set; }
        public int? OverdueDays { get; set; }

        public string ReturnDateText
        {
            get { return ReturnDate.HasValue ? ReturnDate.Value.ToString("yyyy-MM-dd") : "-"; }
        }
    }
}
