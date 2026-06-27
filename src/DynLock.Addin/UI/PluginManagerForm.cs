using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.UI;
using DynLock.Addin.Commands;
using DynLock.Addin.DynamicPlugins;

namespace DynLock.Addin.UI
{
    public class PluginManagerForm : Form
    {
        private readonly UIApplication _uiApp;
        private readonly ListView _list = new ListView();
        private readonly Dictionary<string, string> _pendingIconById = new Dictionary<string, string>();

        public PluginManagerForm(UIApplication uiApp)
        {
            _uiApp = uiApp;

            Text = "BIMLab Manager - Plugins";
            Width = 820;
            Height = 460;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);

            var header = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Color.FromArgb(18, 90, 175) };
            header.Controls.Add(new Label
            {
                Text = "Plugin settings",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(16, 18),
            });

            _list.Dock = DockStyle.Fill;
            _list.View = View.Details;
            _list.FullRowSelect = true;
            _list.GridLines = true;
            _list.Columns.Add("Panel", 150);
            _list.Columns.Add("Plugin", 170);
            _list.Columns.Add("Dynx", 300);
            _list.Columns.Add("Icon", 150);
            _list.DoubleClick += (_, __) => RunSelected();

            var bottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                Padding = new Padding(12, 8, 12, 8),
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.FromArgb(244, 246, 251),
            };

            var close = new Button { Text = "Close", DialogResult = DialogResult.Cancel };
            var save = new Button { Text = "Save" };
            var run = new Button { Text = "Run" };
            var remove = new Button { Text = "Remove" };
            var icon = new Button { Text = "Change icon" };
            var refresh = new Button { Text = "Refresh" };

            StyleButton(close, Color.FromArgb(218, 226, 242), Color.FromArgb(52, 72, 108), 88, 36);
            StyleButton(save, Color.FromArgb(34, 126, 68), Color.White, 88, 36);
            StyleButton(run, Color.FromArgb(30, 120, 215), Color.White, 88, 36);
            StyleButton(remove, Color.FromArgb(218, 226, 242), Color.FromArgb(52, 72, 108), 96, 36);
            StyleButton(icon, Color.FromArgb(218, 226, 242), Color.FromArgb(52, 72, 108), 118, 36);
            StyleButton(refresh, Color.FromArgb(218, 226, 242), Color.FromArgb(52, 72, 108), 88, 36);

            refresh.Click += (_, __) => LoadPlugins();
            icon.Click += (_, __) => PickIcon();
            save.Click += (_, __) => SaveChanges();
            remove.Click += (_, __) => RemoveSelected();
            run.Click += (_, __) => RunSelected();

            bottom.Controls.Add(close);
            bottom.Controls.Add(save);
            bottom.Controls.Add(run);
            bottom.Controls.Add(remove);
            bottom.Controls.Add(icon);
            bottom.Controls.Add(refresh);

            Controls.Add(_list);
            Controls.Add(header);
            Controls.Add(bottom);

            Load += (_, __) => LoadPlugins();
            CancelButton = close;
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

        private void LoadPlugins()
        {
            _list.Items.Clear();

            foreach (var plugin in PluginConfigStore.Load().Plugins.OrderBy(p => p.CreatedAt))
            {
                var item = new ListViewItem(plugin.PanelName ?? "");
                item.SubItems.Add(plugin.ButtonName ?? "");
                item.SubItems.Add(plugin.DynxPath ?? "");
                item.SubItems.Add(IconLabel(plugin));
                item.Tag = plugin.Id;
                _list.Items.Add(item);
            }
        }

        private string IconLabel(PluginItem plugin)
        {
            if (plugin == null)
                return "";

            if (_pendingIconById.TryGetValue(plugin.Id, out string pending))
                return Path.GetFileName(pending) + " (pending)";

            return string.IsNullOrWhiteSpace(plugin.IconPath)
                ? "(question mark)"
                : Path.GetFileName(plugin.IconPath);
        }

        private PluginItem SelectedPlugin()
        {
            if (_list.SelectedItems.Count == 0)
            {
                MessageBox.Show("Select a plugin first.", "BIMLab Manager",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            string id = _list.SelectedItems[0].Tag as string;
            return PluginConfigStore.Load().Plugins.FirstOrDefault(p => p.Id == id);
        }

        private void PickIcon()
        {
            var plugin = SelectedPlugin();
            if (plugin == null)
                return;

            using (var dlg = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.ico)|*.png;*.jpg;*.jpeg;*.bmp;*.ico",
                Title = "Choose plugin icon",
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                _pendingIconById[plugin.Id] = dlg.FileName;
                LoadPlugins();
                SelectPlugin(plugin.Id);
            }
        }

        private void SaveChanges()
        {
            if (_pendingIconById.Count == 0)
            {
                MessageBox.Show("No changes to save.", "BIMLab Manager",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var config = PluginConfigStore.Load();
            foreach (var pair in _pendingIconById.ToList())
            {
                var plugin = config.Plugins.FirstOrDefault(p => p.Id == pair.Key);
                if (plugin == null)
                    continue;

                plugin.IconPath = PluginConfigStore.CopyIcon(pair.Value);
                UpdateRibbonIconIfLoaded(plugin);
            }

            PluginConfigStore.Save(config);
            _pendingIconById.Clear();
            LoadPlugins();

            MessageBox.Show("Saved. Loaded ribbon buttons were updated immediately.",
                "BIMLab Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateRibbonIconIfLoaded(PluginItem plugin)
        {
            if (plugin == null)
                return;

            if (!DynamicRibbonRegistry.TryGetButton(plugin.Id, out PushButton button) || button == null)
                return;

            button.LargeImage = App.BitmapToWpf(AddinIcons.PluginIcon(plugin.IconPath, 32, plugin.ButtonName));
            button.Image = App.BitmapToWpf(AddinIcons.PluginIcon(plugin.IconPath, 16, plugin.ButtonName));
        }

        private void RunSelected()
        {
            var plugin = SelectedPlugin();
            if (plugin == null)
                return;

            string message = null;
            Hide();
            try
            {
                RunDynxCommand.RunDynx(_uiApp, plugin.DynxPath, ref message);
            }
            finally
            {
                if (!IsDisposed)
                {
                    Show();
                    BringToFront();
                    Activate();
                }
            }
        }

        private void RemoveSelected()
        {
            var plugin = SelectedPlugin();
            if (plugin == null)
                return;

            if (MessageBox.Show("Remove selected plugin?", "BIMLab Manager",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            var config = PluginConfigStore.Load();
            config.Plugins = config.Plugins.Where(p => p.Id != plugin.Id).ToList();
            PluginConfigStore.Save(config);
            _pendingIconById.Remove(plugin.Id);
            bool hidden = DynamicRibbonRegistry.HideButton(plugin.Id);
            LoadPlugins();

            MessageBox.Show(hidden
                    ? "Plugin removed from Manager and hidden from the ribbon."
                    : "Plugin removed from Manager. If its ribbon button was not loaded in this session, it will stay removed on the next Revit start.",
                "BIMLab Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SelectPlugin(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return;

            foreach (ListViewItem item in _list.Items)
            {
                if (string.Equals(item.Tag as string, id, StringComparison.OrdinalIgnoreCase))
                {
                    item.Selected = true;
                    item.Focused = true;
                    item.EnsureVisible();
                    return;
                }
            }
        }
    }
}
