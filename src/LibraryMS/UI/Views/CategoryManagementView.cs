using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibraryMS.BLL;
using LibraryMS.Models;
using LibraryMS.UI.Forms;

namespace LibraryMS.UI.Views
{
    /// <summary>读者类别管理：查看、新增与修改类别。</summary>
    public class CategoryManagementView : ViewBase
    {
        private readonly CategoryService _categoryService = new CategoryService();

        private DataGridView _grid;
        private Button _btnEdit;
        private Label _lblHint;

        public CategoryManagementView()
        {
            BuildUi();
        }

        private void BuildUi()
        {
            var root = AppTheme.Table(1, 4);
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));

            root.Controls.Add(AppTheme.PageHeader("读者类别", "维护读者类别及其可借数量、可借天数。"), 0, 0);
            root.Controls.Add(BuildToolbar(), 0, 1);

            _grid = AppTheme.Grid();
            GridColumns.Categories(_grid);
            _grid.SelectionChanged += delegate { UpdateButtons(); };
            _grid.CellDoubleClick += delegate(object sender, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0)
                {
                    EditSelected();
                }
            };
            root.Controls.Add(AppTheme.Card(_grid), 0, 2);

            _lblHint = AppTheme.HintLabel("正在加载类别数据…");
            root.Controls.Add(_lblHint, 0, 3);

            Controls.Add(root);
        }

        private Control BuildToolbar()
        {
            FlowLayoutPanel bar = AppTheme.Toolbar();

            Button add = AppTheme.PrimaryButton("新增类别");
            add.Width = 112;
            add.Click += delegate { OpenEditor(null); };

            _btnEdit = AppTheme.SecondaryButton("修改类别");
            _btnEdit.Width = 112;
            _btnEdit.Enabled = false;
            _btnEdit.Click += delegate { EditSelected(); };

            Button refresh = AppTheme.SecondaryButton("刷 新");
            refresh.Width = 88;
            refresh.Click += delegate { ReloadAsync(); };

            bar.Controls.Add(add);
            bar.Controls.Add(_btnEdit);
            bar.Controls.Add(refresh);
            return bar;
        }

        private ReaderCategory SelectedCategory
        {
            get
            {
                if (_grid.CurrentRow == null)
                {
                    return null;
                }
                return _grid.CurrentRow.DataBoundItem as ReaderCategory;
            }
        }

        private void UpdateButtons()
        {
            _btnEdit.Enabled = SelectedCategory != null;
        }

        private void EditSelected()
        {
            ReaderCategory category = SelectedCategory;
            if (category == null)
            {
                Ui.ShowInfo(this, "请先在列表中选择要修改的类别。");
                return;
            }
            OpenEditor(category);
        }

        private void OpenEditor(ReaderCategory existing)
        {
            using (var form = new CategoryEditForm(existing))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    ReloadAsync();
                }
            }
        }

        protected override async Task LoadDataAsync()
        {
            var categories = await Task.Run(() => _categoryService.GetAll());
            BindGrid(_grid, categories);
            UpdateButtons();
            _lblHint.Text = "共 " + _grid.Rows.Count + " 个类别。双击某一行可直接修改。";
        }
    }
}
