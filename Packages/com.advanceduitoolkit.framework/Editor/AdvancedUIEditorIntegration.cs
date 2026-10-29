using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedUIToolkit.Editor
{
    /// <summary>Options for creating one linked prefab through the public Editor integration API.</summary>
    public sealed class UIEditorCreationRequest
    {
        /// <summary>Explicit profile to resolve; never reads the Library's active profile.</summary>
        public UIStyleProfile Profile { get; set; }
        /// <summary>Ordinary non-prefab RectTransform in an editable scene.</summary>
        public RectTransform Parent { get; set; }
        /// <summary>Registered component kind; use Custom with a matching CustomTemplateId.</summary>
        public UIComponentKind Kind { get; set; }
        /// <summary>Custom template ID, required when Kind is Custom.</summary>
        public string CustomTemplateId { get; set; }
        /// <summary>Optional name; blank uses the template's default or the built-in fallback.</summary>
        public string InstanceName { get; set; }
        /// <summary>Optional single-label override; disabled by default.</summary>
        public UIEditorTextOverride TextOverride { get; set; }
        /// <summary>
        /// Select and ping the resulting instance when true. Defaults to false so external
        /// generators do not replace the user's active hierarchy selection.
        /// </summary>
        public bool UpdateSelection { get; set; }
        /// <summary>
        /// External-only: allow a Custom prefab with an ordinary, nonbinding object
        /// name (for decoration). Prefab instances remain unavailable as parents.
        /// </summary>
        public bool AllowUnbound { get; set; }
    }

    /// <summary>One identity-checked text replacement on a newly created prefab instance.</summary>
    public sealed class UIEditorTextOverride
    {
        /// <summary>Enables the override. False preserves the prefab's original text.</summary>
        public bool Enabled { get; set; }
        /// <summary>
        /// Component selected from GetTextTargets on the saved source prefab.
        /// A removed or replaced source component causes validation to fail.
        /// </summary>
        public Component SourceComponent { get; set; }
        /// <summary>Replacement text. Null or empty intentionally clears the target when enabled.</summary>
        public string Text { get; set; }
    }

    /// <summary>A supported text component discovered on a prefab asset, including inactive children.</summary>
    public sealed class UIEditorTextTarget
    {
        /// <summary>Source-prefab component used to identify the intended label.</summary>
        public Component SourceComponent { get; internal set; }
        /// <summary>Readable relative path and component type.</summary>
        public string Label { get; internal set; }
        /// <summary>Text stored on the source prefab.</summary>
        public string CurrentText { get; internal set; }
    }

    /// <summary>Nonthrowing validation result returned to an external Editor assembly.</summary>
    public sealed class UIEditorValidationResult
    {
        /// <summary>Whether validation succeeded.</summary>
        public bool Succeeded { get; internal set; }
        /// <summary>Human-readable error when Succeeded is false.</summary>
        public string Error { get; internal set; }
    }

    /// <summary>Result of a public Editor creation operation.</summary>
    public sealed class UIEditorCreationResult
    {
        /// <summary>True only when CreatedObject is not null.</summary>
        public bool Succeeded => CreatedObject != null;
        /// <summary>Newly created scene object, or null when creation failed.</summary>
        public GameObject CreatedObject { get; internal set; }
        /// <summary>Validation or execution error, or null on success.</summary>
        public string Error { get; internal set; }
    }

    /// <summary>
    /// Public entry point for external Editor packages. Delegates to the existing
    /// Library/Factory implementation and never stores a global active profile.
    /// Batch creation is available through CreateBatch; headless Binder generation
    /// is not part of this release.
    /// </summary>
    public static class AdvancedUIEditorIntegration
    {
        /// <summary>Resolves a template from an explicit profile and its inherited profiles.</summary>
        /// <param name="profile">Source profile asset.</param>
        /// <param name="kind">Standard kind, or Custom for a custom template ID.</param>
        /// <param name="customTemplateId">Required if kind is Custom.</param>
        /// <param name="template">Resolved entry on success.</param>
        /// <param name="error">Reason resolution failed.</param>
        /// <returns>True when a registered prefab template was found.</returns>
        public static bool TryResolveTemplate(UIStyleProfile profile, UIComponentKind kind,
            string customTemplateId, out UIComponentTemplate template, out string error)
        {
            template = null;
            error = null;
            if (profile == null)
                error = "A Style Profile must be explicitly provided.";
            else if (kind == UIComponentKind.Custom && string.IsNullOrWhiteSpace(customTemplateId))
                error = "A nonempty CustomTemplateId is required for a custom template.";
            else if (!(kind == UIComponentKind.Custom
                ? profile.TryResolveCustom(customTemplateId, out template)
                : profile.TryResolve(kind, out template)))
                error = "No prefab template for " + (kind == UIComponentKind.Custom
                    ? "Custom:" + customTemplateId : "Role:" + kind) + " in the selected profile.";
            return error == null;
        }

        /// <summary>Finds supported source text targets, including disabled descendants and optional TMP.</summary>
        /// <param name="prefabAsset">Saved prefab source; scene instances are not accepted.</param>
        /// <returns>A snapshot of available text targets, or an empty list for an invalid source.</returns>
        public static IReadOnlyList<UIEditorTextTarget> GetTextTargets(GameObject prefabAsset)
        {
            List<UIEditorTextTarget> result = new List<UIEditorTextTarget>();
            if (prefabAsset == null || !UnityEditor.AssetDatabase.Contains(prefabAsset))
                return result.AsReadOnly();
            foreach (UIPrefabTextOverride.Target target in UIPrefabTextOverride.FindTargets(prefabAsset))
                result.Add(new UIEditorTextTarget
                {
                    SourceComponent = target.Component,
                    Label = target.Label,
                    CurrentText = target.CurrentText
                });
            return result.AsReadOnly();
        }

        /// <summary>Checks the requested prefab creation without changing scene or Editor state.</summary>
        /// <param name="request">Profile, parent, template role and optional label override.</param>
        /// <returns>Success or a human-readable validation reason.</returns>
        public static UIEditorValidationResult Validate(UIEditorCreationRequest request)
        {
            if (request == null) return Invalid("Creation request cannot be null.");
            if (!TryResolveTemplate(request.Profile, request.Kind, request.CustomTemplateId,
                out UIComponentTemplate template, out string reason)) return Invalid(reason);
            if (!TryBuildTextRequest(request.TextOverride, out UIPrefabTextOverrideRequest textRequest, out reason))
                return Invalid(reason);
            bool valid = UIStyleFactory.TryValidatePrefabCreation(request.Parent, template,
                UIStyleFactory.DefaultName(template.Kind), request.InstanceName, textRequest,
                out _, out _, out reason, request.AllowUnbound);
            return new UIEditorValidationResult { Succeeded = valid, Error = reason };
        }

        /// <summary>
        /// Creates one linked prefab under a valid scene UI container using an explicitly
        /// selected profile. Uses the Factory's existing per-instance Undo and validated
        /// text-target path. CreateBatch provides a separate grouped creation path.
        /// </summary>
        /// <param name="request">Creation settings, including an optional source-label identity.</param>
        /// <returns>The created instance or an error, without showing Editor dialogs.</returns>
        public static UIEditorCreationResult Create(UIEditorCreationRequest request)
        {
            UIEditorValidationResult validation = Validate(request);
            if (!validation.Succeeded)
                return new UIEditorCreationResult { Error = validation.Error };
            TryResolveTemplate(request.Profile, request.Kind, request.CustomTemplateId,
                out UIComponentTemplate template, out _);
            TryBuildTextRequest(request.TextOverride, out UIPrefabTextOverrideRequest textRequest,
                out _);
            try
            {
                GameObject result = UIStyleFactory.CreatePrefab(request.Parent, template,
                    UIStyleFactory.DefaultName(template.Kind), request.InstanceName,
                    textRequest, request.UpdateSelection,
                    joinUndoGroup: false, allowUnbound: request.AllowUnbound);
                return new UIEditorCreationResult
                {
                    CreatedObject = result,
                    Error = result == null ? "Prefab creation did not complete; inspect the Unity Console." : null
                };
            }
            catch (Exception exception)
            {
                return new UIEditorCreationResult { Error = "Prefab creation failed: " + exception.Message };
            }
        }

        /// <summary>Validates an ordinary container creation without modifying the scene.</summary>
        /// <param name="parent">Non-prefab scene RectTransform.</param>
        /// <param name="name">Name of the new container; binding prefixes are not required.</param>
        /// <returns>Success or reason for refusal.</returns>
        public static UIEditorValidationResult ValidateContainer(RectTransform parent, string name)
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return Invalid("Container creation is unavailable in Play Mode.");
            if (parent == null || !UIHierarchySelection.IsSelectableContainer(parent.gameObject))
                return Invalid("Select an ordinary, non-prefab scene RectTransform parent.");
            if (string.IsNullOrWhiteSpace(name))
                return Invalid("Container name cannot be empty.");
            return new UIEditorValidationResult { Succeeded = true };
        }

        /// <summary>Creates one ordinary RectTransform container with the Factory's existing Undo behavior.</summary>
        /// <param name="parent">Target non-prefab scene UI container.</param>
        /// <param name="name">Any nonblank hierarchy name.</param>
        /// <param name="updateSelection">Whether to select the new container (default false).</param>
        /// <returns>The created container or a descriptive error.</returns>
        public static UIEditorCreationResult CreateContainer(RectTransform parent, string name,
            bool updateSelection = false)
        {
            UIEditorValidationResult validation = ValidateContainer(parent, name);
            if (!validation.Succeeded) return new UIEditorCreationResult { Error = validation.Error };
            try
            {
                GameObject result = UIStyleFactory.CreateEmptyContainer(parent, name, updateSelection);
                return new UIEditorCreationResult
                {
                    CreatedObject = result,
                    Error = result == null ? "Container creation did not complete." : null
                };
            }
            catch (Exception exception)
            {
                return new UIEditorCreationResult { Error = "Container creation failed: " + exception.Message };
            }
        }

        /// <summary>
        /// Preflights a complete ordered hierarchy without creating scene objects.
        /// All prefab profiles and parent references are supplied explicitly.
        /// </summary>
        /// <param name="request">Nonempty batch definition.</param>
        /// <returns>Validation outcome with no Unity scene or Undo side effects.</returns>
        public static UIEditorValidationResult ValidateBatch(UIEditorBatchRequest request)
        {
            return AdvancedUIBatchIntegration.ValidateBatch(request);
        }

        /// <summary>
        /// Creates a hierarchy as one Undo operation. Failure attempts to revert all
        /// objects created during the batch; no C# binding files are generated here.
        /// </summary>
        /// <param name="request">Valid ordered batch definition.</param>
        /// <returns>Created objects keyed by step ID, or a failure result.</returns>
        public static UIEditorBatchResult CreateBatch(UIEditorBatchRequest request)
        {
            return AdvancedUIBatchIntegration.CreateBatch(request);
        }

        /// <summary>Validates public text options before converting them to the internal request.</summary>
        /// <param name="source">Optional external override request.</param>
        /// <param name="converted">Corresponding internal request.</param>
        /// <param name="error">Reason for refusal.</param>
        /// <returns>Whether the request is valid enough for source-component verification.</returns>
        private static bool TryBuildTextRequest(UIEditorTextOverride source,
            out UIPrefabTextOverrideRequest converted, out string error)
        {
            converted = default(UIPrefabTextOverrideRequest);
            error = null;
            if (source == null || !source.Enabled) return true;
            if (source.SourceComponent == null)
            {
                error = "Select a text target from GetTextTargets before enabling an override.";
                return false;
            }
            converted = new UIPrefabTextOverrideRequest
            {
                Enabled = true,
                RequireSourceComponent = true,
                SourceComponent = source.SourceComponent,
                Text = source.Text
            };
            return true;
        }

        /// <summary>Creates a common failed validation result.</summary>
        /// <param name="error">The human-readable error.</param>
        /// <returns>Failed validation.</returns>
        private static UIEditorValidationResult Invalid(string error)
        {
            return new UIEditorValidationResult { Succeeded = false, Error = error };
        }
    }
}
