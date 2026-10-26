using System;
using AdvancedUIToolkit;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Samples
{
    /// <summary>
    /// Demonstration of generated component references, kept hand-written
    /// so this imported sample compiles without a code-generation step.
    /// </summary>
    public sealed class ScreenBindingsSample : IUIBindings
    {
        /// <summary>References the panel's Arm button.</summary>
        public Button Arm { get; private set; }

        /// <summary>References the panel's Fire button.</summary>
        public Button Fire { get; private set; }

        /// <summary>References the panel's Load button.</summary>
        public Button Load { get; private set; }

        /// <summary>Resolves the three buttons from the serialized UI root.</summary>
        /// <param name="root">Owner of the prepared button binding manifest.</param>
        public void Initialize(UIBindingRoot root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            Arm = root.Get<Button>("Arm");
            Fire = root.Get<Button>("Fire");
            Load = root.Get<Button>("Load");
        }
    }
}
