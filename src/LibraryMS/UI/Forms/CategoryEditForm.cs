using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibraryMS.BLL;
using LibraryMS.Models;

namespace LibraryMS.UI.Forms
{
    /// <summary>新增 / 修改读者类别（管理员）。existing 为 null 表示新增。</summary>
    public class CategoryEditForm : DialogBase
    {
        private readonly CategoryService _categoryService = new CategoryService();
        private readonly ReaderCategory _existing;

        private TextBox _txtName;
        private NumericUpDown _numCount;
        private NumericUpDown _numDays;
        private Label _lblError;
        private Button _btnSave;
        private Button _btnCancel;

        /// <summary>保存成功后的类别号（新增时为新分配的编号）。</summary>
        public int SavedCategoryId { get; private set; }

        public CategoryEditForm(ReaderCategory existing)
        {
            _existing = existing;
            Text = existing == null ? "新增读者类别" : "修改读者类别";
            ClientSize = new Size(440, 336);
            BuildUi();
        }

        private bool IsEdit
        {
            get { return _existing != null; }
        }

        private void BuildUi()
        {
            var root = AppTheme.Table(1, 7);
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(28, 18, 28, 16);
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Label title = AppTheme.Heading(Text);
            title.Font = AppTheme.TitleFont;
            root.Controls.Add(title, 0, 0);

            Label hint = AppTheme.MutedLabel("可借数量决定该类别读者同时可借的图书上限，可借天数为借阅期限。");
            hint.Dock = DockStyle.Fill;
            root.Controls.Add(hint, 0, 1);

            _txtName = AppTheme.InputBox();
            root.Controls.Add(AppTheme.Field("类别名称 *（1-50 个字符）", _txtName), 0, 2);

            _numCount = AppTheme.Number(1m, 50m, 0);
            _numCount.Value = 5m;
            root.Controls.Add(AppTheme.Field("可借数量 *（1 - 50 本）", _numCount), 0, 3);

            _numDays = AppTheme.Number(1m, 365m, 0);
            _numDays.Value = 30m;
            root.Controls.Add(AppTheme.Field("可借天数 *（1 - 365 天）", _numDays), 0, 4);

            _lblError = AppTheme.ErrorLabel();
            root.Controls.Add(_lblError, 0, 5);

            _btnSave = AppTheme.PrimaryButton("保 存");
            _btnSave.Width = 110;
            _btnSave.Click += OnSaveClick;

            _btnCancel = AppTheme.SecondaryButton("取 消");
            _btnCancel.Width = 96;
            _btnCancel.DialogResult = DialogResult.Cancel;

            root.Controls.Add(AppTheme.ButtonRow(_btnSave, _btnCancel), 0, 6);

            if (IsEdit)
            {
                _txtName.Text = _existing.CategoryName;
                _numCount.Value = Clamp(_existing.MaxBorrowCount, _numCount.Minimum, _numCount.Maximum);
                _numDays.Value = Clamp(_existing.MaxBorrowDays, _numDays.Minimum, _numDays.Maximum);
            }

            Controls.Add(root);
            AcceptButton = _btnSave;
            CancelButton = _btnCancel;
        }

        private static decimal Clamp(int value, decimal min, decimal max)
        {
            decimal result = value;
            if (result < min)
            {
                result = min;
            }
            if (result > max)
            {
                result = max;
            }
            return result;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _txtName.Focus();
            _txtName.SelectAll();
        }

        private async void OnSaveClick(object sender, EventArgs e)
        {
            _lblError.Text = string.Empty;

            string name = _txtName.Text.Trim();
            if (name.Length == 0)
            {
                _lblError.Text = "请输入类别名称。";
                _txtName.Focus();
                return;
            }

            int maxCount = (int)_numCount.Value;
            int maxDays = (int)_numDays.Value;

            bool ok = false;
            int savedId = IsEdit ? _existing.CategoryId : -1;

            await RunBusyAsync(async () =>
            {
                if (IsEdit)
                {
                    int categoryId = _existing.CategoryId;
                    await Task.Run(() => _categoryService.Update(categoryId, name, maxCount, maxDays));
                }
                else
                {
                    savedId = await Task.Run(() => _categoryService.Add(name, maxCount, maxDays));
                }
                ok = true;
            }, IsEdit ? "正在保存修改…" : "正在新增类别…");

            if (!ok)
            {
                return;
            }

            SavedCategoryId = savedId;
            Ui.ShowSuccess(this, IsEdit
                ? "类别修改成功。"
                : "新增成功，类别号为 " + savedId + "。");
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
