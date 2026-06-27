using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace DynLock.Addin
{
    /// <summary>
    /// Tìm các file .dynx mà nhân viên được phép chạy.
    /// Nguồn: (1) các thư mục khai báo trong %ProgramData%\BIMLab\DynLock\config.json
    ///        (2) thư mục "Scripts" nằm cạnh DLL add-in.
    /// config.json: { "ScriptFolders": [ "\\\\server\\share\\dynx", "D:\\Tools" ] }
    /// </summary>
    public static class ScriptLocator
    {
        public static string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "BIMLab", "DynLock", "config.json");

        public static string DefaultScriptsDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "BIMLab", "DynLock", "Scripts");

        public static List<string> FindScripts()
        {
            var folders = new List<string>();

            folders.Add(DefaultScriptsDir);

            string addinDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            folders.Add(Path.Combine(addinDir, "Scripts"));

            try
            {
                if (File.Exists(ConfigPath))
                {
                    var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigPath));
                    if (config?.ScriptFolders != null)
                        folders.AddRange(config.ScriptFolders);
                }
            }
            catch (Exception) { /* config hỏng -> bỏ qua, vẫn còn nút Browse */ }

            return folders
                .Where(Directory.Exists)
                .SelectMany(f =>
                {
                    try { return Directory.GetFiles(f, "*.dynx"); }
                    catch (Exception) { return Array.Empty<string>(); }
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private class Config
        {
            public List<string> ScriptFolders { get; set; }
        }
    }
}
