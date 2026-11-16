using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibraryMS.BLL;
using LibraryMS.Models;

namespace LibraryMS.UI.Forms
{
    /// <summary>读者自助注册：创建读者记录并同时开通登录账号。</summary>
    public class ReaderRegisterForm : DialogBase
    {
        private readonly AuthService _authService = new AuthService();
        private readonly ReaderService _readerService = new ReaderService();

        private DataGridView _grid;
        private TextBox _txtName;
        private RadioButton _rdoMale;
        private RadioButton _rdoFemale;
        private TextBox _txtPhone;
        private NumericUpDown _numCategory;
        private TextBox _txtUsername;
        private TextBox _txtPassword;
        private TextBox _txtConfirm;
        private Label _lblError;
        private Button _btnRegister;
        private Button _btnCancel;

        public ReaderRegisterForm()
        {
            Text = "读者注册";
            ClientSize = new Size(580, 700);
            BuildUi();
        }

        private void BuildUi()
        {
            var root = AppTheme.Table(1, 13);
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(28, 18, 28, 16);
            RowStyle[] heights = new RowStyle[]
            {
                new RowStyle(SizeType.Absolute, 38F),
                new RowStyle(SizeType.Absolute, 22F),
                new RowStyle(SizeType.Absolute, 20F),
                new RowStyle(SizeType.Absolute, 124F),
                new RowStyle(SizeType.Absolute, 56F),
                new RowStyle(SizeType.Absolute, 56F),
                new RowStyle(SizeType.Absolute, 56F),
                new RowStyle(SizeType.Absolute, 56F),
                new RowStyle(SizeType.Absolute, 56F),
                new RowStyle(SizeType.Absolute, 56F),
                new RowStyle(SizeType.Absolute, 56F),
                new RowStyle(SizeType.Absolute, 24F),
                new RowStyle(SizeType.Percent, 100F)
            };
            for (int i = 0; i < heights.Length; i++)
            {
                root.RowStyles.Add(heights[i]);
            }

            Label title = AppTheme.Heading("读者注册");
            title.Font = AppTheme.TitleFont;
            root.Controls.Add(title, 0, 0);

            Label hint = AppTheme.MutedLabel("带 * 为必填项；读者编号由系统在注册成功后自动分配。");
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
            root.Controls.Add(AppTheme.Field("姓名 *", _txtName), 0, 4);

            _rdoMale = new RadioButton();
            _rdoFemale = new RadioButton();
            root.Controls.Add(AppTheme.GenderField(_rdoMale, _rdoFemale, true), 0, 5);

            _txtPhone = AppTheme.InputBox();
            root.Controls.Add(AppTheme.Field("联系电话（可留空）", _txtPhone), 0, 6);

            _numCategory = AppTheme.Number(1, 999, 0);
            _numCategory.Value = 1;
            root.Controls.Add(AppTheme.Field("读者类别号 *", _numCategory), 0, 7);

            _txtUsername = AppTheme.InputBox();
            root.Controls.Add(AppTheme.Field("用户名 *（3-20 位字母 / 数字 / 下划线）", _txtUsername), 0, 8);

            _txtPassword = AppTheme.InputBox();
            _txtPassword.UseSystemPasswordChar = true;
            root.Controls.Add(AppTheme.Field("密码 *（6-32 位）", _txtPassword), 0, 9);

            _txtConfirm = AppTheme.InputBox();
            _txtConfirm.UseSystemPasswordChar = true;
            root.Controls.Add(AppTheme.Field("确认密码 *", _txtConfirm), 0, 10);

            _lblError = AppTheme.ErrorLabel();
            root.Controls.Add(_lblError, 0, 11);

            _btnRegister = AppTheme.PrimaryButton("注 册");
            _btnRegister.Width = 120;
            _btnRegister.Click += OnRegisterClick;

            _btnCancel = AppTheme.SecondaryButton("取 消");
            _btnCancel.Width = 96;
            _btnCancel.DialogResult = DialogResult.Cancel;

            root.Controls.Add(AppTheme.ButtonRow(_btnRegister, _btnCancel), 0, 12);

            Controls.Add(root);
            AcceptButton = _btnRegister;
            CancelButton = _btnCancel;
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                var categories = await Task.Run(() => _readerService.GetCategories());
                _grid.DataSource = new BindingList<ReaderCategory>(categories ?? new System.Collections.Generic.List<ReaderCategory>());
                _grid.ClearSelection();
            }
            catch (Exception ex)
            {
                Ui.ShowError(this, ex);
            }
        }

        private async void OnRegisterClick(object sender, EventArgs e)
        {
            SetError(string.Empty);

            string name = _txtName.Text.Trim();
            if (name.Length == 0)
            {
                SetError("请输入姓名。");
                _txtName.Focus();
                return;
            }

            string password = _txtPassword.Text;
            if (password.Length == 0)
            {
                SetError("请输入密码。");
                _txtPassword.Focus();
                return;
            }
            if (password != _txtConfirm.Text)
            {
                SetError("两次输入的密码不一致。");
                _txtConfirm.Focus();
                return;
            }

            string username = _txtUsername.Text.Trim();
            if (username.Length == 0)
            {
                SetError("请输入用户名。");
                _txtUsername.Focus();
                return;
            }

            string gender = _rdoFemale.Checked ? "女" : "男";
            int categoryId = (int)_numCategory.Value;
            string phone = _txtPhone.Text.Trim();

            Reader reader = null;
            await RunBusyAsync(async () =>
            {
                reader = await Task.Run(() => _authService.RegisterReader(name, gender, phone, categoryId, username, password));
            }, "正在提交注册…");

            if (reader == null)
            {
                return;
            }

            Ui.ShowSuccess(this, "注册成功！您的读者编号是：" + reader.ReaderId + Environment.NewLine + "请牢记该编号，借书时需提供。");
            DialogResult = DialogResult.OK;
            Close();
        }

        private void SetError(string message)
        {
            _lblError.Text = message;
        }
    }
}
