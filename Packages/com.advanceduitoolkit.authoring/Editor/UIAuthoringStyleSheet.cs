using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AdvancedUIAuthoring.Editor
{
    /// <summary>A reusable design token with a string value, optionally referencing another token.</summary>
    [Serializable]
    public sealed class UIAuthoringDesignToken
    {
        /// <summary>Token name, including the -- prefix.</summary>
        public string name = string.Empty;

        /// <summary>Literal value or an entire var(--other-token) expression.</summary>
        public string value = string.Empty;
    }

    /// <summary>One CSS-inspired named property with a literal or token reference value.</summary>
    [Serializable]
    public sealed class UIAuthoringStyleDeclaration
    {
        /// <summary>Built-in or externally registered property name.</summary>
        public string name = string.Empty;

        /// <summary>Property value as an invariant-culture string.</summary>
        public string value = string.Empty;
    }

    /// <summary>Named reusable class, optionally inheriting declarations from another class.</summary>
    [Serializable]
    public sealed class UIAuthoringStyleRule
    {
        /// <summary>Class identifier.</summary>
        public string name = string.Empty;

        /// <summary>Optional name of an existing class in the same stylesheet.</summary>
        public string extends = string.Empty;

        /// <summary>Ordered property declarations for this class.</summary>
        public List<UIAuthoringStyleDeclaration> properties = new List<UIAuthoringStyleDeclaration>();
    }

    /// <summary>Reusable Editor-only stylesheet. Does not require a runtime renderer or USS.</summary>
    [Serializable]
    public sealed class UIAuthoringStyleSheet
    {
        /// <summary>Stylesheet format version.</summary>
        public int version = 1;

        /// <summary>Human-readable description of this sheet.</summary>
        public string name = string.Empty;

        /// <summary>Reusable design tokens.</summary>
        public List<UIAuthoringDesignToken> tokens = new List<UIAuthoringDesignToken>();

        /// <summary>Reusable style classes.</summary>
        public List<UIAuthoringStyleRule> classes = new List<UIAuthoringStyleRule>();
    }

    /// <summary>Parse a JSON stylesheet using Unity's existing serializer.</summary>
    public static class UIAuthoringStyleSheetParser
    {
        /// <summary>Read a stylesheet without creating Unity scene objects.</summary>
        public static bool TryParse(string json, out UIAuthoringStyleSheet sheet, out string error)
        {
            sheet = null;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Stylesheet JSON is empty.";
                return false;
            }
            try
            {
                sheet = JsonUtility.FromJson<UIAuthoringStyleSheet>(json);
                if (sheet == null)
                {
                    error = "Unable to parse stylesheet JSON.";
                    return false;
                }
                if (sheet.tokens == null || sheet.classes == null)
                {
                    sheet = null;
                    error = "Stylesheet tokens and classes must be arrays.";
                    return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                error = "Invalid stylesheet JSON: " + exception.Message;
                sheet = null;
                return false;
            }
        }
    }

    /// <summary>A validated and expanded custom declaration.</summary>
    internal sealed class UIAuthoringResolvedDeclaration
    {
        /// <summary>Adapter property name.</summary>
        internal string Name;

        /// <summary>Resolved literal property value.</summary>
        internal string Value;
    }

    /// <summary>Final built-in style and external adapter declarations for one node.</summary>
    internal sealed class UIAuthoringResolvedStyle
    {
        /// <summary>Resolved native-uGUI properties.</summary>
        internal UIAuthoringStyle BuiltIn = new UIAuthoringStyle();

        /// <summary>Explicit property names after cascading, including values equal to zero or none.</summary>
        internal readonly List<string> PropertyNames = new List<string>();

        /// <summary>External declarations, applied in deterministic order.</summary>
        internal List<UIAuthoringResolvedDeclaration> Custom = new List<UIAuthoringResolvedDeclaration>();
    }

    /// <summary>
    /// Deterministic, Editor-only stylesheet cascade. Keeps the earlier schema/style contracts
    /// valid, and resolves styles without mutating source documents or scene objects.
    /// </summary>
    public static class UIAuthoringStyleResolver
    {
        /// <summary>Supported class name characters.</summary>
        private static readonly Regex ClassPattern = new Regex("^[A-Za-z][A-Za-z0-9_-]*$",
            RegexOptions.CultureInvariant);

        /// <summary>Supported external style property characters.</summary>
        private static readonly Regex PropertyPattern = new Regex("^[a-z][a-z0-9-]*$",
            RegexOptions.CultureInvariant);

        /// <summary>Supported token name characters.</summary>
        private static readonly Regex TokenPattern = new Regex("^--[A-Za-z][A-Za-z0-9_-]*$",
            RegexOptions.CultureInvariant);

        /// <summary>Supported full-value token reference.</summary>
        private static readonly Regex TokenReference = new Regex("^var\\((--[A-Za-z][A-Za-z0-9_-]*)\\)$",
            RegexOptions.CultureInvariant);

        /// <summary>Color values supported by native Image and Text components.</summary>
        private static readonly Regex HexColor = new Regex("^#[A-Fa-f0-9]{6}([A-Fa-f0-9]{2})?$",
            RegexOptions.CultureInvariant);

        /// <summary>Names implemented directly by the native built-in style applier.</summary>
        private static readonly HashSet<string> BuiltInProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "width", "height", "background", "text-color", "font-size", "layout",
            "spacing", "padding", "text-align", "anchor", "pivot-x", "pivot-y",
            "position-x", "position-y", "min-width", "min-height",
            "flexible-width", "flexible-height", "child-alignment",
            "child-control-width", "child-control-height", "child-force-expand-width",
            "child-force-expand-height",
            "padding-left", "padding-right", "padding-top", "padding-bottom",
            "grid-columns", "cell-width", "cell-height"
        };

        /// <summary>Whether a property uses supported kebab-case naming.</summary>
        public static bool IsValidPropertyName(string name)
        {
            return !string.IsNullOrEmpty(name) && PropertyPattern.IsMatch(name);
        }

        /// <summary>Whether the default native renderer supports this property.</summary>
        public static bool IsBuiltInProperty(string name)
        {
            return BuiltInProperties.Contains(name ?? string.Empty);
        }

        /// <summary>List all requested classes, retaining the old single-class field.</summary>
        public static IEnumerable<string> EnumerateClasses(UIAuthoringNode node)
        {
            if (node == null) yield break;
            if (!string.IsNullOrEmpty(node.styleClass)) yield return node.styleClass;
            if (node.styleClasses != null)
            {
                foreach (string name in node.styleClasses) yield return name;
            }
        }

        /// <summary>Validate stylesheet shape, references and inheritance without scene mutation.</summary>
        public static void ValidateSheet(UIAuthoringStyleSheet sheet, List<string> errors)
        {
            if (sheet == null) return;
            if (sheet.version != 1) errors.Add("Unsupported stylesheet version: " + sheet.version + ". Expected 1.");
            if (sheet.tokens == null || sheet.classes == null)
            {
                errors.Add("Stylesheet tokens and classes must be arrays.");
                return;
            }
            if (sheet.tokens.Count > 512 || sheet.classes.Count > 512)
            {
                errors.Add("Stylesheets are limited to 512 tokens and 512 classes.");
            }
            Dictionary<string, UIAuthoringDesignToken> tokens = GetTokens(sheet);
            Dictionary<string, UIAuthoringStyleRule> classes = GetClasses(sheet);
            HashSet<string> tokenNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (UIAuthoringDesignToken token in sheet.tokens)
            {
                if (token == null || !TokenPattern.IsMatch(token.name ?? string.Empty))
                {
                    errors.Add("Stylesheet contains an invalid token name (expected --name).");
                    continue;
                }
                if (!tokenNames.Add(token.name)) errors.Add("Duplicate token: " + token.name);
                string value;
                TryExpandToken(token.value, tokens, "Token " + token.name + ": ", errors, out value);
            }
            HashSet<string> classNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (UIAuthoringStyleRule rule in sheet.classes)
            {
                if (rule == null || !ClassPattern.IsMatch(rule.name ?? string.Empty))
                {
                    errors.Add("Stylesheet contains an invalid class name.");
                    continue;
                }
                if (!classNames.Add(rule.name)) errors.Add("Duplicate class: " + rule.name);
                IUIAuthoringStyleClass external;
                if (UIAuthoringRegistry.TryGetStyleClass(rule.name, out external))
                {
                    errors.Add("Style class conflicts with registered extension: " + rule.name);
                }
                if (rule.properties == null)
                {
                    errors.Add("Class " + rule.name + " must contain a properties array.");
                    continue;
                }
                if (rule.properties.Count > 128) errors.Add("Class " + rule.name + " has too many declarations (128 max).");
                HashSet<string> properties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (UIAuthoringStyleDeclaration property in rule.properties)
                {
                    if (property == null || !IsValidPropertyName(property.name))
                    {
                        errors.Add("Class " + rule.name + " contains an invalid property name.");
                        continue;
                    }
                    if (!properties.Add(property.name)) errors.Add("Duplicate property '" + property.name + "' in " + rule.name);
                    string value;
                    if (TryExpandToken(property.value, tokens, "Class " + rule.name + ": ", errors, out value))
                    {
                        if (IsBuiltInProperty(property.name))
                        {
                            UIAuthoringStyle unused = new UIAuthoringStyle();
                            TryApplyBuiltIn(unused, property.name, value, "Class " + rule.name + ": ", errors);
                        }
                        else
                        {
                            IUIAuthoringStylePropertyAdapter adapter;
                            if (!UIAuthoringRegistry.TryGetStyleProperty(property.name, out adapter))
                            {
                                errors.Add("Class " + rule.name + " uses unsupported property '" + property.name + "'.");
                            }
                        }
                    }
                }
                HashSet<string> visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                ValidateInheritance(rule, classes, visiting, errors);
            }
        }

        /// <summary>Expand all class, legacy and inline properties for one schema node.</summary>
        internal static bool TryResolve(UIAuthoringNode node, UIAuthoringStyleSheet sheet,
            string label, List<string> errors, out UIAuthoringResolvedStyle resolved)
        {
            resolved = new UIAuthoringResolvedStyle();
            int initialErrors = errors.Count;
            if (node == null) return false;
            Dictionary<string, UIAuthoringDesignToken> tokens = GetTokens(sheet);
            Dictionary<string, UIAuthoringStyleRule> classes = GetClasses(sheet);
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            List<string> order = new List<string>();
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string className in EnumerateClasses(node))
            {
                if (string.IsNullOrEmpty(className) || !ClassPattern.IsMatch(className))
                {
                    errors.Add(label + "invalid style class name.");
                    continue;
                }
                if (!names.Add(className))
                {
                    errors.Add(label + "duplicate style class '" + className + "'.");
                    continue;
                }
                UIAuthoringStyleRule rule;
                IUIAuthoringStyleClass handler;
                bool sheetClass = classes.TryGetValue(className, out rule);
                bool registeredClass = UIAuthoringRegistry.TryGetStyleClass(className, out handler);
                if (sheetClass && registeredClass)
                {
                    errors.Add(label + "ambiguous stylesheet and registered class '" + className + "'.");
                }
                else if (sheetClass)
                {
                    ApplyClass(rule, classes, tokens, values, order,
                        new HashSet<string>(StringComparer.OrdinalIgnoreCase), label, errors);
                }
                else if (registeredClass)
                {
                    string error;
                    try
                    {
                        if (!handler.TryValidate(node, out error))
                        {
                            errors.Add(label + (string.IsNullOrEmpty(error) ? "style class rejected node." : error));
                        }
                    }
                    catch (Exception exception)
                    {
                        errors.Add(label + "style class validation failed: " + exception.Message);
                    }
                }
                else
                {
                    errors.Add(label + "unregistered style class '" + className + "'.");
                }
            }

            UIAuthoringStyle legacy = node.style;
            if (legacy != null)
            {
                if (legacy.width != 0f) Set(values, order, "width", Format(legacy.width));
                if (legacy.height != 0f) Set(values, order, "height", Format(legacy.height));
                if (!string.IsNullOrEmpty(legacy.background)) Set(values, order, "background", legacy.background);
                if (!string.IsNullOrEmpty(legacy.textColor)) Set(values, order, "text-color", legacy.textColor);
                if (legacy.fontSize != 0) Set(values, order, "font-size", legacy.fontSize.ToString(CultureInfo.InvariantCulture));
                if (!string.IsNullOrEmpty(legacy.layout)) Set(values, order, "layout", legacy.layout);
                if (legacy.spacing != 0f) Set(values, order, "spacing", Format(legacy.spacing));
                if (legacy.padding != 0) Set(values, order, "padding", legacy.padding.ToString(CultureInfo.InvariantCulture));
                if (!string.IsNullOrEmpty(legacy.textAlignment)) Set(values, order, "text-align", legacy.textAlignment);
                if (!string.IsNullOrEmpty(legacy.anchor)) Set(values, order, "anchor", legacy.anchor);
                if (!string.IsNullOrEmpty(legacy.pivotX)) Set(values, order, "pivot-x", legacy.pivotX);
                if (!string.IsNullOrEmpty(legacy.pivotY)) Set(values, order, "pivot-y", legacy.pivotY);
                if (!string.IsNullOrEmpty(legacy.positionX)) Set(values, order, "position-x", legacy.positionX);
                if (!string.IsNullOrEmpty(legacy.positionY)) Set(values, order, "position-y", legacy.positionY);
                if (!string.IsNullOrEmpty(legacy.minWidth)) Set(values, order, "min-width", legacy.minWidth);
                if (!string.IsNullOrEmpty(legacy.minHeight)) Set(values, order, "min-height", legacy.minHeight);
                if (!string.IsNullOrEmpty(legacy.flexibleWidth)) Set(values, order, "flexible-width", legacy.flexibleWidth);
                if (!string.IsNullOrEmpty(legacy.flexibleHeight)) Set(values, order, "flexible-height", legacy.flexibleHeight);
                if (!string.IsNullOrEmpty(legacy.childAlignment)) Set(values, order, "child-alignment", legacy.childAlignment);
                if (!string.IsNullOrEmpty(legacy.childControlWidth)) Set(values, order, "child-control-width", legacy.childControlWidth);
                if (!string.IsNullOrEmpty(legacy.childControlHeight)) Set(values, order, "child-control-height", legacy.childControlHeight);
                if (!string.IsNullOrEmpty(legacy.childForceExpandWidth)) Set(values, order, "child-force-expand-width", legacy.childForceExpandWidth);
                if (!string.IsNullOrEmpty(legacy.childForceExpandHeight)) Set(values, order, "child-force-expand-height", legacy.childForceExpandHeight);
                if (!string.IsNullOrEmpty(legacy.paddingLeft)) Set(values, order, "padding-left", legacy.paddingLeft);
                if (!string.IsNullOrEmpty(legacy.paddingRight)) Set(values, order, "padding-right", legacy.paddingRight);
                if (!string.IsNullOrEmpty(legacy.paddingTop)) Set(values, order, "padding-top", legacy.paddingTop);
                if (!string.IsNullOrEmpty(legacy.paddingBottom)) Set(values, order, "padding-bottom", legacy.paddingBottom);
                if (!string.IsNullOrEmpty(legacy.gridColumns)) Set(values, order, "grid-columns", legacy.gridColumns);
                if (!string.IsNullOrEmpty(legacy.cellWidth)) Set(values, order, "cell-width", legacy.cellWidth);
                if (!string.IsNullOrEmpty(legacy.cellHeight)) Set(values, order, "cell-height", legacy.cellHeight);
            }
            if (node.styleProperties != null)
            {
                if (node.styleProperties.Count > 128) errors.Add(label + "too many inline properties (128 max).");
                HashSet<string> inline = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (UIAuthoringStyleDeclaration property in node.styleProperties)
                {
                    if (property == null || !IsValidPropertyName(property.name))
                    {
                        errors.Add(label + "invalid inline property name.");
                        continue;
                    }
                    if (!inline.Add(property.name)) errors.Add(label + "duplicate inline property '" + property.name + "'.");
                    string value;
                    if (TryExpandToken(property.value, tokens, label, errors, out value))
                    {
                        Set(values, order, property.name, value);
                    }
                }
            }
            foreach (string property in order)
            {
                resolved.PropertyNames.Add(property);
                string value = values[property];
                if (IsBuiltInProperty(property))
                {
                    TryApplyBuiltIn(resolved.BuiltIn, property, value, label, errors);
                }
                else
                {
                    IUIAuthoringStylePropertyAdapter adapter;
                    if (!UIAuthoringRegistry.TryGetStyleProperty(property, out adapter))
                    {
                        errors.Add(label + "unsupported style property '" + property + "'.");
                        continue;
                    }
                    string error;
                    try
                    {
                        if (!adapter.TryValidate(node, value, out error))
                        {
                            errors.Add(label + "property '" + property + "': " +
                                (string.IsNullOrEmpty(error) ? "invalid value." : error));
                        }
                        else
                        {
                            resolved.Custom.Add(new UIAuthoringResolvedDeclaration { Name = property, Value = value });
                        }
                    }
                    catch (Exception exception)
                    {
                        errors.Add(label + "property '" + property + "' validation failed: " + exception.Message);
                    }
                }
            }
            return errors.Count == initialErrors;
        }

        /// <summary>Get token definitions without keeping scene or domain state.</summary>
        private static Dictionary<string, UIAuthoringDesignToken> GetTokens(UIAuthoringStyleSheet sheet)
        {
            Dictionary<string, UIAuthoringDesignToken> result =
                new Dictionary<string, UIAuthoringDesignToken>(StringComparer.Ordinal);
            if (sheet == null || sheet.tokens == null) return result;
            foreach (UIAuthoringDesignToken token in sheet.tokens)
            {
                if (token != null && !string.IsNullOrEmpty(token.name) && !result.ContainsKey(token.name))
                    result.Add(token.name, token);
            }
            return result;
        }

        /// <summary>Get class definitions while respecting the first duplicate for error reporting.</summary>
        private static Dictionary<string, UIAuthoringStyleRule> GetClasses(UIAuthoringStyleSheet sheet)
        {
            Dictionary<string, UIAuthoringStyleRule> result =
                new Dictionary<string, UIAuthoringStyleRule>(StringComparer.OrdinalIgnoreCase);
            if (sheet == null || sheet.classes == null) return result;
            foreach (UIAuthoringStyleRule rule in sheet.classes)
            {
                if (rule != null && !string.IsNullOrEmpty(rule.name) && !result.ContainsKey(rule.name))
                    result.Add(rule.name, rule);
            }
            return result;
        }

        /// <summary>Check inheritance without allowing circular class references.</summary>
        private static void ValidateInheritance(UIAuthoringStyleRule rule,
            Dictionary<string, UIAuthoringStyleRule> classes, HashSet<string> visiting, List<string> errors)
        {
            if (rule == null || string.IsNullOrEmpty(rule.name)) return;
            if (!visiting.Add(rule.name))
            {
                errors.Add("Cyclic stylesheet inheritance at class " + rule.name + ".");
                return;
            }
            if (!string.IsNullOrEmpty(rule.extends))
            {
                UIAuthoringStyleRule parent;
                if (!classes.TryGetValue(rule.extends, out parent))
                    errors.Add("Class " + rule.name + " extends missing class " + rule.extends + ".");
                else ValidateInheritance(parent, classes, visiting, errors);
            }
            visiting.Remove(rule.name);
        }

        /// <summary>Merge inherited declarations before child class declarations.</summary>
        private static void ApplyClass(UIAuthoringStyleRule rule,
            Dictionary<string, UIAuthoringStyleRule> classes,
            Dictionary<string, UIAuthoringDesignToken> tokens,
            Dictionary<string, string> values, List<string> order, HashSet<string> visiting,
            string label, List<string> errors)
        {
            if (rule == null || !visiting.Add(rule.name))
            {
                errors.Add(label + "cyclic style inheritance.");
                return;
            }
            if (!string.IsNullOrEmpty(rule.extends))
            {
                UIAuthoringStyleRule parent;
                if (!classes.TryGetValue(rule.extends, out parent))
                    errors.Add(label + "missing inherited class '" + rule.extends + "'.");
                else ApplyClass(parent, classes, tokens, values, order, visiting, label, errors);
            }
            if (rule.properties == null)
            {
                errors.Add(label + "class " + rule.name + " has no properties array.");
            }
            else
            {
                foreach (UIAuthoringStyleDeclaration property in rule.properties)
                {
                    if (property == null || !IsValidPropertyName(property.name))
                    {
                        errors.Add(label + "invalid property in class " + rule.name + ".");
                        continue;
                    }
                    string value;
                    if (TryExpandToken(property.value, tokens, label, errors, out value))
                    {
                        Set(values, order, property.name, value);
                    }
                }
            }
            visiting.Remove(rule.name);
        }

        /// <summary>Resolve a full-value var token; fail safely on missing references or cycles.</summary>
        private static bool TryExpandToken(string input,
            Dictionary<string, UIAuthoringDesignToken> tokens, string label,
            List<string> errors, out string value)
        {
            value = input == null ? string.Empty : input.Trim();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < 32; index++)
            {
                Match match = TokenReference.Match(value);
                if (!match.Success)
                {
                    if (value.StartsWith("var(", StringComparison.Ordinal))
                    {
                        errors.Add(label + "invalid token expression '" + value + "'. Expected var(--name).");
                        return false;
                    }
                    return true;
                }
                string name = match.Groups[1].Value;
                if (!seen.Add(name))
                {
                    errors.Add(label + "cyclic token reference involving " + name + ".");
                    return false;
                }
                UIAuthoringDesignToken token;
                if (!tokens.TryGetValue(name, out token))
                {
                    errors.Add(label + "unknown design token " + name + ".");
                    return false;
                }
                value = (token.value ?? string.Empty).Trim();
            }
            errors.Add(label + "design token reference is too deep.");
            return false;
        }

        /// <summary>Assign last-wins values in a deterministic property order.</summary>
        private static void Set(Dictionary<string, string> values, List<string> order,
            string name, string value)
        {
            if (!values.ContainsKey(name)) order.Add(name);
            values[name] = value;
        }

        /// <summary>Invariant float formatting for old numeric fields.</summary>
        private static string Format(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        /// <summary>Validate and convert a built-in property to native-uGUI style data.</summary>
        private static void TryApplyBuiltIn(UIAuthoringStyle style, string name, string value,
            string label, List<string> errors)
        {
            float number;
            int integer;
            if (name == "width" || name == "height" || name == "spacing")
            {
                if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) ||
                    float.IsNaN(number) || float.IsInfinity(number) || number < 0f)
                {
                    errors.Add(label + name + " must be a nonnegative finite number.");
                    return;
                }
                if (name == "width") style.width = number;
                else if (name == "height") style.height = number;
                else style.spacing = number;
            }
            else if (name == "padding" || name == "font-size")
            {
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer) || integer < 0)
                {
                    errors.Add(label + name + " must be a nonnegative integer.");
                    return;
                }
                if (name == "padding") style.padding = integer;
                else style.fontSize = integer;
            }
            else if (name == "background" || name == "text-color")
            {
                if (!HexColor.IsMatch(value))
                {
                    errors.Add(label + name + " must be a #RRGGBB or #RRGGBBAA color.");
                    return;
                }
                if (name == "background") style.background = value;
                else style.textColor = value;
            }
            else if (name == "layout")
            {
                if (value != "horizontal" && value != "vertical" && value != "grid" && value != "none")
                {
                    errors.Add(label + "layout must be horizontal, vertical, grid, or none.");
                    return;
                }
                style.layout = value == "none" ? string.Empty : value;
            }
            else if (name == "text-align")
            {
                if (value != "left" && value != "center" && value != "right")
                {
                    errors.Add(label + "text-align must be left, center, or right.");
                    return;
                }
                style.textAlignment = value;
            }
            else
            {
                string error;
                if (!UIAuthoringLayoutProperties.TryApply(style, name, value, out error))
                {
                    errors.Add(label + name + ": " + error);
                }
            }
        }
    }
}
