using UnityEditor;
using UnityEngine;

namespace AdvancedUIAuthoring.Integration.Editor
{
    /// <summary>Dedicated window hosting the guided layout workflow without exposing later steps early.</summary>
    public sealed class UIAuthoringLayoutsWindow : EditorWindow
    {
        /// <summary>Window-owned wizard state retained across ordinary domain reload.</summary>
        [SerializeField] private UIAuthoringLayoutView view;
        /// <summary>Scroll position for narrow docks or advanced mappings.</summary>
        [SerializeField] private Vector2 scroll;

        /// <summary>Open the primary container-mold workflow.</summary>
        [MenuItem("Tools/Advanced UI Toolkit/Layouts", false, 3)]
        public static void Open()
        {
            var window = GetWindow<UIAuthoringLayoutsWindow>("Layouts");
            window.Show();
            window.Focus();
        }

        /// <summary>Open JSON creation and saved setups in labelled compatibility access, preserving working inputs.</summary>
        [MenuItem("Tools/Advanced UI Toolkit/Compatibility/JSON Layouts")]
        public static void OpenCompatibility()
        {
            var window = GetWindow<UIAuthoringLayoutsWindow>("Layouts");
            window.EnsureView();
            window.view.OpenCompatibility();
            window.Show();
            window.Focus();
        }

        /// <summary>Open a saved setup as a working copy at its first incomplete step.</summary>
        public static void OpenSetup(UIAuthoringIntegrationRecipe setup)
        {
            if (setup == null) return;
            var window = GetWindow<UIAuthoringLayoutsWindow>("Layouts");
            window.EnsureView();
            window.view.LoadSetup(setup);
            window.Show();
            window.Focus();
        }

        /// <summary>Open a saved mold as a detached working copy in the same Layouts window.</summary>
        public static void OpenMold(UIHierarchyMold mold)
        {
            if (mold == null) return;
            var window = GetWindow<UIAuthoringLayoutsWindow>("Layouts");
            window.EnsureView();
            window.view.LoadMold(mold);
            window.Show();
            window.Focus();
        }

        /// <summary>Restore the wizard and subscribe to feedback invalidation events.</summary>
        private void OnEnable()
        {
            titleContent = new GUIContent("Layouts");
            minSize = new Vector2(320, 300);
            EnsureView();
            EditorApplication.projectChanged += Invalidate;
            Undo.undoRedoPerformed += Invalidate;
            Undo.postprocessModifications += OnPropertyChanges;
        }

        /// <summary>Unsubscribe during closure or reload without destroying serialized draft state.</summary>
        private void OnDisable()
        {
            EditorApplication.projectChanged -= Invalidate;
            Undo.undoRedoPerformed -= Invalidate;
            Undo.postprocessModifications -= OnPropertyChanges;
        }

        /// <summary>Release the temporary draft when this window is actually closed.</summary>
        private void OnDestroy()
        {
            if (view != null) DestroyImmediate(view);
        }

        /// <summary>Create one private wizard without a Library registration or dependency.</summary>
        private void EnsureView()
        {
            if (view != null) return;
            view = CreateInstance<UIAuthoringLayoutView>();
            view.hideFlags = HideFlags.HideAndDontSave;
        }

        /// <summary>Follow hierarchy selection only while the wizard is choosing a target.</summary>
        private void OnSelectionChange()
        {
            if (view != null) view.FollowCurrentSelection();
            Repaint();
        }

        /// <summary>Invalidate validation after scene changes without dismissing completion history.</summary>
        private void OnHierarchyChange() { Invalidate(); }

        /// <summary>Observe Inspector edits without modifying their Undo records.</summary>
        private UndoPropertyModification[] OnPropertyChanges(UndoPropertyModification[] modifications)
        {
            Invalidate();
            return modifications;
        }

        /// <summary>Clear cached validation after edits while preserving the working draft.</summary>
        private void Invalidate()
        {
            if (view != null) view.Invalidate();
            Repaint();
        }

        /// <summary>Draw only the current wizard step; context controls belong to their respective steps.</summary>
        private void OnGUI()
        {
            EnsureView();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            view.Draw();
            EditorGUILayout.EndScrollView();
        }
    }
}
