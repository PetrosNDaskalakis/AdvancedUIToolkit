using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Editor
{
    /// <summary>Prefab-aware creation shared by the standard UI Factory and Library Manager.</summary>
    internal static class UIStyleFactory
    {
        /// <summary>Defines the active profile guid pref constant.</summary>
        private const string ActiveProfileGuidPref = "AdvancedUIToolkit.Factory.ActiveStyleProfileGUID";

        /// <summary>Stores the active profile reference or configuration value.</summary>
        internal static UIStyleProfile ActiveProfile
        {
            get
            {
                string guid = EditorPrefs.GetString(ActiveProfileGuidPref, string.Empty);
                if (string.IsNullOrEmpty(guid)) return null;
                string path = AssetDatabase.GUIDToAssetPath(guid);
                return string.IsNullOrEmpty(path)
                    ? null : AssetDatabase.LoadAssetAtPath<UIStyleProfile>(path);
            }
            set
            {
                string path = value != null ? AssetDatabase.GetAssetPath(value) : string.Empty;
                string guid = !string.IsNullOrEmpty(path) ? AssetDatabase.AssetPathToGUID(path) : string.Empty;
                EditorPrefs.SetString(ActiveProfileGuidPref, guid ?? string.Empty);
            }
        }

        /// <summary>Resolves profile.</summary>
        internal static UIStyleProfile ResolveProfile(RectTransform parent)
        {
            if (parent != null)
            {
                // A station/screen context takes precedence over the Editor-wide profile.
                for (Transform node = parent; node != null; node = node.parent)
                {
                    UIStyleContext context = node.GetComponent<UIStyleContext>();
                    if (context != null && context.Profile != null)
                        return context.Profile;
                }
            }
            return ActiveProfile;
        }

        /// <summary>Gets parent.</summary>
        internal static RectTransform GetParent(MenuCommand command)
        {
            GameObject selected = command != null && command.context is GameObject context
                ? context : Selection.activeGameObject;
            return UIHierarchySelection.ResolveParent(null, selected);
        }

        /// <summary>
        /// Creates an ordinary non-prefab RectTransform grouping object. It is not
        /// registered in the Style Profile and may use any developer-chosen name.
        /// </summary>
        /// <param name="parent">Valid non-prefab UI container.</param>
        /// <param name="name">Requested hierarchy name.</param>
        /// <param name="updateSelection">Whether to select the new container (default true for Library users).</param>
        /// <param name="joinUndoGroup">True only inside a caller-owned batch Undo group.</param>
        /// <returns>The new container, or null if creation is not allowed.</returns>
        internal static GameObject CreateEmptyContainer(RectTransform parent, string name,
            bool updateSelection = true, bool joinUndoGroup = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || parent == null ||
                !UIHierarchySelection.IsSelectableContainer(parent.gameObject) ||
                string.IsNullOrWhiteSpace(name)) return null;

            string uniqueName = MakeUniqueName(parent, name.Trim());
            // Keep this creation separate from an unrelated preceding Editor operation.
            if (!joinUndoGroup) Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            if (!joinUndoGroup) Undo.SetCurrentGroupName("Advanced UI Toolkit: Create " + uniqueName);
            GameObject created = new GameObject(uniqueName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(created, "Advanced UI Toolkit: Create " + uniqueName);
            GameObjectUtility.SetParentAndAlign(created, parent.gameObject);
            RectTransform rect = (RectTransform)created.transform;
            Undo.RecordObject(rect, "Advanced UI Toolkit: Configure " + uniqueName);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            if (!joinUndoGroup) Undo.CollapseUndoOperations(group);
            if (updateSelection) Selection.activeGameObject = created;
            return created;
        }

        /// <summary>Attempts to create styled.</summary>
        internal static bool TryCreateStyled(
            RectTransform parent, UIComponentKind kind, string baseName, out GameObject created)
        {
            created = null;
            UIStyleProfile profile = ResolveProfile(parent);
            if (profile == null || !profile.TryResolve(kind, out UIComponentTemplate template)) return false;
            created = CreatePrefab(parent, template, baseName);
            // A configured but invalid template must not silently fall back to a Unity default.
            return true;
        }

        /// <summary>Creates custom.</summary>
        internal static GameObject CreateCustom(RectTransform parent, UIComponentTemplate template)
        {
            if (template == null) return null;
            string name = string.IsNullOrWhiteSpace(template.DefaultObjectName)
                ? template.Kind == UIComponentKind.Custom
                    ? template.Prefab != null ? template.Prefab.name : "RT_Component"
                    : DefaultName(template.Kind)
                : template.DefaultObjectName.Trim();
            return CreatePrefab(parent, template, name);
        }

        /// <summary>
        /// Instantiates a saved UI prefab under an ordinary scene container, retaining
        /// its prefab link and optionally overriding one explicitly selected text label.
        /// Validates source targets before creating anything, allocates an isolated Undo
        /// group, and destroys incomplete instances on a text application failure.
        /// </summary>
        /// <param name="parent">Allowed non-prefab RectTransform parent.</param>
        /// <param name="template">Style template with a saved prefab and component kind.</param>
        /// <param name="fallbackName">Name used if the template supplies no default.</param>
        /// <param name="explicitName">Optional instance name; naming rules are enforced.</param>
        /// <param name="textOverride">Optional instance-only label replacement.</param>
        /// <param name="updateSelection">Whether to select and ping the instance (default true for Library users).</param>
        /// <param name="joinUndoGroup">True only when an external batch owns the current Undo group.</param>
        /// <returns>The linked prefab instance, or null on validation/creation failure.</returns>
        internal static GameObject CreatePrefab(
            RectTransform parent, UIComponentTemplate template, string fallbackName, string explicitName = null,
            UIPrefabTextOverrideRequest textOverride = default(UIPrefabTextOverrideRequest),
            bool updateSelection = true, bool joinUndoGroup = false,
            bool allowUnbound = false)
        {
            if (!TryValidatePrefabCreation(parent, template, fallbackName, explicitName,
                textOverride, out string proposedName, out int sourceTextIndex, out string error,
                allowUnbound))
            {
                Debug.LogError("Advanced UI Toolkit: " + error, template != null ? template.Prefab : null);
                return null;
            }
            GameObject prefab = template.Prefab;
            // Keep the existing warning for ambiguous nested components. Binder will
            // require the developer to disambiguate before producing an accessor.
            Type expected = allowUnbound ? null : GetExpectedType(template, proposedName);
            if (expected != null && AdvancedUIToolkitNaming.ResolveNamedComponent(
                prefab.transform, expected, out string issue) == null && issue != null &&
                issue.StartsWith("has multiple ", StringComparison.Ordinal))
                Debug.LogWarning($"Advanced UI Toolkit: Template '{prefab.name}' {issue}", prefab);

            // InstantiatePrefab preserves the link to the source prefab, including variants.
            // Never absorb unrelated user edits that share Unity's current Undo group.
            if (!joinUndoGroup) Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            if (!joinUndoGroup) Undo.SetCurrentGroupName("Advanced UI Toolkit: Instantiate " + prefab.name);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
            {
                if (!joinUndoGroup) Undo.CollapseUndoOperations(group);
                return null;
            }
            Undo.RegisterCreatedObjectUndo(instance, "Advanced UI Toolkit: Instantiate " + prefab.name);
            instance.name = MakeUniqueName(parent, proposedName, instance);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            if (!UIPrefabTextOverride.Apply(instance, textOverride, sourceTextIndex, out string applyError))
            {
                Debug.LogError("Advanced UI Toolkit: " + applyError, prefab);
                // Do not leave a partially configured control in the user's UI.
                Undo.DestroyObjectImmediate(instance);
                if (!joinUndoGroup) Undo.CollapseUndoOperations(group);
                return null;
            }
            if (!joinUndoGroup) Undo.CollapseUndoOperations(group);
            if (updateSelection)
            {
                Selection.activeGameObject = instance;
                EditorGUIUtility.PingObject(instance);
            }
            return instance;
        }

        /// <summary>
        /// Validates exactly the same prefab, name and text-target constraints used by
        /// CreatePrefab, without modifying the scene, selection, or Undo history.
        /// </summary>
        /// <param name="parent">Target scene container.</param>
        /// <param name="template">Resolved profile entry.</param>
        /// <param name="fallbackName">Fallback instance name.</param>
        /// <param name="explicitName">Optional caller-supplied name.</param>
        /// <param name="textOverride">Optional single-label override.</param>
        /// <param name="proposedName">Resolved instance name when valid.</param>
        /// <param name="sourceTextIndex">Current source label index, or -1 if disabled.</param>
        /// <param name="error">Human-readable validation error.</param>
        /// <returns>Whether creation can be attempted without a known validation error.</returns>
        internal static bool TryValidatePrefabCreation(
            RectTransform parent, UIComponentTemplate template, string fallbackName,
            string explicitName, UIPrefabTextOverrideRequest textOverride,
            out string proposedName, out int sourceTextIndex, out string error,
            bool allowUnbound = false)
        {
            proposedName = null;
            sourceTextIndex = -1;
            error = null;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                error = "UI creation is unavailable while entering or running Play Mode.";
            else if (parent == null || !UIHierarchySelection.IsSelectableContainer(parent.gameObject))
                error = "Select an ordinary, non-prefab scene RectTransform parent.";
            else if (template == null)
                error = "A UI template is required.";
            else if (template.Prefab == null || !AssetDatabase.Contains(template.Prefab))
                error = "The template requires a saved prefab asset.";
            else if (template.Prefab.GetComponent<RectTransform>() == null)
                error = "The template prefab requires a RectTransform on its root.";
            if (error != null) return false;

            GameObject prefab = template.Prefab;
            proposedName = !string.IsNullOrWhiteSpace(explicitName)
                ? explicitName.Trim()
                : !string.IsNullOrWhiteSpace(template.DefaultObjectName)
                    ? template.DefaultObjectName.Trim() : fallbackName;
            if (allowUnbound)
            {
                if (template.Kind != UIComponentKind.Custom)
                {
                    error = "Unbound prefab creation requires a Custom template.";
                    return false;
                }
                // An unbound name must never look like a Binder binding prefix.
                // This prevents accidental generated accessors for decorative objects.
                if (string.IsNullOrWhiteSpace(proposedName) ||
                    AdvancedUIToolkitNaming.HasRecognizedPrefix(proposedName))
                {
                    error = "Unbound prefab name must be nonempty and must not use a registered binding prefix.";
                    return false;
                }
                // Binder also discovers prefixed descendants, not only prefab roots.
                // Reject an apparently decorative prefab that would create bindings.
                foreach (Transform descendant in prefab.GetComponentsInChildren<Transform>(true))
                {
                    if (descendant != prefab.transform &&
                        AdvancedUIToolkitNaming.HasRecognizedPrefix(descendant.name))
                    {
                        error = "Unbound prefab contains bindable child '" + descendant.name +
                            "'. Rename that child or create the prefab in bound mode.";
                        return false;
                    }
                }
            }
            else if (!IsValidDefaultName(proposedName, template.Kind, out string nameError))
            {
                error = "Invalid instance name '" + proposedName + "': " + nameError;
                return false;
            }
            Type expected = allowUnbound ? null : GetExpectedType(template, proposedName);
            if (expected != null && AdvancedUIToolkitNaming.ResolveNamedComponent(
                prefab.transform, expected, out string issue) == null &&
                (issue == null || !issue.StartsWith("has multiple ", StringComparison.Ordinal)))
            {
                error = "Template '" + prefab.name + "' " + (issue ?? "has no compatible component.");
                return false;
            }
            if (!UIPrefabTextOverride.TryResolveSourceIndex(prefab, textOverride,
                out sourceTextIndex, out error)) return false;
            return true;
        }

        /// <summary>Finds the required control type for the resolved template and instance name.</summary>
        /// <param name="template">Style template.</param>
        /// <param name="proposedName">Validated binding-compatible name.</param>
        /// <returns>Required uGUI control type, or null for a role without one.</returns>
        private static Type GetExpectedType(UIComponentTemplate template, string proposedName)
        {
            if (template.Kind != UIComponentKind.Custom) return GetRequiredType(template.Kind);
            string prefix = proposedName.Substring(0, proposedName.IndexOf('_'));
            return AdvancedUIToolkitNaming.TypeByPrefix[prefix];
        }

        // Keeps every generated name a legal binding identifier: BTN_Button_2,
        // never Unity's default "BTN_Button (1)" which would fail validation.
        /// <summary>Performs the make unique name operation.</summary>
        internal static string MakeUniqueName(Transform parent, string baseName, GameObject exclude = null)
        {
            System.Collections.Generic.HashSet<string> names = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (Transform sibling in parent)
                if (sibling.gameObject != exclude) names.Add(sibling.name);
            if (!names.Contains(baseName)) return baseName;
            int suffix = 2;
            while (names.Contains(baseName + "_" + suffix)) suffix++;
            return baseName + "_" + suffix;
        }

        /// <summary>Gets required type.</summary>
        internal static Type GetRequiredType(UIComponentKind kind)
        {
            switch (kind)
            {
                case UIComponentKind.TextLegacy: return typeof(Text);
                case UIComponentKind.Button: return typeof(Button);
                case UIComponentKind.Image: return typeof(Image);
                case UIComponentKind.RawImage: return typeof(RawImage);
                case UIComponentKind.Panel: return typeof(RectTransform);
                case UIComponentKind.Toggle: return typeof(Toggle);
                case UIComponentKind.Slider: return typeof(Slider);
                case UIComponentKind.InputField: return typeof(InputField);
                case UIComponentKind.ScrollView: return typeof(ScrollRect);
                case UIComponentKind.Scrollbar: return typeof(Scrollbar);
                case UIComponentKind.Dropdown: return typeof(Dropdown);
                case UIComponentKind.RectTransform: return typeof(RectTransform);
                default: return null;
            }
        }

        /// <summary>Performs the default name operation.</summary>
        internal static string DefaultName(UIComponentKind kind)
        {
            switch (kind)
            {
                case UIComponentKind.TextLegacy: return "TXT_LegacyText";
                case UIComponentKind.Button: return "BTN_Button";
                case UIComponentKind.Image: return "IMG_Image";
                case UIComponentKind.RawImage: return "RAW_RawImage";
                case UIComponentKind.Panel: return "PNL_Panel";
                case UIComponentKind.Toggle: return "TGL_Toggle";
                case UIComponentKind.Slider: return "SLD_Slider";
                case UIComponentKind.InputField: return "INP_InputField";
                case UIComponentKind.ScrollView: return "SCR_ScrollView";
                case UIComponentKind.Scrollbar: return "SBR_Scrollbar";
                case UIComponentKind.Dropdown: return "DDP_Dropdown";
                case UIComponentKind.RectTransform: return "RT_Container";
                default: return "RT_Component";
            }
        }

        /// <summary>Determines whether valid default name.</summary>
        internal static bool IsValidDefaultName(string name, UIComponentKind kind, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Default object name cannot be empty.";
                return false;
            }
            int underscore = name.IndexOf('_');
            if (underscore < 1 || underscore == name.Length - 1)
            {
                error = "Name must be PREFIX_Identifier, e.g. BTN_TrackTarget.";
                return false;
            }
            string prefix = name.Substring(0, underscore);
            string identifier = name.Substring(underscore + 1);
            if (!AdvancedUIToolkitNaming.TypeByPrefix.TryGetValue(prefix, out Type expected))
            {
                error = "Unrecognized prefix: " + prefix;
                return false;
            }
            if (!(char.IsLetter(identifier[0]) || identifier[0] == '_'))
            {
                error = "Identifier must begin with a letter or underscore.";
                return false;
            }
            foreach (char ch in identifier)
            {
                if (!char.IsLetterOrDigit(ch) && ch != '_')
                {
                    error = "Identifier must contain only letters, digits, or underscore.";
                    return false;
                }
            }
            if (AdvancedUIToolkitNaming.IsReservedAccessorName(identifier))
            {
                error = "Identifier is reserved: " + identifier;
                return false;
            }
            Type required = GetRequiredType(kind);
            if (required != null && !expected.IsAssignableFrom(required))
            {
                error = $"Prefix '{prefix}' is incompatible with role '{kind}'.";
                return false;
            }
            return true;
        }
    }
}
