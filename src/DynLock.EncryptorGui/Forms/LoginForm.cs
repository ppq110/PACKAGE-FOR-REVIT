using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DynLock.EncryptorGui.Auth;

namespace DynLock.EncryptorGui.Forms
{
    internal class LoginForm : Form
    {
        static readonly Color C_Header    = Color.FromArgb(18, 90, 175);
        static readonly Color C_HeaderSub = Color.FromArgb(160, 200, 255);
        static readonly Color C_BtnBlue   = Color.FromArgb(30, 120, 215);
        static readonly Color C_Border    = Color.FromArgb(210, 218, 236);
        static readonly Color C_GrayFg    = Color.FromArgb(52, 72, 108);
        static readonly Color C_Error     = Color.FromArgb(192, 40, 40);

        readonly TextBox _emailBox = new TextBox();
        readonly Button  _btnLogin = new Button();
        readonly Label   _lblError = new Label();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, string lParam);

        public LoginForm()
        {
            Text            = "BIMLab Studio";
            Size            = new Size(420, 310);
            MinimumSize     = new Size(420, 310);
            MaximumSize     = new Size(420, 310);
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox     = false;
            Font            = new Font("Segoe UI", 9.5f);
            BackColor       = Color.White;

            //  Header 
            var header = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = C_Header };
            header.Controls.Add(new Label
            {
                Text      = "BIMLab Studio",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(20, 14),
            });
            header.Controls.Add(new Label
            {
                Text      = "Nhập email được cấp quyền để tiếp tục",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = C_HeaderSub,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(20, 44),
            });

            var sep = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = C_Border };

            //  Content 
            var content = new Panel { Dock = DockStyle.Fill };

            var lblEmail = new Label
            {
                Text      = "Địa chỉ email",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = C_GrayFg,
                AutoSize  = true,
                Location  = new Point(28, 24),
            };

            _emailBox.Location    = new Point(28, 46);
            _emailBox.Width       = 360;
            _emailBox.Font        = new Font("Segoe UI", 10.5f);
            _emailBox.BorderStyle = BorderStyle.FixedSingle;
            _emailBox.KeyDown    += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; TryLogin(); }
            };

            _lblError.AutoSize  = true;
            _lblError.Font      = new Font("Segoe UI", 8.5f);
            _lblError.ForeColor = C_Error;
            _lblError.Location  = new Point(28, 80);
            _lblError.Text      = "";

            _btnLogin.Text      = "Tiếp tục  ->";
            _btnLogin.Location  = new Point(28, 130);
            _btnLogin.Size      = new Size(360, 38);
            _btnLogin.BackColor = C_BtnBlue;
            _btnLogin.ForeColor = Color.White;
            _btnLogin.FlatStyle = FlatStyle.Flat;
            _btnLogin.FlatAppearance.BorderSize = 0;
            _btnLogin.Font      = new Font("Segoe UI", 10f, FontStyle.Bold);
            _btnLogin.Cursor    = Cursors.Hand;
            _btnLogin.Click    += (s, e) => TryLogin();

            content.Controls.AddRange(new Control[]
                { lblEmail, _emailBox, _lblError, _btnLogin });

            Controls.Add(content);
            Controls.Add(sep);
            Controls.Add(header);

            ActiveControl = _emailBox;
            Load += OnLoad;
        }

        void OnLoad(object sender, EventArgs e)
        {
            // Placeholder text (Win32, tuong thich .NET 4.8)
            SendMessage(_emailBox.Handle, 0x1501 /*EM_SETCUEBANNER*/, IntPtr.Zero, "leader@company.com");
        }

        async void TryLogin()
        {
            var email = _emailBox.Text.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(email)) { ShowError("Vui lòng nhập địa chỉ email."); return; }
            if (!email.Contains("@"))        { ShowError("Email không hợp lệ."); return; }

            SetLoading(true);
            try
            {
                bool ok = await Auth.AuthServerService.LoginAsync(email);
                if (ok)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    ShowError("Email này chưa được cấp quyền truy cập.");
                }
            }
            catch (Exception ex)
            {
                ShowError("Không thể kết nối: " + ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }

        void ShowError(string msg) => _lblError.Text = msg;

        void SetLoading(bool loading)
        {
            _btnLogin.Enabled = !loading;
            _emailBox.Enabled = !loading;
            _btnLogin.Text    = loading ? "Đang kiểm tra..." : "Tiếp tục  ->";
            _lblError.Text    = loading ? "" : _lblError.Text;
        }
    }
}
