# Advanced UI Toolkit v0.9.3 — Lifecycle reference

This document describes the lifecycle of `UIView<TBindings>` and events managed by `UIEventScope`.

## Order of callbacks

On first activation of the object:

1. Unity `Awake` (Advanced UI Toolkit) resolves `UIBindingRoot`, initializes `UI` and creates a **suspended** `Events` scope.
2. `OnInitialize()` runs **once**; register listeners via `Events.BindClick` and other `Events` helpers here.
3. `OnViewInitialized()` runs **once** after the first hook.
4. Unity `OnEnable` resumes registered listeners and then invokes `OnViewEnabled()`.

On temporary disable/deactivation:

1. Unity `OnDisable` suspends all subscriptions registered with `Events`.
2. `OnViewDisabled()` runs once for the active-to-disabled transition.
3. The same view and typed `UI` references are retained for reactivation.

On reactivation:

1. Unity `OnEnable` resumes the **same** subscriptions, without registering duplicates.
2. `OnViewEnabled()` runs again. `OnInitialize()` and `OnViewInitialized()` do **not** rerun.

On destruction:

1. Event listeners are suspended (normally by `OnDisable`).
2. `OnViewDestroyed()` runs once if initialization succeeded.
3. The scope is disposed and the old `OnDispose()` hook runs.
4. The view clears its UI references.

## How to extend UIView

```csharp
using AdvancedUIToolkit;
using UnityEngine;
using Game.UI.Generated;

/// <summary>Example panel supporting object pooling.</summary>
public sealed class WeaponPanel : UIView<WeaponPanelBindings>
{
    /// <summary>Registers buttons just once.</summary>
    protected override void OnInitialize()
    {
        Events.BindClick(UI.Arm, OnArm);
        Events.BindClick(UI.Fire, OnFire);
    }

    /// <summary>Refreshes visible state after each activation.</summary>
    protected override void OnViewEnabled()
    {
        Debug.Log("Weapon panel shown.", this);
    }

    /// <summary>Stops non-UI temporary work when the panel is hidden.</summary>
    protected override void OnViewDisabled()
    {
        Debug.Log("Weapon panel hidden.", this);
    }

    /// <summary>Handles arm clicks.</summary>
    private void OnArm() => Debug.Log("ARM", this);

    /// <summary>Handles fire clicks.</summary>
    private void OnFire() => Debug.Log("FIRE", this);
}
```

`SetActive(false)` on this panel suspends its button handlers. Re-enabling restores
those same handlers without reinitializing the view. Normal `MonoBehaviour` features
(`Start`, `Update`, coroutine support) are still inherited.

## Important constraints

- Do **not** override/hide `Awake`, `OnEnable`, `OnDisable`, or `OnDestroy` in a `UIView<TBindings>` subclass. Use the corresponding view hooks so the base lifecycle remains authoritative.
- Register UI listeners via `Events` in `OnInitialize`, not repeatedly in `OnViewEnabled`.
- Manual `.onClick.AddListener` calls, global event subscriptions and coroutines are **not** managed by the Advanced UI Toolkit scope. Manage those explicitly, preferably using the lifecycle hooks.
- `OnViewInitialized` and `OnInitialize` are called once per `UIView` instance, not per pool checkout.
- Disabling the `UIView` MonoBehaviour itself suspends its managed events even if its GameObject stays active.
- The framework assumes normal Unity main-thread access to uGUI components.
- Failed initialization disables the view, logs the exception, and disposes its scope; it does not attempt to reinitialize automatically on re-enable.

## Manual lifecycle verification

1. Open a WeaponPanel with registered buttons, click once, deactivate/re-activate repeatedly, click again and confirm one log per press.
2. Turn off only the `WeaponPanel` MonoBehaviour and confirm clicks do not reach its handlers.
3. Destroy and recreate the panel; confirm no old callbacks continue firing and inspect Console for exceptions.
4. Repeat after pooling/reusing a panel, with and without TMP installed when relevant.

The compact Preview 11 distribution contains no bundled EditMode or PlayMode test assemblies.
