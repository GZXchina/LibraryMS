using System;
using System.Drawing;
using System.Windows.Forms;

namespace LibraryMS.UI
{
    /// <summary>
    /// 加载遮罩：覆盖父容器并显示加载提示与进度条，
    /// 用于异步查询期间阻止误操作并给出明确的状态反馈。
    /// </summary>
    public class BusyOverlay : Panel
    {
        private readonly Panel _box;
        private readonly Label _label;

        public BusyOverlay()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(245, 247, 250);
            Visible = false;

            _box = new Panel();
            _box.Size = new Size(320, 96);
            _box.BackColor = Color.White;
            _box.BorderStyle = BorderStyle.FixedSingle;

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 8F));

            _label = new Label();
            _label.Dock = DockStyle.Fill;
            _label.TextAlign = ContentAlignment.MiddleCenter;
            _label.Text = "加载中…";
            _label.Font = AppTheme.BaseFont;
            _label.ForeColor = AppTheme.TextDark;

            var bar = new ProgressBar();
            bar.Dock = DockStyle.Fill;
            bar.Style = ProgressBarStyle.Marquee;
            bar.MarqueeAnimationSpeed = 30;
            bar.Margin = new Padding(10, 0, 10, 4);

            layout.Controls.Add(_label, 0, 0);
            layout.Controls.Add(bar, 0, 1);
            _box.Controls.Add(layout);
            Controls.Add(_box);
        }

        public void ShowBusy(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                _label.Text = text;
            }
            Visible = true;
            BringToFront();
            CenterBox();
        }

        public void HideBusy()
        {
            Visible = false;
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            CenterBox();
        }

        private void CenterBox()
        {
            _box.Left = Math.Max(0, (Width - _box.Width) / 2);
            _box.Top = Math.Max(0, (Height - _box.Height) / 2);
        }
    }
}
