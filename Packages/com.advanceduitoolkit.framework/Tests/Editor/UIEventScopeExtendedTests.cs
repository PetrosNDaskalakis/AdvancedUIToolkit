using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Tests
{
    /// <summary>Verifies every uGUI event binding releases its registered listener.</summary>
    public sealed class UIEventScopeExtendedTests
    {
        /// <summary>Creates event sources, invokes callbacks, then proves disposal detaches them.</summary>
        [Test]
        public void DisposeDetachesToggleSliderScrollbarInputAndDropdownListeners()
        {
            var toggleObject = new GameObject("Toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
            var sliderObject = new GameObject("Slider", typeof(RectTransform), typeof(Image), typeof(Slider));
            var scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            var inputObject = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(InputField));
            var dropdownObject = new GameObject("Dropdown", typeof(RectTransform), typeof(Image), typeof(Dropdown));
            try
            {
                var scope = new UIEventScope();
                int calls = 0;
                var toggle = toggleObject.GetComponent<Toggle>();
                var slider = sliderObject.GetComponent<Slider>();
                var scrollbar = scrollbarObject.GetComponent<Scrollbar>();
                var input = inputObject.GetComponent<InputField>();
                var dropdown = dropdownObject.GetComponent<Dropdown>();

                scope.BindValueChanged(toggle, value => calls++);
                scope.BindValueChanged(slider, value => calls++);
                scope.BindValueChanged(scrollbar, value => calls++);
                scope.BindValueChanged(input, value => calls++);
                scope.BindEndEdit(input, value => calls++);
                scope.BindValueChanged(dropdown, value => calls++);

                toggle.onValueChanged.Invoke(true);
                slider.onValueChanged.Invoke(0.5f);
                scrollbar.onValueChanged.Invoke(0.3f);
                input.onValueChanged.Invoke("hello");
                input.onEndEdit.Invoke("hello");
                dropdown.onValueChanged.Invoke(1);
                Assert.AreEqual(6, calls);

                scope.Dispose();
                toggle.onValueChanged.Invoke(false);
                slider.onValueChanged.Invoke(0.75f);
                scrollbar.onValueChanged.Invoke(0.7f);
                input.onValueChanged.Invoke("changed");
                input.onEndEdit.Invoke("changed");
                dropdown.onValueChanged.Invoke(0);
                Assert.AreEqual(6, calls);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(toggleObject);
                UnityEngine.Object.DestroyImmediate(sliderObject);
                UnityEngine.Object.DestroyImmediate(scrollbarObject);
                UnityEngine.Object.DestroyImmediate(inputObject);
                UnityEngine.Object.DestroyImmediate(dropdownObject);
            }
        }

        /// <summary>Confirms a disposed scope rejects new event listeners.</summary>
        [Test]
        public void DisposedScopeRejectsFurtherSubscriptions()
        {
            var go = new GameObject("Button", typeof(Button));
            try
            {
                var scope = new UIEventScope();
                scope.Dispose();
                Assert.Throws<ObjectDisposedException>(() =>
                    scope.BindClick(go.GetComponent<Button>(), () => { }));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
