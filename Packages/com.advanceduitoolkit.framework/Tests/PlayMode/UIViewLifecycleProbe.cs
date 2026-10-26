using AdvancedUIToolkit;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Tests
{
    /// <summary>Trivial generated-binding substitute used only in the lifecycle tests.</summary>
    public sealed class UIViewLifecycleTestBindings : IUIBindings
    {
        /// <summary>Accepts the test binding root without requiring generated UI accessors.</summary>
        /// <param name="root">Root used by UIView during initialization.</param>
        public void Initialize(UIBindingRoot root)
        {
            // A real generated class would read its serialized component manifest here.
        }
    }

    /// <summary>Tracks UIView lifecycle notifications and click callbacks in Play Mode.</summary>
    public sealed class UIViewLifecycleProbe : UIView<UIViewLifecycleTestBindings>
    {
        /// <summary>Number of initializations observed by this instance.</summary>
        public int InitializeCalls { get; private set; }

        /// <summary>Number of post-initialization notifications.</summary>
        public int InitializedCalls { get; private set; }

        /// <summary>Number of enable notifications.</summary>
        public int EnabledCalls { get; private set; }

        /// <summary>Number of disable notifications.</summary>
        public int DisabledCalls { get; private set; }

        /// <summary>Number of click events delivered while active.</summary>
        public int ClickCalls { get; private set; }

        /// <summary>Count of destruction callbacks across test instances.</summary>
        public static int DestroyedCalls { get; private set; }

        /// <summary>Count of legacy disposal callbacks across test instances.</summary>
        public static int DisposedCalls { get; private set; }

        /// <summary>Resets counters shared between test instances.</summary>
        public static void ResetTotals()
        {
            DestroyedCalls = 0;
            DisposedCalls = 0;
        }

        /// <summary>Registers the test button exactly once through the scope.</summary>
        protected override void OnInitialize()
        {
            InitializeCalls++;
            Events.BindClick(GetComponent<Button>(), OnClicked);
        }

        /// <summary>Records completion of the one-time initialization.</summary>
        protected override void OnViewInitialized() => InitializedCalls++;

        /// <summary>Records each activation.</summary>
        protected override void OnViewEnabled() => EnabledCalls++;

        /// <summary>Records each deactivation.</summary>
        protected override void OnViewDisabled() => DisabledCalls++;

        /// <summary>Records destruction.</summary>
        protected override void OnViewDestroyed() => DestroyedCalls++;

        /// <summary>Records the legacy disposal hook.</summary>
        protected override void OnDispose() => DisposedCalls++;

        /// <summary>Records a delivered button click.</summary>
        private void OnClicked() => ClickCalls++;
    }
}
