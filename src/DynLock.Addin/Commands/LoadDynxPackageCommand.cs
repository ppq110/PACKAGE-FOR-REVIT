using System;
using System.IO;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DynLock.Addin.Auth;
using DynLock.Addin.DynamicPlugins;
using DynLock.Addin.UI;
using DynLock.Core;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;

namespace DynLock.Addin.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class LoadDynxPackageCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (!AddinSession.IsLoggedIn && !LoginDialog.ShowLogin())
                return Result.Cancelled;

            using (var dlg = new OpenFileDialog
            {
                Filter = "BIMLab plugin package (*.dynx)|*.dynx",
                Title = "Load BIMLab plugin package",
                Multiselect = false,
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return Result.Cancelled;

                try
                {
                    if (!Secrets.TryGetMasterKeyBytes(out byte[] masterKey, out string keyError))
                    {
                        TaskDialog.Show("BIMLab Player", keyError);
                        message = keyError;
                        return Result.Failed;
                    }

                    byte[] plain = DynxCrypto.Decrypt(File.ReadAllBytes(dlg.FileName), masterKey);
                    var package = DynxPackage.ReadPlain(plain);

                    var plugin = PluginConfigStore.AddLoadedPackage(dlg.FileName, package);
                    bool ribbonAdded = RibbonRuntime.TryAddPlugin(plugin);

                    using (var preview = new LoadedPluginPreviewForm(plugin))
                    {
                        preview.Text = ribbonAdded
                            ? "BIMLab Player - Plugin loaded"
                            : "BIMLab Player - Plugin loaded, ribbon pending";
                        preview.ShowDialog();
                    }

                    return Result.Succeeded;
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                    TaskDialog.Show("BIMLab Player", "Could not load .dynx file:\n" + ex.Message);
                    return Result.Failed;
                }
            }
        }
    }
}
