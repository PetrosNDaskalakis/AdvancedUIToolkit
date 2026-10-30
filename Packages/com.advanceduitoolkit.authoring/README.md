# Advanced UI Toolkit — Layouts

**Authoring v0.5.0-preview.8** · Framework 1.2.0-preview.3 · Unity 2022.3 minimum

Layouts captures reusable container hierarchies from your Unity scene. Inspect the captured structure, save a mold and create independent layouts for new screens. Populate their containers with prefab controls from Framework's Library, then connect them to game code with Binder.

Open **Tools → Advanced UI Toolkit → Layouts**. **Create New Layout** captures a scene hierarchy; **Load Layout** opens a saved mold. The capture excludes entire prefab and non-RectTransform branches. External references, including fonts and other Unity assets, are cleared and reported. Supported components and internal references on retained containers are preserved. Read [Hierarchy molds](Documentation~/HIERARCHY_MOLDS.md) before choosing a source.

## Documentation

[Documentation index](Documentation~/INDEX.md) · [Installation](Documentation~/INSTALLATION.md) · [Getting started](Documentation~/GETTING_STARTED.md) · [Developer handbook](Documentation~/DEVELOPER_HANDBOOK.md) · [API reference](Documentation~/API_REFERENCE.md)

[Architecture](Documentation~/ARCHITECTURE.md) · [Technical overview](Documentation~/TECHNICAL_OVERVIEW.md) · [Troubleshooting](Documentation~/TROUBLESHOOTING.md) · [Migration](Documentation~/MIGRATION.md) · [Testing](Documentation~/TESTING.md) · [Changelog](CHANGELOG.md)

## Compatibility tools

Existing JSON layouts, saved setups and read-only generation-record previews remain under **Tools → Advanced UI Toolkit → Compatibility**. They use the same Authoring package; do not install the former standalone Integration package. See the [JSON compatibility reference](Documentation~/INDEX.md#json-compatibility) and [sample guide](Samples~/PrefabLayouts/README.md).

This is an Editor-only package with a preview version. Optional [EditMode tests](Tests~/README.md) are supplied; the [testing guide](Documentation~/TESTING.md) describes their coverage and verification limits.
