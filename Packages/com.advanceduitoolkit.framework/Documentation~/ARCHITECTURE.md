# Framework Architecture

[← README](../README.md) · [Documentation Index](INDEX.md) · [Technical Overview](TECHNICAL_OVERVIEW.md)

Advanced UI Toolkit is **an authoring layer over Unity's uGUI**, not a custom renderer. Editor tools create or inspect ordinary GameObjects and prefabs; a small Runtime assembly supplies binding manifests, typed view initialization, event lifetime management, and Style Profile data.

## High-level system

```text
                         Unity Editor
  ┌─────────────────────────────────────────────────────┐
  │ UI Library / Factory                                │
  │   Profile → prefab selection → named GameObject     │
  │                                                     │
  │ Binder                                              │
  │   scan → validate → compare → preview → generate    │
  │                                                     │
  │ Automation / Rename / Diagnostics / Templates       │
  │   watch changes, preserve identity, verify safety   │
  └──────────────────┬──────────────────────────────────┘
                     │ writes / updates
       ┌─────────────┴─────────────────────────────┐
       │                                           │
  UIBindingRoot manifest                  Generated C# class
  (serialized Components)                 (IUIBindings)
       │                                           │
       └─────────────────┬─────────────────────────┘
                         │ initializes
                  UIView<TBindings>
                         │
                   UIEventScope
                         │
                   Native Unity uGUI
```

## Package boundaries

```text
com.advanceduitoolkit.framework/
├── package.json                    # UPM identity and dependencies
├── Runtime/                        # shipped to player builds
│   ├── UIBindingRoot.cs
│   ├── IUIBindings.cs
│   ├── UIView.cs
│   ├── UIEventScope.cs
│   ├── UIStyleProfile.cs
│   ├── UIStyleContext.cs
│   └── AdvancedUIToolkit.Runtime.asmdef
├── Editor/                         # Unity Editor only
│   ├── AdvancedUIToolkitWorkspaceWindow.cs     # Binder UI
│   ├── UIComponentLibraryWindow.cs # Factory / Library UI
│   ├── AdvancedUIToolkitEditor.cs              # scanning and code generation
│   ├── AdvancedUIToolkitAutomation.cs
│   ├── UIBindingSafety.cs
│   ├── UIPrefabPreviewCache.cs
│   ├── UIStyleFactory.cs
│   └── AdvancedUIToolkit.Editor.asmdef
├── Samples~/
├── Documentation~/
└── Tools~/
```

The application owns its own `Assets/` content: source prefabs, Style Profile assets, generated bindings, and screen scripts. The package does not need game-specific assemblies or assets.

## Runtime responsibilities

### `UIBindingRoot`

One `UIBindingRoot` belongs to one screen or reusable widget root. It stores a serialized manifest with a generated property key, a name-derived key, a stable ID, a component type, and an actual Unity `Component` reference. At runtime it lazily builds a dictionary from the serialized entries. Required calls use `Get<T>()`; optional calls use `TryGet<T>()`.

### Generated `IUIBindings`

The Editor writes one typed class per configured root, for example `WeaponPanelBindings`. `Initialize(UIBindingRoot root)` resolves references **once** and caches them in typed properties, so subsequent `UI.Fire` access does not traverse the hierarchy or repeat dictionary lookup.

### `UIView<TBindings>` and `UIEventScope`

`UIView<TBindings>` is a `MonoBehaviour` base class that retrieves `UIBindingRoot` on its own GameObject, initializes its generated references, creates an event scope, and invokes user-defined view hooks. `UIEventScope` manages subscriptions and suspends them when the view is disabled. Disposing a view removes its registered managed listeners.

### `UIStyleProfile` and `UIStyleContext`

A Style Profile references reusable prefab templates and optionally inherits a parent profile. A Style Context marks a screen or station root with its preferred profile. The Factory inspects that context during Editor authoring; this does not create a runtime theme engine.

## Editor responsibilities

### Factory

Creates uGUI controls using built-in defaults or registered prefab templates; names them with the expected prefixes; preserves prefab linkage; and uses the selected root and relevant Style Profile.

### Binder

Discovers named UI controls, respects nested binding-root boundaries, checks names/types/duplicates, recognizes renamed components, reviews the proposed generated class, and commits validated changes. A root's generation profile stores the generated class name, namespace, output folder, and optional template source path.

### Automation and diagnostics

Automatic mode can scan and regenerate safe changes after edits and Undo/Redo. Manual mode detects pending changes but waits for explicit generation. Diagnostics report invalid or stale mappings. Generated files are guarded against accidental overwrites, including collisions between independent roots.

### Prefab previews

`UIPrefabPreviewCache` renders an isolated Editor-only visual preview of ordinary uGUI controls where supported. It falls back to a simplified graphic if the render pipeline or asset cannot be captured accurately. The preview has no effect on runtime presentation.

## Ownership and storage

| Asset or responsibility              | Owner                   | Location                                                 |
| ------------------------------------ | ----------------------- | -------------------------------------------------------- |
| Framework runtime/editor code        | Package                 | `Packages/com.advanceduitoolkit.framework/` or installed UPM source |
| Game's UI prefabs and Style Profiles | Game/project            | `Assets/...`                                             |
| Generated `...Bindings.cs`           | Game/project            | `Assets/AdvancedUIToolkit/Generated/` by default         |
| Serialized binding manifest          | Selected UI root        | `UIBindingRoot` component on scene/prefab                |
| User-written view/controller logic   | Game/project            | Project C# scripts                                       |
| Example templates                    | Package samples or game | `Samples~/` or project `Assets/`                         |

## Design boundaries

- Runtime cannot depend on UnityEditor or any code in the Editor assembly.
- The project must not depend on Editor APIs in player scripts.
- Nested roots are isolated; the same property name can appear in different roots.
- The source prefab's visual design remains editable through normal Unity Prefab workflows.
- Generated C# can break referring gameplay code after an intentional removal or rename; review before generation.
- JSON UI schemas and reactive model-to-view binding are **not part of this v1.0 release candidate**.

For method-level details see [API Reference](API_REFERENCE.md); for pipeline decisions and limitations see [Technical Overview](TECHNICAL_OVERVIEW.md).
