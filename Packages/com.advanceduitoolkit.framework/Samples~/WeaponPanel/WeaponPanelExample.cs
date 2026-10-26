using AdvancedUIToolkit;
using UnityEngine;

namespace AdvancedUIToolkit.Samples
{
    /// <summary>Demonstrates automatic button references and managed event cleanup.</summary>
    public sealed class WeaponPanelExample : UIView<ScreenBindingsSample>
    {
        /// <summary>Registers the panel's three button callbacks once.</summary>
        protected override void OnInitialize()
        {
            Events.BindClick(UI.Arm, OnArm);
            Events.BindClick(UI.Fire, OnFire);
            Events.BindClick(UI.Load, OnLoad);
        }

        /// <summary>Logs the Arm button interaction.</summary>
        private void OnArm() => Debug.Log("[Advanced UI Toolkit Sample] Arm clicked", this);

        /// <summary>Logs the Fire button interaction.</summary>
        private void OnFire() => Debug.Log("[Advanced UI Toolkit Sample] Fire clicked", this);

        /// <summary>Logs the Load button interaction.</summary>
        private void OnLoad() => Debug.Log("[Advanced UI Toolkit Sample] Load clicked", this);
    }
}
