using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DynLock.Addin.Auth;
using DynLock.Addin.UI;

namespace DynLock.Addin.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    [Regeneration(RegenerationOption.Manual)]
    public class LoginCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (AddinSession.IsLoggedIn)
            {
                var td = new TaskDialog("BIMLab Player - Tài khoản")
                {
                    MainInstruction = "Đang đăng nhập: " + AddinSession.FullName,
                    MainContent     = AddinSession.Email + "\n\nBấm Đăng xuất để đổi tài khoản.",
                    CommonButtons   = TaskDialogCommonButtons.Close,
                };
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Đăng xuất khỏi BIMLab");
                if (td.Show() == TaskDialogResult.CommandLink1)
                    AddinSession.Clear();

                return Result.Succeeded;
            }

            LoginDialog.ShowLogin();
            return Result.Succeeded;
        }
    }
}
