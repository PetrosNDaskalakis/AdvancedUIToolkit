using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Tests
{
    /// <summary>Tests cached reference handling with larger manifests.</summary>
    public sealed class UIBindingRegistryStressTests
    {
        /// <summary>Checks that references survive deserialization and manifest replacement.</summary>
        [Test]
        public void MultipleReferencesSurviveDeserializationAndManifestReplacement()
        {
            const int count = 128;
            var owner = new GameObject("Root", typeof(RectTransform), typeof(UIBindingRoot));
            try
            {
                var root = owner.GetComponent<UIBindingRoot>();
                var references = new List<UIBindingRoot.Entry>(count);
                for (int index = 0; index < count; index++)
                {
                    var child = new GameObject("BTN_" + index, typeof(RectTransform),
                        typeof(Image), typeof(Button));
                    child.transform.SetParent(owner.transform, false);
                    references.Add(new UIBindingRoot.Entry
                    {
                        Key = "Button" + index,
                        StableId = "test-" + index,
                        HierarchyKey = "Button" + index,
                        ComponentType = typeof(Button).FullName,
                        Reference = child.GetComponent<Button>()
                    });
                }
                root.EditorSetEntries(references);
                for (int index = 0; index < count; index++)
                    Assert.AreSame(references[index].Reference, root.Get<Button>("Button" + index));
                root.OnAfterDeserialize();
                for (int index = 0; index < count; index++)
                    Assert.AreSame(references[index].Reference, root.Get<Button>("Button" + index));
                root.EditorSetEntries(new List<UIBindingRoot.Entry> { references[count - 1] });
                Assert.IsFalse(root.TryGet<Button>("Button0", out _));
                Assert.AreSame(references[count - 1].Reference, root.Get<Button>("Button" + (count - 1)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        /// <summary>Checks missing, destroyed and wrong-type references.</summary>
        [Test]
        public void TryGetHandlesMissingDestroyedAndWrongTypeWithoutPoisoningCache()
        {
            var owner = new GameObject("Root", typeof(RectTransform), typeof(UIBindingRoot));
            var child = new GameObject("BTN_Fire", typeof(RectTransform), typeof(Image), typeof(Button));
            child.transform.SetParent(owner.transform, false);
            try
            {
                var root = owner.GetComponent<UIBindingRoot>();
                Button button = child.GetComponent<Button>();
                root.EditorSetEntries(new List<UIBindingRoot.Entry>
                {
                    new UIBindingRoot.Entry { Key = "Fire", Reference = button }
                });
                Assert.IsFalse(root.TryGet<Slider>("Fire", out _));
                Assert.IsFalse(root.TryGet<Button>("Missing", out _));
                Assert.IsFalse(root.TryGet<Button>(null, out _));
                Assert.AreSame(button, root.Get<Button>("Fire"));
                UnityEngine.Object.DestroyImmediate(child);
                Assert.IsFalse(root.TryGet<Button>("Fire", out _));
                Assert.Throws<KeyNotFoundException>(() => root.Get<Button>("Fire"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }
    }
}
