using System;
using System.Collections.Generic;
using AdvancedUIAuthoring.Editor;
using AdvancedUIToolkit;
using AdvancedUIToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace AdvancedUIAuthoring.Integration.Editor
{
    /// <summary>Non-mutating validation or transactional hierarchy creation result.</summary>
    public sealed class UIAuthoringBridgeResult
    {
        /// <summary>Whether validation or hierarchy construction completed.</summary>
        public bool Succeeded { get; internal set; }
        /// <summary>Actionable failure description, empty on success.</summary>
        public string Error { get; internal set; } = string.Empty;
        /// <summary>Created schema nodes; empty for validation and failures.</summary>
        public IReadOnlyDictionary<string, GameObject> CreatedObjects { get; internal set; }
            = new Dictionary<string, GameObject>();
        /// <summary>New container selected by the recipe's bindingRootId; not selected in Unity.</summary>
        public GameObject BindingRoot { get; internal set; }
    }

    /// <summary>Independent Editor bridge using only public Authoring and Toolkit APIs.</summary>
    public static class UIAuthoringToolkitBridge
    {
        /// <summary>Prevents reentrant bridge creation from Editor callbacks.</summary>
        private static bool generating;

        /// <summary>Complete, validated plan retained only for one synchronous call.</summary>
        private sealed class Plan
        {
            /// <summary>Parsed schema snapshot.</summary>
            public UIAuthoringDocument Document;
            /// <summary>Parsed stylesheet snapshot.</summary>
            public UIAuthoringStyleSheet Sheet;
            /// <summary>Prevalidated root-only layout plans keyed by prefab node ID.</summary>
            public readonly Dictionary<string, UIAuthoringRootLayoutPlan> Layouts =
                new Dictionary<string, UIAuthoringRootLayoutPlan>(StringComparer.Ordinal);

            /// <summary>Prefab requests keyed by schema ID.</summary>
            public readonly Dictionary<string, UIEditorBatchStep> Prefabs =
                new Dictionary<string, UIEditorBatchStep>(StringComparer.Ordinal);
        }

        /// <summary>Validate all nodes, templates, labels and target policy without scene mutations.</summary>
        public static UIAuthoringBridgeResult Validate(UIAuthoringIntegrationRecipe recipe, RectTransform parent)
        {
            try
            {
                BuildPlan(recipe, parent);
                return new UIAuthoringBridgeResult { Succeeded = true };
            }
            catch (Exception exception) { return Failure(exception.Message); }
        }

        /// <summary>Read-only source preflight; existing generated root names are checked by the preview service.</summary>
        internal static UIAuthoringBridgeResult ValidateForPreview(UIAuthoringIntegrationRecipe recipe, RectTransform parent)
        {
            try
            {
                BuildPlan(recipe, parent, true);
                return new UIAuthoringBridgeResult { Succeeded = true };
            }
            catch (Exception exception) { return Failure(exception.Message); }
        }

        /// <summary>Create a complete hierarchy with grouped Undo, rollback and automation suspension.</summary>
        public static UIAuthoringBridgeResult Generate(UIAuthoringIntegrationRecipe recipe, RectTransform parent)
        {
            if (generating) return Failure("An integration generation is already in progress.");
            Plan plan;
            try { plan = BuildPlan(recipe, parent); }
            catch (Exception exception) { return Failure(exception.Message); }
            generating = true;
            UnityEngine.Object[] selection = Selection.objects;
            Dictionary<string, GameObject> created = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            int group = -1;
            try
            {
                using (AdvancedUIBindingIntegration.SuspendAutomaticGeneration())
                {
                    Undo.IncrementCurrentGroup();
                    group = Undo.GetCurrentGroup();
                    Undo.SetCurrentGroupName("Author UI with Toolkit: " + plan.Document.name);
                    try
                    {
                        foreach (UIAuthoringNode node in plan.Document.nodes)
                        {
                            RectTransform target = string.IsNullOrEmpty(node.parentId) ? parent
                                : created[node.parentId].GetComponent<RectTransform>();
                            GameObject element;
                            UIEditorBatchStep prefab;
                            if (plan.Prefabs.TryGetValue(node.id, out prefab))
                            {
                                // Each existing service owns its Undo group; the outer group joins
                                // all child operations without requiring internal framework access.
                                var request = new UIEditorBatchRequest { RootParent = target };
                                prefab.ParentId = string.Empty;
                                request.Steps.Add(prefab);
                                UIEditorBatchResult result = AdvancedUIEditorIntegration.CreateBatch(request);
                                if (!result.Succeeded) throw new InvalidOperationException(result.Error);
                                element = result.CreatedObjects[node.id];
                            }
                            else
                            {
                                // Clone to localize the parent reference without mutating the user's schema.
                                var copy = JsonUtility.FromJson<UIAuthoringNode>(JsonUtility.ToJson(node));
                                copy.parentId = string.Empty;
                                var single = new UIAuthoringDocument { name = plan.Document.name };
                                single.nodes.Add(copy);
                                UIAuthoringGenerationResult result = UIAuthoringGenerator.Generate(single, target, plan.Sheet);
                                if (!result.Succeeded) throw new InvalidOperationException(result.Error);
                                element = result.CreatedElements[node.id];
                            }
                            created.Add(node.id, element);
                            UIAuthoringRootLayoutPlan layout;
                            if (plan.Layouts.TryGetValue(node.id, out layout)) layout.Apply(element);
                            if (element.name != node.name)
                                throw new InvalidOperationException("Factory renamed '" + node.id +
                                    "'. Resolve sibling-name conflicts before generating.");
                        }
                        Undo.FlushUndoRecordObjects();
                        Undo.CollapseUndoOperations(group);
                        GameObject bindingRoot = null;
                        if (!string.IsNullOrEmpty(recipe.bindingRootId))
                            created.TryGetValue(recipe.bindingRootId, out bindingRoot);
                        return new UIAuthoringBridgeResult
                        {
                            Succeeded = true,
                            CreatedObjects = new Dictionary<string, GameObject>(created),
                            BindingRoot = bindingRoot
                        };
                    }
                    catch (Exception exception)
                    {
                        string error = exception.Message;
                        try { Undo.RevertAllDownToGroup(group); }
                        catch (Exception rollback) { error += " | Undo rollback: " + rollback.Message; }
                        foreach (GameObject item in created.Values)
                        {
                            if (item == null) continue;
                            try { UnityEngine.Object.DestroyImmediate(item); }
                            catch (Exception cleanup) { error += " | Cleanup: " + cleanup.Message; }
                        }
                        return Failure(error);
                    }
                }
            }
            catch (Exception exception) { return Failure(exception.Message); }
            finally
            {
                try { Selection.objects = selection; }
                finally { generating = false; }
            }
        }

        /// <summary>Inspect an existing Binder root without writes; a missing manifest needs setup.</summary>
        public static UIEditorBindingResult InspectBindings(GameObject target)
        {
            return AdvancedUIBindingIntegration.InspectBindings(
                target != null ? target.GetComponent<UIBindingRoot>() : null);
        }

        /// <summary>Initialize or synchronize Binder after scene creation; accessor removals stay blocked.</summary>
        public static UIEditorBindingResult SyncBindings(UIAuthoringIntegrationRecipe recipe, GameObject target)
        {
            UIBindingRoot root = target != null ? target.GetComponent<UIBindingRoot>() : null;
            if (root != null)
            {
                UIEditorBindingResult inspection = AdvancedUIBindingIntegration.InspectBindings(root);
                if (inspection.Status != UIEditorBindingStatus.SetupRequired)
                    return AdvancedUIBindingIntegration.GenerateBindings(root);
            }
            return AdvancedUIBindingIntegration.InitializeBindings(new UIEditorBindingSetup
            {
                Target = target,
                ClassName = recipe != null ? recipe.bindingClass : null,
                NamespaceName = recipe != null ? recipe.bindingNamespace : null,
                OutputFolder = recipe != null ? recipe.bindingOutputFolder : null,
                TemplateScript = null
            });
        }

        /// <summary>Build a public-API-only plan; exceptions are converted into structured errors.</summary>
        private static Plan BuildPlan(UIAuthoringIntegrationRecipe recipe, RectTransform parent, bool preview = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Wait until Unity finishes compilation/import and leaves Play Mode.");
            if (recipe == null || recipe.schema == null)
                throw new InvalidOperationException("Assign an Integration Recipe with a schema.");
            if (recipe.profile == null) throw new InvalidOperationException("Assign an explicit Style Profile.");
            var plan = new Plan();
            string error;
            if (!UIAuthoringSchemaParser.TryParse(recipe.schema.text, out plan.Document, out error))
                throw new InvalidOperationException(error);
            if (recipe.stylesheet != null && !UIAuthoringStyleSheetParser.TryParse(
                recipe.stylesheet.text, out plan.Sheet, out error)) throw new InvalidOperationException(error);
            UIAuthoringValidationResult validation = UIAuthoringValidator.Validate(plan.Document, parent, plan.Sheet);
            if (!validation.Succeeded) throw new InvalidOperationException(string.Join("\n", validation.Errors));

            var nodes = new Dictionary<string, UIAuthoringNode>(StringComparer.Ordinal);
            foreach (UIAuthoringNode node in plan.Document.nodes) nodes.Add(node.id, node);
            var mappings = new Dictionary<string, UIAuthoringPrefabMapping>(StringComparer.Ordinal);
            if (recipe.mappings != null)
            {
                foreach (UIAuthoringPrefabMapping mapping in recipe.mappings)
                {
                    UIAuthoringNode node;
                    if (mapping == null || string.IsNullOrEmpty(mapping.nodeId) ||
                        !nodes.TryGetValue(mapping.nodeId, out node))
                        throw new InvalidOperationException("A prefab mapping references a missing schema node.");
                    if (IsContainer(node)) throw new InvalidOperationException("Container '" + node.id + "' cannot be a prefab mapping.");
                    if (mappings.ContainsKey(mapping.nodeId))
                        throw new InvalidOperationException("Duplicate mapping for '" + mapping.nodeId + "'.");
                    mappings.Add(mapping.nodeId, mapping);
                }
            }
            if (!string.IsNullOrEmpty(recipe.bindingRootId))
            {
                UIAuthoringNode root;
                if (!nodes.TryGetValue(recipe.bindingRootId, out root) || !IsContainer(root))
                    throw new InvalidOperationException("Binding Root ID must identify a native container, or be empty.");
            }
            var batch = new UIEditorBatchRequest { RootParent = parent };
            foreach (UIAuthoringNode node in plan.Document.nodes)
            {
                if (!string.IsNullOrEmpty(node.parentId) && !IsContainer(nodes[node.parentId]))
                    throw new InvalidOperationException("Prefab node '" + node.parentId + "' cannot contain schema children.");
                if (!preview && string.IsNullOrEmpty(node.parentId))
                    for (int i = 0; i < parent.childCount; i++)
                        if (parent.GetChild(i).name == node.name)
                            throw new InvalidOperationException("Target already contains '" + node.name +
                                "'. Choose another parent or rename/remove the previous generated root.");
                var step = new UIEditorBatchStep { Id = node.id, ParentId = node.parentId, Name = node.name };
                if (IsContainer(node)) step.StepKind = UIEditorBatchStepKind.Container;
                else
                {
                    UIAuthoringRootLayoutPlan rootLayout;
                    if (!UIAuthoringRootLayoutPlan.TryCreate(node, plan.Sheet, out rootLayout, out error))
                        throw new InvalidOperationException(node.id + ": " + error);
                    UIAuthoringPrefabMapping mapping;
                    mappings.TryGetValue(node.id, out mapping);
                    step.StepKind = UIEditorBatchStepKind.Prefab;
                    step.Profile = recipe.profile;
                    step.ComponentKind = mapping != null ? mapping.kind : DefaultKind(node.kind);
                    step.CustomTemplateId = mapping != null ? mapping.customTemplateId : null;
                    step.AllowUnbound = mapping != null && mapping.allowUnbound;
                    UIComponentTemplate template;
                    if (!AdvancedUIEditorIntegration.TryResolveTemplate(step.Profile, step.ComponentKind,
                        step.CustomTemplateId, out template, out error)) throw new InvalidOperationException(node.id + ": " + error);
                    if (template == null || template.Prefab == null)
                        throw new InvalidOperationException(node.id + ": template has no saved prefab.");
                    UIAuthoringNode schemaParent = string.IsNullOrEmpty(node.parentId) ? null : nodes[node.parentId];
                    if (!rootLayout.ValidateSource(template.Prefab, schemaParent, plan.Sheet, parent, out error))
                        throw new InvalidOperationException(node.id + ": " + error);
                    plan.Layouts.Add(node.id, rootLayout);
                    UIAuthoringTextMode mode = mapping != null ? mapping.textMode :
                        (string.Equals(node.kind, "image", StringComparison.OrdinalIgnoreCase)
                            ? UIAuthoringTextMode.PreservePrefab : UIAuthoringTextMode.SingleLabel);
                    if (mapping == null && mode == UIAuthoringTextMode.PreservePrefab && !string.IsNullOrEmpty(node.text))
                        throw new InvalidOperationException(node.id + ": add an explicit text mapping to override an image prefab's label.");
                    if (!Enum.IsDefined(typeof(UIAuthoringTextMode), mode))
                        throw new InvalidOperationException(node.id + ": unknown text override mode.");
                    if (mode != UIAuthoringTextMode.PreservePrefab)
                    {
                        IReadOnlyList<UIEditorTextTarget> labels = AdvancedUIEditorIntegration.GetTextTargets(template.Prefab);
                        Component source = null;
                        if (mode == UIAuthoringTextMode.SingleLabel)
                        {
                            if (labels.Count != 1) throw new InvalidOperationException(node.id +
                                ": expected one text label; use an ExplicitSource mapping or PreservePrefab.");
                            source = labels[0].SourceComponent;
                        }
                        else
                        {
                            foreach (UIEditorTextTarget label in labels)
                                if (mapping.sourceText != null && label.SourceComponent == mapping.sourceText)
                                    source = label.SourceComponent;
                            if (source == null) throw new InvalidOperationException(node.id +
                                ": saved source text is missing or does not belong to the resolved template.");
                        }
                        step.TextOverride = new UIEditorTextOverride { Enabled = true, SourceComponent = source, Text = node.text };
                    }
                    plan.Prefabs.Add(node.id, step);
                }
                batch.Steps.Add(step);
            }
            UIEditorValidationResult batchValidation = AdvancedUIEditorIntegration.ValidateBatch(batch);
            if (!batchValidation.Succeeded) throw new InvalidOperationException(batchValidation.Error);
            return plan;
        }

        /// <summary>Resolve the three supported portable prefab kinds without using global registry state.</summary>
        private static UIComponentKind DefaultKind(string kind)
        {
            if (string.Equals(kind, "text", StringComparison.OrdinalIgnoreCase)) return UIComponentKind.TextLegacy;
            if (string.Equals(kind, "button", StringComparison.OrdinalIgnoreCase)) return UIComponentKind.Button;
            if (string.Equals(kind, "image", StringComparison.OrdinalIgnoreCase)) return UIComponentKind.Image;
            throw new InvalidOperationException("Unsupported integration node kind: " + kind);
        }

        /// <summary>Recognize a native container independently of JSON casing.</summary>
        private static bool IsContainer(UIAuthoringNode node)
        {
            return string.Equals(node.kind, "container", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Create an error result without exposing a partial object map.</summary>
        private static UIAuthoringBridgeResult Failure(string error)
        {
            return new UIAuthoringBridgeResult { Error = error ?? "Integration operation failed." };
        }
    }
}
