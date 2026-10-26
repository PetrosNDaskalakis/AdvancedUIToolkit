using System.Collections.Generic;
using NUnit.Framework;
using AdvancedUIToolkit.Editor;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Tests
{
    /// <summary>
    /// EditMode tests for the simplified Workspace's selection and read-only component preview.
    /// These tests do not modify assets or trigger code generation.
    /// </summary>
    public sealed class AdvancedUIToolkitSimplifiedWorkspaceTests
    {
        /// <summary>Confirms a new panel gets its own readable bindings class by default.</summary>
        [Test]
        public void NewRootGetsSuggestedBindingsClassName()
        {
            Assert.AreEqual("WeaponPanelBindings", UIAutoBinderWindow.SuggestClassName("WeaponPanel"));
            Assert.AreEqual("WeaponPanelBindings", UIAutoBinderWindow.SuggestClassName("PNL_WeaponPanel"));
            Assert.AreEqual("_3DPanelBindings", UIAutoBinderWindow.SuggestClassName("3D Panel"));
        }

        /// <summary>
        /// Detects a hierarchy rename even if the public C# property is preserved.
        /// This tests read-only scanning and never generates source files.
        /// </summary>
        [Test]
        public void PreservedApiRenameStillAppearsAsPendingManifestChange()
        {
            GameObject panel = new GameObject("WeaponPanel", typeof(RectTransform));
            GameObject button = new GameObject("BTN_Arm", typeof(RectTransform),
                typeof(Image), typeof(Button));
            UIAutoBinderWindow scanner = null;
            try
            {
                UIBindingRoot root = panel.AddComponent<UIBindingRoot>();
                button.transform.SetParent(panel.transform, false);
                Button buttonComponent = button.GetComponent<Button>();
                root.EditorSetEntries(new List<UIBindingRoot.Entry>
                {
                    new UIBindingRoot.Entry
                    {
                        Key = "Arm", HierarchyKey = "Arm", StableId = "test-arm-stable-id",
                        ComponentType = typeof(Button).FullName, Reference = buttonComponent
                    }
                });
                scanner = ScriptableObject.CreateInstance<UIAutoBinderWindow>();
                scanner.SetWorkspaceTarget(panel);
                Assert.AreEqual(0, scanner.WorkspaceChangeCount);

                button.name = "BTN_ArmWeapon";
                scanner.RefreshWorkspace();
                Assert.AreEqual(1, scanner.WorkspaceChangeCount);
                Assert.AreEqual("Arm", scanner.GetWorkspaceBindings()[0].ApiKey);
            }
            finally
            {
                if (scanner != null) Object.DestroyImmediate(scanner);
                Object.DestroyImmediate(button);
                Object.DestroyImmediate(panel);
            }
        }

        /// <summary>Ensures a selected nested control resolves to its owning UI root.</summary>
        [Test]
        public void SelectingChildControlUsesClosestBindingRoot()
        {
            GameObject panel = new GameObject("WeaponPanel", typeof(RectTransform));
            GameObject button = new GameObject("BTN_Fire", typeof(RectTransform));
            try
            {
                panel.AddComponent<UIBindingRoot>();
                button.transform.SetParent(panel.transform, false);
                Assert.AreSame(panel, AdvancedUIToolkitWorkspaceWindow.ResolveRoot(button));
            }
            finally
            {
                Object.DestroyImmediate(button);
                Object.DestroyImmediate(panel);
            }
        }

        /// <summary>Ensures a new panel can be bound before it has a registered manifest.</summary>
        [Test]
        public void UnregisteredRectTransformIsAValidSelectionCandidate()
        {
            GameObject panel = new GameObject("NewScreen", typeof(RectTransform));
            try
            {
                Assert.AreSame(panel, AdvancedUIToolkitWorkspaceWindow.ResolveRoot(panel));
            }
            finally
            {
                Object.DestroyImmediate(panel);
            }
        }

        /// <summary>Ensures the Workspace does not treat non-UI scene objects as panels.</summary>
        [Test]
        public void OrdinaryGameObjectIsNotAnAutomaticUIRoot()
        {
            GameObject objectWithoutUI = new GameObject("OrdinaryObject");
            try
            {
                Assert.IsNull(AdvancedUIToolkitWorkspaceWindow.ResolveRoot(objectWithoutUI));
            }
            finally
            {
                Object.DestroyImmediate(objectWithoutUI);
            }
        }

        /// <summary>
        /// Confirms UI component discovery remains read-only and no manifest is written
        /// until the user explicitly selects the generation action.
        /// </summary>
        [Test]
        public void ComponentPreviewDoesNotCreateManifestEntries()
        {
            GameObject panel = new GameObject("WeaponPanel", typeof(RectTransform));
            GameObject button = new GameObject("BTN_Arm", typeof(RectTransform),
                typeof(Image), typeof(Button));
            UIAutoBinderWindow scanner = null;
            try
            {
                UIBindingRoot root = panel.AddComponent<UIBindingRoot>();
                button.transform.SetParent(panel.transform, false);
                scanner = ScriptableObject.CreateInstance<UIAutoBinderWindow>();
                scanner.SetWorkspaceTarget(panel);

                Assert.AreEqual(1, scanner.GetWorkspaceBindings().Count);
                Assert.AreEqual("Arm", scanner.GetWorkspaceBindings()[0].ApiKey);
                Assert.AreEqual(1, scanner.WorkspaceChangeCount);
                Assert.AreEqual(0, root.EditorGetEntries().Count);
            }
            finally
            {
                if (scanner != null) Object.DestroyImmediate(scanner);
                Object.DestroyImmediate(button);
                Object.DestroyImmediate(panel);
            }
        }
    }
}
