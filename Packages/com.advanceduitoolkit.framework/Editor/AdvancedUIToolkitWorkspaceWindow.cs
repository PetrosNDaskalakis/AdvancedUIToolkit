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
            Diagnostics
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

        /// <summary>Whether the entire warning/error list is expanded.</summary>
        [SerializeField] private bool showAllIssues;

        /// <summary>Whether editable property names and XML summaries are expanded.</summary>
        [SerializeField] private bool showPropertyDocumentation;

        /// <summary>Scrollable component preview position when a screen has many bindings.</summary>
        private Vector2 componentScroll;

        /// <summary>Reusable row-filter buffer, rebuilt in-place when the search changes.</summary>
        private readonly List<UIAutoBinderWindow.WorkspaceBinding> filteredComponentRows =
            new List<UIAutoBinderWindow.WorkspaceBinding>();

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
            window.binder.SetWorkspaceTarget(ResolveRoot(root != null ? root.gameObject : null));
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
            window.binder.SetWorkspaceTarget(ResolveRoot(root));
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
        }

        /// <summary>Refreshes the component listing when the selected hierarchy changes.</summary>
        private void OnHierarchyChange()
        {
            if (binder != null) binder.WorkspaceHierarchyChanged();
            // The old success message may no longer describe the regenerated source.
            actionMessage = null;
            Repaint();
        }

        /// <summary>
        /// Follows non-prefab scene UI containers, resolving prefab selections to
        /// their closest ordinary parent. Generation remains scoped to the owning
        /// binding root instead of introducing a manifest on a prefab instance.
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
                componentScroll = Vector2.zero;
                pageScroll = Vector2.zero;
                showAllIssues = false;
            }
            Repaint();
        }

        /// <summary>
        /// Chooses which binding scope to inspect after a Hierarchy selection change.
        /// Child controls are displayed as selected while generation remains scoped
        /// to their owning root; independent nested roots switch that scope.
        /// </summary>
        /// <param name="current">Root currently displayed by the Workspace.</param>
        /// <param name="selected">Newly selected object in Unity's Hierarchy.</param>
        /// <returns>The active UI root; null when none is available.</returns>
        internal static GameObject ResolveSelectionTarget(GameObject current, GameObject selected)
        {
            if (selected == null || EditorUtility.IsPersistent(selected)) return current;
            GameObject eligible = ResolveSelectedGameObject(selected);
            if (eligible == null) return current;

            GameObject resolved = ResolveRoot(eligible);
            if (current != null && (eligible == current ||
                eligible.transform.IsChildOf(current.transform)))
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
            RectTransform eligible = UIHierarchySelection.ResolveParent(null, selected);
            if (eligible == null) return null;
            UIBindingRoot existing = eligible.GetComponentInParent<UIBindingRoot>();
            // A binding root on a prefab must not become the selected authoring scope.
            if (existing != null && UIHierarchySelection.IsSelectableContainer(existing.gameObject))
                return existing.gameObject;
            return eligible.gameObject;
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
        }

        /// <summary>Opens the corresponding advanced foldout when navigating from older tools.</summary>
        /// <param name="tab">The previously separate page to reveal.</param>
        private void ExpandRequestedTool(WorkspaceTab tab)
        {
            if (tab == WorkspaceTab.Bindings) return;
            showAdvanced = true;
            if (tab == WorkspaceTab.Automation) showAutomation = true;
            if (tab == WorkspaceTab.Diagnostics) showDiagnostics = true;
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
                if (binder.GetWorkspaceBindings().Count > 0)
                {
                    showPropertyDocumentation = EditorGUILayout.Foldout(showPropertyDocumentation,
                        "Public Properties & XML Comments", true);
                    if (showPropertyDocumentation)
                        binder.DrawWorkspacePropertyDocumentation(componentSearch);
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Select a Canvas panel or UI root in the Hierarchy to begin.",
                    MessageType.Info);
            }
            if (binder.WorkspaceTarget != null) DrawActions();
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

        /// <summary>
        /// Displays the exact Hierarchy selection alongside its owning binding scope.
        /// Inspecting child controls must not create separate binding manifests for
        /// each child; users can still explicitly choose a different binding root.
        /// </summary>
        private void DrawRootSelector()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(new GUIContent("Selected",
                    "Closest non-prefab UI container to the Hierarchy selection."), GUILayout.Width(78f));
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField(ResolveSelectedGameObject(Selection.activeGameObject),
                        typeof(GameObject), true, GUILayout.MinWidth(80f));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(new GUIContent("Binding Root",
                    "Owning binding scope. Prefab instances cannot become authoring targets."),
                    GUILayout.Width(78f));
                GameObject previous = binder.WorkspaceTarget;
                GameObject selected = (GameObject)EditorGUILayout.ObjectField(
                    previous, typeof(GameObject), true, GUILayout.MinWidth(80f));
                if (selected != previous)
                {
                    // Explicit Object-field selection may intentionally bind a child
                    // panel that does not yet have its own manifest.
                    binder.SetWorkspaceTarget(ResolveManualBindingTarget(previous, selected));
                    actionMessage = null;
                    componentScroll = Vector2.zero;
                    pageScroll = Vector2.zero;
                    showAllIssues = false;
                }
            }
        }

        /// <summary>
        /// Resolves a deliberate manual Binding Root field edit. A non-prefab child
        /// may intentionally become a new binding scope; a prefab selection instead
        /// falls back to its owning root without creating a manifest on the prefab.
        /// </summary>
        /// <param name="current">Previously active binding root.</param>
        /// <param name="requested">Object chosen in the Binding Root field.</param>
        /// <returns>A valid manual binding target, or null when cleared.</returns>
        internal static GameObject ResolveManualBindingTarget(GameObject current, GameObject requested)
        {
            if (requested == null) return null;
            GameObject eligible = ResolveSelectedGameObject(requested);
            if (eligible == null) return current;
            return PrefabUtility.IsPartOfPrefabInstance(requested)
                ? ResolveRoot(eligible) : eligible;
        }

        /// <summary>
        /// Shows only non-prefab UI containers, resolving clicked prefab controls
        /// to their closest ordinary parent.
        /// </summary>
        /// <param name="selected">The currently selected GameObject.</param>
        /// <returns>The selected scene GameObject or null.</returns>
        internal static GameObject ResolveSelectedGameObject(GameObject selected) =>
            UIHierarchySelection.ResolveParent(null, selected) is RectTransform parent
                ? parent.gameObject : null;

        /// <summary>
        /// Returns whether the selected non-prefab UI object matches a binding row.
        /// Prefab instances are never shown as selected authoring targets.
        /// </summary>
        /// <param name="selected">The exact selected GameObject.</param>
        /// <param name="component">A discovered UI component.</param>
        /// <returns>True when the component itself or one of its children is selected.</returns>
        internal static bool IsSelectedBinding(GameObject selected, Component component) =>
            selected != null && component != null &&
            UIHierarchySelection.IsSelectableContainer(selected) &&
            !PrefabUtility.IsPartOfPrefabInstance(component.gameObject) &&
            (selected == component.gameObject ||
                selected.transform.IsChildOf(component.transform) ||
                (selected.GetComponent<UIBindingRoot>() == null &&
                 component.transform.IsChildOf(selected.transform) &&
                 AdvancedUIToolkitNaming.TypeByPrefix.ContainsKey(
                     selected.name.Split('_')[0])));

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
                string pendingLabel = pending > 0 ? pending + " pending" :
                    bindings.Count == 0 && binder.WorkspaceRemovedCount == 0 ? "No controls" :
                    binder.WorkspaceHasConfigurationChanges ? "Settings pending" : "Up to date";
                GUILayout.Label(pendingLabel, EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(errors > 0 ? errors + " blocking" :
                    binder.WorkspaceCanGenerate ? "Ready" : "No changes", EditorStyles.miniLabel);
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
                    GUILayout.MinWidth(80f), GUILayout.MaxWidth(210f));
                if (!string.IsNullOrEmpty(componentSearch) &&
                    GUILayout.Button(new GUIContent("×", "Clear component and property search"),
                        EditorStyles.toolbarButton, GUILayout.Width(22f)))
                {
                    componentSearch = string.Empty;
                    GUI.FocusControl(null);
                }
            }

            List<UIAutoBinderWindow.WorkspaceBinding> items = binder.GetWorkspaceBindings();
            filteredComponentRows.Clear();
            foreach (UIAutoBinderWindow.WorkspaceBinding item in items)
            {
                if (item.Component == null) continue;
                if (!UIAutoBinderWindow.MatchesBindingSearch(componentSearch,
                    item.Component.gameObject.name, item.ApiKey, item.HierarchyKey))
                    continue;
                filteredComponentRows.Add(item);
            }

            if (filteredComponentRows.Count == 0)
            {
                string message = items.Count == 0
                    ? (binder.WorkspaceRemovedCount > 0
                        ? "All controls removed. Generate to clear old bindings."
                        : "No named UI controls found. Add components in Factory.")
                    : "No matching components.";
                GUILayout.Label(message, EditorStyles.wordWrappedMiniLabel);
                if (!string.IsNullOrEmpty(componentSearch) &&
                    GUILayout.Button("Clear Search", EditorStyles.miniButton))
                    componentSearch = string.Empty;
                return;
            }

            bool needsScrolling = filteredComponentRows.Count > VisibleComponentRows;
            if (needsScrolling)
                componentScroll = EditorGUILayout.BeginScrollView(componentScroll,
                    GUILayout.Height(VisibleComponentRows * 30f), GUILayout.ExpandHeight(false));

            GameObject selectedContainer = ResolveSelectedGameObject(Selection.activeGameObject);
            foreach (UIAutoBinderWindow.WorkspaceBinding item in filteredComponentRows)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    bool isSelected = IsSelectedBinding(selectedContainer, item.Component);
                    GUILayout.Label(item.Component.gameObject.name,
                        isSelected ? EditorStyles.boldLabel : EditorStyles.label,
                        GUILayout.MinWidth(70f), GUILayout.ExpandWidth(true));
                    if (isSelected && position.width > 405f)
                        GUILayout.Label("SELECTED", EditorStyles.miniBoldLabel, GUILayout.Width(67f));
                    if (position.width > 460f)
                        GUILayout.Label(item.ApiKey, EditorStyles.miniLabel, GUILayout.Width(110f));
                    if (item.RequiresGeneration)
                        GUILayout.Label(item.Change == "New binding" ? "NEW" : "EDIT",
                            EditorStyles.miniBoldLabel, GUILayout.Width(34f));
                    if (GUILayout.Button(new GUIContent(">", "Locate component without changing prefab parent selection"),
                        GUILayout.Width(25f)))
                    {
                        // Prefab instances can be pinged for inspection, but cannot
                        // become the active Binder/Factory authoring selection.
                        if (UIHierarchySelection.IsSelectableContainer(item.Component.gameObject))
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
                    "Generation settings", true);
                if (showGenerationSettings) binder.DrawAdvancedWorkspaceSettings();
                showAutomation = EditorGUILayout.Foldout(showAutomation,
                    "Automation & batch generation", true);
                if (showAutomation) automation.DrawWorkspacePage();
                showDiagnostics = EditorGUILayout.Foldout(showDiagnostics,
                    "Project diagnostics", true);
                if (showDiagnostics) diagnostics.DrawWorkspacePage();
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
            if (!valid && binder.WorkspaceErrorCount == 0 && binder.WorkspaceRemovedCount == 0)
                GUILayout.Label("Add a named UI control to enable generation.",
                    EditorStyles.wordWrappedMiniLabel);
            // Blocking reasons are shown in the Issue summary directly above the list;
            // repeating the same warning below disabled actions wastes dock space.
            if (configured && pending == 0 && !binder.WorkspaceHasConfigurationChanges && valid)
                EditorGUILayout.LabelField("Bindings up to date", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField(
                AdvancedUIToolkitAutomationMonitor.GenerationMode == AdvancedUIToolkitGenerationMode.Manual
                    ? "Manual mode: C# changes only when you generate."
                    : "Automatic mode: safe hierarchy changes may generate C#.",
                EditorStyles.centeredGreyMiniLabel);
        }

        /// <summary>
        /// Opens Factory at the exact selected UI object, falling back to the active
        /// binding root when a non-UI object is selected.
        /// </summary>
        private void OpenFactoryForRoot()
        {
            GameObject selected = ResolveSelectedGameObject(Selection.activeGameObject);
            RectTransform selectedRect = selected != null ? selected.GetComponent<RectTransform>() : null;
            RectTransform root = binder != null && binder.WorkspaceTarget != null
                ? binder.WorkspaceTarget.GetComponent<RectTransform>() : null;
            UIComponentLibraryWindow.OpenFor(selectedRect != null ? selectedRect : root);
        }
    }
}
