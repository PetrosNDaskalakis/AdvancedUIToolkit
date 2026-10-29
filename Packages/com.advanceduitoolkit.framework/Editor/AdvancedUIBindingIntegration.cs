using System;
using UnityEditor;
using UnityEngine;

namespace AdvancedUIToolkit.Editor
{
    /// <summary>Outcomes returned by headless binding inspection and generation.</summary>
    public enum UIEditorBindingStatus
    {
        /// <summary>The saved binding manifest matches its UI hierarchy.</summary>
        UpToDate = 0,
        /// <summary>Safe additions or hierarchy renames are awaiting generation.</summary>
        PendingChanges = 1,
        /// <summary>Previously generated accessors would be removed.</summary>
        PendingRemoval = 2,
        /// <summary>C# and binding manifest were generated successfully.</summary>
        Generated = 3,
        /// <summary>Initial class, namespace and output settings must be supplied.</summary>
        SetupRequired = 4,
        /// <summary>The operation is invalid or blocked by a safety check.</summary>
        Blocked = 5
    }

    /// <summary>Read-only summary of one Editor-only Binder operation.</summary>
    public sealed class UIEditorBindingResult
    {
        /// <summary>Whether generation or inspection was blocked.</summary>
        public bool Succeeded => Status != UIEditorBindingStatus.Blocked &&
            Status != UIEditorBindingStatus.SetupRequired;
        /// <summary>Current binding state, including non-error pending states.</summary>
        public UIEditorBindingStatus Status { get; internal set; }
        /// <summary>A human-readable reason or next action.</summary>
        public string Message { get; internal set; }
    }

    /// <summary>
    /// Initial binding generation configuration for an ordinary, editable UI root.
    /// The generator writes a C# file under Assets and saves a UIBindingRoot manifest.
    /// </summary>
    public sealed class UIEditorBindingSetup
    {
        /// <summary>Existing scene GameObject or a root opened in Prefab Mode.</summary>
        public GameObject Target { get; set; }
        /// <summary>Legal generated C# class name.</summary>
        public string ClassName { get; set; }
        /// <summary>Legal dotted C# namespace.</summary>
        public string NamespaceName { get; set; }
        /// <summary>Generated destination folder relative to the Unity project, under Assets/.</summary>
        public string OutputFolder { get; set; }
        /// <summary>Optional custom C# generation template, or null for the built-in template.</summary>
        public MonoScript TemplateScript { get; set; }
    }

    /// <summary>
    /// Public non-visual Binder facade for independent Unity Editor assemblies.
    /// Reuses the normal Binder scan, saved generation profile, file ownership checks,
    /// XML documentation generation and rollback safeguards.
    /// </summary>
    public static class AdvancedUIBindingIntegration
    {
        /// <summary>
        /// Prevents background Automatic Generation while an external package modifies
        /// a UI hierarchy. The outermost Dispose queues one debounced refresh.
        /// Always use a using statement or finally block; does not suppress manual API calls.
        /// </summary>
        /// <returns>A nestable disposable automation suspension scope.</returns>
        public static IDisposable SuspendAutomaticGeneration()
        {
            return AdvancedUIToolkitAutomationMonitor.Suspend();
        }

        /// <summary>
        /// Inspects a registered root without opening Binder or modifying the scene or C#.
        /// This reads the saved manifest and generation profile and validates source ownership.
        /// </summary>
        /// <param name="root">Registered UI root in an editable scene or Prefab Mode.</param>
        /// <returns>Current state and an explanation.</returns>
        public static UIEditorBindingResult InspectBindings(UIBindingRoot root)
        {
            if (IsEditorBusy()) return Blocked("Unity is compiling, importing, entering Play Mode, or processing automatic generation.");
            return Convert(UIAutoBinderWindow.InspectForAutomation(root, false));
        }

        /// <summary>
        /// Regenerates a registered root without opening Binder or showing dialogs.
        /// Deleting C# accessors requires allowRemovedAccessors=true. All normal Binder
        /// file-ownership, external-edit, hierarchy and prefab safety checks still apply.
        /// </summary>
        /// <param name="root">Root with a saved class, namespace and output folder.</param>
        /// <param name="allowRemovedAccessors">Explicit opt-in for removal of stale accessors.</param>
        /// <returns>Generation status or a noninteractive block reason.</returns>
        public static UIEditorBindingResult GenerateBindings(UIBindingRoot root,
            bool allowRemovedAccessors = false)
        {
            if (IsEditorBusy()) return Blocked("Unity is compiling, importing, entering Play Mode, or processing automatic generation.");
            if (AdvancedUIToolkitAutomationMonitor.IsSuspended)
                return Blocked("Finish the UI hierarchy transaction before generating C# bindings.");

            UIAutoGenerationResult inspection = UIAutoBinderWindow.InspectForAutomation(root, false);
            if (inspection.Status == UIAutoGenerationStatus.UpToDate ||
                inspection.Status == UIAutoGenerationStatus.SetupRequired ||
                inspection.Status == UIAutoGenerationStatus.Blocked)
                return Convert(inspection);
            if (inspection.Status == UIAutoGenerationStatus.PendingRemoval && !allowRemovedAccessors)
                return new UIEditorBindingResult
                {
                    Status = UIEditorBindingStatus.PendingRemoval,
                    Message = "Accessor removal requires explicit approval. " + inspection.Message
                };

            UIAutoGenerationResult generation = UIAutoBinderWindow.InspectForAutomation(root, true);
            if (generation.Status == UIAutoGenerationStatus.Generated)
                AdvancedUIToolkitAutomationMonitor.Schedule();
            return Convert(generation);
        }

        /// <summary>
        /// Generates the initial C# class and manifest for a new UI hierarchy without
        /// opening the Binder window. Does not rename GameObjects or overwrite an owned
        /// file. A root with an existing generation profile must use GenerateBindings.
        /// </summary>
        /// <param name="setup">Explicit generation destination and optional template.</param>
        /// <returns>Whether the first binding file was generated.</returns>
        public static UIEditorBindingResult InitializeBindings(UIEditorBindingSetup setup)
        {
            if (IsEditorBusy()) return Blocked("Unity is compiling, importing, entering Play Mode, or processing automatic generation.");
            if (AdvancedUIToolkitAutomationMonitor.IsSuspended)
                return Blocked("Finish the UI hierarchy transaction before generating C# bindings.");
            if (setup == null || setup.Target == null)
                return Blocked("Provide an editable UI root and explicit generation settings.");
            UIAutoGenerationResult result = UIAutoBinderWindow.InitializeForIntegration(setup);
            if (result.Status == UIAutoGenerationStatus.Generated)
                AdvancedUIToolkitAutomationMonitor.Schedule();
            return Convert(result);
        }

        /// <summary>Checks Editor state without inspecting or changing any UI hierarchy.</summary>
        /// <returns>True if Editor callbacks must not start a binding operation.</returns>
        private static bool IsEditorBusy()
        {
            return AdvancedUIToolkitAutomationMonitor.IsProcessing ||
                EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling || EditorApplication.isUpdating;
        }

        /// <summary>Converts internal automation state into stable public status values.</summary>
        /// <param name="result">Internal Binder result.</param>
        /// <returns>A public immutable-by-consumers result.</returns>
        private static UIEditorBindingResult Convert(UIAutoGenerationResult result)
        {
            if (result == null) return Blocked("Binder did not return an inspection result.");
            UIEditorBindingStatus status;
            switch (result.Status)
            {
                case UIAutoGenerationStatus.UpToDate:
                    status = UIEditorBindingStatus.UpToDate;
                    break;
                case UIAutoGenerationStatus.SafeChange:
                    status = UIEditorBindingStatus.PendingChanges;
                    break;
                case UIAutoGenerationStatus.PendingRemoval:
                    status = UIEditorBindingStatus.PendingRemoval;
                    break;
                case UIAutoGenerationStatus.Generated:
                    status = UIEditorBindingStatus.Generated;
                    break;
                case UIAutoGenerationStatus.SetupRequired:
                    status = UIEditorBindingStatus.SetupRequired;
                    break;
                default:
                    status = UIEditorBindingStatus.Blocked;
                    break;
            }
            return new UIEditorBindingResult { Status = status, Message = result.Message };
        }

        /// <summary>Builds a clear blocked result without throwing on invalid user input.</summary>
        /// <param name="message">Actionable reason generation was refused.</param>
        /// <returns>A blocked integration result.</returns>
        private static UIEditorBindingResult Blocked(string message)
        {
            return new UIEditorBindingResult
            {
                Status = UIEditorBindingStatus.Blocked,
                Message = message
            };
        }
    }
}
