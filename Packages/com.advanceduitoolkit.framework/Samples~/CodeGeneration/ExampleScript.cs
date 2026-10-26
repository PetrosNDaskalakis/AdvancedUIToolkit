using System;
using AdvancedUIToolkit;

namespace Example.Template
{
    /// <summary>
    /// Example layout used by Advanced UI Toolkit to generate a screen's typed references.
    /// Edit the spacing, using directives, XML comments and any custom helper methods.
    /// Keep the two insertion markers and the Initialize method intact.
    /// </summary>
    public sealed class ExampleScript : IUIBindings
    {
        // Optional: customize the XML summary for each generated reference.
        // Supported tokens: {name}, {hierarchy}, {type}.
        // <ADVANCEDUITOOLKIT:PROPERTY_DOC>Reference to {name} ({type}) on {hierarchy}.
        // <ADVANCEDUITOOLKIT:PROPERTIES>

        /// <summary>Resolves component references once when the owning view is created.</summary>
        /// <param name="root">The UI root holding the serialized component manifest.</param>
        public void Initialize(UIBindingRoot root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            // <ADVANCEDUITOOLKIT:ASSIGNMENTS>
        }
    }
}
