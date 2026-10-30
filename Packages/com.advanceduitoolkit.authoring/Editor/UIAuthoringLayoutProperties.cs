using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedUIAuthoring.Editor
{
    /// <summary>
    /// Validated Editor-only mapping from declarative layout properties to native uGUI
    /// RectTransform, LayoutElement, and LayoutGroup components. No runtime renderer is required.
    /// </summary>
    internal static class UIAuthoringLayoutProperties
    {
        /// <summary>Names of the nine native child-alignment positions.</summary>
        private static readonly Dictionary<string, TextAnchor> Alignments =
            new Dictionary<string, TextAnchor>(StringComparer.Ordinal)
            {
                { "upper-left", TextAnchor.UpperLeft },
                { "upper-center", TextAnchor.UpperCenter },
                { "upper-right", TextAnchor.UpperRight },
                { "middle-left", TextAnchor.MiddleLeft },
                { "middle-center", TextAnchor.MiddleCenter },
                { "middle-right", TextAnchor.MiddleRight },
                { "lower-left", TextAnchor.LowerLeft },
                { "lower-center", TextAnchor.LowerCenter },
                { "lower-right", TextAnchor.LowerRight }
            };

        /// <summary>Permitted RectTransform anchor presets.</summary>
        private static readonly HashSet<string> Anchors = new HashSet<string>(StringComparer.Ordinal)
        {
            "top-left", "top", "top-right", "left", "center", "right",
            "bottom-left", "bottom", "bottom-right", "stretch",
            "stretch-horizontal", "stretch-vertical"
        };

        /// <summary>Parse one additional built-in property with an explicit, invariant value.</summary>
        internal static bool TryApply(UIAuthoringStyle style, string name, string value, out string error)
        {
            error = string.Empty;
            string normalized = value ?? string.Empty;
            float number;
            int integer;
            switch (name)
            {
                case "anchor":
                    if (!Anchors.Contains(normalized)) { error = "unknown anchor preset."; return false; }
                    style.anchor = normalized;
                    return true;
                case "child-alignment":
                    if (!Alignments.ContainsKey(normalized)) { error = "expected upper/middle/lower-left/center/right."; return false; }
                    style.childAlignment = normalized;
                    return true;
                case "child-control-width":
                case "child-control-height":
                case "child-force-expand-width":
                case "child-force-expand-height":
                    if (normalized != "true" && normalized != "false")
                    {
                        error = "must be true or false (lowercase).";
                        return false;
                    }
                    SetString(style, name, normalized);
                    return true;
                case "grid-columns":
                    if (!int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer) ||
                        integer < 1 || integer > 1024)
                    {
                        error = "must be an integer between 1 and 1024.";
                        return false;
                    }
                    style.gridColumns = normalized;
                    return true;
                case "padding-left":
                case "padding-right":
                case "padding-top":
                case "padding-bottom":
                    if (!int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer) ||
                        integer < 0 || integer > 100000)
                    {
                        error = "must be a nonnegative integer no greater than 100000.";
                        return false;
                    }
                    SetString(style, name, normalized);
                    return true;
                case "pivot-x":
                case "pivot-y":
                    if (!TryFiniteFloat(normalized, out number) || number < 0f || number > 1f)
                    {
                        error = "must be a number between 0 and 1.";
                        return false;
                    }
                    SetString(style, name, normalized);
                    return true;
                case "position-x":
                case "position-y":
                    if (!TryFiniteFloat(normalized, out number))
                    {
                        error = "must be a finite number.";
                        return false;
                    }
                    SetString(style, name, normalized);
                    return true;
                case "min-width":
                case "min-height":
                case "flexible-width":
                case "flexible-height":
                    if (!TryFiniteFloat(normalized, out number) || number < 0f)
                    {
                        error = "must be a nonnegative finite number.";
                        return false;
                    }
                    SetString(style, name, normalized);
                    return true;
                case "cell-width":
                case "cell-height":
                    if (!TryFiniteFloat(normalized, out number) || number <= 0f)
                    {
                        error = "must be a positive finite number.";
                        return false;
                    }
                    SetString(style, name, normalized);
                    return true;
                default:
                    error = "unsupported layout property.";
                    return false;
            }
        }

        /// <summary>Dispatch a parsed property without reflection or implicit variable types.</summary>
        private static void SetString(UIAuthoringStyle style, string name, string value)
        {
            switch (name)
            {
                case "child-control-width": style.childControlWidth = value; break;
                case "child-control-height": style.childControlHeight = value; break;
                case "child-force-expand-width": style.childForceExpandWidth = value; break;
                case "child-force-expand-height": style.childForceExpandHeight = value; break;
                case "pivot-x": style.pivotX = value; break;
                case "pivot-y": style.pivotY = value; break;
                case "position-x": style.positionX = value; break;
                case "position-y": style.positionY = value; break;
                case "min-width": style.minWidth = value; break;
                case "min-height": style.minHeight = value; break;
                case "flexible-width": style.flexibleWidth = value; break;
                case "flexible-height": style.flexibleHeight = value; break;
                case "padding-left": style.paddingLeft = value; break;
                case "padding-right": style.paddingRight = value; break;
                case "padding-top": style.paddingTop = value; break;
                case "padding-bottom": style.paddingBottom = value; break;
                case "cell-width": style.cellWidth = value; break;
                case "cell-height": style.cellHeight = value; break;
            }
        }

        /// <summary>Check cross-property constraints on a fully resolved style.</summary>
        internal static void Validate(UIAuthoringStyle style, string label, List<string> errors)
        {
            if (style == null) return;
            string layout = style.layout ?? string.Empty;
            if (layout.Length == 0 && !string.IsNullOrEmpty(style.childAlignment))
            {
                errors.Add(label + "child-alignment requires a horizontal, vertical, or grid layout.");
            }
            if (layout != "horizontal" && layout != "vertical" &&
                (!string.IsNullOrEmpty(style.childControlWidth) ||
                 !string.IsNullOrEmpty(style.childControlHeight) ||
                 !string.IsNullOrEmpty(style.childForceExpandWidth) ||
                 !string.IsNullOrEmpty(style.childForceExpandHeight)))
            {
                errors.Add(label + "child-control and child-force-expand require horizontal or vertical layout.");
            }
            if (layout != "grid" && (!string.IsNullOrEmpty(style.gridColumns) ||
                !string.IsNullOrEmpty(style.cellWidth) || !string.IsNullOrEmpty(style.cellHeight)))
            {
                errors.Add(label + "grid-columns and cell-width/height require layout: grid.");
            }
            if ((style.anchor == "stretch" || style.anchor == "stretch-horizontal") && style.width > 0f)
            {
                errors.Add(label + "width conflicts with a horizontal stretch anchor. Use stretch with no fixed width.");
            }
            if ((style.anchor == "stretch" || style.anchor == "stretch-vertical") && style.height > 0f)
            {
                errors.Add(label + "height conflicts with a vertical stretch anchor. Use stretch with no fixed height.");
            }
            if (!string.IsNullOrEmpty(style.minWidth) && style.width > 0f &&
                ReadFloat(style.minWidth) > style.width)
            {
                errors.Add(label + "min-width cannot exceed an explicit width.");
            }
            if (!string.IsNullOrEmpty(style.minHeight) && style.height > 0f &&
                ReadFloat(style.minHeight) > style.height)
            {
                errors.Add(label + "min-height cannot exceed an explicit height.");
            }
        }

        /// <summary>Detect manual transforms that will be overwritten by a parent layout group.</summary>
        internal static bool HasManualPositioning(UIAuthoringStyle style)
        {
            return style != null && (!string.IsNullOrEmpty(style.anchor) ||
                !string.IsNullOrEmpty(style.pivotX) || !string.IsNullOrEmpty(style.pivotY) ||
                !string.IsNullOrEmpty(style.positionX) || !string.IsNullOrEmpty(style.positionY));
        }

        /// <summary>Whether any individual padding side was specified.</summary>
        internal static bool HasSidePadding(UIAuthoringStyle style)
        {
            return style != null && (!string.IsNullOrEmpty(style.paddingLeft) ||
                !string.IsNullOrEmpty(style.paddingRight) || !string.IsNullOrEmpty(style.paddingTop) ||
                !string.IsNullOrEmpty(style.paddingBottom));
        }

        /// <summary>Parse an already validated invariant float.</summary>
        internal static float ReadFloat(string value)
        {
            return float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        /// <summary>Parse an already validated invariant integer.</summary>
        internal static int ReadInt(string value)
        {
            return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        /// <summary>Prevent NaN or Infinity and disallow locale-dependent decimal parsing.</summary>
        private static bool TryFiniteFloat(string value, out float number)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) &&
                !float.IsNaN(number) && !float.IsInfinity(number);
        }

        /// <summary>Resolve a child-alignment keyword, defaulting to the prior center alignment.</summary>
        internal static TextAnchor ResolveAlignment(string value)
        {
            TextAnchor anchor;
            return !string.IsNullOrEmpty(value) && Alignments.TryGetValue(value, out anchor)
                ? anchor : TextAnchor.MiddleCenter;
        }

        /// <summary>Build a fresh native padding instance, allowing individual side overrides.</summary>
        internal static RectOffset MakePadding(UIAuthoringStyle style)
        {
            int basePadding = style.padding;
            int left = string.IsNullOrEmpty(style.paddingLeft) ? basePadding : ReadInt(style.paddingLeft);
            int right = string.IsNullOrEmpty(style.paddingRight) ? basePadding : ReadInt(style.paddingRight);
            int top = string.IsNullOrEmpty(style.paddingTop) ? basePadding : ReadInt(style.paddingTop);
            int bottom = string.IsNullOrEmpty(style.paddingBottom) ? basePadding : ReadInt(style.paddingBottom);
            return new RectOffset(left, right, top, bottom);
        }

        /// <summary>Apply named anchors, pivot and local offsets to a generated RectTransform.</summary>
        internal static void ApplyRectTransform(RectTransform rectangle, UIAuthoringStyle style)
        {
            if (!string.IsNullOrEmpty(style.anchor))
            {
                Vector2 min;
                Vector2 max;
                switch (style.anchor)
                {
                    case "top-left": min = max = new Vector2(0f, 1f); break;
                    case "top": min = max = new Vector2(.5f, 1f); break;
                    case "top-right": min = max = new Vector2(1f, 1f); break;
                    case "left": min = max = new Vector2(0f, .5f); break;
                    case "right": min = max = new Vector2(1f, .5f); break;
                    case "bottom-left": min = max = new Vector2(0f, 0f); break;
                    case "bottom": min = max = new Vector2(.5f, 0f); break;
                    case "bottom-right": min = max = new Vector2(1f, 0f); break;
                    case "stretch": min = Vector2.zero; max = Vector2.one; break;
                    case "stretch-horizontal": min = new Vector2(0f, .5f); max = new Vector2(1f, .5f); break;
                    case "stretch-vertical": min = new Vector2(.5f, 0f); max = new Vector2(.5f, 1f); break;
                    default: min = max = new Vector2(.5f, .5f); break;
                }
                rectangle.anchorMin = min;
                rectangle.anchorMax = max;
                Vector2 size = rectangle.sizeDelta;
                if (max.x > min.x && style.width <= 0f) size.x = 0f;
                if (max.y > min.y && style.height <= 0f) size.y = 0f;
                rectangle.sizeDelta = size;
            }
            Vector2 pivot = rectangle.pivot;
            if (!string.IsNullOrEmpty(style.pivotX)) pivot.x = ReadFloat(style.pivotX);
            if (!string.IsNullOrEmpty(style.pivotY)) pivot.y = ReadFloat(style.pivotY);
            rectangle.pivot = pivot;
            Vector2 position = rectangle.anchoredPosition;
            if (!string.IsNullOrEmpty(style.positionX)) position.x = ReadFloat(style.positionX);
            if (!string.IsNullOrEmpty(style.positionY)) position.y = ReadFloat(style.positionY);
            rectangle.anchoredPosition = position;
        }

        /// <summary>Apply min/flexible constraints without overwriting existing preferred sizes.</summary>
        internal static void ApplyLayoutElement(GameObject element, UIAuthoringStyle style)
        {
            if (string.IsNullOrEmpty(style.minWidth) && string.IsNullOrEmpty(style.minHeight) &&
                string.IsNullOrEmpty(style.flexibleWidth) && string.IsNullOrEmpty(style.flexibleHeight)) return;
            LayoutElement layout = element.GetComponent<LayoutElement>();
            if (layout == null) layout = element.AddComponent<LayoutElement>();
            if (!string.IsNullOrEmpty(style.minWidth)) layout.minWidth = ReadFloat(style.minWidth);
            if (!string.IsNullOrEmpty(style.minHeight)) layout.minHeight = ReadFloat(style.minHeight);
            if (!string.IsNullOrEmpty(style.flexibleWidth)) layout.flexibleWidth = ReadFloat(style.flexibleWidth);
            if (!string.IsNullOrEmpty(style.flexibleHeight)) layout.flexibleHeight = ReadFloat(style.flexibleHeight);
        }
    }
}
