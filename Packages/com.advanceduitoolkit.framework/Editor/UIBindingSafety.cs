using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdvancedUIToolkit.Editor
{
    /// <summary>
    /// Provides shared, read-only safety checks for serialized binding manifests,
    /// nested prefab ownership, and generated-source destinations.
    /// </summary>
    internal static class UIBindingSafety
    {
        /// <summary>
        /// Determines whether a component belongs to a binding root without crossing
        /// another UIBindingRoot boundary.
        /// </summary>
        /// <param name="root">The owner of the binding manifest.</param>
        /// <param name="component">The component to check.</param>
        /// <returns>True when the component belongs to the selected root.</returns>
        internal static bool IsInScope(UIBindingRoot root, Component component)
        {
            if (root == null || component == null)
                return false;

            for (Transform current = component.transform; current != null; current = current.parent)
            {
                if (current == root.transform)
                    return true;
                if (current.GetComponent<UIBindingRoot>() != null)
                    return false;
            }
            return false;
        }

        /// <summary>
        /// Inspects saved entries and reports duplicates, missing references, wrong
        /// component types, and references owned by a different UI binding root.
        /// </summary>
        /// <param name="root">The owner of the manifest.</param>
        /// <param name="entries">Entries to inspect.</param>
        /// <param name="error">Callback receiving actionable errors.</param>
        internal static void ValidateManifest(UIBindingRoot root,
            IReadOnlyList<UIBindingRoot.Entry> entries, Action<string> error)
        {
            if (error == null)
                throw new ArgumentNullException(nameof(error));
            if (root == null || entries == null)
            {
                error("Binding root or manifest is missing.");
                return;
            }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var references = new HashSet<Component>();
            for (int index = 0; index < entries.Count; index++)
            {
                UIBindingRoot.Entry entry = entries[index];
                if (entry == null)
                {
                    error("Null binding entry at index " + index + ".");
                    continue;
                }
                if (string.IsNullOrEmpty(entry.Key))
                    error("Binding at index " + index + " has no generated API key.");
                else if (!keys.Add(entry.Key))
                    error("Duplicate generated API key '" + entry.Key + "'.");

                if (string.IsNullOrEmpty(entry.StableId))
                    error("Binding '" + entry.Key + "' has no stable ID.");
                else if (!ids.Add(entry.StableId))
                    error("Duplicate stable binding ID '" + entry.StableId + "'.");

                if (entry.Reference == null)
                {
                    error("Missing component reference for binding '" + entry.Key + "'.");
                    continue;
                }
                if (!references.Add(entry.Reference))
                    error("Component registered more than once ('" + entry.Key + "').");
                if (!IsInScope(root, entry.Reference))
                    error("Binding '" + entry.Key + "' references a component outside its UI root or inside a nested binding root.");
                if (!string.IsNullOrEmpty(entry.ComponentType) &&
                    !MatchesType(entry.Reference, entry.ComponentType))
                    error("Binding '" + entry.Key + "' expects " + entry.ComponentType +
                          " but references " + entry.Reference.GetType().FullName + ".");
            }
        }

        /// <summary>
        /// Returns whether the registered component matches the recorded type or one
        /// of its Unity component base types.
        /// </summary>
        /// <param name="component">The registered component.</param>
        /// <param name="typeName">Saved full component type name.</param>
        /// <returns>True when the type name is compatible.</returns>
        private static bool MatchesType(Component component, string typeName)
        {
            for (Type type = component.GetType(); type != null; type = type.BaseType)
                if (string.Equals(type.FullName, typeName, StringComparison.Ordinal))
                    return true;
            return false;
        }

        /// <summary>
        /// Rejects directly editing prefab assets or scene prefab instances. Prefab
        /// sources must be edited in Prefab Mode so changes do not become instance overrides.
        /// </summary>
        /// <param name="target">The selected UI root.</param>
        /// <returns>A blocking message or null when editing is safe.</returns>
        internal static string GetTargetError(GameObject target)
        {
            if (target == null)
                return "Select a UI root.";
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return "Binding generation is only available in Edit Mode.";
            if (EditorUtility.IsPersistent(target))
                return "Open the source prefab in Prefab Mode instead of editing the Project asset directly.";
            if (PrefabUtility.IsPartOfPrefabInstance(target))
            {
                // A prefab variant's outermost staged root can inherit prefab-instance
                // semantics. Editing that root in its own Prefab Mode remains valid.
                var stage = PrefabStageUtility.GetCurrentPrefabStage();
                if (stage == null || stage.prefabContentsRoot != target)
                    return "The selected root is a prefab instance. Open the source prefab in Prefab Mode " +
                           "to avoid creating binding overrides on an instance.";
            }
            return null;
        }

        /// <summary>
        /// Prevents a newly configured UI root from silently taking over an existing
        /// generated file, including when the generated class or output path was changed.
        /// </summary>
        /// <param name="root">The selected UI root, optionally without a manifest yet.</param>
        /// <param name="path">Proposed relative source path.</param>
        /// <returns>A blocking message or null when ownership is consistent.</returns>
        internal static string GetExistingFileError(UIBindingRoot root, string path)
        {
            if (!File.Exists(path))
                return null;
            if (root == null || !root.EditorHasGenerationProfile)
                return "Generated output already exists, but this root has no saved generation profile: " +
                       path + ". Choose a unique class name or open the original root.";

            root.EditorGetGenerationProfile(out string className,
                out string namespaceName, out string folder);
            string ownedPath = (folder ?? string.Empty).Replace('\\', '/').TrimEnd('/') +
                               "/" + className + ".cs";
            if (!string.Equals(ownedPath, path, StringComparison.OrdinalIgnoreCase))
                return "Generated output belongs to a different class or path: " + path +
                       ". Choose an unused destination instead.";
            return null;
        }

        /// <summary>
        /// Finds a different UI root already configured to generate into the same C# file.
        /// Prefab instances of the same source prefab count as one logical owner.
        /// </summary>
        /// <param name="target">Selected GameObject; may not yet contain a UIBindingRoot.</param>
        /// <param name="path">Proposed generated C# path.</param>
        /// <returns>An error message or null if no conflicting root was found.</returns>
        internal static string GetOtherOwnerError(GameObject target, string path)
        {
            if (target == null)
                return "Select a UI root before generating.";
            string ownerAsset = GetPrefabAssetPath(target);
            var visited = new HashSet<UIBindingRoot>();
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            foreach (string guid in guids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null) continue;
                foreach (var other in asset.GetComponentsInChildren<UIBindingRoot>(true))
                {
                    if (visited.Add(other) && IsDifferentOwner(target, other, ownerAsset) &&
                        HasOutputPath(other, path))
                        return "Output path is already owned by '" + other.name + "' in prefab '" +
                               AssetDatabase.GetAssetPath(asset) + "'.";
                }
            }
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                foreach (var sceneRoot in scene.GetRootGameObjects())
                foreach (var other in sceneRoot.GetComponentsInChildren<UIBindingRoot>(true))
                {
                    if (visited.Add(other) && IsDifferentOwner(target, other, ownerAsset) &&
                        HasOutputPath(other, path))
                        return "Output path is already owned by '" + other.name + "' in scene '" +
                               scene.name + "'.";
                }
            }
            return null;
        }

        /// <summary>Determines whether another root is distinct from this binding owner.</summary>
        /// <param name="selected">The selected UI GameObject.</param>
        /// <param name="other">Another binding root in an asset or scene.</param>
        /// <param name="ownerAsset">The selected root's prefab asset path.</param>
        /// <returns>False for the same root or an instance of its own source prefab.</returns>
        private static bool IsDifferentOwner(GameObject selected, UIBindingRoot other, string ownerAsset)
        {
            if (other == null || other.gameObject == selected) return false;
            string otherAsset = GetPrefabAssetPath(other.gameObject);
            if (!string.IsNullOrEmpty(ownerAsset) &&
                string.Equals(ownerAsset, otherAsset, StringComparison.Ordinal) &&
                string.Equals(GetPathWithinPrefab(selected),
                    GetPathWithinPrefab(other.gameObject), StringComparison.Ordinal))
                return false;
            return true;
        }

        /// <summary>Gets the prefab asset path, if the object belongs to a prefab or prefab stage.</summary>
        /// <param name="target">GameObject to inspect.</param>
        /// <returns>Prefab source asset path or an empty string.</returns>
        private static string GetPrefabAssetPath(GameObject target)
        {
            string path = AssetDatabase.GetAssetPath(target);
            if (!string.IsNullOrEmpty(path)) return path;
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.prefabContentsRoot != null &&
                target.transform.IsChildOf(stage.prefabContentsRoot.transform))
                return stage.assetPath;
            return PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);
        }

        /// <summary>
        /// Computes a path within the owning prefab so that separate binding roots
        /// inside one prefab do not incorrectly count as the same code-generation owner.
        /// </summary>
        /// <param name="target">The UI root to describe.</param>
        /// <returns>Relative hierarchy path including sibling indices.</returns>
        private static string GetPathWithinPrefab(GameObject target)
        {
            Transform boundary = null;
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.prefabContentsRoot != null &&
                target.transform.IsChildOf(stage.prefabContentsRoot.transform))
                boundary = stage.prefabContentsRoot.transform;
            if (boundary == null)
            {
                GameObject instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(target);
                if (instanceRoot != null)
                    boundary = instanceRoot.transform;
            }
            if (boundary == null)
            {
                boundary = target.transform;
                while (boundary.parent != null)
                    boundary = boundary.parent;
            }
            var segments = new Stack<string>();
            // The prefab root itself has no relative path segment. Its sibling
            // index differs between the source asset and instances in scenes.
            for (Transform current = target.transform; current != null && current != boundary;
                 current = current.parent)
                segments.Push(current.GetSiblingIndex() + ":" + current.name);
            return segments.Count == 0 ? "." : string.Join("/", segments.ToArray());
        }

        /// <summary>Checks whether a root's generation profile uses the given file.</summary>
        /// <param name="root">Root to inspect.</param>
        /// <param name="path">Normalized relative output path.</param>
        /// <returns>True when the root has a matching saved generation path.</returns>
        private static bool HasOutputPath(UIBindingRoot root, string path)
        {
            if (!root.EditorHasGenerationProfile) return false;
            root.EditorGetGenerationProfile(out string className, out string namespaceName,
                out string folder);
            string output = (folder ?? string.Empty).Replace('\\', '/').TrimEnd('/') +
                            "/" + className + ".cs";
            return string.Equals(output, path, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Warns about nested prefab instances whose controls will be registered by
        /// a parent UI root rather than being independently bound.
        /// </summary>
        /// <param name="root">The selected hierarchy root.</param>
        /// <param name="warnings">Destination for non-blocking guidance.</param>
        internal static void AppendNestedPrefabWarnings(GameObject root, List<string> warnings)
        {
            if (root == null || warnings == null) return;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child == root.transform) continue;
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject)) continue;
                if (child.GetComponent<UIBindingRoot>() != null) continue;
                bool hasBindableControls = false;
                foreach (Transform descendant in child.GetComponentsInChildren<Transform>(true))
                {
                    bool independentlyOwned = false;
                    for (Transform ancestor = descendant; ancestor != null && ancestor != child;
                         ancestor = ancestor.parent)
                    {
                        if (ancestor.GetComponent<UIBindingRoot>() == null) continue;
                        independentlyOwned = true;
                        break;
                    }
                    if (independentlyOwned) continue;
                    int split = descendant.name.IndexOf('_');
                    if (split <= 0) continue;
                    if (AdvancedUIToolkitNaming.TypeByPrefix.ContainsKey(descendant.name.Substring(0, split)))
                    {
                        hasBindableControls = true;
                        break;
                    }
                }
                if (!hasBindableControls) continue;
                warnings.Add("Nested prefab '" + child.name + "' has no UIBindingRoot. " +
                             "Its bindable children may be owned by the parent screen; " +
                             "add a nested UIBindingRoot if it should be independently generated.");
            }
        }
    }
}
