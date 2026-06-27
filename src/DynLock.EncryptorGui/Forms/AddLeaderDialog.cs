using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DynLock.EncryptorGui.Forms
{
    internal class AddLeaderDialog : Form
    {
        static readonly Color C_BtnBlue = Color.FromArgb(30, 120, 215);
        static readonly Color C_BtnGray = Color.FromArgb(218, 226, 242);
        static readonly Color C_GrayFg  = Color.FromArgb(52, 72, 108);
        static readonly Color C_Border  = Color.FromArgb(210, 218, 236);

        readonly TextBox _email = new TextBox();
        readonly TextBox _name  = new TextBox();

        public string LeaderEmail => _email.Text.Trim().ToLowerInvariant();
        public string LeaderName  => _name.Text.Trim();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, string lParam);

        // Margin trai/phai va layout constants
        const int MX = 24;   // margin ngang
        const int TW = 332;  // chieu rong control (380 - 24*2)

        public AddLeaderDialog()
        {
            Text            = "Thêm Leader mới";
            Size            = new Size(380, 266);
            MinimumSize     = new Size(380, 266);
            MaximumSize     = new Size(380, 266);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            Font            = new Font("Segoe UI", 9.5f);
            BackColor       = Color.White;

            var content = new Panel { Dock = DockStyle.Fill };

            //  Email 
            var lblEmail = MakeLabel("Email *", MX, 20);

            _email.Bounds      = new Rectangle(MX, 42, TW, 28);
            _email.Font        = new Font("Segoe UI", 10f);
            _email.BorderStyle = BorderStyle.FixedSingle;

            //  Ten 
            var lblName = MakeLabel("Ho va ten", MX, 82);

            _name.Bounds      = new Rectangle(MX, 104, TW, 28);
            _name.Font        = new Font("Segoe UI", 10f);
            _name.BorderStyle = BorderStyle.FixedSingle;

            //  Separator 
            var sep = new Panel
            {
                Bounds    = new Rectangle(MX, 150, TW, 1),
                BackColor = C_Border,
            };

            //  Buttons 
            var btnOk     = MakeBtn("Thêm", C_BtnBlue, Color.White,
                                    new Rectangle(MX + TW - 140, 162, 140, 36), bold: true);
            var btnCancel = MakeBtn("Hủy",  C_BtnGray, C_GrayFg,
                                    new Rectangle(MX,             162, 140, 36), bold: false);

            btnOk.Click     += (_, __) => Confirm();
            btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };
            AcceptButton     = btnOk;
            CancelButton     = btnCancel;

            content.Controls.AddRange(new Control[]
                { lblEmail, _email, lblName, _name, sep, btnOk, btnCancel });
            Controls.Add(content);

            ActiveControl = _email;
            Load += OnLoad;
        }

        void OnLoad(object sender, EventArgs e)
        {
            SendMessage(_email.Handle, 0x1501, IntPtr.Zero, "leader@company.com");
            SendMessage(_name.Handle,  0x1501, IntPtr.Zero, "Nguyen Van A");
        }

        void Confirm()
        {
            var em = _email.Text.Trim();
            if (string.IsNullOrWhiteSpace(em))
            {
                MessageBox.Show("Vui lòng nhập email.", "BIMLab", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!em.Contains("@") || !em.Contains("."))
            {
                MessageBox.Show("Email không hợp lệ.", "BIMLab", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        static Label MakeLabel(string text, int x, int y) => new Label
        {
            Text      = text,
            AutoSize  = true,
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(52, 72, 108),
            Location  = new Point(x, y),
        };

        static Button MakeBtn(string text, Color bg, Color fg, Rectangle bounds, bool bold)
        {
            var b = new Button
            {
                Text      = text,
                Bounds    = bounds,
                BackColor = bg,
                ForeColor = fg,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f, bold ? FontStyle.Bold : FontStyle.Regular),
                Cursor    = Cursors.Hand,
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}