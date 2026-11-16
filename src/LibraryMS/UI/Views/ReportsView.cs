using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibraryMS.BLL;
using LibraryMS.Models;

namespace LibraryMS.UI.Views
{
    /// <summary>借阅统计：总体概览、热门图书、类别统计与超期清单。</summary>
    public class ReportsView : ViewBase
    {
        /// <summary>热门图书行（在 BookStat 基础上补充排名）。</summary>
        private sealed class TopBookRow
        {
            public int Rank { get; set; }
            public string Title { get; set; }
            public int BorrowCount { get; set; }
        }

        private readonly ReportService _reportService = new ReportService();

        private Label _lblBooksValue;
        private Label _lblBooksDetail;
        private Label _lblReadersValue;
        private Label _lblReadersDetail;
        private Label _lblBorrowsValue;
        private Label _lblBorrowsDetail;
        private Label _lblOverdueValue;
        private Label _lblOverdueDetail;
        private Label _lblSummary;

        private DataGridView _topGrid;
        private DataGridView _categoryGrid;
        private DataGridView _overdueGrid;

        public ReportsView()
        {
            BuildUi();
        }

        private void BuildUi()
        {
            var root = AppTheme.Table(1, 3);
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            root.Controls.Add(AppTheme.PageHeader("借阅统计", "查看馆藏与借阅的总体情况、热门图书、类别分布和超期清单。"), 0, 0);
            root.Controls.Add(BuildToolbar(), 0, 1);
            root.Controls.Add(BuildTabs(), 0, 2);

            Controls.Add(root);
        }

        private Control BuildToolbar()
        {
            FlowLayoutPanel bar = AppTheme.Toolbar();

            Button refresh = AppTheme.PrimaryButton("刷 新");
            refresh.Width = 96;
            refresh.Click += delegate { ReloadAsync(); };

            Label hint = AppTheme.MutedLabel("统计数据在刷新时重新计算，反映当前数据库状态。");
            hint.Margin = new Padding(8, 12, 0, 0);

            bar.Controls.Add(refresh);
            bar.Controls.Add(hint);
            return bar;
        }

        private Control BuildTabs()
        {
            var tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            tabs.Font = AppTheme.BaseFont;
            tabs.Padding = new Point(16, 6);
            tabs.TabPages.Add(BuildOverviewPage());
            tabs.TabPages.Add(BuildTopBooksPage());
            tabs.TabPages.Add(BuildCategoryPage());
            tabs.TabPages.Add(BuildOverduePage());
            return tabs;
        }

        private static TabPage NewPage(string title)
        {
            var page = new TabPage(title);
            page.BackColor = AppTheme.PageBack;
            page.Padding = new Padding(12);
            page.Font = AppTheme.BaseFont;
            return page;
        }

        #region 总体概览

        private TabPage BuildOverviewPage()
        {
            TabPage page = NewPage("总体概览");

            var layout = AppTheme.Table(1, 2);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 168F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var cards = AppTheme.Table(4, 1);
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            cards.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            cards.Margin = new Padding(0, 0, 0, 10);

            cards.Controls.Add(BuildStatCard("馆藏图书", AppTheme.Primary, out _lblBooksValue, out _lblBooksDetail), 0, 0);
            cards.Controls.Add(BuildStatCard("注册读者", AppTheme.Success, out _lblReadersValue, out _lblReadersDetail), 1, 0);
            cards.Controls.Add(BuildStatCard("当前在借", AppTheme.Warning, out _lblBorrowsValue, out _lblBorrowsDetail), 2, 0);
            cards.Controls.Add(BuildStatCard("超期未还", AppTheme.Danger, out _lblOverdueValue, out _lblOverdueDetail), 3, 0);

            layout.Controls.Add(cards, 0, 0);

            var note = AppTheme.Table(1, 2);
            note.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            note.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            note.Padding = new Padding(20, 14, 20, 14);

            Label title = AppTheme.Heading("统计说明");
            title.Dock = DockStyle.Fill;
            title.TextAlign = ContentAlignment.MiddleLeft;

            _lblSummary = new Label();
            _lblSummary.Dock = DockStyle.Fill;
            _lblSummary.Font = AppTheme.SmallFont;
            _lblSummary.ForeColor = AppTheme.TextMuted;
            _lblSummary.Text = "正在加载统计数据…";

            note.Controls.Add(title, 0, 0);
            note.Controls.Add(_lblSummary, 0, 1);

            var noteCard = AppTheme.Card(note);
            noteCard.Margin = new Padding(0, 0, 0, 0);
            layout.Controls.Add(noteCard, 0, 1);

            page.Controls.Add(layout);
            return page;
        }

        private static Control BuildStatCard(string caption, Color accent,
                                             out Label valueLabel, out Label detailLabel)
        {
            var inner = AppTheme.Table(1, 4);
            inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            inner.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            inner.Padding = new Padding(20, 14, 16, 10);

            Label captionLabel = new Label();
            captionLabel.Text = caption;
            captionLabel.Dock = DockStyle.Fill;
            captionLabel.Font = AppTheme.BaseFont;
            captionLabel.ForeColor = AppTheme.TextMuted;
            captionLabel.TextAlign = ContentAlignment.MiddleLeft;

            valueLabel = new Label();
            valueLabel.Text = "-";
            valueLabel.Dock = DockStyle.Fill;
            valueLabel.Font = AppTheme.StatFont;
            valueLabel.ForeColor = accent;
            valueLabel.TextAlign = ContentAlignment.MiddleLeft;

            detailLabel = new Label();
            detailLabel.Text = string.Empty;
            detailLabel.Dock = DockStyle.Fill;
            detailLabel.Font = AppTheme.SmallFont;
            detailLabel.ForeColor = AppTheme.TextMuted;
            detailLabel.TextAlign = ContentAlignment.TopLeft;

            inner.Controls.Add(captionLabel, 0, 0);
            inner.Controls.Add(valueLabel, 0, 1);
            inner.Controls.Add(detailLabel, 0, 2);

            Panel card = AppTheme.Card(inner);
            card.Margin = new Padding(0, 0, 10, 0);
            return card;
        }

        #endregion

        #region 其它页签

        private TabPage BuildTopBooksPage()
        {
            TabPage page = NewPage("热门图书 Top 10");

            _topGrid = AppTheme.Grid();
            GridColumns.TopBooks(_topGrid);
            page.Controls.Add(AppTheme.Card(_topGrid));
            return page;
        }

        private TabPage BuildCategoryPage()
        {
            TabPage page = NewPage("类别统计");

            _categoryGrid = AppTheme.Grid();
            GridColumns.CategoryStats(_categoryGrid);
            page.Controls.Add(AppTheme.Card(_categoryGrid));
            return page;
        }

        private TabPage BuildOverduePage()
        {
            TabPage page = NewPage("超期未还清单");

            _overdueGrid = AppTheme.Grid();
            GridColumns.Borrows(_overdueGrid);
            page.Controls.Add(AppTheme.Card(_overdueGrid));
            return page;
        }

        #endregion

        protected override async Task LoadDataAsync()
        {
            OverviewStats overview = null;
            List<BookStat> topBooks = null;
            List<CategoryStat> categories = null;
            List<BorrowRecord> overdue = null;

            await Task.Run(() =>
            {
                overview = _reportService.GetOverview();
                topBooks = _reportService.GetTopBooks(10);
                categories = _reportService.GetCategoryStats();
                overdue = _reportService.GetOverdueList();
            });

            if (overview != null)
            {
                _lblBooksValue.Text = overview.TotalBooks.ToString();
                _lblBooksDetail.Text = "其中可借 " + overview.AvailableBooks + " 本";
                _lblReadersValue.Text = overview.TotalReaders.ToString();
                _lblReadersDetail.Text = "正常状态 " + overview.ActiveReaders + " 位";
                _lblBorrowsValue.Text = overview.ActiveBorrows.ToString();
                _lblBorrowsDetail.Text = "累计借阅 " + overview.TotalBorrows + " 次";
                _lblOverdueValue.Text = overview.OverdueBorrows.ToString();
                _lblOverdueDetail.Text = overview.OverdueBorrows > 0 ? "请及时联系读者归还" : "暂无超期记录";
            }

            var rows = new List<TopBookRow>();
            if (topBooks != null)
            {
                for (int i = 0; i < topBooks.Count; i++)
                {
                    var row = new TopBookRow();
                    row.Rank = i + 1;
                    row.Title = topBooks[i].Title;
                    row.BorrowCount = topBooks[i].BorrowCount;
                    rows.Add(row);
                }
            }
            BindGrid(_topGrid, rows);
            BindGrid(_categoryGrid, categories ?? new List<CategoryStat>());
            BindGrid(_overdueGrid, overdue ?? new List<BorrowRecord>());

            _lblSummary.Text =
                "馆藏图书共 " + (overview == null ? 0 : overview.TotalBooks) + " 本，其中可借 " +
                (overview == null ? 0 : overview.AvailableBooks) + " 本；注册读者共 " +
                (overview == null ? 0 : overview.TotalReaders) + " 位，正常 " +
                (overview == null ? 0 : overview.ActiveReaders) + " 位。\r\n" +
                "当前在借 " + (overview == null ? 0 : overview.ActiveBorrows) + " 条，超期未还 " +
                (overview == null ? 0 : overview.OverdueBorrows) + " 条，累计借阅 " +
                (overview == null ? 0 : overview.TotalBorrows) + " 次。\r\n" +
                "统计时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "　数据来源：SQL Server · LibraryDB";
        }
    }
}
