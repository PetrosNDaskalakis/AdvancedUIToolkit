# API Reference

[← README](../README.md) · [Architecture](ARCHITECTURE.md) · [Developer Handbook](DEVELOPER_HANDBOOK.md)

This reference covers the **runtime API available in Advanced UI Toolkit `1.1.0-preview.2`**, based on the package's Runtime source. It does not include proposed JSON schemas or reactive data binding.

> **Namespace:** `AdvancedUIToolkit` · **Runtime assembly:** `AdvancedUIToolkit.Runtime` · **UPM package:** `com.advanceduitoolkit.framework`

## `UIBindingRoot : MonoBehaviour`

Stores the serialized component manifest for a single screen/prefab root.

| Member                                                            | Purpose                                                                                   |
| ----------------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| `T Get<T>(string key) where T : Component`                        | Required typed lookup. Throws `KeyNotFoundException` when unavailable or type mismatched. |
| `bool TryGet<T>(string key, out T component) where T : Component` | Optional typed lookup. Returns `false` if missing, destroyed, or type mismatched.         |

```csharp
using UnityEngine.UI;
using AdvancedUIToolkit;

/// <summary>Demonstrates required and optional component lookups.</summary>
public sealed class ReferenceExample : UnityEngine.MonoBehaviour
{
    /// <summary>Returns whether the optional reset button is registered.</summary>
    private bool HasResetButton()
    {
        UIBindingRoot root = GetComponent<UIBindingRoot>();
        return root.TryGet("Reset", out Button reset);
    }
}
```

The lookup key is the **generated property name**, not the prefixed GameObject name: `BTN_Fire` produces `"Fire"`, not `"BTN_Fire"`.

### Manifest entry

`UIBindingRoot.Entry` contains `Key`, `HierarchyKey`, `StableId`, `ComponentType`, and `Reference`. The Editor creates and validates entries; game code should normally use the generated binding class rather than edit entries manually.

## `IUIBindings`

```csharp
/// <summary>Contract implemented by generated reference containers.</summary>
public interface IUIBindings
{
    /// <summary>Resolves registered component references once.</summary>
    /// <param name="root">The owning serialized UI root.</param>
    void Initialize(UIBindingRoot root);
}
```

Generated classes implement `IUIBindings` and expose cached strongly typed properties. They must have a public parameterless constructor for `UIView<TBindings>`.

## `UIView<TBindings> : MonoBehaviour`

```csharp
public abstract class UIView<TBindings> : MonoBehaviour
    where TBindings : class, IUIBindings, new()
```

This is the recommended base class for a screen that uses a generated binding class.

| Protected member               | Description                                   |
| ------------------------------ | --------------------------------------------- |
| `TBindings UI { get; }`        | Initialized binding container, e.g. `UI.Fire` |
| `UIEventScope Events { get; }` | Owned event listener scope                    |

### Lifecycle hooks

| Hook                                         | When called                                                          |
| -------------------------------------------- | -------------------------------------------------------------------- |
| `protected abstract void OnInitialize()`     | Once, after references are resolved; register managed UI events here |
| `protected virtual void OnViewInitialized()` | Once, after `OnInitialize()` completes                               |
| `protected virtual void OnViewEnabled()`     | On each activation after successful initialization                   |
| `protected virtual void OnViewDisabled()`    | On each deactivation after being enabled                             |
| `protected virtual void OnViewDestroyed()`   | Once on destruction, before managed listener cleanup                 |
| `protected virtual void OnDispose()`         | Compatibility cleanup hook, called during teardown                   |

Do not hide Unity `Awake`, `OnEnable`, `OnDisable`, or `OnDestroy` in subclasses. Those callbacks are managed by `UIView<TBindings>`. Other MonoBehaviour features (e.g. `Start`, `Update`, `transform`, coroutines) remain available.

### Example

```csharp
using UnityEngine;
using AdvancedUIToolkit;
using Game.UI.Generated;

/// <summary>Connects the weapon buttons to test actions.</summary>
public sealed class WeaponPanel : UIView<WeaponPanelBindings>
{
    /// <summary>Registers listeners once.</summary>
    protected override void OnInitialize()
    {
        Events.BindClick(UI.Arm, OnArm);
        Events.BindClick(UI.Fire, OnFire);
    }

    /// <summary>Reports when this view is activated.</summary>
    protected override void OnViewEnabled() => Debug.Log("Weapon UI enabled");

    /// <summary>Reports when the Arm button is clicked.</summary>
    private void OnArm() => Debug.Log("Arm");

    /// <summary>Reports when the Fire button is clicked.</summary>
    private void OnFire() => Debug.Log("Fire");
}
```

## `UIEventScope : IDisposable`

Registers UnityEvent callbacks owned by the screen and detaches them safely during disable or destruction.

| Method                                              | Control               | Callback type                |
| --------------------------------------------------- | --------------------- | ---------------------------- |
| `BindClick(Button, UnityAction)`                    | Button                | no parameters                |
| `BindValueChanged(Toggle, UnityAction<bool>)`       | Toggle                | `bool`                       |
| `BindValueChanged(Slider, UnityAction<float>)`      | Slider                | `float`                      |
| `BindValueChanged(Scrollbar, UnityAction<float>)`   | Scrollbar             | `float`                      |
| `BindValueChanged(InputField, UnityAction<string>)` | Legacy InputField     | `string`                     |
| `BindEndEdit(InputField, UnityAction<string>)`      | Legacy InputField     | `string`                     |
| `BindValueChanged(Dropdown, UnityAction<int>)`      | Legacy Dropdown       | `int`                        |
| `Suspend()`                                         | All managed listeners | detach temporarily           |
| `Resume()`                                          | All managed listeners | reattach without duplicates  |
| `Dispose()`                                         | All managed listeners | detach permanently and clear |

Properties: `IsSuspended`, `IsDisposed`.

**Important:** Only listeners registered through `Events` are managed by the scope. Manually registered `onClick.AddListener()` callbacks remain your responsibility.

## `UIStyleProfile : ScriptableObject`

A reusable prefab library that may inherit a base profile.

| Member                                                               | Purpose                                                 |
| -------------------------------------------------------------------- | ------------------------------------------------------- |
| `BaseProfile`                                                        | Optional inherited profile                              |
| `Templates`                                                          | Read-only collection of local template entries          |
| `TryResolve(UIComponentKind kind, out UIComponentTemplate template)` | Resolve an effective role, preferring derived overrides |
| `TryResolveCustom(string id, out UIComponentTemplate template)`      | Resolve an effective custom entry                       |
| `GetEffectiveTemplates()`                                            | Returns effective templates including inherited ones    |

### `UIComponentTemplate`

Serializable fields: `Kind` (`UIComponentKind`), `Id` (for `Custom`), `DefaultObjectName`, `Prefab`. Its `Identity` combines the component role or custom ID.

### `UIComponentKind`

Current values: `TextLegacy`, `Button`, `Image`, `RawImage`, `Panel`, `Toggle`, `Slider`, `InputField`, `ScrollView`, `Scrollbar`, `Dropdown`, `RectTransform`, `Custom`.

## `UIStyleContext : MonoBehaviour`

Optional root component whose `Profile` property identifies the style profile used by the Factory. The nearest ancestor context is considered when authoring child controls.

## Editor tools (not player API)

| Entry point                                               | Purpose                                        |
| --------------------------------------------------------- | ---------------------------------------------- |
| **Tools → Advanced UI Toolkit → Binder**                  | Scan, review, preview, and generate bindings   |
| **Tools → Advanced UI Toolkit → Factory**                 | Browse and instantiate reusable components     |
| **GameObject → Advanced UI Toolkit → ...**                | Create configured, named standard uGUI objects |
| **Assets → Create → Advanced UI Toolkit → Style Profile** | Create a profile asset                         |

Editor-only APIs are not intended to be called by gameplay code or player builds.

## Assemblies and game scripts

If your game scripts use an assembly definition (`.asmdef`), add a reference to `AdvancedUIToolkit.Runtime`. The Editor code belongs to `AdvancedUIToolkit.Editor` and must not be referenced by runtime game assemblies. Generated binding C# lives in `Assets/` and uses `using AdvancedUIToolkit;` even though the product is branded Advanced UI Toolkit.

For full architecture context, see [Architecture](ARCHITECTURE.md) and [Technical Overview](TECHNICAL_OVERVIEW.md).
