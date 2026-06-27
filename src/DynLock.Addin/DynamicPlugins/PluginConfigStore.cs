using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace DynLock.Addin.DynamicPlugins
{
    internal sealed class PluginConfig
    {
        public List<PluginItem> Plugins { get; set; } = new List<PluginItem>();
    }

    internal sealed class PluginItem
    {
        public string Id { get; set; }
        public string PackageId { get; set; }
        public string PanelName { get; set; }
        public string ButtonName { get; set; }
        public string SourceFileName { get; set; }
        public string DynxPath { get; set; }
        public string IconPath { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    internal static class PluginConfigStore
    {
        public const int MaxRibbonPlugins = 30;

        private static readonly string BaseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "BIMLab", "DynLock");

        public static readonly string ConfigPath = Path.Combine(BaseDir, "plugins.json");
        public static readonly string ScriptsDir = Path.Combine(BaseDir, "Scripts");
        public static readonly string IconsDir = Path.Combine(BaseDir, "PluginIcons");

        public static PluginConfig Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                    return new PluginConfig();

                var config = JsonConvert.DeserializeObject<PluginConfig>(File.ReadAllText(ConfigPath));
                if (config?.Plugins == null)
                    return new PluginConfig();

                config.Plugins = config.Plugins
                    .Where(IsValid)
                    .OrderBy(p => p.CreatedAt)
                    .ToList();

                return config;
            }
            catch
            {
                return new PluginConfig();
            }
        }

        public static void Save(PluginConfig config)
        {
            Directory.CreateDirectory(BaseDir);
            Directory.CreateDirectory(ScriptsDir);
            Directory.CreateDirectory(IconsDir);

            string json = JsonConvert.SerializeObject(config ?? new PluginConfig(), Formatting.Indented);
            File.WriteAllText(ConfigPath, json);
        }

        public static string AddPlugin(string sourceDynxPath, string panelName, string buttonName)
        {
            if (string.IsNullOrWhiteSpace(sourceDynxPath) || !File.Exists(sourceDynxPath))
                throw new FileNotFoundException("Cannot find dynx file.", sourceDynxPath);

            panelName = CleanName(panelName, "Script Engine");
            buttonName = CleanName(buttonName, Path.GetFileNameWithoutExtension(sourceDynxPath));

            Directory.CreateDirectory(ScriptsDir);

            string copiedDynx = UniquePath(ScriptsDir, Path.GetFileName(sourceDynxPath));
            File.Copy(sourceDynxPath, copiedDynx, false);

            var config = Load();
            config.Plugins.Add(new PluginItem
            {
                Id = Guid.NewGuid().ToString("N"),
                PackageId = Guid.NewGuid().ToString("N"),
                PanelName = panelName,
                ButtonName = buttonName,
                SourceFileName = Path.GetFileName(sourceDynxPath),
                DynxPath = copiedDynx,
                CreatedAt = DateTime.UtcNow,
            });
            Save(config);

            return copiedDynx;
        }

        public static PluginItem AddLoadedPackage(string sourceDynxPath, DynLock.Core.DynxPackageData packageData)
        {
            if (packageData == null)
                throw new ArgumentNullException(nameof(packageData));

            if (string.IsNullOrWhiteSpace(sourceDynxPath) || !File.Exists(sourceDynxPath))
                throw new FileNotFoundException("Cannot find dynx file.", sourceDynxPath);

            Directory.CreateDirectory(ScriptsDir);

            string packageId = CleanName(packageData.PackageId, Guid.NewGuid().ToString("N"));
            var config = Load();
            var existing = config.Plugins.FirstOrDefault(p =>
                string.Equals(p.PackageId, packageId, StringComparison.OrdinalIgnoreCase));

            string copiedDynx = UniquePath(ScriptsDir, Path.GetFileName(sourceDynxPath));
            File.Copy(sourceDynxPath, copiedDynx, false);

            string iconPath = WriteIcon(packageData.Plugin);

            if (existing != null)
            {
                existing.PanelName = CleanName(packageData.Plugin?.PanelName, existing.PanelName ?? "Utilities");
                existing.ButtonName = CleanName(packageData.Plugin?.PluginName, existing.ButtonName ?? Path.GetFileNameWithoutExtension(sourceDynxPath));
                existing.SourceFileName = Path.GetFileName(sourceDynxPath);
                existing.DynxPath = copiedDynx;
                existing.IconPath = iconPath ?? existing.IconPath;
                existing.PackageId = packageId;
                Save(config);
                return existing;
            }

            var item = new PluginItem
            {
                Id = Guid.NewGuid().ToString("N"),
                PackageId = packageId,
                PanelName = CleanName(packageData.Plugin?.PanelName, "Utilities"),
                ButtonName = CleanName(packageData.Plugin?.PluginName, Path.GetFileNameWithoutExtension(sourceDynxPath)),
                SourceFileName = Path.GetFileName(sourceDynxPath),
                DynxPath = copiedDynx,
                IconPath = iconPath,
                CreatedAt = DateTime.UtcNow,
            };

            config.Plugins.Add(item);
            Save(config);
            return item;
        }

        public static List<PluginItem> OrderedPlugins()
        {
            return Load().Plugins
                .Where(IsValid)
                .OrderBy(p => p.CreatedAt)
                .Take(MaxRibbonPlugins)
                .ToList();
        }

        public static PluginItem GetBySlot(int slot)
        {
            if (slot < 1 || slot > MaxRibbonPlugins)
                return null;

            return OrderedPlugins().Skip(slot - 1).FirstOrDefault();
        }

        public static PluginItem GetById(string pluginId)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
                return null;

            return Load().Plugins.FirstOrDefault(p =>
                string.Equals(p.Id, pluginId, StringComparison.OrdinalIgnoreCase));
        }

        public static PluginItem GetByPackageId(string packageId)
        {
            if (string.IsNullOrWhiteSpace(packageId))
                return null;

            return Load().Plugins.FirstOrDefault(p =>
                string.Equals(p.PackageId, packageId, StringComparison.OrdinalIgnoreCase));
        }

        public static string CopyIcon(string sourceIconPath)
        {
            if (string.IsNullOrWhiteSpace(sourceIconPath) || !File.Exists(sourceIconPath))
                return null;

            Directory.CreateDirectory(IconsDir);
            string copiedIcon = UniquePath(IconsDir, Path.GetFileName(sourceIconPath));
            File.Copy(sourceIconPath, copiedIcon, false);
            return copiedIcon;
        }

        private static bool IsValid(PluginItem item)
        {
            return item != null &&
                   !string.IsNullOrWhiteSpace(item.PanelName) &&
                   !string.IsNullOrWhiteSpace(item.ButtonName) &&
                   !string.IsNullOrWhiteSpace(item.DynxPath) &&
                   File.Exists(item.DynxPath);
        }

        private static string CleanName(string value, string fallback)
        {
            value = (value ?? "").Trim();
            if (string.IsNullOrWhiteSpace(value))
                value = fallback ?? "Plugin";

            foreach (char c in Path.GetInvalidFileNameChars())
                value = value.Replace(c.ToString(), "");

            return string.IsNullOrWhiteSpace(value) ? "Plugin" : value;
        }

        private static string UniquePath(string dir, string fileName)
        {
            string target = Path.Combine(dir, fileName);
            if (!File.Exists(target))
                return target;

            string name = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);
            int i = 2;
            do
            {
                target = Path.Combine(dir, name + " (" + i + ")" + ext);
                i++;
            }
            while (File.Exists(target));

            return target;
        }

        private static string WriteIcon(DynLock.Core.DynxPluginInfo pluginInfo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pluginInfo?.IconBase64))
                    return null;

                Directory.CreateDirectory(IconsDir);
                string fileName = string.IsNullOrWhiteSpace(pluginInfo.IconFileName)
                    ? "plugin.png"
                    : pluginInfo.IconFileName;
                string target = UniquePath(IconsDir, fileName);
                File.WriteAllBytes(target, Convert.FromBase64String(pluginInfo.IconBase64));
                return target;
            }
            catch
            {
                return null;
            }
        }
    }
}
