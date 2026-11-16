using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibraryMS.BLL;
using LibraryMS.Common;

namespace LibraryMS.UI.Forms
{
    /// <summary>首次启动且系统中没有管理员时，强制创建管理员账号。</summary>
    public class AdminSetupForm : DialogBase
    {
        private readonly AuthService _authService;
        private readonly TextBox _txtUsername;
        private readonly TextBox _txtPassword;
        private readonly TextBox _txtConfirm;
        private readonly Label _lblError;
        private readonly Button _btnCreate;
        private readonly Button _btnCancel;

        public AdminSetupForm(AuthService authService)
        {
            _authService = authService;

            Text = "首次使用：创建管理员账号";
            ClientSize = new Size(480, 400);
            BackColor = Color.White;

            var root = AppTheme.Table(1, 7);
            root.Padding = new Padding(32, 24, 32, 20);
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var lblTitle = new Label();
            lblTitle.Text = "创建管理员账号";
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.Font = AppTheme.TitleFont;
            lblTitle.ForeColor = AppTheme.TextDark;
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;

            var lblWarn = new Label();
            lblWarn.Text = "系统尚未创建管理员，请先创建后再登录。";
            lblWarn.Dock = DockStyle.Fill;
            lblWarn.Font = AppTheme.SmallFont;
            lblWarn.ForeColor = AppTheme.Warning;
            lblWarn.TextAlign = ContentAlignment.MiddleLeft;

            _txtUsername = AppTheme.InputBox();
            _txtPassword = AppTheme.InputBox();
            _txtPassword.UseSystemPasswordChar = true;
            _txtConfirm = AppTheme.InputBox();
            _txtConfirm.UseSystemPasswordChar = true;

            _lblError = new Label();
            _lblError.Dock = DockStyle.Fill;
            _lblError.Font = AppTheme.SmallFont;
            _lblError.ForeColor = AppTheme.Danger;
            _lblError.TextAlign = ContentAlignment.MiddleLeft;

            _btnCreate = AppTheme.PrimaryButton("创建管理员");
            _btnCreate.Width = 120;
            _btnCancel = AppTheme.SecondaryButton("取消");
            _btnCancel.Width = 96;

            var buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.WrapContents = false;
            buttons.Margin = new Padding(0, 8, 0, 0);
            buttons.Controls.Add(_btnCreate);
            buttons.Controls.Add(_btnCancel);

            root.Controls.Add(lblTitle, 0, 0);
            root.Controls.Add(lblWarn, 0, 1);
            root.Controls.Add(Field("管理员用户名（3-20 位字母/数字/下划线）", _txtUsername), 0, 2);
            root.Controls.Add(Field("管理员密码（6-32 位）", _txtPassword), 0, 3);
            root.Controls.Add(Field("确认密码", _txtConfirm), 0, 4);
            root.Controls.Add(_lblError, 0, 5);
            root.Controls.Add(buttons, 0, 6);
            Controls.Add(root);

            AcceptButton = _btnCreate;
            CancelButton = _btnCancel;

            _btnCreate.Click += OnCreateClick;
            _btnCancel.Click += OnCancelClick;
        }

        private static Control Field(string label, TextBox box)
        {
            var table = AppTheme.Table(1, 2);
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));

            var lbl = AppTheme.FieldLabel(label);
            lbl.AutoSize = false;
            lbl.Dock = DockStyle.Fill;
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            lbl.Margin = new Padding(0);

            box.Dock = DockStyle.Top;

            table.Controls.Add(lbl, 0, 0);
            table.Controls.Add(box, 0, 1);
            return table;
        }

        private async void OnCreateClick(object sender, EventArgs e)
        {
            _lblError.Text = string.Empty;
            string username = _txtUsername.Text.Trim();
            string password = _txtPassword.Text;
            string confirm = _txtConfirm.Text;

            if (username.Length == 0)
            {
                _lblError.Text = "请输入管理员用户名。";
                _txtUsername.Focus();
                return;
            }
            if (password.Length == 0)
            {
                _lblError.Text = "请输入管理员密码。";
                _txtPassword.Focus();
                return;
            }
            if (password != confirm)
            {
                _lblError.Text = "两次输入的密码不一致。";
                _txtConfirm.SelectAll();
                _txtConfirm.Focus();
                return;
            }

            SetEnabled(false);
            Busy.ShowBusy("正在创建管理员账号…");
            Cursor = Cursors.WaitCursor;
            try
            {
                await Task.Run(() => _authService.RegisterAdmin(username, password));
                Busy.HideBusy();
                Cursor = Cursors.Default;
                MessageBox.Show(this, "管理员账号创建成功，请登录。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (BizException ex)
            {
                ResetBusy();
                _lblError.Text = ex.Message;
            }
            catch (Exception ex)
            {
                ResetBusy();
                _lblError.Text = "创建失败: " + ex.Message;
            }
        }

        private void ResetBusy()
        {
            Busy.HideBusy();
            Cursor = Cursors.Default;
            SetEnabled(true);
        }

        private void SetEnabled(bool enabled)
        {
            _txtUsername.Enabled = enabled;
            _txtPassword.Enabled = enabled;
            _txtConfirm.Enabled = enabled;
            _btnCreate.Enabled = enabled;
            _btnCancel.Enabled = enabled;
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
