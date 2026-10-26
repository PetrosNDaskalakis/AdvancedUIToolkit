using System;
using UnityEngine;

namespace AdvancedUIToolkit
{
    /// <summary>
    /// Base MonoBehaviour for UI screens with AutoBinder-generated references.
    /// Initializes bindings once and safely suspends UI event subscriptions when disabled.
    /// Keep Unity lifecycle messages in this base class; override its view hooks instead.
    /// </summary>
    /// <typeparam name="TBindings">A generated class implementing IUIBindings.</typeparam>
    public abstract class UIView<TBindings> : MonoBehaviour
        where TBindings : class, IUIBindings, new()
    {
        /// <summary>Strongly typed UI references initialized for this view.</summary>
        protected TBindings UI { get; private set; }

        /// <summary>Event subscriptions suspended while the view is disabled.</summary>
        protected UIEventScope Events { get; private set; }

        /// <summary>Whether initialization completed successfully.</summary>
        private bool initialized;

        /// <summary>Whether the view has received an active enable notification.</summary>
        private bool viewEnabled;

        /// <summary>
        /// Creates references and registers events once; the scope starts suspended
        /// until Unity sends OnEnable.
        /// </summary>
        private void Awake()
        {
            var root = GetComponent<UIBindingRoot>();
            if (root == null)
            {
                Debug.LogError("Advanced UI Toolkit: Missing UIBindingRoot on " + name + ".", this);
                enabled = false;
                return;
            }

            try
            {
                UI = new TBindings();
                UI.Initialize(root);

                Events = new UIEventScope();
                Events.Suspend();

                // Preserved from v0.9.2: OnInitialize executes only once.
                OnInitialize();
                initialized = true;
                OnViewInitialized();
            }
            catch (Exception exception)
            {
                Events?.Dispose();
                Events = null;
                UI = null;
                initialized = false;
                Debug.LogException(exception, this);
                enabled = false;
            }
        }

        /// <summary>Resumes event delivery and invokes the view's enable hook.</summary>
        private void OnEnable()
        {
            if (!initialized || viewEnabled)
                return;

            try
            {
                Events?.Resume();
                viewEnabled = true;
                OnViewEnabled();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                enabled = false;
                // OnDisable handles suspension if Unity issues it for this transition.
            }
        }

        /// <summary>Suspends event delivery and invokes the view's disable hook.</summary>
        private void OnDisable()
        {
            if (!initialized)
                return;

            Events?.Suspend();
            if (!viewEnabled)
                return;

            viewEnabled = false;
            try
            {
                OnViewDisabled();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        /// <summary>
        /// Tears down the view exactly once. Retains the previous OnDispose hook
        /// and releases event subscriptions even if a custom hook throws.
        /// </summary>
        private void OnDestroy()
        {
            // OnDisable normally precedes OnDestroy, but explicit cleanup remains safe.
            Events?.Suspend();
            if (viewEnabled)
            {
                viewEnabled = false;
                try { OnViewDisabled(); }
                catch (Exception exception) { Debug.LogException(exception, this); }
            }

            if (initialized)
            {
                try { OnViewDestroyed(); }
                catch (Exception exception) { Debug.LogException(exception, this); }
            }

            try
            {
                Events?.Dispose();
            }
            finally
            {
                Events = null;
                try { OnDispose(); }
                catch (Exception exception) { Debug.LogException(exception, this); }
                UI = null;
                initialized = false;
            }
        }

        /// <summary>
        /// Called once after binding references are resolved. Bind UI events here using Events.
        /// Existing v0.9.2 subclasses continue overriding this method.
        /// </summary>
        protected abstract void OnInitialize();

        /// <summary>Called once after OnInitialize finishes successfully.</summary>
        protected virtual void OnViewInitialized() { }

        /// <summary>Called each time the initialized view becomes enabled.</summary>
        protected virtual void OnViewEnabled() { }

        /// <summary>Called each time an enabled view is disabled or its GameObject is deactivated.</summary>
        protected virtual void OnViewDisabled() { }

        /// <summary>Called once when an initialized view is destroyed, before OnDispose.</summary>
        protected virtual void OnViewDestroyed() { }

        /// <summary>Legacy teardown hook preserved for compatibility with v0.9.2 subclasses.</summary>
        protected virtual void OnDispose() { }
    }
}
