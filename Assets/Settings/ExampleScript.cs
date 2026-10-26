
using System;
using AdvancedUIToolkit;

namespace Example.Template
{
    /// <summary>
    /// Example template used by Advanced UI Toolkit to generate
    /// strongly typed UI component references.
    /// </summary>
    public sealed class ExampleScript : IUIBindings
    {
        // <ADVANCEDUITOOLKIT:PROPERTIES>

        /// <summary>
        /// Initializes the generated UI component references.
        /// </summary>
        /// <param name="root">
        /// The UI binding root containing registered components.
        /// </param>
        public void Initialize(UIBindingRoot root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            // <ADVANCEDUITOOLKIT:ASSIGNMENTS>
        }
    }
}
