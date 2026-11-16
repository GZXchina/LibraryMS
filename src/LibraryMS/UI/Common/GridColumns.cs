using System.Windows.Forms;

namespace LibraryMS.UI
{
    /// <summary>各数据列表的列定义，保证同一实体在不同界面展示一致。</summary>
    internal static class GridColumns
    {
        public static void Books(DataGridView grid)
        {
            grid.Columns.Clear();
            grid.Columns.Add(AppTheme.Column("书号", "BookId", 45, null, DataGridViewContentAlignment.MiddleCenter));
            grid.Columns.Add(AppTheme.Column("书名", "Title", 220));
            grid.Columns.Add(AppTheme.Column("作者", "Author", 100));
            grid.Columns.Add(AppTheme.Column("出版社", "Publisher", 110));
            grid.Columns.Add(AppTheme.Column("单价", "Price", 70, "0.00", DataGridViewContentAlignment.MiddleRight));
            grid.Columns.Add(AppTheme.Column("状态", "AvailabilityText", 50, null, DataGridViewContentAlignment.MiddleCenter));
            grid.Columns.Add(AppTheme.Column("入库日期", "RegisterDate", 90, "yyyy-MM-dd", DataGridViewContentAlignment.MiddleCenter));
        }

        public static void Readers(DataGridView grid)
        {
            grid.Columns.Clear();
            grid.Columns.Add(AppTheme.Column("读者编号", "ReaderId", 75, null, DataGridViewContentAlignment.MiddleCenter));
            grid.Columns.Add(AppTheme.Column("姓名", "Name", 80));
            grid.Columns.Add(AppTheme.Column("性别", "Gender", 45, null, DataGridViewContentAlignment.MiddleCenter));
            grid.Columns.Add(AppTheme.Column("电话", "Phone", 100));
            grid.Columns.Add(AppTheme.Column("类别", "CategoryName", 70));
            grid.Columns.Add(AppTheme.Column("状态", "Status", 50, null, DataGridViewContentAlignment.MiddleCenter));
            grid.Columns.Add(AppTheme.Column("可借", "MaxBorrowCount", 50, null, DataGridViewContentAlignment.MiddleCenter));
            grid.Columns.Add(AppTheme.Column("天数", "MaxBorrowDays", 50, null, DataGridViewContentAlignment.MiddleCenter));
            grid.Columns.Add(AppTheme.Column("注册日期", "RegisterDate", 90, "yyyy-MM-dd", DataGridViewContentAlignment.MiddleCenter));
        }

        public static void Categories(DataGridView grid)
        {
            grid.Columns.Clear();
            grid.Columns.Add(AppTheme.Column("类别号", "CategoryId", 55, null, DataGridViewContentAlignment.MiddleCenter));
            grid.Columns.Add(AppTheme.Column("类别名称", "CategoryName", 150));
            grid.Columns.Add(AppTheme.Column("可借数量", "MaxBorrowCount", 80, null, DataGridViewContentAlignment.MiddleCenter));
            grid.Columns.Add(AppTheme.Column("可借天数", "MaxBorrowDays", 80, null, DataGridViewContentAlignment.MiddleCenter));
        }

        public static void Borrows(DataGridView grid)
        {
            grid.Columns.Clear();
            grid.Columns.Add(AppTheme.Column("借阅号", "BorrowId", 45, null, DataGridViewContentAlignment.MiddleCenter));
            grid.Columns.Add(AppTheme.Column("读者编号", "ReaderId", 72, null, DataGridViewContentAlignment.MiddleCenter));
            grid.Columns.Add(AppTheme.Column("姓名", "ReaderName", 66));
            grid.Columns.Add(AppTheme.Column("类别", "CategoryName", 58));
            grid.Columns.Add(AppTheme.Column("书号", "BookId", 40, null, DataGridViewContentAlignment.MiddleCenter));
            grid.Columns.Add(AppTheme.Column("书名", "BookTitle", 150));
            grid.Columns.Add(AppTheme.Column("借出日期", "BorrowDate", 78, "yyyy-MM-dd", DataGridViewContentAlignment.MiddleCenter));
            grid.Columns.Add(AppTheme.Column("应还日期", "DueDate", 78, "yyyy-MM-dd", DataGridViewContentAlignment.MiddleCenter));
            grid.Columns.Add(AppTheme.Column("归还日期", "ReturnDateText", 78, null, DataGridViewContentAlignment.MiddleCenter));
            grid.Columns.Add(AppTheme.Column("状态", "Status", 43, null, DataGridViewContentAlignment.MiddleCenter));

            // 列宽紧张时 4 字表头会被裁切，故表头用 2 字，完整含义放在悬停提示里。
            DataGridViewTextBoxColumn overdue =
                AppTheme.Column("超期", "OverdueDays", 45, null, DataGridViewContentAlignment.MiddleCenter);
            overdue.HeaderCell.ToolTipText = "超期天数：已过应还日期且未归还的天数";
            grid.Columns.Add(overdue);
        }

        public static void TopBooks(DataGridView grid)
        {
            grid.Columns.Clear();
            grid.Columns.Add(AppTheme.Column("排名", "Rank", 50, null, DataGridViewContentAlignment.MiddleCenter));
            grid.Columns.Add(AppTheme.Column("书名", "Title", 300));
            grid.Columns.Add(AppTheme.Column("借阅次数", "BorrowCount", 80, null, DataGridViewContentAlignment.MiddleCenter));
        }

        public static void CategoryStats(DataGridView grid)
        {
            grid.Columns.Clear();
            grid.Columns.Add(AppTheme.Column("类别", "CategoryName", 160));
            grid.Columns.Add(AppTheme.Column("读者数", "ReaderCount", 80, null, DataGridViewContentAlignment.MiddleCenter));
            grid.Columns.Add(AppTheme.Column("借阅次数", "BorrowCount", 80, null, DataGridViewContentAlignment.MiddleCenter));
        }
    }
}
