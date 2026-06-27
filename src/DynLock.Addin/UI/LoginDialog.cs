using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using DynLock.Addin.Auth;

namespace DynLock.Addin.UI
{
    internal class LoginDialog : Form
    {
        static readonly Color C_Header   = Color.FromArgb(18, 90, 175);
        static readonly Color C_Sub      = Color.FromArgb(160, 200, 255);
        static readonly Color C_Toolbar  = Color.FromArgb(244, 246, 251);
        static readonly Color C_Border   = Color.FromArgb(210, 218, 236);
        static readonly Color C_BtnGreen = Color.FromArgb(34, 126, 68);
        static readonly Color C_BtnGray  = Color.FromArgb(218, 226, 242);
        static readonly Color C_GrayFg   = Color.FromArgb(52, 72, 108);
        static readonly Color C_Error    = Color.FromArgb(180, 40, 40);

        readonly TextBox _txtEmail  = new TextBox();
        readonly Button  _btnLogin  = new Button();
        readonly Label   _lblError  = new Label();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, string lParam);

        // Mo dialog; tra ve true neu login thanh cong.
        internal static bool ShowLogin()
        {
            using (var dlg = new LoginDialog())
                return dlg.ShowDialog() == DialogResult.OK;
        }

        private LoginDialog()
        {
            Text            = "BIMLab Player - Đăng nhập";
            Size            = new Size(420, 295);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterScreen;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.White;

            //  HEADER 
            var header = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = C_Header };

            var iconWrap = new Panel { Bounds = new Rectangle(16, 12, 44, 44), BackColor = Color.White };
            var iconBox  = new PictureBox
            {
                Image     = AddinIcons.HeaderIcon(36),
                SizeMode  = PictureBoxSizeMode.Zoom,
                Bounds    = new Rectangle(4, 4, 36, 36),
                BackColor = Color.White,
            };
            iconWrap.Controls.Add(iconBox);

            var lblTitle = new Label
            {
                Text      = "Đăng nhập BIMLab",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(70, 14),
            };
            var lblSub = new Label
            {
                Text      = "Nhập email được cấp quyền để sử dụng công cụ",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = C_Sub,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(72, 40),
            };
            header.Controls.AddRange(new Control[] { iconWrap, lblTitle, lblSub });

            var sep1 = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = C_Border };

            //  BODY 
            var body = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            var lblEmailLbl = new Label
            {
                Text      = "Địa chỉ Email",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = C_GrayFg,
                AutoSize  = true,
                Location  = new Point(24, 20),
            };

            _txtEmail.Location    = new Point(24, 42);
            _txtEmail.Width       = 356;
            _txtEmail.Height      = 28;
            _txtEmail.Font        = new Font("Segoe UI", 10f);
            _txtEmail.BorderStyle = BorderStyle.FixedSingle;
            _txtEmail.KeyDown    += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; TryLogin(); }
            };

            _lblError.Text      = "";
            _lblError.Font      = new Font("Segoe UI", 9f);
            _lblError.ForeColor = C_Error;
            _lblError.AutoSize  = true;
            _lblError.Location  = new Point(24, 80);

            body.Controls.AddRange(new Control[] { lblEmailLbl, _txtEmail, _lblError });

            //  BOTTOM 
            var bottom = new FlowLayoutPanel
            {
                Dock          = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height        = 54,
                Padding       = new Padding(12, 8, 12, 8),
                BackColor     = C_Toolbar,
            };
            var sep2 = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = C_Border };

            var btnCancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel };
            StyleBtn(_btnLogin, C_BtnGreen, Color.White, 120, 38);
            StyleBtn(btnCancel, C_BtnGray,  C_GrayFg,    80, 38);
            _btnLogin.Text   = "Tiếp tục  ->";
            _btnLogin.Click += (_, __) => TryLogin();
            bottom.Controls.AddRange(new Control[] { btnCancel, _btnLogin });

            Controls.Add(body);
            Controls.Add(sep1);
            Controls.Add(header);
            Controls.Add(bottom);
            Controls.Add(sep2);

            AcceptButton = _btnLogin;
            CancelButton = btnCancel;
            Load        += (_, __) => SendMessage(_txtEmail.Handle, 0x1501, IntPtr.Zero, "vd: ten@gmail.com");
        }

        static void StyleBtn(Button b, Color bg, Color fg, int w, int h)
        {
            b.Width     = w; b.Height = h;
            b.BackColor = bg; b.ForeColor = fg;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize  = 0;
            b.FlatAppearance.BorderColor = bg;
            b.Font   = new Font("Segoe UI", 9.5f);
            b.Cursor = Cursors.Hand;
        }

        private async void TryLogin()
        {
            string email = _txtEmail.Text?.Trim().ToLowerInvariant() ?? "";
            if (email.Length == 0 || !email.Contains("@") || !email.Contains("."))
            {
                ShowError("Email không hợp lệ.");
                return;
            }

            SetLoading(true);
            bool ok = await Task.Run(() => AddinAuthService.TryLogin(email));
            SetLoading(false);

            if (ok)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                ShowError(string.IsNullOrWhiteSpace(AddinAuthService.LastError) ? "Email này chưa được cấp quyền truy cập." : AddinAuthService.LastError);
            }
        }

        private void ShowError(string msg)   => _lblError.Text = msg;

        private void SetLoading(bool loading)
        {
            _btnLogin.Enabled = !loading;
            _txtEmail.Enabled = !loading;
            _btnLogin.Text    = loading ? "Đang kiểm tra..." : "Tiếp tục  ->";
            _lblError.Text    = "";
        }
    }
}