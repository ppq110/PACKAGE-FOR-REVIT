using System;
using System.IO;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace DynLock.Addin.Commands
{
    public abstract class FixedDynxCommandBase : IExternalCommand
    {
        protected abstract string ToolName { get; }
        protected abstract string[] CandidateFileNames { get; }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            string dynxPath = FindDynx();
            if (string.IsNullOrWhiteSpace(dynxPath))
            {
                TaskDialog.Show("BIMLab Player",
                    "Chưa tìm thấy file .dynx cho công cụ: " + ToolName + "\n\n" +
                    "Leader cần gửi/copy file .dynx tương ứng vào thư mục BIMLab Scripts:\n" +
                    ScriptLocator.DefaultScriptsDir);
                return Result.Cancelled;
            }

            return RunDynxCommand.RunDynx(commandData.Application, dynxPath, ref message);
        }

        private string FindDynx()
        {
            var scripts = ScriptLocator.FindScripts();
            foreach (string candidate in CandidateFileNames)
            {
                string found = scripts.FirstOrDefault(path =>
                    string.Equals(Path.GetFileName(path), candidate, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(found))
                    return found;
            }

            foreach (string candidate in CandidateFileNames)
            {
                string candidateName = Path.GetFileNameWithoutExtension(candidate);
                string found = scripts.FirstOrDefault(path =>
                    string.Equals(Path.GetFileNameWithoutExtension(path), candidateName, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(found))
                    return found;
            }

            return null;
        }
    }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class AutoJoinCommand : FixedDynxCommandBase
    {
        protected override string ToolName => "Auto Join";

        protected override string[] CandidateFileNames => new[]
        {
            "BLB_AUTO JOIN ALL.dynx",
            "Auto Join.dynx",
            "JOIN ALL.dynx",
            "Join All.dynx",
        };
    }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CreatePileCommand : FixedDynxCommandBase
    {
        protected override string ToolName => "Create Pile";

        protected override string[] CandidateFileNames => new[]
        {
            "FILE DYNAMO GUI TEAM IOT.dynx",
            "Create Pile.dynx",
            "Pile Modeling.dynx",
        };
    }
}
