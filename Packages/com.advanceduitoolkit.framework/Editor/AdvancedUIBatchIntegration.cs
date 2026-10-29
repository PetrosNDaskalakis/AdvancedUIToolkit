using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AdvancedUIToolkit.Editor
{
    /// <summary>Determines whether a batch step creates a plain container or a linked prefab.</summary>
    public enum UIEditorBatchStepKind
    {
        /// <summary>Create a normal RectTransform container with no binding prefix requirement.</summary>
        Container = 0,
        /// <summary>Instantiate a saved prefab template using an explicit Style Profile.</summary>
        Prefab = 1
    }

    /// <summary>
    /// One ordered batch instruction. A step can be parented to the request's RootParent,
    /// or to the ID of an earlier Container step. A prefab can never be a batch parent.
    /// </summary>
    public sealed class UIEditorBatchStep
    {
        /// <summary>Unique nonblank identifier used only for linking steps within this batch.</summary>
        public string Id { get; set; }
        /// <summary>Earlier Container step ID, or null/empty to use the request's RootParent.</summary>
        public string ParentId { get; set; }
        /// <summary>Object name; mandatory for containers and optional for prefab instances.</summary>
        public string Name { get; set; }
        /// <summary>Whether to create a container or registered prefab.</summary>
        public UIEditorBatchStepKind StepKind { get; set; }
        /// <summary>Explicit Style Profile for a prefab step; ignored for containers.</summary>
        public UIStyleProfile Profile { get; set; }
        /// <summary>Prefab component role; ignored for containers.</summary>
        public UIComponentKind ComponentKind { get; set; }
        /// <summary>Custom template ID when ComponentKind is Custom.</summary>
        public string CustomTemplateId { get; set; }
        /// <summary>Optional source-identity-checked text replacement for this prefab instance.</summary>
        public UIEditorTextOverride TextOverride { get; set; }
        /// <summary>Allow a Custom decorative prefab with a nonbinding instance name.</summary>
        public bool AllowUnbound { get; set; }
    }

    /// <summary>Ordered instructions for one reversible native Unity hierarchy creation.</summary>
    public sealed class UIEditorBatchRequest
    {
        /// <summary>Existing editable scene RectTransform outside prefab instances.</summary>
        public RectTransform RootParent { get; set; }
        /// <summary>Nonempty ordered operations; parents must precede their children.</summary>
        public IList<UIEditorBatchStep> Steps { get; } = new List<UIEditorBatchStep>();
        /// <summary>Optional name for Unity's single Undo step.</summary>
        public string UndoName { get; set; }
    }

    /// <summary>Result of a batch transaction; failed results expose no created objects.</summary>
    public sealed class UIEditorBatchResult
    {
        /// <summary>True only when every instruction completed successfully.</summary>
        public bool Succeeded { get; internal set; }
        /// <summary>Human-readable failure, if any.</summary>
        public string Error { get; internal set; }
        /// <summary>Index of a failed step, or -1 when no specific step failed.</summary>
        public int FailedStepIndex { get; internal set; } = -1;
        /// <summary>Snapshot mapping every completed step's ID to its scene GameObject.</summary>
        public IReadOnlyDictionary<string, GameObject> CreatedObjects { get; internal set; }
            = new Dictionary<string, GameObject>();
    }

    /// <summary>
    /// Editor-only transactional creation boundary for external packages.
    /// Does not change the interactive Library's default selection/Undo behavior.
    /// </summary>
    public static class AdvancedUIBatchIntegration
    {
        /// <summary>Prevents a callback from starting a nested Undo transaction.</summary>
        private static bool batchInProgress;

        /// <summary>
        /// Preflights the entire ordered hierarchy without creating GameObjects or Undo entries.
        /// Checks scene-parent rules, unique step IDs, parent references, templates,
        /// binding names and selected source-text identities.
        /// </summary>
        /// <param name="request">Batch to inspect.</param>
        /// <returns>Valid or an actionable error; later Unity execution can still fail.</returns>
        public static UIEditorValidationResult ValidateBatch(UIEditorBatchRequest request)
        {
            return TryValidate(request, out string error, out int ignoredIndex)
                ? new UIEditorValidationResult { Succeeded = true }
                : new UIEditorValidationResult { Succeeded = false, Error = error };
        }

        /// <summary>
        /// Creates every step inside one Unity Undo group. A failure reverts the group
        /// without leaving partially created children; success collapses to one Ctrl+Z.
        /// Selection is never deliberately changed and is restored if Unity changes it.
        /// No C# file generation is performed by this method.
        /// </summary>
        /// <param name="request">Ordered request with explicit profiles and valid parent references.</param>
        /// <returns>Created object map on success, or an error and failed step index.</returns>
        public static UIEditorBatchResult CreateBatch(UIEditorBatchRequest request)
        {
            if (batchInProgress)
                return new UIEditorBatchResult { Error = "Nested batch creation is not supported." };
            if (!TryValidate(request, out string validationError, out int invalidStep))
                return new UIEditorBatchResult { Error = validationError, FailedStepIndex = invalidStep };

            Dictionary<string, GameObject> created = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            UnityEngine.Object[] originalSelection = Selection.objects;
            batchInProgress = true;
            int group = -1;
            int currentStep = -1;
            bool groupStarted = false;
            // Pause automatic generation across all creation/rollback callbacks.
            // When the scope ends, the monitor performs one debounced rescan.
            using (AdvancedUIToolkitAutomationMonitor.Suspend())
            try
            {
                Undo.IncrementCurrentGroup();
                group = Undo.GetCurrentGroup();
                groupStarted = true;
                string groupName = string.IsNullOrWhiteSpace(request.UndoName)
                    ? "Advanced UI Toolkit: Create UI Hierarchy" : request.UndoName.Trim();
                Undo.SetCurrentGroupName(groupName);

                for (int index = 0; index < request.Steps.Count; index++)
                {
                    currentStep = index;
                    UIEditorBatchStep step = request.Steps[index];
                    RectTransform parent = string.IsNullOrEmpty(step.ParentId)
                        ? request.RootParent
                        : created[step.ParentId].GetComponent<RectTransform>();
                    GameObject result;
                    if (step.StepKind == UIEditorBatchStepKind.Container)
                    {
                        result = UIStyleFactory.CreateEmptyContainer(parent, step.Name,
                            updateSelection: false, joinUndoGroup: true);
                    }
                    else
                    {
                        if (!AdvancedUIEditorIntegration.TryResolveTemplate(step.Profile,
                            step.ComponentKind, step.CustomTemplateId,
                            out UIComponentTemplate template, out string templateError))
                            throw new InvalidOperationException(templateError);
                        if (!TryBuildTextOverride(step.TextOverride,
                            out UIPrefabTextOverrideRequest overrideRequest, out string textError))
                            throw new InvalidOperationException(textError);
                        result = UIStyleFactory.CreatePrefab(parent, template,
                            UIStyleFactory.DefaultName(template.Kind), step.Name,
                            overrideRequest, updateSelection: false, joinUndoGroup: true,
                            allowUnbound: step.AllowUnbound);
                    }
                    if (result == null)
                        throw new InvalidOperationException("Factory did not create '" + step.Id +
                            "'. Check Unity Console for the underlying prefab error.");
                    created.Add(step.Id, result);
                }

                // Unity's object registrations all belong to this group; do not split it.
                Undo.CollapseUndoOperations(group);
                return new UIEditorBatchResult
                {
                    Succeeded = true,
                    CreatedObjects = new Dictionary<string, GameObject>(created, StringComparer.Ordinal)
                };
            }
            catch (Exception exception)
            {
                string error = "Batch creation failed: " + exception.Message;
                if (groupStarted)
                {
                    try
                    {
                        Undo.RevertAllDownToGroup(group);
                    }
                    catch (Exception rollbackException)
                    {
                        error += " Rollback error: " + rollbackException.Message;
                        // Do not intentionally leave scene objects behind when Unity's
                        // Undo rollback fails. This fallback cannot repair Undo history.
                        foreach (GameObject item in created.Values)
                        {
                            if (item == null) continue;
                            try
                            {
                                UnityEngine.Object.DestroyImmediate(item);
                            }
                            catch (Exception cleanupException)
                            {
                                error += " Cleanup error: " + cleanupException.Message;
                            }
                        }
                    }
                }
                return new UIEditorBatchResult
                {
                    FailedStepIndex = currentStep,
                    Error = error
                };
            }
            finally
            {
                try
                {
                    if (!SameSelection(originalSelection, Selection.objects))
                        Selection.objects = originalSelection;
                }
                finally
                {
                    batchInProgress = false;
                }
            }
        }

        /// <summary>Checks a batch without touching scene objects or the current selection.</summary>
        /// <param name="request">Request to validate.</param>
        /// <param name="error">Actionable error, if validation fails.</param>
        /// <param name="failedStepIndex">Invalid step index, or -1 for a request-level error.</param>
        /// <returns>True when the whole plan passes structural and Factory checks.</returns>
        private static bool TryValidate(UIEditorBatchRequest request, out string error,
            out int failedStepIndex)
        {
            error = null;
            failedStepIndex = -1;
            if (request == null) { error = "Batch request cannot be null."; return false; }
            if (request.RootParent == null ||
                !UIHierarchySelection.IsSelectableContainer(request.RootParent.gameObject))
            {
                error = "RootParent must be an ordinary non-prefab scene RectTransform.";
                return false;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                error = "Batch creation is unavailable during Play Mode, compilation, or import.";
                return false;
            }
            if (request.Steps == null || request.Steps.Count == 0)
            {
                error = "At least one batch step is required.";
                return false;
            }
            Dictionary<string, UIEditorBatchStepKind> ids =
                new Dictionary<string, UIEditorBatchStepKind>(StringComparer.Ordinal);
            for (int index = 0; index < request.Steps.Count; index++)
            {
                failedStepIndex = index;
                UIEditorBatchStep step = request.Steps[index];
                if (step == null) { error = "Step cannot be null."; return false; }
                if (string.IsNullOrWhiteSpace(step.Id) || step.Id != step.Id.Trim())
                {
                    error = "Each step needs a nonblank ID without leading/trailing spaces.";
                    return false;
                }
                if (ids.ContainsKey(step.Id))
                {
                    error = "Duplicate step ID '" + step.Id + "'.";
                    return false;
                }
                if (!string.IsNullOrEmpty(step.ParentId) &&
                    (!ids.TryGetValue(step.ParentId, out UIEditorBatchStepKind parentKind) ||
                     parentKind != UIEditorBatchStepKind.Container))
                {
                    error = "ParentId '" + step.ParentId +
                        "' must identify an earlier ordinary Container step.";
                    return false;
                }
                UIEditorValidationResult validation;
                if (step.StepKind == UIEditorBatchStepKind.Container)
                {
                    validation = AdvancedUIEditorIntegration.ValidateContainer(request.RootParent,
                        step.Name);
                }
                else if (step.StepKind == UIEditorBatchStepKind.Prefab)
                {
                    validation = AdvancedUIEditorIntegration.Validate(new UIEditorCreationRequest
                    {
                        Parent = request.RootParent,
                        Profile = step.Profile,
                        Kind = step.ComponentKind,
                        CustomTemplateId = step.CustomTemplateId,
                        InstanceName = step.Name,
                        TextOverride = step.TextOverride,
                        AllowUnbound = step.AllowUnbound
                    });
                }
                else
                {
                    error = "Unknown batch step kind: " + step.StepKind;
                    return false;
                }
                if (!validation.Succeeded)
                {
                    error = "Step '" + step.Id + "': " + validation.Error;
                    return false;
                }
                ids.Add(step.Id, step.StepKind);
            }
            failedStepIndex = -1;
            return true;
        }

        /// <summary>Maps validated public source-label identity into the Factory's internal request.</summary>
        /// <param name="source">Optional one-target override.</param>
        /// <param name="converted">Factory-compatible override.</param>
        /// <param name="error">Reason for failure.</param>
        /// <returns>True when override configuration is valid.</returns>
        private static bool TryBuildTextOverride(UIEditorTextOverride source,
            out UIPrefabTextOverrideRequest converted, out string error)
        {
            converted = default(UIPrefabTextOverrideRequest);
            error = null;
            if (source == null || !source.Enabled) return true;
            if (source.SourceComponent == null)
            {
                error = "Choose a saved source-prefab text target before enabling an override.";
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

        /// <summary>Compares two selection snapshots without allocating a new array.</summary>
        /// <param name="left">Selection before batch creation.</param>
        /// <param name="right">Selection after batch creation.</param>
        /// <returns>True when both selections contain the same objects in order.</returns>
        private static bool SameSelection(UnityEngine.Object[] left, UnityEngine.Object[] right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int index = 0; index < left.Length; index++)
                if (left[index] != right[index]) return false;
            return true;
        }
    }
}
