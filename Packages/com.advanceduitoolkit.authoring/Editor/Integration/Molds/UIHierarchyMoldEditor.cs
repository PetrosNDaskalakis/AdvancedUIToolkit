using UnityEditor;
using UnityEngine;

namespace AdvancedUIAuthoring.Integration.Editor
{
    /// <summary>Keep captured mold data read-only in the Inspector and reuse the single Layouts workflow.</summary>
    [CustomEditor(typeof(UIHierarchyMold))]
    public sealed class UIHierarchyMoldEditor : UnityEditor.Editor
    {
        /// <summary>Show a concise snapshot summary and open a detached working copy.</summary>
        public override void OnInspectorGUI()
        {
            var mold = (UIHierarchyMold)target;
            EditorGUILayout.LabelField("Hierarchy Mold", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Objects", mold.nodes == null ? "0" : mold.nodes.Count.ToString());
            EditorGUILayout.HelpBox("Selectable containers captured from a scene; prefab branches are excluded. Open in Layouts to review and create the structure. Recapture an edited hierarchy to save a new mold.", MessageType.Info);
            if (!UIHierarchyMoldBuilder.ValidateMold(mold, out string error)) EditorGUILayout.HelpBox(error, MessageType.Warning);
            if (GUILayout.Button("Open in Layouts")) UIAuthoringLayoutsWindow.OpenMold(mold);
        }
    }
}
