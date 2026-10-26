using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Tests
{
    /// <summary>Tests event listener stability across repeated pooling cycles.</summary>
    public sealed class UIEventScopeStressTests
    {
        /// <summary>Verifies repeated suspension and resumption do not duplicate callbacks.</summary>
        [Test]
        public void RepeatedPoolingCyclesProduceExactlyOneCallbackPerActivation()
        {
            var owner = new GameObject("BTN_Test", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                var button = owner.GetComponent<Button>();
                using (var scope = new UIEventScope())
                {
                    int calls = 0;
                    scope.BindClick(button, () => calls++);
                    for (int index = 0; index < 200; index++)
                    {
                        scope.Suspend();
                        button.onClick.Invoke();
                        Assert.AreEqual(index, calls);
                        scope.Resume();
                        scope.Resume();
                        button.onClick.Invoke();
                        Assert.AreEqual(index + 1, calls);
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        /// <summary>Verifies large subscriptions registered while suspended activate once.</summary>
        [Test]
        public void ManyListenersAddedWhileSuspendedAttachOnceAndDetachOnDispose()
        {
            var owner = new GameObject("BTN_Test", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                var button = owner.GetComponent<Button>();
                var scope = new UIEventScope();
                int calls = 0;
                scope.Suspend();
                for (int index = 0; index < 32; index++)
                    scope.BindClick(button, () => calls++);
                button.onClick.Invoke();
                Assert.AreEqual(0, calls);
                scope.Resume();
                button.onClick.Invoke();
                Assert.AreEqual(32, calls);
                scope.Suspend();
                scope.Resume();
                button.onClick.Invoke();
                Assert.AreEqual(64, calls);
                scope.Dispose();
                scope.Dispose();
                button.onClick.Invoke();
                Assert.AreEqual(64, calls);
                Assert.Throws<ObjectDisposedException>(() => scope.Resume());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }
    }
}
