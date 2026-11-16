using System.Collections.Generic;
using LibraryMS.DAL;
using LibraryMS.Models;

namespace LibraryMS.BLL
{
    public class ReportService
    {
        private readonly ReportDao _dao = new ReportDao();

        public OverviewStats GetOverview()
        {
            return _dao.GetOverview();
        }

        public List<BookStat> GetTopBooks(int topN)
        {
            return _dao.GetTopBooks(topN <= 0 ? 10 : topN);
        }

        public List<CategoryStat> GetCategoryStats()
        {
            return _dao.GetCategoryStats();
        }

        public List<BorrowRecord> GetOverdueList()
        {
            return _dao.GetOverdueList();
        }
    }
}
