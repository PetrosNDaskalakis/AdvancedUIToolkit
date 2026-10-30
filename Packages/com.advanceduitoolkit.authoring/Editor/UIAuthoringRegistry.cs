using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedUIAuthoring.Editor
{
    /// <summary>Creates one native UI element. Implementations must not select objects or register Undo.</summary>
    public interface IUIAuthoringElementFactory
    {
        /// <summary>Whether other schema elements may be parented under this kind.</summary>
        bool CanContainChildren { get; }

        /// <summary>Create a RectTransform-based object without parenting it to the scene.</summary>
        GameObject Create(UIAuthoringNode node);
    }

    /// <summary>Optional factory-side preflight for custom element-specific constraints.</summary>
    public interface IUIAuthoringElementValidator
    {
        /// <summary>Validate this element without changing scene objects.</summary>
        bool TryValidate(UIAuthoringNode node, out string error);
    }

    /// <summary>Optional named style extension for future themes and project-specific appearance.</summary>
    public interface IUIAuthoringStyleClass
    {
        /// <summary>Validate node-specific style requirements without changing scene objects.</summary>
        bool TryValidate(UIAuthoringNode node, out string error);

        /// <summary>Apply only this extension's custom styling to a freshly created object.</summary>
        void Apply(GameObject target, UIAuthoringNode node);
    }

    /// <summary>Additional declarative style property provided by a separate Editor package.</summary>
    public interface IUIAuthoringStylePropertyAdapter
    {
        /// <summary>Validate the fully resolved declaration for the given element.</summary>
        bool TryValidate(UIAuthoringNode node, string value, out string error);

        /// <summary>Apply a validated property to a newly created native Unity UI object.</summary>
        void Apply(GameObject target, UIAuthoringNode node, string value);
    }

    /// <summary>Editor-only registry with built-in kinds and optional external extensions.</summary>
    public static class UIAuthoringRegistry
    {
        /// <summary>Factories keyed by element kind.</summary>
        private static readonly Dictionary<string, IUIAuthoringElementFactory> Factories =
            new Dictionary<string, IUIAuthoringElementFactory>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Extension handlers keyed by style class name.</summary>
        private static readonly Dictionary<string, IUIAuthoringStyleClass> Classes =
            new Dictionary<string, IUIAuthoringStyleClass>(StringComparer.OrdinalIgnoreCase);

        /// <summary>External style-property adapters keyed by CSS-inspired names.</summary>
        private static readonly Dictionary<string, IUIAuthoringStylePropertyAdapter> StyleProperties =
            new Dictionary<string, IUIAuthoringStylePropertyAdapter>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Registers native kinds, once per Editor domain.</summary>
        static UIAuthoringRegistry()
        {
            Factories.Add("container", new UIBuiltInElementFactory("container"));
            Factories.Add("text", new UIBuiltInElementFactory("text"));
            Factories.Add("image", new UIBuiltInElementFactory("image"));
            Factories.Add("button", new UIBuiltInElementFactory("button"));
        }

        /// <summary>Register an additional element kind without overriding built-in kinds.</summary>
        public static bool TryRegisterElement(string kind, IUIAuthoringElementFactory factory, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(kind) || factory == null)
            {
                error = "A nonempty kind and a factory are required.";
                return false;
            }
            if (Factories.ContainsKey(kind))
            {
                error = "Element kind is already registered: " + kind;
                return false;
            }
            Factories.Add(kind, factory);
            return true;
        }

        /// <summary>Register an optional style-class implementation; no CSS parser is implied.</summary>
        public static bool TryRegisterStyleClass(string name, IUIAuthoringStyleClass handler, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(name) || handler == null)
            {
                error = "A nonempty class name and a handler are required.";
                return false;
            }
            if (Classes.ContainsKey(name))
            {
                error = "Style class is already registered: " + name;
                return false;
            }
            Classes.Add(name, handler);
            return true;
        }

        /// <summary>Register an additional style property without overriding built-in declarations.</summary>
        public static bool TryRegisterStyleProperty(string name, IUIAuthoringStylePropertyAdapter adapter,
            out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(name) || adapter == null ||
                !UIAuthoringStyleResolver.IsValidPropertyName(name))
            {
                error = "A valid property name and an adapter are required.";
                return false;
            }
            if (UIAuthoringStyleResolver.IsBuiltInProperty(name) || StyleProperties.ContainsKey(name))
            {
                error = "Style property is already defined: " + name;
                return false;
            }
            StyleProperties.Add(name, adapter);
            return true;
        }

        /// <summary>Find a registered external property adapter.</summary>
        public static bool TryGetStyleProperty(string name, out IUIAuthoringStylePropertyAdapter adapter)
        {
            return StyleProperties.TryGetValue(name ?? string.Empty, out adapter);
        }

        /// <summary>Finds a registered factory, without creating a scene object.</summary>
        public static bool TryGetElement(string kind, out IUIAuthoringElementFactory factory)
        {
            return Factories.TryGetValue(kind ?? string.Empty, out factory);
        }

        /// <summary>Finds an optional registered style class.</summary>
        public static bool TryGetStyleClass(string name, out IUIAuthoringStyleClass handler)
        {
            return Classes.TryGetValue(name ?? string.Empty, out handler);
        }
    }
}
