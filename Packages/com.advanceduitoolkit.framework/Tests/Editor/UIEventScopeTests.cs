using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Tests
{
    /// <summary>Defines the ui event scope tests class.</summary>
    public class UIEventScopeTests
    {
        /// <summary>Performs the dispose removes click listener and can be called twice operation.</summary>
        [Test]
        public void Dispose_RemovesClickListener_AndCanBeCalledTwice()
        {
            var buttonObject = new GameObject("BTN_Test", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                var button = buttonObject.GetComponent<Button>();
                int clicks = 0;
                var scope = new UIEventScope();
                scope.BindClick(button, () => clicks++);
                button.onClick.Invoke();
                Assert.AreEqual(1, clicks);
                scope.Dispose();
                scope.Dispose();
                button.onClick.Invoke();
                Assert.AreEqual(1, clicks);
            }
            finally { UnityEngine.Object.DestroyImmediate(buttonObject); }
        }
    }
}
