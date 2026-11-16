using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibraryMS.BLL;
using LibraryMS.Common;
using LibraryMS.DAL;
using LibraryMS.Models;

namespace LibraryMS.UI.Forms
{
    /// <summary>登录窗口：数据库自检 → 首次管理员初始化 → 账号登录 / 读者自助注册。</summary>
    public class LoginForm : DialogBase
    {
        private readonly AuthService _authService;
        private readonly TextBox _txtUsername;
        private readonly TextBox _txtPassword;
        private readonly Label _lblError;
        private readonly Button _btnLogin;
        private readonly Button _btnRegister;
        private readonly Button _btnExit;

        public User AuthenticatedUser { get; private set; }

        public LoginForm(AuthService authService)
        {
            _authService = authService;

            Text = "图书馆管理系统 - 登录";
            ClientSize = new Size(900, 520);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            var root = AppTheme.Table(2, 1);
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            Panel brand = BuildBrandPanel();

            var form = AppTheme.Table(1, 9);
            form.BackColor = Color.White;
            form.Padding = new Padding(56, 36, 56, 26);
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 74F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 74F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            form.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var lblTitle = new Label();
            lblTitle.Text = "登录";
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.Font = AppTheme.TitleFont;
            lblTitle.ForeColor = AppTheme.TextDark;
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;

            var lblSub = new Label();
            lblSub.Text = "请输入账号信息以继续";
            lblSub.Dock = DockStyle.Fill;
            lblSub.Font = AppTheme.SmallFont;
            lblSub.ForeColor = AppTheme.TextMuted;
            lblSub.TextAlign = ContentAlignment.MiddleLeft;

            _txtUsername = AppTheme.InputBox();
            _txtUsername.CharacterCasing = CharacterCasing.Normal;
            _txtPassword = AppTheme.InputBox();
            _txtPassword.UseSystemPasswordChar = true;

            _lblError = new Label();
            _lblError.Dock = DockStyle.Fill;
            _lblError.Font = AppTheme.SmallFont;
            _lblError.ForeColor = AppTheme.Danger;
            _lblError.TextAlign = ContentAlignment.MiddleLeft;

            _btnLogin = AppTheme.PrimaryButton("登 录");
            _btnLogin.Dock = DockStyle.Fill;
            _btnLogin.Margin = new Padding(0, 2, 0, 8);

            _btnRegister = AppTheme.SecondaryButton("读者注册（新读者自助注册）");
            _btnRegister.Dock = DockStyle.Fill;
            _btnRegister.Margin = new Padding(0, 2, 0, 8);

            _btnExit = AppTheme.SecondaryButton("退出系统");
            _btnExit.Dock = DockStyle.Fill;
            _btnExit.Margin = new Padding(0, 2, 0, 8);

            form.Controls.Add(lblTitle, 0, 0);
            form.Controls.Add(lblSub, 0, 1);
            form.Controls.Add(Field("用户名", _txtUsername), 0, 2);
            form.Controls.Add(Field("密码", _txtPassword), 0, 3);
            form.Controls.Add(_lblError, 0, 4);
            form.Controls.Add(_btnLogin, 0, 5);
            form.Controls.Add(_btnRegister, 0, 6);
            form.Controls.Add(_btnExit, 0, 7);

            root.Controls.Add(brand, 0, 0);
            root.Controls.Add(form, 1, 0);
            Controls.Add(root);

            AcceptButton = _btnLogin;
            CancelButton = _btnExit;

            _btnLogin.Click += OnLoginClick;
            _btnRegister.Click += OnRegisterClick;
            _btnExit.Click += OnExitClick;
        }

        private static Panel BuildBrandPanel()
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = AppTheme.Sidebar;

            var lblTitle = new Label();
            lblTitle.Text = "图书馆管理系统";
            lblTitle.AutoSize = true;
            lblTitle.Font = AppTheme.BrandFont;
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(30, 150);

            var lblEn = new Label();
            lblEn.Text = "Library Management System";
            lblEn.AutoSize = true;
            lblEn.Font = AppTheme.SmallFont;
            lblEn.ForeColor = AppTheme.SidebarText;
            lblEn.Location = new Point(32, 192);

            var line = new Panel();
            line.BackColor = AppTheme.Primary;
            line.Location = new Point(32, 226);
            line.Size = new Size(56, 3);

            var lblDesc = new Label();
            lblDesc.Text = "借阅登记 · 馆藏管理 · 统计分析";
            lblDesc.AutoSize = true;
            lblDesc.Font = AppTheme.SmallFont;
            lblDesc.ForeColor = AppTheme.SidebarText;
            lblDesc.Location = new Point(32, 246);

            var lblFoot = new Label();
            lblFoot.Text = "C# 程序设计 · 课程设计";
            lblFoot.AutoSize = true;
            lblFoot.Font = AppTheme.SmallFont;
            lblFoot.ForeColor = Color.FromArgb(148, 163, 184);
            lblFoot.Location = new Point(32, 470);
            lblFoot.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;

            panel.Controls.Add(lblTitle);
            panel.Controls.Add(lblEn);
            panel.Controls.Add(line);
            panel.Controls.Add(lblDesc);
            panel.Controls.Add(lblFoot);
            return panel;
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
            box.Width = 260;

            table.Controls.Add(lbl, 0, 0);
            table.Controls.Add(box, 0, 1);
            return table;
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            SetButtonsEnabled(false);
            SetBusy(true, "正在检查数据库连接…");

            try
            {
                await Task.Run(() =>
                {
                    DbHelper.TestConnection();
                    return true;
                });
            }
            catch (Exception ex)
            {
                SetBusy(false);
                ShowDatabaseError(ex);
                return;
            }

            // 遮罩保留，连续完成首次管理员检测
            Busy.ShowBusy("正在检查系统初始化状态…");
            try
            {
                bool adminExists = await Task.Run(() => _authService.AdminExists());
                if (!adminExists)
                {
                    SetBusy(false);
                    using (var setup = new AdminSetupForm(_authService))
                    {
                        if (setup.ShowDialog(this) != DialogResult.OK)
                        {
                            DialogResult = DialogResult.Cancel;
                            Close();
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SetBusy(false);
                Ui.ShowError(this, ex);
                _txtPassword.Focus();
                return;
            }

            SetBusy(false);
            SetButtonsEnabled(true);
            _txtUsername.Focus();
        }

        private void SetBusy(bool busy, string text = null)
        {
            if (busy)
            {
                Busy.ShowBusy(text);
            }
            else
            {
                Busy.HideBusy();
            }
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }

        private void SetButtonsEnabled(bool enabled)
        {
            _btnLogin.Enabled = enabled;
            _btnRegister.Enabled = enabled;
        }

        private void ShowDatabaseError(Exception ex)
        {
            string message =
                "无法连接数据库 LibraryDB。" + Environment.NewLine + Environment.NewLine +
                "请确认：" + Environment.NewLine +
                "  1) 已执行 database\\01_create_database.sql；" + Environment.NewLine +
                "  2) App.config 中的连接字符串正确；" + Environment.NewLine +
                "  3) SQL Server 服务已启动。" + Environment.NewLine + Environment.NewLine +
                "详细信息: " + ex.Message + Environment.NewLine +
                "配置文件: " + AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            MessageBox.Show(this, message, "数据库连接失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void SetError(string text)
        {
            _lblError.Text = text;
        }

        private async void OnLoginClick(object sender, EventArgs e)
        {
            SetError(string.Empty);
            string username = _txtUsername.Text.Trim();
            string password = _txtPassword.Text;

            if (username.Length == 0)
            {
                SetError("请输入用户名。");
                _txtUsername.Focus();
                return;
            }
            if (password.Length == 0)
            {
                SetError("请输入密码。");
                _txtPassword.Focus();
                return;
            }

            SetButtonsEnabled(false);
            SetBusy(true, "正在登录…");
            try
            {
                User user = await Task.Run(() => _authService.Login(username, password));
                AuthenticatedUser = user;
                Busy.HideBusy();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (BizException ex)
            {
                SetBusy(false);
                SetButtonsEnabled(true);
                SetError(ex.Message);
                _txtPassword.SelectAll();
                _txtPassword.Focus();
            }
            catch (Exception ex)
            {
                SetBusy(false);
                SetButtonsEnabled(true);
                SetError("登录异常: " + ex.Message);
            }
        }

        private void OnRegisterClick(object sender, EventArgs e)
        {
            using (var form = new ReaderRegisterForm())
            {
                form.ShowDialog(this);
            }
        }

        private void OnExitClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
