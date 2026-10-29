using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedUIToolkit
{
    /// <summary>
    /// Stores serialized component references for a single UI screen or prefab.
    /// Each manifest entry keeps a stable identifier across hierarchy renames.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIBindingRoot : MonoBehaviour, ISerializationCallbackReceiver
    {
        /// <summary>Identifies a component and its generated C# accessor.</summary>
        [Serializable]
        public sealed class Entry
        {
            /// <summary>Public accessor name emitted by the code generator.</summary>
            public string Key;

            /// <summary>Current GameObject name after removing the naming prefix.</summary>
            public string HierarchyKey;

            /// <summary>Persistent identifier used to recognize renamed components.</summary>
            public string StableId;

            /// <summary>Full type name used by the generated accessor.</summary>
            public string ComponentType;

            /// <summary>
            /// Optional, per-property XML summary authored in Binder. An empty value uses
            /// the selected generation template's default documentation pattern.
            /// </summary>
            public string XmlSummary;

            /// <summary>Serialized reference to the actual Unity component.</summary>
            public Component Reference;
        }

        /// <summary>The binding manifest stored with this UI root.</summary>
        [SerializeField, HideInInspector]
        private List<Entry> entries = new List<Entry>();

        /// <summary>Lazy lookup built from serialized component references.</summary>
        private Dictionary<string, Component> lookup;

#if UNITY_EDITOR
        /// <summary>Name of the generated C# class for this root.</summary>
        [SerializeField, HideInInspector] private string generatedClassName;

        /// <summary>Namespace of the generated C# class.</summary>
        [SerializeField, HideInInspector] private string generatedNamespace;

        /// <summary>Project-relative folder containing generated C# files.</summary>
        [SerializeField, HideInInspector] private string generatedOutputFolder;

        /// <summary>Project-relative C# template path, or empty for default generation.</summary>
        [SerializeField, HideInInspector] private string generatedTemplatePath;
#endif

        /// <summary>
        /// Retrieves a component by its generated accessor key.
        /// The lookup is created once and reused for subsequent calls.
        /// </summary>
        /// <typeparam name="T">Expected Unity component type.</typeparam>
        /// <param name="key">Generated accessor key, excluding the GameObject prefix.</param>
        /// <returns>The registered component.</returns>
        /// <exception cref="KeyNotFoundException">No matching component of the requested type exists.</exception>
        public T Get<T>(string key) where T : Component
        {
            if (TryGet(key, out T component))
                return component;

            throw new KeyNotFoundException(
                $"Advanced UI Toolkit: {typeof(T).Name} '{key ?? "<null>"}' not found on {name}.");
        }

        /// <summary>
        /// Looks up a registered component without throwing for a missing or mismatched key.
        /// </summary>
        /// <typeparam name="T">Expected Unity component type.</typeparam>
        /// <param name="key">Accessor name to search for.</param>
        /// <param name="component">Matching component, or null when unavailable.</param>
        /// <returns>True when the key resolves to a live component of type T.</returns>
        public bool TryGet<T>(string key, out T component) where T : Component
        {
            component = null;
            if (string.IsNullOrEmpty(key))
                return false;

            if (lookup == null)
                RebuildLookup();

            if (!lookup.TryGetValue(key, out Component stored) || stored == null)
                return false;

            component = stored as T;
            return component != null;
        }

        /// <summary>Populates the lookup from the serialized manifest without hierarchy searches.</summary>
        private void RebuildLookup()
        {
            Dictionary<string, Component> rebuilt = new Dictionary<string, Component>(StringComparer.Ordinal);
            if (entries != null)
            {
                foreach (Entry entry in entries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.Key) || entry.Reference == null)
                        continue;

                    // Invalid duplicate entries are reported by Editor diagnostics. Retain
                    // the first entry here to match the behavior of previous releases.
                    if (!rebuilt.ContainsKey(entry.Key))
                        rebuilt.Add(entry.Key, entry.Reference);
                }
            }
            lookup = rebuilt;
        }

        /// <summary>Called by Unity before serializing this component; no work is necessary.</summary>
        public void OnBeforeSerialize() { }

        /// <summary>Invalidates the cached lookup when Unity restores the serialized manifest.</summary>
        public void OnAfterDeserialize()
        {
            lookup = null;
        }

#if UNITY_EDITOR
        /// <summary>Whether this root has a saved class, namespace, and output folder.</summary>
        public bool EditorHasGenerationProfile =>
            !string.IsNullOrEmpty(generatedClassName) &&
            !string.IsNullOrEmpty(generatedNamespace) &&
            !string.IsNullOrEmpty(generatedOutputFolder);

        /// <summary>Reads generation options previously saved for this UI root.</summary>
        /// <param name="className">Stored generated class name.</param>
        /// <param name="namespaceName">Stored namespace.</param>
        /// <param name="outputFolder">Stored source output folder.</param>
        public void EditorGetGenerationProfile(out string className,
            out string namespaceName, out string outputFolder)
        {
            className = generatedClassName;
            namespaceName = generatedNamespace;
            outputFolder = generatedOutputFolder;
        }

        /// <summary>Stores the options required for automated regeneration.</summary>
        /// <param name="className">Generated binding class name.</param>
        /// <param name="namespaceName">Generated class namespace.</param>
        /// <param name="outputFolder">Project-relative source folder.</param>
        public void EditorSetGenerationProfile(string className,
            string namespaceName, string outputFolder)
        {
            generatedClassName = className;
            generatedNamespace = namespaceName;
            generatedOutputFolder = outputFolder;
        }

        /// <summary>Returns the configured C# generation template path.</summary>
        /// <returns>Project-relative path, or an empty string.</returns>
        public string EditorGetGenerationTemplatePath() => generatedTemplatePath;

        /// <summary>Stores the optional C# example template for this root.</summary>
        /// <param name="path">Project asset path, or empty to use the default generator.</param>
        public void EditorSetGenerationTemplatePath(string path)
        {
            generatedTemplatePath = path ?? string.Empty;
        }

        /// <summary>Returns the serialized manifest for editor validation.</summary>
        /// <returns>Read-only collection of existing binding entries.</returns>
        public IReadOnlyList<Entry> EditorGetEntries() => entries;

        /// <summary>Replaces the manifest and invalidates the runtime reference cache.</summary>
        /// <param name="value">New validated manifest entries.</param>
        public void EditorSetEntries(List<Entry> value)
        {
            entries = value ?? new List<Entry>();
            lookup = null;
        }
#endif
    }
}
