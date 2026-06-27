using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using DynLock.EncryptorGui.Auth;

namespace DynLock.EncryptorGui.Forms
{
    internal class LeaderManagementForm : Form
    {
        static readonly Color C_Header    = Color.FromArgb(18, 90, 175);
        static readonly Color C_HeaderSub = Color.FromArgb(160, 200, 255);
        static readonly Color C_Toolbar   = Color.FromArgb(244, 246, 251);
        static readonly Color C_Border    = Color.FromArgb(210, 218, 236);
        static readonly Color C_BtnBlue   = Color.FromArgb(30, 120, 215);
        static readonly Color C_BtnGray   = Color.FromArgb(218, 226, 242);
        static readonly Color C_GrayFg    = Color.FromArgb(52, 72, 108);
        static readonly Color C_Muted     = Color.FromArgb(108, 124, 152);
        static readonly Color C_Green     = Color.FromArgb(34, 126, 68);
        static readonly Color C_Red       = Color.FromArgb(192, 40, 40);

        readonly DataGridView _grid      = new DataGridView();
        readonly Button       _btnAdd    = new Button();
        readonly Button       _btnToggle = new Button();
        readonly Button       _btnDelete = new Button();
        readonly Label        _status    = new Label();

        public LeaderManagementForm()
        {
            Text          = "BIMLab Studio - Quản lý Email Leader";
            Size          = new Size(760, 520);
            MinimumSize   = new Size(640, 420);
            StartPosition = FormStartPosition.CenterParent;
            Font          = new Font("Segoe UI", 9.5f);
            BackColor     = Color.White;

            //  Header 
            var header = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = C_Header };
            header.Controls.Add(new Label
            {
                Text      = "Quản lý Email Leader",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(20, 14),
            });
            header.Controls.Add(new Label
            {
                Text      = "Danh sách email có quyền truy cập BIMLab Studio",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = C_HeaderSub,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(20, 44),
            });

            var sep1 = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = C_Border };

            //  Toolbar 
            var toolbar = new FlowLayoutPanel
            {
                Dock      = DockStyle.Top,
                Height    = 52,
                Padding   = new Padding(12, 10, 12, 10),
                BackColor = C_Toolbar,
            };
            StyledBtn(_btnAdd,    "  Thêm Leader",     C_BtnBlue,              Color.White, 140, 32);
            StyledBtn(_btnToggle, "  Bật / Tắt quyền", C_BtnGray,              C_GrayFg,   150, 32);
            StyledBtn(_btnDelete, "  Xóa",              Color.FromArgb(200,50,50), Color.White, 86,  32);
            _btnToggle.Enabled = false;
            _btnDelete.Enabled = false;
            _btnAdd.Click    += (_, __) => OnAddClicked();
            _btnToggle.Click += (_, __) => OnToggleClicked();
            _btnDelete.Click += (_, __) => OnDeleteClicked();
            toolbar.Controls.AddRange(new Control[] { _btnAdd, _btnToggle, _btnDelete });

            //  Grid 
            BuildGrid();
            _grid.SelectionChanged += (_, __) => RefreshToggleButton();

            //  Status bar 
            var sep2 = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = C_Border };

            _status.Dock      = DockStyle.Bottom;
            _status.Height    = 28;
            _status.Padding   = new Padding(14, 5, 14, 5);
            _status.Font      = new Font("Segoe UI", 9f);
            _status.ForeColor = C_Muted;
            _status.BackColor = Color.FromArgb(238, 243, 252);

            //  Compose 
            Controls.Add(_grid);
            Controls.Add(header);
            Controls.Add(sep1);
            Controls.Add(toolbar);
            Controls.Add(sep2);
            Controls.Add(_status);

            Load += async (_, __) => await LoadDataAsync();
        }

        //  Grid setup 

        void BuildGrid()
        {
            _grid.Dock                     = DockStyle.Fill;
            _grid.ReadOnly                 = true;
            _grid.AllowUserToAddRows       = false;
            _grid.AllowUserToDeleteRows    = false;
            _grid.MultiSelect              = false;
            _grid.SelectionMode            = DataGridViewSelectionMode.FullRowSelect;
            _grid.RowHeadersVisible        = false;
            _grid.BorderStyle              = BorderStyle.None;
            _grid.BackgroundColor          = Color.White;
            _grid.GridColor                = Color.FromArgb(228, 234, 246);
            _grid.Font                     = new Font("Segoe UI", 9.5f);
            _grid.RowTemplate.Height       = 30;
            _grid.EnableHeadersVisualStyles = false;

            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(244, 246, 251);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(52, 72, 108);
            _grid.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            _grid.ColumnHeadersHeight                     = 32;
            _grid.ColumnHeadersBorderStyle                = DataGridViewHeaderBorderStyle.Single;

            _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(210, 225, 250);
            _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(18, 60, 130);

            _grid.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "col_email",   HeaderText = "Email",              Width = 195 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "col_name",    HeaderText = "Họ tên",             Width = 135 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "col_status",  HeaderText = "Trạng thái",         Width = 88  });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "col_manage",  HeaderText = "Quản lý",            Width = 72  });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "col_addedby", HeaderText = "Được thêm bởi",
                  AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "col_last",    HeaderText = "Đăng nhập lần cuối", Width = 150 });

            foreach (DataGridViewColumn col in _grid.Columns)
                col.ReadOnly = true;

            _grid.CellFormatting += OnCellFormatting;
        }

        void OnCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            string colName = _grid.Columns[e.ColumnIndex].Name;

            if (colName == "col_status")
            {
                bool active = e.Value?.ToString() == "Hoạt động";
                e.CellStyle.ForeColor = active ? C_Green : C_Red;
                e.CellStyle.Font      = new Font("Segoe UI", 9f, active ? FontStyle.Bold : FontStyle.Regular);
            }
            else if (colName == "col_manage")
            {
                bool canManage = e.Value?.ToString() == "OK";
                e.CellStyle.ForeColor   = canManage ? C_Green : Color.FromArgb(200, 200, 200);
                e.CellStyle.Alignment   = DataGridViewContentAlignment.MiddleCenter;
                e.CellStyle.Font        = new Font("Segoe UI", 10f, FontStyle.Bold);
            }
        }

        //  Data loading 

        async Task LoadDataAsync()
        {
            _status.Text       = "Đang tải...";
            _btnAdd.Enabled    = false;
            _btnToggle.Enabled = false;
            _grid.Rows.Clear();

            try
            {
                var leaders = await SupabaseService.GetAllLeadersAsync();
                foreach (var l in leaders)
                {
                    int idx = _grid.Rows.Add();
                    var row = _grid.Rows[idx];
                    row.Cells["col_email"].Value   = l.Email;
                    row.Cells["col_name"].Value    = l.FullName;
                    row.Cells["col_status"].Value  = l.IsActive ? "Hoạt động" : "Đã tắt";
                    row.Cells["col_manage"].Value  = l.CanManage ? "OK" : "-";
                    row.Cells["col_addedby"].Value = string.IsNullOrEmpty(l.AddedBy) ? "(Admin)" : l.AddedBy;
                    row.Cells["col_last"].Value    = FormatDate(l.LastLogin);
                    row.Tag = l;

                    // Lam mo dong bi tat
                    if (!l.IsActive)
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(160, 160, 160);
                }

                _status.Text = $"{leaders.Count} leader trong hệ thống.";
            }
            catch (Exception ex)
            {
                _status.Text = "Lỗi tải dữ liệu: " + ex.Message;
            }
            finally
            {
                _btnAdd.Enabled = true;
                RefreshToggleButton();
            }
        }

        static string FormatDate(string iso)
        {
            if (string.IsNullOrEmpty(iso)) return "Chưa đăng nhập";
            return DateTime.TryParse(iso, out var dt)
                ? dt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                : iso;
        }

        //  Button state 

        void RefreshToggleButton()
        {
            if (_grid.SelectedRows.Count == 0)
            {
                _btnToggle.Enabled = false;
            _btnToggle.Text    = "  Bật / Tắt quyền";
                _btnDelete.Enabled = false;
                return;
            }

            var leader = _grid.SelectedRows[0].Tag as LeaderInfo;
            // Khong cho tat hoac xoa chinh minh
            bool canAct = leader != null && leader.Email != SessionContext.Email;
            _btnToggle.Enabled = canAct;
            _btnToggle.Text    = (leader?.IsActive == true) ? "  Tắt quyền" : ">  Bật quyền";
            _btnDelete.Enabled = canAct;
        }

        //  Actions 

        async void OnAddClicked()
        {
            using (var dlg = new AddLeaderDialog())
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                _btnAdd.Enabled    = false;
                _btnToggle.Enabled = false;
                _status.Text       = "Đang thêm...";

                try
                {
                    bool ok = await SupabaseService.AddLeaderAsync(dlg.LeaderEmail, dlg.LeaderName);
                    if (ok)
                        await LoadDataAsync();
                    else
                        _status.Text = "Thêm thất bại - email có thể đã tồn tại.";
                }
                catch (Exception ex)
                {
                    _status.Text = "Lỗi: " + ex.Message;
                }
                finally
                {
                    _btnAdd.Enabled = true;
                    RefreshToggleButton();
                }
            }
        }

        async void OnToggleClicked()
        {
            if (_grid.SelectedRows.Count == 0) return;
            var leader = _grid.SelectedRows[0].Tag as LeaderInfo;
            if (leader == null) return;

            bool   newState = !leader.IsActive;
            string action   = newState ? "bật quyền cho" : "tắt quyền của";

            var confirm = MessageBox.Show(this,
                $"Xac nhan {action}:\n{leader.Email}?",
                "BIMLab Studio", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            _btnAdd.Enabled    = false;
            _btnToggle.Enabled = false;
            _status.Text       = "Đang cập nhật...";

            try
            {
                bool ok = await SupabaseService.SetActiveAsync(leader.Email, newState);
                if (ok)
                    await LoadDataAsync();   // reload -> selection cleared -> RefreshToggleButton called
                else
                    _status.Text = "Cập nhật thất bại.";
            }
            catch (Exception ex)
            {
                _status.Text = "Lỗi: " + ex.Message;
            }
            finally
            {
                _btnAdd.Enabled = true;
                RefreshToggleButton();
            }
        }

        async void OnDeleteClicked()
        {
            if (_grid.SelectedRows.Count == 0) return;
            var leader = _grid.SelectedRows[0].Tag as LeaderInfo;
            if (leader == null) return;

            var confirm = MessageBox.Show(this,
                $"Xóa vĩnh viễn email:\n{leader.Email}\n\nHành động này không thể hoàn tác!",
                "BIMLab Studio", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes) return;

            _btnAdd.Enabled    = false;
            _btnToggle.Enabled = false;
            _btnDelete.Enabled = false;
            _status.Text       = "Đang xóa...";

            try
            {
                bool ok = await SupabaseService.DeleteLeaderAsync(leader.Email);
                if (ok)
                    await LoadDataAsync();
                else
                    _status.Text = "Xóa thất bại.";
            }
            catch (Exception ex)
            {
                _status.Text = "Lỗi: " + ex.Message;
            }
            finally
            {
                _btnAdd.Enabled = true;
                RefreshToggleButton();
            }
        }

        //  Helpers 

        static void StyledBtn(Button b, string text, Color bg, Color fg, int w, int h)
        {
            b.Text      = text;
            b.Size      = new Size(w, h);
            b.BackColor = bg;
            b.ForeColor = fg;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.Font      = new Font("Segoe UI", 9.5f);
            b.Cursor    = Cursors.Hand;
        }
    }
}