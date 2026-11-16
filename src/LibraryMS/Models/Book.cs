using System;

namespace LibraryMS.Models
{
    public class Book
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Publisher { get; set; }
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime RegisterDate { get; set; }

        public string AvailabilityText
        {
            get { return IsAvailable ? "在馆" : "借出"; }
        }
    }
}
