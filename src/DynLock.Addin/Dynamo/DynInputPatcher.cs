using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DynLock.Addin.Dynamo
{
    public enum InputKind
    {
        Text,
        Number,
        Bool,
        CategoryDropdown,
        LevelDropdown,
        FamilyTypeDropdown,
        GenericDropdown,
        FilePath,
        DirectoryPath,
        ElementSelection,
        ElementsSelection,
        Unsupported,
    }

    public class DynInputItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public InputKind Kind { get; set; }
        public JObject Node { get; set; }
        public JObject Metadata { get; set; }
        public int Order { get; set; }
        public double ViewX { get; set; } = double.MaxValue;
        public double ViewY { get; set; } = double.MaxValue;

        public bool IsSelection =>
            Kind == InputKind.ElementSelection || Kind == InputKind.ElementsSelection;

        public string CurrentText =>
            (string)(Node?["InputValue"] ?? Node?["SelectedString"] ?? Metadata?["Value"] ?? "");

        public void SetValue(string value)
        {
            if (Node == null) return;

            if (Node["SelectedString"] != null && Node["InputValue"] == null)
                SetDropdown(value);
            else
                Node["InputValue"] = value;
        }

        public void SetNumber(double value)
        {
            if (Node != null)
                Node["InputValue"] = value;
        }

        public void SetBool(bool value)
        {
            if (Node != null)
                Node["InputValue"] = value;
        }

        public void SetDropdown(string selectedString)
        {
            if (Node == null) return;
            Node["SelectedString"] = selectedString;
            Node["SelectedIndex"] = 0;
        }

        public void SetSelection(IEnumerable<string> uniqueIds)
        {
            if (Node == null) return;
            Node["InstanceId"] = new JArray(uniqueIds);
        }
    }

    /// <summary>
    /// Reads the input list from a Dynamo graph JSON and lets us override
    /// values before running headless.
    /// </summary>
    public class DynInputPatcher
    {
        public JObject Graph { get; }
        public string GraphName => (string)Graph["Name"] ?? "Dynamo Tool";
        public List<DynInputItem> Inputs { get; } = new List<DynInputItem>();

        public DynInputPatcher(JObject graph)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));

            var nodesById = (graph["Nodes"] as JArray ?? new JArray())
                .OfType<JObject>()
                .Where(n => n["Id"] != null)
                .ToDictionary(n => (string)n["Id"], n => n, StringComparer.OrdinalIgnoreCase);

            var nodeViewsById = (graph["View"]?["NodeViews"] as JArray ?? new JArray())
                .OfType<JObject>()
                .Where(v => v["Id"] != null)
                .ToDictionary(v => (string)v["Id"], v => v, StringComparer.OrdinalIgnoreCase);

            var knownInputIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            bool hasExplicitInputs = false;
            foreach (var input in (graph["Inputs"] as JArray ?? new JArray()).OfType<JObject>())
            {
                string id = (string)input["Id"];
                if (string.IsNullOrWhiteSpace(id) || !nodesById.TryGetValue(id, out JObject node))
                    continue;

                AddInput(id, node, input, nodeViewsById, knownInputIds, Inputs.Count);
                hasExplicitInputs = true;
            }

            // Dynamo Player treats graph["Inputs"] as the source of truth when present.
            // Only fall back to scanning nodes for older graphs that do not have this list.
            if (hasExplicitInputs)
            {
                SortInputsLikeDynamoPlayer();
                return;
            }

            foreach (var pair in nodesById)
            {
                if (knownInputIds.Contains(pair.Key))
                    continue;

                JObject node = pair.Value;
                if (!LooksLikeStandaloneInputNode(node))
                    continue;

                nodeViewsById.TryGetValue(pair.Key, out JObject nodeView);

                string name = FirstNonEmpty(
                    (string)nodeView?["Name"],
                    (string)node["Name"],
                    (string)node["Nickname"],
                    "Input");

                var kind = Classify(
                    (string)node["ConcreteType"] ?? "",
                    "",
                    "",
                    (string)node["NodeType"] ?? "",
                    node);

                if (kind == InputKind.Unsupported)
                    kind = InferKindFromStoredValue(node);

                Inputs.Add(new DynInputItem
                {
                    Id = pair.Key,
                    Name = name,
                    Node = node,
                    Metadata = nodeView,
                    Kind = kind,
                    Order = Inputs.Count,
                    ViewX = ReadDouble(nodeView?["X"]),
                    ViewY = ReadDouble(nodeView?["Y"]),
                });
                knownInputIds.Add(pair.Key);
            }

            SortInputsLikeDynamoPlayer();
        }

        /// <summary>
        /// Normalize graph before headless run.
        /// </summary>
        public List<string> PrepareForHeadlessRun()
        {
            ForceAutomaticRun();
            NormalizePythonTransactions();
            return StripOrphanPackageNodes();
        }

        private void NormalizePythonTransactions()
        {
            foreach (var node in (Graph["Nodes"] as JArray ?? new JArray()).OfType<JObject>())
            {
                string concreteType = (string)node["ConcreteType"] ?? "";
                string nodeType = (string)node["NodeType"] ?? "";
                if (!ContainsIgnoreCase(concreteType, "PythonNode") &&
                    !ContainsIgnoreCase(nodeType, "Python"))
                    continue;

                string code = (string)node["Code"];
                if (string.IsNullOrWhiteSpace(code) ||
                    code.IndexOf("# BIMLab transaction guard", StringComparison.Ordinal) >= 0)
                    continue;

                const string start = "TransactionManager.Instance.EnsureInTransaction(doc)";
                if (code.Contains(start))
                {
                    code = code.Replace(start,
                        "# BIMLab transaction guard\r\n" +
                        "try:\r\n" +
                        "    TransactionManager.Instance.ForceCloseTransaction()\r\n" +
                        "except:\r\n" +
                        "    pass\r\n" +
                        "TransactionManager.Instance.EnsureInTransaction(doc)");
                }

                const string done = "TransactionManager.Instance.TransactionTaskDone()";
                if (code.Contains(done))
                {
                    code = code.Replace(done,
                        "try:\r\n" +
                        "    TransactionManager.Instance.TransactionTaskDone()\r\n" +
                        "except:\r\n" +
                        "    pass");
                }

                node["Code"] = code;
            }
        }

        private void AddInput(
            string id,
            JObject node,
            JObject metadata,
            IReadOnlyDictionary<string, JObject> nodeViewsById,
            HashSet<string> knownInputIds,
            int order)
        {
            if (knownInputIds.Contains(id))
                return;

            nodeViewsById.TryGetValue(id, out JObject nodeView);

            string name = FirstNonEmpty(
                (string)metadata?["Name"],
                (string)nodeView?["Name"],
                (string)node["Name"],
                (string)node["Nickname"],
                "Input");

            var kind = Classify(
                (string)node["ConcreteType"] ?? "",
                (string)metadata?["Type"] ?? "",
                (string)metadata?["Type2"] ?? "",
                (string)node["NodeType"] ?? "",
                node);

            if (kind == InputKind.Unsupported)
                kind = InferKindFromStoredValue(node);

            Inputs.Add(new DynInputItem
            {
                Id = id,
                Name = name,
                Node = node,
                Metadata = metadata ?? nodeView,
                Kind = kind,
                Order = order,
                ViewX = ReadDouble(nodeView?["X"]),
                ViewY = ReadDouble(nodeView?["Y"]),
            });
            knownInputIds.Add(id);
        }

        private void SortInputsLikeDynamoPlayer()
        {
            var sorted = Inputs
                .OrderBy(i => i.ViewY)
                .ThenBy(i => i.ViewX)
                .ThenBy(i => i.Order)
                .ToList();

            Inputs.Clear();
            Inputs.AddRange(sorted);
        }

        private void ForceAutomaticRun()
        {
            if (!(Graph["View"] is JObject view))
            {
                view = new JObject();
                Graph["View"] = view;
            }

            if (!(view["Dynamo"] is JObject dynamoView))
            {
                dynamoView = new JObject();
                view["Dynamo"] = dynamoView;
            }

            dynamoView["RunType"] = "Automatic";
            dynamoView["HasRunWithoutCrash"] = true;
        }

        private List<string> StripOrphanPackageNodes()
        {
            var stillRequired = new List<string>();
            if (!(Graph["NodeLibraryDependencies"] is JArray deps) || deps.Count == 0)
                return stillRequired;

            var wiredPorts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in (Graph["Connectors"] as JArray ?? new JArray()).OfType<JObject>())
            {
                string start = (string)c["Start"];
                string end = (string)c["End"];
                if (start != null) wiredPorts.Add(start);
                if (end != null) wiredPorts.Add(end);
            }

            var nodes = Graph["Nodes"] as JArray ?? new JArray();
            var nodeViews = Graph["View"]?["NodeViews"] as JArray;

            foreach (var dep in deps.OfType<JObject>().ToList())
            {
                if (!string.Equals((string)dep["ReferenceType"], "Package", StringComparison.OrdinalIgnoreCase))
                    continue;

                var idList = dep["Nodes"] as JArray ?? new JArray();
                foreach (var idToken in idList.ToList())
                {
                    string id = (string)idToken;
                    var node = nodes.OfType<JObject>().FirstOrDefault(n => string.Equals((string)n["Id"], id, StringComparison.OrdinalIgnoreCase));
                    if (node == null)
                    {
                        idToken.Remove();
                        continue;
                    }

                    bool wired = (node["Inputs"] as JArray ?? new JArray())
                        .Concat(node["Outputs"] as JArray ?? new JArray())
                        .OfType<JObject>()
                        .Any(p => p["Id"] != null && wiredPorts.Contains((string)p["Id"]));
                    if (wired) continue;

                    node.Remove();
                    nodeViews?.OfType<JObject>().FirstOrDefault(v => string.Equals((string)v["Id"], id, StringComparison.OrdinalIgnoreCase))?.Remove();
                    RemoveById(Graph["Inputs"] as JArray, id);
                    RemoveById(Graph["Outputs"] as JArray, id);
                    Inputs.RemoveAll(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));
                    idToken.Remove();
                }

                if (idList.Count == 0)
                    dep.Remove();
                else
                    stillRequired.Add((((string)dep["Name"]) ?? "?") + " " + (((string)dep["Version"]) ?? ""));
            }

            return stillRequired;
        }

        private static void RemoveById(JArray array, string id)
        {
            if (array == null) return;
            foreach (var item in array.OfType<JObject>().Where(o => string.Equals((string)o["Id"], id, StringComparison.OrdinalIgnoreCase)).ToList())
                item.Remove();
        }

        private static bool LooksLikeStandaloneInputNode(JObject node)
        {
            if (node == null) return false;

            string concreteType = (string)node["ConcreteType"] ?? "";
            string nodeType = (string)node["NodeType"] ?? "";

            if (IsTruthy(node["IsSetAsInput"]))
                return true;

            if (ContainsIgnoreCase(concreteType, "CoreNodeModels.Input."))
                return true;
            if (ContainsIgnoreCase(concreteType, "DSRevitNodesUI.Categories") ||
                ContainsIgnoreCase(concreteType, "DSRevitNodesUI.Levels") ||
                ContainsIgnoreCase(concreteType, "DSRevitNodesUI.FamilyTypes") ||
                ContainsIgnoreCase(concreteType, "DSModelElementSelection") ||
                ContainsIgnoreCase(concreteType, "DSModelElementsSelection"))
                return true;

            if (nodeType.EndsWith("InputNode", StringComparison.OrdinalIgnoreCase))
                return true;
            if (nodeType.Equals("StringInputNode", StringComparison.OrdinalIgnoreCase) ||
                nodeType.Equals("BoolInputNode", StringComparison.OrdinalIgnoreCase) ||
                nodeType.Equals("BooleanInputNode", StringComparison.OrdinalIgnoreCase) ||
                nodeType.Equals("NumberInputNode", StringComparison.OrdinalIgnoreCase) ||
                nodeType.Equals("IntegerInputNode", StringComparison.OrdinalIgnoreCase) ||
                nodeType.Equals("DoubleInputNode", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static InputKind Classify(
            string concreteType,
            string inputType,
            string inputType2,
            string nodeType,
            JObject node)
        {
            concreteType = concreteType ?? "";
            inputType = inputType ?? "";
            inputType2 = inputType2 ?? "";
            nodeType = nodeType ?? "";

            if (ContainsIgnoreCase(concreteType, "CoreNodeModels.Input.StringInput") ||
                nodeType.Equals("StringInputNode", StringComparison.OrdinalIgnoreCase))
                return InputKind.Text;

            if (ContainsIgnoreCase(concreteType, "CoreNodeModels.Input.DoubleInput") ||
                ContainsIgnoreCase(concreteType, "CoreNodeModels.Input.IntegerInput") ||
                ContainsIgnoreCase(concreteType, "CoreNodeModels.Input.IntegerSlider") ||
                ContainsIgnoreCase(concreteType, "CoreNodeModels.Input.DoubleSlider") ||
                nodeType.Equals("DoubleInputNode", StringComparison.OrdinalIgnoreCase) ||
                nodeType.Equals("IntegerInputNode", StringComparison.OrdinalIgnoreCase) ||
                nodeType.Equals("NumberInputNode", StringComparison.OrdinalIgnoreCase))
                return InputKind.Number;

            if (ContainsIgnoreCase(concreteType, "CoreNodeModels.Input.BoolSelector") ||
                nodeType.Equals("BooleanInputNode", StringComparison.OrdinalIgnoreCase) ||
                nodeType.Equals("BoolInputNode", StringComparison.OrdinalIgnoreCase))
                return InputKind.Bool;

            if (ContainsIgnoreCase(concreteType, "DSRevitNodesUI.Categories"))
                return InputKind.CategoryDropdown;

            if (ContainsIgnoreCase(concreteType, "DSRevitNodesUI.Levels"))
                return InputKind.LevelDropdown;

            if (ContainsIgnoreCase(concreteType, "DSRevitNodesUI.FamilyTypes"))
                return InputKind.FamilyTypeDropdown;

            if (ContainsIgnoreCase(concreteType, "DSModelElementsSelection"))
                return InputKind.ElementsSelection;

            if (ContainsIgnoreCase(concreteType, "DSModelElementSelection"))
                return InputKind.ElementSelection;

            if (ContainsIgnoreCase(concreteType, "FilePath") ||
                inputType.Equals("filePath", StringComparison.OrdinalIgnoreCase) ||
                inputType2.Equals("filePath", StringComparison.OrdinalIgnoreCase))
                return InputKind.FilePath;

            if (ContainsIgnoreCase(concreteType, "DirectoryPath") ||
                inputType.Equals("directoryPath", StringComparison.OrdinalIgnoreCase) ||
                inputType2.Equals("directoryPath", StringComparison.OrdinalIgnoreCase))
                return InputKind.DirectoryPath;

            if (inputType.Equals("string", StringComparison.OrdinalIgnoreCase) ||
                inputType2.Equals("string", StringComparison.OrdinalIgnoreCase) ||
                inputType.Equals("text", StringComparison.OrdinalIgnoreCase))
                return InputKind.Text;

            if (inputType.Equals("number", StringComparison.OrdinalIgnoreCase) ||
                inputType2.Equals("number", StringComparison.OrdinalIgnoreCase) ||
                inputType2.Equals("numberInput", StringComparison.OrdinalIgnoreCase) ||
                inputType.Equals("integer", StringComparison.OrdinalIgnoreCase))
                return InputKind.Number;

            if (inputType.Equals("boolean", StringComparison.OrdinalIgnoreCase) ||
                inputType2.Equals("boolean", StringComparison.OrdinalIgnoreCase))
                return InputKind.Bool;

            if (inputType2.Equals("hostSelection", StringComparison.OrdinalIgnoreCase))
                return InputKind.ElementsSelection;

            if (inputType.Equals("selection", StringComparison.OrdinalIgnoreCase) ||
                inputType2.Equals("dropdownSelection", StringComparison.OrdinalIgnoreCase) ||
                inputType.Equals("dropdown", StringComparison.OrdinalIgnoreCase))
                return InputKind.GenericDropdown;

            if (node != null && node["SelectedString"] != null)
                return InputKind.GenericDropdown;

            return InputKind.Unsupported;
        }

        private static InputKind InferKindFromStoredValue(JObject node)
        {
            if (node == null)
                return InputKind.Unsupported;

            if (node["SelectedString"] != null)
                return InputKind.GenericDropdown;

            var value = node["InputValue"];
            if (value == null)
                return InputKind.Unsupported;

            if (value.Type == JTokenType.Boolean)
                return InputKind.Bool;
            if (value.Type == JTokenType.Integer || value.Type == JTokenType.Float)
                return InputKind.Number;
            if (value.Type == JTokenType.String)
                return InputKind.Text;

            return InputKind.Unsupported;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null) return null;
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
            return null;
        }

        private static bool IsTruthy(JToken token)
        {
            if (token == null) return false;
            if (token.Type == JTokenType.Boolean) return token.Value<bool>();
            if (token.Type == JTokenType.String)
                return string.Equals(token.Value<string>(), "true", StringComparison.OrdinalIgnoreCase);
            return false;
        }

        private static double ReadDouble(JToken token)
        {
            if (token == null)
                return double.MaxValue;

            if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer)
                return token.Value<double>();

            if (double.TryParse((string)token, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double value))
                return value;

            return double.MaxValue;
        }

        private static bool ContainsIgnoreCase(string source, string value)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
                return false;
            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
