using UnityEditor;
using UnityEngine;

namespace AdvancedUIToolkit.Editor
{
    /// <summary>
    /// Shared Hierarchy selection policy for Binder and UI Library.
    /// Ordinary RectTransform containers are valid authoring targets, while
    /// prefab instances and their children are never used as creation parents.
    /// This does not affect which prefab components Binder discovers or binds.
    /// </summary>
    internal static class UIHierarchySelection
    {
        /// <summary>
        /// Returns whether a GameObject is a scene UI container that can be selected
        /// as an authoring parent, including Canvas, Panel, and ordinary named groups.
        /// </summary>
        /// <param name="candidate">GameObject to evaluate.</param>
        /// <returns>True for a non-prefab scene RectTransform.</returns>
        internal static bool IsSelectableContainer(GameObject candidate)
        {
            if (candidate == null || EditorUtility.IsPersistent(candidate) ||
                candidate.GetComponent<RectTransform>() == null)
                return false;
            // Added overrides can be regular GameObjects parented *inside* a prefab
            // instance. Those are not valid top-level authoring containers either.
            for (Transform node = candidate.transform; node != null; node = node.parent)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(node.gameObject)) return false;
            }
            return true;
        }

        /// <summary>
        /// Resolves an explicitly selected UI object to its closest valid parent.
        /// A selected prefab (including its internal children) resolves to the
        /// nearest non-prefab ancestor instead of becoming an active parent.
        /// Non-UI selections preserve the previous parent.
        /// </summary>
        /// <param name="current">Previously resolved parent.</param>
        /// <param name="selected">Currently selected scene GameObject.</param>
        /// <returns>The closest valid non-prefab RectTransform, or current.</returns>
        internal static RectTransform ResolveParent(RectTransform current, GameObject selected)
        {
            // An older window session may remember a prefab as its last parent.
            // Never return that stale invalid selection.
            if (current != null && !IsSelectableContainer(current.gameObject))
            {
                RectTransform validAncestor = null;
                for (Transform node = current.parent; node != null; node = node.parent)
                {
                    if (IsSelectableContainer(node.gameObject))
                    {
                        validAncestor = (RectTransform)node;
                        break;
                    }
                }
                current = validAncestor;
            }
            if (selected == null || EditorUtility.IsPersistent(selected)) return current;

            // An arbitrary non-UI object must not unexpectedly redirect the UI tools.
            // Prefab members may be selected even when they are beneath a valid parent.
            if (selected.GetComponent<RectTransform>() == null &&
                !PrefabUtility.IsPartOfPrefabInstance(selected))
                return current;

            for (Transform node = selected.transform; node != null; node = node.parent)
            {
                if (IsSelectableContainer(node.gameObject))
                    return (RectTransform)node;
            }
            // A standalone prefab with no ordinary scene parent must not silently
            // create its children in a previously selected, unrelated screen.
            return PrefabUtility.IsPartOfPrefabInstance(selected) ? null : current;
        }
    }
}
