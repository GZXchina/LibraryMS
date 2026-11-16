using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibraryMS.BLL;
using LibraryMS.Models;

namespace LibraryMS.UI.Views
{
    /// <summary>读者端我的借阅：查看本人借阅历史、应还日期与超期情况。</summary>
    public class MyBorrowsView : ViewBase
    {
        private readonly BorrowService _borrowService = new BorrowService();
        private readonly Reader _reader;

        private DataGridView _grid;
        private Label _lblHint;

        public MyBorrowsView(Reader reader)
        {
            _reader = reader;
            BuildUi();
        }

        private void BuildUi()
        {
            var root = AppTheme.Table(1, 4);
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));

            root.Controls.Add(AppTheme.PageHeader("我的借阅", "查看本人全部借阅记录、应还日期与超期天数。"), 0, 0);
            root.Controls.Add(BuildToolbar(), 0, 1);

            _grid = AppTheme.Grid();
            GridColumns.Borrows(_grid);
            root.Controls.Add(AppTheme.Card(_grid), 0, 2);

            _lblHint = AppTheme.HintLabel("正在加载借阅记录…");
            root.Controls.Add(_lblHint, 0, 3);

            Controls.Add(root);
        }

        private Control BuildToolbar()
        {
            FlowLayoutPanel bar = AppTheme.Toolbar();

            Button refresh = AppTheme.PrimaryButton("刷 新");
            refresh.Width = 96;
            refresh.Click += delegate { ReloadAsync(); };

            Label hint = AppTheme.MutedLabel("数据实时读取自借阅记录，归还后状态自动更新。");
            hint.Margin = new Padding(8, 12, 0, 0);

            bar.Controls.Add(refresh);
            bar.Controls.Add(hint);
            return bar;
        }

        protected override async Task LoadDataAsync()
        {
            if (_reader == null)
            {
                BindGrid(_grid, new List<BorrowRecord>());
                _lblHint.Text = "未获取到读者信息，请重新登录后查看。";
                return;
            }

            var records = await Task.Run(() => _borrowService.GetByReader(_reader.ReaderId));
            BindGrid(_grid, records);

            int active = 0;
            int overdue = 0;
            foreach (BorrowRecord record in records)
            {
                if (record.Status == "已还")
                {
                    continue;
                }
                active++;
                if (record.Status == "超期")
                {
                    overdue++;
                }
            }

            _lblHint.Text = "读者 " + _reader.ReaderId + "（" + _reader.Name + " · " + _reader.CategoryName + "）共 " +
                            records.Count + " 条借阅记录，在借 " + active + " 条，超期 " + overdue +
                            " 条；可借 " + _reader.MaxBorrowCount + " 本 / " + _reader.MaxBorrowDays + " 天。";
        }
    }
}
