using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LibraryMS.UI
{
    /// <summary>
    /// 业务视图基类：负责统一外观、首次加载、忙碌遮罩与异常提示。
    /// 派生类只需实现 LoadDataAsync 并声明自己的控件。
    /// </summary>
    public class ViewBase : UserControl
    {
        protected readonly BusyOverlay Busy;
        private bool _loaded;

        public ViewBase()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.PageBack;
            Font = AppTheme.BaseFont;
            Busy = new BusyOverlay();
            Controls.Add(Busy);
        }

        /// <summary>后添加的业务控件不能盖住遮罩。</summary>
        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            if (Busy != null && e.Control != Busy)
            {
                Busy.BringToFront();
            }
        }

        /// <summary>由主窗体在首次显示时调用，只加载一次。</summary>
        public void ActivateView()
        {
            if (_loaded)
            {
                return;
            }
            _loaded = true;
            ReloadAsync();
        }

        protected async void ReloadAsync()
        {
            await RunBusyAsync(LoadDataAsync, "正在加载数据…");
        }

        protected virtual Task LoadDataAsync()
        {
            return Task.FromResult(true);
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
                    // 先撤掉遮罩再弹提示，否则错误框会压在一块"加载中"的遮罩上，看不到出错页面
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

        protected static void BindGrid<T>(DataGridView grid, IList<T> data)
        {
            grid.DataSource = new BindingList<T>(data ?? new List<T>());
            grid.ClearSelection();
        }
    }
}
