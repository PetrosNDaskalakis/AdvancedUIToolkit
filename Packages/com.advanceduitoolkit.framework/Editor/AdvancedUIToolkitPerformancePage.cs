using System;
using UnityEditor;
using UnityEngine;

namespace AdvancedUIToolkit.Editor
{
    /// <summary>Displays opt-in performance measurements in the Advanced UI Toolkit Workspace.</summary>
    internal sealed class AdvancedUIToolkitPerformancePage
    {
        /// <summary>Supported synthetic control sizes.</summary>
        private static readonly int[] ControlCounts = { 16, 64, 128 };

        /// <summary>Labels for synthetic control sizes.</summary>
        private static readonly string[] ControlLabels = { "16 controls", "64 controls", "128 controls" };

        /// <summary>Supported cached reference operation counts.</summary>
        private static readonly int[] IterationCounts = { 10000, 50000, 100000 };

        /// <summary>Labels for cached reference operation counts.</summary>
        private static readonly string[] IterationLabels = { "10,000", "50,000", "100,000" };

        /// <summary>Selected UI size.</summary>
        private int controlIndex = 1;

        /// <summary>Selected operation count.</summary>
        private int iterationIndex = 1;

        /// <summary>Most recent local report.</summary>
        private AdvancedUIToolkitPerformanceBenchmarks.Report report;

        /// <summary>Latest error, if the benchmark could not run.</summary>
        private string error;

        /// <summary>Report scrolling position.</summary>
        private Vector2 scroll;

        /// <summary>Draws the performance tab and its report.</summary>
        internal void Draw()
        {
            EditorGUILayout.LabelField("Advanced UI Toolkit / Performance", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Opt-in synthetic tests for references and event subscriptions. " +
                "No prefabs, project assets or generated C# files are modified. " +
                "Run while the Editor is idle for more comparable results.", MessageType.Info);
            controlIndex = EditorGUILayout.Popup("Synthetic UI size", controlIndex, ControlLabels);
            iterationIndex = EditorGUILayout.Popup("Cached lookups per sample", iterationIndex, IterationLabels);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Run Benchmarks")) RunBenchmarks();
                using (new EditorGUI.DisabledScope(report == null))
                {
                    if (GUILayout.Button("Copy Report")) EditorGUIUtility.systemCopyBuffer = report.ToText();
                }
                if (GUILayout.Button("Open Test Runner"))
                    EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
            }
            if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
            if (report == null)
            {
                EditorGUILayout.HelpBox("No benchmarks have been run yet.", MessageType.None);
                return;
            }
            EditorGUILayout.Space();
            foreach (AdvancedUIToolkitPerformanceBenchmarks.Metric metric in report.Metrics)
                EditorGUILayout.LabelField(metric.Name,
                    metric.MicrosecondsPerOperation.ToString("F3") + " us/op");
            EditorGUILayout.HelpBox("Synthetic medians are not frame-time guarantees. " +
                "GC counts are not bytes allocated.", MessageType.None);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.TextArea(report.ToText(), GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        /// <summary>Performs measurements with a synthetic temporary hierarchy.</summary>
        private void RunBenchmarks()
        {
            error = null;
            report = null;
            try
            {
                report = AdvancedUIToolkitPerformanceBenchmarks.Run(
                    ControlCounts[Mathf.Clamp(controlIndex, 0, ControlCounts.Length - 1)],
                    IterationCounts[Mathf.Clamp(iterationIndex, 0, IterationCounts.Length - 1)]);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                Debug.LogException(exception);
            }
        }
    }
}
