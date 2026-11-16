using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibraryMS.BLL;
using LibraryMS.Models;

namespace LibraryMS.UI.Views
{
    /// <summary>还书登记：按借阅号还书，并可按读者编号查看其未归还记录。</summary>
    public class ReturnView : ViewBase
    {
        private readonly BorrowService _borrowService = new BorrowService();

        private NumericUpDown _numBorrowId;
        private TextBox _txtReaderId;
        private DataGridView _grid;
        private Label _lblHint;

        public ReturnView()
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

            root.Controls.Add(AppTheme.PageHeader("还书登记", "按借阅号办理还书，系统自动恢复图书在馆状态并结算超期天数。"), 0, 0);
            root.Controls.Add(BuildToolbar(), 0, 1);

            _grid = AppTheme.Grid();
            GridColumns.Borrows(_grid);
            _grid.CellClick += OnGridCellClick;
            root.Controls.Add(AppTheme.Card(_grid), 0, 2);

            _lblHint = AppTheme.HintLabel("正在加载未归还记录…");
            root.Controls.Add(_lblHint, 0, 3);

            Controls.Add(root);
        }

        private Control BuildToolbar()
        {
            FlowLayoutPanel bar = AppTheme.Toolbar();

            Label borrowCaption = new Label();
            borrowCaption.Text = "借阅号：";
            borrowCaption.AutoSize = true;
            borrowCaption.ForeColor = AppTheme.TextDark;
            borrowCaption.Margin = new Padding(0, 9, 0, 0);

            _numBorrowId = AppTheme.Number(1m, int.MaxValue, 0);
            _numBorrowId.Width = 100;
            _numBorrowId.Margin = new Padding(0, 7, 12, 0);

            Button doReturn = AppTheme.PrimaryButton("还 书");
            doReturn.Width = 96;
            doReturn.Click += OnReturnClick;

            Label readerCaption = new Label();
            readerCaption.Text = "读者编号：";
            readerCaption.AutoSize = true;
            readerCaption.ForeColor = AppTheme.TextDark;
            readerCaption.Margin = new Padding(12, 9, 0, 0);

            _txtReaderId = AppTheme.SearchBox(140);
            _txtReaderId.Margin = new Padding(0, 7, 8, 0);
            _txtReaderId.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    QueryByReader();
                }
            };

            Button query = AppTheme.SecondaryButton("查询在借");
            query.Width = 104;
            query.Click += delegate { QueryByReader(); };

            Button showAll = AppTheme.SecondaryButton("显示全部");
            showAll.Width = 96;
            showAll.Click += delegate { ReloadAsync(); };

            bar.Controls.Add(borrowCaption);
            bar.Controls.Add(_numBorrowId);
            bar.Controls.Add(doReturn);
            bar.Controls.Add(readerCaption);
            bar.Controls.Add(_txtReaderId);
            bar.Controls.Add(query);
            bar.Controls.Add(showAll);
            return bar;
        }

        private void OnGridCellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            BorrowRecord record = _grid.Rows[e.RowIndex].DataBoundItem as BorrowRecord;
            if (record != null)
            {
                _numBorrowId.Value = record.BorrowId;
            }
        }

        protected override async Task LoadDataAsync()
        {
            var records = await Task.Run(() => _borrowService.QueryRecords(null, null, null));
            BindGrid(_grid, FilterActive(records));
            _lblHint.Text = "当前未归还记录 " + _grid.Rows.Count + " 条。点击表格中的记录可自动填入借阅号。";
        }

        private static List<BorrowRecord> FilterActive(List<BorrowRecord> records)
        {
            if (records == null)
            {
                return new List<BorrowRecord>();
            }
            return records.Where(delegate(BorrowRecord r) { return r.Status != "已还"; }).ToList();
        }

        private void QueryByReader()
        {
            string readerId = _txtReaderId.Text.Trim();
            if (readerId.Length == 0)
            {
                Ui.ShowInfo(this, "请输入读者编号。");
                _txtReaderId.Focus();
                return;
            }

            RunReaderQueryAsync(readerId);
        }

        private async void RunReaderQueryAsync(string readerId)
        {
            await RunBusyAsync(async () =>
            {
                var records = await Task.Run(() => _borrowService.GetUnreturnedByReader(readerId));
                BindGrid(_grid, records);
                _lblHint.Text = "读者 " + readerId + " 的未归还记录：共 " + _grid.Rows.Count + " 条。";
            }, "正在查询未归还记录…");
        }

        private async void OnReturnClick(object sender, EventArgs e)
        {
            int borrowId = (int)_numBorrowId.Value;
            ReturnResult result = null;

            await RunBusyAsync(async () =>
            {
                result = await Task.Run(() => _borrowService.ReturnBook(borrowId));
            }, "正在办理还书…");

            if (result == null)
            {
                return;
            }

            if (result.AlreadyReturned)
            {
                Ui.ShowWarning(this, string.Format("借阅号 {0}（《{1}》）已归还，无需重复操作。",
                    result.BorrowId, result.BookTitle));
            }
            else if (result.OverdueDays > 0)
            {
                Ui.ShowWarning(this, string.Format("还书成功，但已超期 {0} 天（《{1}》）。",
                    result.OverdueDays, result.BookTitle));
            }
            else
            {
                Ui.ShowSuccess(this, string.Format("还书成功：《{0}》，图书已恢复在馆。", result.BookTitle));
            }

            ReloadAsync();
        }
    }
}
