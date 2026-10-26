# Technical Overview

[← README](../README.md) · [Architecture](ARCHITECTURE.md) · [API Reference](API_REFERENCE.md)

## Design objective

The goal is to **reduce repetitive UI authoring and maintenance work in uGUI-heavy Unity projects**, while retaining familiar Prefab, Canvas, and MonoBehaviour workflows. The toolkit automates discovery and references, not rendering or game logic.

Its central trade-off is deliberate: a team adopts naming conventions and an Editor-side generation step in exchange for fewer handwritten references and earlier validation.

## 1. Why use generated code instead of runtime reflection?

The Builder/Factory already knows which objects exist in the Unity hierarchy. Rather than repeatedly searching for them at runtime, Binder uses the Editor to build a serialized manifest and generate typed C# accessors.

The result is familiar C# autocomplete and direct property access:

```csharp
/// <summary>Uses a cached, generated reference to the Fire button.</summary>
private void DisableFireButton() => UI.Fire.interactable = false;
```

The generated initializer performs each required manifest lookup once. Subsequent accesses use cached component properties. This does not imply a measurable FPS improvement over well-written manual serialized fields; the primary benefit is **productivity, consistency, and maintainability**.

## 2. Discovery and naming

Binder scans a selected screen/root for GameObjects with recognized prefixes. For example, `BTN_Fire` maps to `UnityEngine.UI.Button` with public key `Fire`. It checks that the expected component exists on the named GameObject and that public identifiers are unambiguous within the scope.

UI roots are hierarchical boundaries. If a child widget has its own `UIBindingRoot`, its controls belong to that widget rather than the parent manifest. This makes reusable nested prefabs independently bindable.

## 3. Persistent identity and renaming

Manifest entries include stable identifiers, the current hierarchy-derived key, the generated accessor name, component type, and serialized reference. Matching existing component references to new scan results lets the Editor recognize a rename as the same UI element.

Developers may preserve the previous public property name after a GameObject rename. If they intentionally rename or delete a generated property, consuming code must be updated; the toolkit does not silently rewrite arbitrary handwritten C#.

## 4. Generation pipeline

```text
Hierarchy or prefab change
        ↓
Detect selected owning UIBindingRoot
        ↓
Scan names, types, and nested scopes
        ↓
Compare with current serialized manifest
        ↓
Validate duplicates, missing components,
unsafe prefab edits, output ownership, template validity
        ↓
Preview the proposed source and changes
        ↓
Manual approval OR allowed automatic generation
        ↓
Write generated C# to the project Assets folder
        ↓
Commit serialized manifest and generation profile
        ↓
Unity imports / compiles modified C#
```

The order of guarded file writes and manifest changes matters: if output is unsafe or ownership is ambiguous, generation should stop rather than corrupt another screen's generated file.

An empty screen is valid: deleting the last bound component can produce a valid bindings class with no component properties.

## 5. Manual versus Automatic generation

**Manual mode:** A developer creates several controls, renames or removes some, and generates the reviewed changes once. This limits C# import/compile interruptions during UI layout work.

**Automatic mode:** The Editor tracks relevant hierarchy changes and may regenerate validated changes as they happen. Deletion and Undo/Redo can require a second scan after Unity restores objects. Generated code itself is not part of normal Unity Undo history, so synchronization means *regenerating from the current hierarchy and manifest*, not attempting to undo a text-file write.

These modes do not override Unity's own Auto Refresh preferences.

## 6. Template-based generated source

Default generation produces a documented `IUIBindings` class. Optionally a team supplies a compilable C# template (`ExampleScript.cs`) with two insertion markers:

```csharp
// <UICORE:PROPERTIES>
// <UICORE:ASSIGNMENTS>
```

The first marker belongs inside the class; the second inside `Initialize(UIBindingRoot root)`. The generator replaces the example's namespace/class identity and inserts typed properties and assignments. Optional custom property XML-summary patterns can use `{name}`, `{hierarchy}`, and `{type}`. Templates are intentionally constrained to insertion markers; this is not a general AST refactoring or an arbitrary C# rewriting engine.

## 7. Prefab libraries and styling

`UIStyleProfile` holds prefab templates by component role and can inherit a base profile. A derived profile overrides a role or custom ID, allowing a game to share common station controls while overriding specific designs. Factory instantiates linked prefab assets, retaining ordinary Unity prefab override behavior.

A Style Profile selects *which prefab to create*. It does not become a reactive runtime stylesheet, control the game's UI state, or replace a design system.

## 8. Event and lifecycle ownership

The generated binding object holds Unity component references. `UIView<TBindings>` owns initialization and lifetime; `UIEventScope` owns managed listeners.

A view calls `Events.BindClick` or other supported methods in `OnInitialize()` once. On disable, `UIEventScope.Suspend()` removes those listeners; on enable, `Resume()` restores them without duplicating callbacks. On destruction, `Dispose()` removes managed subscriptions. Manually added UnityEvent listeners are outside that scope and must be managed separately.

## 9. Build, performance, and compatibility

- **Assembly separation:** `UICore.Runtime` is player-compatible, `UICore.Editor` is Editor-only.
- **Serialized references:** preserve Unity `.meta` GUIDs and prefab links during package migration.
- **Performance:** prefer explicit measurements of scan/generate cost, managed allocations, Editor stalls, and real view initialization. Do not infer runtime speedups solely from fewer lines of source.
- **Editor previews:** thumbnail quality and fidelity vary with fonts, shader support, and render pipelines.
- **Unity versions:** the package declares 2022.3 as a minimum; team-specific versions need clean installs and player-build verification.
- **Safety:** generated-file ownership checks cannot prevent all logical errors, especially code outside the generated file still referencing deleted accessors.

## 10. Evaluation plan for a team

Compare one representative 20–50-component screen built with normal Inspector wiring versus Advanced UI Toolkit:

| Metric | Why it matters |
| --- | --- |
| Setup time | Measures the actual productivity benefit |
| Handwritten reference-field count | Shows boilerplate avoided |
| Rename/delete repair time | Tests refactoring quality |
| Compilation interruptions | Measures Manual/Automatic mode usability |
| Missing-reference errors | Tests validation value |
| Time to onboard a new developer | Tests whether the tool is simpler than the workflow it replaces |
| Player build and package compatibility | Establishes basic adoption readiness |

## 11. Current limitations and next research area

This v1.0 release candidate provides **component reference binding**, not reactive game-data binding, JSON-generated UI layouts, screen navigation, or universal UI across rendering backends.

A future optional schema system could define prefab choices, parent–child relationships, and layout metadata in a structured format such as JSON, then construct initial uGUI hierarchies using existing Style Profiles. Safe bidirectional synchronization and migration rules would need separate design and testing.

## Publication readiness

Before a public GitHub release, confirm the copyright owner, remove any proprietary assets, approve a license, and run the full Unity acceptance checklist. The package has a release-candidate version; passing static checks is not the same as player-build or Editor verification.
