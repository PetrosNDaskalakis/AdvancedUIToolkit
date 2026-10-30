using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AdvancedUIAuthoring.Editor
{
    /// <summary>Outcome of one hierarchy generation operation.</summary>
    public sealed class UIAuthoringGenerationResult
    {
        /// <summary>Whether all elements were successfully created.</summary>
        public bool Succeeded { get; internal set; }

        /// <summary>Human-readable error when creation fails.</summary>
        public string Error { get; internal set; }

        /// <summary>Generated elements by stable schema ID; empty on failure.</summary>
        public IReadOnlyDictionary<string, GameObject> CreatedElements { get; internal set; }

        /// <summary>Initialize an empty result.</summary>
        internal UIAuthoringGenerationResult()
        {
            Succeeded = false;
            Error = string.Empty;
            CreatedElements = new Dictionary<string, GameObject>();
        }
    }

    /// <summary>
    /// Editor-only native UI generator. Does not reference Advanced UI Toolkit and does not
    /// create components that must exist at runtime beyond Unity's built-in uGUI types.
    /// </summary>
    public static class UIAuthoringGenerator
    {
        /// <summary>
        /// Validate and create an ordered document as one Undo operation. The scene selection
        /// remains unchanged. A failed operation attempts to revert all generated objects.
        /// </summary>
        public static UIAuthoringGenerationResult Generate(UIAuthoringDocument document, RectTransform parent)
        {
            return Generate(document, parent, null);
        }

        /// <summary>Generate with a prevalidated reusable stylesheet and resolved style adapters.</summary>
        public static UIAuthoringGenerationResult Generate(UIAuthoringDocument document,
            RectTransform parent, UIAuthoringStyleSheet sheet)
        {
            UIAuthoringGenerationResult result = new UIAuthoringGenerationResult();
            UIAuthoringValidationResult validation = UIAuthoringValidator.Validate(document, parent, sheet);
            if (!validation.Succeeded)
            {
                result.Error = string.Join("\n", validation.Errors);
                return result;
            }

            Dictionary<string, GameObject> created = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            List<GameObject> creationOrder = new List<GameObject>();
            UnityEngine.Object[] originalSelection = Selection.objects;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            string operation = "Author UI: " + (string.IsNullOrEmpty(document.name) ? "Generate" : document.name);
            Undo.SetCurrentGroupName(operation);
            try
            {
                foreach (UIAuthoringNode node in document.nodes)
                {
                    RectTransform targetParent = parent;
                    if (!string.IsNullOrEmpty(node.parentId))
                    {
                        GameObject parentObject = created[node.parentId];
                        targetParent = parentObject.GetComponent<RectTransform>();
                    }

                    IUIAuthoringElementFactory factory;
                    if (!UIAuthoringRegistry.TryGetElement(node.kind, out factory))
                    {
                        throw new InvalidOperationException("Unregistered kind during creation: " + node.kind);
                    }
                    GameObject element = factory.Create(node);
                    if (element == null)
                    {
                        throw new InvalidOperationException("Factory returned null for '" + node.id + "'.");
                    }
                    if (EditorUtility.IsPersistent(element) || element.GetComponent<RectTransform>() == null ||
                        element.transform.parent != null)
                    {
                        if (!EditorUtility.IsPersistent(element) && element.transform.parent == null)
                        {
                            UnityEngine.Object.DestroyImmediate(element);
                        }
                        throw new InvalidOperationException("Factory must return an unparented scene RectTransform for '" + node.id + "'.");
                    }

                    element.name = node.name;
                    element.transform.SetParent(targetParent, false);
                    Undo.RegisterCreatedObjectUndo(element, operation);
                    creationOrder.Add(element);
                    created.Add(node.id, element);
                    UIAuthoringResolvedStyle resolved;
                    List<string> styleErrors = new List<string>();
                    if (!UIAuthoringStyleResolver.TryResolve(node, sheet, "Node '" + node.id + "': ",
                        styleErrors, out resolved))
                    {
                        throw new InvalidOperationException(string.Join("; ", styleErrors));
                    }
                    UIBuiltInStyleApplier.Apply(element, node, resolved.BuiltIn);
                    foreach (UIAuthoringResolvedDeclaration declaration in resolved.Custom)
                    {
                        IUIAuthoringStylePropertyAdapter adapter;
                        if (!UIAuthoringRegistry.TryGetStyleProperty(declaration.Name, out adapter))
                        {
                            throw new InvalidOperationException("Style adapter disappeared: " + declaration.Name);
                        }
                        adapter.Apply(element, node, declaration.Value);
                    }
                    foreach (string className in UIAuthoringStyleResolver.EnumerateClasses(node))
                    {
                        IUIAuthoringStyleClass handler;
                        if (UIAuthoringRegistry.TryGetStyleClass(className, out handler))
                        {
                            handler.Apply(element, node);
                        }
                    }
                }
                Undo.CollapseUndoOperations(group);
                result.Succeeded = true;
                result.CreatedElements = new Dictionary<string, GameObject>(created);
                return result;
            }
            catch (Exception exception)
            {
                result.Error = "Generation failed. Undo rollback attempted: " + exception.Message;
                try
                {
                    Undo.RevertAllDownToGroup(group);
                }
                catch (Exception undoException)
                {
                    result.Error += " | Undo rollback error: " + undoException.Message;
                }
                for (int index = creationOrder.Count - 1; index >= 0; index--)
                {
                    GameObject objectToClean = creationOrder[index];
                    if (objectToClean != null) UnityEngine.Object.DestroyImmediate(objectToClean);
                }
                return result;
            }
            finally
            {
                Selection.objects = originalSelection;
            }
        }
    }
}
