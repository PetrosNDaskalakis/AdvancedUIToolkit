# Advanced UI Toolkit v0.9.2 – Stabilization

This update targets runtime robustness without changing the public generated `ScreenBindings` model or existing Editor UX.

## Reference retrieval

- Required UI elements: `UIBindingRoot.Get<Button>("Fire")` throws if absent or wrong type.
- Optional UI elements: `UIBindingRoot.TryGet("DebugButton", out Button button)` returns false if absent.
- The binding registry is serialized with each root. A lookup cache is built on first access and invalidated after Unity deserializes its data.

## Event lifecycle

`UIView<T>` creates `Events` in `Awake` and disposes it in `OnDestroy`. Use these methods in `OnInitialize()`:

```csharp
Events.BindClick(UI.Fire, OnFire);
Events.BindValueChanged(UI.Volume, OnVolumeChanged);
Events.BindValueChanged(UI.Armed, OnArmedChanged);
Events.BindEndEdit(UI.Search, OnSearchSubmitted);
```

Do not attach the same listener manually with `onClick.AddListener` unless you also remove it yourself. For pooled UI, release/rebind scopes explicitly as part of a future pooling lifecycle; this release does not change OnDisable behavior.

## Tests in Unity

1. Open **Window > General > Test Runner** and select EditMode tests.
2. Run `AdvancedUIToolkit.Tests` tests, especially `UIBindingRootSafetyTests` and `UIEventScopeExtendedTests`.
3. Play existing `WeaponPanel` test, then rename/regenerate a button to exercise AutoBinder.
4. Verify Factory and Workspace still open.

## Compatibility

No changes to serialized field names or script GUIDs. This package is based on the earlier **tailored migration** archive containing the original project's `.meta` identifiers. To update an installed UPM package, apply the patch with Unity closed and back up the package first. Generated game-specific source stays in `Assets/AdvancedUIToolkit/Generated`.
