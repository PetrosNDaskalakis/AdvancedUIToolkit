using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdvancedUIToolkit.Editor
{
    /// <summary>
    /// Controls when Advanced UI Toolkit writes generated C# source files.
    /// This is separate from Unity's own asset refresh and script compilation preferences.
    /// </summary>
    internal enum AdvancedUIToolkitGenerationMode
    {
        /// <summary>Changes are reported but C# files are written only by explicit user action.</summary>
        Manual = 0,

        /// <summary>Safe binding changes automatically update generated C# files.</summary>
        Automatic = 1
    }

    /// <summary>
    /// Watches UI binding roots in editable scenes and Prefab Mode.
    /// Automatically applies source-validated additions, renames and removals only
    /// when Automatic mode is selected. Deleted accessors may break consuming code.
    /// </summary>
    [InitializeOnLoad]
    internal static class AdvancedUIToolkitAutomationMonitor
    {
        /// <summary>Defines the watch pref constant.</summary>
        private const string WatchPref = "AdvancedUIToolkit.Automation.Watch";
        /// <summary>Defines the auto generate pref constant.</summary>
        private const string AutoGeneratePref = "AdvancedUIToolkit.Automation.AutoGenerate";
        /// <summary>Persists the two-state generation mode across Unity Editor sessions.</summary>
        private const string GenerationModePref = "AdvancedUIToolkit.Automation.GenerationMode";
        /// <summary>Defines the debounce seconds constant.</summary>
        private const double DebounceSeconds = 0.4;

        /// <summary>Stores the item reference or configuration value.</summary>
        internal sealed class Item
        {
            /// <summary>Stores the root reference or configuration value.</summary>
            internal UIBindingRoot Root;
            /// <summary>Stores the result reference or configuration value.</summary>
            internal UIAutoGenerationResult Result;
        }

        /// <summary>Stores the items reference or configuration value.</summary>
        private static readonly List<Item> Items = new List<Item>();
        /// <summary>Stores the pending reference or configuration value.</summary>
        private static bool pending;
        /// <summary>Stores the next check reference or configuration value.</summary>
        private static double nextCheck;
        /// <summary>Stores whether the monitor is currently processing a scan.</summary>
        private static bool processing;
        /// <summary>Counts active nested Editor integration authoring operations.</summary>
        private static int suspensionDepth;

        /// <summary>True while a hierarchy authoring transaction is active.</summary>
        internal static bool IsSuspended => suspensionDepth > 0;
        /// <summary>True while automation is executing a refresh or regeneration pass.</summary>
        internal static bool IsProcessing => processing;

        /// <summary>One owned suspension of background hierarchy checks.</summary>
        private sealed class Suspension : IDisposable
        {
            /// <summary>Prevents double-release of the outer monitor scope.</summary>
            private bool disposed;

            /// <summary>Releases one suspension and schedules a deferred rescan.</summary>
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                suspensionDepth--;
                if (suspensionDepth == 0)
                    Schedule();
            }
        }

        /// <summary>
        /// Delays background automatic Binder work until the last caller releases its scope.
        /// Nested scopes are supported. Must be disposed on the Unity Editor main thread.
        /// </summary>
        /// <returns>A disposable suspension of automation scans and writes.</returns>
        internal static IDisposable Suspend()
        {
            suspensionDepth++;
            return new Suspension();
        }

        /// <summary>Notifies listeners when changed occurs.</summary>
        internal static event Action Changed;
        /// <summary>Gets results.</summary>
        internal static IReadOnlyList<Item> Results => Items;

        /// <summary>Stores the watch enabled reference or configuration value.</summary>
        internal static bool WatchEnabled
        {
            get => EditorPrefs.GetBool(WatchPref, true);
            set { EditorPrefs.SetBool(WatchPref, value); Schedule(); }
        }

        /// <summary>
        /// Gets or sets Advanced UI Toolkit's C# generation mode. Existing installations that opted into
        /// auto-generation retain that choice until the developer changes the mode.
        /// </summary>
        internal static AdvancedUIToolkitGenerationMode GenerationMode
        {
            get
            {
                if (EditorPrefs.HasKey(GenerationModePref))
                    return EditorPrefs.GetInt(GenerationModePref, 0) == 1
                        ? AdvancedUIToolkitGenerationMode.Automatic
                        : AdvancedUIToolkitGenerationMode.Manual;

                return EditorPrefs.GetBool(AutoGeneratePref, false)
                    ? AdvancedUIToolkitGenerationMode.Automatic
                    : AdvancedUIToolkitGenerationMode.Manual;
            }
            set
            {
                EditorPrefs.SetInt(GenerationModePref, (int)value);
                // Preserve the old preference for compatibility with earlier releases.
                EditorPrefs.SetBool(AutoGeneratePref, value == AdvancedUIToolkitGenerationMode.Automatic);
                Schedule();
            }
        }

        /// <summary>Indicates whether safe changes are currently generated automatically.</summary>
        internal static bool AutoGenerateEnabled
        {
            get => GenerationMode == AdvancedUIToolkitGenerationMode.Automatic;
            set => GenerationMode = value ? AdvancedUIToolkitGenerationMode.Automatic : AdvancedUIToolkitGenerationMode.Manual;
        }

        /// <summary>Counts pending additions, renames, and deletions eligible for regeneration.</summary>
        internal static int PendingSafeCount => Items.Count(item =>
            item.Root != null && item.Result != null &&
            IsPending(item.Result));

        /// <summary>Identifies a change that can be applied after source ownership validation.</summary>
        /// <param name="result">Current automated inspection result.</param>
        /// <returns>True when a generation run is pending.</returns>
        internal static bool IsPending(UIAutoGenerationResult result) => result != null &&
            (result.Status == UIAutoGenerationStatus.SafeChange ||
             result.Status == UIAutoGenerationStatus.PendingRemoval);

        /// <summary>Counts queued updates that remove public binding properties.</summary>
        internal static int PendingRemovalCount => Items.Count(item =>
            item.Root != null && item.Result != null &&
            item.Result.Status == UIAutoGenerationStatus.PendingRemoval);

        /// <summary>Performs the advanced ui toolkit automation monitor operation.</summary>
        static AdvancedUIToolkitAutomationMonitor()
        {
            EditorApplication.hierarchyChanged += Schedule;
            EditorApplication.projectChanged += Schedule;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            EditorSceneManager.sceneOpened += (scene, mode) => Schedule();
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) Schedule();
            };
            EditorApplication.update += Update;
            Schedule();
        }

        /// <summary>
        /// Schedules a second, delayed check after Unity finishes restoring a deleted
        /// hierarchy object or prefab instance through Undo/Redo. The ordinary
        /// hierarchyChanged callback may run before all restored references settle.
        /// </summary>
        private static void OnUndoRedoPerformed()
        {
            Schedule();
            EditorApplication.delayCall += Schedule;
        }

        /// <summary>Queues a debounced binding check after an Editor hierarchy change.</summary>
        internal static void Schedule()
        {
            if (!WatchEnabled) return;
            pending = true;
            nextCheck = EditorApplication.timeSinceStartup + DebounceSeconds;
        }

        /// <summary>Performs the update operation.</summary>
        private static void Update()
        {
            if (!pending || IsSuspended || EditorApplication.timeSinceStartup < nextCheck) return;
            // Never consume a scheduled Undo/Redo rescan while another generation
            // pass is still running. Leaving pending=true guarantees a later retry.
            if (processing || IsSuspended || EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            pending = false;
            Refresh();
        }

        /// <summary>Refreshes value.</summary>
        internal static void Refresh()
        {
            if (processing || IsSuspended || EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            processing = true;
            try
            {
                Items.Clear();
                // Automation does not traverse or edit closed scene assets or prefab assets.
                foreach (UIBindingRoot root in GetEditableRoots())
                {
                    UIAutoGenerationResult result = UIAutoBinderWindow.InspectForAutomation(root, false);
                    if (IsPending(result) && AutoGenerateEnabled)
                        result = UIAutoBinderWindow.InspectForAutomation(root, true);
                    Items.Add(new Item { Root = root, Result = result });
                }
            }
            finally
            {
                processing = false;
                Changed?.Invoke();
            }
        }

        /// <summary>Generates one.</summary>
        internal static UIAutoGenerationResult GenerateOne(UIBindingRoot root)
        {
            if (processing || IsSuspended || EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling || EditorApplication.isUpdating)
                return new UIAutoGenerationResult(UIAutoGenerationStatus.Blocked,
                    "Unity is compiling, importing assets, or entering Play Mode.");
            UIAutoGenerationResult preview = UIAutoBinderWindow.InspectForAutomation(root, false);
            if (preview.Status == UIAutoGenerationStatus.PendingRemoval &&
                !EditorUtility.DisplayDialog("Advanced UI Toolkit - Remove deleted bindings",
                    preview.Message + "\n\nRemove these generated C# accessors now?",
                    "Remove Bindings", "Cancel"))
                return preview;
            UIAutoGenerationResult result = UIAutoBinderWindow.InspectForAutomation(root, true);
            Schedule();
            return result;
        }

        /// <summary>
        /// Applies all currently eligible additions, renames and removals on user request.
        /// Asset editing is batched so Unity can import the generated C# files together.
        /// Removals are explicitly confirmed in manual mode. Changes that fail
        /// source ownership or generated-file safety checks remain blocked.
        /// </summary>
        /// <returns>A summary of the generation results.</returns>
        internal static string GenerateAllPending()
        {
            if (processing || IsSuspended || EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling || EditorApplication.isUpdating)
                return "Cannot generate while Unity is busy compiling, importing, or entering Play Mode.";

            // Snapshot before generation; successful imports may trigger additional scans.
            List<UIBindingRoot> roots = Items
                .Where(item => item.Root != null && item.Result != null &&
                    IsPending(item.Result))
                .Select(item => item.Root).Distinct().ToList();

            if (roots.Count == 0)
                return "No pending binding changes. Use Refresh Now to rescan the open scene.";

            int removals = Items.Count(item => item.Root != null &&
                roots.Contains(item.Root) && item.Result != null &&
                item.Result.Status == UIAutoGenerationStatus.PendingRemoval);
            if (removals > 0 && !EditorUtility.DisplayDialog(
                "Advanced UI Toolkit - Removed components",
                removals + " UI root(s) contain deleted bindings. Their generated C# " +
                "properties will be removed and scripts using those properties may " +
                "need updating. Generate all pending changes?",
                "Generate Changes", "Cancel"))
                return "Generation cancelled. Removed bindings remain pending.";

            int generated = 0;
            int blocked = 0;
            processing = true;
            try
            {
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (UIBindingRoot root in roots)
                    {
                        if (root == null)
                        {
                            blocked++;
                            continue;
                        }

                        UIAutoGenerationResult result = UIAutoBinderWindow.InspectForAutomation(root, true);
                        if (result.Status == UIAutoGenerationStatus.Generated)
                            generated++;
                        else
                            blocked++;
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return $"Generation interrupted: {generated} updated, {blocked} blocked. " +
                       "Review the Console and refresh the workspace.";
            }
            finally
            {
                processing = false;
                Schedule();
                Changed?.Invoke();
            }

            return $"Generated {generated} binding class(es); {blocked} require review. " +
                   "Unity may now import and compile the changed C# files.";
        }

        /// <summary>Gets editable roots.</summary>
        private static IEnumerable<UIBindingRoot> GetEditableRoots()
        {
            HashSet<UIBindingRoot> visited = new HashSet<UIBindingRoot>();
            // A prefab opened in isolation has its own stage scene. Include it even
            // when it isn't among normal user scenes.
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.prefabContentsRoot != null)
                foreach (UIBindingRoot root in stage.prefabContentsRoot.GetComponentsInChildren<UIBindingRoot>(true))
                    if (root != null && visited.Add(root)) yield return root;

            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (!scene.isLoaded || !scene.IsValid()) continue;
                // Don't touch the prefab stage twice if Unity lists it in SceneManager.
                foreach (GameObject obj in scene.GetRootGameObjects())
                    foreach (UIBindingRoot root in obj.GetComponentsInChildren<UIBindingRoot>(true))
                        if (root != null && visited.Add(root) && !EditorUtility.IsPersistent(root))
                            yield return root;
            }
        }
    }

    /// <summary>Stores the editor window reference or configuration value.</summary>
    public sealed class AdvancedUIToolkitAutomationWindow : EditorWindow
    {
        /// <summary>Stores the scroll reference or configuration value.</summary>
        private Vector2 scroll;
        /// <summary>Stores the show up to date reference or configuration value.</summary>
        private bool showUpToDate;
        /// <summary>Stores the last action reference or configuration value.</summary>
        private string lastAction;

        /// <summary>Opens value.</summary>
        public static void Open() => AdvancedUIToolkitWorkspaceWindow.OpenTab(AdvancedUIToolkitWorkspaceWindow.WorkspaceTab.Automation);

        /// <summary>Handles the Unity OnEnable callback.</summary>
        private void OnEnable() => AdvancedUIToolkitAutomationMonitor.Changed += Repaint;
        /// <summary>Handles the Unity OnDisable callback.</summary>
        private void OnDisable() => AdvancedUIToolkitAutomationMonitor.Changed -= Repaint;

        /// <summary>Draws automation controls within the Advanced UI Toolkit workspace.</summary>
        internal void DrawWorkspacePage()
        {
            EditorGUILayout.LabelField("Advanced UI Toolkit / Automation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Watches open scenes and the current Prefab Stage. Only registered UI roots " +
                "can regenerate. Removed bindings are queued in Manual mode and regenerated " +
                "in Automatic mode. Invalid names and unexpected edits remain blocked.", MessageType.Info);

            bool watching = EditorGUILayout.ToggleLeft("Watch hierarchy changes",
                AdvancedUIToolkitAutomationMonitor.WatchEnabled);
            if (watching != AdvancedUIToolkitAutomationMonitor.WatchEnabled)
                AdvancedUIToolkitAutomationMonitor.WatchEnabled = watching;

            AdvancedUIToolkitGenerationMode currentMode = AdvancedUIToolkitAutomationMonitor.GenerationMode;
            AdvancedUIToolkitGenerationMode selectedMode = (AdvancedUIToolkitGenerationMode)EditorGUILayout.EnumPopup(
                new GUIContent("Code Generation Mode", "When Advanced UI Toolkit should write generated C# source."),
                currentMode);
            if (selectedMode != currentMode)
                AdvancedUIToolkitAutomationMonitor.GenerationMode = selectedMode;

            EditorGUILayout.HelpBox(
                selectedMode == AdvancedUIToolkitGenerationMode.Manual
                    ? "Manual: Advanced UI Toolkit detects and queues changes without writing C# files. " +
                      "Use Generate Pending Changes when you are ready for Unity to compile."
                    : "Automatic: Advanced UI Toolkit regenerates C# for additions, renames, and deletions. " +
                      "Removing properties may require script changes and trigger compilation.",
                selectedMode == AdvancedUIToolkitGenerationMode.Manual ? MessageType.Info : MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Now")) AdvancedUIToolkitAutomationMonitor.Refresh();
            if (GUILayout.Button("Open Diagnostics")) AdvancedUIToolkitDiagnosticsWindow.Open();
            EditorGUILayout.EndHorizontal();

            if (AdvancedUIToolkitAutomationMonitor.PendingRemovalCount > 0)
                EditorGUILayout.HelpBox(AdvancedUIToolkitAutomationMonitor.PendingRemovalCount +
                    " UI root(s) contain deleted bindings. Generating removes their C# " +
                    "properties; update consuming scripts if necessary.", MessageType.Warning);
            using (new EditorGUI.DisabledScope(AdvancedUIToolkitAutomationMonitor.PendingSafeCount == 0))
            {
                string caption = "Generate Pending Changes (" +
                    AdvancedUIToolkitAutomationMonitor.PendingSafeCount + ")";
                if (GUILayout.Button(caption, GUILayout.Height(28f)))
                    lastAction = AdvancedUIToolkitAutomationMonitor.GenerateAllPending();
            }
            showUpToDate = EditorGUILayout.ToggleLeft("Show up-to-date roots", showUpToDate);

            if (!string.IsNullOrEmpty(lastAction))
                EditorGUILayout.HelpBox(lastAction, MessageType.None);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            int shown = 0;
            foreach (AdvancedUIToolkitAutomationMonitor.Item item in AdvancedUIToolkitAutomationMonitor.Results)
            {
                if (item.Root == null || item.Result == null) continue;
                if (!showUpToDate && item.Result.Status == UIAutoGenerationStatus.UpToDate) continue;
                shown++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(item.Root.name + " — " + item.Result.Status,
                    EditorStyles.boldLabel);
                EditorGUILayout.LabelField(item.Result.Message, EditorStyles.wordWrappedLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select", GUILayout.Width(70)))
                    Selection.activeGameObject = item.Root.gameObject;
                if (GUILayout.Button("Review in AutoBinder"))
                    UIAutoBinderWindow.OpenFor(item.Root);
                if (AdvancedUIToolkitAutomationMonitor.IsPending(item.Result) &&
                    GUILayout.Button(item.Result.Status == UIAutoGenerationStatus.PendingRemoval
                        ? "Remove Deleted Bindings" : "Generate Changes"))
                {
                    UIAutoGenerationResult result = AdvancedUIToolkitAutomationMonitor.GenerateOne(item.Root);
                    lastAction = item.Root.name + ": " + result.Message;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            if (shown == 0)
                EditorGUILayout.HelpBox("No pending changes in editable UI roots. " +
                    "Use Refresh Now after opening a scene or prefab.", MessageType.None);
            EditorGUILayout.EndScrollView();
        }
    }
}
