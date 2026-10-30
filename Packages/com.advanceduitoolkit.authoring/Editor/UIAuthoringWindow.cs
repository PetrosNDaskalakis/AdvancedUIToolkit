using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AdvancedUIAuthoring.Editor
{
    /// <summary>Compact UI schema picker, validation feedback, and scene generation window.</summary>
    public sealed class UIAuthoringWindow : EditorWindow
    {
        /// <summary>JSON schema asset selected by the developer.</summary>
        private TextAsset schema;

        /// <summary>Optional reusable JSON stylesheet selected for this generation.</summary>
        private TextAsset stylesheet;

        /// <summary>Scene container where generated objects will be parented.</summary>
        private RectTransform targetParent;

        /// <summary>Whether normal Unity Hierarchy selections should update the active parent.</summary>
        private bool followSelection = true;

        /// <summary>Last inspected schema and its validation result.</summary>
        private UIAuthoringValidationResult validation;

        /// <summary>Last message shown below the creation controls.</summary>
        private string message = string.Empty;

        /// <summary>Editor GUI scroll offset for multi-error schemas.</summary>
        private Vector2 scroll;

        /// <summary>Open the Authoring workspace without touching Binder or UI Library.</summary>
        [MenuItem("Tools/Advanced UI Toolkit/Compatibility/Native Authoring")]
        public static void Open()
        {
            UIAuthoringWindow window = GetWindow<UIAuthoringWindow>("Native Authoring");
            window.minSize = new Vector2(320f, 220f);
            window.Show();
        }

        /// <summary>Adopt the current valid hierarchy selection when the window opens.</summary>
        private void OnEnable()
        {
            if (followSelection) targetParent = FindContainer(Selection.activeGameObject);
        }

        /// <summary>Track ordinary containers but never select linked prefab instances as parents.</summary>
        private void OnSelectionChange()
        {
            if (!followSelection) return;
            RectTransform next = FindContainer(Selection.activeGameObject);
            if (next != null) targetParent = next;
            validation = null;
            Repaint();
        }

        /// <summary>Nearest valid scene RectTransform, skipping prefab instances and assets.</summary>
        private static RectTransform FindContainer(GameObject selection)
        {
            Transform cursor = selection != null ? selection.transform : null;
            while (cursor != null)
            {
                RectTransform rectangle = cursor.GetComponent<RectTransform>();
                if (rectangle != null && !EditorUtility.IsPersistent(rectangle) &&
                    !PrefabUtility.IsPartOfPrefabInstance(cursor.gameObject)) return rectangle;
                cursor = cursor.parent;
            }
            return null;
        }

        /// <summary>Draw the small authoring workflow with no test or benchmark controls.</summary>
        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox("Native compatibility workflow. For schemas using saved prefabs, open Tools > Advanced UI Toolkit > Layouts.", MessageType.Info);
            EditorGUILayout.LabelField("Native Authoring", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Native uGUI · Schema v1", EditorStyles.miniLabel);
            EditorGUILayout.Space(5f);

            EditorGUI.BeginChangeCheck();
            schema = (TextAsset)EditorGUILayout.ObjectField("Schema", schema, typeof(TextAsset), false);
            if (EditorGUI.EndChangeCheck()) { validation = null; message = string.Empty; }

            EditorGUI.BeginChangeCheck();
            stylesheet = (TextAsset)EditorGUILayout.ObjectField("Stylesheet (Optional)",
                stylesheet, typeof(TextAsset), false);
            if (EditorGUI.EndChangeCheck()) { validation = null; message = string.Empty; }

            EditorGUI.BeginChangeCheck();
            followSelection = EditorGUILayout.ToggleLeft("Follow Hierarchy Selection", followSelection);
            if (EditorGUI.EndChangeCheck() && followSelection)
            {
                RectTransform selection = FindContainer(Selection.activeGameObject);
                if (selection != null) targetParent = selection;
                validation = null;
            }
            EditorGUI.BeginChangeCheck();
            targetParent = (RectTransform)EditorGUILayout.ObjectField("Target Parent", targetParent,
                typeof(RectTransform), true);
            if (EditorGUI.EndChangeCheck()) validation = null;

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate", GUILayout.Height(27f))) ValidateSelected();
                using (new EditorGUI.DisabledScope(schema == null || targetParent == null))
                {
                    if (GUILayout.Button("Generate UI", GUILayout.Height(27f))) GenerateSelected();
                }
            }

            EditorGUILayout.Space(6f);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (validation != null)
            {
                if (validation.Succeeded)
                {
                    EditorGUILayout.HelpBox("Schema valid: " + validation.NodeCount + " nodes ready.",
                        MessageType.Info);
                }
                else
                {
                    foreach (string issue in validation.Errors)
                    {
                        EditorGUILayout.HelpBox(issue, MessageType.Error);
                    }
                }
            }
            if (!string.IsNullOrEmpty(message))
            {
                EditorGUILayout.HelpBox(message, MessageType.Info);
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>Parse and check the selected TextAsset without editing the scene.</summary>
        private UIAuthoringDocument ValidateSelected()
        {
            validation = null;
            message = string.Empty;
            if (schema == null)
            {
                validation = new UIAuthoringValidationResult(
                    new System.Collections.Generic.List<string> { "Assign a JSON TextAsset first." }, 0);
                return null;
            }
            UIAuthoringDocument document;
            string error;
            if (!UIAuthoringSchemaParser.TryParse(schema.text, out document, out error))
            {
                validation = new UIAuthoringValidationResult(
                    new System.Collections.Generic.List<string> { error }, 0);
                return null;
            }
            UIAuthoringStyleSheet sheet;
            string sheetError;
            if (!TryReadStylesheet(out sheet, out sheetError))
            {
                validation = new UIAuthoringValidationResult(
                    new System.Collections.Generic.List<string> { sheetError }, 0);
                return null;
            }
            validation = UIAuthoringValidator.Validate(document, targetParent, sheet);
            return document;
        }

        /// <summary>Read the optional stylesheet, without modifying Unity objects.</summary>
        private bool TryReadStylesheet(out UIAuthoringStyleSheet sheet, out string error)
        {
            sheet = null;
            error = string.Empty;
            return stylesheet == null ||
                UIAuthoringStyleSheetParser.TryParse(stylesheet.text, out sheet, out error);
        }

        /// <summary>Generate only after validating the current exact schema content.</summary>
        private void GenerateSelected()
        {
            UIAuthoringDocument document = ValidateSelected();
            if (document == null || validation == null || !validation.Succeeded) return;
            UIAuthoringStyleSheet sheet;
            string sheetError;
            if (!TryReadStylesheet(out sheet, out sheetError))
            {
                message = sheetError;
                return;
            }
            UIAuthoringGenerationResult result = UIAuthoringGenerator.Generate(document, targetParent, sheet);
            if (!result.Succeeded)
            {
                message = string.Empty;
                Debug.LogError("Advanced UI Authoring: " + result.Error);
                EditorUtility.DisplayDialog("UI Authoring", result.Error, "OK");
                return;
            }
            message = "Created " + result.CreatedElements.Count + " native UI objects. Ctrl+Z undoes the operation.";
            Debug.Log("Advanced UI Authoring: " + message, targetParent);
        }
    }
}
