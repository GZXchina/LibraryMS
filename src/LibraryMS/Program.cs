using System;
using System.Windows.Forms;
using LibraryMS.BLL;
using LibraryMS.Models;
using LibraryMS.UI.Forms;

namespace LibraryMS
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainException;

            var authService = new AuthService();

            // 登录 → 主窗体 → 退出登录 的循环，等价于原控制台的会话循环。
            while (true)
            {
                User user;
                using (var login = new LoginForm(authService))
                {
                    if (login.ShowDialog() != DialogResult.OK)
                    {
                        break;
                    }
                    user = login.AuthenticatedUser;
                }

                if (user == null)
                {
                    break;
                }

                Reader reader = null;
                if (!user.IsAdmin)
                {
                    reader = new ReaderService().GetById(user.ReaderId);
                    if (reader == null)
                    {
                        MessageBox.Show("读者信息不存在，请联系管理员。", "提示",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }
                }

                using (var main = new MainForm(user, reader))
                {
                    main.ShowDialog();
                    if (!main.LogoutRequested)
                    {
                        break;
                    }
                }

                MessageBox.Show("已退出登录。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static void OnThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            ShowFatal(e.Exception);
        }

        private static void OnDomainException(object sender, UnhandledExceptionEventArgs e)
        {
            ShowFatal(e.ExceptionObject as Exception);
        }

        private static void ShowFatal(Exception ex)
        {
            string detail = ex == null ? "未知错误。" : ex.Message;
            MessageBox.Show("程序发生未处理的异常：\r\n" + detail, "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
