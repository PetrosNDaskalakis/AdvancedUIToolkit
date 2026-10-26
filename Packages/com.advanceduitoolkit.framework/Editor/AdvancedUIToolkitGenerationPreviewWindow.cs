using System;
using UnityEditor;
using UnityEngine;

namespace AdvancedUIToolkit.Editor
{
    /// <summary>
    /// Read-only source review for Advanced UI Toolkit binding generation.
    /// Opening this window does not write C# files or import assets.
    /// </summary>
    internal sealed class AdvancedUIToolkitGenerationPreviewWindow : EditorWindow
    {
        /// <summary>Destination of the proposed generated source.</summary>
        private string destination;

        /// <summary>Proposed source captured when the user requested a preview.</summary>
        private string proposed;

        /// <summary>Existing source at the same destination, or null for a new file.</summary>
        private string current;

        /// <summary>Cached comparison output.</summary>
        private string comparison;

        /// <summary>Currently displayed preview tab.</summary>
        private int tab;

        /// <summary>Scroll offset within the code preview.</summary>
        private Vector2 scrollPosition;

        /// <summary>Opens a preview using in-memory source without performing generation.</summary>
        /// <param name="path">Project-relative destination of the generated file.</param>
        /// <param name="nextSource">New source that would be written after confirmation in AutoBinder.</param>
        /// <param name="existingSource">Current file contents or null.</param>
        internal static void Open(string path, string nextSource, string existingSource)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("Missing destination.", nameof(path));
            if (nextSource == null) throw new ArgumentNullException(nameof(nextSource));
            var window = GetWindow<AdvancedUIToolkitGenerationPreviewWindow>("Binding C# Preview");
            window.destination = path;
            window.proposed = nextSource;
            window.current = existingSource;
            window.comparison = AdvancedUIToolkitGenerationDiff.Compare(existingSource, nextSource);
            window.tab = 0;
            window.scrollPosition = Vector2.zero;
            window.minSize = new Vector2(460f, 340f);
            window.Show();
            window.Focus();
        }

        /// <summary>Draws the source review with no write or compilation actions.</summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Advanced UI Toolkit / Generated C# Preview", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(destination ?? "No destination selected",
                EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight + 3f));
            EditorGUILayout.HelpBox("Read-only preview. No code has been saved or compiled. " +
                "Return to Workspace > Bindings to generate when ready.", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                int selected = GUILayout.Toolbar(tab, new[] { "Proposed", "Changes", "Current" },
                    EditorStyles.toolbarButton);
                if (selected != tab)
                {
                    tab = selected;
                    scrollPosition = Vector2.zero;
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Copy", EditorStyles.toolbarButton, GUILayout.Width(55f)))
                    EditorGUIUtility.systemCopyBuffer = GetVisibleSource();
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.SelectableLabel(GetVisibleSource(), EditorStyles.textArea,
                GUILayout.MinHeight(Mathf.Max(300f, (GetVisibleSource().Split('\n').Length + 2) *
                    EditorGUIUtility.singleLineHeight)));
            EditorGUILayout.EndScrollView();
        }

        /// <summary>Returns the source associated with the selected preview tab.</summary>
        /// <returns>In-memory source or an explanation when the destination does not exist.</returns>
        private string GetVisibleSource()
        {
            switch (tab)
            {
                case 1: return comparison ?? "";
                case 2: return current ?? "No generated file exists at this path yet.";
                default: return proposed ?? "";
            }
        }
    }
}
