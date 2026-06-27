using System;
using System.Drawing;
using System.Windows.Forms;
using DynLock.Addin.DynamicPlugins;

namespace DynLock.Addin.UI
{
    internal sealed class LoadedPluginPreviewForm : Form
    {
        private readonly PluginItem _plugin;

        public LoadedPluginPreviewForm(PluginItem plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));

            Text = "BIMLab Player - Plugin loaded";
            Width = 780;
            Height = 430;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);

            var header = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Color.FromArgb(18, 90, 175) };
            var iconWrap = new Panel { Bounds = new Rectangle(16, 14, 44, 44), BackColor = Color.White };
            var iconBox = new PictureBox
            {
                Image = AddinIcons.PluginIcon(_plugin.IconPath, 36, _plugin.ButtonName),
                SizeMode = PictureBoxSizeMode.Zoom,
                Bounds = new Rectangle(4, 4, 36, 36),
                BackColor = Color.White,
            };
            iconWrap.Controls.Add(iconBox);

            var lblTitle = new Label
            {
                Text = "Plugin đã được nạp",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(72, 14),
            };
            header.Controls.AddRange(new Control[] { iconWrap, lblTitle });

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(16),
                BackColor = Color.White,
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 2; i++)
                body.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

            AddRow(body, 0, "Panel", _plugin.PanelName);
            AddRow(body, 1, "Plugin", _plugin.ButtonName);

            var bottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 58,
                Padding = new Padding(12, 8, 12, 8),
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.FromArgb(244, 246, 251),
            };

            var close = new Button { Text = "Đóng", DialogResult = DialogResult.OK };
            StyleButton(close, Color.FromArgb(34, 126, 68), Color.White, 88, 36);
            bottom.Controls.Add(close);
            bottom.Controls.Add(new Panel { Width = 0 });

            Controls.Add(body);
            Controls.Add(header);
            Controls.Add(bottom);

            AcceptButton = close;
            CancelButton = close;
        }

        private static void AddRow(TableLayoutPanel table, int row, string label, string value)
        {
            var left = new Label
            {
                Text = label,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(52, 72, 108),
                Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            };

            var right = new TextBox
            {
                Text = value ?? "",
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 250, 254),
            };

            table.Controls.Add(left, 0, row);
            table.Controls.Add(right, 1, row);
        }

        private static void StyleButton(Button b, Color bg, Color fg, int w, int h)
        {
            b.Width = w;
            b.Height = h;
            b.BackColor = bg;
            b.ForeColor = fg;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.Font = new Font("Segoe UI", 9.5f);
            b.Cursor = Cursors.Hand;
        }

    }
}
