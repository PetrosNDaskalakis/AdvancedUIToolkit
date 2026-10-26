using System;
using AdvancedUIToolkit;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Samples.Settings
{
    /// <summary>
    /// Example settings bindings using the same contract as generated Advanced UI Toolkit classes.
    /// This class exists only to keep the sample compile-ready before scanning a screen.
    /// </summary>
    public sealed class SettingsPanelBindingsSample : IUIBindings
    {
        /// <summary>Volume slider on the settings panel.</summary>
        public Slider Volume { get; private set; }

        /// <summary>Mute toggle on the settings panel.</summary>
        public Toggle Mute { get; private set; }

        /// <summary>Legacy input field for the player name.</summary>
        public InputField PlayerName { get; private set; }

        /// <summary>Legacy dropdown for graphics quality.</summary>
        public Dropdown Quality { get; private set; }

        /// <summary>Legacy status text used for feedback.</summary>
        public Text Status { get; private set; }

        /// <summary>Resolves the demo control references once from the saved root manifest.</summary>
        /// <param name="root">The serialized binding root on SettingsPanel.</param>
        public void Initialize(UIBindingRoot root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            Volume = root.Get<Slider>("Volume");
            Mute = root.Get<Toggle>("Mute");
            PlayerName = root.Get<InputField>("PlayerName");
            Quality = root.Get<Dropdown>("Quality");
            Status = root.Get<Text>("Status");
        }
    }
}
