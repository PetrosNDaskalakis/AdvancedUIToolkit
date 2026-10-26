using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using AdvancedUIToolkit.Editor;

namespace AdvancedUIToolkit.Tests
{
    /// <summary>Tests removal detection and empty-root code generation.</summary>
    public sealed class AdvancedUIToolkitDeletedBindingsTests
    {
        /// <summary>Tracks the temporary UI hierarchy used by each test.</summary>
        private GameObject rootObject;

        /// <summary>Tracks the Editor scanner created for a test.</summary>
        private UIAutoBinderWindow scanner;

        /// <summary>Creates an isolated UI binding root for the test.</summary>
        [SetUp]
        public void SetUp()
        {
            rootObject = new GameObject("WeaponPanel", typeof(RectTransform), typeof(UIBindingRoot));
            scanner = ScriptableObject.CreateInstance<UIAutoBinderWindow>();
        }

        /// <summary>Removes the test hierarchy and Editor scanner.</summary>
        [TearDown]
        public void TearDown()
        {
            if (scanner != null) UnityEngine.Object.DestroyImmediate(scanner);
            if (rootObject != null) UnityEngine.Object.DestroyImmediate(rootObject);
        }

        /// <summary>Deleted prefab-like controls must become pending removals.</summary>
        [Test]
        public void DeletingBoundButtonIsReportedAsPendingRemoval()
        {
            GameObject button = AddRegisteredButton("Fire");
            scanner.SetWorkspaceTarget(rootObject);
            Assert.AreEqual(0, scanner.WorkspaceRemovedCount);

            UnityEngine.Object.DestroyImmediate(button);
            scanner.RefreshWorkspace();

            Assert.AreEqual(1, scanner.WorkspaceRemovedCount);
            Assert.AreEqual(1, scanner.WorkspaceChangeCount);
            Assert.IsTrue(scanner.WorkspaceCanGenerate,
                "Deleting the final UI component must still permit generation.");
        }

        /// <summary>Removing just one component must retain all other bindings.</summary>
        [Test]
        public void DeletingOneButtonKeepsOtherReferences()
        {
            GameObject fire = AddRegisteredButton("Fire");
            AddRegisteredButton("Arm");
            UnityEngine.Object.DestroyImmediate(fire);
            scanner.SetWorkspaceTarget(rootObject);

            Assert.AreEqual(1, scanner.WorkspaceRemovedCount);
            Assert.IsTrue(scanner.WorkspaceCanGenerate);
            List<UIAutoBinderWindow.WorkspaceBinding> bindings = scanner.GetWorkspaceBindings();
            Assert.AreEqual(1, bindings.Count);
            Assert.AreEqual("Arm", bindings[0].ApiKey);
        }

        /// <summary>An empty manifest produces valid, documented C# bindings.</summary>
        [Test]
        public void EmptyBindingClassCanBeGenerated()
        {
            string output = AdvancedUIToolkitCodeTemplate.Generate("WeaponPanelBindings", "Game.UI.Generated",
                new List<UIBindingRoot.Entry>(), null);
            StringAssert.Contains("public sealed class WeaponPanelBindings : IUIBindings", output);
            StringAssert.Contains("public void Initialize(UIBindingRoot root)", output);
            Assert.IsFalse(output.Contains("root.Get<"));
        }

        /// <summary>New, never-generated empty roots remain invalid.</summary>
        [Test]
        public void BrandNewEmptyRootNeedsBindableControls()
        {
            scanner.SetWorkspaceTarget(rootObject);
            Assert.IsFalse(scanner.WorkspaceCanGenerate);
        }

        /// <summary>Automation differentiates deleted accessors from safe additions.</summary>
        [Test]
        public void PendingRemovalIsRecognizedByAutomation()
        {
            Assert.IsTrue(AdvancedUIToolkitAutomationMonitor.IsPending(
                new UIAutoGenerationResult(UIAutoGenerationStatus.PendingRemoval, "Removed Fire")));
            Assert.IsTrue(AdvancedUIToolkitAutomationMonitor.IsPending(
                new UIAutoGenerationResult(UIAutoGenerationStatus.SafeChange, "Added Load")));
            Assert.IsFalse(AdvancedUIToolkitAutomationMonitor.IsPending(
                new UIAutoGenerationResult(UIAutoGenerationStatus.Blocked, "Invalid")));
        }

        /// <summary>Creates a normal uGUI button and adds it to the saved manifest.</summary>
        /// <param name="key">Generated property name.</param>
        /// <returns>The button GameObject for simulated deletion.</returns>
        private GameObject AddRegisteredButton(string key)
        {
            GameObject buttonObject = new GameObject("BTN_" + key,
                typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(rootObject.transform, false);
            UIBindingRoot root = rootObject.GetComponent<UIBindingRoot>();
            List<UIBindingRoot.Entry> entries = new List<UIBindingRoot.Entry>(root.EditorGetEntries());
            entries.Add(new UIBindingRoot.Entry
            {
                Key = key,
                HierarchyKey = key,
                StableId = Guid.NewGuid().ToString("N"),
                ComponentType = typeof(Button).FullName,
                Reference = buttonObject.GetComponent<Button>()
            });
            root.EditorSetEntries(entries);
            return buttonObject;
        }
    }
}
