using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibraryMS.BLL;
using LibraryMS.Models;
using LibraryMS.UI.Forms;

namespace LibraryMS.UI.Views
{
    /// <summary>读者管理：录入、查询、注销读者，以及为读者开通登录账号。</summary>
    public class ReaderManagementView : ViewBase
    {
        private readonly ReaderService _readerService = new ReaderService();

        private DataGridView _grid;
        private TextBox _txtKeyword;
        private Button _btnAccount;
        private Button _btnCancelReader;
        private Label _lblHint;

        public ReaderManagementView()
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

            root.Controls.Add(AppTheme.PageHeader("读者管理", "录入读者、开通登录账号、注销读者。"), 0, 0);
            root.Controls.Add(BuildToolbar(), 0, 1);

            _grid = AppTheme.Grid();
            GridColumns.Readers(_grid);
            _grid.SelectionChanged += delegate { UpdateButtons(); };
            root.Controls.Add(AppTheme.Card(_grid), 0, 2);

            _lblHint = AppTheme.HintLabel("正在加载读者数据…");
            root.Controls.Add(_lblHint, 0, 3);

            Controls.Add(root);
        }

        private Control BuildToolbar()
        {
            FlowLayoutPanel bar = AppTheme.Toolbar();

            Button add = AppTheme.PrimaryButton("录入读者");
            add.Width = 112;
            add.Click += OnAddClick;

            _btnAccount = AppTheme.SecondaryButton("开通账号");
            _btnAccount.Width = 112;
            _btnAccount.Enabled = false;
            _btnAccount.Click += OnAccountClick;

            _btnCancelReader = AppTheme.DangerButton("注销读者");
            _btnCancelReader.Width = 112;
            _btnCancelReader.Enabled = false;
            _btnCancelReader.Click += OnCancelClick;

            Button refresh = AppTheme.SecondaryButton("刷 新");
            refresh.Width = 88;
            refresh.Click += delegate { ReloadAsync(); };

            Label caption = new Label();
            caption.Text = "关键字：";
            caption.AutoSize = true;
            caption.ForeColor = AppTheme.TextDark;
            caption.Margin = new Padding(12, 9, 0, 0);

            _txtKeyword = AppTheme.SearchBox(200);
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
            bar.Controls.Add(_btnAccount);
            bar.Controls.Add(_btnCancelReader);
            bar.Controls.Add(refresh);
            bar.Controls.Add(caption);
            bar.Controls.Add(_txtKeyword);
            bar.Controls.Add(search);
            bar.Controls.Add(showAll);
            return bar;
        }

        private Reader SelectedReader
        {
            get
            {
                if (_grid.CurrentRow == null)
                {
                    return null;
                }
                return _grid.CurrentRow.DataBoundItem as Reader;
            }
        }

        private void UpdateButtons()
        {
            bool hasSelection = SelectedReader != null;
            _btnAccount.Enabled = hasSelection;
            _btnCancelReader.Enabled = hasSelection;
        }

        private void Search()
        {
            string keyword = _txtKeyword.Text.Trim();
            if (keyword.Length == 0)
            {
                Ui.ShowInfo(this, "请输入查询关键字（编号 / 姓名 / 电话）。");
                _txtKeyword.Focus();
                return;
            }

            RunSearchAsync(keyword);
        }

        private async void RunSearchAsync(string keyword)
        {
            await RunBusyAsync(async () =>
            {
                var readers = await Task.Run(() => _readerService.Search(keyword));
                BindGrid(_grid, readers);
                UpdateButtons();
                _lblHint.Text = "关键字 \"" + keyword + "\" 的查询结果：共 " + _grid.Rows.Count + " 位读者。";
            }, "正在查询读者…");
        }

        protected override async Task LoadDataAsync()
        {
            var readers = await Task.Run(() => _readerService.GetAll());
            BindGrid(_grid, readers);
            UpdateButtons();
            _lblHint.Text = "共 " + _grid.Rows.Count + " 位读者。";
        }

        private void OnAddClick(object sender, EventArgs e)
        {
            using (var form = new ReaderAddForm())
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    ReloadAsync();
                }
            }
        }

        private void OnAccountClick(object sender, EventArgs e)
        {
            Reader reader = SelectedReader;
            if (reader == null)
            {
                Ui.ShowInfo(this, "请先在列表中选择读者。");
                return;
            }

            using (var form = new ReaderAccountForm(reader.ReaderId))
            {
                form.ShowDialog(this);
            }
        }

        private async void OnCancelClick(object sender, EventArgs e)
        {
            Reader reader = SelectedReader;
            if (reader == null)
            {
                Ui.ShowInfo(this, "请先在列表中选择要注销的读者。");
                return;
            }

            if (!Ui.Confirm(this, "确认注销读者 " + reader.ReaderId + " ？"))
            {
                return;
            }

            string readerId = reader.ReaderId;
            bool ok = false;
            await RunBusyAsync(async () =>
            {
                await Task.Run(() => _readerService.Cancel(readerId));
                ok = true;
            }, "正在注销读者…");

            if (!ok)
            {
                return;
            }

            Ui.ShowSuccess(this, "读者已注销。");
            ReloadAsync();
        }
    }
}
