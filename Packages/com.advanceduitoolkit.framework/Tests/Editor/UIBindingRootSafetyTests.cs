using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Tests
{
    /// <summary>Verifies safe lookup and cache invalidation in serialized UI roots.</summary>
    public sealed class UIBindingRootSafetyTests
    {
        /// <summary>Checks missing keys and incompatible types return false.</summary>
        [Test]
        public void TryGetHandlesUnknownNullAndWrongType()
        {
            var rootObject = new GameObject("Root", typeof(UIBindingRoot));
            var buttonObject = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                var root = rootObject.GetComponent<UIBindingRoot>();
                root.EditorSetEntries(new List<UIBindingRoot.Entry>
                {
                    new UIBindingRoot.Entry { Key = "Start", Reference = buttonObject.GetComponent<Button>() }
                });
                Assert.IsTrue(root.TryGet("Start", out Button found));
                Assert.AreSame(buttonObject.GetComponent<Button>(), found);
                Assert.IsFalse(root.TryGet("Start", out Slider missingSlider));
                Assert.IsNull(missingSlider);
                Assert.IsFalse(root.TryGet("Missing", out Button missingButton));
                Assert.IsNull(missingButton);
                Assert.IsFalse(root.TryGet(null, out Button nullKeyResult));
                Assert.IsNull(nullKeyResult);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(buttonObject);
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        /// <summary>Confirms restoring the manifest forces a fresh cached lookup.</summary>
        [Test]
        public void DeserializationInvalidatesLookup()
        {
            var rootObject = new GameObject("Root", typeof(UIBindingRoot));
            var first = new GameObject("First", typeof(RectTransform), typeof(Image), typeof(Button));
            var second = new GameObject("Second", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                var root = rootObject.GetComponent<UIBindingRoot>();
                root.EditorSetEntries(new List<UIBindingRoot.Entry>
                {
                    new UIBindingRoot.Entry { Key = "Action", Reference = first.GetComponent<Button>() }
                });
                Assert.AreSame(first.GetComponent<Button>(), root.Get<Button>("Action"));
                var saved = new List<UIBindingRoot.Entry>
                {
                    new UIBindingRoot.Entry { Key = "Action", Reference = second.GetComponent<Button>() }
                };
                // Mimics serialized data replacing the old manifest without EditorSetEntries.
                var field = typeof(UIBindingRoot).GetField("entries",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(field);
                field.SetValue(root, saved);
                root.OnAfterDeserialize();
                Assert.AreSame(second.GetComponent<Button>(), root.Get<Button>("Action"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }
    }
}
