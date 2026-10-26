using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Editor
{
    /// <summary>
    /// Single source of truth for AutoBinder's supported prefixes.
    /// Factory menu names below must match these prefix definitions.
    /// </summary>
    internal static class AdvancedUIToolkitNaming
    {
        /// <summary>Stores the type by prefix reference or configuration value.</summary>
        internal static readonly Dictionary<string, Type> TypeByPrefix =
            new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                { "TXT", typeof(Text) },
                { "BTN", typeof(Button) },
                { "IMG", typeof(Image) },
                { "RAW", typeof(RawImage) },
                { "TGL", typeof(Toggle) },
                { "SLD", typeof(Slider) },
                { "INP", typeof(InputField) },
                { "SCR", typeof(ScrollRect) },
                { "SBR", typeof(Scrollbar) },
                { "DDP", typeof(Dropdown) },
                { "RT", typeof(RectTransform) },
                { "PNL", typeof(RectTransform) }
            };

        // Generated classes have an Initialize method, so a property with
        // this identifier would conflict with the required interface member.
        /// <summary>Stores the reserved reference or configuration value.</summary>
        private static readonly HashSet<string> Reserved =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "Initialize", "class", "namespace", "public", "private", "internal",
                "protected", "new", "return", "event", "void", "object", "string",
                "int", "float", "bool", "default", "base", "this", "null", "true",
                "false", "using", "static", "sealed", "readonly", "ref", "out",
                "in", "params", "operator", "implicit", "explicit", "is", "as",
                "if", "else", "for", "foreach", "while", "do", "switch", "case",
                "break", "continue", "goto", "try", "catch", "finally", "throw",
                "typeof", "sizeof", "checked", "unchecked", "lock", "fixed",
                "unsafe", "volatile", "abstract", "virtual", "override", "interface",
                "struct", "enum", "delegate", "const", "get", "set", "value",
                "yield", "await", "async", "var", "dynamic", "decimal", "double",
                "long", "short", "byte", "sbyte", "uint", "ulong", "ushort",
                "char", "stackalloc", "extern", "partial"
            };

        /// <summary>
        /// Resolves a named control from its own GameObject or, for composite prefab
        /// wrappers, from exactly one matching descendant. Root components always win.
        /// Separate nested binding roots are not searched, and ambiguous children are
        /// never chosen arbitrarily.
        /// </summary>
        /// <param name="namedObject">The GameObject whose name declares the binding.</param>
        /// <param name="expectedType">The uGUI component type implied by its prefix.</param>
        /// <param name="problem">A diagnostic when no unique component can be resolved.</param>
        /// <returns>The component to bind, or null when none can be chosen safely.</returns>
        internal static Component ResolveNamedComponent(Transform namedObject, Type expectedType,
            out string problem)
        {
            problem = null;
            if (namedObject == null || expectedType == null) return null;

            Component onRoot = namedObject.GetComponent(expectedType);
            if (onRoot != null) return onRoot;

            Component match = null;
            foreach (Transform descendant in namedObject.GetComponentsInChildren<Transform>(true))
            {
                if (descendant == namedObject || InsideDifferentBindingRoot(descendant, namedObject))
                    continue;

                Component candidate = descendant.GetComponent(expectedType);
                if (candidate == null) continue;
                if (match != null)
                {
                    problem = "has multiple " + expectedType.Name +
                        " components in its children. Move the intended " + expectedType.Name +
                        " to the named GameObject or give the intended child its own binding name.";
                    return null;
                }
                match = candidate;
            }

            if (match == null)
                problem = "has no " + expectedType.Name +
                    " component on itself or within its binding scope.";
            return match;
        }

        /// <summary>
        /// Checks whether a candidate descendant belongs to another binding scope.
        /// The named object's own scope is always allowed.
        /// </summary>
        /// <param name="candidate">Potential child component owner.</param>
        /// <param name="namedObject">Root of the requested binding.</param>
        /// <returns>True when an intermediate object owns a different binding root.</returns>
        private static bool InsideDifferentBindingRoot(Transform candidate, Transform namedObject)
        {
            for (Transform node = candidate; node != null && node != namedObject; node = node.parent)
            {
                if (node.GetComponent<UIBindingRoot>() != null) return true;
            }
            return false;
        }

        /// <summary>Determines whether reserved accessor name.</summary>
        internal static bool IsReservedAccessorName(string identifier) =>
            Reserved.Contains(identifier);
    }
}
