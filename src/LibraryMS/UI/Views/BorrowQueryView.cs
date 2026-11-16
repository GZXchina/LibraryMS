using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibraryMS.BLL;
using LibraryMS.Models;

namespace LibraryMS.UI.Views
{
    /// <summary>借阅记录查询：按读者编号、关键字与状态组合筛选借阅记录。</summary>
    public class BorrowQueryView : ViewBase
    {
        private readonly BorrowService _borrowService = new BorrowService();

        private TextBox _txtReaderId;
        private TextBox _txtKeyword;
        private ComboBox _cboStatus;
        private DataGridView _grid;
        private Label _lblHint;

        public BorrowQueryView()
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

            root.Controls.Add(AppTheme.PageHeader("借阅查询", "按读者编号、关键字与状态组合筛选借阅记录。"), 0, 0);
            root.Controls.Add(BuildToolbar(), 0, 1);

            _grid = AppTheme.Grid();
            GridColumns.Borrows(_grid);
            root.Controls.Add(AppTheme.Card(_grid), 0, 2);

            _lblHint = AppTheme.HintLabel("正在加载借阅记录…");
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

            _txtReaderId = AppTheme.SearchBox(130);
            _txtReaderId.Margin = new Padding(0, 7, 10, 0);

            Label keywordCaption = new Label();
            keywordCaption.Text = "关键字：";
            keywordCaption.AutoSize = true;
            keywordCaption.ForeColor = AppTheme.TextDark;
            keywordCaption.Margin = new Padding(0, 9, 0, 0);

            _txtKeyword = AppTheme.SearchBox(180);
            _txtKeyword.Margin = new Padding(0, 7, 10, 0);
            _txtKeyword.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    Query();
                }
            };

            Label statusCaption = new Label();
            statusCaption.Text = "状态：";
            statusCaption.AutoSize = true;
            statusCaption.ForeColor = AppTheme.TextDark;
            statusCaption.Margin = new Padding(0, 9, 0, 0);

            _cboStatus = new ComboBox();
            _cboStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboStatus.FlatStyle = FlatStyle.Flat;
            _cboStatus.Font = AppTheme.BaseFont;
            _cboStatus.Width = 112;
            _cboStatus.Margin = new Padding(0, 6, 10, 0);
            _cboStatus.Items.Add("全部");
            _cboStatus.Items.Add("在借");
            _cboStatus.Items.Add("超期");
            _cboStatus.Items.Add("已还");
            _cboStatus.SelectedIndex = 0;

            Button query = AppTheme.PrimaryButton("查 询");
            query.Width = 88;
            query.Click += delegate { Query(); };

            Button reset = AppTheme.SecondaryButton("重 置");
            reset.Width = 88;
            reset.Click += OnResetClick;

            bar.Controls.Add(readerCaption);
            bar.Controls.Add(_txtReaderId);
            bar.Controls.Add(keywordCaption);
            bar.Controls.Add(_txtKeyword);
            bar.Controls.Add(statusCaption);
            bar.Controls.Add(_cboStatus);
            bar.Controls.Add(query);
            bar.Controls.Add(reset);
            return bar;
        }

        private string SelectedStatus
        {
            get
            {
                string value = _cboStatus.SelectedItem as string;
                if (string.IsNullOrEmpty(value) || value == "全部")
                {
                    return null;
                }
                return value;
            }
        }

        protected override async Task LoadDataAsync()
        {
            var records = await Task.Run(() => _borrowService.QueryRecords(null, null, null));
            BindGrid(_grid, records);
            _lblHint.Text = "共 " + _grid.Rows.Count + " 条借阅记录。";
        }

        private void Query()
        {
            string readerId = _txtReaderId.Text.Trim();
            string keyword = _txtKeyword.Text.Trim();
            RunQueryAsync(readerId, keyword, SelectedStatus);
        }

        private async void RunQueryAsync(string readerId, string keyword, string status)
        {
            await RunBusyAsync(async () =>
            {
                var records = await Task.Run(() => _borrowService.QueryRecords(
                    readerId.Length == 0 ? null : readerId,
                    keyword.Length == 0 ? null : keyword,
                    status));
                BindGrid(_grid, records);
                _lblHint.Text = "查询结果：共 " + _grid.Rows.Count + " 条借阅记录。";
            }, "正在查询借阅记录…");
        }

        private void OnResetClick(object sender, EventArgs e)
        {
            _txtReaderId.Clear();
            _txtKeyword.Clear();
            _cboStatus.SelectedIndex = 0;
            ReloadAsync();
        }
    }
}
