# Extension APIs

[Compatibility index](INDEX.md#json-compatibility) · [Installation](INSTALLATION.md)

This guide documents the retained JSON compatibility workflow in Authoring. For scene hierarchy molds, start with [Layouts getting started](GETTING_STARTED.md).

The public `AdvancedUIAuthoring.Editor` assembly exposes:

- `UIAuthoringSchemaParser.TryParse(string, out UIAuthoringDocument, out string)`
- `UIAuthoringValidator.Validate(UIAuthoringDocument, RectTransform)`
- `UIAuthoringGenerator.Generate(UIAuthoringDocument, RectTransform)`
- `UIAuthoringRegistry.TryRegisterElement(string, IUIAuthoringElementFactory, out string)`
- `UIAuthoringRegistry.TryRegisterStyleClass(string, IUIAuthoringStyleClass, out string)`

## Register custom element types

External Editor assemblies may register a factory on every domain reload (using `InitializeOnLoad`, for example). A factory must return a **new, unparented, non-persistent GameObject with RectTransform**. It must not update Unity selection or use its own Undo group. The generator owns parenting, undo, and cleanup. Custom factories that create prefab instances must leave them unparented and preserve prefab links.

```csharp
using AdvancedUIAuthoring.Editor;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ExampleRegistration
{
    static ExampleRegistration()
    {
        string error;
        if (!UIAuthoringRegistry.TryRegisterElement("decorative-panel", new DecorativePanelFactory(), out error))
        {
            Debug.LogError(error);
        }
    }
}

public sealed class DecorativePanelFactory : IUIAuthoringElementFactory
{
    public bool CanContainChildren { get { return true; } }

    public GameObject Create(UIAuthoringNode node)
    {
        return new GameObject(node.name, typeof(RectTransform));
    }
}
```

`IUIAuthoringElementValidator` is an optional companion interface for factories that need extra non-mutating preflight checks. `IUIAuthoringStyleClass` offers validation and styling callbacks for named `styleClass` values. Built-in style fields apply first; the class handler applies afterward. A later stylesheet package could parse its own format and register styles, without modifying this Editor assembly.

## Custom style properties

External Editor assemblies can implement `IUIAuthoringStylePropertyAdapter` and register property names such as `border-width` through `UIAuthoringRegistry.TryRegisterStyleProperty("border-width", adapter, out error)`. The adapter validates the **fully expanded** value during preflight, and applies it to the **newly created** native GameObject after built-in styling. Built-in property names cannot be replaced by external adapters. Register after assembly reload (for example using `[InitializeOnLoad]`). No runtime component is automatically added.

Example registration:

```csharp
string error;
bool accepted = UIAuthoringRegistry.TryRegisterStyleProperty(
    "border-width", new ExampleBorderAdapter(), out error);
if (!accepted) Debug.LogError(error);
```

The `ExampleBorderAdapter` above is a user-provided type, **not bundled** with this package. The extension interface exposes `TryValidate(UIAuthoringNode node, string value, out string error)` and `Apply(GameObject target, UIAuthoringNode node, string value)`.

## Integration boundary

The Integration assembly included in Authoring owns Style Profile resolution, linked prefab creation, text-target identity and Binder synchronization through Framework's public Editor APIs. The current Authoring package depends on Framework. See the [bridge API](LegacyIntegration/API.md) for this compatibility path and [Architecture](ARCHITECTURE.md) for package boundaries.
