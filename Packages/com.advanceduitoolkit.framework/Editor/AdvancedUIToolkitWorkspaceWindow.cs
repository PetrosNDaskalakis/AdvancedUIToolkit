using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AdvancedUIToolkit.Editor
{
    /// <summary>
    /// A single, task-oriented Workspace for component discovery, validation and code generation.
    /// Advanced diagnostics and automation remain available without requiring separate tabs.
    /// </summary>
    public sealed class AdvancedUIToolkitWorkspaceWindow : EditorWindow
    {
        /// <summary>Legacy destinations used by existing menus and editor integrations.</summary>
        internal enum WorkspaceTab
        {
            /// <summary>The regular UI binding workflow.</summary>
            Bindings,
            /// <summary>Optional automatic or manual batch generation.</summary>
            Automation,
            /// <summary>Optional project-wide diagnostics.</summary>
            Diagnostics,
            /// <summary>Optional local performance measurements.</summary>
            Performance
        }

        /// <summary>The minimum width suitable for Inspector docking.</summary>
        private const float MinDockWidth = 320f;

        /// <summary>Current component-name search.</summary>
        [SerializeField] private string componentSearch = string.Empty;

        /// <summary>Whether the less frequently used tools are expanded.</summary>
        [SerializeField] private bool showAdvanced;

        /// <summary>Whether the template and generation settings are expanded.</summary>
        [SerializeField] private bool showGenerationSettings;

        /// <summary>Whether automation and batch controls are expanded.</summary>
        [SerializeField] private bool showAutomation;

        /// <summary>Whether the project-wide diagnostic report is expanded.</summary>
        [SerializeField] private bool showDiagnostics;

        /// <summary>Whether the performance tools are expanded.</summary>
        [SerializeField] private bool showPerformance;

        /// <summary>Whether the entire warning/error list is expanded.</summary>
        [SerializeField] private bool showAllIssues;

        /// <summary>Scrollable component preview position when a screen has many bindings.</summary>
        private Vector2 componentScroll;

        /// <summary>Scrollable position of the unified Binder page.</summary>
        private Vector2 pageScroll;

        /// <summary>Maximum number of component rows shown before internal scrolling.</summary>
        private const int VisibleComponentRows = 7;

        /// <summary>AutoBinder engine hosted within this Editor window.</summary>
        private UIAutoBinderWindow binder;

        /// <summary>Existing advanced automation page.</summary>
        private AdvancedUIToolkitAutomationWindow automation;

        /// <summary>Existing advanced diagnostic page.</summary>
        private AdvancedUIToolkitDiagnosticsWindow diagnostics;

        /// <summary>Existing read-only benchmark page.</summary>
        private AdvancedUIToolkitPerformancePage performance;

        /// <summary>Short feedback after a user action.</summary>
        private string actionMessage;

        /// <summary>Opens the primary Advanced UI Toolkit Workspace.</summary>
        [MenuItem("Tools/Advanced UI Toolkit/Binder", false, 1)]
        public static void Open() => OpenTab(WorkspaceTab.Bindings);

        /// <summary>
        /// Preserves calls from older tools, but opens the unified Workspace with the
        /// requested advanced tool visible rather than presenting separate top-level tabs.
        /// </summary>
        /// <param name="tab">The legacy destination requested by another Editor tool.</param>
        internal static void OpenTab(WorkspaceTab tab)
        {
            AdvancedUIToolkitWorkspaceWindow window = GetWindow<AdvancedUIToolkitWorkspaceWindow>("Binder");
            window.ExpandRequestedTool(tab);
            window.Show();
            window.Focus();
        }

        /// <summary>Opens the Workspace for a specific saved binding root.</summary>
        /// <param name="root">The root to review.</param>
        internal static void OpenFor(UIBindingRoot root)
        {
            AdvancedUIToolkitWorkspaceWindow window = GetWindow<AdvancedUIToolkitWorkspaceWindow>("Binder");
            window.EnsurePages();
            window.binder.SetWorkspaceTarget(root != null ? root.gameObject : null);
            window.actionMessage = null;
            window.Show();
            window.Focus();
        }

        /// <summary>Opens the unified Workspace for an arbitrary selected UI root.</summary>
        /// <param name="root">The UI root to display.</param>
        internal static void OpenForRoot(GameObject root)
        {
            AdvancedUIToolkitWorkspaceWindow window = GetWindow<AdvancedUIToolkitWorkspaceWindow>("Binder");
            window.EnsurePages();
            window.binder.SetWorkspaceTarget(root);
            window.Show();
            window.Focus();
        }

        /// <summary>Initializes Editor listeners and docking constraints.</summary>
        private void OnEnable()
        {
            minSize = new Vector2(MinDockWidth, 360f);
            titleContent = new GUIContent("Binder", EditorGUIUtility.IconContent("d_Prefab Icon").image);
            AdvancedUIToolkitAutomationMonitor.Changed += Repaint;
        }

        /// <summary>Releases embedded pages and their transient state.</summary>
        private void OnDisable()
        {
            AdvancedUIToolkitAutomationMonitor.Changed -= Repaint;
            if (binder != null) DestroyImmediate(binder);
            if (automation != null) DestroyImmediate(automation);
            if (diagnostics != null) DestroyImmediate(diagnostics);
            binder = null;
            automation = null;
            diagnostics = null;
            performance = null;
        }

        /// <summary>Refreshes the component listing when the selected hierarchy changes.</summary>
        private void OnHierarchyChange()
        {
            if (binder != null) binder.WorkspaceHierarchyChanged();
            Repaint();
        }

        /// <summary>
        /// Updates the selected root without losing the current screen when a normal child
        /// is selected. Nested roots, however, are independent and must become active.
        /// </summary>
        private void OnSelectionChange()
        {
            if (binder == null) return;
            GameObject current = binder.WorkspaceTarget;
            GameObject target = ResolveSelectionTarget(current, Selection.activeGameObject);
            if (target != current)
            {
                binder.SetWorkspaceTarget(target);
                actionMessage = null;
            }
            Repaint();
        }

        /// <summary>
        /// Chooses which UI root should be displayed after a Hierarchy selection change.
        /// Keeps the current root for ordinary descendants and non-UI selections but
        /// switches to the closest independent nested UIBindingRoot when selected.
        /// </summary>
        /// <param name="current">Root currently displayed by the Workspace.</param>
        /// <param name="selected">Newly selected object in Unity's Hierarchy.</param>
        /// <returns>The active UI root; null when none is available.</returns>
        internal static GameObject ResolveSelectionTarget(GameObject current, GameObject selected)
        {
            if (selected == null || EditorUtility.IsPersistent(selected)) return current;

            GameObject resolved = ResolveRoot(selected);
            if (current != null && (selected == current ||
                selected.transform.IsChildOf(current.transform)))
            {
                if (resolved != null && resolved != current &&
                    resolved.transform.IsChildOf(current.transform) &&
                    resolved.GetComponent<UIBindingRoot>() != null)
                    return resolved;

                return current;
            }

            return resolved ?? current;
        }

        /// <summary>
        /// Finds the nearest existing binding root, or uses the selected RectTransform as
        /// a candidate for a first-time binding manifest.
        /// </summary>
        /// <param name="selected">The current Hierarchy selection.</param>
        /// <returns>The root to inspect, or null when the selection is not UI.</returns>
        internal static GameObject ResolveRoot(GameObject selected)
        {
            if (selected == null || EditorUtility.IsPersistent(selected)) return null;
            UIBindingRoot existing = selected.GetComponentInParent<UIBindingRoot>();
            if (existing != null) return existing.gameObject;
            return selected.GetComponent<RectTransform>() != null ? selected : null;
        }

        /// <summary>Creates the existing Editor-only implementation pages when needed.</summary>
        private void EnsurePages()
        {
            if (binder == null)
            {
                binder = CreateInstance<UIAutoBinderWindow>();
                binder.SetWorkspaceTarget(ResolveRoot(Selection.activeGameObject));
            }
            if (automation == null) automation = CreateInstance<AdvancedUIToolkitAutomationWindow>();
            if (diagnostics == null) diagnostics = CreateInstance<AdvancedUIToolkitDiagnosticsWindow>();
            if (performance == null) performance = new AdvancedUIToolkitPerformancePage();
        }

        /// <summary>Opens the corresponding advanced foldout when navigating from older tools.</summary>
        /// <param name="tab">The previously separate page to reveal.</param>
        private void ExpandRequestedTool(WorkspaceTab tab)
        {
            if (tab == WorkspaceTab.Bindings) return;
            showAdvanced = true;
            if (tab == WorkspaceTab.Automation) showAutomation = true;
            if (tab == WorkspaceTab.Diagnostics) showDiagnostics = true;
            if (tab == WorkspaceTab.Performance) showPerformance = true;
        }

        /// <summary>
        /// Draws an economical, top-aligned workflow. The whole page scrolls when
        /// necessary, rather than stretching the component list to fill the dock.
        /// </summary>
        private void OnGUI()
        {
            EnsurePages();
            DrawToolbar();
            DrawRootSelector();
            pageScroll = EditorGUILayout.BeginScrollView(pageScroll, GUILayout.ExpandHeight(true));
            if (binder.WorkspaceTarget != null)
            {
                DrawBindingSummary();
                DrawIssues();
                DrawComponentList();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Select a Canvas panel or UI root in the Hierarchy to begin.",
                    MessageType.Info);
            }
            DrawActions();
            if (showAdvanced) DrawAdvanced();
            EditorGUILayout.EndScrollView();
        }

        /// <summary>Draws only the primary navigation and a path to rarely used settings.</summary>
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Binder", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Factory", EditorStyles.toolbarButton, GUILayout.Width(64f)))
                    OpenFactoryForRoot();
                if (GUILayout.Button(showAdvanced ? "Show Less" : "Show More",
                    EditorStyles.toolbarButton, GUILayout.Width(91f)))
                    showAdvanced = !showAdvanced;
            }
        }

        /// <summary>Lets developers pick their UI root or use the current Hierarchy selection.</summary>
        private void DrawRootSelector()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("UI Root", GUILayout.Width(52f));
                GameObject previous = binder.WorkspaceTarget;
                GameObject selected = (GameObject)EditorGUILayout.ObjectField(
                    previous, typeof(GameObject), true, GUILayout.MinWidth(80f));
                if (selected != previous)
                {
                    binder.SetWorkspaceTarget(selected);
                    actionMessage = null;
                }
                if (GUILayout.Button(new GUIContent("Use Selected", "Use the closest UI binding root in the Hierarchy."),
                    GUILayout.Width(92f)))
                {
                    binder.SetWorkspaceTarget(ResolveRoot(Selection.activeGameObject));
                    actionMessage = null;
                }
            }
        }

        /// <summary>Displays a concise scan status without exposing engine internals.</summary>
        private void DrawBindingSummary()
        {
            if (!binder.WorkspaceHasScan) binder.RefreshWorkspace();
            List<UIAutoBinderWindow.WorkspaceBinding> bindings = binder.GetWorkspaceBindings();
            int pending = binder.WorkspaceChangeCount;
            int errors = binder.WorkspaceErrorCount;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(bindings.Count + " components", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(pending + " changes", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(errors == 0 ? "No errors" : errors + " errors", EditorStyles.miniLabel);
                if (GUILayout.Button(new GUIContent("↻", "Re-scan selected UI root."),
                    GUILayout.Width(26f)))
                    binder.RefreshWorkspace();
            }
        }

        /// <summary>
        /// Summarizes binding issues in a compact row, with full diagnostic text
        /// available on demand instead of stacking large HelpBoxes by default.
        /// </summary>
        private void DrawIssues()
        {
            List<string> issues = binder.GetWorkspaceIssues(true);
            if (issues.Count == 0) return;

            int errors = binder.WorkspaceErrorCount;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string headline = errors > 0
                        ? errors + " blocking issue(s) · " + issues.Count + " total"
                        : issues.Count + " change warning(s)";
                    GUILayout.Label(headline, EditorStyles.miniBoldLabel, GUILayout.ExpandWidth(true));
                    if (GUILayout.Button(showAllIssues ? "Hide Details" : "Details",
                        EditorStyles.miniButton, GUILayout.Width(86f)))
                        showAllIssues = !showAllIssues;
                }

                if (!showAllIssues)
                {
                    // Full text is available in the tooltip and expandable list.
                    string first = issues[0];
                    string shortMessage = first.Length > 80 ? first.Substring(0, 77) + "..." : first;
                    GUILayout.Label(new GUIContent(shortMessage, first),
                        EditorStyles.wordWrappedMiniLabel, GUILayout.ExpandWidth(true));
                }
                else
                {
                    for (int i = 0; i < issues.Count; i++)
                        EditorGUILayout.HelpBox(issues[i],
                            i < errors ? MessageType.Error : MessageType.Warning);
                }
            }
        }

        /// <summary>
        /// Shows up to seven component rows without reserving unused vertical space.
        /// A long list scrolls locally, while generation stays immediately below.
        /// </summary>
        private void DrawComponentList()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Components", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                componentSearch = GUILayout.TextField(componentSearch,
                    GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.textField,
                    GUILayout.MinWidth(95f), GUILayout.MaxWidth(210f));
            }

            List<UIAutoBinderWindow.WorkspaceBinding> items = binder.GetWorkspaceBindings();
            List<UIAutoBinderWindow.WorkspaceBinding> matches = new List<UIAutoBinderWindow.WorkspaceBinding>();
            foreach (UIAutoBinderWindow.WorkspaceBinding item in items)
            {
                if (item.Component == null) continue;
                if (!string.IsNullOrEmpty(componentSearch) &&
                    item.Component.gameObject.name.IndexOf(componentSearch,
                        StringComparison.OrdinalIgnoreCase) < 0 &&
                    item.ApiKey.IndexOf(componentSearch, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                matches.Add(item);
            }

            if (matches.Count == 0)
            {
                string message = items.Count == 0
                    ? (binder.WorkspaceRemovedCount > 0
                        ? "All controls removed. Generate to clear old bindings."
                        : "No named UI controls found. Add components in Factory.")
                    : "No matching components.";
                GUILayout.Label(message, EditorStyles.wordWrappedMiniLabel);
                return;
            }

            bool needsScrolling = matches.Count > VisibleComponentRows;
            if (needsScrolling)
                componentScroll = EditorGUILayout.BeginScrollView(componentScroll,
                    GUILayout.Height(VisibleComponentRows * 30f), GUILayout.ExpandHeight(false));

            foreach (UIAutoBinderWindow.WorkspaceBinding item in matches)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label(item.Component.gameObject.name, GUILayout.MinWidth(70f),
                        GUILayout.ExpandWidth(true));
                    if (position.width > 460f)
                        GUILayout.Label(item.ApiKey, EditorStyles.miniLabel, GUILayout.Width(110f));
                    if (item.RequiresGeneration)
                        GUILayout.Label(item.Change == "New binding" ? "NEW" : "EDIT",
                            EditorStyles.miniBoldLabel, GUILayout.Width(34f));
                    if (GUILayout.Button(new GUIContent(">", "Select component in the Hierarchy"),
                        GUILayout.Width(25f)))
                    {
                        Selection.activeObject = item.Component.gameObject;
                        EditorGUIUtility.PingObject(item.Component.gameObject);
                    }
                }
            }

            if (needsScrolling) EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Keeps advanced controls on the same vertically scrolling page, below
        /// the normal binding flow rather than replacing it with another screen.
        /// </summary>
        private void DrawAdvanced()
        {
            if (!showAdvanced) return;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("More Options", EditorStyles.boldLabel);
                showGenerationSettings = EditorGUILayout.Foldout(showGenerationSettings,
                    "Generation settings & property names", true);
                if (showGenerationSettings) binder.DrawAdvancedWorkspaceSettings();
                showAutomation = EditorGUILayout.Foldout(showAutomation,
                    "Automation & batch generation", true);
                if (showAutomation) automation.DrawWorkspacePage();
                showDiagnostics = EditorGUILayout.Foldout(showDiagnostics,
                    "Project diagnostics", true);
                if (showDiagnostics) diagnostics.DrawWorkspacePage();
                showPerformance = EditorGUILayout.Foldout(showPerformance,
                    "Performance benchmarks", true);
                if (showPerformance) performance.Draw();
            }
        }

        /// <summary>Draws the single main generation action and optional source preview.</summary>
        private void DrawActions()
        {
            EditorGUILayout.Space(3f);
            if (!string.IsNullOrEmpty(actionMessage))
                EditorGUILayout.HelpBox(actionMessage, MessageType.Info);
            bool valid = binder.WorkspaceCanGenerate;
            int pending = binder.WorkspaceChangeCount;
            UIBindingRoot root = binder.WorkspaceTarget != null
                ? binder.WorkspaceTarget.GetComponent<UIBindingRoot>() : null;
            bool configured = root != null && root.EditorHasGenerationProfile;
            bool needsGeneration = !configured || pending > 0 ||
                binder.WorkspaceHasConfigurationChanges;
            if (binder.WorkspaceTarget == null) needsGeneration = false;
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!valid))
                {
                    if (GUILayout.Button(new GUIContent("Preview C#", "Read-only preview; never writes scripts."),
                        GUILayout.Width(103f), GUILayout.Height(29f)))
                        binder.PreviewWorkspaceSource();
                }
                using (new EditorGUI.DisabledScope(!valid || !needsGeneration))
                {
                    string caption = !configured ? "Generate Bindings" :
                        "Generate " + pending + " Changes";
                    if (GUILayout.Button(caption, GUILayout.Height(29f)))
                    {
                        if (binder.GenerateWorkspaceSource())
                        {
                            actionMessage = "Bindings generated. Unity will now import the updated C# file.";
                            AdvancedUIToolkitAutomationMonitor.Schedule();
                        }
                        else actionMessage = "Generation was cancelled or blocked; review the issues above.";
                    }
                }
            }
            if (configured && pending == 0 && !binder.WorkspaceHasConfigurationChanges && valid)
                EditorGUILayout.LabelField("Bindings up to date", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField(
                AdvancedUIToolkitAutomationMonitor.GenerationMode == AdvancedUIToolkitGenerationMode.Manual
                    ? "Manual mode: C# changes only when you generate."
                    : "Automatic mode: safe hierarchy changes may generate C#.",
                EditorStyles.centeredGreyMiniLabel);
        }

        /// <summary>Opens Factory preconfigured for the current UI root when possible.</summary>
        private void OpenFactoryForRoot()
        {
            RectTransform uiRoot = binder != null && binder.WorkspaceTarget != null
                ? binder.WorkspaceTarget.GetComponent<RectTransform>() : null;
            UIComponentLibraryWindow.OpenFor(uiRoot);
        }
    }
}
