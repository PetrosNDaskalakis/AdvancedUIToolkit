using AdvancedUIToolkit;
using UnityEngine;

namespace AdvancedUIToolkit.Samples.Settings
{
    /// <summary>Demonstrates managed callbacks for uGUI settings controls.</summary>
    public sealed class SettingsPanelExample : UIView<SettingsPanelBindingsSample>
    {
        /// <summary>Tracks the number of UI interactions for debugging.</summary>
        private int interactionCount;

        /// <summary>Registers all input handlers once after references are resolved.</summary>
        protected override void OnInitialize()
        {
            Events.BindValueChanged(UI.Volume, OnVolumeChanged);
            Events.BindValueChanged(UI.Mute, OnMuteChanged);
            Events.BindValueChanged(UI.PlayerName, OnPlayerNameChanged);
            Events.BindValueChanged(UI.Quality, OnQualityChanged);
            SetStatus("Settings ready");
        }

        /// <summary>Handles changes to volume.</summary>
        /// <param name="value">Current slider value from the component.</param>
        private void OnVolumeChanged(float value)
        {
            SetStatus($"Volume: {value:0.00}");
        }

        /// <summary>Handles changes to the Mute checkbox.</summary>
        /// <param name="muted">Whether the Mute toggle is enabled.</param>
        private void OnMuteChanged(bool muted)
        {
            SetStatus(muted ? "Audio muted" : "Audio unmuted");
        }

        /// <summary>Handles edits to the player's name.</summary>
        /// <param name="playerName">Name currently entered in the input field.</param>
        private void OnPlayerNameChanged(string playerName)
        {
            SetStatus("Player: " + playerName);
        }

        /// <summary>Handles dropdown selection changes.</summary>
        /// <param name="selectedIndex">Zero-based index of the selected option.</param>
        private void OnQualityChanged(int selectedIndex)
        {
            string quality = selectedIndex >= 0 && selectedIndex < UI.Quality.options.Count
                ? UI.Quality.options[selectedIndex].text
                : "Unknown";
            SetStatus("Quality: " + quality);
        }

        /// <summary>Sets a visible status message and logs it for debugging.</summary>
        /// <param name="message">Message describing the latest user interaction.</param>
        private void SetStatus(string message)
        {
            interactionCount++;
            UI.Status.text = message;
            Debug.Log($"[Advanced UI Toolkit Settings Sample] #{interactionCount}: {message}", this);
        }
    }
}
