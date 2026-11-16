namespace LibraryMS.Models
{
    public class ReaderCategory
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int MaxBorrowCount { get; set; }
        public int MaxBorrowDays { get; set; }
    }
}
