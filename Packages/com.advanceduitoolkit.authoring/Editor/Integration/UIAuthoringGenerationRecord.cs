using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedUIAuthoring.Integration.Editor
{
    /// <summary>Editor-window-only identity handoff for a completed generation, before saving a baseline.</summary>
    [Serializable]
    public sealed class UIAuthoringPendingGeneration
    {
        /// <summary>Recipe used during generation.</summary>
        [SerializeField] internal UIAuthoringIntegrationRecipe recipe;
        /// <summary>Original target parent; never inferred from a current selection.</summary>
        [SerializeField] internal RectTransform parent;
        /// <summary>Canonical schema at generation time.</summary>
        [SerializeField] internal string schemaJson;
        /// <summary>Digest of recipe inputs at generation time.</summary>
        [SerializeField] internal string inputStamp;
        /// <summary>Stable schema IDs in generation order.</summary>
        [SerializeField] internal List<string> ids = new List<string>();
        /// <summary>Corresponding generated scene objects; preserved through ordinary domain reload.</summary>
        [SerializeField] internal List<GameObject> objects = new List<GameObject>();
    }

    /// <summary>One saved schema-to-scene identity and baseline state; not a runtime component.</summary>
    [Serializable]
    internal sealed class UIAuthoringRecordedNode
    {
        /// <summary>Exact schema ID.</summary>
        [SerializeField] internal string id;
        /// <summary>Canonical schema-node JSON at generation time.</summary>
        [SerializeField] internal string definition;
        /// <summary>Desired sibling order within its schema parent.</summary>
        [SerializeField] internal int sourceOrder;
        /// <summary>Persistent scene object identity.</summary>
        [SerializeField] internal string objectId;
        /// <summary>Persistent scene parent identity at baseline capture.</summary>
        [SerializeField] internal string parentObjectId;
        /// <summary>Actual sibling index at baseline capture.</summary>
        [SerializeField] internal int sceneOrder;
        /// <summary>Root and owned-descendant serialized-state digest.</summary>
        [SerializeField] internal string sceneHash;
        /// <summary>Resolved source prefab identity or the native-container marker.</summary>
        [SerializeField] internal string templateId;
        /// <summary>Explicit per-node template/text policy digest.</summary>
        [SerializeField] internal string mappingHash;
    }

    /// <summary>Persistent Editor-only baseline. Created through Capture Baseline, never attached to a scene.</summary>
    public sealed class UIAuthoringGenerationRecord : ScriptableObject
    {
        /// <summary>Record format and fingerprint algorithm version.</summary>
        [SerializeField, HideInInspector] internal int formatVersion = 1;
        /// <summary>Explicit recipe asset used by the generated hierarchy.</summary>
        [SerializeField, HideInInspector] internal UIAuthoringIntegrationRecipe recipe;
        /// <summary>Saved scene asset GUID; scene renames preserve identity.</summary>
        [SerializeField, HideInInspector] internal string sceneGuid;
        /// <summary>Target parent GlobalObjectId.</summary>
        [SerializeField, HideInInspector] internal string parentObjectId;
        /// <summary>Capture timestamp in UTC.</summary>
        [SerializeField, HideInInspector] internal string capturedUtc;
        /// <summary>Canonical stylesheet digest, including tokens and classes.</summary>
        [SerializeField, HideInInspector] internal string stylesheetHash;
        /// <summary>Explicit profile dependency digest, including prefab source changes.</summary>
        [SerializeField, HideInInspector] internal string profileHash;
        /// <summary>Initial Binder output settings digest; does not represent generated file contents.</summary>
        [SerializeField, HideInInspector] internal string binderHash;
        /// <summary>External target parent's layout configuration digest.</summary>
        [SerializeField, HideInInspector] internal string parentLayoutHash;
        /// <summary>Owned schema-node identities and baseline states.</summary>
        [SerializeField, HideInInspector] internal List<UIAuthoringRecordedNode> nodes = new List<UIAuthoringRecordedNode>();

        /// <summary>Recipe associated with this record.</summary>
        public UIAuthoringIntegrationRecipe Recipe { get { return recipe; } }
        /// <summary>Number of recorded schema nodes.</summary>
        public int NodeCount { get { return nodes == null ? 0 : nodes.Count; } }
        /// <summary>Human-readable baseline timestamp.</summary>
        public string CapturedUtc { get { return capturedUtc; } }
    }

    /// <summary>Proposed source-side change; v0.3 does not apply any of these operations.</summary>
    public enum UIAuthoringChangeKind
    {
        /// <summary>No detected source-definition difference.</summary>
        Unchanged,
        /// <summary>A new schema ID would create an object.</summary>
        Add,
        /// <summary>A removed schema ID would remove an object.</summary>
        Remove,
        /// <summary>The definition, order or label policy changed.</summary>
        Update,
        /// <summary>The kind or resolved source prefab identity changed.</summary>
        Replace,
        /// <summary>A shared source dependency changed; review scope is deliberately conservative.</summary>
        Review
    }

    /// <summary>One immutable preview row, with source change and scene conflict represented separately.</summary>
    public sealed class UIAuthoringChangeRow
    {
        /// <summary>Schema node ID.</summary>
        public string NodeId { get; internal set; }
        /// <summary>Proposed source-side operation.</summary>
        public UIAuthoringChangeKind Change { get; internal set; }
        /// <summary>True when scene state is missing, altered or cannot be safely inspected.</summary>
        public bool HasConflict { get; internal set; }
        /// <summary>Source change and conflict explanation.</summary>
        public string Message { get; internal set; }
    }

    /// <summary>Read-only preview report; never an approval to mutate scene objects or source files.</summary>
    public sealed class UIAuthoringChangePreviewResult
    {
        /// <summary>Schema-order rows followed by removed IDs.</summary>
        public IReadOnlyList<UIAuthoringChangeRow> Rows { get; internal set; }
            = new List<UIAuthoringChangeRow>().AsReadOnly();
        /// <summary>Blocking input or scene errors.</summary>
        public IReadOnlyList<string> Errors { get; internal set; } = new List<string>().AsReadOnly();
        /// <summary>Shared source changes or scope notes.</summary>
        public IReadOnlyList<string> Notices { get; internal set; } = new List<string>().AsReadOnly();
        /// <summary>Whether any blocking error or per-node scene conflict was found.</summary>
        public bool HasConflicts
        {
            get
            {
                if (Errors.Count != 0) return true;
                foreach (UIAuthoringChangeRow row in Rows) if (row.HasConflict) return true;
                return false;
            }
        }
    }
}
