using System;
using System.Collections.Generic;

namespace AdvancedUIToolkit.Editor
{
    /// <summary>
    /// Public Editor-only extension point for third-party binding components.
    /// Bindable GameObjects use PREFIX_Accessor and must contain one unambiguous
    /// component of the registered type on the named object or in its children.
    /// Registrations are in-memory and must be restored after Unity domain reload.
    /// </summary>
    public static class AdvancedUIComponentRegistry
    {
        /// <summary>
        /// Adds an external component prefix. Existing default and custom mappings
        /// cannot be replaced. Repeating the same mapping is safe and has no side effects.
        /// </summary>
        /// <param name="prefix">Unique 2-8 character uppercase binding prefix.</param>
        /// <param name="componentType">Public, concrete Unity Component subtype in a runtime assembly.</param>
        /// <param name="error">Actionable failure reason.</param>
        /// <returns>True when registered or when an identical mapping exists.</returns>
        public static bool TryRegister(string prefix, Type componentType, out string error)
        {
            return AdvancedUIToolkitNaming.TryRegister(prefix, componentType, out error);
        }

        /// <summary>Returns the registered component type for a binding prefix.</summary>
        /// <param name="prefix">Exact case-sensitive prefix.</param>
        /// <param name="componentType">Resolved type, or null when unknown.</param>
        /// <returns>True if a type is registered for this prefix.</returns>
        public static bool TryGetType(string prefix, out Type componentType)
        {
            return AdvancedUIToolkitNaming.TypeByPrefix.TryGetValue(prefix, out componentType);
        }

        /// <summary>
        /// Returns a read-only snapshot of the current prefix mappings. Modifications
        /// made by external consumers to local collections cannot change the registry.
        /// </summary>
        /// <returns>Snapshot of built-in and external mappings.</returns>
        public static IReadOnlyDictionary<string, Type> GetRegisteredTypes()
        {
            return AdvancedUIToolkitNaming.GetSnapshot();
        }
    }
}
