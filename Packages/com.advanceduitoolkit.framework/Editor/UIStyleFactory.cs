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
                var guid = EditorPrefs.GetString(ActiveProfileGuidPref, string.Empty);
                if (string.IsNullOrEmpty(guid)) return null;
                var path = AssetDatabase.GUIDToAssetPath(guid);
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
                    var context = node.GetComponent<UIStyleContext>();
                    if (context != null && context.Profile != null)
                        return context.Profile;
                }
            }
            return ActiveProfile;
        }

        /// <summary>Gets parent.</summary>
        internal static RectTransform GetParent(MenuCommand command)
        {
            var selected = command != null && command.context is GameObject context
                ? context.transform : Selection.activeTransform;
            var parent = selected as RectTransform;
            if (parent == null || EditorUtility.IsPersistent(parent.gameObject))
                return null;
            return parent;
        }

        /// <summary>Attempts to create styled.</summary>
        internal static bool TryCreateStyled(
            RectTransform parent, UIComponentKind kind, string baseName, out GameObject created)
        {
            created = null;
            UIStyleProfile profile = ResolveProfile(parent);
            if (profile == null || !profile.TryResolve(kind, out var template)) return false;
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

        /// <summary>Creates prefab.</summary>
        internal static GameObject CreatePrefab(
            RectTransform parent, UIComponentTemplate template, string fallbackName, string explicitName = null)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || parent == null || template == null)
                return null;
            var prefab = template.Prefab;
            if (prefab == null || !AssetDatabase.Contains(prefab))
            {
                Debug.LogError("Advanced UI Toolkit: A template requires a saved prefab asset.");
                return null;
            }
            if (prefab.GetComponent<RectTransform>() == null)
            {
                Debug.LogError($"Advanced UI Toolkit: Template '{prefab.name}' needs a RectTransform on the root.", prefab);
                return null;
            }
            string proposedName = !string.IsNullOrWhiteSpace(explicitName)
                ? explicitName.Trim()
                : !string.IsNullOrWhiteSpace(template.DefaultObjectName)
                    ? template.DefaultObjectName.Trim() : fallbackName;
            if (!IsValidDefaultName(proposedName, template.Kind, out string reason))
            {
                Debug.LogError("Advanced UI Toolkit: Invalid template instance name '" + proposedName + "': " + reason, prefab);
                return null;
            }
            Type expected = GetRequiredType(template.Kind);
            if (template.Kind == UIComponentKind.Custom)
            {
                string prefix = proposedName.Substring(0, proposedName.IndexOf('_'));
                expected = AdvancedUIToolkitNaming.TypeByPrefix[prefix];
            }
            // Composite UI prefabs can keep their actual control on a child.
            // Do not search separately bound widgets nested under this prefab.
            if (expected != null && AdvancedUIToolkitNaming.ResolveNamedComponent(
                prefab.transform, expected, out string resolutionProblem) == null)
            {
                if (resolutionProblem != null && resolutionProblem.StartsWith("has multiple ",
                    StringComparison.Ordinal))
                {
                    // Instantiation is valid; the user must choose an unambiguous
                    // target before Binder can generate a typed accessor.
                    Debug.LogWarning($"Advanced UI Toolkit: Template '{prefab.name}' {resolutionProblem}", prefab);
                }
                else
                {
                    Debug.LogError($"Advanced UI Toolkit: Template '{prefab.name}' {resolutionProblem} " +
                        "Choose a matching prefab or component role.", prefab);
                    return null;
                }
            }

            // InstantiatePrefab preserves the link to the source prefab, including variants.
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Advanced UI Toolkit: Instantiate " + prefab.name);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null) return null;
            Undo.RegisterCreatedObjectUndo(instance, "Advanced UI Toolkit: Instantiate " + prefab.name);
            instance.name = MakeUniqueName(parent, proposedName, instance);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = instance;
            EditorGUIUtility.PingObject(instance);
            return instance;
        }

        // Keeps every generated name a legal binding identifier: BTN_Button_2,
        // never Unity's default "BTN_Button (1)" which would fail validation.
        /// <summary>Performs the make unique name operation.</summary>
        internal static string MakeUniqueName(Transform parent, string baseName, GameObject exclude = null)
        {
            var names = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
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
            var prefix = name.Substring(0, underscore);
            var identifier = name.Substring(underscore + 1);
            if (!AdvancedUIToolkitNaming.TypeByPrefix.TryGetValue(prefix, out var expected))
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
            var required = GetRequiredType(kind);
            if (required != null && !expected.IsAssignableFrom(required))
            {
                error = $"Prefix '{prefix}' is incompatible with role '{kind}'.";
                return false;
            }
            return true;
        }
    }
}
