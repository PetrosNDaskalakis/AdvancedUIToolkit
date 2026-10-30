# Advanced UI Toolkit — Framework

**v1.2.0-preview.3** · Unity 2022.3 minimum · uGUI

Framework provides prefab-based UI authoring, typed component bindings and lifecycle-managed UI events.

- **Tools → Advanced UI Toolkit → Library** browses saved prefab templates, with search, kind filters, inherited entries and a persistent target container.
- **Tools → Advanced UI Toolkit → Binder** discovers named components, reviews changes and generates C# accessors.
- `UIBindingRoot`, `UIView<TBindings>` and `UIEventScope` connect the resulting screen to game code.

Install this directory as an embedded package under `Packages/com.advanceduitoolkit.framework`, or select its `package.json` with Package Manager's **Add package from disk**. Keep project-specific prefabs, Style Profiles, scenes and generated classes under `Assets`.

The optional [Authoring package](../com.advanceduitoolkit.authoring/README.md) adds **Layouts** for reusable container hierarchies. Framework itself has no dependency on Authoring.

## Documentation

[Documentation index](Documentation~/INDEX.md) · [Installation](Documentation~/INSTALLATION.md) · [Getting started](Documentation~/GETTING_STARTED.md) · [Developer handbook](Documentation~/DEVELOPER_HANDBOOK.md) · [API reference](Documentation~/API_REFERENCE.md)

[Architecture](Documentation~/ARCHITECTURE.md) · [Technical overview](Documentation~/TECHNICAL_OVERVIEW.md) · [Troubleshooting](Documentation~/TROUBLESHOOTING.md) · [Migration](Documentation~/MIGRATION.md) · [Testing](Documentation~/TESTING.md) · [Changelog](CHANGELOG.md)

## Samples

Import samples through Package Manager, or read the bundled guides: [Weapon Panel](Samples~/WeaponPanel/README.md), [Settings Panel](Samples~/SettingsPanel/README.md), [Station Libraries](Samples~/StationLibraries/README.md) and [Code Generation](Samples~/CodeGeneration/README.md).

This package retains its preview version. See [Testing](Documentation~/TESTING.md) for verification scope and Unity checks.
