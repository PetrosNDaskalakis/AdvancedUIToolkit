using NUnit.Framework;
using UnityEditor;
using AdvancedUIToolkit.Editor;

namespace AdvancedUIToolkit.Tests
{
    /// <summary>Verifies deferred code generation preferences without generating C# files.</summary>
    public sealed class AdvancedUIToolkitGenerationModeTests
    {
        /// <summary>Ensures selecting Manual and Automatic persists the requested mode.</summary>
        [Test]
        public void GenerationMode_SavesAndRestoresMode()
        {
            AdvancedUIToolkitGenerationMode oldMode = AdvancedUIToolkitAutomationMonitor.GenerationMode;
            bool oldWatch = AdvancedUIToolkitAutomationMonitor.WatchEnabled;
            try
            {
                AdvancedUIToolkitAutomationMonitor.WatchEnabled = false;

                AdvancedUIToolkitAutomationMonitor.GenerationMode = AdvancedUIToolkitGenerationMode.Manual;
                Assert.AreEqual(AdvancedUIToolkitGenerationMode.Manual,
                    AdvancedUIToolkitAutomationMonitor.GenerationMode);
                Assert.IsFalse(AdvancedUIToolkitAutomationMonitor.AutoGenerateEnabled);

                AdvancedUIToolkitAutomationMonitor.GenerationMode = AdvancedUIToolkitGenerationMode.Automatic;
                Assert.AreEqual(AdvancedUIToolkitGenerationMode.Automatic,
                    AdvancedUIToolkitAutomationMonitor.GenerationMode);
                Assert.IsTrue(AdvancedUIToolkitAutomationMonitor.AutoGenerateEnabled);
            }
            finally
            {
                AdvancedUIToolkitAutomationMonitor.GenerationMode = oldMode;
                AdvancedUIToolkitAutomationMonitor.WatchEnabled = oldWatch;
            }
        }

    }
}
