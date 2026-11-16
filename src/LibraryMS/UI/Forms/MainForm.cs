using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LibraryMS.Models;
using LibraryMS.UI.Views;

namespace LibraryMS.UI.Forms
{
    /// <summary>主窗体：左侧导航 + 右侧内容区 + 底部状态栏，承载全部功能模块。</summary>
    public class MainForm : Form
    {
        private sealed class NavItem
        {
            public string Title;
            public Func<ViewBase> Factory;
            public Button Button;
        }

        private readonly User _user;
        private readonly Reader _reader;
        private readonly List<NavItem> _items = new List<NavItem>();

        private FlowLayoutPanel _nav;
        private Panel _content;
        private Label _lblStatus;
        private Label _userLabel;
        private Button _selectedButton;
        private ViewBase _current;

        public bool LogoutRequested { get; private set; }

        public MainForm(User user, Reader reader)
        {
            _user = user;
            _reader = reader;

            Text = "图书馆管理系统";
            ClientSize = new Size(1240, 780);
            MinimumSize = new Size(1024, 640);
            StartPosition = FormStartPosition.CenterScreen;
            Font = AppTheme.BaseFont;
            BackColor = AppTheme.PageBack;

            var root = AppTheme.Table(1, 3);
            root.Dock = DockStyle.Fill;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildBody(), 0, 1);
            root.Controls.Add(BuildStatusBar(), 0, 2);

            Controls.Add(root);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_items.Count > 0)
            {
                _items[0].Button.PerformClick();
            }
        }

        #region 界面构建

        private Control BuildHeader()
        {
            var header = AppTheme.Table(2, 1);
            header.BackColor = Color.White;
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 430F));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            header.Padding = new Padding(20, 0, 16, 0);
            header.Margin = new Padding(0);

            var title = new Label();
            title.Text = "图书馆管理系统";
            title.Dock = DockStyle.Fill;
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.Font = new Font(AppTheme.BaseFont.FontFamily, 15F, FontStyle.Bold);
            title.ForeColor = AppTheme.TextDark;
            header.Controls.Add(title, 0, 0);

            var right = new FlowLayoutPanel();
            right.Dock = DockStyle.Fill;
            right.FlowDirection = FlowDirection.RightToLeft;
            right.WrapContents = false;
            right.Margin = new Padding(0);
            right.Padding = new Padding(0, 15, 0, 0);

            Button logout = AppTheme.SecondaryButton("退出登录");
            logout.Width = 96;
            logout.Height = 30;
            logout.Margin = new Padding(0, 3, 0, 0);
            logout.Click += OnLogoutClick;

            _userLabel = new Label();
            _userLabel.AutoSize = true;
            _userLabel.Margin = new Padding(0, 8, 16, 0);
            _userLabel.ForeColor = AppTheme.TextMuted;
            _userLabel.Font = AppTheme.SmallFont;
            _userLabel.Text = BuildUserText();

            right.Controls.Add(logout);
            right.Controls.Add(_userLabel);
            header.Controls.Add(right, 1, 0);

            return header;
        }

        private Control BuildBody()
        {
            var body = AppTheme.Table(2, 1);
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 212F));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _nav = new FlowLayoutPanel();
            _nav.Dock = DockStyle.Fill;
            _nav.FlowDirection = FlowDirection.TopDown;
            _nav.WrapContents = false;
            _nav.AutoScroll = true;
            _nav.BackColor = AppTheme.Sidebar;
            _nav.Padding = new Padding(10, 14, 10, 14);
            _nav.Margin = new Padding(0);
            _nav.Resize += delegate { ResizeNavButtons(); };

            var brand = new Label();
            brand.Text = "功能菜单";
            brand.AutoSize = false;
            brand.Height = 26;
            brand.Width = 180;
            brand.ForeColor = Color.FromArgb(148, 163, 184);
            brand.Font = AppTheme.SmallFont;
            brand.Margin = new Padding(4, 0, 0, 6);
            brand.TextAlign = ContentAlignment.MiddleLeft;
            _nav.Controls.Add(brand);

            BuildNavItems();
            body.Controls.Add(_nav, 0, 0);

            _content = new Panel();
            _content.Dock = DockStyle.Fill;
            _content.BackColor = AppTheme.PageBack;
            _content.Padding = new Padding(16, 12, 16, 12);
            body.Controls.Add(_content, 1, 0);

            return body;
        }

        private Control BuildStatusBar()
        {
            var bar = AppTheme.Table(2, 1);
            bar.BackColor = Color.White;
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300F));
            bar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            bar.Margin = new Padding(0);

            _lblStatus = new Label();
            _lblStatus.Text = "当前模块：-";
            _lblStatus.Dock = DockStyle.Fill;
            _lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            _lblStatus.Padding = new Padding(20, 0, 0, 0);
            _lblStatus.ForeColor = AppTheme.TextMuted;
            _lblStatus.Font = AppTheme.SmallFont;

            var db = new Label();
            db.Text = "数据库：SQL Server · LibraryDB";
            db.Dock = DockStyle.Fill;
            db.TextAlign = ContentAlignment.MiddleRight;
            db.Padding = new Padding(0, 0, 20, 0);
            db.ForeColor = AppTheme.TextMuted;
            db.Font = AppTheme.SmallFont;

            bar.Controls.Add(_lblStatus, 0, 0);
            bar.Controls.Add(db, 1, 0);
            return bar;
        }

        private void BuildNavItems()
        {
            if (_user.IsAdmin)
            {
                AddNavItem("图书管理", delegate { return new BookManagementView(); });
                AddNavItem("读者类别", delegate { return new CategoryManagementView(); });
                AddNavItem("读者管理", delegate { return new ReaderManagementView(); });
                AddNavItem("借书登记", delegate { return new BorrowView(); });
                AddNavItem("还书登记", delegate { return new ReturnView(); });
                AddNavItem("借阅查询", delegate { return new BorrowQueryView(); });
                AddNavItem("借阅统计", delegate { return new ReportsView(); });
            }
            else
            {
                AddNavItem("查询图书", delegate { return new ReaderBookSearchView(_reader); });
                AddNavItem("我的借阅", delegate { return new MyBorrowsView(_reader); });
                AddNavItem("修改密码", delegate { return new ChangePasswordView(_user); });
            }
        }

        private void AddNavItem(string title, Func<ViewBase> factory)
        {
            var button = new Button();
            button.Text = title;
            button.Width = 182;
            button.Height = 40;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.Padding = new Padding(12, 0, 0, 0);
            button.Font = AppTheme.NavFont;
            button.BackColor = AppTheme.Sidebar;
            button.ForeColor = AppTheme.SidebarText;
            button.Margin = new Padding(0, 0, 0, 4);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;

            var item = new NavItem();
            item.Title = title;
            item.Factory = factory;
            item.Button = button;
            button.Tag = item;

            button.Click += OnNavClick;
            button.MouseEnter += OnNavMouseEnter;
            button.MouseLeave += OnNavMouseLeave;

            _items.Add(item);
            _nav.Controls.Add(button);
        }

        #endregion

        #region 交互

        private void OnNavClick(object sender, EventArgs e)
        {
            var item = ((Button)sender).Tag as NavItem;
            if (item == null)
            {
                return;
            }

            ViewBase view;
            try
            {
                view = item.Factory();
            }
            catch (Exception ex)
            {
                Ui.ShowError(this, ex);
                return;
            }

            ShowView(item.Title, view, item.Button);
        }

        private void ShowView(string title, ViewBase view, Button navButton)
        {
            if (_current != null)
            {
                _content.Controls.Remove(_current);
                _current.Dispose();
                _current = null;
            }

            _current = view;
            _content.Controls.Add(view);
            view.BringToFront();

            _lblStatus.Text = "当前模块：" + title;
            SelectNav(navButton);
            view.ActivateView();
        }

        private void SelectNav(Button selected)
        {
            _selectedButton = selected;
            foreach (NavItem item in _items)
            {
                if (item.Button == selected)
                {
                    item.Button.BackColor = AppTheme.Primary;
                    item.Button.ForeColor = Color.White;
                }
                else
                {
                    item.Button.BackColor = AppTheme.Sidebar;
                    item.Button.ForeColor = AppTheme.SidebarText;
                }
            }
        }

        private void OnNavMouseEnter(object sender, EventArgs e)
        {
            var button = (Button)sender;
            if (button != _selectedButton)
            {
                button.BackColor = AppTheme.SidebarHover;
                button.ForeColor = Color.White;
            }
        }

        private void OnNavMouseLeave(object sender, EventArgs e)
        {
            var button = (Button)sender;
            if (button != _selectedButton)
            {
                button.BackColor = AppTheme.Sidebar;
                button.ForeColor = AppTheme.SidebarText;
            }
        }

        private void ResizeNavButtons()
        {
            int width = _nav.ClientSize.Width - _nav.Padding.Horizontal - 4;
            if (width < 80)
            {
                width = 80;
            }
            foreach (NavItem item in _items)
            {
                item.Button.Width = width;
            }
        }

        private void OnLogoutClick(object sender, EventArgs e)
        {
            if (!Ui.Confirm(this, "确认退出登录？"))
            {
                return;
            }

            LogoutRequested = true;
            Close();
        }

        private string BuildUserText()
        {
            if (_user.IsAdmin)
            {
                return "管理员：" + _user.Username;
            }

            string name = _reader == null ? _user.Username : _reader.Name;
            string readerId = _reader == null ? _user.ReaderId : _reader.ReaderId;
            string category = _reader == null ? string.Empty : _reader.CategoryName;
            return "读者：" + name + "（" + readerId + " / " + category + "）";
        }

        #endregion
    }
}
