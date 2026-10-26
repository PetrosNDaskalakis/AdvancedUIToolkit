# Advanced UI Toolkit — v1.0.0-rc.13

**A portable, uGUI-first authoring toolkit for Unity.** This is a **release candidate**, not a production-verified stable release. Do not publish or deploy as stable until the Unity acceptance checks pass.

Advanced UI Toolkit reduces repetitive UI setup without replacing Unity prefabs or MonoBehaviours. It scans named uGUI objects, persists component references in a serialized manifest, and generates strongly typed C# accessors. Its Factory instantiates registered prefabs from reusable Style Profiles; Binder manages scanning, change review, safe regeneration, and optional diagnostics.

## Requirements

- Target: **Unity 2022.3+** (verify the exact Unity LTS versions your team uses).
- `com.unity.ugui` (declared as a package dependency).
- Unity Test Framework, for executing the included EditMode and PlayMode tests.
- No external dependencies on MNW assemblies in this package's Runtime or Editor source.

## Install in a new Unity project

1. Extract the package so its root contains `package.json`, `Runtime/`, `Editor/`, `Tests/` and `Samples~/`.
2. In Unity open **Window → Package Manager → + → Add package from disk** and select the extracted `package.json`. For an embedded editable package instead, place the folder under `Packages/com.advanceduitoolkit.framework/` while Unity is closed.
3. Open **Tools → Advanced UI Toolkit → Binder** and **Tools → Advanced UI Toolkit → Factory**.
4. To import samples, open Advanced UI Toolkit in Package Manager and use the **Samples** section. Samples provide code and instructions rather than complete scene assets.

**Existing projects:** Back up first and preserve original Unity script `.meta` GUIDs. When upgrading an earlier installation, migrate your project first and replace the old package rather than installing both copies. Generated classes and project-specific Style Profiles remain under `Assets/`. See [migration guide](Documentation~/MIGRATION_TO_ADVANCED_UI_TOOLKIT.md).

## Everyday workflow

1. Create or select a Canvas panel (your UI root).
2. Open **Factory** to choose a reusable prefab (button, toggle, image, etc.) from a Style Profile. The instance is named with the configured prefix. The UI Library automatically follows selected panels while ignoring ordinary child-control selections.
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
| **Binder** | Select root → inspect → preview → generate | **Show More**: naming/templates, automation, diagnostics, performance |
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

- [v1.0 release validation](Documentation~/V1_RELEASE_GATES.md) — detailed pass/fail gates for the Unity Editor, builds, Undo/Redo and clean installation.
- [Getting started](Documentation~/GETTING_STARTED.md) · [Developer handbook](Documentation~/DEVELOPER_HANDBOOK.md) · [API reference](Documentation~/API_REFERENCE.md) · [Troubleshooting](Documentation~/TROUBLESHOOTING.md)
- Static audit: run `python3 Tools~/verify_package.py` from the package root.
- Unity tests: **Window → General → Test Runner**, run both EditMode and PlayMode suites. Run a player build for a target platform before signing off stable 1.0.

## Publication status

**Not cleared for public GitHub.** There is intentionally no open-source license yet; publication rights, ownership, third-party assets, and the license must be approved first. See [publication checklist](PUBLICATION_CHECKLIST.md). Until licensed, publicly visible source code does not automatically grant permission to reuse it.

JSON UI schemas, reactive value binding, advanced navigation and other major systems are planned for post-v1.0 releases, **not** included here.


## Naming and project compatibility

The package, runtime and Editor namespaces, assembly names, generator markers, and branded script files all use **Advanced UI Toolkit** identifiers. New UI assets use `Assets/AdvancedUIToolkit/Generated` and `Assets/AdvancedUIToolkit/Libraries` by default.

**This release is not a drop-in upgrade for an older project.** The rename changes code symbols, so migrate your existing game scripts, generated C# files, assembly references and asset folders before reopening Unity. Existing saved generation paths may also require inspection.

## Migration from previous project versions

This release uses the `AdvancedUIToolkit` namespace and renamed assemblies throughout. It is a **breaking update** for previously integrated game scripts. Use the [migration guide](Documentation~/MIGRATION_TO_ADVANCED_UI_TOOLKIT.md) and the standalone project migration utility before upgrading existing projects.
