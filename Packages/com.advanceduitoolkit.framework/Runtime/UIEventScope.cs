using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AdvancedUIToolkit
{
    /// <summary>
    /// Owns uGUI event subscriptions and supports temporary suspension for pooled views.
    /// An independent scope starts active; UIView creates a suspended scope and resumes it on enable.
    /// </summary>
    public sealed class UIEventScope : IDisposable
    {
        /// <summary>Represents a single event subscription that can be attached and detached repeatedly.</summary>
        private sealed class Subscription
        {
            /// <summary>Registers the callback with its event source.</summary>
            public readonly Action Attach;

            /// <summary>Removes the callback from its event source.</summary>
            public readonly Action Detach;

            /// <summary>Indicates whether the event listener is currently registered.</summary>
            public bool IsAttached;

            /// <summary>Initializes a reversible subscription.</summary>
            /// <param name="attach">Action that registers the listener.</param>
            /// <param name="detach">Action that removes the listener.</param>
            public Subscription(Action attach, Action detach)
            {
                Attach = attach;
                Detach = detach;
            }
        }

        /// <summary>Listeners owned by this scope, in registration order.</summary>
        private readonly List<Subscription> subscriptions = new List<Subscription>();

        /// <summary>Indicates whether this scope has permanently released its listeners.</summary>
        private bool disposed;

        /// <summary>Whether event delivery is currently suspended.</summary>
        public bool IsSuspended { get; private set; }

        /// <summary>Whether this scope has been disposed permanently.</summary>
        public bool IsDisposed => disposed;

        /// <summary>Subscribes to a button click and tracks its cleanup.</summary>
        /// <param name="button">Button to observe.</param>
        /// <param name="listener">Callback invoked when clicked.</param>
        public void BindClick(Button button, UnityAction listener)
        {
            EnsureValid(button, listener);
            Register(() => button.onClick.AddListener(listener),
                () => { if (button != null) button.onClick.RemoveListener(listener); });
        }

        /// <summary>Subscribes to toggle state changes and tracks cleanup.</summary>
        /// <param name="toggle">Toggle to observe.</param>
        /// <param name="listener">Callback receiving the current toggle state.</param>
        public void BindValueChanged(Toggle toggle, UnityAction<bool> listener)
        {
            EnsureValid(toggle, listener);
            Register(() => toggle.onValueChanged.AddListener(listener),
                () => { if (toggle != null) toggle.onValueChanged.RemoveListener(listener); });
        }

        /// <summary>Subscribes to slider value changes and tracks cleanup.</summary>
        /// <param name="slider">Slider to observe.</param>
        /// <param name="listener">Callback receiving the slider value.</param>
        public void BindValueChanged(Slider slider, UnityAction<float> listener)
        {
            EnsureValid(slider, listener);
            Register(() => slider.onValueChanged.AddListener(listener),
                () => { if (slider != null) slider.onValueChanged.RemoveListener(listener); });
        }

        /// <summary>Subscribes to scrollbar value changes and tracks cleanup.</summary>
        /// <param name="scrollbar">Scrollbar to observe.</param>
        /// <param name="listener">Callback receiving the scrollbar value.</param>
        public void BindValueChanged(Scrollbar scrollbar, UnityAction<float> listener)
        {
            EnsureValid(scrollbar, listener);
            Register(() => scrollbar.onValueChanged.AddListener(listener),
                () => { if (scrollbar != null) scrollbar.onValueChanged.RemoveListener(listener); });
        }

        /// <summary>Subscribes to changes in a legacy input field.</summary>
        /// <param name="input">Legacy input field to observe.</param>
        /// <param name="listener">Callback receiving the edited text.</param>
        public void BindValueChanged(InputField input, UnityAction<string> listener)
        {
            EnsureValid(input, listener);
            Register(() => input.onValueChanged.AddListener(listener),
                () => { if (input != null) input.onValueChanged.RemoveListener(listener); });
        }

        /// <summary>Subscribes to the end-edit event of a legacy input field.</summary>
        /// <param name="input">Legacy input field to observe.</param>
        /// <param name="listener">Callback receiving the final input text.</param>
        public void BindEndEdit(InputField input, UnityAction<string> listener)
        {
            EnsureValid(input, listener);
            Register(() => input.onEndEdit.AddListener(listener),
                () => { if (input != null) input.onEndEdit.RemoveListener(listener); });
        }

        /// <summary>Subscribes to changes in a legacy dropdown selection.</summary>
        /// <param name="dropdown">Legacy dropdown to observe.</param>
        /// <param name="listener">Callback receiving the selected index.</param>
        public void BindValueChanged(Dropdown dropdown, UnityAction<int> listener)
        {
            EnsureValid(dropdown, listener);
            Register(() => dropdown.onValueChanged.AddListener(listener),
                () => { if (dropdown != null) dropdown.onValueChanged.RemoveListener(listener); });
        }

        /// <summary>
        /// Detaches all listeners without forgetting them. Calling this repeatedly is safe.
        /// New bindings registered while suspended are attached on the next Resume call.
        /// </summary>
        public void Suspend()
        {
            if (disposed || IsSuspended)
                return;

            IsSuspended = true;
            for (int i = subscriptions.Count - 1; i >= 0; --i)
                DetachSafely(subscriptions[i]);
        }

        /// <summary>
        /// Reattaches suspended listeners once. Calling this while already active does nothing.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The scope was disposed.</exception>
        public void Resume()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(UIEventScope));

            if (!IsSuspended)
                return;

            try
            {
                for (int i = 0; i < subscriptions.Count; ++i)
                    Attach(subscriptions[i]);

                IsSuspended = false;
            }
            catch
            {
                // Do not leave a partially resumed scope with duplicate subscriptions.
                for (int i = subscriptions.Count - 1; i >= 0; --i)
                    DetachSafely(subscriptions[i]);
                IsSuspended = true;
                throw;
            }
        }

        /// <summary>Detaches registered listeners and permanently releases them.</summary>
        public void Dispose()
        {
            if (disposed)
                return;

            Suspend();
            disposed = true;
            subscriptions.Clear();
        }

        /// <summary>Registers a reversible event subscription and attaches it if active.</summary>
        /// <param name="attach">Action that registers the listener.</param>
        /// <param name="detach">Action that removes the listener.</param>
        private void Register(Action attach, Action detach)
        {
            Subscription subscription = new Subscription(attach, detach);
            if (!IsSuspended)
                Attach(subscription);

            subscriptions.Add(subscription);
        }

        /// <summary>Attaches a subscription at most once.</summary>
        /// <param name="subscription">Subscription to attach.</param>
        private static void Attach(Subscription subscription)
        {
            if (subscription.IsAttached)
                return;

            subscription.Attach();
            subscription.IsAttached = true;
        }

        /// <summary>Attempts to detach a subscription, allowing cleanup to continue after an error.</summary>
        /// <param name="subscription">Subscription to detach.</param>
        private static void DetachSafely(Subscription subscription)
        {
            if (!subscription.IsAttached)
                return;

            try
            {
                subscription.Detach();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                subscription.IsAttached = false;
            }
        }

        /// <summary>Rejects invalid listener registrations.</summary>
        /// <param name="control">Unity UI control associated with the event.</param>
        /// <param name="listener">Listener to register.</param>
        /// <exception cref="ObjectDisposedException">The scope was disposed.</exception>
        /// <exception cref="ArgumentNullException">A control or listener is missing.</exception>
        private void EnsureValid(Component control, Delegate listener)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(UIEventScope));
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));
        }
    }
}
