using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DynLock.Addin.DynamicPlugins;

namespace DynLock.Addin.Commands
{
    public abstract class DynamicPluginCommandBase : IExternalCommand
    {
        protected abstract int Slot { get; }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            string pluginId = DynamicRibbonRegistry.PluginIdForSlot(Slot);
            var plugin = PluginConfigStore.GetById(pluginId);
            if (plugin == null)
            {
                TaskDialog.Show("BIMLab Player", "This plugin was removed or is no longer available.");
                return Result.Cancelled;
            }

            return RunDynxCommand.RunDynx(commandData.Application, plugin.DynxPath, ref message);
        }
    }

    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand01 : DynamicPluginCommandBase { protected override int Slot => 1; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand02 : DynamicPluginCommandBase { protected override int Slot => 2; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand03 : DynamicPluginCommandBase { protected override int Slot => 3; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand04 : DynamicPluginCommandBase { protected override int Slot => 4; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand05 : DynamicPluginCommandBase { protected override int Slot => 5; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand06 : DynamicPluginCommandBase { protected override int Slot => 6; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand07 : DynamicPluginCommandBase { protected override int Slot => 7; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand08 : DynamicPluginCommandBase { protected override int Slot => 8; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand09 : DynamicPluginCommandBase { protected override int Slot => 9; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand10 : DynamicPluginCommandBase { protected override int Slot => 10; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand11 : DynamicPluginCommandBase { protected override int Slot => 11; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand12 : DynamicPluginCommandBase { protected override int Slot => 12; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand13 : DynamicPluginCommandBase { protected override int Slot => 13; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand14 : DynamicPluginCommandBase { protected override int Slot => 14; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand15 : DynamicPluginCommandBase { protected override int Slot => 15; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand16 : DynamicPluginCommandBase { protected override int Slot => 16; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand17 : DynamicPluginCommandBase { protected override int Slot => 17; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand18 : DynamicPluginCommandBase { protected override int Slot => 18; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand19 : DynamicPluginCommandBase { protected override int Slot => 19; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand20 : DynamicPluginCommandBase { protected override int Slot => 20; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand21 : DynamicPluginCommandBase { protected override int Slot => 21; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand22 : DynamicPluginCommandBase { protected override int Slot => 22; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand23 : DynamicPluginCommandBase { protected override int Slot => 23; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand24 : DynamicPluginCommandBase { protected override int Slot => 24; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand25 : DynamicPluginCommandBase { protected override int Slot => 25; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand26 : DynamicPluginCommandBase { protected override int Slot => 26; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand27 : DynamicPluginCommandBase { protected override int Slot => 27; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand28 : DynamicPluginCommandBase { protected override int Slot => 28; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand29 : DynamicPluginCommandBase { protected override int Slot => 29; }
    [Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)] public class DynamicPluginCommand30 : DynamicPluginCommandBase { protected override int Slot => 30; }
}
