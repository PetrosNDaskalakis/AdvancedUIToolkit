using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdvancedUIToolkit.Editor
{
    /// <summary>Produces a line-based, read-only comparison for code generation previews.</summary>
    internal static class AdvancedUIToolkitGenerationDiff
    {
        /// <summary>Limits the comparison to avoid slow quadratic work on very large files.</summary>
        private const int MaximumLines = 500;

        /// <summary>Builds a simple unified-style line comparison.</summary>
        /// <param name="previous">Previous generated source, or null for a new file.</param>
        /// <param name="proposed">Proposed generated source.</param>
        /// <returns>Lines prefixed with a space (unchanged), minus (removed), or plus (added).</returns>
        internal static string Compare(string previous, string proposed)
        {
            if (proposed == null) throw new ArgumentNullException(nameof(proposed));
            if (previous == proposed) return "No source changes detected.";
            if (previous == null)
                return "New file — no existing generated source.\n\n" +
                    string.Join("\n", Split(proposed).Select(line => "+ " + line));

            string[] oldLines = Split(previous);
            string[] newLines = Split(proposed);
            if (oldLines.Length > MaximumLines || newLines.Length > MaximumLines)
                return "Source exceeds the 500-line comparison limit. " +
                    "Use the Proposed and Current tabs to inspect the full files.";

            int[,] lcs = new int[oldLines.Length + 1, newLines.Length + 1];
            for (int i = oldLines.Length - 1; i >= 0; i--)
            {
                for (int j = newLines.Length - 1; j >= 0; j--)
                {
                    lcs[i, j] = oldLines[i] == newLines[j]
                        ? 1 + lcs[i + 1, j + 1]
                        : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
                }
            }

            var output = new StringBuilder();
            int oldIndex = 0;
            int newIndex = 0;
            while (oldIndex < oldLines.Length || newIndex < newLines.Length)
            {
                if (oldIndex < oldLines.Length && newIndex < newLines.Length &&
                    oldLines[oldIndex] == newLines[newIndex])
                {
                    output.Append("  ").AppendLine(oldLines[oldIndex]);
                    oldIndex++;
                    newIndex++;
                }
                else if (newIndex < newLines.Length &&
                    (oldIndex == oldLines.Length || lcs[oldIndex, newIndex + 1] >= lcs[oldIndex + 1, newIndex]))
                {
                    output.Append("+ ").AppendLine(newLines[newIndex++]);
                }
                else
                {
                    output.Append("- ").AppendLine(oldLines[oldIndex++]);
                }
            }
            return output.ToString();
        }

        /// <summary>Splits source text into lines while normalizing line endings.</summary>
        /// <param name="source">C# source text.</param>
        /// <returns>Lines without an extra trailing empty line.</returns>
        private static string[] Split(string source)
        {
            string normalized = source.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd('\n');
            return normalized.Split('\n');
        }
    }
}
