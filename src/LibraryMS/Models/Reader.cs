using System;

namespace LibraryMS.Models
{
    public class Reader
    {
        public string ReaderId { get; set; }
        public string Name { get; set; }
        public string Gender { get; set; }
        public string Phone { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int MaxBorrowCount { get; set; }
        public int MaxBorrowDays { get; set; }
        public string Status { get; set; }
        public DateTime RegisterDate { get; set; }

        public bool IsActive
        {
            get { return Status == "正常"; }
        }
    }
}
