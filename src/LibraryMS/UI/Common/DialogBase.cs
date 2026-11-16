using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LibraryMS.UI
{
    /// <summary>模态对话框基类：固定边框、居中父窗体、统一忙碌遮罩。</summary>
    public class DialogBase : Form
    {
        protected readonly BusyOverlay Busy;

        public DialogBase()
        {
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MaximizeBox = false;
            MinimizeBox = false;
            AutoScaleMode = AutoScaleMode.Font;
            Font = AppTheme.BaseFont;
            BackColor = Color.White;
            Text = "图书馆管理系统";
            Busy = new BusyOverlay();
            Controls.Add(Busy);
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            if (Busy != null && e.Control != Busy)
            {
                Busy.BringToFront();
            }
        }

        protected async Task RunBusyAsync(Func<Task> action, string text)
        {
            Busy.ShowBusy(text);
            Cursor = Cursors.WaitCursor;
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                {
                    // 先撤掉遮罩再弹提示，避免错误框显示在"处理中"的遮罩之上
                    Busy.HideBusy();
                    Ui.ShowError(this, ex);
                }
            }
            finally
            {
                Busy.HideBusy();
                if (!IsDisposed)
                {
                    Cursor = Cursors.Default;
                }
            }
        }
    }
}
