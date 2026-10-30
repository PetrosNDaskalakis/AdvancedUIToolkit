using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedUIAuthoring.Editor
{
    /// <summary>
    /// Immutable, prevalidated root-only layout overrides for a scene UI object.
    /// Resolves Authoring styles without touching prefab visuals, labels or child components.
    /// </summary>
    public sealed class UIAuthoringRootLayoutPlan
    {
        /// <summary>Supported properties; layout-group and appearance declarations are deliberately excluded.</summary>
        private static readonly HashSet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "width", "height", "anchor", "pivot-x", "pivot-y", "position-x", "position-y",
            "min-width", "min-height", "flexible-width", "flexible-height"
        };

        /// <summary>Private resolved style snapshot, never returned to callers.</summary>
        private readonly UIAuthoringStyle style;

        /// <summary>Immutable explicit-property snapshot.</summary>
        private readonly IReadOnlyList<string> propertyNames;

        /// <summary>Names actually declared after cascading, including explicit zero-valued declarations.</summary>
        public IReadOnlyList<string> PropertyNames { get { return propertyNames; } }

        /// <summary>Whether applying this plan can change any root property.</summary>
        public bool HasChanges { get { return propertyNames.Count != 0; } }

        /// <summary>Whether the root needs a LayoutElement for preferred, minimum or flexible sizes.</summary>
        private bool NeedsLayoutElement
        {
            get
            {
                return style.width > 0f || style.height > 0f || !string.IsNullOrEmpty(style.minWidth) ||
                    !string.IsNullOrEmpty(style.minHeight) || !string.IsNullOrEmpty(style.flexibleWidth) ||
                    !string.IsNullOrEmpty(style.flexibleHeight);
            }
        }

        /// <summary>Whether the plan edits RectTransform values in addition to layout hints.</summary>
        private bool ChangesRect
        {
            get { return style.width > 0f || style.height > 0f || UIAuthoringLayoutProperties.HasManualPositioning(style); }
        }

        /// <summary>Capture the freshly resolved style and its explicit declarations.</summary>
        private UIAuthoringRootLayoutPlan(UIAuthoringResolvedStyle resolved)
        {
            style = resolved.BuiltIn;
            propertyNames = new List<string>(resolved.PropertyNames).AsReadOnly();
        }

        /// <summary>
        /// Resolve classes, inheritance, tokens and inline values into a root-layout-only plan.
        /// The caller must also validate the full schema for parent and node-kind constraints.
        /// </summary>
        public static bool TryCreate(UIAuthoringNode node, UIAuthoringStyleSheet sheet,
            out UIAuthoringRootLayoutPlan plan, out string error)
        {
            plan = null;
            error = string.Empty;
            if (node == null) { error = "A schema node is required."; return false; }
            var errors = new List<string>();
            UIAuthoringStyleResolver.ValidateSheet(sheet, errors);
            foreach (string name in UIAuthoringStyleResolver.EnumerateClasses(node))
            {
                IUIAuthoringStyleClass handler;
                if (UIAuthoringRegistry.TryGetStyleClass(name, out handler))
                    errors.Add("Root layout overrides do not execute registered style-class callbacks: " + name);
            }
            if (errors.Count != 0) { error = string.Join("\n", errors); return false; }
            UIAuthoringResolvedStyle resolved;
            if (!UIAuthoringStyleResolver.TryResolve(node, sheet, "Root layout: ", errors, out resolved))
            {
                error = string.Join("\n", errors);
                return false;
            }
            foreach (string property in resolved.PropertyNames)
                if (!Allowed.Contains(property)) errors.Add("Prefab root overrides do not support '" + property +
                    "'. Keep appearance in the prefab and child layout in native containers.");
            // Legacy numeric zero means unspecified. An explicit declaration of width/height zero
            // is rejected here rather than silently preserving a nonzero prefab size.
            if (resolved.PropertyNames.Contains("width") && resolved.BuiltIn.width <= 0f)
                errors.Add("A prefab width override must be positive; omit width to preserve the prefab.");
            if (resolved.PropertyNames.Contains("height") && resolved.BuiltIn.height <= 0f)
                errors.Add("A prefab height override must be positive; omit height to preserve the prefab.");
            UIAuthoringLayoutProperties.Validate(resolved.BuiltIn, "Root layout: ", errors);
            if (errors.Count != 0) { error = string.Join("\n", errors); return false; }
            plan = new UIAuthoringRootLayoutPlan(resolved);
            return true;
        }

        /// <summary>
        /// Check a source prefab or scene target without mutations. Supply the intended parent
        /// when it exists; full-schema validation covers layout parents that are not created yet.
        /// </summary>
        public bool ValidateTarget(GameObject target, Transform intendedParent, out string error)
        {
            LayoutGroup parentLayout = intendedParent != null ? intendedParent.GetComponent<LayoutGroup>() : null;
            return ValidateTarget(target, parentLayout, parentLayout != null && parentLayout.enabled, out error);
        }

        /// <summary>Preflight a source prefab against either a future schema container or an existing parent.</summary>
        public bool ValidateSource(GameObject source, UIAuthoringNode schemaParent, UIAuthoringStyleSheet sheet,
            Transform existingParent, out string error)
        {
            if (schemaParent == null) return ValidateTarget(source, existingParent, out error);
            var errors = new List<string>();
            UIAuthoringResolvedStyle parentStyle;
            if (!UIAuthoringStyleResolver.TryResolve(schemaParent, sheet, "Parent layout: ", errors, out parentStyle))
            {
                error = string.Join("\n", errors);
                return false;
            }
            return ValidateTarget(source, null, !string.IsNullOrEmpty(parentStyle.BuiltIn.layout), out error);
        }

        /// <summary>Validate native root constraints using actual or preflight parent-layout ownership.</summary>
        private bool ValidateTarget(GameObject target, LayoutGroup parentLayout, bool parentControlsLayout, out string error)
        {
            error = string.Empty;
            if (target == null || target.GetComponent<RectTransform>() == null)
            {
                error = "Root layout requires a RectTransform on the target itself.";
                return false;
            }
            if (!HasChanges) return true;
            RectTransform rect = target.GetComponent<RectTransform>();
            if (parentControlsLayout && UIAuthoringLayoutProperties.HasManualPositioning(style))
            {
                error = "Anchor, pivot and position overrides conflict with the parent LayoutGroup.";
                return false;
            }
            // Explicit schema stretch conflicts were already checked. Preserve an implicit source
            // stretch unless a layout parent owns sizing or an explicit anchor replaces it.
            if (!parentControlsLayout && string.IsNullOrEmpty(style.anchor) &&
                ((style.width > 0f && rect.anchorMin.x != rect.anchorMax.x) ||
                 (style.height > 0f && rect.anchorMin.y != rect.anchorMax.y)))
            {
                error = "A fixed size conflicts with the prefab's stretched anchors. Choose a non-stretch anchor or omit that size.";
                return false;
            }
            ContentSizeFitter fitter = target.GetComponent<ContentSizeFitter>();
            if (fitter != null && fitter.enabled &&
                ((fitter.horizontalFit != ContentSizeFitter.FitMode.Unconstrained &&
                    (style.width > 0f || !string.IsNullOrEmpty(style.anchor))) ||
                 (fitter.verticalFit != ContentSizeFitter.FitMode.Unconstrained &&
                    (style.height > 0f || !string.IsNullOrEmpty(style.anchor)))))
            {
                error = "A root ContentSizeFitter already controls the requested dimension.";
                return false;
            }
            AspectRatioFitter aspect = target.GetComponent<AspectRatioFitter>();
            if (ChangesRect && aspect != null && aspect.enabled && aspect.aspectMode != AspectRatioFitter.AspectMode.None)
            {
                error = "A root AspectRatioFitter already drives this RectTransform.";
                return false;
            }
            if (ChangesRect && rect.drivenByObject != null && rect.drivenByObject != parentLayout)
            {
                error = "Another component currently drives this RectTransform; remove the conflicting override.";
                return false;
            }
            if (NeedsLayoutElement)
            {
                LayoutElement[] elements = target.GetComponents<LayoutElement>();
                if (elements.Length > 1)
                {
                    error = "Multiple root LayoutElements make size overrides ambiguous.";
                    return false;
                }
                if (elements.Length == 1 &&
                    ((style.width > 0f && (string.IsNullOrEmpty(style.minWidth) ? elements[0].minWidth :
                        UIAuthoringLayoutProperties.ReadFloat(style.minWidth)) > style.width) ||
                     (style.height > 0f && (string.IsNullOrEmpty(style.minHeight) ? elements[0].minHeight :
                        UIAuthoringLayoutProperties.ReadFloat(style.minHeight)) > style.height)))
                {
                    error = "A root minimum size exceeds the requested fixed size. Override that minimum explicitly or increase the size.";
                    return false;
                }
                if (elements.Length == 1 && (!elements[0].enabled || elements[0].ignoreLayout))
                {
                    error = "The root LayoutElement is disabled or ignores layout.";
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Apply to a caller-owned scene target under the caller's current Undo group. Throws on
        /// invalid targets or Unity failures; the caller owns transaction rollback. Assets are never edited.
        /// </summary>
        public void Apply(GameObject target)
        {
            if (target == null || EditorUtility.IsPersistent(target) || !target.scene.IsValid() || !target.scene.isLoaded)
                throw new InvalidOperationException("Apply root layout only to an editable scene object, never a prefab asset.");
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Root layout is unavailable during Play Mode, compilation or import.");
            string error;
            if (!ValidateTarget(target, target.transform.parent, out error)) throw new InvalidOperationException(error);
            if (!HasChanges) return;
            const string undoName = "Apply UI Root Layout";
            RectTransform rect = target.GetComponent<RectTransform>();
            if (ChangesRect)
            {
                Undo.RegisterCompleteObjectUndo(rect, undoName);
                UIAuthoringLayoutProperties.ApplyRectTransform(rect, style);
                if (style.width > 0f) rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, style.width);
                if (style.height > 0f) rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, style.height);
                RecordPrefabOverride(rect);
            }
            if (NeedsLayoutElement)
            {
                LayoutElement element = target.GetComponent<LayoutElement>();
                if (element == null) element = Undo.AddComponent<LayoutElement>(target);
                else Undo.RegisterCompleteObjectUndo(element, undoName);
                if (style.width > 0f) element.preferredWidth = style.width;
                if (style.height > 0f) element.preferredHeight = style.height;
                UIAuthoringLayoutProperties.ApplyLayoutElement(target, style);
                RecordPrefabOverride(element);
            }
        }

        /// <summary>Persist instance-only edits; ordinary scene components need no prefab record.</summary>
        private static void RecordPrefabOverride(UnityEngine.Object target)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(target))
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
