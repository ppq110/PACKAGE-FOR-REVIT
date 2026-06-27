using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.UI;

namespace DynLock.Addin.Dynamo
{
    /// <summary>Kết quả chạy headless: graph có thực thi không, node nào lỗi/cảnh báo.</summary>
    public sealed class DynamoRunReport
    {
        /// <summary>false = graph được mở nhưng KHÔNG hề evaluate (thường do RunType Manual).</summary>
        public bool Evaluated { get; set; } = true;

        /// <summary>Node không nạp được (thiếu package / sai phiên bản Dynamo).</summary>
        public List<string> UnresolvedNodes { get; } = new List<string>();

        /// <summary>Cảnh báo / lỗi của từng node sau khi chạy ("Tên node: thông điệp").</summary>
        public List<string> Issues { get; } = new List<string>();

        /// <summary>Dynamo/Revit trả Result.Failed nhưng graph vẫn có thể đã tạo kết quả trong model.</summary>
        public bool EngineReportedFailure { get; set; }
    }

    /// <summary>
    /// Chạy một file .dyn bằng engine Dynamo for Revit ở chế độ ngầm
    /// (không mở cửa sổ Dynamo, tự Run, tự đóng model sau khi chạy).
    /// Nạp DynamoRevitDS.dll bằng reflection để không phụ thuộc
    /// phiên bản Dynamo lúc biên dịch.
    /// LƯU Ý: trong chế độ automation, Dynamo bỏ qua dynPathExecute -
    /// graph PHẢI ở RunType Automatic (DynInputPatcher.PrepareForHeadlessRun lo việc này).
    /// </summary>
    public static class DynamoHeadlessRunner
    {
        public static DynamoRunReport Run(UIApplication uiApplication, string dynPath)
        {
            string dsPath = LocateDynamoRevitDs();
            Assembly asm = Assembly.LoadFrom(dsPath);

            Type dynamoRevitType = asm.GetType("Dynamo.Applications.DynamoRevit", throwOnError: true);
            Type commandDataType = asm.GetType("Dynamo.Applications.DynamoRevitCommandData", throwOnError: true);

            object commandData = Activator.CreateInstance(commandDataType);
            commandDataType.GetProperty("Application").SetValue(commandData, uiApplication);

            var journal = new Dictionary<string, string>
            {
                ["dynShowUI"] = "false",          // không hiện cửa sổ Dynamo
                ["dynAutomation"] = "true",       // chạy đồng bộ, chặn mở UI
                ["dynPath"] = dynPath,
                ["dynPathExecute"] = "true",      // (bị bỏ qua khi automation - giữ cho tương thích)
                ["dynForceManualRun"] = "false",
                ["dynModelShutDown"] = "true",    // lần chạy sau sẽ tạo model Dynamo mới
            };
            commandDataType.GetProperty("JournalData").SetValue(commandData, journal);

            object dynamoRevit = Activator.CreateInstance(dynamoRevitType);
            MethodInfo execute = dynamoRevitType.GetMethod("ExecuteCommand", new[] { commandDataType })
                ?? throw new MissingMethodException("DynamoRevit.ExecuteCommand không tồn tại - phiên bản Dynamo không tương thích.");

            bool engineReportedFailure = false;
            try
            {
                object result = execute.Invoke(dynamoRevit, new[] { commandData });
                if (result is Result revitResult && revitResult == Result.Failed)
                    engineReportedFailure = true;
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                throw new InvalidOperationException(
                    "Lỗi khi chạy script Dynamo: " + tie.InnerException.Message, tie.InnerException);
            }

            // Automation mode chạy đồng bộ -> tới đây graph đã evaluate xong,
            // model vẫn còn sống nên đọc được trạng thái từng node.
            var report = CollectReport(dynamoRevitType);
            report.EngineReportedFailure = engineReportedFailure;
            return report;
        }

        private static DynamoRunReport CollectReport(Type dynamoRevitType)
        {
            var report = new DynamoRunReport();
            object model = null;
            try
            {
                model = dynamoRevitType
                    .GetProperty("RevitDynamoModel", BindingFlags.Public | BindingFlags.Static)
                    ?.GetValue(null);
                if (model == null) return report;

                object workspace = model.GetType().GetProperty("CurrentWorkspace")?.GetValue(model);
                if (workspace == null) return report;

                if (workspace.GetType().GetProperty("EvaluationCount")?.GetValue(workspace) is int evalCount)
                    report.Evaluated = evalCount > 0;

                if (workspace.GetType().GetProperty("Nodes")?.GetValue(workspace)
                    is System.Collections.IEnumerable nodes)
                {
                    foreach (object node in nodes)
                    {
                        Type t = node.GetType();
                        string name = t.GetProperty("Name")?.GetValue(node) as string ?? t.Name;

                        if (t.Name == "DummyNode")
                        {
                            report.UnresolvedNodes.Add(name);
                            continue;
                        }

                        string state = t.GetProperty("State")?.GetValue(node)?.ToString() ?? "";
                        if (state == "Warning" || state == "PersistentWarning" ||
                            state == "Error" || state == "AstBuildBroken")
                        {
                            string detail = GetNodeMessages(node);
                            if (IsBenignTransactionWarning(name, detail))
                                continue;

                            report.Issues.Add(string.IsNullOrWhiteSpace(detail)
                                ? name + " (" + state + ")"
                                : name + ": " + detail);
                        }
                    }
                }
            }
            catch
            {
                // chẩn đoán thất bại không được làm hỏng kết quả chạy
            }
            finally
            {
                ClearWorkspace(model); // xóa graph đã giải mã khỏi bộ nhớ Dynamo
            }
            return report;
        }

        private static string GetNodeMessages(object node)
        {
            try
            {
                if (node.GetType().GetProperty("NodeInfos")?.GetValue(node)
                    is System.Collections.IEnumerable infos)
                {
                    var messages = new List<string>();
                    foreach (object info in infos)
                    {
                        string m = (info.GetType().GetField("Message")?.GetValue(info)
                                 ?? info.GetType().GetProperty("Message")?.GetValue(info)) as string;
                        if (!string.IsNullOrWhiteSpace(m)) messages.Add(m.Trim());
                    }
                    if (messages.Count > 0)
                        return string.Join(" | ", messages.Distinct().Take(3));
                }
                return node.GetType().GetProperty("ToolTipText")?.GetValue(node) as string;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsBenignTransactionWarning(string nodeName, string detail)
        {
            if (string.IsNullOrWhiteSpace(detail))
                return false;

            string text = (nodeName + "\n" + detail).ToLowerInvariant();
            return text.Contains("python script") &&
                   text.Contains("cannot modify the document") &&
                   text.Contains("read-only external command") &&
                   text.Contains("transaction.start");
        }

        private static void ClearWorkspace(object model)
        {
            if (model == null) return;
            try
            {
                model.GetType()
                    .GetMethod("ClearCurrentWorkspace",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                        null, Type.EmptyTypes, null)
                    ?.Invoke(model, null);
            }
            catch
            {
                // không xóa được cũng không sao - model sẽ bị shutdown ở lần chạy sau
            }
        }

        private static string LocateDynamoRevitDs()
        {
            // RevitAPIUI.dll nằm ở thư mục gốc Revit, ví dụ C:\Program Files\Autodesk\Revit 2024
            string revitDir = Path.GetDirectoryName(typeof(UIApplication).Assembly.Location);

            var candidates = new[]
            {
                Path.Combine(revitDir, "AddIns", "DynamoForRevit", "Revit", "DynamoRevitDS.dll"),
                Path.Combine(revitDir, "AddIns", "DynamoForRevit", "DynamoRevitDS.dll"),
            };

            foreach (var path in candidates)
                if (File.Exists(path))
                    return path;

            throw new FileNotFoundException(
                "Không tìm thấy DynamoRevitDS.dll. Hãy kiểm tra Dynamo for Revit đã được cài " +
                "(mở Dynamo một lần từ tab Manage để chắc chắn). Đã tìm tại:\n" +
                string.Join("\n", candidates));
        }
    }
}
