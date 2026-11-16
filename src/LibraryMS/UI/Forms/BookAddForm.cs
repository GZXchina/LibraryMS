using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibraryMS.BLL;

namespace LibraryMS.UI.Forms
{
    /// <summary>录入新图书（管理员）。</summary>
    public class BookAddForm : DialogBase
    {
        private readonly BookService _bookService = new BookService();

        private TextBox _txtTitle;
        private TextBox _txtAuthor;
        private TextBox _txtPublisher;
        private NumericUpDown _numPrice;
        private Label _lblError;
        private Button _btnSave;
        private Button _btnCancel;

        public int NewBookId { get; private set; }

        public BookAddForm()
        {
            Text = "录入图书";
            ClientSize = new Size(460, 392);
            BuildUi();
        }

        private void BuildUi()
        {
            var root = AppTheme.Table(1, 8);
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(28, 18, 28, 16);
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Label title = AppTheme.Heading("录入图书");
            title.Font = AppTheme.TitleFont;
            root.Controls.Add(title, 0, 0);

            Label hint = AppTheme.MutedLabel("图书录入后默认状态为“在馆”。");
            hint.Dock = DockStyle.Fill;
            root.Controls.Add(hint, 0, 1);

            _txtTitle = AppTheme.InputBox();
            root.Controls.Add(AppTheme.Field("图书名称 *（1-100 个字符）", _txtTitle), 0, 2);

            _txtAuthor = AppTheme.InputBox();
            root.Controls.Add(AppTheme.Field("作者 *（1-50 个字符）", _txtAuthor), 0, 3);

            _txtPublisher = AppTheme.InputBox();
            root.Controls.Add(AppTheme.Field("出版社（可留空，最多 50 个字符）", _txtPublisher), 0, 4);

            _numPrice = AppTheme.Number(0m, 99999999.99m, 2);
            _numPrice.Value = 0m;
            root.Controls.Add(AppTheme.Field("单价（0 - 99999999.99）", _numPrice), 0, 5);

            _lblError = AppTheme.ErrorLabel();
            root.Controls.Add(_lblError, 0, 6);

            _btnSave = AppTheme.PrimaryButton("保 存");
            _btnSave.Width = 110;
            _btnSave.Click += OnSaveClick;

            _btnCancel = AppTheme.SecondaryButton("取 消");
            _btnCancel.Width = 96;
            _btnCancel.DialogResult = DialogResult.Cancel;

            root.Controls.Add(AppTheme.ButtonRow(_btnSave, _btnCancel), 0, 7);

            Controls.Add(root);
            AcceptButton = _btnSave;
            CancelButton = _btnCancel;
        }

        private async void OnSaveClick(object sender, EventArgs e)
        {
            _lblError.Text = string.Empty;

            string title = _txtTitle.Text.Trim();
            if (title.Length == 0)
            {
                _lblError.Text = "请输入图书名称。";
                _txtTitle.Focus();
                return;
            }

            string author = _txtAuthor.Text.Trim();
            if (author.Length == 0)
            {
                _lblError.Text = "请输入作者。";
                _txtAuthor.Focus();
                return;
            }

            string publisher = _txtPublisher.Text.Trim();
            decimal price = _numPrice.Value;

            int newId = -1;
            await RunBusyAsync(async () =>
            {
                newId = await Task.Run(() => _bookService.AddBook(title, author, publisher, price));
            }, "正在保存图书…");

            if (newId < 0)
            {
                return;
            }

            NewBookId = newId;
            Ui.ShowSuccess(this, "录入成功，书号为 " + newId + "。");
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
