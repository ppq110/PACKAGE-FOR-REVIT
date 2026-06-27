using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DynLock.Addin.Auth;
using DynLock.Addin.UI;

namespace DynLock.Addin.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class AddPluginCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (!AddinSession.IsLoggedIn && !LoginDialog.ShowLogin())
                return Result.Cancelled;

            using (var form = new AddPluginForm())
            {
                form.ShowDialog();
            }

            return Result.Succeeded;
        }
    }
}
