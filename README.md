# Advanced UI Toolkit

<img width="1254" height="1254" alt="Advanced UI Toolkit Logo" src="https://github.com/user-attachments/assets/e7bf8a6f-3c06-4e41-8f4e-69041e63eaa2" />


Build reusable Unity uGUI screens with prefab libraries, reusable container layouts and generated C# bindings.

Advanced UI Toolkit keeps visual authoring in Unity. **Library** places your saved control prefabs, **Layouts** captures and recreates container structures, and **Binder** connects those controls to typed gameplay code.

## Packages

| Package | Version | Purpose |
| --- | --- | --- |
| [Framework](Packages/com.advanceduitoolkit.framework/README.md) | `1.2.0-preview.3` | Library, Binder, generated bindings and runtime view lifecycle |
| [Authoring / Layouts](Packages/com.advanceduitoolkit.authoring/README.md) | `0.5.0-preview.8` | Reusable hierarchy molds and optional JSON authoring tools |

Framework works on its own. Install Authoring alongside it to enable Layouts. Both packages declare Unity **2022.3** as their minimum and depend on uGUI. The version identifiers above are preview versions; compatibility with your exact Editor version and player target should be verified in your project.

## Main tools

| Open from Tools → Advanced UI Toolkit | Use it to |
| --- | --- |
| **Library** | Browse, search and filter prefab templates; choose a target container; create linked prefab instances using a Style Profile |
| **Layouts** | Capture an eligible scene hierarchy, inspect omissions, save a reusable mold and create an independent layout |
| **Binder** | Discover named controls, review changes, preview generated C# and generate typed component accessors |

Layouts captures ordinary scene RectTransforms and their supported components. Prefab branches and non-RectTransform branches are excluded in full. References inside the retained structure are remapped; references to external assets or omitted objects are cleared and reported. Add complete prefab controls with Library after creating the layout. See [mold behavior and limits](Packages/com.advanceduitoolkit.authoring/Documentation~/HIERARCHY_MOLDS.md).

## Install

1. Save your Unity project and close the Editor.
2. Copy `Packages/com.advanceduitoolkit.framework` into your project's `Packages` folder.
3. For Layouts, also copy `Packages/com.advanceduitoolkit.authoring` alongside it.
4. Reopen Unity and allow package import and compilation to finish.
5. Open **Tools → Advanced UI Toolkit → Library**, **Layouts** or **Binder**.

Copy the package directories, keeping their `.meta` files. Keep your project's own `Packages/manifest.json` and `packages-lock.json`; the supplied project files are not a universal dependency configuration. For upgrades, replace the complete toolkit package directories so obsolete files are removed. Do not keep duplicate package scripts under `Assets` or install the former standalone Integration package alongside Authoring.

Alternative installation and upgrade instructions: [Framework](Packages/com.advanceduitoolkit.framework/Documentation~/INSTALLATION.md) · [Layouts](Packages/com.advanceduitoolkit.authoring/Documentation~/INSTALLATION.md).

## Build your first screen

1. Create a Canvas and an ordinary panel hierarchy. Optionally capture it in **Layouts → Create New Layout**, inspect the result, choose a destination and create a new layout. **Load Layout** reuses saved molds.
2. Create a **Style Profile** from **Assets → Create → Advanced UI Toolkit → Style Profile**. Register your saved uGUI control prefabs and choose the profile in **Library**.
3. Choose an eligible container and create a Button named `BTN_Fire`.
4. Add `UIBindingRoot` to the screen root. In **Binder**, select that root, configure the binding class as `WeaponPanelBindings`, namespace `Game.UI.Generated` and an output folder under `Assets`, then review and generate.
5. After Unity compiles the generated class, add this view to the same root:

```csharp
using AdvancedUIToolkit;
using Game.UI.Generated;
using UnityEngine;

public sealed class WeaponPanel : UIView<WeaponPanelBindings>
{
    protected override void OnInitialize()
    {
        Events.BindClick(UI.Fire, OnFire);
    }

    private void OnFire() => Debug.Log("Fire");
}
```

Managed listeners suspend when the view is disabled and resume when it is enabled. Register them once in `OnInitialize`; use the view lifecycle hooks instead of hiding Unity's `Awake`, `OnEnable`, `OnDisable` or `OnDestroy` methods.

## Documentation

| Guide | Framework | Layouts |
| --- | --- | --- |
| Documentation index | [Index](Packages/com.advanceduitoolkit.framework/Documentation~/INDEX.md) | [Index](Packages/com.advanceduitoolkit.authoring/Documentation~/INDEX.md) |
| Installation | [Install](Packages/com.advanceduitoolkit.framework/Documentation~/INSTALLATION.md) | [Install](Packages/com.advanceduitoolkit.authoring/Documentation~/INSTALLATION.md) |
| Getting started | [Quick start](Packages/com.advanceduitoolkit.framework/Documentation~/GETTING_STARTED.md) | [Quick start](Packages/com.advanceduitoolkit.authoring/Documentation~/GETTING_STARTED.md) |
| Developer handbook | [Handbook](Packages/com.advanceduitoolkit.framework/Documentation~/DEVELOPER_HANDBOOK.md) | [Handbook](Packages/com.advanceduitoolkit.authoring/Documentation~/DEVELOPER_HANDBOOK.md) |
| Architecture | [Architecture](Packages/com.advanceduitoolkit.framework/Documentation~/ARCHITECTURE.md) | [Architecture](Packages/com.advanceduitoolkit.authoring/Documentation~/ARCHITECTURE.md) |
| Technical overview | [Overview](Packages/com.advanceduitoolkit.framework/Documentation~/TECHNICAL_OVERVIEW.md) | [Overview](Packages/com.advanceduitoolkit.authoring/Documentation~/TECHNICAL_OVERVIEW.md) |
| API reference | [API](Packages/com.advanceduitoolkit.framework/Documentation~/API_REFERENCE.md) | [API](Packages/com.advanceduitoolkit.authoring/Documentation~/API_REFERENCE.md) |
| Troubleshooting | [Help](Packages/com.advanceduitoolkit.framework/Documentation~/TROUBLESHOOTING.md) | [Help](Packages/com.advanceduitoolkit.authoring/Documentation~/TROUBLESHOOTING.md) |
| Migration | [Upgrade](Packages/com.advanceduitoolkit.framework/Documentation~/MIGRATION.md) | [Upgrade](Packages/com.advanceduitoolkit.authoring/Documentation~/MIGRATION.md) |
| Testing | [Verification](Packages/com.advanceduitoolkit.framework/Documentation~/TESTING.md) | [Verification](Packages/com.advanceduitoolkit.authoring/Documentation~/TESTING.md) |
| Changelog | [Changes](Packages/com.advanceduitoolkit.framework/CHANGELOG.md) | [Changes](Packages/com.advanceduitoolkit.authoring/CHANGELOG.md) |

JSON layouts, stylesheets and integration records remain available under **Tools → Advanced UI Toolkit → Compatibility**. Start with the [compatibility documentation](Packages/com.advanceduitoolkit.authoring/Documentation~/INDEX.md#json-compatibility) when maintaining an existing JSON workflow.

## Samples and verification

Framework includes documented [Weapon Panel](Packages/com.advanceduitoolkit.framework/Samples~/WeaponPanel/README.md), [Settings Panel](Packages/com.advanceduitoolkit.framework/Samples~/SettingsPanel/README.md), [Station Libraries](Packages/com.advanceduitoolkit.framework/Samples~/StationLibraries/README.md) and [Code Generation](Packages/com.advanceduitoolkit.framework/Samples~/CodeGeneration/README.md) samples. Authoring includes [JSON prefab-layout examples](Packages/com.advanceduitoolkit.authoring/Samples~/PrefabLayouts/README.md).

## Contributing and license

See the [contribution guide](Packages/com.advanceduitoolkit.framework/CONTRIBUTING.md) for package boundaries, validation and issue reports. No `LICENSE` file is included in this source distribution.
