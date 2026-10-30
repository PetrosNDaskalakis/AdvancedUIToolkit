using System;
using System.IO;
using System.Collections.Generic;
using AdvancedUIAuthoring.Editor;
using AdvancedUIToolkit;
using AdvancedUIToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace AdvancedUIAuthoring.Integration.Editor
{
    /// <summary>Mold-first layout navigation with a retained, explicitly labelled JSON compatibility workflow.</summary>
    public sealed class UIAuthoringLayoutView : ScriptableObject
    {
        /// <summary>Explicit wizard states; only the active state's controls are drawn.</summary>
        private enum Step
        {
            /// <summary>Choose a new working layout or a saved layout.</summary>
            Home = 0,
            /// <summary>Choose and parse a layout JSON source.</summary>
            Source = 1,
            /// <summary>Choose an existing saved container mold.</summary>
            Load = 2,
            /// <summary>Choose the saved-prefab profile.</summary>
            Profile = 3,
            /// <summary>Choose the exact scene destination.</summary>
            Target = 4,
            /// <summary>Validate the full request and review before creation.</summary>
            Review = 5,
            /// <summary>Display the last operation and optional next actions.</summary>
            Complete = 6,
            /// <summary>Retained ordinal for the source-choice page, now the JSON compatibility landing page.</summary>
            ChooseSource = 7,
            /// <summary>Host the hierarchy mold wizard.</summary>
            Mold = 8,
            /// <summary>Choose a saved JSON setup without exposing it in the normal mold picker.</summary>
            LegacyLoad = 9
        }

        /// <summary>Owned hierarchy capture/reuse wizard, independent of JSON draft data.</summary>
        [SerializeField] private UIHierarchyMoldView moldView;
        /// <summary>Last active draft branch, used when returning to an existing draft from Home.</summary>
        [SerializeField] private bool lastWasMold;
        /// <summary>Last selected saved asset; each load screen filters to its own supported asset type.</summary>
        [SerializeField] private ScriptableObject loadSelection;

        /// <summary>Current wizard state, retained through ordinary domain reload.</summary>
        [SerializeField] private Step stage;
        /// <summary>Transient working copy; persistent assets are never edited through the wizard.</summary>
        [SerializeField] private UIAuthoringIntegrationRecipe draft;
        /// <summary>Optional setup selected for loading or last saved as a new asset.</summary>
        [SerializeField] private UIAuthoringIntegrationRecipe savedSetup;
        /// <summary>Whether the profile step should return to the load branch.</summary>
        [SerializeField] private bool loadedBranch;
        /// <summary>Exact scene target, independent of Library and frozen outside the target step.</summary>
        [SerializeField] private RectTransform targetParent;
        /// <summary>Follow hierarchy selection only while choosing a target.</summary>
        [SerializeField] private bool followSelection = true;
        /// <summary>Optional styling controls on the source step.</summary>
        [SerializeField] private bool showStyles;
        /// <summary>Optional per-node policies on the review step.</summary>
        [SerializeField] private bool showAdvanced;
        /// <summary>Containers from the last successful generation.</summary>
        [SerializeField] private List<GameObject> createdRoots = new List<GameObject>();
        /// <summary>Schema IDs matching generated containers.</summary>
        [SerializeField] private List<string> createdIds = new List<string>();
        /// <summary>Chosen Binder root index; -1 requires a choice.</summary>
        [SerializeField] private int binderIndex = -1;
        /// <summary>Historical completion text, not a claim that later scene edits are unchanged.</summary>
        [SerializeField] private string creationSummary;
        /// <summary>Last explicit save result, retained when project notifications invalidate validation.</summary>
        [SerializeField] private string lastSavedPath;
        /// <summary>Short action feedback.</summary>
        private string message;
        /// <summary>Action-feedback severity.</summary>
        private MessageType messageType;
        /// <summary>Canonical draft used only to invalidate UI feedback.</summary>
        private string inputState;
        /// <summary>Explicit target used by the latest validation.</summary>
        private RectTransform previousParent;
        /// <summary>Parsed source document; rebuilt from the selected asset.</summary>
        private UIAuthoringDocument document;
        /// <summary>Cached read-only full preflight; creation always validates again.</summary>
        private UIAuthoringBridgeResult review;

        /// <summary>Restore JSON state only when that branch is active; the mold path needs no recipe.</summary>
        private void OnEnable() { if (IsJsonStep(stage)) EnsureDraft(); }

        /// <summary>Release only the owned temporary recipe when the window closes.</summary>
        private void OnDestroy()
        {
            if (draft != null && !AssetDatabase.Contains(draft)) DestroyImmediate(draft);
            if (moldView != null) DestroyImmediate(moldView);
        }

        /// <summary>Create generic settings without sample-specific binding IDs or class names.</summary>
        private void EnsureDraft()
        {
            if (draft != null && !AssetDatabase.Contains(draft)) return;
            draft = CreateInstance<UIAuthoringIntegrationRecipe>();
            draft.hideFlags = HideFlags.HideAndDontSave;
            draft.bindingRootId = string.Empty;
            draft.bindingClass = string.Empty;
            draft.bindingNamespace = string.Empty;
            draft.bindingOutputFolder = "Assets/Generated";
        }

        /// <summary>Load a working copy and advance only as far as its source inputs permit.</summary>
        public void LoadSetup(UIAuthoringIntegrationRecipe setup)
        {
            if (setup == null) return;
            lastWasMold = false;
            EnsureDraft();
            EditorUtility.CopySerialized(setup, draft);
            draft.hideFlags = HideFlags.HideAndDontSave;
            savedSetup = setup;
            loadSelection = setup;
            loadedBranch = true;
            showStyles = draft.stylesheet != null;
            targetParent = null;
            followSelection = true;
            ClearCompletion();
            string error;
            stage = TryReadSource(out error) ? Step.Profile : Step.Source;
            Invalidate();
            if (stage == Step.Source) SetMessage(error + " Fix this working copy to continue.", MessageType.Warning);
        }

        /// <summary>Load a saved hierarchy mold into an independent temporary copy.</summary>
        public void LoadMold(UIHierarchyMold mold)
        {
            if (mold == null) return;
            EnsureMoldView();
            moldView.Load(mold);
            lastWasMold = true;
            loadSelection = mold;
            GoTo(Step.Mold);
        }

        /// <summary>Open the retained JSON choices without discarding either working draft.</summary>
        public void OpenCompatibility() { GoTo(Step.ChooseSource); }

        /// <summary>Identify steps that require a JSON recipe; Home, mold and asset-picker pages do not.</summary>
        private static bool IsJsonStep(Step step)
        {
            return step == Step.Source || step == Step.Profile || step == Step.Target ||
                step == Step.Review || step == Step.Complete;
        }

        /// <summary>Create the mold wizard only when that workflow is chosen.</summary>
        private void EnsureMoldView()
        {
            if (moldView != null) return;
            moldView = CreateInstance<UIHierarchyMoldView>();
            moldView.hideFlags = HideFlags.HideAndDontSave;
        }

        /// <summary>Clear stale preflight without erasing inputs or historical completion.</summary>
        public void Invalidate() { review = null; message = null; }

        /// <summary>Only the target step follows scene selection; review and creation use the displayed destination.</summary>
        public void FollowCurrentSelection()
        {
            if (stage == Step.Mold) { if (moldView != null) moldView.FollowCurrentSelection(); return; }
            if (stage != Step.Target || !followSelection) return;
            GameObject selected = Selection.activeGameObject;
            if (selected == null || EditorUtility.IsPersistent(selected)) return;
            RectTransform next = selected.GetComponent<RectTransform>();
            if (next == null && !PrefabUtility.IsPartOfPrefabInstance(selected)) return;
            if (targetParent == next) return;
            targetParent = next;
            Invalidate();
        }

        /// <summary>Show one gated wizard step, retaining entered values when navigating backward.</summary>
        public void Draw()
        {
            if (IsJsonStep(stage))
            {
                EnsureDraft();
                string state = JsonUtility.ToJson(draft);
                if (state != inputState || previousParent != targetParent) Invalidate();
                inputState = state;
                previousParent = targetParent;
            }
            bool compatibility = IsJsonStep(stage) || stage == Step.ChooseSource || stage == Step.LegacyLoad;
            EditorGUILayout.LabelField(compatibility ? "Layouts — JSON Compatibility" : "Layouts", EditorStyles.boldLabel);
            bool busy = EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode;
            if (busy)
            {
                EditorGUILayout.HelpBox("Your progress is kept. Wait for Unity to finish importing/compiling and leave Play Mode to continue.", MessageType.Info);
                return;
            }
            EnsurePrerequisites();
            switch (stage)
            {
                case Step.Home: DrawHome(); break;
                case Step.ChooseSource: DrawCompatibility(); break;
                case Step.Mold:
                    EnsureMoldView();
                    if (moldView.Draw()) GoTo(Step.Home);
                    break;
                case Step.Source: DrawSource(); break;
                case Step.Load: DrawLoad(); break;
                case Step.LegacyLoad: DrawLegacyLoad(); break;
                case Step.Profile: DrawProfile(); break;
                case Step.Target: DrawTarget(); break;
                case Step.Review: DrawReview(); break;
                case Step.Complete: DrawComplete(); break;
            }
            if (IsJsonStep(stage))
            {
                EnsureDraft();
                inputState = JsonUtility.ToJson(draft);
                previousParent = targetParent;
            }
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, messageType);
        }

        /// <summary>Return to the first invalid prerequisite if an asset or target changes externally.</summary>
        private void EnsurePrerequisites()
        {
            if (stage != Step.Profile && stage != Step.Target && stage != Step.Review) return;
            string error;
            if (!TryReadSource(out error))
            {
                stage = Step.Source;
                Invalidate();
                SetMessage(error, MessageType.Warning);
                return;
            }
            if ((stage == Step.Target || stage == Step.Review) && !ValidProfile()) GoTo(Step.Profile);
            else if (stage == Step.Review && !AdvancedUIEditorIntegration.IsValidCreationParent(targetParent))
            {
                // Show the invalid explicit target before allowing selection-follow to change it.
                stage = Step.Target;
                followSelection = false;
                Invalidate();
            }
        }

        /// <summary>Present exactly the two initial choices, with no future-step configuration.</summary>
        private void DrawHome()
        {
            EditorGUILayout.LabelField("What would you like to do?", EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("Create New Layout", GUILayout.Height(32))) StartNew(false);
            EditorGUILayout.LabelField("Capture your hierarchy's containers as a reusable mold.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(8);
            if (GUILayout.Button("Load Layout", GUILayout.Height(32))) GoTo(Step.Load);
            EditorGUILayout.LabelField("Reuse a saved container mold (.asset).", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Existing JSON layouts: Tools → Advanced UI Toolkit → Compatibility → JSON Layouts.", EditorStyles.wordWrappedMiniLabel);
        }

        /// <summary>Start a fresh draft after explicit confirmation when previous input would be discarded.</summary>
        private void StartNew(bool json)
        {
            bool hasInput = moldView != null || (draft != null && (draft.schema != null || draft.profile != null || draft.stylesheet != null ||
                (draft.mappings != null && draft.mappings.Count > 0)));
            if (hasInput && !EditorUtility.DisplayDialog("Start a new layout?",
                "This replaces the current working draft. Saved layouts and scene objects are unchanged.", "Start New", "Keep Draft"))
            { GoTo(lastWasMold && moldView != null ? Step.Mold : Step.Source); return; }
            if (draft != null) DestroyImmediate(draft);
            if (moldView != null) { DestroyImmediate(moldView); moldView = null; }
            draft = null;
            savedSetup = null;
            loadSelection = null;
            loadedBranch = false;
            showStyles = false;
            showAdvanced = false;
            targetParent = null;
            followSelection = true;
            ClearCompletion();
            lastWasMold = !json;
            if (json) GoTo(Step.Source);
            else
            {
                EnsureMoldView(); moldView.BeginCapture(); GoTo(Step.Mold);
            }
        }

        /// <summary>Retain JSON creation/loading in one compatibility destination without changing its generators.</summary>
        private void DrawCompatibility()
        {
            EditorGUILayout.HelpBox("Existing JSON schemas, stylesheets and saved setups remain supported here. These actions create UI through the existing prefab workflow; they do not convert setups into molds.", MessageType.Info);
            if (GUILayout.Button("Create from JSON", GUILayout.Height(32))) StartNew(true);
            if (GUILayout.Button("Load JSON Setup", GUILayout.Height(32))) GoTo(Step.LegacyLoad);
            if (draft != null && (draft.schema != null || draft.profile != null || draft.stylesheet != null ||
                (draft.mappings != null && draft.mappings.Count > 0)))
                if (GUILayout.Button("Resume JSON Draft")) GoTo(Step.Source);
            if (GUILayout.Button("Back to Layouts")) GoTo(Step.Home);
        }

        /// <summary>Clear navigation and history belonging to a previous generation, without touching the scene.</summary>
        private void ClearCompletion()
        {
            createdRoots.Clear();
            createdIds.Clear();
            binderIndex = -1;
            creationSummary = null;
            lastSavedPath = null;
        }

        /// <summary>Choose the layout source and reveal optional styling only on this step.</summary>
        private void DrawSource()
        {
            DrawStepTitle(1, "Choose your layout");
            EditorGUILayout.LabelField("Choose the JSON file that describes your UI hierarchy.", EditorStyles.wordWrappedMiniLabel);
            EditorGUI.BeginChangeCheck();
            draft.schema = (TextAsset)EditorGUILayout.ObjectField("Layout JSON", draft.schema, typeof(TextAsset), false);
            showStyles = EditorGUILayout.Foldout(showStyles, "Optional styling" + (draft.stylesheet != null ? " — " + draft.stylesheet.name : ""), true);
            if (showStyles)
                draft.stylesheet = (TextAsset)EditorGUILayout.ObjectField("Stylesheet", draft.stylesheet, typeof(TextAsset), false);
            if (EditorGUI.EndChangeCheck()) Invalidate();
            string error;
            bool valid = TryReadSource(out error);
            if (!valid) EditorGUILayout.HelpBox(error, draft.schema == null ? MessageType.Info : MessageType.Warning);
            else EditorGUILayout.LabelField(document.nodes.Count + " UI elements described.", EditorStyles.miniLabel);
            DrawNavigation(Step.ChooseSource, valid, Step.Profile);
        }

        /// <summary>Load only a saved mold in the main workflow; JSON setups have their own compatibility picker.</summary>
        private void DrawLoad()
        {
            DrawStepTitle(1, "Load a saved layout");
            loadSelection = (UIHierarchyMold)EditorGUILayout.ObjectField("Saved Mold", loadSelection as UIHierarchyMold,
                typeof(UIHierarchyMold), false);
            EditorGUILayout.HelpBox("Choose a saved container mold. Loading opens a working copy and leaves the saved asset unchanged.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Back")) GoTo(Step.Home);
                using (new EditorGUI.DisabledScope(loadSelection == null))
                    if (GUILayout.Button("Load and Continue"))
                        LoadMold((UIHierarchyMold)loadSelection);
            }
        }

        /// <summary>Load a saved JSON setup as a copy through the existing compatibility path.</summary>
        private void DrawLegacyLoad()
        {
            DrawStepTitle(1, "Load a JSON setup");
            loadSelection = (UIAuthoringIntegrationRecipe)EditorGUILayout.ObjectField("Saved JSON Setup",
                loadSelection as UIAuthoringIntegrationRecipe, typeof(UIAuthoringIntegrationRecipe), false);
            EditorGUILayout.HelpBox("Loading opens a working copy. Your saved schema, stylesheet, profile and setup asset remain unchanged.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Back")) GoTo(Step.ChooseSource);
                using (new EditorGUI.DisabledScope(loadSelection == null))
                    if (GUILayout.Button("Load and Continue")) LoadSetup((UIAuthoringIntegrationRecipe)loadSelection);
            }
        }

        /// <summary>Choose the source of saved prefab components; defer target and advanced controls.</summary>
        private void DrawProfile()
        {
            DrawStepTitle(2, "Choose your prefabs");
            EditorGUILayout.LabelField("Which saved prefab profile should this layout use?", EditorStyles.wordWrappedMiniLabel);
            EditorGUI.BeginChangeCheck();
            draft.profile = (UIStyleProfile)EditorGUILayout.ObjectField("Prefab Profile", draft.profile, typeof(UIStyleProfile), false);
            if (EditorGUI.EndChangeCheck()) Invalidate();
            bool valid = ValidProfile();
            if (!valid) EditorGUILayout.HelpBox("Choose a saved profile. Required prefab mappings are checked before creation.", MessageType.Info);
            DrawNavigation(loadedBranch ? Step.LegacyLoad : Step.Source, valid, Step.Target);
        }

        /// <summary>Choose a destination; selection following stops outside this step.</summary>
        private void DrawTarget()
        {
            DrawStepTitle(3, "Choose where it goes");
            EditorGUI.BeginChangeCheck();
            targetParent = (RectTransform)EditorGUILayout.ObjectField("Target Parent", targetParent, typeof(RectTransform), true);
            if (EditorGUI.EndChangeCheck()) { followSelection = false; Invalidate(); }
            bool follow = EditorGUILayout.ToggleLeft("Follow Selection while on this step", followSelection);
            if (follow != followSelection)
            {
                followSelection = follow;
                if (follow) FollowCurrentSelection();
            }
            bool valid = AdvancedUIEditorIntegration.IsValidCreationParent(targetParent);
            if (!valid) EditorGUILayout.HelpBox(targetParent == null ? "Select a scene UI container to continue." :
                "Choose a non-prefab scene RectTransform. Its ancestor will not be substituted.", MessageType.Info);
            DrawNavigation(Step.Profile, valid, Step.Review);
        }

        /// <summary>Show a short summary and full preflight, exposing mappings only when requested.</summary>
        private void DrawReview()
        {
            DrawStepTitle(4, "Review and create");
            EditorGUILayout.LabelField("Layout", draft.schema.name);
            EditorGUILayout.LabelField("Prefabs", draft.profile.name);
            EditorGUILayout.LabelField("Target", targetParent.name);
            if (draft.stylesheet != null) EditorGUILayout.LabelField("Stylesheet", draft.stylesheet.name);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Edit Layout", EditorStyles.miniButton)) { loadedBranch = false; GoTo(Step.Source); }
                if (GUILayout.Button("Edit Prefabs", EditorStyles.miniButton)) GoTo(Step.Profile);
            }
            // Keep the current IMGUI draw balanced if an edit button changed state.
            if (stage != Step.Review) return;
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced options", true);
            if (showAdvanced)
            {
                EditorGUI.BeginChangeCheck();
                UIAuthoringIntegrationRecipeEditor.DrawMappingControls(draft);
                DrawBindingDestination();
                if (EditorGUI.EndChangeCheck()) Invalidate();
            }
            if (review == null) review = UIAuthoringToolkitBridge.Validate(draft, targetParent);
            bool valid = review.Succeeded;
            if (!valid)
            {
                EditorGUILayout.HelpBox(review.Error, MessageType.Error);
                EditorGUILayout.LabelField("Use Edit Layout / Edit Prefabs for source issues, or Advanced options for per-node mappings and label targets.", EditorStyles.wordWrappedMiniLabel);
            }
            else EditorGUILayout.LabelField("Ready to create " + document.nodes.Count + " UI objects.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField(AdvancedUIBindingIntegration.AutomaticGenerationEnabled
                ? "Automatic bindings: configured owning roots may update after creation."
                : "Manual bindings: generate C# later in Binder.", EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Back")) GoTo(Step.Target);
                using (new EditorGUI.DisabledScope(!valid))
                    if (GUILayout.Button("Create UI")) Generate(targetParent);
            }
            using (new EditorGUI.DisabledScope(!valid))
                if (GUILayout.Button("Save Layout for Reuse (optional)", EditorStyles.miniButton)) SaveAsNewSetup();
        }

        /// <summary>Show completion history and optional next actions, without another Create button.</summary>
        private void DrawComplete()
        {
            EditorGUILayout.LabelField("Layout created", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Last operation: " + creationSummary, MessageType.Info);
            EditorGUILayout.LabelField("Scene Undo and generated C# have separate recovery.", EditorStyles.wordWrappedMiniLabel);
            DrawCreatedRoots();
            if (GUILayout.Button("Save Layout for Reuse (optional)")) SaveAsNewSetup();
            if (!string.IsNullOrEmpty(lastSavedPath)) EditorGUILayout.LabelField("Last saved: " + lastSavedPath, EditorStyles.wordWrappedMiniLabel);
            if (GUILayout.Button("Start Another Layout")) GoTo(Step.Home);
        }

        /// <summary>Parse selected source assets; scene-dependent validation is performed on the review step.</summary>
        private bool TryReadSource(out string error)
        {
            document = null;
            if (draft.schema == null) { error = "Choose a layout JSON file to continue."; return false; }
            if (!UIAuthoringSchemaParser.TryParse(draft.schema.text, out document, out error)) return false;
            if (document.nodes.Count == 0) { error = "The layout must describe at least one UI element."; return false; }
            if (draft.stylesheet != null)
            {
                UIAuthoringStyleSheet sheet;
                if (!UIAuthoringStyleSheetParser.TryParse(draft.stylesheet.text, out sheet, out error)) return false;
            }
            error = null;
            return true;
        }

        /// <summary>Require an actual saved prefab profile, not a transient object.</summary>
        private bool ValidProfile() { return draft.profile != null && AssetDatabase.Contains(draft.profile); }

        /// <summary>Display progress without clickable links to uncompleted steps.</summary>
        private static void DrawStepTitle(int number, string title)
        {
            EditorGUILayout.LabelField("Step " + number + " of 4 — " + title, EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
        }

        /// <summary>Render Back and a gated Continue action without discarding entered values.</summary>
        private void DrawNavigation(Step back, bool ready, Step next)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Back")) GoTo(back);
                using (new EditorGUI.DisabledScope(!ready))
                    if (GUILayout.Button("Continue")) GoTo(next);
            }
        }

        /// <summary>Transition explicitly; take the current scene selection only when entering the target step.</summary>
        private void GoTo(Step next)
        {
            stage = next;
            if (IsJsonStep(stage)) { EnsureDraft(); lastWasMold = false; }
            else if (stage == Step.Mold) lastWasMold = true;
            Invalidate();
            if (stage == Step.Target) FollowCurrentSelection();
        }

        /// <summary>Choose an optional container ID without introducing sample-specific defaults.</summary>
        private void DrawBindingDestination()
        {
            var ids = new List<string> { string.Empty };
            var labels = new List<string> { "Choose after creation" };
            if (document != null && document.nodes != null)
                foreach (UIAuthoringNode node in document.nodes)
                    if (node != null && string.Equals(node.kind, "container", StringComparison.OrdinalIgnoreCase))
                    { ids.Add(node.id); labels.Add(node.id + " — " + node.name); }
            int index = ids.IndexOf(draft.bindingRootId ?? string.Empty);
            if (index < 0)
            {
                ids.Add(draft.bindingRootId);
                labels.Add("Missing container: " + draft.bindingRootId);
                index = ids.Count - 1;
            }
            int next = EditorGUILayout.Popup("Binder destination", index, labels.ToArray());
            if (next != index) draft.bindingRootId = ids[next];
            EditorGUILayout.LabelField("Class name and output settings are configured in Binder.", EditorStyles.wordWrappedMiniLabel);
        }

        /// <summary>Create through the established prefab bridge; never capture a generation baseline.</summary>
        private void Generate(RectTransform parent)
        {
            UIAuthoringBridgeResult result = UIAuthoringToolkitBridge.Generate(draft, parent);
            if (!result.Succeeded) { SetMessage(result.Error, MessageType.Error); return; }
            createdRoots.Clear();
            createdIds.Clear();
            binderIndex = -1;
            if (document != null && document.nodes != null)
            {
                foreach (UIAuthoringNode node in document.nodes)
                {
                    GameObject instance;
                    if (node == null || !string.Equals(node.kind, "container", StringComparison.OrdinalIgnoreCase) ||
                        !result.CreatedObjects.TryGetValue(node.id, out instance)) continue;
                    createdIds.Add(node.id);
                    createdRoots.Add(instance);
                    if (instance == result.BindingRoot) binderIndex = createdRoots.Count - 1;
                }
            }
            if (binderIndex < 0 && createdRoots.Count == 1) binderIndex = 0;
            if (createdRoots.Count == 0)
            {
                createdRoots.Add(parent.gameObject);
                createdIds.Add("Target parent");
                binderIndex = 0;
            }
            creationSummary = "Created " + result.CreatedObjects.Count + " UI objects under " + parent.name + ".";
            stage = Step.Complete;
            Invalidate();
        }

        /// <summary>Navigate to the exact last-created container without initializing or generating bindings.</summary>
        private void DrawCreatedRoots()
        {
            if (createdRoots.Count == 0 || createdRoots.Count != createdIds.Count) return;
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Last created UI", EditorStyles.boldLabel);
            string[] labels = new string[createdRoots.Count + 1];
            labels[0] = "Choose Binder root";
            for (int i = 0; i < createdRoots.Count; i++)
                labels[i + 1] = createdIds[i] + (createdRoots[i] != null ? " — " + createdRoots[i].name : " (missing)");
            binderIndex = EditorGUILayout.Popup("Binder root", binderIndex + 1, labels) - 1;
            GameObject target = binderIndex >= 0 && binderIndex < createdRoots.Count ? createdRoots[binderIndex] : null;
            using (new EditorGUI.DisabledScope(target == null))
                if (GUILayout.Button("Open Binder"))
                {
                    try { AdvancedUIToolkitWorkspaceWindow.OpenForTarget(target); }
                    catch (Exception exception) { SetMessage(exception.Message, MessageType.Error); }
                }
        }

        /// <summary>Save an independent setup at an unused Assets path; never overwrite existing project content.</summary>
        private void SaveAsNewSetup()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Layout", "UILayoutSetup", "asset", "Choose a new setup asset path.");
            if (string.IsNullOrEmpty(path)) return;
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)) ||
                File.Exists(path) || Directory.Exists(path))
            { SetMessage("Choose an unused asset path under Assets. Existing files are not overwritten.", MessageType.Error); return; }
            UIAuthoringIntegrationRecipe copy = null;
            try
            {
                copy = CreateInstance<UIAuthoringIntegrationRecipe>();
                EditorUtility.CopySerialized(draft, copy);
                copy.hideFlags = HideFlags.None;
                AssetDatabase.CreateAsset(copy, path);
                savedSetup = copy;
                lastSavedPath = path;
                SetMessage("Saved setup: " + path, MessageType.Info);
            }
            catch (Exception exception)
            {
                if (copy != null && !AssetDatabase.Contains(copy)) DestroyImmediate(copy);
                SetMessage("Could not save setup: " + exception.Message, MessageType.Error);
            }
        }

        /// <summary>Set short feedback for the current action.</summary>
        private void SetMessage(string value, MessageType severity) { message = value; messageType = severity; }
    }
}
