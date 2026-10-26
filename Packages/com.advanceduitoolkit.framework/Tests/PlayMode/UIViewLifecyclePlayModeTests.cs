using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Tests
{
    /// <summary>Verifies Unity enable/disable and object pooling behavior in Play Mode.</summary>
    public sealed class UIViewLifecyclePlayModeTests
    {
        /// <summary>Tests view callbacks and listener suspension across repeated pooling cycles.</summary>
        /// <returns>A coroutine that waits for Unity destruction to complete.</returns>
        [UnityTest]
        public IEnumerator PooledViewInitializesOnceAndNeverDuplicatesClickHandlers()
        {
            UIViewLifecycleProbe.ResetTotals();
            var root = new GameObject("WeaponPanel", typeof(RectTransform),
                typeof(UIBindingRoot), typeof(Image), typeof(Button));
            root.SetActive(false);
            var view = root.AddComponent<UIViewLifecycleProbe>();
            var button = root.GetComponent<Button>();

            try
            {
                root.SetActive(true);
                Assert.AreEqual(1, view.InitializeCalls);
                Assert.AreEqual(1, view.InitializedCalls);
                Assert.AreEqual(1, view.EnabledCalls);
                button.onClick.Invoke();
                Assert.AreEqual(1, view.ClickCalls);

                root.SetActive(false);
                Assert.AreEqual(1, view.DisabledCalls);
                button.onClick.Invoke();
                Assert.AreEqual(1, view.ClickCalls);

                root.SetActive(true);
                Assert.AreEqual(2, view.EnabledCalls);
                Assert.AreEqual(1, view.InitializeCalls);
                button.onClick.Invoke();
                Assert.AreEqual(2, view.ClickCalls);

                view.enabled = false;
                Assert.AreEqual(2, view.DisabledCalls);
                button.onClick.Invoke();
                Assert.AreEqual(2, view.ClickCalls);

                view.enabled = true;
                Assert.AreEqual(3, view.EnabledCalls);
                button.onClick.Invoke();
                Assert.AreEqual(3, view.ClickCalls);
            }
            finally
            {
                Object.Destroy(root);
            }

            yield return null;
            Assert.AreEqual(1, UIViewLifecycleProbe.DestroyedCalls);
            Assert.AreEqual(1, UIViewLifecycleProbe.DisposedCalls);
        }
    }
}
