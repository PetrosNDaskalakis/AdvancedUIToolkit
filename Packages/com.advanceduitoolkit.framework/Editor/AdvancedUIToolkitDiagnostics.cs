using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdvancedUIToolkit.Editor
{
    /// <summary>
    /// Read-only verification of the serialized AutoBinder manifest and the current
    /// uGUI hierarchy. This never changes prefabs, scenes, names, or generated code.
    /// </summary>
    internal static class AdvancedUIToolkitDiagnostics
    {
        /// <summary>Defines the severity enum.</summary>
        internal enum Severity
        {
            /// <summary>Informational diagnostic.</summary>
            Info,
            /// <summary>Non-blocking issue.</summary>
            Warning,
            /// <summary>Invalid or missing binding requiring attention.</summary>
            Error
        }

        /// <summary>Stores the finding reference or configuration value.</summary>
        internal sealed class Finding
        {
            /// <summary>Stores the level reference or configuration value.</summary>
            public readonly Severity Level;
            /// <summary>Stores the location reference or configuration value.</summary>
            public readonly string Location;
            /// <summary>Stores the description reference or configuration value.</summary>
            public readonly string Description;
            /// <summary>Stores the context reference or configuration value.</summary>
            public readonly UnityEngine.Object Context;

            /// <summary>Finds ing.</summary>
            public Finding(Severity level, string location, string description, UnityEngine.Object context)
            {
                Level = level;
                Location = location;
                Description = description;
                Context = context;
            }
        }

        /// <summary>Stores the report reference or configuration value.</summary>
        internal sealed class Report
        {
            /// <summary>Stores the findings reference or configuration value.</summary>
            public readonly List<Finding> Findings = new List<Finding>();
            /// <summary>Stores the roots checked reference or configuration value.</summary>
            public int RootsChecked;
            /// <summary>Stores the bindings checked reference or configuration value.</summary>
            public int BindingsChecked;
            /// <summary>Stores the prefabs checked reference or configuration value.</summary>
            public int PrefabsChecked;
            /// <summary>Stores the scenes checked reference or configuration value.</summary>
            public int ScenesChecked;
            /// <summary>Gets error count.</summary>
            public int ErrorCount => Findings.Count(f => f.Level == Severity.Error);
            /// <summary>Gets warning count.</summary>
            public int WarningCount => Findings.Count(f => f.Level == Severity.Warning);
        }

        /// <summary>
        /// Checks prefab assets (including inactive children) and currently loaded scenes.
        /// Unopened scene files are NOT checked. Loading prefab assets is read-only.
        /// </summary>
        internal static Report Scan(bool prefabs, bool openScenes)
        {
            var report = new Report();
            var checkedRoots = new HashSet<UIBindingRoot>();

            if (prefabs)
            {
                var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(path => !string.IsNullOrEmpty(path))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(path => path, StringComparer.Ordinal);
                foreach (var path in paths)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null) continue;
                    report.PrefabsChecked++;
                    foreach (var root in prefab.GetComponentsInChildren<UIBindingRoot>(true))
                        if (checkedRoots.Add(root)) ValidateRoot(root, path, report);
                }
            }

            if (openScenes)
            {
                for (int i = 0; i < SceneManager.sceneCount; ++i)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;
                    report.ScenesChecked++;
                    string path = string.IsNullOrEmpty(scene.path) ? "<Unsaved Scene>" : scene.path;
                    foreach (var sceneRoot in scene.GetRootGameObjects())
                        foreach (var root in sceneRoot.GetComponentsInChildren<UIBindingRoot>(true))
                            if (checkedRoots.Add(root)) ValidateRoot(root, path, report);
                }
            }

            return report;
        }

        /// <summary>Validates root.</summary>
        internal static void ValidateRoot(UIBindingRoot root, string location, Report report)
        {
            if (root == null) return;
            report.RootsChecked++;
            string scope = location + " / " + HierarchyPath(root.transform);
            var entries = root.EditorGetEntries();
            if (PrefabUtility.IsPartOfPrefabInstance(root))
                Add(report, Severity.Info, scope,
                    "This is a prefab instance. Edit binding manifests in the source prefab's Prefab Mode.", root);

            if (entries == null || entries.Count == 0)
            {
                Add(report, Severity.Warning, scope,
                    "Binding root has an empty manifest. Scan and generate if this is a UI screen.", root);
            }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            var hierarchyKeys = new HashSet<string>(StringComparer.Ordinal);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var references = new HashSet<Component>();

            if (entries != null)
            foreach (var entry in entries)
            {
                report.BindingsChecked++;
                if (entry == null)
                {
                    Add(report, Severity.Error, scope, "Null manifest entry.", root);
                    continue;
                }
                // Check identity even when the referenced Unity component was deleted.
                // A broken reference must not hide duplicate keys or persistent IDs.
                if (!IsValidIdentifier(entry.Key))
                    Add(report, Severity.Error, scope,
                        "Invalid C# API key '" + entry.Key + "'.", root);
                if (!keys.Add(entry.Key ?? ""))
                    Add(report, Severity.Error, scope,
                        "Duplicate generated API key '" + entry.Key + "'.", root);
                if (string.IsNullOrEmpty(entry.StableId))
                    Add(report, Severity.Warning, scope,
                        "Binding '" + entry.Key + "' has no stable ID. Regenerate to migrate.", root);
                else if (!ids.Add(entry.StableId))
                    Add(report, Severity.Error, scope,
                        "Duplicate stable binding ID '" + entry.StableId + "'.", root);
                if (entry.Reference == null)
                {
                    Add(report, Severity.Error, scope,
                        "Missing/destroyed component for API key '" + entry.Key + "'.", root);
                    continue;
                }
                if (!UIBindingSafety.IsInScope(root, entry.Reference))
                    Add(report, Severity.Error, scope,
                        "Component for '" + entry.Key + "' is outside this binding root's scope.",
                        entry.Reference);
                if (!references.Add(entry.Reference))
                    Add(report, Severity.Error, scope,
                        "Same component appears more than once in this manifest.", entry.Reference);
                if (string.IsNullOrEmpty(entry.HierarchyKey))
                    Add(report, Severity.Warning, scope,
                        "Binding '" + entry.Key + "' has no saved hierarchy key. Regenerate.", entry.Reference);
                else if (!hierarchyKeys.Add(entry.HierarchyKey))
                    Add(report, Severity.Error, scope,
                        "Duplicate saved hierarchy key '" + entry.HierarchyKey + "'.", entry.Reference);

                if (!string.IsNullOrEmpty(entry.ComponentType) &&
                    !MatchesTypeName(entry.Reference, entry.ComponentType))
                    Add(report, Severity.Error, scope,
                        "Binding '" + entry.Key + "' expects " + entry.ComponentType +
                        " but points to " + entry.Reference.GetType().FullName + ".", entry.Reference);

                // A prefab wrapper can declare the binding name while its Image,
                // Button, etc. is on a child. Inspect the owning named ancestor,
                // not necessarily the component's own GameObject name.
                Transform bindingOwner = FindNamedBindingOwner(root, entry.Reference);
                if (bindingOwner == null)
                {
                    Add(report, Severity.Warning, scope,
                        "Binding '" + entry.Key + "' no longer has a matching named object " +
                        "or unique component in its scope.", entry.Reference);
                }
                else
                {
                    string ownerName = bindingOwner.name;
                    string currentKey = ownerName.Substring(ownerName.IndexOf('_') + 1);
                    if (!string.Equals(currentKey, entry.HierarchyKey, StringComparison.Ordinal))
                        Add(report, Severity.Warning, scope,
                            "Hierarchy name changed from '" + entry.HierarchyKey + "' to '" + currentKey +
                            "'. Scan in Binder to accept the change (public API can be preserved).",
                            entry.Reference);
                }
            }

            var currentKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (!IsInsideScope(root.transform, transform)) continue;
                string name = transform.name;
                int split = name.IndexOf('_');
                if (split <= 0) continue;
                Type expected;
                if (!AdvancedUIToolkitNaming.TypeByPrefix.TryGetValue(name.Substring(0, split), out expected)) continue;

                string currentKey = name.Substring(split + 1);
                if (!IsValidIdentifier(currentKey))
                    Add(report, Severity.Error, scope,
                        "Invalid hierarchy binding name '" + name + "'.", transform.gameObject);
                if (!currentKeys.Add(currentKey))
                    Add(report, Severity.Error, scope,
                        "Duplicate hierarchy binding name key '" + currentKey + "'.", transform.gameObject);

                Component component = AdvancedUIToolkitNaming.ResolveNamedComponent(
                    transform, expected, out string resolutionProblem);
                if (component == null)
                {
                    Add(report, Severity.Error, scope,
                        "'" + name + "' " + resolutionProblem, transform.gameObject);
                    continue;
                }
                if (!references.Contains(component))
                    Add(report, Severity.Warning, scope,
                        "New unbound UI component '" + name + "'. Scan and generate bindings.", component);
            }

            var nestedWarnings = new List<string>();
            UIBindingSafety.AppendNestedPrefabWarnings(root.gameObject, nestedWarnings);
            foreach (string warning in nestedWarnings)
                Add(report, Severity.Info, scope, warning, root);
        }

        /// <summary>
        /// Finds the closest named object that owns an already registered component.
        /// A wrapper's binding name can differ from the nested component's name.
        /// </summary>
        /// <param name="root">The manifest's owning UI root.</param>
        /// <param name="reference">The serialized component reference.</param>
        /// <returns>The closest valid named owner, or null if none exists.</returns>
        private static Transform FindNamedBindingOwner(UIBindingRoot root, Component reference)
        {
            if (root == null || reference == null) return null;
            for (Transform node = reference.transform; node != null; node = node.parent)
            {
                if (!IsInsideScope(root.transform, node)) break;
                string name = node.name;
                int split = name.IndexOf('_');
                if (split > 0 &&
                    AdvancedUIToolkitNaming.TypeByPrefix.TryGetValue(name.Substring(0, split),
                        out Type expected) &&
                    expected.IsInstanceOfType(reference) &&
                    AdvancedUIToolkitNaming.ResolveNamedComponent(node, expected, out _) == reference)
                    return node;
                if (node == root.transform) break;
            }
            return null;
        }

        /// <summary>Performs the matches type name operation.</summary>
        private static bool MatchesTypeName(Component component, string expectedFullName)
        {
            for (Type type = component.GetType(); type != null; type = type.BaseType)
                if (string.Equals(type.FullName, expectedFullName, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>Determines whether inside scope.</summary>
        private static bool IsInsideScope(Transform selectedRoot, Transform child)
        {
            if (child == null) return false;
            bool reachedRoot = false;
            for (var t = child; t != null; t = t.parent)
            {
                if (t == selectedRoot) { reachedRoot = true; break; }
                if (t.GetComponent<UIBindingRoot>() != null) return false;
            }
            return reachedRoot;
        }

        /// <summary>Performs the hierarchy path operation.</summary>
        private static string HierarchyPath(Transform transform)
        {
            var names = new List<string>();
            for (var current = transform; current != null; current = current.parent)
                names.Add(current.name);
            names.Reverse();
            return string.Join("/", names.ToArray());
        }

        /// <summary>Determines whether valid identifier.</summary>
        private static bool IsValidIdentifier(string key)
        {
            if (string.IsNullOrEmpty(key) || AdvancedUIToolkitNaming.IsReservedAccessorName(key)) return false;
            if (!(char.IsLetter(key[0]) || key[0] == '_')) return false;
            return key.All(ch => char.IsLetterOrDigit(ch) || ch == '_');
        }

        /// <summary>Performs the add operation.</summary>
        private static void Add(Report report, Severity severity, string scope,
            string description, UnityEngine.Object context)
        {
            report.Findings.Add(new Finding(severity, scope, description, context));
        }
    }

    /// <summary>Stores the editor window reference or configuration value.</summary>
    public sealed class AdvancedUIToolkitDiagnosticsWindow : EditorWindow
    {
        /// <summary>Stores the report reference or configuration value.</summary>
        private AdvancedUIToolkitDiagnostics.Report report;
        /// <summary>Stores the scroll reference or configuration value.</summary>
        private Vector2 scroll;
        /// <summary>Stores the prefabs reference or configuration value.</summary>
        private bool prefabs = true;
        /// <summary>Stores the open scenes reference or configuration value.</summary>
        private bool openScenes = true;
        /// <summary>Stores the show warnings reference or configuration value.</summary>
        private bool showWarnings = true;
        /// <summary>Stores the show info reference or configuration value.</summary>
        private bool showInfo;

        /// <summary>Opens value.</summary>
        public static void Open() => AdvancedUIToolkitWorkspaceWindow.OpenTab(AdvancedUIToolkitWorkspaceWindow.WorkspaceTab.Diagnostics);

        /// <summary>Draws project validation within the Advanced UI Toolkit workspace.</summary>
        internal void DrawWorkspacePage()
        {
            EditorGUILayout.LabelField("Advanced UI Toolkit / Binding Diagnostics", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Read-only checks of UIBindingRoot manifests and hierarchy naming. " +
                "Checks prefab assets and/or currently open scenes; unopened scenes aren't included.",
                MessageType.Info);

            prefabs = EditorGUILayout.ToggleLeft("Scan prefab assets under Assets", prefabs);
            openScenes = EditorGUILayout.ToggleLeft("Scan currently open scenes", openScenes);
            using (new EditorGUI.DisabledScope(!prefabs && !openScenes))
            {
                if (GUILayout.Button("Run Validation", GUILayout.Height(28)))
                {
                    report = AdvancedUIToolkitDiagnostics.Scan(prefabs, openScenes);
                    Repaint();
                }
            }

            if (report == null)
            {
                EditorGUILayout.HelpBox("Run validation to see results.", MessageType.None);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Roots: " + report.RootsChecked + "   Bindings: " + report.BindingsChecked +
                "   Prefabs: " + report.PrefabsChecked + "   Scenes: " + report.ScenesChecked,
                EditorStyles.boldLabel);
            var summaryType = report.ErrorCount > 0 ? MessageType.Error :
                report.WarningCount > 0 ? MessageType.Warning : MessageType.Info;
            EditorGUILayout.HelpBox(
                report.ErrorCount + " errors, " + report.WarningCount + " warnings. " +
                (report.ErrorCount == 0 && report.WarningCount == 0
                    ? "All checked binding roots are valid." : "Review findings below."), summaryType);

            EditorGUILayout.BeginHorizontal();
            showWarnings = EditorGUILayout.ToggleLeft("Warnings", showWarnings, GUILayout.Width(90));
            showInfo = EditorGUILayout.ToggleLeft("Info", showInfo, GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var finding in report.Findings)
            {
                if (finding.Level == AdvancedUIToolkitDiagnostics.Severity.Warning && !showWarnings) continue;
                if (finding.Level == AdvancedUIToolkitDiagnostics.Severity.Info && !showInfo) continue;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(finding.Location, EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox(finding.Description,
                    finding.Level == AdvancedUIToolkitDiagnostics.Severity.Error ? MessageType.Error :
                    finding.Level == AdvancedUIToolkitDiagnostics.Severity.Warning ? MessageType.Warning : MessageType.Info);
                if (finding.Context != null && GUILayout.Button("Locate", GUILayout.Width(72)))
                {
                    Selection.activeObject = finding.Context;
                    EditorGUIUtility.PingObject(finding.Context);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
