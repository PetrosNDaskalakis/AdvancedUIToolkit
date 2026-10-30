using System;
using System.IO;
using AdvancedUIToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace AdvancedUIAuthoring.Integration.Editor
{
    /// <summary>Guided hierarchy capture and reuse, hosted only by the Layouts window.</summary>
    public sealed class UIHierarchyMoldView : ScriptableObject
    {
        /// <summary>Only the active stage exposes its controls.</summary>
        private enum Step
        {
            /// <summary>Choose a source root and capture a detached snapshot.</summary>
            Capture,
            /// <summary>Inspect the captured hierarchy and removed references.</summary>
            Inspect,
            /// <summary>Choose a held destination and root name.</summary>
            Target,
            /// <summary>Review and create after validation.</summary>
            Review,
            /// <summary>Show historical completion and optional Binder navigation.</summary>
            Complete
        }

        /// <summary>Current guided step.</summary>
        [SerializeField] private Step stage;
        /// <summary>Explicit capture source; never stored inside the saved mold.</summary>
        [SerializeField] private GameObject source;
        /// <summary>Owned detached snapshot, including when loaded from an asset.</summary>
        [SerializeField] private UIHierarchyMold draft;
        /// <summary>Whether this snapshot came from a saved mold.</summary>
        [SerializeField] private bool loaded;
        /// <summary>Explicit creation destination, frozen after the target step.</summary>
        [SerializeField] private RectTransform target;
        /// <summary>Name for the new root; original descendants keep their names.</summary>
        [SerializeField] private string rootName;
        /// <summary>Follow scene selection only during target choice.</summary>
        [SerializeField] private bool followSelection = true;
        /// <summary>Disclosure state for hierarchy details.</summary>
        [SerializeField] private bool showHierarchy = true;
        /// <summary>Disclosure state for removed-reference details.</summary>
        [SerializeField] private bool showOmissions;
        /// <summary>Created root for optional navigation; may disappear through Undo.</summary>
        [SerializeField] private GameObject created;
        /// <summary>Historical creation result, independent of later edits.</summary>
        [SerializeField] private string completion;
        /// <summary>Last saved snapshot path.</summary>
        [SerializeField] private string savedPath;
        /// <summary>Current actionable feedback.</summary>
        [SerializeField] private string feedback;

        /// <summary>Release only the owned temporary snapshot.</summary>
        private void OnDestroy() { ReleaseDraft(); }

        /// <summary>Discard temporary data without changing a saved asset or scene.</summary>
        private void ReleaseDraft()
        {
            if (draft != null && !AssetDatabase.Contains(draft)) DestroyImmediate(draft);
            draft = null;
        }

        /// <summary>Start a new capture, optionally seeding the currently selected source.</summary>
        public void BeginCapture()
        {
            ReleaseDraft();
            source = UIHierarchyMoldCapture.IsValidSource(Selection.activeGameObject) ? Selection.activeGameObject : null;
            loaded = false; target = null; followSelection = true; created = null;
            completion = null; savedPath = null; feedback = null; stage = Step.Capture;
        }

        /// <summary>Load a working copy with no writes to the selected mold asset.</summary>
        public void Load(UIHierarchyMold mold)
        {
            if (mold == null) return;
            ReleaseDraft();
            draft = CreateInstance<UIHierarchyMold>();
            EditorUtility.CopySerialized(mold, draft);
            draft.hideFlags = HideFlags.HideAndDontSave;
            loaded = true; source = null; target = null; followSelection = true;
            rootName = draft.nodes != null && draft.nodes.Count > 0 && draft.nodes[0] != null ? draft.nodes[0].name : string.Empty;
            created = null; completion = null; feedback = null; savedPath = null; stage = Step.Inspect;
        }

        /// <summary>Follow only while choosing a target; selecting the source never retargets a reviewed operation.</summary>
        public void FollowCurrentSelection()
        {
            if (stage != Step.Target || !followSelection || Selection.activeGameObject == null || EditorUtility.IsPersistent(Selection.activeGameObject)) return;
            RectTransform next = Selection.activeGameObject.GetComponent<RectTransform>();
            if (next != null) target = next;
        }

        /// <summary>Draw the current step and return true only when the user explicitly returns to the Layouts start screen.</summary>
        public bool Draw()
        {
            bool home = false;
            switch (stage)
            {
                case Step.Capture: home = DrawCapture(); break;
                case Step.Inspect: home = DrawInspect(); break;
                case Step.Target: DrawTarget(); break;
                case Step.Review: DrawReview(); break;
                case Step.Complete: home = DrawComplete(); break;
            }
            if (!string.IsNullOrEmpty(feedback)) EditorGUILayout.HelpBox(feedback, MessageType.Info);
            return home;
        }

        /// <summary>Read the source only on explicit capture; never auto-recapture during repaint or scene selection.</summary>
        private bool DrawCapture()
        {
            EditorGUILayout.LabelField("Step 1 of 4 — Capture a hierarchy", EditorStyles.boldLabel);
            source = (GameObject)EditorGUILayout.ObjectField("Root GameObject", source, typeof(GameObject), true);
            EditorGUILayout.HelpBox("Capture selectable containers and their components, using the same rule as automatic root selection. Prefab instances and their entire contents are excluded. The source stays untouched.", MessageType.Info);
            if (source != null && !UIHierarchyMoldCapture.IsValidSource(source))
                EditorGUILayout.HelpBox("Choose an ordinary scene RectTransform outside prefab instances and Prefab Mode. The root will not be replaced with an ancestor automatically.", MessageType.Warning);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Back")) return true;
                using (new EditorGUI.DisabledScope(!UIHierarchyMoldCapture.IsValidSource(source)))
                    if (GUILayout.Button("Capture and Continue"))
                    {
                        if (UIHierarchyMoldCapture.TryCapture(source, out UIHierarchyMold next, out string error))
                        {
                            ReleaseDraft(); draft = next; rootName = draft.nodes[0].name;
                            loaded = false; savedPath = null; feedback = null; stage = Step.Inspect;
                        }
                        else feedback = error;
                    }
            }
            return false;
        }

        /// <summary>Inspect the immutable snapshot before saving it or choosing any scene destination.</summary>
        private bool DrawInspect()
        {
            EditorGUILayout.LabelField("Step 2 of 4 — Review your mold", EditorStyles.boldLabel);
            bool valid = UIHierarchyMoldBuilder.ValidateMold(draft, out string error);
            if (!valid) EditorGUILayout.HelpBox(error, MessageType.Error);
            else
            {
                int count = 0;
                foreach (UIMoldNode node in draft.nodes) count += node.components.Count;
                EditorGUILayout.LabelField(draft.nodes.Count + " containers · " + count + " components", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("This is an independent snapshot. To reuse later hierarchy edits, capture a new mold.", EditorStyles.wordWrappedMiniLabel);
                showHierarchy = EditorGUILayout.Foldout(showHierarchy, "Hierarchy and components", true);
                if (showHierarchy)
                    for (int i = 0; i < Math.Min(draft.nodes.Count, 200); i++)
                    {
                        UIMoldNode node = draft.nodes[i];
                        int depth = 0;
                        for (int p = node.parent; p >= 0; p = draft.nodes[p].parent) depth++;
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(Math.Min(depth, 12) * 12);
                            EditorGUILayout.LabelField(node.name + (node.active ? "" : " (inactive)") + " · " + ComponentNames(node), EditorStyles.wordWrappedMiniLabel);
                        }
                    }
                if (showHierarchy && draft.nodes.Count > 200) EditorGUILayout.LabelField("Showing the first 200 objects; all " + draft.nodes.Count + " are captured.", EditorStyles.wordWrappedMiniLabel);
                DrawOmissions();
                if (GUILayout.Button("Save Mold for Reuse")) SaveMold();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Back"))
                {
                    if (loaded) return true;
                    stage = Step.Capture; feedback = null;
                }
                using (new EditorGUI.DisabledScope(!valid))
                    if (GUILayout.Button("Continue")) { stage = Step.Target; feedback = null; FollowCurrentSelection(); }
            }
            return false;
        }

        /// <summary>List component types without exposing assembly-qualified implementation identifiers in ordinary UI.</summary>
        private static string ComponentNames(UIMoldNode node)
        {
            string names = node.rectTransform ? "RectTransform" : "Transform";
            foreach (UIMoldComponent component in node.components) names += ", " + Type.GetType(component.typeName, false).Name;
            return names;
        }

        /// <summary>Always disclose removed references; detailed locations expand only when needed.</summary>
        private void DrawOmissions()
        {
            int count = draft.omissions == null ? 0 : draft.omissions.Count;
            EditorGUILayout.HelpBox("Only captured containers will be created. Excluded prefab branches stay empty; add your saved prefabs through Library afterward. External references on retained components are cleared.", MessageType.Info);
            if (count == 0) return;
            showOmissions = EditorGUILayout.Foldout(showOmissions, count + " excluded branches / cleared references / resets", true);
            if (showOmissions)
                foreach (string item in draft.omissions) EditorGUILayout.LabelField(item, EditorStyles.wordWrappedMiniLabel);
        }

        /// <summary>Choose a target and collision-free root name; the source root is never replaced.</summary>
        private void DrawTarget()
        {
            EditorGUILayout.LabelField("Step 3 of 4 — Choose a destination", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            target = (RectTransform)EditorGUILayout.ObjectField("Create under", target, typeof(RectTransform), true);
            if (EditorGUI.EndChangeCheck()) followSelection = false;
            bool next = EditorGUILayout.ToggleLeft("Follow Hierarchy selection on this step", followSelection);
            if (next != followSelection) { followSelection = next; FollowCurrentSelection(); }
            rootName = EditorGUILayout.TextField("New root name", rootName);
            bool valid = UIHierarchyMoldBuilder.Validate(draft, target, rootName, out string error);
            if (!valid) EditorGUILayout.HelpBox(error, MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Back")) { stage = Step.Inspect; feedback = null; }
                using (new EditorGUI.DisabledScope(!valid))
                    if (GUILayout.Button("Continue")) { stage = Step.Review; feedback = null; }
            }
        }

        /// <summary>Revalidate held input and present the final mutation separately from capture and saving.</summary>
        private void DrawReview()
        {
            EditorGUILayout.LabelField("Step 4 of 4 — Create from mold", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("New root", rootName);
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("Create under", target, typeof(RectTransform), true);
            bool valid = UIHierarchyMoldBuilder.Validate(draft, target, rootName, out string error);
            if (!valid) EditorGUILayout.HelpBox(error, MessageType.Error);
            else
            {
                EditorGUILayout.LabelField(draft.nodes.Count + " containers. Prefab controls and their children are not included.", EditorStyles.wordWrappedLabel);
                DrawOmissions();
            }
            EditorGUILayout.LabelField(AdvancedUIBindingIntegration.AutomaticGenerationEnabled
                ? "Automatic bindings: configured owning roots may update after creation."
                : "Manual bindings: configure and generate later in Binder.", EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Back")) { stage = Step.Target; feedback = null; }
                using (new EditorGUI.DisabledScope(!valid))
                    if (GUILayout.Button("Create Layout"))
                    {
                        if (UIHierarchyMoldBuilder.TryCreate(draft, target, rootName, out created, out error))
                        {
                            completion = "Created " + draft.nodes.Count + " objects under " + target.name + ".";
                            feedback = null; stage = Step.Complete;
                        }
                        else feedback = error;
                    }
            }
        }

        /// <summary>Offer explicit optional next actions without repeating the Create action.</summary>
        private bool DrawComplete()
        {
            EditorGUILayout.LabelField("Layout created", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Last operation: " + completion, MessageType.Info);
            EditorGUILayout.LabelField("Edit this layout normally in Unity. Source or mold changes will not update it. Capture it again to save a revised structure.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("Scene Undo and generated C# have separate recovery.", EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUI.DisabledScope(created == null))
                if (GUILayout.Button("Open Binder"))
                    try { AdvancedUIToolkitWorkspaceWindow.OpenForTarget(created); }
                    catch (Exception exception) { feedback = exception.Message; }
            if (GUILayout.Button("Save Mold for Reuse")) SaveMold();
            if (!string.IsNullOrEmpty(savedPath)) EditorGUILayout.LabelField("Saved: " + savedPath, EditorStyles.wordWrappedMiniLabel);
            return GUILayout.Button("Start Another Layout");
        }

        /// <summary>Save a new asset without overwriting a previous mold or changing the draft.</summary>
        private void SaveMold()
        {
            if (!UIHierarchyMoldBuilder.ValidateMold(draft, out string error)) { feedback = error; return; }
            string path = EditorUtility.SaveFilePanelInProject("Save Hierarchy Mold", "LayoutMold", "asset", "Choose a new asset path.");
            if (string.IsNullOrEmpty(path)) return;
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || File.Exists(path) || Directory.Exists(path) || !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
            { feedback = "Choose a new .asset path inside Assets. Existing files are never overwritten."; return; }
            UIHierarchyMold copy = CreateInstance<UIHierarchyMold>();
            try
            {
                EditorUtility.CopySerialized(draft, copy); copy.hideFlags = HideFlags.None;
                AssetDatabase.CreateAsset(copy, path);
                savedPath = path; feedback = "Mold saved: " + path;
            }
            catch (Exception exception)
            {
                if (!AssetDatabase.Contains(copy)) DestroyImmediate(copy);
                feedback = "Could not save mold: " + exception.Message;
            }
        }
    }
}
