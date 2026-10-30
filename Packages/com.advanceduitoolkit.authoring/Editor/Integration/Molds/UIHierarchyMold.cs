using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedUIAuthoring.Integration.Editor
{
    /// <summary>A prefab-free, versioned snapshot of one UI hierarchy; contains no Unity object references.</summary>
    public sealed class UIHierarchyMold : ScriptableObject
    {
        /// <summary>Shared capture and loaded-data limit for the number of containers.</summary>
        public const int MaximumNodeCount = 10000;
        /// <summary>Shared capture and loaded-data depth limit, with the root at depth zero.</summary>
        public const int MaximumDepth = 256;
        /// <summary>Version 2 captures selectable containers only; version 1 flattened prefab contents and requires recapture.</summary>
        public int version = 2;
        /// <summary>Root-first eligible container hierarchy, including inactive containers but excluding non-selectable branches.</summary>
        public List<UIMoldNode> nodes = new List<UIMoldNode>();
        /// <summary>Excluded branches, cleared references and binding-state resets, shown before reuse.</summary>
        public List<string> omissions = new List<string>();
    }

    /// <summary>One named object with a parent index, local transform and ordered component slots.</summary>
    [Serializable]
    public sealed class UIMoldNode
    {
        /// <summary>Original object name; names are not used as identity.</summary>
        public string name;
        /// <summary>Earlier parent index; only the root uses -1.</summary>
        public int parent = -1;
        /// <summary>Original activeSelf state, independent of ancestors.</summary>
        public bool active = true;
        /// <summary>Original Unity layer number.</summary>
        public int layer;
        /// <summary>Whether this object uses RectTransform instead of Transform.</summary>
        public bool rectTransform;
        /// <summary>Local position for ordinary Transform children.</summary>
        public Vector3 localPosition;
        /// <summary>Local rotation relative to the new parent.</summary>
        public Quaternion localRotation = Quaternion.identity;
        /// <summary>Local scale relative to the new parent.</summary>
        public Vector3 localScale = Vector3.one;
        /// <summary>RectTransform minimum anchor.</summary>
        public Vector2 anchorMin;
        /// <summary>RectTransform maximum anchor.</summary>
        public Vector2 anchorMax;
        /// <summary>RectTransform pivot.</summary>
        public Vector2 pivot;
        /// <summary>RectTransform size relative to anchors.</summary>
        public Vector2 sizeDelta;
        /// <summary>RectTransform anchored position, including Z.</summary>
        public Vector3 anchoredPosition;
        /// <summary>Non-transform components; internal references address slots, not names.</summary>
        public List<UIMoldComponent> components = new List<UIMoldComponent>();
    }

    /// <summary>Installed component type plus portable serialized property values.</summary>
    [Serializable]
    public sealed class UIMoldComponent
    {
        /// <summary>Assembly-qualified component type; scripts are required in the destination project.</summary>
        public string typeName;
        /// <summary>Parent-first serialized values; no raw instance IDs or prefab GUIDs.</summary>
        public List<UIMoldProperty> properties = new List<UIMoldProperty>();
    }

    /// <summary>One portable property or hierarchy-local reference; data is interpreted by its recorded Unity property kind.</summary>
    [Serializable]
    public sealed class UIMoldProperty
    {
        /// <summary>Unity serialized property path.</summary>
        public string path;
        /// <summary>SerializedPropertyType numeric value for the supported Unity editor.</summary>
        public int kind;
        /// <summary>Unity field type name, used to reject changed scalar/container definitions on restore.</summary>
        public string valueType;
        /// <summary>JSON wrapper for a scalar/value type; unused for references and generic containers.</summary>
        public string value;
        /// <summary>Referenced node index; -1 denotes an intentionally empty reference.</summary>
        public int node = -1;
        /// <summary>Reference slot: -1 GameObject, -2 Transform, otherwise non-transform component index.</summary>
        public int component = -1;
    }
}
