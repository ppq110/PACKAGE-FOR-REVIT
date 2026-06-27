using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.UI;

namespace DynLock.Addin.DynamicPlugins
{
    internal static class DynamicRibbonRegistry
    {
        private static readonly Dictionary<string, PushButton> ButtonsByPluginId =
            new Dictionary<string, PushButton>();
        private static readonly Dictionary<int, string> PluginIdBySlot =
            new Dictionary<int, string>();

        public static void Register(int slot, string pluginId, PushButton button)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
                return;

            PluginIdBySlot[slot] = pluginId;
            if (button != null)
                ButtonsByPluginId[pluginId] = button;
        }

        public static string PluginIdForSlot(int slot)
        {
            return PluginIdBySlot.TryGetValue(slot, out string pluginId) ? pluginId : null;
        }

        public static bool TryGetButton(string pluginId, out PushButton button)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
            {
                button = null;
                return false;
            }

            return ButtonsByPluginId.TryGetValue(pluginId, out button);
        }

        public static bool HideButton(string pluginId)
        {
            if (!TryGetButton(pluginId, out PushButton button) || button == null)
                return false;

            button.Visible = false;
            button.Enabled = false;
            return true;
        }

        public static int NextAvailableSlot(int maxSlots = 30)
        {
            for (int slot = 1; slot <= maxSlots; slot++)
            {
                if (!PluginIdBySlot.ContainsKey(slot))
                    return slot;
            }

            return 0;
        }

        public static int CountLoadedPlugins()
        {
            return PluginIdBySlot.Values
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .Count();
        }
    }
}
