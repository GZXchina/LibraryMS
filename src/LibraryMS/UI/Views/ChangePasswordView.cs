using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibraryMS.BLL;
using LibraryMS.Models;

namespace LibraryMS.UI.Views
{
    /// <summary>读者端修改密码：校验原密码后更新登录密码。</summary>
    public class ChangePasswordView : ViewBase
    {
        private readonly AuthService _authService = new AuthService();
        private readonly User _user;

        private TextBox _txtOld;
        private TextBox _txtNew;
        private TextBox _txtConfirm;
        private Label _lblError;

        public ChangePasswordView(User user)
        {
            _user = user;
            BuildUi();
        }

        private void BuildUi()
        {
            var root = AppTheme.Table(1, 3);
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));

            root.Controls.Add(AppTheme.PageHeader("修改密码", "修改当前账号的登录密码，修改后立即生效。"), 0, 0);
            root.Controls.Add(BuildForm(), 0, 1);

            Label hint = AppTheme.HintLabel("提示：请牢记新密码；如忘记密码，请联系管理员在后台重置。");
            root.Controls.Add(hint, 0, 2);

            Controls.Add(root);
        }

        private Control BuildForm()
        {
            var wrapper = AppTheme.Table(3, 1);
            wrapper.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            wrapper.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 460F));
            wrapper.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            wrapper.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var form = AppTheme.Table(1, 7);
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            form.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            form.Padding = new Padding(28, 22, 28, 22);

            Label title = AppTheme.Heading("修改登录密码");
            title.Dock = DockStyle.Fill;
            title.TextAlign = ContentAlignment.MiddleLeft;
            form.Controls.Add(title, 0, 0);

            _txtOld = NewPasswordBox();
            _txtNew = NewPasswordBox();
            _txtConfirm = NewPasswordBox();

            form.Controls.Add(AppTheme.Field("原密码", _txtOld), 0, 1);
            form.Controls.Add(AppTheme.Field("新密码", _txtNew), 0, 2);
            form.Controls.Add(AppTheme.Field("确认新密码", _txtConfirm), 0, 3);

            _lblError = AppTheme.ErrorLabel();
            form.Controls.Add(_lblError, 0, 4);

            Button submit = AppTheme.PrimaryButton("确认修改");
            submit.Width = 120;
            submit.Click += OnSubmitClick;

            Button reset = AppTheme.SecondaryButton("重 置");
            reset.Width = 96;
            reset.Click += OnResetClick;

            form.Controls.Add(AppTheme.ButtonRow(submit, reset), 0, 5);

            wrapper.Controls.Add(AppTheme.Card(form), 1, 0);
            return wrapper;
        }

        private TextBox NewPasswordBox()
        {
            TextBox box = AppTheme.InputBox();
            box.UseSystemPasswordChar = true;
            box.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    OnSubmitClick(sender, EventArgs.Empty);
                }
            };
            return box;
        }

        private void Fail(string message)
        {
            _lblError.Text = message;
        }

        private void OnResetClick(object sender, EventArgs e)
        {
            _lblError.Text = string.Empty;
            _txtOld.Clear();
            _txtNew.Clear();
            _txtConfirm.Clear();
            _txtOld.Focus();
        }

        private async void OnSubmitClick(object sender, EventArgs e)
        {
            _lblError.Text = string.Empty;

            string oldPassword = _txtOld.Text;
            string newPassword = _txtNew.Text;
            string confirmPassword = _txtConfirm.Text;

            if (oldPassword.Length == 0)
            {
                Fail("请输入原密码。");
                _txtOld.Focus();
                return;
            }
            if (newPassword.Length == 0)
            {
                Fail("请输入新密码。");
                _txtNew.Focus();
                return;
            }
            if (newPassword != confirmPassword)
            {
                Fail("两次输入的新密码不一致。");
                _txtConfirm.Clear();
                _txtConfirm.Focus();
                return;
            }
            if (_user == null)
            {
                Fail("未获取到当前登录账号，请重新登录后重试。");
                return;
            }
            if (!Ui.Confirm(this, "确认修改当前账号的登录密码？"))
            {
                return;
            }

            bool ok = false;
            await RunBusyAsync(async () =>
            {
                await Task.Run(() => _authService.ChangePassword(_user.UserId, oldPassword, newPassword));
                ok = true;
            }, "正在修改密码…");

            if (!ok)
            {
                return;
            }

            Ui.ShowSuccess(this, "密码修改成功，下次登录请使用新密码。");
            OnResetClick(sender, EventArgs.Empty);
        }
    }
}
