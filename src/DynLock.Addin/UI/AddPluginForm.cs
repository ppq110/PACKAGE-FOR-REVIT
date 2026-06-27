using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using DynLock.Addin.DynamicPlugins;

namespace DynLock.Addin.UI
{
    public class AddPluginForm : Form
    {
        private readonly TextBox _dynxPath = new TextBox();
        private readonly TextBox _panelName = new TextBox();
        private readonly TextBox _buttonName = new TextBox();
        private readonly Button _ok = new Button();

        public AddPluginForm()
        {
            Text = "BIMLab Manager - Add";
            Width = 620;
            Height = 280;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);

            var header = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Color.FromArgb(18, 90, 175) };
            header.Controls.Add(new Label
            {
                Text = "Add dynx plugin",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(16, 18),
            });

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                ColumnCount = 3,
                RowCount = 3,
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            var browse = new Button { Text = "Browse..." };
            StyleButton(browse, Color.FromArgb(218, 226, 242), Color.FromArgb(52, 72, 108), 84, 30);
            browse.Click += (_, __) => BrowseDynx();

            AddLabel(body, "Dynx file", 0);
            AddLabel(body, "Panel name", 1);
            AddLabel(body, "Plugin name", 2);

            body.Controls.Add(_dynxPath, 1, 0);
            body.Controls.Add(browse, 2, 0);
            body.Controls.Add(_panelName, 1, 1);
            body.SetColumnSpan(_panelName, 2);
            body.Controls.Add(_buttonName, 1, 2);
            body.SetColumnSpan(_buttonName, 2);

            _dynxPath.Dock = DockStyle.Fill;
            _panelName.Dock = DockStyle.Fill;
            _buttonName.Dock = DockStyle.Fill;
            _panelName.Text = "Script Engine";

            var bottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                Padding = new Padding(12, 8, 12, 8),
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.FromArgb(244, 246, 251),
            };

            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
            _ok.Text = "Add";
            StyleButton(cancel, Color.FromArgb(218, 226, 242), Color.FromArgb(52, 72, 108), 88, 36);
            StyleButton(_ok, Color.FromArgb(34, 126, 68), Color.White, 96, 36);
            _ok.Click += (_, __) => SavePlugin();
            bottom.Controls.Add(_ok);
            bottom.Controls.Add(cancel);

            Controls.Add(body);
            Controls.Add(header);
            Controls.Add(bottom);

            AcceptButton = _ok;
            CancelButton = cancel;
        }

        private static void AddLabel(TableLayoutPanel table, string text, int row)
        {
            table.Controls.Add(new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(52, 72, 108),
            }, 0, row);
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

        private void BrowseDynx()
        {
            using (var dlg = new OpenFileDialog
            {
                Filter = "BIMLab dynx (*.dynx)|*.dynx",
                Title = "Select dynx file",
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                _dynxPath.Text = dlg.FileName;
                if (string.IsNullOrWhiteSpace(_buttonName.Text))
                    _buttonName.Text = Path.GetFileNameWithoutExtension(dlg.FileName);
            }
        }

        private void SavePlugin()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_dynxPath.Text) || !File.Exists(_dynxPath.Text))
                {
                    MessageBox.Show("Choose a valid .dynx file.", "BIMLab Manager",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (PluginConfigStore.OrderedPlugins().Count >= PluginConfigStore.MaxRibbonPlugins)
                {
                    MessageBox.Show("This test build supports up to 30 ribbon plugins.", "BIMLab Manager",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                PluginConfigStore.AddPlugin(_dynxPath.Text, _panelName.Text, _buttonName.Text);

                MessageBox.Show(
                    "Plugin added.\n\nRestart Revit to load the new ribbon button.",
                    "BIMLab Manager",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "BIMLab Manager",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
