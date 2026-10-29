using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Editor
{
    /// <summary>
    /// Editor-only text override requested when instantiating a prefab through UI Library.
    /// A disabled request preserves the source prefab's original text, whereas an enabled
    /// request with an empty string intentionally clears the selected text component.
    /// </summary>
    internal struct UIPrefabTextOverrideRequest
    {
        /// <summary>Whether the user explicitly requested a text override.</summary>
        internal bool Enabled;
        /// <summary>Zero-based index of the selected supported text component in prefab traversal order.</summary>
        internal int TargetIndex;
        /// <summary>Replacement text, including an intentionally empty string.</summary>
        internal string Text;
        /// <summary>
        /// When true, the source component must still be present on the chosen prefab.
        /// An obsolete selection must never fall back to a different component by index.
        /// </summary>
        internal bool RequireSourceComponent;
        /// <summary>Selected source-prefab component (not the new instance component).</summary>
        internal Component SourceComponent;
    }

    /// <summary>
    /// Identifies serialized text controls in a prefab and applies one text override to
    /// an instantiated prefab only. TextMeshPro is discovered by reflection so that
    /// Advanced UI Toolkit never requires a compile-time dependency on TMP.
    /// </summary>
    internal static class UIPrefabTextOverride
    {
        /// <summary>A supported text component and its authoring-time description.</summary>
        internal sealed class Target
        {
            /// <summary>Actual Unity component on the supplied source or instance.</summary>
            internal Component Component;
            /// <summary>Readable hierarchy path and text-component type for a selection popup.</summary>
            internal string Label;
            /// <summary>Text currently stored on the component.</summary>
            internal string CurrentText;
        }

        /// <summary>
        /// Collects text components on the prefab root and all descendants, including
        /// inactive controls. A component receives a stable index within this snapshot.
        /// </summary>
        /// <param name="prefab">Prefab asset or newly instantiated prefab root.</param>
        /// <returns>Supported text targets ordered by hierarchy traversal.</returns>
        internal static List<Target> FindTargets(GameObject prefab)
        {
            List<Target> targets = new List<Target>();
            if (prefab == null) return targets;
            foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
            {
                if (component == null || !IsSupported(component)) continue;
                targets.Add(new Target
                {
                    Component = component,
                    Label = RelativePath(prefab.transform, component.transform) +
                        " (" + component.GetType().Name + ")",
                    CurrentText = ReadText(component)
                });
            }
            return targets;
        }

        /// <summary>
        /// Determines whether a prefab contains at least one supported label without
        /// creating target descriptions or reading the original label text. Factory
        /// uses this on disabled-override repaints instead of full target discovery.
        /// </summary>
        /// <param name="prefab">Prefab root to examine, including inactive descendants.</param>
        /// <returns>True when legacy Text or a supported TMP text component exists.</returns>
        internal static bool HasTargets(GameObject prefab)
        {
            if (prefab == null) return false;
            foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
            {
                if (component != null && IsSupported(component)) return true;
            }
            return false;
        }

        /// <summary>Checks that an override still identifies its intended source label.</summary>
        /// <param name="prefab">Saved source prefab whose hierarchy may have changed.</param>
        /// <param name="request">Disabled, index-only, or component-identity-checked request.</param>
        /// <param name="error">Reason for rejection, if any.</param>
        /// <returns>True if disabled or the requested label still exists.</returns>
        internal static bool Validate(GameObject prefab, UIPrefabTextOverrideRequest request,
            out string error)
        {
            return TryResolveSourceIndex(prefab, request, out _, out error);
        }

        /// <summary>
        /// Resolves the selected source label to its CURRENT traversal index. The Factory
        /// uses that index for the freshly instantiated hierarchy. This supports reordered
        /// children and duplicate names, and fails closed when a selected label is deleted.
        /// </summary>
        /// <param name="source">Saved source prefab, not an instance.</param>
        /// <param name="request">Request made from the authoring interface.</param>
        /// <param name="index">Current source traversal index, or -1 on failure.</param>
        /// <param name="error">Human-readable error for a stale selection.</param>
        /// <returns>True if the selection is valid (or the override is disabled).</returns>
        internal static bool TryResolveSourceIndex(GameObject source,
            UIPrefabTextOverrideRequest request, out int index, out string error)
        {
            index = -1;
            error = null;
            if (!request.Enabled) return true;
            // The creation path needs only a stable traversal index. Avoid allocating
            // Target objects, hierarchy-path strings, and copied text for every label.
            // Preserve the same ordering as FindTargets, including inactive descendants.
            if (source != null && (!request.RequireSourceComponent || request.SourceComponent != null))
            {
                int currentIndex = 0;
                foreach (Component component in source.GetComponentsInChildren<Component>(true))
                {
                    if (component == null || !IsSupported(component)) continue;
                    if (request.RequireSourceComponent
                        ? component == request.SourceComponent
                        : currentIndex == request.TargetIndex)
                    {
                        index = currentIndex;
                        return true;
                    }
                    currentIndex++;
                }
            }
            // Do not fall back to a possibly shifted index if source identity is lost.
            error = "The selected prefab text component no longer exists. Select a new text target.";
            return false;
        }

        /// <summary>
        /// Applies an override using the traversal index resolved from the source prefab
        /// immediately before instantiation. Retained for direct, index-based internal use.
        /// </summary>
        /// <param name="instance">New prefab instance or test GameObject.</param>
        /// <param name="request">Text to apply; must have been source-validated by Factory.</param>
        /// <param name="error">Reason application failed.</param>
        /// <returns>Whether the operation succeeded or was disabled.</returns>
        internal static bool Apply(GameObject instance, UIPrefabTextOverrideRequest request,
            out string error)
        {
            return Apply(instance, request, request.TargetIndex, out error);
        }

        /// <summary>
        /// Changes one text component on an instance, preserving the linked prefab asset.
        /// Source identity is verified by Factory before this method is called; only the
        /// freshly created instance is modified using Undo and prefab property recording.
        /// </summary>
        /// <param name="instance">New prefab instance.</param>
        /// <param name="request">Explicit optional text override.</param>
        /// <param name="sourceIndex">Current traversal index resolved from the source.</param>
        /// <param name="error">Reason application failed.</param>
        /// <returns>True when disabled or successfully applied.</returns>
        internal static bool Apply(GameObject instance, UIPrefabTextOverrideRequest request,
            int sourceIndex, out string error)
        {
            error = null;
            if (!request.Enabled) return true;
            // A newly instantiated hierarchy needs one component, not UI labels,
            // paths and current-text snapshots for all other supported labels.
            Component component = FindComponentAtIndex(instance, sourceIndex);
            if (component == null)
            {
                error = "The selected prefab text component no longer exists. Select a new text target.";
                return false;
            }
            string desiredText = request.Text ?? string.Empty;
            try
            {
                // Resolve the optional reflected TMP property before recording changes.
                PropertyInfo property = component is Text ? null : FindTextProperty(component.GetType());
                if (!(component is Text) && (property == null || !property.CanWrite))
                {
                    error = "The selected text component cannot be edited.";
                    return false;
                }
                Undo.RecordObject(component, "Advanced UI Toolkit: Override Displayed Text");
                if (component is Text legacy)
                    legacy.text = desiredText;
                else
                    property.SetValue(component, desiredText, null);

                if (PrefabUtility.IsPartOfPrefabInstance(component))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                EditorUtility.SetDirty(component);
                return true;
            }
            catch (Exception exception)
            {
                error = "Cannot override prefab text: " + exception.GetBaseException().Message;
                return false;
            }
        }

        /// <summary>
        /// Locates a supported instance text component at a source-validated index
        /// without allocating selection-popup labels or reading other text fields.
        /// </summary>
        /// <param name="root">New instance or test root.</param>
        /// <param name="targetIndex">Index in supported text traversal order.</param>
        /// <returns>The selected component, or null if the hierarchy is incompatible.</returns>
        private static Component FindComponentAtIndex(GameObject root, int targetIndex)
        {
            if (root == null || targetIndex < 0) return null;
            int currentIndex = 0;
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null || !IsSupported(component)) continue;
                if (currentIndex++ == targetIndex) return component;
            }
            return null;
        }

        /// <summary>Checks whether this component is a supported legacy or optional TMP text element.</summary>
        /// <param name="component">Component to inspect.</param>
        /// <returns>True when it exposes text in a supported way.</returns>
        private static bool IsSupported(Component component)
        {
            if (component is Text) return true;
            for (Type type = component.GetType(); type != null; type = type.BaseType)
            {
                if (type.FullName == "TMPro.TMP_Text")
                    return FindTextProperty(component.GetType()) != null;
            }
            return false;
        }

        /// <summary>Finds the public text property on a TMP component without a TMP assembly reference.</summary>
        /// <param name="type">Actual component type.</param>
        /// <returns>The public text property, or null.</returns>
        private static PropertyInfo FindTextProperty(Type type)
        {
            PropertyInfo property = type.GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
            return property != null && property.PropertyType == typeof(string) ? property : null;
        }

        /// <summary>Reads original text from either supported text implementation.</summary>
        /// <param name="component">Supported text component.</param>
        /// <returns>Original text or an empty string.</returns>
        private static string ReadText(Component component)
        {
            if (component is Text legacy) return legacy.text ?? string.Empty;
            PropertyInfo property = FindTextProperty(component.GetType());
            return property == null ? string.Empty : property.GetValue(component, null) as string ?? string.Empty;
        }

        /// <summary>Formats a relative hierarchy path to distinguish multiple text targets.</summary>
        /// <param name="root">Prefab root transform.</param>
        /// <param name="child">Transform that owns the text component.</param>
        /// <returns>Path relative to the prefab root.</returns>
        private static string RelativePath(Transform root, Transform child)
        {
            if (root == child) return "Root";
            List<string> parts = new List<string>();
            for (Transform node = child; node != null && node != root; node = node.parent)
                parts.Add(node.name);
            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }
    }
}
