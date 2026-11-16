using System.Drawing;
using System.Windows.Forms;

namespace LibraryMS.UI
{
    /// <summary>统一配色、字体与常用控件工厂，保证各窗体视觉一致。</summary>
    public static class AppTheme
    {
        public static readonly Color Primary = Color.FromArgb(37, 99, 235);
        public static readonly Color PrimaryDark = Color.FromArgb(29, 78, 216);
        public static readonly Color Sidebar = Color.FromArgb(30, 41, 59);
        public static readonly Color SidebarHover = Color.FromArgb(51, 65, 85);
        public static readonly Color SidebarText = Color.FromArgb(203, 213, 225);
        public static readonly Color PageBack = Color.FromArgb(243, 246, 250);
        public static readonly Color Border = Color.FromArgb(218, 224, 233);
        public static readonly Color TextDark = Color.FromArgb(31, 41, 55);
        public static readonly Color TextMuted = Color.FromArgb(107, 114, 128);
        public static readonly Color Success = Color.FromArgb(22, 163, 74);
        public static readonly Color Warning = Color.FromArgb(217, 119, 6);
        public static readonly Color Danger = Color.FromArgb(220, 38, 38);
        public static readonly Color DangerDark = Color.FromArgb(185, 28, 28);
        public static readonly Color GridAltRow = Color.FromArgb(249, 251, 253);
        public static readonly Color GridHeader = Color.FromArgb(241, 245, 249);
        public static readonly Color GridSelection = Color.FromArgb(219, 234, 254);
        public static readonly Color HoverLight = Color.FromArgb(241, 245, 249);

        public static readonly Font BaseFont = new Font("Microsoft YaHei UI", 9F);
        public static readonly Font SmallFont = new Font("Microsoft YaHei UI", 8.5F);
        public static readonly Font TitleFont = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold);
        public static readonly Font SubTitleFont = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        public static readonly Font NavFont = new Font("Microsoft YaHei UI", 10F);
        public static readonly Font BrandFont = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold);
        public static readonly Font StatFont = new Font("Microsoft YaHei UI", 20F, FontStyle.Bold);

        /// <summary>统一样式的只读数据表格。</summary>
        public static DataGridView Grid()
        {
            var grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AllowUserToOrderColumns = false;
            grid.ReadOnly = true;
            grid.AutoGenerateColumns = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.RowHeadersVisible = false;
            grid.RowTemplate.Height = 30;
            grid.ColumnHeadersHeight = 36;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.EnableHeadersVisualStyles = false;
            grid.GridColor = Border;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            grid.ColumnHeadersDefaultCellStyle.BackColor = GridHeader;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextDark;
            grid.ColumnHeadersDefaultCellStyle.Font = SubTitleFont;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = GridHeader;

            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.ForeColor = TextDark;
            grid.DefaultCellStyle.Font = BaseFont;
            grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            grid.DefaultCellStyle.SelectionBackColor = GridSelection;
            grid.DefaultCellStyle.SelectionForeColor = TextDark;
            grid.AlternatingRowsDefaultCellStyle.BackColor = GridAltRow;
            return grid;
        }

        public static DataGridViewTextBoxColumn Column(string header, string property, int fillWeight)
        {
            return Column(header, property, fillWeight, null, DataGridViewContentAlignment.MiddleLeft);
        }

        public static DataGridViewTextBoxColumn Column(string header, string property, int fillWeight,
                                                       string format,
                                                       DataGridViewContentAlignment alignment)
        {
            var column = new DataGridViewTextBoxColumn();
            column.HeaderText = header;
            column.DataPropertyName = property;
            column.FillWeight = fillWeight;
            column.MinimumWidth = 45;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            column.DefaultCellStyle.Alignment = alignment;
            column.DefaultCellStyle.NullValue = "-";
            if (!string.IsNullOrEmpty(format))
            {
                column.DefaultCellStyle.Format = format;
            }
            return column;
        }

        public static TextBox InputBox()
        {
            var box = new TextBox();
            box.BorderStyle = BorderStyle.FixedSingle;
            box.Font = BaseFont;
            box.Height = 28;
            box.Margin = new Padding(0);
            return box;
        }

        public static Label FieldLabel(string text)
        {
            var label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.Font = BaseFont;
            label.ForeColor = TextDark;
            label.Margin = new Padding(0, 6, 0, 4);
            return label;
        }

        public static Label MutedLabel(string text)
        {
            var label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.Font = SmallFont;
            label.ForeColor = TextMuted;
            return label;
        }

        public static Label Heading(string text)
        {
            var label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.Font = SubTitleFont;
            label.ForeColor = TextDark;
            return label;
        }

        /// <summary>页面标题区（标题 + 副标题），放在固定高度行中。</summary>
        public static Panel PageHeader(string title, string subtitle)
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = PageBack;

            var lblTitle = new Label();
            lblTitle.Text = title;
            lblTitle.AutoSize = true;
            lblTitle.Font = TitleFont;
            lblTitle.ForeColor = TextDark;
            lblTitle.Location = new Point(0, 0);

            panel.Controls.Add(lblTitle);

            if (!string.IsNullOrEmpty(subtitle))
            {
                var lblSub = new Label();
                lblSub.Text = subtitle;
                lblSub.AutoSize = true;
                lblSub.Font = SmallFont;
                lblSub.ForeColor = TextMuted;
                lblSub.Location = new Point(2, 32);
                panel.Controls.Add(lblSub);
            }
            return panel;
        }

        public static Button PrimaryButton(string text)
        {
            return FlatButton(text, Primary, Color.White, PrimaryDark, 0, Color.Empty);
        }

        public static Button SecondaryButton(string text)
        {
            return FlatButton(text, Color.White, TextDark, HoverLight, 1, Border);
        }

        public static Button DangerButton(string text)
        {
            return FlatButton(text, Danger, Color.White, DangerDark, 0, Color.Empty);
        }

        private static Button FlatButton(string text, Color back, Color fore, Color hover,
                                         int borderSize, Color borderColor)
        {
            var button = new Button();
            button.Text = text;
            button.Size = new Size(104, 34);
            button.Font = BaseFont;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = borderSize;
            if (borderSize > 0)
            {
                button.FlatAppearance.BorderColor = borderColor;
            }
            button.BackColor = back;
            button.ForeColor = fore;
            button.Cursor = Cursors.Hand;
            button.Margin = new Padding(0, 0, 8, 0);
            button.UseVisualStyleBackColor = false;

            Color normal = back;
            button.MouseEnter += delegate { button.BackColor = hover; };
            button.MouseLeave += delegate { button.BackColor = normal; };
            return button;
        }

        /// <summary>只读提示条，用于放置操作说明或结果摘要。</summary>
        public static Label HintLabel(string text)
        {
            var label = new Label();
            label.Text = text;
            label.AutoSize = false;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Font = SmallFont;
            label.ForeColor = TextMuted;
            return label;
        }

        public static GroupBox Group(string text)
        {
            var box = new GroupBox();
            box.Text = text;
            box.Font = BaseFont;
            box.ForeColor = TextDark;
            box.Dock = DockStyle.Fill;
            box.Padding = new Padding(12, 8, 12, 10);
            return box;
        }

        /// <summary>横向工具条，放按钮与搜索框；窗口变窄时可横向滚动。</summary>
        public static FlowLayoutPanel Toolbar()
        {
            var panel = new FlowLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.FlowDirection = FlowDirection.LeftToRight;
            panel.WrapContents = false;
            panel.AutoScroll = true;
            panel.BackColor = PageBack;
            panel.Margin = new Padding(0);
            panel.Padding = new Padding(0, 6, 0, 6);
            return panel;
        }

        public static TextBox SearchBox(int width)
        {
            var box = InputBox();
            box.Width = width;
            box.Margin = new Padding(0, 3, 8, 0);
            return box;
        }

        public static NumericUpDown Number(decimal min, decimal max, int decimals)
        {
            var number = new NumericUpDown();
            number.Minimum = min;
            number.Maximum = max;
            number.DecimalPlaces = decimals;
            number.BorderStyle = BorderStyle.FixedSingle;
            number.Font = BaseFont;
            number.TextAlign = HorizontalAlignment.Right;
            number.Width = 100;
            number.Height = 26;
            number.Margin = new Padding(0, 3, 8, 0);
            return number;
        }

        /// <summary>白底卡片容器，给表格或表单一个细边框。</summary>
        public static Panel Card(Control content)
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = Color.White;
            panel.Padding = new Padding(1);
            panel.Margin = new Padding(0);
            content.Dock = DockStyle.Fill;
            panel.Controls.Add(content);
            return panel;
        }

        /// <summary>表单字段：上方标题 + 下方输入控件，随容器高度自适应。</summary>
        public static TableLayoutPanel Field(string label, Control input)
        {
            var table = Table(1, 2);
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            table.Margin = new Padding(0, 6, 0, 0);
            table.Dock = DockStyle.Fill;

            Label caption = FieldLabel(label);
            caption.AutoSize = false;
            caption.Dock = DockStyle.Fill;
            // 标题行只有 20px，必须清空标签外边距，否则文字上下被裁切。
            caption.Margin = new Padding(0);
            caption.TextAlign = ContentAlignment.MiddleLeft;

            input.Dock = DockStyle.Fill;
            table.Controls.Add(caption, 0, 0);
            table.Controls.Add(input, 0, 1);
            return table;
        }

        /// <summary>性别单选行。</summary>
        public static TableLayoutPanel GenderField(RadioButton male, RadioButton female, bool maleChecked)
        {
            male.Text = "男";
            male.AutoSize = true;
            male.Checked = maleChecked;
            male.Margin = new Padding(0, 4, 28, 0);

            female.Text = "女";
            female.AutoSize = true;
            female.Checked = !maleChecked;
            female.Margin = new Padding(0, 4, 0, 0);

            var panel = new FlowLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.FlowDirection = FlowDirection.LeftToRight;
            panel.WrapContents = false;
            panel.Margin = new Padding(0);
            panel.Controls.Add(male);
            panel.Controls.Add(female);

            return Field("性别 *", panel);
        }

        /// <summary>表单内的行内错误提示标签。</summary>
        public static Label ErrorLabel()
        {
            var label = new Label();
            label.AutoSize = false;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.ForeColor = Danger;
            label.Font = SmallFont;
            return label;
        }

        /// <summary>对话框底部的"确认 / 取消"按钮行（确认在右）。</summary>
        public static FlowLayoutPanel ButtonRow(Button primary, Button secondary)
        {
            primary.Margin = new Padding(8, 6, 0, 0);
            secondary.Margin = new Padding(8, 6, 0, 0);

            var panel = new FlowLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.FlowDirection = FlowDirection.RightToLeft;
            panel.WrapContents = false;
            panel.Margin = new Padding(0);
            panel.Controls.Add(primary);
            panel.Controls.Add(secondary);
            return panel;
        }

        public static TableLayoutPanel Table(int columns, int rows)
        {
            var table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.ColumnCount = columns;
            table.RowCount = rows;
            table.BackColor = Color.Transparent;
            table.Margin = new Padding(0);
            table.Padding = new Padding(0);
            return table;
        }
    }
}
