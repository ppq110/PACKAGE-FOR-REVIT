using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DynLock.Core
{
    public sealed class DynxPluginInfo
    {
        public string PanelName { get; set; }
        public string PluginName { get; set; }
        public string IconBase64 { get; set; }
        public string IconFileName { get; set; }
    }

    public sealed class DynxPackageData
    {
        public int BimLabDynxPackageVersion { get; set; } = 3;
        public string PackageId { get; set; }
        public string SourceFileName { get; set; }
        public DynxPluginInfo Plugin { get; set; } = new DynxPluginInfo();
        public string GraphJson { get; set; }
    }

    public static class DynxPackage
    {
        public const int CurrentVersion = 3;

        public static byte[] Create(
            string graphJson,
            string panelName,
            string pluginName,
            string iconPath,
            string sourceFileName = null,
            string packageId = null)
        {
            var package = new DynxPackageData
            {
                BimLabDynxPackageVersion = CurrentVersion,
                PackageId = Clean(packageId, Guid.NewGuid().ToString("N")),
                SourceFileName = Clean(sourceFileName, null),
                GraphJson = graphJson,
                Plugin = new DynxPluginInfo
                {
                    PanelName = Clean(panelName, "Utilities"),
                    PluginName = Clean(pluginName, "BIMLab Tool"),
                    IconBase64 = ReadIconBase64(iconPath),
                    IconFileName = string.IsNullOrWhiteSpace(iconPath) ? null : Path.GetFileName(iconPath),
                },
            };

            string json = JsonConvert.SerializeObject(package, Formatting.None);
            return Encoding.UTF8.GetBytes(json);
        }

        public static DynxPackageData ReadPlain(byte[] plain)
        {
            string text = Encoding.UTF8.GetString(plain ?? new byte[0]);
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("File .dynx không có dữ liệu.");

            JObject root = JObject.Parse(text);
            if (root["BimLabDynxPackageVersion"] == null)
            {
                return new DynxPackageData
                {
                    BimLabDynxPackageVersion = 1,
                    PackageId = null,
                    GraphJson = text,
                    Plugin = new DynxPluginInfo(),
                };
            }

            var package = root.ToObject<DynxPackageData>();
            if (package == null || string.IsNullOrWhiteSpace(package.GraphJson))
                throw new InvalidOperationException("Gói .dynx không hợp lệ.");

            if (package.Plugin == null)
                package.Plugin = new DynxPluginInfo();

            package.PackageId = Clean(package.PackageId, null);
            package.Plugin.PanelName = Clean(package.Plugin.PanelName, "Utilities");
            package.Plugin.PluginName = Clean(
                package.Plugin.PluginName,
                Clean(Path.GetFileNameWithoutExtension(package.SourceFileName), "BIMLab Tool"));
            return package;
        }

        private static string Clean(string value, string fallback)
        {
            value = (value ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            return fallback;
        }

        private static string ReadIconBase64(string iconPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(iconPath) || !File.Exists(iconPath))
                    return null;

                return Convert.ToBase64String(File.ReadAllBytes(iconPath));
            }
            catch
            {
                return null;
            }
        }
    }
}
