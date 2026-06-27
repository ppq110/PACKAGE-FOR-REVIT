using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DynLock.Addin.Auth;
using DynLock.Addin.Dynamo;
using DynLock.Addin.UI;
using DynLock.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;
using TaskDialogResult = Autodesk.Revit.UI.TaskDialogResult;

namespace DynLock.Addin.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class RunDynxCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            string dynxPath = ScriptPickerForm.PickScript();
            if (string.IsNullOrWhiteSpace(dynxPath))
                return Result.Cancelled;

            return RunDynx(commandData.Application, dynxPath, ref message);
        }

        public static Result RunDynx(UIApplication uiApp, string dynxPath, ref string message)
        {
            if (uiApp.ActiveUIDocument?.Document == null)
            {
                TaskDialog.Show("BIMLab Player", "Open a Revit model before running this tool.");
                return Result.Cancelled;
            }

            if (!AddinSession.IsLoggedIn)
            {
                bool loggedIn = LoginDialog.ShowLogin();
                if (!loggedIn)
                    return Result.Cancelled;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(dynxPath) || !File.Exists(dynxPath))
                {
                    TaskDialog.Show("BIMLab Player", "Cannot find the assigned .dynx file.");
                    return Result.Cancelled;
                }

                if (!Secrets.TryGetMasterKeyBytes(out byte[] masterKey, out string keyError))
                {
                    message = keyError;
                    TaskDialog.Show("BIMLab Player", keyError);
                    return Result.Failed;
                }

                byte[] plain = DynxCrypto.Decrypt(File.ReadAllBytes(dynxPath), masterKey);
                var package = DynxPackage.ReadPlain(plain);
                var graph = JObject.Parse(package.GraphJson);

                var patcher = new DynInputPatcher(graph);

                List<string> requiredPackages = patcher.PrepareForHeadlessRun();
                if (!ConfirmMissingPackages(requiredPackages))
                    return Result.Cancelled;

                if (patcher.Inputs.Count > 0)
                {
                    using (var form = new InputForm(uiApp, patcher))
                    {
                        if (form.ShowDialog() != DialogResult.OK)
                            return Result.Cancelled;
                    }
                }

                string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".dyn");
                DynamoRunReport report;
                try
                {
                    File.WriteAllText(tempPath, graph.ToString(Formatting.None), Encoding.UTF8);
                    report = DynamoHeadlessRunner.Run(uiApp, tempPath);
                }
                finally
                {
                    TryDelete(tempPath);
                }

                return ShowRunReport(patcher.GraphName, report, ref message);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("BIMLab Player", "Error:\n" + ex.Message);
                return Result.Failed;
            }
        }

        private static bool ConfirmMissingPackages(List<string> requiredPackages)
        {
            if (requiredPackages == null || requiredPackages.Count == 0)
                return true;

            var missing = requiredPackages
                .Where(p => !IsPackageInstalled(NormalizePackageName(p)))
                .ToList();
            if (missing.Count == 0)
                return true;

            var td = new TaskDialog("BIMLab - Missing Dynamo packages")
            {
                MainInstruction = "This tool needs Dynamo packages that are not installed:",
                MainContent =
                    "  -  " + string.Join("\n  -  ", missing) +
                    "\n\nInstall them from Revit -> Manage -> Dynamo -> Packages -> Search for a Package." +
                    "\n\nTry running anyway?",
                CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No,
                DefaultButton = TaskDialogResult.No,
            };
            return td.Show() == TaskDialogResult.Yes;
        }

        private static bool IsPackageInstalled(string packageName)
        {
            try
            {
                string root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Dynamo", "Dynamo Revit");
                if (!Directory.Exists(root))
                    return false;

                return Directory.GetDirectories(root)
                    .Select(ver => Path.Combine(ver, "packages", packageName))
                    .Any(Directory.Exists);
            }
            catch
            {
                return true;
            }
        }

        private static string NormalizePackageName(string packageDisplayName)
        {
            if (string.IsNullOrWhiteSpace(packageDisplayName))
                return packageDisplayName;

            string trimmed = packageDisplayName.Trim();
            int lastSpace = trimmed.LastIndexOf(' ');
            if (lastSpace <= 0)
                return trimmed;

            string trailing = trimmed.Substring(lastSpace + 1);
            if (LooksLikeVersionToken(trailing))
                return trimmed.Substring(0, lastSpace).Trim();

            return trimmed;
        }

        private static bool LooksLikeVersionToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            bool hasDigit = false;
            foreach (char ch in token)
            {
                if (char.IsDigit(ch))
                {
                    hasDigit = true;
                    continue;
                }

                if (ch == '.' || ch == '-' || ch == '_')
                    continue;

                return false;
            }

            return hasDigit;
        }

        private static Result ShowRunReport(string graphName, DynamoRunReport report, ref string message)
        {
            if (!report.Evaluated)
            {
                message = "Graph was not evaluated.";
                TaskDialog.Show("BIMLab Player",
                    "Tool \"" + graphName + "\" opened but did not execute.\n\n" +
                    "Please ask the team lead to check the original graph and save it in Automatic mode.");
                return Result.Failed;
            }

            var lines = new List<string>();
            bool hasRealIssues = report.UnresolvedNodes.Count > 0 || report.Issues.Count > 0;
            if (report.EngineReportedFailure && hasRealIssues)
                lines.Add("Dynamo reported warnings/errors after execution. If the model result is correct, you can continue using it.");
            if (report.UnresolvedNodes.Count > 0)
                lines.Add("Unresolved nodes, possibly missing packages:\n  -  " +
                          string.Join("\n  -  ", report.UnresolvedNodes.Distinct().Take(5)));
            if (report.Issues.Count > 0)
                lines.Add("Dynamo warnings:\n  -  " +
                          string.Join("\n  -  ", report.Issues.Take(8)));

            if (lines.Count == 0)
            {
                TaskDialog.Show("BIMLab Player", "Finished: " + graphName);
                return Result.Succeeded;
            }

            TaskDialog.Show("BIMLab Player",
                "Finished: " + graphName + "\n\n" +
                string.Join("\n\n", lines) +
                "\n\nIf the model result is wrong, send this screen to the team lead.");
            return Result.Succeeded;
        }

        private static void TryDelete(string path)
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    if (File.Exists(path))
                        File.Delete(path);
                    return;
                }
                catch (IOException)
                {
                    System.Threading.Thread.Sleep(200);
                }
            }
        }
    }
}
