using System;
using System.Windows.Forms;
using LibraryMS.Common;

namespace LibraryMS.UI
{
    /// <summary>统一的提示、确认与异常展示。</summary>
    public static class Ui
    {
        /// <summary>业务异常显示 [代码] 消息，其它异常显示 "操作失败: 详情"。</summary>
        public static string Describe(Exception ex)
        {
            var biz = ex as BizException;
            if (biz != null)
            {
                return "[" + biz.Code + "] " + biz.Message;
            }
            return "操作失败: " + ex.Message;
        }

        public static void ShowError(IWin32Window owner, Exception ex)
        {
            MessageBox.Show(owner, Describe(ex), "操作失败",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void ShowInfo(IWin32Window owner, string text)
        {
            MessageBox.Show(owner, text, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void ShowSuccess(IWin32Window owner, string text)
        {
            MessageBox.Show(owner, text, "操作成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void ShowWarning(IWin32Window owner, string text)
        {
            MessageBox.Show(owner, text, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static bool Confirm(IWin32Window owner, string text)
        {
            return Confirm(owner, text, "请确认");
        }

        public static bool Confirm(IWin32Window owner, string text, string title)
        {
            return MessageBox.Show(owner, text, title, MessageBoxButtons.YesNo,
                MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }
    }
}
