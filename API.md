# API Reference

A focused reference to the main API surface **verified in the supplied source**. Namespaces, overloads and secondary extension points can be explored in the source files or the archived reference guides in `Document~/`.

## Runtime — `AdvancedUIToolkit`

| Type / member | Purpose |
| --- | --- |
| `UIBindingRoot : MonoBehaviour` | Owns a screen's serialized binding manifest. |
| `T Get<T>(string key) where T : Component` | Required component lookup; throws `KeyNotFoundException` on failure. |
| `bool TryGet<T>(string key, out T component)` | Optional component lookup; returns false when absent or incompatible. |
| `IUIBindings.Initialize(UIBindingRoot root)` | Interface implemented by generated binding containers. |
| `UIView<TBindings> : MonoBehaviour` | Base view; `TBindings : class, IUIBindings, new()`. |
| `UIView<TBindings>.UI` | Protected typed binding container. |
| `UIView<TBindings>.Events` | Protected managed event scope. |
| `UIEventScope.BindClick(Button, UnityAction)` | Registers a managed uGUI button listener. |
| `UIEventScope.BindValueChanged(...)` | Overloads for Toggle, Slider, Scrollbar, InputField and Dropdown. |
| `UIEventScope.BindEndEdit(InputField, UnityAction<string>)` | Registers an input end-edit listener. |
| `UIEventScope.Suspend() / Resume() / Dispose()` | Listener lifecycle control. |

`UIBindingRoot` lookup keys use generated accessor names: for `BTN_Fire`, use `"Fire"`, not `"BTN_Fire"`.

### View lifecycle

Override `OnInitialize()` (required), optionally `OnViewInitialized()`, `OnViewEnabled()`, `OnViewDisabled()`, `OnViewDestroyed()` and `OnDispose()`. The base class owns `Awake`, `OnEnable`, `OnDisable` and `OnDestroy`.

```csharp
protected override void OnInitialize()
{
    Events.BindClick(UI.Fire, OnFire);
}
```

## Framework Editor API

The editor assembly exposes these integration entry points (editor code only):

| Entry point | Verified members |
| --- | --- |
| `AdvancedUIBindingIntegration` | `InspectBindings(UIBindingRoot)`, `GenerateBindings(...)`, `InitializeBindings(UIEditorBindingSetup)`, `AutomaticGenerationEnabled`, `SuspendAutomaticGeneration()` |
| `AdvancedUIBatchIntegration` | `ValidateBatch(UIEditorBatchRequest)`, `CreateBatch(UIEditorBatchRequest)` |
| `AdvancedUIComponentRegistry` | `TryRegister(string, Type, out string)`, `TryGetType(string, out Type)` |

These are lower-level integration APIs. Most projects should begin with the editor windows rather than writing automation code.

## Authoring Editor API

`UIAuthoringRegistry` exposes registration and lookup for optional JSON authoring element factories, validators, style classes and property adapters. This compatibility/extension surface is separate from the recommended mold-first Layouts window.

## Source of truth

- Runtime: `Packages/com.advanceduitoolkit.framework/Runtime/`
- Framework editor: `Packages/com.advanceduitoolkit.framework/Editor/`
- Authoring editor and molds: `Packages/com.advanceduitoolkit.authoring/Editor/`

Public signatures may evolve during the preview releases. Check the actual source before relying on secondary or advanced APIs.
