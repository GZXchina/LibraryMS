using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibraryMS.BLL;
using LibraryMS.Models;

namespace LibraryMS.UI.Forms
{
    /// <summary>录入读者（管理员），不创建登录账号。</summary>
    public class ReaderAddForm : DialogBase
    {
        private readonly ReaderService _readerService = new ReaderService();

        private DataGridView _grid;
        private TextBox _txtName;
        private RadioButton _rdoMale;
        private RadioButton _rdoFemale;
        private TextBox _txtPhone;
        private NumericUpDown _numCategory;
        private Label _lblError;
        private Button _btnSave;
        private Button _btnCancel;

        /// <summary>录入成功后的读者记录。</summary>
        public Reader AddedReader { get; private set; }

        public ReaderAddForm()
        {
            Text = "录入读者";
            ClientSize = new Size(620, 660);
            BuildUi();
        }

        private void BuildUi()
        {
            var root = AppTheme.Table(1, 10);
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(28, 18, 28, 16);
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 124F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Label title = AppTheme.Heading("录入读者");
            title.Font = AppTheme.TitleFont;
            root.Controls.Add(title, 0, 0);

            Label hint = AppTheme.MutedLabel("带 * 为必填项；读者编号由系统自动分配。");
            hint.Dock = DockStyle.Fill;
            root.Controls.Add(hint, 0, 1);

            Label categoryHint = AppTheme.FieldLabel("读者类别（下表供参考，请在下方填写类别号）");
            categoryHint.Dock = DockStyle.Fill;
            // 该行仅 20px，标签外边距会把文字压掉一半，这里清零。
            categoryHint.Margin = new Padding(0);
            categoryHint.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(categoryHint, 0, 2);

            _grid = AppTheme.Grid();
            GridColumns.Categories(_grid);
            root.Controls.Add(AppTheme.Card(_grid), 0, 3);

            _txtName = AppTheme.InputBox();
            root.Controls.Add(AppTheme.Field("姓名 *（1-30 个字符）", _txtName), 0, 4);

            _rdoMale = new RadioButton();
            _rdoFemale = new RadioButton();
            root.Controls.Add(AppTheme.GenderField(_rdoMale, _rdoFemale, true), 0, 5);

            _txtPhone = AppTheme.InputBox();
            root.Controls.Add(AppTheme.Field("联系电话（可留空，最多 20 个字符）", _txtPhone), 0, 6);

            _numCategory = AppTheme.Number(1m, 99m, 0);
            _numCategory.Value = 1m;
            root.Controls.Add(AppTheme.Field("读者类别号 *", _numCategory), 0, 7);

            _lblError = AppTheme.ErrorLabel();
            root.Controls.Add(_lblError, 0, 8);

            _btnSave = AppTheme.PrimaryButton("录 入");
            _btnSave.Width = 110;
            _btnSave.Click += OnSaveClick;

            _btnCancel = AppTheme.SecondaryButton("取 消");
            _btnCancel.Width = 96;
            _btnCancel.DialogResult = DialogResult.Cancel;

            root.Controls.Add(AppTheme.ButtonRow(_btnSave, _btnCancel), 0, 9);

            Controls.Add(root);
            AcceptButton = _btnSave;
            CancelButton = _btnCancel;
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _txtName.Focus();
            try
            {
                var categories = await Task.Run(() => _readerService.GetCategories());
                _grid.DataSource = new BindingList<ReaderCategory>(categories ?? new List<ReaderCategory>());
                _grid.ClearSelection();
            }
            catch (Exception ex)
            {
                Ui.ShowError(this, ex);
            }
        }

        private async void OnSaveClick(object sender, EventArgs e)
        {
            _lblError.Text = string.Empty;

            string name = _txtName.Text.Trim();
            if (name.Length == 0)
            {
                _lblError.Text = "请输入姓名。";
                _txtName.Focus();
                return;
            }

            string gender = _rdoFemale.Checked ? "女" : "男";
            string phone = _txtPhone.Text.Trim();
            int categoryId = (int)_numCategory.Value;

            Reader reader = null;
            await RunBusyAsync(async () =>
            {
                reader = await Task.Run(() => _readerService.AddReader(name, gender, phone, categoryId));
            }, "正在录入读者…");

            if (reader == null)
            {
                return;
            }

            AddedReader = reader;
            Ui.ShowSuccess(this,
                string.Format("录入成功，读者编号为 {0}（{1}，可借 {2} 本 / {3} 天）。",
                    reader.ReaderId, reader.CategoryName, reader.MaxBorrowCount, reader.MaxBorrowDays)
                + Environment.NewLine
                + "提示：如需该读者登录，请在\"读者管理 → 为读者开通账号\"中设置用户名密码。");
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
