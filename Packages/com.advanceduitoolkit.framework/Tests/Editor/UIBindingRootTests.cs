using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Tests
{
    /// <summary>Defines the ui binding root tests class.</summary>
    public class UIBindingRootTests
    {
        /// <summary>Gets returns registered component.</summary>
        [Test]
        public void Get_ReturnsRegisteredComponent()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform), typeof(UIBindingRoot));
            var buttonObject = new GameObject("BTN_Arm", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                buttonObject.transform.SetParent(rootObject.transform, false);
                var root = rootObject.GetComponent<UIBindingRoot>();
                var button = buttonObject.GetComponent<Button>();
                root.EditorSetEntries(new List<UIBindingRoot.Entry>
                {
                    new UIBindingRoot.Entry
                    {
                        Key = "Arm", HierarchyKey = "Arm", StableId = Guid.NewGuid().ToString("N"),
                        ComponentType = typeof(Button).FullName, Reference = button
                    }
                });
                Assert.AreSame(button, root.Get<Button>("Arm"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        /// <summary>Gets throws for unknown key.</summary>
        [Test]
        public void Get_ThrowsForUnknownKey()
        {
            var rootObject = new GameObject("Root", typeof(UIBindingRoot));
            try
            {
                var root = rootObject.GetComponent<UIBindingRoot>();
                Assert.Throws<KeyNotFoundException>(() => root.Get<Button>("DoesNotExist"));
            }
            finally { UnityEngine.Object.DestroyImmediate(rootObject); }
        }

        /// <summary>Performs the updating manifest refreshes cached lookup operation.</summary>
        [Test]
        public void UpdatingManifest_RefreshesCachedLookup()
        {
            var rootObject = new GameObject("Root", typeof(UIBindingRoot));
            var buttonObject = new GameObject("BTN_Arm", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                var root = rootObject.GetComponent<UIBindingRoot>();
                var button = buttonObject.GetComponent<Button>();
                root.EditorSetEntries(new List<UIBindingRoot.Entry>());
                Assert.Throws<KeyNotFoundException>(() => root.Get<Button>("Arm"));
                root.EditorSetEntries(new List<UIBindingRoot.Entry>
                {
                    new UIBindingRoot.Entry { Key = "Arm", Reference = button }
                });
                Assert.AreSame(button, root.Get<Button>("Arm"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(buttonObject);
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }
    }
}
