using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Tests
{
    /// <summary>Tests suspension, resumption and permanent cleanup of UIEventScope.</summary>
    public sealed class UIEventScopeLifecycleTests
    {
        /// <summary>Verifies temporary suspension does not duplicate existing callbacks.</summary>
        [Test]
        public void SuspendAndResumePreserveSingleSubscription()
        {
            var gameObject = new GameObject("BTN_Lifecycle", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                var button = gameObject.GetComponent<Button>();
                var scope = new UIEventScope();
                int calls = 0;
                scope.BindClick(button, () => calls++);

                button.onClick.Invoke();
                Assert.AreEqual(1, calls);

                scope.Suspend();
                scope.Suspend();
                Assert.IsTrue(scope.IsSuspended);
                button.onClick.Invoke();
                Assert.AreEqual(1, calls);

                scope.Resume();
                scope.Resume();
                Assert.IsFalse(scope.IsSuspended);
                button.onClick.Invoke();
                Assert.AreEqual(2, calls);

                scope.Suspend();
                scope.Resume();
                button.onClick.Invoke();
                Assert.AreEqual(3, calls);

                scope.Dispose();
                Assert.IsTrue(scope.IsDisposed);
                button.onClick.Invoke();
                Assert.AreEqual(3, calls);
                Assert.Throws<ObjectDisposedException>(() => scope.Resume());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>Ensures a listener bound while suspended activates on resume, not before.</summary>
        [Test]
        public void BindWhileSuspendedWaitsForResume()
        {
            var gameObject = new GameObject("BTN_Late", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                var button = gameObject.GetComponent<Button>();
                var scope = new UIEventScope();
                int calls = 0;
                scope.Suspend();
                scope.BindClick(button, () => calls++);
                button.onClick.Invoke();
                Assert.AreEqual(0, calls);

                scope.Resume();
                button.onClick.Invoke();
                Assert.AreEqual(1, calls);
                scope.Dispose();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>Verifies disposal of a suspended scope prevents later callbacks.</summary>
        [Test]
        public void DisposeWhileSuspendedIsPermanent()
        {
            var gameObject = new GameObject("BTN_Disposed", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                var button = gameObject.GetComponent<Button>();
                var scope = new UIEventScope();
                int calls = 0;
                scope.BindClick(button, () => calls++);
                scope.Suspend();
                scope.Dispose();
                scope.Dispose();
                Assert.IsTrue(scope.IsDisposed);
                Assert.Throws<ObjectDisposedException>(() => scope.BindClick(button, () => calls++));
                button.onClick.Invoke();
                Assert.AreEqual(0, calls);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}
