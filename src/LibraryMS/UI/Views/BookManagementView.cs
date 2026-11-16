using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibraryMS.BLL;
using LibraryMS.Models;
using LibraryMS.UI.Forms;

namespace LibraryMS.UI.Views
{
    /// <summary>图书管理：录入、关键字查询、全部图书浏览。</summary>
    public class BookManagementView : ViewBase
    {
        private readonly BookService _bookService = new BookService();

        private DataGridView _grid;
        private TextBox _txtKeyword;
        private Label _lblHint;

        public BookManagementView()
        {
            BuildUi();
        }

        private void BuildUi()
        {
            var root = AppTheme.Table(1, 4);
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));

            root.Controls.Add(AppTheme.PageHeader("图书管理", "录入新书、按关键字查询并浏览全部馆藏。"), 0, 0);
            root.Controls.Add(BuildToolbar(), 0, 1);

            _grid = AppTheme.Grid();
            GridColumns.Books(_grid);
            root.Controls.Add(AppTheme.Card(_grid), 0, 2);

            _lblHint = AppTheme.HintLabel("正在加载图书数据…");
            root.Controls.Add(_lblHint, 0, 3);

            Controls.Add(root);
        }

        private Control BuildToolbar()
        {
            FlowLayoutPanel bar = AppTheme.Toolbar();

            Button add = AppTheme.PrimaryButton("录入图书");
            add.Width = 112;
            add.Click += OnAddClick;

            Button refresh = AppTheme.SecondaryButton("刷 新");
            refresh.Width = 88;
            refresh.Click += delegate { ReloadAsync(); };

            Label caption = new Label();
            caption.Text = "关键字：";
            caption.AutoSize = true;
            caption.ForeColor = AppTheme.TextDark;
            caption.Margin = new Padding(12, 9, 0, 0);

            _txtKeyword = AppTheme.SearchBox(220);
            _txtKeyword.Margin = new Padding(0, 7, 8, 0);
            _txtKeyword.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    Search();
                }
            };

            Button search = AppTheme.SecondaryButton("查 询");
            search.Width = 88;
            search.Click += delegate { Search(); };

            Button showAll = AppTheme.SecondaryButton("显示全部");
            showAll.Width = 96;
            showAll.Click += delegate { ReloadAsync(); };

            bar.Controls.Add(add);
            bar.Controls.Add(refresh);
            bar.Controls.Add(caption);
            bar.Controls.Add(_txtKeyword);
            bar.Controls.Add(search);
            bar.Controls.Add(showAll);
            return bar;
        }

        private void Search()
        {
            string keyword = _txtKeyword.Text.Trim();
            if (keyword.Length == 0)
            {
                Ui.ShowInfo(this, "请输入查询关键字（书名 / 作者 / 出版社）。");
                _txtKeyword.Focus();
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
                _lblHint.Text = "关键字 \"" + keyword + "\" 的查询结果：共 " + _grid.Rows.Count + " 本。";
            }, "正在查询图书…");
        }

        protected override async Task LoadDataAsync()
        {
            var books = await Task.Run(() => _bookService.GetAll());
            BindGrid(_grid, books);
            _lblHint.Text = "共 " + _grid.Rows.Count + " 本图书。";
        }

        private void OnAddClick(object sender, EventArgs e)
        {
            using (var form = new BookAddForm())
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    ReloadAsync();
                }
            }
        }
    }
}
