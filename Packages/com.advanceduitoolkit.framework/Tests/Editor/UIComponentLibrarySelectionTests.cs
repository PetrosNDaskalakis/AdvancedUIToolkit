using NUnit.Framework;
using AdvancedUIToolkit.Editor;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Tests
{
    /// <summary>
    /// Ensures that the component library follows Hierarchy selection while keeping
    /// creation inside the intended UI root when ordinary controls are selected.
    /// </summary>
    public sealed class UIComponentLibrarySelectionTests
    {
        /// <summary>Tests switching between independent UI panels with no manual root button.</summary>
        [Test]
        public void SwitchingBetweenIndependentPanelsUpdatesRoot()
        {
            GameObject first = new GameObject("SonarPanel", typeof(RectTransform));
            GameObject second = new GameObject("WeaponsPanel", typeof(RectTransform));
            try
            {
                Assert.AreSame(second.transform,
                    UIComponentLibraryWindow.ResolveSelectionRoot((RectTransform)first.transform, second));
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        /// <summary>Tests that a selected button never becomes the new creation root.</summary>
        [Test]
        public void SelectingButtonInsideCurrentPanelPreservesRoot()
        {
            GameObject panel = new GameObject("WeaponPanel", typeof(RectTransform));
            GameObject button = new GameObject("BTN_Fire", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                button.transform.SetParent(panel.transform, false);
                Assert.AreSame(panel.transform,
                    UIComponentLibraryWindow.ResolveSelectionRoot((RectTransform)panel.transform, button));
            }
            finally
            {
                Object.DestroyImmediate(panel);
            }
        }

        /// <summary>Tests selecting a nested UI root with its own binding manifest.</summary>
        [Test]
        public void SelectingNestedBindingRootSwitchesToNestedScope()
        {
            GameObject station = new GameObject("Station", typeof(RectTransform), typeof(UIBindingRoot));
            GameObject nested = new GameObject("NestedControls", typeof(RectTransform), typeof(UIBindingRoot));
            GameObject button = new GameObject("BTN_Test", typeof(RectTransform));
            try
            {
                nested.transform.SetParent(station.transform, false);
                button.transform.SetParent(nested.transform, false);
                Assert.AreSame(nested.transform,
                    UIComponentLibraryWindow.ResolveSelectionRoot((RectTransform)station.transform, button));
            }
            finally
            {
                Object.DestroyImmediate(station);
            }
        }

        /// <summary>Tests switching to a sibling panel under the same parent UI root.</summary>
        [Test]
        public void SelectingSiblingPanelUnderSameBindingRootSwitchesTarget()
        {
            GameObject canvas = new GameObject("Canvas", typeof(RectTransform), typeof(UIBindingRoot));
            GameObject first = new GameObject("SonarPanel", typeof(RectTransform));
            GameObject second = new GameObject("WeaponsPanel", typeof(RectTransform));
            try
            {
                first.transform.SetParent(canvas.transform, false);
                second.transform.SetParent(canvas.transform, false);
                Assert.AreSame(second.transform,
                    UIComponentLibraryWindow.ResolveSelectionRoot((RectTransform)first.transform, second));
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        /// <summary>Tests switching to a nested panel when explicitly selected.</summary>
        [Test]
        public void ExplicitNestedPanelSelectionSwitchesTarget()
        {
            GameObject parent = new GameObject("StationPanel", typeof(RectTransform));
            GameObject nested = new GameObject("PNL_Controls", typeof(RectTransform));
            try
            {
                nested.transform.SetParent(parent.transform, false);
                Assert.AreSame(nested.transform,
                    UIComponentLibraryWindow.ResolveSelectionRoot((RectTransform)parent.transform, nested));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        /// <summary>Tests that ordinary non-UI selections don't clear the active root.</summary>
        [Test]
        public void SelectingNonUIObjectPreservesCurrentRoot()
        {
            GameObject panel = new GameObject("StationPanel", typeof(RectTransform));
            GameObject light = new GameObject("Light");
            try
            {
                Assert.AreSame(panel.transform,
                    UIComponentLibraryWindow.ResolveSelectionRoot((RectTransform)panel.transform, light));
            }
            finally
            {
                Object.DestroyImmediate(panel);
                Object.DestroyImmediate(light);
            }
        }
    }
}
