using System;
using System.Collections.Generic;
using NUnit.Framework;
using AdvancedUIToolkit.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Tests
{
    /// <summary>
    /// Prevents automatic manifest updates from interfering with Unity's Undo history.
    /// Generated C# is not undoable, so the derived manifest must not create another
    /// Undo step between a control deletion and the user's Ctrl+Z restore.
    /// </summary>
    public sealed class AdvancedUIToolkitUndoRestoreTests
    {
        /// <summary>Root created for each isolated Undo regression.</summary>
        private GameObject rootObject;

        /// <summary>Current root's manifest component.</summary>
        private UIBindingRoot root;

        /// <summary>Temporary UI AutoBinder scanner.</summary>
        private UIAutoBinderWindow scanner;

        /// <summary>Builds a temporary root without registering construction on the Undo stack.</summary>
        [SetUp]
        public void SetUp()
        {
            rootObject = new GameObject("WeaponPanel", typeof(RectTransform), typeof(UIBindingRoot));
            root = rootObject.GetComponent<UIBindingRoot>();
            scanner = ScriptableObject.CreateInstance<UIAutoBinderWindow>();
        }

        /// <summary>Releases all temporary objects after the test.</summary>
        [TearDown]
        public void TearDown()
        {
            if (scanner != null) UnityEngine.Object.DestroyImmediate(scanner);
            if (rootObject != null) UnityEngine.Object.DestroyImmediate(rootObject);
        }

        /// <summary>
        /// Verifies that after deleting the last bound control, saving an empty derived
        /// manifest does not steal Ctrl+Z, and Undo restores the control as a new binding.
        /// </summary>
        [Test]
        public void AutomaticManifestPersistenceDoesNotStealDeleteUndo()
        {
            GameObject button = new GameObject("BTN_Fire", typeof(RectTransform),
                typeof(Image), typeof(Button));
            button.transform.SetParent(rootObject.transform, false);
            root.EditorSetEntries(new List<UIBindingRoot.Entry>
            {
                new UIBindingRoot.Entry
                {
                    Key = "Fire",
                    HierarchyKey = "Fire",
                    StableId = Guid.NewGuid().ToString("N"),
                    ComponentType = typeof(Button).FullName,
                    Reference = button.GetComponent<Button>()
                }
            });

            // Mimic deletion through the Unity Hierarchy, where Undo is expected to
            // restore the prefab/control and not an automatically generated manifest.
            Undo.IncrementCurrentGroup();
            Undo.DestroyObjectImmediate(button);
            Undo.IncrementCurrentGroup();

            UIAutoBinderWindow.PersistDerivedManifest(rootObject, root,
                new List<UIBindingRoot.Entry>(), "WeaponPanelBindings", "Game.UI.Generated",
                "Assets/AdvancedUIToolkit/Generated", string.Empty);
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();

            Transform restored = rootObject.transform.Find("BTN_Fire");
            Assert.IsNotNull(restored,
                "Ctrl+Z must restore the deleted UI control rather than undoing an automatic manifest update.");
            Assert.AreEqual(0, root.EditorGetEntries().Count,
                "The derived manifest must remain at its last generated state until automation rescans.");

            scanner.SetWorkspaceTarget(rootObject);
            Assert.AreEqual(1, scanner.WorkspaceChangeCount,
                "A restored component must be discovered as an addition and be eligible for regeneration.");
            Assert.IsTrue(scanner.WorkspaceCanGenerate);
            Assert.AreEqual("Fire", scanner.GetWorkspaceBindings()[0].ApiKey);
        }

        /// <summary>Checks that derived metadata changes remain persisted in the component.</summary>
        [Test]
        public void DerivedManifestPersistenceRetainsGenerationProfile()
        {
            UIAutoBinderWindow.PersistDerivedManifest(rootObject, root,
                new List<UIBindingRoot.Entry>(), "WeaponPanelBindings", "Game.UI.Generated",
                "Assets/AdvancedUIToolkit/Generated", string.Empty);

            Assert.IsTrue(root.EditorHasGenerationProfile);
            root.EditorGetGenerationProfile(out string className, out string namespaceName,
                out string outputFolder);
            Assert.AreEqual("WeaponPanelBindings", className);
            Assert.AreEqual("Game.UI.Generated", namespaceName);
            Assert.AreEqual("Assets/AdvancedUIToolkit/Generated", outputFolder);
        }
    }
}
