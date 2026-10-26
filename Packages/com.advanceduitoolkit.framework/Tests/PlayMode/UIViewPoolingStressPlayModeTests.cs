using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Tests
{
    /// <summary>Verifies a pooled view does not duplicate its events across many activations.</summary>
    public sealed class UIViewPoolingStressPlayModeTests
    {
        /// <summary>Reuses a single view forty times with the same event subscriptions.</summary>
        /// <returns>A frame to await Unity object destruction.</returns>
        [UnityTest]
        public IEnumerator RepeatedPanelReuseMaintainsOneListener()
        {
            UIViewLifecycleProbe.ResetTotals();
            var owner = new GameObject("TestPanel", typeof(RectTransform),
                typeof(UIBindingRoot), typeof(Image), typeof(Button));
            owner.SetActive(false);
            var view = owner.AddComponent<UIViewLifecycleProbe>();
            var button = owner.GetComponent<Button>();
            const int cycles = 40;
            try
            {
                for (int index = 0; index < cycles; index++)
                {
                    owner.SetActive(true);
                    button.onClick.Invoke();
                    Assert.AreEqual(index + 1, view.ClickCalls);
                    owner.SetActive(false);
                    button.onClick.Invoke();
                    Assert.AreEqual(index + 1, view.ClickCalls);
                }
                Assert.AreEqual(1, view.InitializeCalls);
                Assert.AreEqual(1, view.InitializedCalls);
                Assert.AreEqual(cycles, view.EnabledCalls);
                Assert.AreEqual(cycles, view.DisabledCalls);
            }
            finally
            {
                Object.Destroy(owner);
            }
            yield return null;
            Assert.AreEqual(1, UIViewLifecycleProbe.DestroyedCalls);
            Assert.AreEqual(1, UIViewLifecycleProbe.DisposedCalls);
        }
    }
}
