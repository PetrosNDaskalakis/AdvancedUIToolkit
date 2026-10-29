# Advanced UI Toolkit — v1.1.3

**A portable, uGUI-first authoring toolkit for Unity.** This is the **v1.1.4 Headless Binder integration release**, built on the v1.1.2 explicit-type and v1.1.1 External API foundations. It adds a documented public API for creating a complete UI hierarchy as a single Undo operation, without changing Binder or UI Library. Package construction and static checks have completed, but clean Unity compilation, real Undo/Redo and player-build acceptance have not been independently verified.

Advanced UI Toolkit reduces repetitive UI setup without replacing Unity prefabs or MonoBehaviours. It scans named uGUI objects, persists component references in a serialized manifest, and generates strongly typed C# accessors. Its Factory instantiates registered prefabs from reusable Style Profiles; Binder manages scanning, change review, safe regeneration, and optional diagnostics.

**v1.1.1 integration baseline (Unity validation pending):** Carries forward RC1's unchanged production behavior and compact distribution. No bundled EditMode/PlayMode suites or synthetic benchmark tools. [Stable package notes](Documentation~/V1_1_STABLE_RELEASE_NOTES.md) · [Final acceptance checklist](Documentation~/V1_1_STABLE_ACCEPTANCE.md). Prior [RC1 history](Documentation~/RC1_RELEASE_NOTES.md).

**For external packages:** `AdvancedUIEditorIntegration` exposes explicit Style Profile lookup, prefab and container creation, single-label text overrides, and the new `ValidateBatch` / `CreateBatch` hierarchy API. All batch steps are grouped into one Undo operation, with failure rollback and preserved Unity selection. Noninteractive Binder inspection, initial binding generation, safe regeneration, and automatic-generation suspension are now supported through `AdvancedUIBindingIntegration`. [External Editor API](Documentation~/EXTERNAL_EDITOR_API.md) · [Batch creation guide](Documentation~/BATCH_CREATION_API.md) · [Headless Binder integration](Documentation~/HEADLESS_BINDER_API.md).

**Preview 11 (Integration Stabilization):** Fixes native-control creation Undo isolation. The original developer-only regression fixtures are not bundled in the compact distribution. [Release verification guide](Documentation~/STABILIZATION_PREVIEW11.md).

**Preview 10 (Factory Performance Polish):** Avoids building unnecessary text-target descriptions for source validation, instance application, and disabled overrides. (Developer-only benchmark and test suites are not shipped in this compact package.) [Details](Documentation~/PERFORMANCE_POLISH_PREVIEW10.md).

**Preview 9 (Foundation Stabilization):** Keeps your chosen text target attached to its source component, even when labels move; missing labels now block creation instead of silently retargeting. Factory creation uses isolated Undo groups. Adds targeted regressions and a v1.1 release gate. [Details](Documentation~/STABILIZATION_PREVIEW9.md).

**Preview 8 (Prefab Text Override):** Edit a reusable prefab's **Displayed Text** before creating an instance in UI Library. Choose a label when a prefab has multiple text components, with optional TextMeshPro support. The original prefab and existing bindings are unchanged. [Guide](Documentation~/PREFAB_TEXT_OVERRIDE.md).

**Preview 7 (Performance Polish):** Reuses Binder display snapshots until edits occur, resolves inherited library templates only once per UI draw, and limits the high-resolution prefab preview cache. [Performance polish](Documentation~/PERFORMANCE_POLISH_PREVIEW7.md).

**Preview 6 (Generation Reliability):** Validated C# writes now use a recoverable commit step: external file edits block overwrites, failed manifest updates restore the previous generated file, and unchanged source is not rewritten. [Generation reliability](Documentation~/GENERATION_RELIABILITY_PREVIEW6.md).

**Foundation preview:** Binder supports optional per-property XML summaries. Preview 5 treats Canvas, Panels, and ordinary UI layout containers as valid authoring parents; selecting prefab instances or their descendants instead targets the nearest non-prefab container. Use **Factory → Container → + Create** to make named, non-prefab UI groups. Prefab controls remain discoverable and bindable under their owning binding root. [XML comments](Documentation~/BINDER_XML_SUMMARIES.md) · [Roadmap](Documentation~/FOUNDATION_ROADMAP.md) · [Preview 5 container selection](Documentation~/CONTAINER_SELECTION_PREVIEW5.md). The release remains focused on foundation quality and a small Editor-only authoring convenience.

**Compact package policy (v1.1.0):** Removed bundled EditMode/PlayMode tests, the Binder benchmark foldout, and synthetic benchmarking scripts. The production tools, generated code, and runtime API are unchanged. Measure performance with the Unity Profiler and use the manual release checklist.

## Requirements

- Target: **Unity 2022.3+** (verify the exact Unity LTS versions your team uses).
- `com.unity.ugui` (declared as a package dependency).
- No external dependencies on MNW assemblies in this package's Runtime or Editor source.

## Install in a new Unity project

1. Extract the package so its root contains `package.json`, `Runtime/`, `Editor/` and `Samples~/`.
2. In Unity open **Window → Package Manager → + → Add package from disk** and select the extracted `package.json`. For an embedded editable package instead, place the folder under `Packages/com.advanceduitoolkit.framework/` while Unity is closed.
3. Open **Tools → Advanced UI Toolkit → Binder** and **Tools → Advanced UI Toolkit → Factory**.
4. To import samples, open Advanced UI Toolkit in Package Manager and use the **Samples** section. Samples provide code and instructions rather than complete scene assets.

**Existing projects:** Back up first and preserve original Unity script `.meta` GUIDs. When upgrading an earlier installation, migrate your project first and replace the old package rather than installing both copies. Generated classes and project-specific Style Profiles remain under `Assets/`. See [migration guide](Documentation~/MIGRATION_TO_ADVANCED_UI_TOOLKIT.md).

## Everyday workflow

1. Create or select a Canvas panel (your UI root).
2. Select any non-prefab UI container or create a new one using **Factory → Container → + Create**. Open **Factory** to choose a reusable prefab (button, toggle, image, etc.) from a Style Profile. For text-bearing prefabs, optionally set **Override Displayed Text** in Selected Component before creating the instance. The UI Library never treats a prefab instance or its children as the active creation parent: selecting one resolves to its nearest regular UI container. The instance is named using its configured prefix.
3. Open **Binder** to see discovered components, pending changes and errors for the selected root.
4. Select **Preview C#** to inspect the generated class without writing a file.
5. Choose **Generate Bindings / Generate Changes** to update the C# source and manifest. In **Manual** mode this happens only when requested; **Automatic** mode can update safe changes following hierarchy edits, including deletion and Undo restoration.
6. Use the generated type in a `UIView<TBindings>` subclass and attach it to the same GameObject as `UIBindingRoot`.

### Example

Given UI objects `BTN_Arm`, `BTN_Fire`, `BTN_Load`, generate `WeaponPanelBindings` then write:

```csharp
using UnityEngine;
using AdvancedUIToolkit;
using Game.UI.Generated;

/// <summary>Uses Advanced UI Toolkit to handle the weapon panel's buttons.</summary>
public sealed class WeaponPanel : UIView<WeaponPanelBindings>
{
    /// <summary>Registers click handlers that are cleaned up automatically.</summary>
    protected override void OnInitialize()
    {
        Events.BindClick(UI.Arm, OnArm);
        Events.BindClick(UI.Fire, OnFire);
        Events.BindClick(UI.Load, OnLoad);
    }

    /// <summary>Handles the Arm button.</summary>
    private void OnArm() => Debug.Log("Armed");

    /// <summary>Handles the Fire button.</summary>
    private void OnFire() => Debug.Log("Fired");

    /// <summary>Handles the Load button.</summary>
    private void OnLoad() => Debug.Log("Loaded");
}
```

## What lives where?

| Window | Everyday task | Extra options |
|---|---|---|
| **Binder** | Select a UI GameObject → inspect its owning binding root → preview → generate | **Show More**: naming/templates, automation, diagnostics |
| **Factory / UI Library** | Select library → inspect prefab → create | **Show More**: profile management and validation |

Both windows are dockable alongside Unity's Inspector. The Factory uses cached, normal-state prefab previews, with a fallback when custom rendering is unavailable.

## Scope and safeguards

- **Reference binding, not MVVM data binding**: `UI.Arm` resolves a cached Button reference; it does not synchronize gameplay data automatically.
- Binding roots are isolated so nested UI components can have independent manifests.
- Renames may retain stable API property names. Removing controls removes corresponding generated accessors; update custom code that still references them.
- **Automatic mode writes C#** and Unity may recompile; Manual mode batches changes at a developer-selected time.
- Generated C# cannot be undone through Unity's normal Undo history. A restored hierarchy object triggers a fresh scan and generation in Automatic mode.
- Preview thumbnails are best-effort Editor representations. Advanced UI Toolkit does **not** optimize rendering performance or guarantee portable font/shader appearance.

## Testing and documentation

- [v1.1 release validation](Documentation~/V1_1_RELEASE_GATES.md) — detailed pass/fail gates for the Unity Editor, builds, Undo/Redo and clean installation.
- [Getting started](Documentation~/GETTING_STARTED.md) · [Developer handbook](Documentation~/DEVELOPER_HANDBOOK.md) · [API reference](Documentation~/API_REFERENCE.md) · [Troubleshooting](Documentation~/TROUBLESHOOTING.md)
- Static audit: run `python3 Tools~/verify_package.py` from the package root.
- Unity verification: check Binder/Factory workflows in a clean project, compile scripts and run a player build; record outcomes in the [final acceptance checklist](Documentation~/V1_1_STABLE_ACCEPTANCE.md) before signing off stable 1.1.0. For performance, use **Window → Analysis → Profiler**.

## Publication status

**Not cleared for public GitHub.** There is intentionally no open-source license yet; publication rights, ownership, third-party assets, and the license must be approved first. See [publication checklist](PUBLICATION_CHECKLIST.md). Until licensed, publicly visible source code does not automatically grant permission to reuse it.

JSON UI schemas are planned for **Phase 3 (v1.2 onward, after documented v1.1.0 stable acceptance)**; they are not included in this package.


## Naming and project compatibility

The package, runtime and Editor namespaces, assembly names, generator markers, and branded script files all use **Advanced UI Toolkit** identifiers. New UI assets use `Assets/AdvancedUIToolkit/Generated` and `Assets/AdvancedUIToolkit/Libraries` by default.

**This release is not a drop-in upgrade for an older project.** The rename changes code symbols, so migrate your existing game scripts, generated C# files, assembly references and asset folders before reopening Unity. Existing saved generation paths may also require inspection.

## Migration from previous project versions

This release uses the `AdvancedUIToolkit` namespace and renamed assemblies throughout. It is a **breaking update** for previously integrated game scripts. Use the [migration guide](Documentation~/MIGRATION_TO_ADVANCED_UI_TOOLKIT.md) and the standalone project migration utility before upgrading existing projects.

**v1.1.5:** External Editor packages can register custom binding prefixes via `AdvancedUIComponentRegistry`, and create unbound Custom decorative prefabs using `AllowUnbound`. See [Component Extensibility API](Documentation~/COMPONENT_EXTENSIBILITY_API.md).
