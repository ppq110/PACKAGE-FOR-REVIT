using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DynLock.Core;
using DynLock.EncryptorGui.Auth;

namespace DynLock.EncryptorGui
{
    public class MainForm : Form
    {
        // Palette
        static readonly Color C_Header    = Color.FromArgb(18, 90, 175);
        static readonly Color C_HeaderSub = Color.FromArgb(160, 200, 255);
        static readonly Color C_Toolbar   = Color.FromArgb(244, 246, 251);
        static readonly Color C_Border    = Color.FromArgb(210, 218, 236);
        static readonly Color C_BtnBlue   = Color.FromArgb(30, 120, 215);
        static readonly Color C_BtnGreen  = Color.FromArgb(34, 126, 68);
        static readonly Color C_BtnGray   = Color.FromArgb(218, 226, 242);
        static readonly Color C_GrayFg    = Color.FromArgb(52, 72, 108);
        static readonly Color C_Muted     = Color.FromArgb(108, 124, 152);
        static readonly Color C_StatusBg  = Color.FromArgb(238, 243, 252);

        readonly ListView _list          = new ListView();
        readonly Button   _add           = new Button();
        readonly Button   _clear         = new Button();
        readonly Button   _manageLeaders = new Button();
        readonly Button   _run           = new Button();
        readonly Button   _openFolder    = new Button();
        readonly Button   _browseIcon    = new Button();
        readonly TextBox  _panelName     = new TextBox();
        readonly TextBox  _pluginName    = new TextBox();
        readonly TextBox  _iconPath      = new TextBox();
        readonly Label    _status        = new Label();
        readonly Label    _emptyHint     = new Label();

        readonly List<string> _files = new List<string>();
        string _lastOutDir;

        public MainForm()
        {
            Text          = "BIMLab Studio - " + SessionContext.FullName;
            Size          = new Size(780, 560);
            MinimumSize   = new Size(640, 460);
            StartPosition = FormStartPosition.CenterScreen;
            Font          = new Font("Segoe UI", 9.5f);
            BackColor     = Color.White;
            AllowDrop     = true;
            DragEnter    += OnDragEnter;
            DragDrop     += OnDragDrop;

            //  HEADER 
            var header = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 76,
                BackColor = C_Header,
            };

            var iconBmp = LoadBimDynamoIcon(40);
            // Nen trang de icon noi bat tren header xanh
            var iconWrap = new Panel
            {
                Bounds    = new Rectangle(18, 14, 48, 48),
                BackColor = Color.White,
            };
            var iconBox = new PictureBox
            {
                Image     = iconBmp != null ? (Image)iconBmp : (Image)LogoHelper.GenerateLogo(40),
                SizeMode  = PictureBoxSizeMode.Zoom,
                Bounds    = new Rectangle(4, 4, 40, 40),
                BackColor = Color.White,
            };
            iconWrap.Controls.Add(iconBox);
            var lblTitle = new Label
            {
                Text      = "BIMLab Studio",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(78, 17),
            };
            var lblSub = new Label
            {
                Text      = "Chọn file .dyn để mã hóa ra .dynx   -   " + SessionContext.Email,
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = C_HeaderSub,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(80, 46),
            };
            header.Controls.AddRange(new Control[] { iconWrap, lblTitle, lblSub });

            var sep1 = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = C_Border };

            //  TOOLBAR 
            var toolbar = new FlowLayoutPanel
            {
                Dock      = DockStyle.Top,
                Height    = 52,
                Padding   = new Padding(12, 10, 12, 10),
                BackColor = C_Toolbar,
            };

            Btn(_add,   "  Thêm file .dyn", C_BtnBlue, Color.White, 152, 32);
            Btn(_clear, "x  Xóa danh sách",    C_BtnGray, C_GrayFg,   126, 32);
            _add.Click   += (_, __) => AddFilesDialog();
            _clear.Click += (_, __) =>
            {
                _files.Clear();
                _list.Items.Clear();
                _emptyHint.Visible = true;
                SetStatus("");
            };
            toolbar.Controls.AddRange(new Control[] { _add, _clear });

            // Nut quan ly email - chi hien khi leader co quyen can_manage
            if (SessionContext.Email == Auth.SupabaseConfig.SuperAdminEmail)
            {
                Btn(_manageLeaders, "  Quản lý Email", C_BtnGray, C_GrayFg, 148, 32);
                _manageLeaders.Margin = new Padding(3, 3, 3, 3);
                _manageLeaders.FlatAppearance.BorderSize  = 1;
                _manageLeaders.FlatAppearance.BorderColor = C_Border;
                _manageLeaders.Click += (_, __) =>
                {
                    using (var dlg = new Forms.LeaderManagementForm())
                        dlg.ShowDialog(this);
                };
                toolbar.Controls.Add(_manageLeaders);
            }

            var pluginPanel = BuildPluginPanel();

            //  LIST PANEL 
            var listPanel = new Panel { Dock = DockStyle.Fill };

            _list.Dock          = DockStyle.Fill;
            _list.View          = View.Details;
            _list.FullRowSelect = true;
            _list.GridLines     = false;
            _list.BorderStyle   = BorderStyle.None;
            _list.Font          = new Font("Segoe UI", 9.5f);
            _list.BackColor     = Color.White;
            _list.HeaderStyle   = ColumnHeaderStyle.Nonclickable;
            _list.Columns.Add("File .dyn", 390);
            _list.Columns.Add("Trạng thái", 330);

            // Empty-state hint overlay (ẩn khi có file, hiện khi danh sách trống)
            _emptyHint.Text      = "Kéo & thả file .dyn vào đây\r\nhoặc bấm  \" Thêm file .dyn \"";
            _emptyHint.Font      = new Font("Segoe UI", 11f);
            _emptyHint.ForeColor = Color.FromArgb(168, 184, 212);
            _emptyHint.TextAlign = ContentAlignment.MiddleCenter;
            _emptyHint.Dock      = DockStyle.Fill;
            _emptyHint.BackColor = Color.White;
            _emptyHint.Visible   = true;

            listPanel.Controls.Add(_list);
            listPanel.Controls.Add(_emptyHint);
            _emptyHint.BringToFront();

            //  BOTTOM 
            var bottomFlow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height        = 58,
                Padding       = new Padding(14, 10, 14, 10),
                BackColor     = C_Toolbar,
            };

            Btn(_run,        "  Mã hóa tất cả",  C_BtnGreen, Color.White, 170, 38);
            Btn(_openFolder, "Mở thư mục kết quả", C_BtnGray,  C_GrayFg,   162, 38);
            _openFolder.Enabled = false;
            _run.Click        += (_, __) => EncryptAll();
            _openFolder.Click += (_, __) => OpenLastFolder();
            bottomFlow.Controls.AddRange(new Control[] { _run, _openFolder });

            var sep2 = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = C_Border };

            //  STATUS 
            _status.Dock      = DockStyle.Bottom;
            _status.Height    = 28;
            _status.Padding   = new Padding(14, 5, 14, 5);
            _status.Font      = new Font("Segoe UI", 9f);
            _status.ForeColor = C_Muted;
            _status.BackColor = C_StatusBg;

            //  COMPOSE 
            // Top: thu tu add = thu tu tu tren xuong.
            // Bottom: phan tu add sau cung = sat day nhat.
            Controls.Add(listPanel);   // Fill
            Controls.Add(header);      // Top -> cao nhat
            Controls.Add(sep1);        // Top -> duoi header
            Controls.Add(toolbar);     // Top -> duoi sep1
            Controls.Add(pluginPanel); // Top -> thong tin plugin
            Controls.Add(bottomFlow);  // Bottom -> tren sep2
            Controls.Add(sep2);        // Bottom -> tren status
            Controls.Add(_status);     // Bottom -> sat day nhat
        }

        private Panel BuildPluginPanel()
        {
            var host = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                Padding = new Padding(12, 8, 12, 8),
                BackColor = Color.White,
            };

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 2,
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

            _panelName.Text = "Utilities";
            _pluginName.Text = "";
            _iconPath.ReadOnly = true;

            AddPluginLabel(table, "Tên mục", 0, 0);
            table.Controls.Add(_panelName, 1, 0);
            AddPluginLabel(table, "Tên plugin", 2, 0);
            table.Controls.Add(_pluginName, 3, 0);

            AddPluginLabel(table, "Icon", 0, 1);
            table.Controls.Add(_iconPath, 1, 1);
            table.SetColumnSpan(_iconPath, 2);
            Btn(_browseIcon, "Chọn icon", C_BtnGray, C_GrayFg, 96, 28);
            _browseIcon.Click += (_, __) => ChooseIcon();
            table.Controls.Add(_browseIcon, 3, 1);

            foreach (Control c in table.Controls)
            {
                c.Margin = new Padding(3, 3, 8, 3);
                if (c is TextBox tb)
                    tb.Dock = DockStyle.Fill;
            }

            host.Controls.Add(table);
            return host;
        }

        private static void AddPluginLabel(TableLayoutPanel table, string text, int col, int row)
        {
            table.Controls.Add(new Label
            {
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = C_GrayFg,
                Dock = DockStyle.Fill,
            }, col, row);
        }

        private void ChooseIcon()
        {
            using (var dlg = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.ico)|*.png;*.jpg;*.jpeg;*.bmp;*.ico",
                Title = "Chọn icon plugin",
            })
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    _iconPath.Text = dlg.FileName;
            }
        }

        static void Btn(Button b, string text, Color bg, Color fg, int w, int h)
        {
            b.Text      = text;
            b.Width     = w;
            b.Height    = h;
            b.BackColor = bg;
            b.ForeColor = fg;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize  = 0;
            b.FlatAppearance.BorderColor = bg;
            b.Font   = new Font("Segoe UI", 9.5f);
            b.Cursor = Cursors.Hand;
        }

        //  DRAG & DROP 

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            AddFiles(paths);
        }

        //  FILE MANAGEMENT 

        private void AddFilesDialog()
        {
            using (var dlg = new OpenFileDialog
            {
                Filter     = "Dynamo script (*.dyn)|*.dyn",
                Multiselect = true,
                Title      = "Chọn file .dyn cần mã hóa",
            })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    AddFiles(dlg.FileNames);
            }
        }

        private void AddFiles(IEnumerable<string> paths)
        {
            foreach (var p in paths)
            {
                var toAdd = new List<string>();
                if (Directory.Exists(p))
                    toAdd.AddRange(Directory.GetFiles(p, "*.dyn"));
                else if (File.Exists(p) &&
                         string.Equals(Path.GetExtension(p), ".dyn", StringComparison.OrdinalIgnoreCase))
                    toAdd.Add(p);

                foreach (var f in toAdd)
                {
                    if (_files.Any(x => string.Equals(x, f, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    _files.Add(f);
                    var item = new ListViewItem(Path.GetFileName(f));
                    item.SubItems.Add("Chờ mã hóa");
                    _list.Items.Add(item);
                }
            }

            if (_files.Count > 0)
                _emptyHint.Visible = false;

            SetStatus(_files.Count + " file trong danh sách.");
        }

        //  ENCRYPT 

        private void EncryptAll()
        {
            if (_files.Count == 0)
            {
                MessageBox.Show(this, "Chưa có file nào. Hãy thêm file .dyn trước.",
                    "BIMLab", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!Secrets.TryGetMasterKeyBytes(out byte[] masterKey, out string keyError))
            {
                MessageBox.Show(this, keyError,
                    "BIMLab", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int ok = 0, fail = 0;
            for (int i = 0; i < _files.Count; i++)
            {
                string dyn = _files[i];
                try
                {
                    string graphJson = File.ReadAllText(dyn);
                    string pluginName = string.IsNullOrWhiteSpace(_pluginName.Text)
                        ? Path.GetFileNameWithoutExtension(dyn)
                        : _pluginName.Text;
                    byte[] plain = DynxPackage.Create(
                        graphJson,
                        _panelName.Text,
                        pluginName,
                        _iconPath.Text,
                        Path.GetFileName(dyn));
                    byte[] blob  = DynxCrypto.Encrypt(plain, masterKey);
                    string outPath = Path.Combine(
                        Path.GetDirectoryName(dyn),
                        Path.GetFileNameWithoutExtension(dyn) + ".dynx");
                    File.WriteAllBytes(outPath, blob);
                    _lastOutDir = Path.GetDirectoryName(outPath);

                    _list.Items[i].SubItems[1].Text = "OK Xong  ->  " + Path.GetFileName(outPath);
                    _list.Items[i].ForeColor = Color.FromArgb(30, 120, 68);
                    ok++;
                }
                catch (Exception ex)
                {
                    _list.Items[i].SubItems[1].Text = " Lỗi: " + ex.Message;
                    _list.Items[i].ForeColor = Color.FromArgb(192, 40, 40);
                    fail++;
                }
            }

            _openFolder.Enabled = _lastOutDir != null;
            SetStatus($"Hoàn tất: {ok} thành công" + (fail > 0 ? $", {fail} lỗi." : "."));
            MessageBox.Show(this,
                    $"Đã mã hóa {ok} file" + (fail > 0 ? $", {fail} file lỗi." : ".") +
                "\n\nChỉ gửi file .dynx cho member. Giữ file .dyn gốc lại.",
                    "BIMLab", MessageBoxButtons.OK,
                    fail > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        private void OpenLastFolder()
        {
            if (_lastOutDir != null && Directory.Exists(_lastOutDir))
                System.Diagnostics.Process.Start("explorer.exe", "\"" + _lastOutDir + "\"");
        }

        private void SetStatus(string text) => _status.Text = text;

        //  ICON LOADER 

        private static Bitmap LoadBimDynamoIcon(int size)
        {
            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                using (var s = asm.GetManifestResourceStream("DynLock.EncryptorGui.Resources.BimDynamo.ico"))
                {
                    if (s == null) return null;

                    var raw = new byte[s.Length];
                    s.Read(raw, 0, raw.Length);

                    int count = BitConverter.ToUInt16(raw, 4);
                    int bestIdx = 0, bestDiff = int.MaxValue;
                    for (int i = 0; i < count; i++)
                    {
                        int b = 6 + i * 16;
                        int w = raw[b] == 0 ? 256 : raw[b];
                        int d = Math.Abs(w - size);
                        if (d < bestDiff) { bestDiff = d; bestIdx = i; }
                    }

                    int entryBase = 6 + bestIdx * 16;
                    int dataSize  = (int)BitConverter.ToUInt32(raw, entryBase + 8);
                    int dataOff   = (int)BitConverter.ToUInt32(raw, entryBase + 12);

                    using (var ms = new MemoryStream(raw, dataOff, dataSize))
                    {
                        var src = new Bitmap(ms);
                        if (src.Width == size && src.Height == size) return src;

                        var resized = new Bitmap(size, size);
                        using (var g = Graphics.FromImage(resized))
                        {
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.DrawImage(src, 0, 0, size, size);
                        }
                        src.Dispose();
                        return resized;
                    }
                }
            }
            catch { return null; }
        }
    }
}
