using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibraryMS.BLL;
using LibraryMS.Models;

namespace LibraryMS.UI.Views
{
    /// <summary>借书登记：输入读者编号与书号完成借阅，下方列出全部图书便于查找书号。</summary>
    public class BorrowView : ViewBase
    {
        private readonly BorrowService _borrowService = new BorrowService();
        private readonly BookService _bookService = new BookService();

        private TextBox _txtReaderId;
        private NumericUpDown _numBookId;
        private DataGridView _grid;
        private Label _lblHint;

        public BorrowView()
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

            root.Controls.Add(AppTheme.PageHeader("借书登记", "登记读者借书，系统自动校验借阅资格与库存并计算应还日期。"), 0, 0);
            root.Controls.Add(BuildToolbar(), 0, 1);

            _grid = AppTheme.Grid();
            GridColumns.Books(_grid);
            _grid.CellClick += OnGridCellClick;
            root.Controls.Add(AppTheme.Card(_grid), 0, 2);

            _lblHint = AppTheme.HintLabel("正在加载图书数据…");
            root.Controls.Add(_lblHint, 0, 3);

            Controls.Add(root);
        }

        private Control BuildToolbar()
        {
            FlowLayoutPanel bar = AppTheme.Toolbar();

            Label readerCaption = new Label();
            readerCaption.Text = "读者编号：";
            readerCaption.AutoSize = true;
            readerCaption.ForeColor = AppTheme.TextDark;
            readerCaption.Margin = new Padding(0, 9, 0, 0);

            _txtReaderId = AppTheme.SearchBox(140);
            _txtReaderId.Margin = new Padding(0, 7, 12, 0);

            Label bookCaption = new Label();
            bookCaption.Text = "书号：";
            bookCaption.AutoSize = true;
            bookCaption.ForeColor = AppTheme.TextDark;
            bookCaption.Margin = new Padding(0, 9, 0, 0);

            _numBookId = AppTheme.Number(1m, int.MaxValue, 0);
            _numBookId.Width = 100;
            _numBookId.Margin = new Padding(0, 7, 12, 0);

            Button borrow = AppTheme.PrimaryButton("借 书");
            borrow.Width = 96;
            borrow.Click += OnBorrowClick;

            Button refresh = AppTheme.SecondaryButton("刷 新");
            refresh.Width = 88;
            refresh.Click += delegate { ReloadAsync(); };

            bar.Controls.Add(readerCaption);
            bar.Controls.Add(_txtReaderId);
            bar.Controls.Add(bookCaption);
            bar.Controls.Add(_numBookId);
            bar.Controls.Add(borrow);
            bar.Controls.Add(refresh);
            return bar;
        }

        private void OnGridCellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            Book book = _grid.Rows[e.RowIndex].DataBoundItem as Book;
            if (book != null)
            {
                _numBookId.Value = book.BookId;
            }
        }

        protected override async Task LoadDataAsync()
        {
            var books = await Task.Run(() => _bookService.GetAll());
            BindGrid(_grid, books);
            _lblHint.Text = "共 " + _grid.Rows.Count + " 本图书。点击表格中的图书可自动填入书号。";
        }

        private async void OnBorrowClick(object sender, EventArgs e)
        {
            string readerId = _txtReaderId.Text.Trim();
            if (readerId.Length == 0)
            {
                Ui.ShowInfo(this, "请输入读者编号。");
                _txtReaderId.Focus();
                return;
            }

            int bookId = (int)_numBookId.Value;
            BorrowResult result = null;

            await RunBusyAsync(async () =>
            {
                result = await Task.Run(() => _borrowService.BorrowBook(readerId, bookId));
            }, "正在登记借书…");

            if (result == null)
            {
                return;
            }

            Ui.ShowSuccess(this, string.Format(
                "借书成功！借阅号 {0}，读者 {1}（{2}），图书《{3}》，应还日期 {4:yyyy-MM-dd}。",
                result.BorrowId, result.ReaderName, result.ReaderId, result.BookTitle, result.DueDate));

            ReloadAsync();
        }
    }
}
