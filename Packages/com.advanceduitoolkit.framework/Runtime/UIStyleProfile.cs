using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedUIToolkit
{
    /// <summary>Standard uGUI control roles supported by the Factory.</summary>
    public enum UIComponentKind
    {
        /// <summary>Represents the TextLegacy component role.</summary>
        TextLegacy,
        /// <summary>Represents the Button component role.</summary>
        Button,
        /// <summary>Represents the Image component role.</summary>
        Image,
        /// <summary>Represents the RawImage component role.</summary>
        RawImage,
        /// <summary>Represents the Panel component role.</summary>
        Panel,
        /// <summary>Represents the Toggle component role.</summary>
        Toggle,
        /// <summary>Represents the Slider component role.</summary>
        Slider,
        /// <summary>Represents the InputField component role.</summary>
        InputField,
        /// <summary>Represents the ScrollView component role.</summary>
        ScrollView,
        /// <summary>Represents the Scrollbar component role.</summary>
        Scrollbar,
        /// <summary>Represents the Dropdown component role.</summary>
        Dropdown,
        /// <summary>Represents the RectTransform component role.</summary>
        RectTransform,
        /// <summary>Represents the Custom component role.</summary>
        Custom
    }

    /// <summary>
    /// A reusable prefab template. A Custom entry uses Id as its unique library name
    /// and DefaultObjectName as its naming-convention-compatible instance name.
    /// For standard entries, the role (Kind) is its identity and overrides a parent's role.
    /// </summary>
    [Serializable]
    public sealed class UIComponentTemplate
    {
        /// <summary>Stores the kind reference or configuration value.</summary>
        public UIComponentKind Kind;
        /// <summary>Stores the id reference or configuration value.</summary>
        public string Id;
        /// <summary>Stores the default object name reference or configuration value.</summary>
        public string DefaultObjectName;
        /// <summary>Stores the prefab reference or configuration value.</summary>
        public GameObject Prefab;

        /// <summary>Gets identity.</summary>
        public string Identity => Kind == UIComponentKind.Custom
            ? "Custom:" + (Id ?? string.Empty).Trim()
            : "Role:" + Kind;
    }

    /// <summary>
    /// ScriptableObject-backed prefab library. Supports single-parent inheritance:
    /// e.g. BaseStation -> SonarStation and BaseStation -> WeaponsStation.
    /// No Editor dependencies; assets may be stored anywhere under Assets.
    /// </summary>
    [CreateAssetMenu(fileName = "UIStyleProfile", menuName = "Advanced UI Toolkit/Style Profile")]
    public sealed class UIStyleProfile : ScriptableObject
    {
        /// <summary>Optional parent style profile used for inherited component templates.</summary>
        [SerializeField] private UIStyleProfile baseProfile;
        /// <summary>Component templates explicitly registered by this profile.</summary>
        [SerializeField] private List<UIComponentTemplate> templates = new List<UIComponentTemplate>();

        /// <summary>Gets base profile.</summary>
        public UIStyleProfile BaseProfile => baseProfile;
        /// <summary>Gets templates.</summary>
        public IReadOnlyList<UIComponentTemplate> Templates => templates;

        /// <summary>Gets the first override of a role in this profile or its ancestors.</summary>
        public bool TryResolve(UIComponentKind kind, out UIComponentTemplate template)
        {
            return TryResolveIdentity("Role:" + kind, out template);
        }

        /// <summary>Attempts to resolve custom.</summary>
        public bool TryResolveCustom(string id, out UIComponentTemplate template)
        {
            return TryResolveIdentity("Custom:" + (id ?? string.Empty).Trim(), out template);
        }

        /// <summary>Attempts to resolve identity.</summary>
        private bool TryResolveIdentity(string identity, out UIComponentTemplate template)
        {
            var seen = new HashSet<UIStyleProfile>();
            for (UIStyleProfile current = this; current != null && seen.Add(current);
                 current = current.baseProfile)
            {
                foreach (var item in current.templates)
                {
                    if (item != null && item.Identity == identity && item.Prefab != null)
                    {
                        template = item;
                        return true;
                    }
                }
            }
            template = null;
            return false;
        }

        /// <summary>Returns effective templates; a derived profile overrides inherited roles/IDs.</summary>
        public List<UIComponentTemplate> GetEffectiveTemplates()
        {
            var result = new List<UIComponentTemplate>();
            var seenProfiles = new HashSet<UIStyleProfile>();
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);
            for (UIStyleProfile current = this; current != null && seenProfiles.Add(current);
                 current = current.baseProfile)
            {
                foreach (var item in current.templates)
                {
                    if (item != null && item.Prefab != null && seenKeys.Add(item.Identity))
                        result.Add(item);
                }
            }
            return result;
        }
    }
}
