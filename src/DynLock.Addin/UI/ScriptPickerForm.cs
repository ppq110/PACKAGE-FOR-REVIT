using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DynLock.Addin;

namespace DynLock.Addin.UI
{
    /// <summary>
    /// Script picker with a Dynamo-Player-like list of available .dynx files.
    /// </summary>
    public class ScriptPickerForm : Form
    {
        static readonly Color C_Header = Color.FromArgb(18, 90, 175);
        static readonly Color C_Sub = Color.FromArgb(160, 200, 255);
        static readonly Color C_Toolbar = Color.FromArgb(244, 246, 251);
        static readonly Color C_Border = Color.FromArgb(210, 218, 236);
        static readonly Color C_BtnBlue = Color.FromArgb(30, 120, 215);
        static readonly Color C_BtnGray = Color.FromArgb(218, 226, 242);
        static readonly Color C_GrayFg = Color.FromArgb(52, 72, 108);

        public string SelectedPath { get; private set; }

        public static string PickScript()
        {
            using (var form = new ScriptPickerForm())
                return form.ShowDialog() == DialogResult.OK ? form.SelectedPath : null;
        }

        private readonly ListBox _list = new ListBox();
        private readonly Label _selectedInfo = new Label();
        private readonly Button _btnRun = new Button();
        private readonly Button _btnBrowse = new Button();
        private readonly Button _btnRefresh = new Button();
        private readonly Label _emptyHint = new Label();

        private ScriptPickerForm()
        {
            Text = "BIMLab Player";
            Size = new Size(760, 520);
            MinimumSize = new Size(760, 520);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.White;

            var header = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = C_Header };

            var iconWrap = new Panel { Bounds = new Rectangle(16, 12, 44, 44), BackColor = Color.White };
            var iconBox = new PictureBox
            {
                Image = AddinIcons.HeaderIcon(36),
                SizeMode = PictureBoxSizeMode.Zoom,
                Bounds = new Rectangle(4, 4, 36, 36),
                BackColor = Color.White,
            };
            iconWrap.Controls.Add(iconBox);

            var lblTitle = new Label
            {
                Text = "BIMLab Player",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(70, 14),
            };
            var lblSub = new Label
            {
                Text = "Chọn file .dynx từ danh sách rồi bấm Chạy",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = C_Sub,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(72, 40),
            };
            header.Controls.AddRange(new Control[] { iconWrap, lblTitle, lblSub });

            var sep1 = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = C_Border };

            var topBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = C_Toolbar,
                Padding = new Padding(12, 7, 12, 7),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
            };
            _btnBrowse.Text = "Thêm file .dynx";
            _btnRefresh.Text = "Làm mới";
            StyleBtn(_btnBrowse, C_BtnGray, C_GrayFg, 128, 30);
            StyleBtn(_btnRefresh, C_BtnGray, C_GrayFg, 90, 30);
            _btnBrowse.Click += (_, __) => AddDynxFile();
            _btnRefresh.Click += (_, __) => LoadScripts();
            topBar.Controls.Add(_btnBrowse);
            topBar.Controls.Add(_btnRefresh);

            var content = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12) };

            _list.Dock = DockStyle.Left;
            _list.Width = 320;
            _list.BorderStyle = BorderStyle.FixedSingle;
            _list.Font = new Font("Segoe UI", 9.25f);
            _list.DisplayMember = "DisplayName";
            _list.ValueMember = "Path";
            _list.DoubleClick += (_, __) => AcceptSelection();
            _list.SelectedIndexChanged += (_, __) => UpdateSelectedPath();

            _emptyHint.Dock = DockStyle.Fill;
            _emptyHint.Text = "Chưa có file .dynx trong danh sách.\r\nBấm \"Thêm file .dynx\" để thêm vào BIMLab Scripts.";
            _emptyHint.TextAlign = ContentAlignment.MiddleCenter;
            _emptyHint.Font = new Font("Segoe UI", 10f);
            _emptyHint.ForeColor = Color.FromArgb(120, 136, 165);
            _emptyHint.BackColor = Color.White;
            _emptyHint.Visible = false;

            var right = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 0, 0, 0),
                BackColor = Color.White,
                ColumnCount = 1,
                RowCount = 4,
            };
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var selectedLabel = new Label
            {
                Text = "Công cụ đang chọn",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = C_GrayFg,
            };

            _selectedInfo.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            _selectedInfo.ForeColor = Color.FromArgb(30, 70, 125);
            _selectedInfo.BackColor = Color.FromArgb(245, 247, 252);
            _selectedInfo.BorderStyle = BorderStyle.FixedSingle;
            _selectedInfo.Dock = DockStyle.Fill;
            _selectedInfo.TextAlign = ContentAlignment.MiddleLeft;
            _selectedInfo.Padding = new Padding(8, 0, 8, 0);
            _selectedInfo.Margin = new Padding(0, 0, 0, 8);

            var hint = new Label
            {
                Text = "Danh sách này lấy từ BIMLab Scripts. Đường dẫn file được ẩn để tránh lộ vị trí lưu trữ.",
                AutoSize = false,
                Dock = DockStyle.Fill,
                ForeColor = C_GrayFg,
                Font = new Font("Segoe UI", 8.75f),
            };

            right.Controls.Add(selectedLabel, 0, 0);
            right.Controls.Add(_selectedInfo, 0, 1);
            right.Controls.Add(hint, 0, 2);

            content.Controls.Add(right);
            content.Controls.Add(_list);
            content.Controls.Add(_emptyHint);
            _emptyHint.BringToFront();

            var bottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 54,
                Padding = new Padding(12, 8, 12, 8),
                BackColor = C_Toolbar,
            };
            var sep2 = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = C_Border };

            var btnCancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel };
            StyleBtn(btnCancel, C_BtnGray, C_GrayFg, 88, 38);
            StyleBtn(_btnRun, C_BtnBlue, Color.White, 88, 38);
            _btnRun.Text = "Chạy";
            _btnRun.Enabled = false;
            _btnRun.Click += (_, __) => AcceptSelection();
            bottom.Controls.AddRange(new Control[] { btnCancel, _btnRun });

            Controls.Add(content);
            Controls.Add(topBar);
            Controls.Add(sep1);
            Controls.Add(header);
            Controls.Add(bottom);
            Controls.Add(sep2);

            AcceptButton = _btnRun;
            CancelButton = btnCancel;

            Load += (_, __) => LoadScripts();
        }

        private static void StyleBtn(Button b, Color bg, Color fg, int w, int h)
        {
            b.Width = w;
            b.Height = h;
            b.BackColor = bg;
            b.ForeColor = fg;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.BorderColor = bg;
            b.Font = new Font("Segoe UI", 9.5f);
            b.Cursor = Cursors.Hand;
        }

        private void LoadScripts()
        {
            var scripts = ScriptLocator.FindScripts()
                .Select(path => new ScriptRow(path))
                .ToList();

            _list.BeginUpdate();
            _list.DataSource = null;
            _list.Items.Clear();
            foreach (var item in scripts)
                _list.Items.Add(item);
            _list.EndUpdate();

            _emptyHint.Visible = _list.Items.Count == 0;

            if (_list.Items.Count > 0)
                _list.SelectedIndex = 0;

            UpdateSelectedPath();
        }

        private void UpdateSelectedPath()
        {
            if (_list.SelectedItem is ScriptRow row)
            {
                SelectedPath = row.Path;
                _selectedInfo.Text = row.DisplayName;
                _btnRun.Enabled = true;
                return;
            }

            _selectedInfo.Text = "Chưa chọn công cụ";
            _btnRun.Enabled = false;
            SelectedPath = null;
        }

        private void AddDynxFile()
        {
            using (var dlg = new OpenFileDialog
            {
                Filter = "BIMLab Công cụ BIM (*.dynx)|*.dynx",
                Title = "Thêm file .dynx vào BIMLab Scripts",
                Multiselect = true,
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;

                string targetDir = ScriptLocator.DefaultScriptsDir;
                Directory.CreateDirectory(targetDir);

                string lastAdded = null;
                foreach (string source in dlg.FileNames)
                {
                    string target = UniqueTargetPath(targetDir, Path.GetFileName(source));
                    File.Copy(source, target, false);
                    lastAdded = target;
                }

                LoadScripts();
                SelectPath(lastAdded);
            }
        }

        private static string UniqueTargetPath(string targetDir, string fileName)
        {
            string target = Path.Combine(targetDir, fileName);
            if (!File.Exists(target))
                return target;

            string name = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);
            int index = 2;
            do
            {
                target = Path.Combine(targetDir, name + " (" + index + ")" + ext);
                index++;
            }
            while (File.Exists(target));

            return target;
        }

        private void SelectPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            for (int i = 0; i < _list.Items.Count; i++)
            {
                if (_list.Items[i] is ScriptRow row &&
                    string.Equals(row.Path, path, StringComparison.OrdinalIgnoreCase))
                {
                    _list.SelectedIndex = i;
                    return;
                }
            }
        }

        private void AcceptSelection()
        {
            if (string.IsNullOrWhiteSpace(SelectedPath))
                UpdateSelectedPath();

            if (string.IsNullOrWhiteSpace(SelectedPath))
                return;

            DialogResult = DialogResult.OK;
            Close();
        }

        private sealed class ScriptRow
        {
            public ScriptRow(string path)
            {
                Path = path;
                DisplayName = System.IO.Path.GetFileNameWithoutExtension(path);
            }

            public string Path { get; }
            public string DisplayName { get; }

            public override string ToString() => DisplayName;
        }
    }
}
