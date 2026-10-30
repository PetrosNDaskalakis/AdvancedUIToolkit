using UnityEditor;
using UnityEngine;

namespace AdvancedUIAuthoring.Integration.Editor
{
    /// <summary>Read-only record Inspector; baseline contents are not edited accidentally through default fields.</summary>
    [CustomEditor(typeof(UIAuthoringGenerationRecord))]
    public sealed class UIAuthoringGenerationRecordEditor : UnityEditor.Editor
    {
        /// <summary>Display record provenance and offer the existing preview window.</summary>
        public override void OnInspectorGUI()
        {
            var record = (UIAuthoringGenerationRecord)target;
            EditorGUILayout.LabelField("UI Generation Baseline", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Recipe", record.Recipe, typeof(UIAuthoringIntegrationRecipe), false);
            EditorGUILayout.LabelField("Scene", AssetDatabase.GUIDToAssetPath(record.sceneGuid), EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("Schema nodes", record.NodeCount.ToString());
            EditorGUILayout.LabelField("Captured UTC", record.CapturedUtc, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.HelpBox("Open the recorded scene, then preview changes. This asset is Editor-only and never applies scene changes.", MessageType.Info);
            if (GUILayout.Button("Open Change Preview")) UIAuthoringIntegrationWindow.OpenRecord(record);
        }
    }
}
