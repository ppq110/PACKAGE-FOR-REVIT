using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace DynLock.Installer
{
    internal class LoginForm : Form
    {
        private const string Base = @"C:\ProgramData\BIMLab\DynLock";
        private const string BuiltInAuthServerUrl = "http://192.168.110.213:5050";

        private static readonly Color HeaderColor = Color.FromArgb(18, 90, 175);
        private static readonly Color BorderColor = Color.FromArgb(210, 218, 236);
        private static readonly Color GreenButton = Color.FromArgb(34, 126, 68);
        private static readonly Color GrayText = Color.FromArgb(52, 72, 108);
        private static readonly Color ErrorText = Color.FromArgb(180, 40, 40);

        private readonly TextBox _email = new TextBox();
        private readonly Button _login = new Button();
        private readonly Label _error = new Label();
        private readonly string _authServerUrl;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, string lParam);

        public LoginForm()
        {
            Text = "BIMLab Player - Đăng nhập";
            Size = new Size(480, 290);
            MinimumSize = new Size(480, 290);
            MaximumSize = new Size(480, 290);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 9.5f);
            BackColor = Color.White;
            _authServerUrl = LoadExistingAuthServerUrl();

            var header = new Panel { Dock = DockStyle.Top, Height = 74, BackColor = HeaderColor };
            header.Controls.Add(new Label
            {
                Text = "BIMLab Player",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(22, 14),
            });
            header.Controls.Add(new Label
            {
                Text = "Đăng nhập Gmail trước khi cài add-in",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(160, 200, 255),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(24, 44),
            });

            var sep = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = BorderColor };
            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 22, 28, 12) };

            body.Controls.Add(new Label
            {
                Text = "Gmail",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = GrayText,
                AutoSize = true,
                Location = new Point(28, 28),
            });
            _email.Location = new Point(28, 50);
            _email.Width = 408;
            _email.Font = new Font("Segoe UI", 10f);
            _email.BorderStyle = BorderStyle.FixedSingle;
            _email.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    TryLogin();
                }
            };

            _error.Text = "";
            _error.Font = new Font("Segoe UI", 8.5f);
            _error.ForeColor = ErrorText;
            _error.AutoSize = true;
            _error.Location = new Point(28, 86);

            _login.Text = "Tiếp tục";
            _login.Location = new Point(28, 122);
            _login.Size = new Size(408, 38);
            _login.BackColor = GreenButton;
            _login.ForeColor = Color.White;
            _login.FlatStyle = FlatStyle.Flat;
            _login.FlatAppearance.BorderSize = 0;
            _login.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            _login.Cursor = Cursors.Hand;
            _login.Click += (_, __) => TryLogin();

            body.Controls.AddRange(new Control[] { _email, _error, _login });
            Controls.Add(body);
            Controls.Add(sep);
            Controls.Add(header);

            AcceptButton = _login;
            Load += (_, __) =>
            {
                SendMessage(_email.Handle, 0x1501, IntPtr.Zero, "ten@gmail.com");
            };
        }

        private async void TryLogin()
        {
            string authUrl = NormalizeUrl(_authServerUrl);
            string email = (_email.Text ?? "").Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(authUrl))
            {
                ShowError("Thiếu cấu hình máy chủ Auth Server.");
                return;
            }

            if (!IsGmail(email))
            {
                ShowError("Gmail phải có dạng ten@gmail.com.");
                return;
            }

            SetLoading(true);
            try
            {
                var user = await Task.Run(() => CheckLogin(authUrl, email));
                if (user == null)
                {
                    ShowError("Gmail này chưa được cấp quyền truy cập.");
                    return;
                }

                AuthSession.Email = user.Email;
                AuthSession.FullName = user.FullName;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                ShowError("Không kết nối được Auth Server: " + ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private static UserInfo CheckLogin(string authServerUrl, string email)
        {
            string url = authServerUrl + "/api/auth/check?email=" + Uri.EscapeDataString(email);
            using (var wc = new WebClient())
            {
                wc.Headers.Add("Accept", "application/json");
                try
                {
                    string json = wc.DownloadString(url);
                    var row = new JavaScriptSerializer()
                        .Deserialize<Dictionary<string, object>>(json);
                    if (row == null || !Get(row, "isActive", false))
                        return null;

                    return new UserInfo
                    {
                        Email = Get(row, "email", email),
                        FullName = Get(row, "fullName", ""),
                    };
                }
                catch (WebException ex)
                {
                    if (ex.Response is HttpWebResponse resp &&
                        resp.StatusCode == HttpStatusCode.NotFound)
                        return null;

                    throw;
                }
            }
        }

        private static T Get<T>(Dictionary<string, object> row, string key, T def)
        {
            if (row == null || !row.ContainsKey(key) || row[key] == null)
                return def;

            try { return (T)row[key]; }
            catch { return def; }
        }

        private static string LoadExistingAuthServerUrl()
        {
            foreach (string path in AuthConfigCandidates())
            {
                try
                {
                    if (!File.Exists(path))
                        continue;

                    string json = File.ReadAllText(path);
                    var row = new JavaScriptSerializer()
                        .Deserialize<Dictionary<string, object>>(json);
                    if (row != null && row.TryGetValue("AuthServerUrl", out object value))
                        return NormalizeUrl(value?.ToString());
                }
                catch
                {
                }
            }

            return BuiltInAuthServerUrl;
        }

        private static IEnumerable<string> AuthConfigCandidates()
        {
            yield return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "authserver.json");
            yield return Path.Combine(Base, "authserver.json");
        }

        private static string NormalizeUrl(string value)
        {
            return (value ?? "").Trim().TrimEnd('/');
        }

        private static bool IsGmail(string email)
        {
            email = (email ?? "").Trim().ToLowerInvariant();
            return email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase)
                && email.Length > "@gmail.com".Length;
        }

        private void ShowError(string message)
        {
            _error.Text = message;
        }

        private void SetLoading(bool loading)
        {
            _login.Enabled = !loading;
            _email.Enabled = !loading;
            _login.Text = loading ? "Đang kiểm tra..." : "Tiếp tục";
            if (loading)
                _error.Text = "";
        }

        private sealed class UserInfo
        {
            public string Email { get; set; }
            public string FullName { get; set; }
        }
    }
}
