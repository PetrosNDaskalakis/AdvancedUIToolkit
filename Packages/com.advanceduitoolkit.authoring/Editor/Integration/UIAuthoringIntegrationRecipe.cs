using System;
using System.Collections.Generic;
using AdvancedUIToolkit;
using UnityEngine;

namespace AdvancedUIAuthoring.Integration.Editor
{
    /// <summary>How a prefab node chooses its single text override.</summary>
    public enum UIAuthoringTextMode
    {
        /// <summary>Require exactly one supported source label; use the schema text, including empty.</summary>
        SingleLabel,
        /// <summary>Use the saved source component identity; missing or replaced components are errors.</summary>
        ExplicitSource,
        /// <summary>Keep all text stored in the prefab.</summary>
        PreservePrefab
    }

    /// <summary>Optional node-specific template and label selection, kept outside the portable JSON.</summary>
    [Serializable]
    public sealed class UIAuthoringPrefabMapping
    {
        /// <summary>Exact schema node ID; names and IDs are not interchangeable.</summary>
        public string nodeId = string.Empty;
        /// <summary>Standard role or Custom; overrides the schema's default role for this node.</summary>
        public UIComponentKind kind = UIComponentKind.Button;
        /// <summary>Style Profile custom template identity when kind is Custom.</summary>
        public string customTemplateId = string.Empty;
        /// <summary>Allow a decorative Custom template with no recognized binding names.</summary>
        public bool allowUnbound;
        /// <summary>Explicit text policy; SingleLabel also permits intentionally clearing text.</summary>
        public UIAuthoringTextMode textMode = UIAuthoringTextMode.SingleLabel;
        /// <summary>Component on the saved source prefab, used only in ExplicitSource mode.</summary>
        public Component sourceText;
    }

    /// <summary>Editor-only settings connecting portable authoring JSON to project prefab assets.</summary>
    [CreateAssetMenu(fileName = "UIAuthoringRecipe", menuName = "Advanced UI Toolkit/Layout Setup")]
    public sealed class UIAuthoringIntegrationRecipe : ScriptableObject
    {
        /// <summary>Portable schema version 1 asset.</summary>
        public TextAsset schema;
        /// <summary>Optional container and prefab-root layout stylesheet; prefab appearance comes from the Style Profile.</summary>
        public TextAsset stylesheet;
        /// <summary>Explicit profile; global UI Library preferences are never read.</summary>
        public UIStyleProfile profile;
        /// <summary>Optional overrides keyed by node ID; stale or duplicate entries are rejected.</summary>
        public List<UIAuthoringPrefabMapping> mappings = new List<UIAuthoringPrefabMapping>();
        /// <summary>Generated container ID used for initial Binder setup.</summary>
        public string bindingRootId = "panel";
        /// <summary>Initial generated class name; existing roots use their saved Binder settings.</summary>
        public string bindingClass = "WeaponPanelBindings";
        /// <summary>Initial generated namespace.</summary>
        public string bindingNamespace = "Game.UI.Generated";
        /// <summary>Initial output folder under Assets.</summary>
        public string bindingOutputFolder = "Assets/Generated";
    }
}
