using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using DynLock.Addin.DynamicPlugins;
using DynLock.Addin.UI;

namespace DynLock.Addin
{
    public class App : IExternalApplication
    {
        private const string TabName = "BIMLab";

        public Result OnStartup(UIControlledApplication application)
        {
            try { application.CreateRibbonTab(TabName); }
            catch { }

            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            RibbonRuntime.Initialize(application, assemblyPath);

            RibbonPanel managerPanel = GetOrCreatePanel(application, TabName, "Manager");
            AddLoginButton(managerPanel, assemblyPath);
            AddLoadButton(managerPanel, assemblyPath);
            RibbonRuntime.LoadPersistedPlugins();

            return Result.Succeeded;
        }

        private static void AddLoginButton(RibbonPanel panel, string assemblyPath)
        {
            var login = new PushButtonData(
                "DynLockLogin",
                "Login",
                assemblyPath,
                "DynLock.Addin.Commands.LoginCommand")
            {
                ToolTip = "Đăng nhập BIMLab",
                LongDescription = "Đăng nhập bằng Gmail trước khi sử dụng công cụ BIMLab.",
            };
            login.LargeImage = BitmapToWpf(AddinIcons.LoginIcon(32));
            login.Image = BitmapToWpf(AddinIcons.LoginIcon(16));
            TryAddItem(panel, login);
        }

        private static void AddLoadButton(RibbonPanel panel, string assemblyPath)
        {
            var load = new PushButtonData(
                "DynLockLoadPlugin",
                "Load",
                assemblyPath,
                "DynLock.Addin.Commands.LoadDynxPackageCommand")
            {
                ToolTip = "Load plugin .dynx từ leader",
                LongDescription = "Đăng nhập rồi load file .dynx do leader gửi. File .dynx chứa graph, tên mục, tên plugin và icon.",
            };
            load.LargeImage = BitmapToWpf(AddinIcons.AddIcon(32));
            load.Image = BitmapToWpf(AddinIcons.AddIcon(16));
            TryAddItem(panel, load);
        }

        private static RibbonPanel GetOrCreatePanel(UIControlledApplication application, string tabName, string panelName)
        {
            RibbonPanel existing = application
                .GetRibbonPanels(tabName)
                .FirstOrDefault(p => string.Equals(p.Name, panelName, StringComparison.OrdinalIgnoreCase));
            return existing ?? application.CreateRibbonPanel(tabName, panelName);
        }

        private static PushButton TryAddItem(RibbonPanel panel, PushButtonData data)
        {
            try { return panel.AddItem(data) as PushButton; }
            catch { return null; }
        }

        internal static BitmapImage BitmapToWpf(System.Drawing.Bitmap bmp)
        {
            if (bmp == null) return null;
            using (bmp)
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                var img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.StreamSource = ms;
                img.EndInit();
                img.Freeze();
                return img;
            }
        }

        public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;
    }
}
