using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AdvancedUIAuthoring.Editor
{
    /// <summary>Complete, non-mutating preflight result for a schema and target parent.</summary>
    public sealed class UIAuthoringValidationResult
    {
        /// <summary>Whether generation may proceed.</summary>
        public bool Succeeded { get { return Errors.Count == 0; } }

        /// <summary>All discovered issues, in schema order.</summary>
        public IReadOnlyList<string> Errors { get; private set; }

        /// <summary>Number of planned elements.</summary>
        public int NodeCount { get; private set; }

        /// <summary>Create a result from an immutable snapshot of validation errors.</summary>
        internal UIAuthoringValidationResult(List<string> errors, int count)
        {
            Errors = errors.AsReadOnly();
            NodeCount = count;
        }
    }

    /// <summary>Validates all supported schema features before any scene mutation.</summary>
    public static class UIAuthoringValidator
    {
        /// <summary>Allowed schema IDs are ASCII stable identifiers, independent of object names.</summary>
        private static readonly Regex IdPattern = new Regex("^[A-Za-z][A-Za-z0-9_-]*$", RegexOptions.CultureInvariant);

        /// <summary>Permitted explicit hex color formats.</summary>
        private static readonly Regex ColorPattern = new Regex("^#[0-9a-fA-F]{6}([0-9a-fA-F]{2})?$", RegexOptions.CultureInvariant);

        /// <summary>Checks the schema, registry, ordering and scene parent.</summary>
        public static UIAuthoringValidationResult Validate(UIAuthoringDocument document, RectTransform parent)
        {
            return Validate(document, parent, null);
        }

        /// <summary>Validates a document and an optional reusable stylesheet before any mutation.</summary>
        public static UIAuthoringValidationResult Validate(UIAuthoringDocument document,
            RectTransform parent, UIAuthoringStyleSheet sheet)
        {
            List<string> errors = new List<string>();
            UIAuthoringStyleResolver.ValidateSheet(sheet, errors);
            if (parent == null)
            {
                errors.Add("Select a scene Canvas or another RectTransform container.");
            }
            else if (EditorUtility.IsPersistent(parent) || PrefabUtility.IsPartOfPrefabInstance(parent.gameObject))
            {
                errors.Add("The target must be a regular scene RectTransform, not a prefab asset or instance.");
            }

            if (document == null)
            {
                errors.Add("No schema document was provided.");
                return new UIAuthoringValidationResult(errors, 0);
            }
            if (document.version != 1)
            {
                errors.Add("Unsupported schema version: " + document.version + ". Expected 1.");
            }
            if (document.nodes == null || document.nodes.Count == 0)
            {
                errors.Add("The schema must contain at least one node.");
                return new UIAuthoringValidationResult(errors, 0);
            }
            if (document.nodes.Count > 2048)
            {
                errors.Add("The initial generator is limited to 2048 nodes per operation.");
            }

            Dictionary<string, UIAuthoringNode> seen =
                new Dictionary<string, UIAuthoringNode>(StringComparer.Ordinal);
            Dictionary<string, HashSet<string>> namesByParent =
                new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            Dictionary<string, UIAuthoringResolvedStyle> resolvedById =
                new Dictionary<string, UIAuthoringResolvedStyle>(StringComparer.Ordinal);
            for (int index = 0; index < document.nodes.Count; index++)
            {
                UIAuthoringNode node = document.nodes[index];
                string label = "Node " + (index + 1) + ": ";
                if (node == null)
                {
                    errors.Add(label + "null nodes are not allowed.");
                    continue;
                }
                string id = node.id ?? string.Empty;
                if (!IdPattern.IsMatch(id))
                {
                    errors.Add(label + "id must start with a letter and contain only letters, digits, _ or -." );
                }
                if (seen.ContainsKey(id))
                {
                    errors.Add(label + "duplicate id '" + id + "'.");
                }
                else
                {
                    seen.Add(id, node);
                }

                string parentId = node.parentId ?? string.Empty;
                if (parentId.Length > 0)
                {
                    UIAuthoringNode parentNode;
                    if (!seen.TryGetValue(parentId, out parentNode) || ReferenceEquals(node, parentNode))
                    {
                        errors.Add(label + "parentId '" + parentId + "' must reference an earlier node.");
                    }
                    else
                    {
                        IUIAuthoringElementFactory parentFactory;
                        if (!UIAuthoringRegistry.TryGetElement(parentNode.kind, out parentFactory) ||
                            !parentFactory.CanContainChildren)
                        {
                            errors.Add(label + "parent '" + parentId + "' cannot contain children.");
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(node.name))
                {
                    errors.Add(label + "name is required.");
                }
                else
                {
                    HashSet<string> siblingNames;
                    if (!namesByParent.TryGetValue(parentId, out siblingNames))
                    {
                        siblingNames = new HashSet<string>(StringComparer.Ordinal);
                        namesByParent.Add(parentId, siblingNames);
                    }
                    if (!siblingNames.Add(node.name))
                    {
                        errors.Add(label + "duplicate sibling name '" + node.name + "'.");
                    }
                }

                IUIAuthoringElementFactory factory;
                if (!UIAuthoringRegistry.TryGetElement(node.kind, out factory))
                {
                    errors.Add(label + "unregistered element kind '" + node.kind + "'.");
                }
                else
                {
                    IUIAuthoringElementValidator customValidator = factory as IUIAuthoringElementValidator;
                    if (customValidator != null)
                    {
                        string error;
                        try
                        {
                            if (!customValidator.TryValidate(node, out error))
                            {
                                errors.Add(label + (string.IsNullOrEmpty(error) ?
                                    "custom element rejected the node." : error));
                            }
                        }
                        catch (Exception exception)
                        {
                            errors.Add(label + "custom element validation failed: " + exception.Message);
                        }
                    }
                }
                UIAuthoringResolvedStyle resolved;
                UIAuthoringStyleResolver.TryResolve(node, sheet, label, errors, out resolved);
                if (resolved != null)
                {
                    UIAuthoringStyle validationStyle = resolved.BuiltIn;
                    ValidateStyle(node, validationStyle, label, errors);
                    if (!string.IsNullOrEmpty(id) && !resolvedById.ContainsKey(id))
                    {
                        resolvedById.Add(id, resolved);
                    }
                    UIAuthoringResolvedStyle parentStyle;
                    if (!string.IsNullOrEmpty(parentId) &&
                        resolvedById.TryGetValue(parentId, out parentStyle) &&
                        !string.IsNullOrEmpty(parentStyle.BuiltIn.layout) &&
                        UIAuthoringLayoutProperties.HasManualPositioning(validationStyle))
                    {
                        errors.Add(label + "anchor, pivot and position cannot be specified for children of a layout group; the parent layout controls their placement.");
                    }
                }
            }

            return new UIAuthoringValidationResult(errors, document.nodes.Count);
        }

        /// <summary>Validates built-in style values and compatibility.</summary>
        private static void ValidateStyle(UIAuthoringNode node, UIAuthoringStyle style, string label, List<string> errors)
        {
            if (style == null) return;
            if (!IsValidSize(style.width) || !IsValidSize(style.height))
            {
                errors.Add(label + "width and height must be nonnegative and finite (zero means unspecified).");
            }
            if (!IsValidColor(style.background) || !IsValidColor(style.textColor))
            {
                errors.Add(label + "colors must use #RRGGBB or #RRGGBBAA.");
            }
            if (style.fontSize < 0)
            {
                errors.Add(label + "fontSize must be nonnegative (zero means unspecified).");
            }
            if (!string.IsNullOrEmpty(style.textAlignment) && style.textAlignment != "left" &&
                style.textAlignment != "center" && style.textAlignment != "right")
            {
                errors.Add(label + "textAlignment must be left, center, or right.");
            }
            if (style.spacing < 0f || float.IsNaN(style.spacing) || float.IsInfinity(style.spacing))
            {
                errors.Add(label + "spacing must be nonnegative and finite.");
            }
            if (style.padding < 0)
            {
                errors.Add(label + "padding must be nonnegative.");
            }
            string layout = style.layout ?? string.Empty;
            if (layout.Length > 0 && layout != "horizontal" && layout != "vertical" && layout != "grid")
            {
                errors.Add(label + "layout must be horizontal, vertical, grid, or empty.");
            }
            if (layout.Length > 0)
            {
                IUIAuthoringElementFactory factory;
                if (!UIAuthoringRegistry.TryGetElement(node.kind, out factory) || !factory.CanContainChildren)
                {
                    errors.Add(label + "layout requires a container-capable element kind.");
                }
            }
            if ((style.spacing > 0f || style.padding > 0 ||
                 UIAuthoringLayoutProperties.HasSidePadding(style)) && layout.Length == 0)
            {
                errors.Add(label + "spacing and padding require a layout.");
            }
            if ((style.fontSize > 0 || !string.IsNullOrEmpty(style.textColor) ||
                 !string.IsNullOrEmpty(style.textAlignment)) &&
                (string.Equals(node.kind, "container", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(node.kind, "image", StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add(label + "fontSize and textColor require a text/button or custom element kind.");
            }
            if (!string.IsNullOrEmpty(style.background) &&
                string.Equals(node.kind, "text", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(label + "root text nodes do not support backgrounds; wrap the text in a container.");
            }
            UIAuthoringLayoutProperties.Validate(style, label, errors);
        }

        /// <summary>Checks the size sentinel and numeric range.</summary>
        private static bool IsValidSize(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
        }

        /// <summary>Checks only supported hex formats, not arbitrary Unity named colors.</summary>
        private static bool IsValidColor(string value)
        {
            return string.IsNullOrEmpty(value) || ColorPattern.IsMatch(value);
        }
    }
}
