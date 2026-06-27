using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.UI;
using DynLock.Addin.UI;

namespace DynLock.Addin.DynamicPlugins
{
    internal static class RibbonRuntime
    {
        private const string TabName = "BIMLab";

        private static UIControlledApplication _application;
        private static string _assemblyPath;
        private static readonly Dictionary<string, RibbonPanel> PanelsByName =
            new Dictionary<string, RibbonPanel>(StringComparer.OrdinalIgnoreCase);

        public static void Initialize(UIControlledApplication application, string assemblyPath)
        {
            _application = application;
            _assemblyPath = assemblyPath;
            PanelsByName.Clear();
        }

        public static void LoadPersistedPlugins()
        {
            if (_application == null || string.IsNullOrWhiteSpace(_assemblyPath))
                return;

            foreach (var plugin in PluginConfigStore.OrderedPlugins())
                TryAddPlugin(plugin);
        }

        public static bool TryAddPlugin(PluginItem plugin)
        {
            if (_application == null || string.IsNullOrWhiteSpace(_assemblyPath) || plugin == null)
                return false;

            if (DynamicRibbonRegistry.TryGetButton(plugin.Id, out _))
                return true;

            int slot = DynamicRibbonRegistry.NextAvailableSlot(PluginConfigStore.MaxRibbonPlugins);
            if (slot == 0)
                return false;

            RibbonPanel panel = GetOrCreatePanel(plugin.PanelName);
            if (panel == null)
                return false;

            string className = "DynLock.Addin.Commands.DynamicPluginCommand" + slot.ToString("00");
            var data = new PushButtonData(
                "DynLockLoadedPlugin" + slot.ToString("00"),
                plugin.ButtonName,
                _assemblyPath,
                className)
            {
                ToolTip = "Chạy " + plugin.ButtonName,
                LongDescription = BuildDescription(plugin),
            };

            data.LargeImage = App.BitmapToWpf(AddinIcons.PluginIcon(plugin.IconPath, 32, plugin.ButtonName));
            data.Image = App.BitmapToWpf(AddinIcons.PluginIcon(plugin.IconPath, 16, plugin.ButtonName));

            PushButton button = TryAddItem(panel, data);
            if (button == null)
                return false;

            DynamicRibbonRegistry.Register(slot, plugin.Id, button);
            return true;
        }

        private static RibbonPanel GetOrCreatePanel(string panelName)
        {
            string normalized = NormalizePanelName(panelName);
            string key = PanelKey(normalized);

            if (PanelsByName.TryGetValue(key, out RibbonPanel cached) && cached != null)
                return cached;

            var existing = _application
                .GetRibbonPanels(TabName)
                .FirstOrDefault(p => string.Equals(PanelKey(p.Name), key, StringComparison.OrdinalIgnoreCase));

            RibbonPanel panel = existing ?? _application.CreateRibbonPanel(TabName, normalized);
            PanelsByName[key] = panel;
            return panel;
        }

        private static string NormalizePanelName(string panelName)
        {
            string value = string.IsNullOrWhiteSpace(panelName) ? "Utilities" : panelName.Trim();
            var parts = value
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            value = string.Join(" ", parts);
            return string.IsNullOrWhiteSpace(value) ? "Utilities" : value;
        }

        private static string PanelKey(string panelName)
        {
            return NormalizePanelName(panelName).ToUpperInvariant();
        }

        private static PushButton TryAddItem(RibbonPanel panel, PushButtonData data)
        {
            try
            {
                return panel.AddItem(data) as PushButton;
            }
            catch
            {
                return null;
            }
        }

        private static string BuildDescription(PluginItem plugin)
        {
            string filePart = string.IsNullOrWhiteSpace(plugin.DynxPath)
                ? "File .dynx đã được lưu trong BIMLab Scripts."
                : "File .dynx: " + plugin.DynxPath;

            return "Tự động tạo từ package .dynx của leader.\n" +
                   "Mục: " + plugin.PanelName + "\n" +
                   "Plugin: " + plugin.ButtonName + "\n" +
                   filePart;
        }
    }
}
