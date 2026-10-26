using NUnit.Framework;
using AdvancedUIToolkit.Editor;
using UnityEngine;

namespace AdvancedUIToolkit.Tests
{
    /// <summary>
    /// Regression coverage for root selection while navigating nested prefab UI scopes.
    /// Tests never write generated code or modify project assets.
    /// </summary>
    public sealed class AdvancedUIToolkitWorkspaceSelectionRegressionTests
    {
        /// <summary>
        /// Selecting a nested UI binding root must open that root instead of its parent.
        /// </summary>
        [Test]
        public void SelectingNestedBindingRootSwitchesScope()
        {
            GameObject parent = new GameObject("Station", typeof(RectTransform), typeof(UIBindingRoot));
            GameObject nested = new GameObject("TorpedoControls", typeof(RectTransform), typeof(UIBindingRoot));
            GameObject button = new GameObject("BTN_Load", typeof(RectTransform));
            try
            {
                nested.transform.SetParent(parent.transform, false);
                button.transform.SetParent(nested.transform, false);

                Assert.AreSame(nested, AdvancedUIToolkitWorkspaceWindow.ResolveSelectionTarget(parent, button));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        /// <summary>
        /// Selecting a normal control does not accidentally replace the active screen.
        /// </summary>
        [Test]
        public void SelectingOrdinaryChildPreservesCurrentRoot()
        {
            GameObject parent = new GameObject("Station", typeof(RectTransform), typeof(UIBindingRoot));
            GameObject child = new GameObject("BTN_Fire", typeof(RectTransform));
            try
            {
                child.transform.SetParent(parent.transform, false);
                Assert.AreSame(parent, AdvancedUIToolkitWorkspaceWindow.ResolveSelectionTarget(parent, child));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        /// <summary>
        /// Clicking a non-UI object does not unexpectedly remove the previous UI selection.
        /// </summary>
        [Test]
        public void SelectingNonUiObjectPreservesCurrentRoot()
        {
            GameObject panel = new GameObject("Station", typeof(RectTransform), typeof(UIBindingRoot));
            GameObject other = new GameObject("Light");
            try
            {
                Assert.AreSame(panel, AdvancedUIToolkitWorkspaceWindow.ResolveSelectionTarget(panel, other));
            }
            finally
            {
                Object.DestroyImmediate(panel);
                Object.DestroyImmediate(other);
            }
        }

        /// <summary>
        /// A separate screen selected in the scene becomes the new active UI root.
        /// </summary>
        [Test]
        public void SelectingIndependentRootSwitchesScope()
        {
            GameObject first = new GameObject("Sonar", typeof(RectTransform), typeof(UIBindingRoot));
            GameObject second = new GameObject("Weapons", typeof(RectTransform), typeof(UIBindingRoot));
            try
            {
                Assert.AreSame(second, AdvancedUIToolkitWorkspaceWindow.ResolveSelectionTarget(first, second));
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }
    }
}
