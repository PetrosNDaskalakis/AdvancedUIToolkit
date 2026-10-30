using System;
using AdvancedUIToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace AdvancedUIAuthoring.Integration.Editor
{
    /// <summary>Compact creation and explicit Binder workflow for Integration Recipes.</summary>
    public sealed class UIAuthoringIntegrationWindow : EditorWindow
    {
        /// <summary>Recipe retained through Editor domain reload.</summary>
        [SerializeField] private UIAuthoringIntegrationRecipe recipe;
        /// <summary>Explicit scene target, independent of the Library's selected parent.</summary>
        [SerializeField] private RectTransform parent;
        /// <summary>Explicit Binder target, populated after a successful build.</summary>
        [SerializeField] private GameObject bindingTarget;
        /// <summary>Exact object references from the last successful generation, awaiting an explicit saved baseline.</summary>
        [SerializeField] private UIAuthoringPendingGeneration pending;
        /// <summary>Editor-only saved scene baseline selected for preview.</summary>
        [SerializeField] private UIAuthoringGenerationRecord record;
        /// <summary>Compact preview controls remain collapsible.</summary>
        [SerializeField] private bool showChanges;
        /// <summary>Whether unchanged rows should be expanded in the report.</summary>
        [SerializeField] private bool showUnchanged;
        /// <summary>Latest read-only report; not retained across reload or source changes.</summary>
        private UIAuthoringChangePreviewResult preview;
        /// <summary>Whether a previous report was invalidated by an observed edit.</summary>
        private bool previewStale;

        /// <summary>Current operation feedback.</summary>
        private string message = string.Empty;
        /// <summary>Current feedback severity.</summary>
        private MessageType messageType = MessageType.Info;
        /// <summary>Scroll position for errors on small windows.</summary>
        private Vector2 scroll;

        /// <summary>Preserve the public integration entry point by opening JSON compatibility in Layouts.</summary>
        public static void Open() { UIAuthoringLayoutsWindow.OpenCompatibility(); }

        /// <summary>Retain explicit compatibility access to the previous generation-record workflow.</summary>
        [MenuItem("Tools/Advanced UI Toolkit/Compatibility/Legacy Integration Preview")]
        public static void OpenLegacy()
        {
            var window = GetWindow<UIAuthoringIntegrationWindow>("UI Integration");
            window.minSize = new Vector2(340, 270);
            window.Show();
        }

        /// <summary>Capture an ordinary selection on first open without changing it.</summary>
        private void OnEnable()
        {
            Undo.undoRedoPerformed += InvalidatePreview;
            Undo.postprocessModifications += OnPropertyChanges;
            EditorApplication.projectChanged += InvalidatePreview;
            if (record != null) parent = UIAuthoringChangePreview.ResolveParent(record);
            else if (parent == null && Selection.activeGameObject != null)
            {
                var candidate = Selection.activeGameObject.GetComponent<RectTransform>();
                if (candidate != null && !EditorUtility.IsPersistent(candidate) &&
                    !PrefabUtility.IsPartOfPrefabInstance(candidate)) parent = candidate;
            }
        }

        /// <summary>Render creation and Binder as two explicit operations.</summary>
        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.HelpBox("Compatibility workflow for existing generation records. Use the Layouts window for new UI.", MessageType.Info);
            EditorGUILayout.LabelField("Legacy Integration Preview", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            recipe = (UIAuthoringIntegrationRecipe)EditorGUILayout.ObjectField("Recipe", recipe,
                typeof(UIAuthoringIntegrationRecipe), false);
            if (EditorGUI.EndChangeCheck())
            {
                message = string.Empty; bindingTarget = null; pending = null; record = null; preview = null;
            }
            EditorGUI.BeginChangeCheck();
            parent = (RectTransform)EditorGUILayout.ObjectField("Target Parent", parent, typeof(RectTransform), true);
            if (EditorGUI.EndChangeCheck()) { pending = null; record = null; preview = null; }
            using (new EditorGUI.DisabledScope(recipe == null || parent == null))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate")) ShowResult(UIAuthoringToolkitBridge.Validate(recipe, parent), false);
                if (GUILayout.Button("Generate UI"))
                {
                    UIAuthoringBridgeResult result = UIAuthoringToolkitBridge.Generate(recipe, parent);
                    // Clear a previous result on failure so a later click cannot sync an unrelated root.
                    bindingTarget = result.Succeeded ? result.BindingRoot : null;
                    pending = null;
                    preview = null;
                    ShowResult(result, true);
                    if (result.Succeeded)
                    {
                        record = null;
                        try
                        {
                            pending = UIAuthoringChangePreview.BeginTracking(recipe, parent, result);
                            showChanges = true;
                            message += " Save the scene, then Capture Baseline to enable change previews.";
                        }
                        catch (Exception exception)
                        {
                            message += " UI creation succeeded, but preview tracking is unavailable: " + exception.Message;
                            messageType = MessageType.Warning;
                        }
                    }
                }
            }
            EditorGUILayout.Space(6);
            bindingTarget = (GameObject)EditorGUILayout.ObjectField("Binding Root", bindingTarget, typeof(GameObject), true);
            EditorGUILayout.LabelField("C# generation is separate from hierarchy Undo.", EditorStyles.miniLabel);
            using (new EditorGUI.DisabledScope(bindingTarget == null))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Inspect Binder")) ShowBinding(UIAuthoringToolkitBridge.InspectBindings(bindingTarget));
                using (new EditorGUI.DisabledScope(recipe == null))
                    if (GUILayout.Button("Sync Bindings")) ShowBinding(UIAuthoringToolkitBridge.SyncBindings(recipe, bindingTarget));
            }
            EditorGUILayout.Space(6);
            showChanges = EditorGUILayout.Foldout(showChanges, "Change Preview (read-only)", true);
            if (showChanges) DrawChangePreview();
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, messageType);
            EditorGUILayout.EndScrollView();
        }

        /// <summary>Unsubscribe from Editor notifications when the window closes or reloads.</summary>
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= InvalidatePreview;
            Undo.postprocessModifications -= OnPropertyChanges;
            EditorApplication.projectChanged -= InvalidatePreview;
        }

        /// <summary>Clear reports after observed scene structure changes.</summary>
        private void OnHierarchyChange() { InvalidatePreview(); }

        /// <summary>Observe Inspector edits without altering their Undo modifications.</summary>
        private UndoPropertyModification[] OnPropertyChanges(UndoPropertyModification[] modifications)
        {
            InvalidatePreview();
            return modifications;
        }

        /// <summary>Reports are snapshots and must be rebuilt after edits, asset imports or Undo.</summary>
        private void InvalidatePreview()
        {
            if (preview != null) { preview = null; previewStale = true; Repaint(); }
        }

        /// <summary>Open a saved record without changing Unity selection or opening its scene implicitly.</summary>
        public static void OpenRecord(UIAuthoringGenerationRecord value)
        {
            var window = GetWindow<UIAuthoringIntegrationWindow>("UI Integration");
            window.SelectRecord(value);
            window.showChanges = true;
            window.Show();
        }

        /// <summary>Adopt the explicitly selected record's recipe and target context.</summary>
        private void SelectRecord(UIAuthoringGenerationRecord value)
        {
            record = value;
            pending = null;
            preview = null;
            previewStale = false;
            bindingTarget = null;
            if (record != null)
            {
                recipe = record.Recipe;
                parent = UIAuthoringChangePreview.ResolveParent(record);
            }
        }

        /// <summary>Draw baseline capture, explicit preview and source/conflict rows without any Apply action.</summary>
        private void DrawChangePreview()
        {
            EditorGUI.BeginChangeCheck();
            UIAuthoringGenerationRecord selected = (UIAuthoringGenerationRecord)EditorGUILayout.ObjectField(
                "Generation Record", record, typeof(UIAuthoringGenerationRecord), false);
            if (EditorGUI.EndChangeCheck()) SelectRecord(selected);
            EditorGUILayout.LabelField("Capture the saved scene's current UI state. Preview never applies changes.", EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(pending == null))
                    if (GUILayout.Button("Capture Baseline"))
                    {
                        UIAuthoringGenerationRecord saved;
                        string error;
                        if (UIAuthoringChangePreview.CaptureBaseline(pending, out saved, out error))
                        {
                            record = saved;
                            pending = null;
                            preview = null;
                            previewStale = false;
                            message = "Captured baseline for " + saved.NodeCount + " nodes. Edit the schema, then Preview Changes.";
                            messageType = MessageType.Info;
                        }
                        else { message = error; messageType = MessageType.Error; }
                    }
                using (new EditorGUI.DisabledScope(record == null))
                    if (GUILayout.Button("Preview Changes"))
                    {
                        preview = UIAuthoringChangePreview.Preview(record);
                        previewStale = false;
                    }
            }
            if (previewStale) EditorGUILayout.LabelField("Inputs changed. Run Preview Changes again.", EditorStyles.wordWrappedMiniLabel);
            if (preview == null) return;
            foreach (string error in preview.Errors) EditorGUILayout.HelpBox(error, MessageType.Error);
            foreach (string notice in preview.Notices) EditorGUILayout.HelpBox(notice, MessageType.Info);
            int changed = 0;
            int conflicts = 0;
            foreach (UIAuthoringChangeRow row in preview.Rows)
            {
                if (row.Change != UIAuthoringChangeKind.Unchanged) changed++;
                if (row.HasConflict) conflicts++;
            }
            EditorGUILayout.LabelField(preview.Rows.Count + " nodes · " + changed + " source changes/reviews · " + conflicts + " scene conflicts",
                EditorStyles.wordWrappedMiniLabel);
            showUnchanged = EditorGUILayout.ToggleLeft("Show unchanged nodes", showUnchanged);
            foreach (UIAuthoringChangeRow row in preview.Rows)
            {
                if (!showUnchanged && row.Change == UIAuthoringChangeKind.Unchanged && !row.HasConflict) continue;
                EditorGUILayout.LabelField(row.NodeId + " — " + row.Change + (row.HasConflict ? " · CONFLICT" : string.Empty), EditorStyles.boldLabel);
                EditorGUILayout.LabelField(row.Message, EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.LabelField("Snapshot only. No Apply action is included in this release.", EditorStyles.wordWrappedMiniLabel);
        }

        /// <summary>Display validation or creation status without a modal dialog.</summary>
        private void ShowResult(UIAuthoringBridgeResult result, bool generated)
        {
            messageType = result.Succeeded ? MessageType.Info : MessageType.Error;
            message = result.Succeeded ? (generated ? "Created " + result.CreatedObjects.Count +
                " schema nodes. One Ctrl+Z undoes this hierarchy." : "Schema, templates, text targets and root layouts are valid.") : result.Error;
        }

        /// <summary>Preserve precise Binder status, especially a blocked accessor removal.</summary>
        private void ShowBinding(UIEditorBindingResult result)
        {
            message = result.Status + ": " + result.Message;
            messageType = result.Status == UIEditorBindingStatus.Blocked ? MessageType.Error :
                (result.Status == UIEditorBindingStatus.PendingRemoval || result.Status == UIEditorBindingStatus.SetupRequired
                    ? MessageType.Warning : MessageType.Info);
        }
    }
}
