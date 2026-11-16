using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibraryMS.BLL;
using LibraryMS.Models;

namespace LibraryMS.UI.Views
{
    /// <summary>读者端图书查询：按关键字检索馆藏并查看可借状态。</summary>
    public class ReaderBookSearchView : ViewBase
    {
        private readonly BookService _bookService = new BookService();
        private readonly Reader _reader;

        private TextBox _txtKeyword;
        private DataGridView _grid;
        private Label _lblHint;

        public ReaderBookSearchView(Reader reader)
        {
            _reader = reader;
            BuildUi();
        }

        private void BuildUi()
        {
            var root = AppTheme.Table(1, 4);
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));

            root.Controls.Add(AppTheme.PageHeader("查询图书", "检索馆藏图书并查看在馆状态；需要借阅请联系管理员登记。"), 0, 0);
            root.Controls.Add(BuildToolbar(), 0, 1);

            _grid = AppTheme.Grid();
            GridColumns.Books(_grid);
            root.Controls.Add(AppTheme.Card(_grid), 0, 2);

            _lblHint = AppTheme.HintLabel(BuildQuotaText());
            root.Controls.Add(_lblHint, 0, 3);

            Controls.Add(root);
        }

        private Control BuildToolbar()
        {
            FlowLayoutPanel bar = AppTheme.Toolbar();

            Label caption = new Label();
            caption.Text = "关键字：";
            caption.AutoSize = true;
            caption.ForeColor = AppTheme.TextDark;
            caption.Margin = new Padding(0, 9, 0, 0);

            _txtKeyword = AppTheme.SearchBox(240);
            _txtKeyword.Margin = new Padding(0, 7, 10, 0);
            _txtKeyword.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    Search();
                }
            };

            Button search = AppTheme.PrimaryButton("查 询");
            search.Width = 88;
            search.Click += delegate { Search(); };

            Button all = AppTheme.SecondaryButton("显示全部");
            all.Width = 96;
            all.Click += delegate { ReloadAsync(); };

            bar.Controls.Add(caption);
            bar.Controls.Add(_txtKeyword);
            bar.Controls.Add(search);
            bar.Controls.Add(all);
            return bar;
        }

        private string BuildQuotaText()
        {
            if (_reader == null)
            {
                return "借书请联系管理员登记。";
            }
            return "可借 " + _reader.MaxBorrowCount + " 本 / " + _reader.MaxBorrowDays + " 天，借书请联系管理员登记。";
        }

        protected override async Task LoadDataAsync()
        {
            var books = await Task.Run(() => _bookService.GetAll());
            BindGrid(_grid, books);
            _lblHint.Text = "共 " + _grid.Rows.Count + " 本图书。" + BuildQuotaText();
        }

        private void Search()
        {
            string keyword = _txtKeyword.Text.Trim();
            if (keyword.Length == 0)
            {
                ReloadAsync();
                return;
            }
            RunSearchAsync(keyword);
        }

        private async void RunSearchAsync(string keyword)
        {
            await RunBusyAsync(async () =>
            {
                var books = await Task.Run(() => _bookService.Search(keyword));
                BindGrid(_grid, books);
                if (_grid.Rows.Count == 0)
                {
                    _lblHint.Text = "未找到符合条件的图书。";
                }
                else
                {
                    _lblHint.Text = "关键字 \"" + keyword + "\" 的查询结果：共 " + _grid.Rows.Count + " 本。";
                }
            }, "正在检索图书…");
        }
    }
}
