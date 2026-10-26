
// using UnityEngine;
// using AdvancedUIToolkit;
// using Game.UI.Generated;

// /// <summary>
// /// Tests UI Core lifecycle management.
// /// </summary>
// public sealed class WeaponPanel : UIView<WeaponPanelBindings>
// {
//     /// <summary>
//     /// Registers UI events once.
//     /// </summary>
//     protected override void OnInitialize()
//     {
//         Events.BindClick(UI.Arm, OnArm);
//         Events.BindClick(UI.Fire, OnFire);
//         Events.BindClick(UI.Load, OnLoad);
//     }

//     /// <summary>
//     /// Called whenever the panel becomes active.
//     /// </summary>
//     protected override void OnViewEnabled()
//     {
//         Debug.Log("[WeaponPanel] Enabled!");
//     }

//     /// <summary>
//     /// Called whenever the panel is disabled.
//     /// </summary>
//     protected override void OnViewDisabled()
//     {
//         Debug.Log("[WeaponPanel] Disabled!");
//     }

//     /// <summary>
//     /// Called before the panel is destroyed.
//     /// </summary>
//     protected override void OnViewDestroyed()
//     {
//         Debug.Log("[WeaponPanel] Destroyed!");
//     }

//     /// <summary>Handles Arm button clicks.</summary>
//     private void OnArm() => Debug.Log("Armed!");

//     /// <summary>Handles Fire button clicks.</summary>
//     private void OnFire() => Debug.Log("Fired!");

//     /// <summary>Handles Load button clicks.</summary>
//     private void OnLoad() => Debug.Log("Loaded!");
// }
