using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibraryMS.BLL;

namespace LibraryMS.UI.Forms
{
    /// <summary>为已有读者开通登录账号（管理员）。</summary>
    public class ReaderAccountForm : DialogBase
    {
        private readonly AuthService _authService = new AuthService();
        private readonly string _readerId;

        private TextBox _txtReaderId;
        private TextBox _txtUsername;
        private TextBox _txtPassword;
        private TextBox _txtConfirm;
        private Label _lblError;
        private Button _btnSave;
        private Button _btnCancel;

        public ReaderAccountForm(string readerId)
        {
            _readerId = readerId;
            Text = "为读者开通账号";
            ClientSize = new Size(460, 404);
            BuildUi();
        }

        private void BuildUi()
        {
            var root = AppTheme.Table(1, 8);
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(28, 18, 28, 16);
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Label title = AppTheme.Heading("为读者开通账号");
            title.Font = AppTheme.TitleFont;
            root.Controls.Add(title, 0, 0);

            Label hint = AppTheme.MutedLabel("开通后读者即可使用该用户名与密码登录系统。");
            hint.Dock = DockStyle.Fill;
            root.Controls.Add(hint, 0, 1);

            _txtReaderId = AppTheme.InputBox();
            _txtReaderId.Text = _readerId;
            _txtReaderId.ReadOnly = true;
            _txtReaderId.BackColor = AppTheme.HoverLight;
            root.Controls.Add(AppTheme.Field("读者编号", _txtReaderId), 0, 2);

            _txtUsername = AppTheme.InputBox();
            _txtUsername.MaxLength = 20;
            root.Controls.Add(AppTheme.Field("登录用户名 *（3-20 位字母 / 数字 / 下划线）", _txtUsername), 0, 3);

            _txtPassword = AppTheme.InputBox();
            _txtPassword.MaxLength = 32;
            _txtPassword.UseSystemPasswordChar = true;
            root.Controls.Add(AppTheme.Field("密码 *（6-32 位）", _txtPassword), 0, 4);

            _txtConfirm = AppTheme.InputBox();
            _txtConfirm.MaxLength = 32;
            _txtConfirm.UseSystemPasswordChar = true;
            root.Controls.Add(AppTheme.Field("确认密码 *", _txtConfirm), 0, 5);

            _lblError = AppTheme.ErrorLabel();
            root.Controls.Add(_lblError, 0, 6);

            _btnSave = AppTheme.PrimaryButton("开 通");
            _btnSave.Width = 110;
            _btnSave.Click += OnSaveClick;

            _btnCancel = AppTheme.SecondaryButton("取 消");
            _btnCancel.Width = 96;
            _btnCancel.DialogResult = DialogResult.Cancel;

            root.Controls.Add(AppTheme.ButtonRow(_btnSave, _btnCancel), 0, 7);

            Controls.Add(root);
            AcceptButton = _btnSave;
            CancelButton = _btnCancel;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _txtUsername.Focus();
        }

        private async void OnSaveClick(object sender, EventArgs e)
        {
            _lblError.Text = string.Empty;

            string username = _txtUsername.Text.Trim();
            string password = _txtPassword.Text;
            string confirm = _txtConfirm.Text;

            if (username.Length == 0)
            {
                _lblError.Text = "请输入用户名。";
                _txtUsername.Focus();
                return;
            }

            if (password.Length == 0)
            {
                _lblError.Text = "请输入密码。";
                _txtPassword.Focus();
                return;
            }

            if (password != confirm)
            {
                _lblError.Text = "两次输入的密码不一致。";
                _txtConfirm.Clear();
                _txtConfirm.Focus();
                return;
            }

            bool ok = false;
            await RunBusyAsync(async () =>
            {
                await Task.Run(() => _authService.CreateAccountForReader(_readerId, username, password));
                ok = true;
            }, "正在开通账号…");

            if (!ok)
            {
                return;
            }

            Ui.ShowSuccess(this, "账号开通成功，读者可使用该用户名登录。");
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
