using AdvancedUIToolkit;
using AdvancedUIToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace AdvancedUIAuthoring.Integration.Editor
{
    /// <summary>Recipe Inspector with source-identity label choices for explicit mappings.</summary>
    [CustomEditor(typeof(UIAuthoringIntegrationRecipe))]
    public sealed class UIAuthoringIntegrationRecipeEditor : UnityEditor.Editor
    {
        /// <summary>Draw standard recipe settings and optional per-mapping source label pickers.</summary>
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Saved JSON Layout Setup", EditorStyles.boldLabel);
            DrawDefaultInspector();
            var recipe = (UIAuthoringIntegrationRecipe)target;
            if (GUILayout.Button("Open in Layouts — JSON Compatibility"))
            {
                UIAuthoringLayoutsWindow.OpenSetup(recipe);
            }
            DrawTextTargets(recipe);
        }

        /// <summary>Draw mappings for a transient draft without exposing legacy Binder configuration.</summary>
        public static void DrawMappingControls(UIAuthoringIntegrationRecipe recipe)
        {
            using (var serialized = new SerializedObject(recipe))
            {
                serialized.Update();
                EditorGUILayout.PropertyField(serialized.FindProperty("mappings"), new GUIContent("Per-node overrides"), true);
                serialized.ApplyModifiedProperties();
            }
            DrawTextTargets(recipe);
        }

        /// <summary>Choose source labels by prefab component identity, preserving existing label selection.</summary>
        private static void DrawTextTargets(UIAuthoringIntegrationRecipe recipe)
        {
            if (recipe.profile == null || recipe.mappings == null) return;
            foreach (UIAuthoringPrefabMapping mapping in recipe.mappings)
            {
                if (mapping == null || mapping.textMode != UIAuthoringTextMode.ExplicitSource) continue;
                UIComponentTemplate template;
                string error;
                if (!AdvancedUIEditorIntegration.TryResolveTemplate(recipe.profile, mapping.kind,
                    mapping.customTemplateId, out template, out error) || template == null)
                {
                    EditorGUILayout.HelpBox(mapping.nodeId + ": " + error, MessageType.Warning);
                    continue;
                }
                var targets = AdvancedUIEditorIntegration.GetTextTargets(template.Prefab);
                string[] options = new string[targets.Count + 1];
                options[0] = "Choose source label (required)";
                int current = 0;
                for (int i = 0; i < targets.Count; i++)
                {
                    options[i + 1] = targets[i].Label;
                    if (mapping.sourceText != null && targets[i].SourceComponent == mapping.sourceText) current = i + 1;
                }
                int selected = EditorGUILayout.Popup(mapping.nodeId + " label", current, options);
                if (selected != current)
                {
                    Undo.RecordObject(recipe, "Choose authoring text source");
                    mapping.sourceText = selected > 0 ? targets[selected - 1].SourceComponent : null;
                    EditorUtility.SetDirty(recipe);
                }
            }
        }
    }
}
