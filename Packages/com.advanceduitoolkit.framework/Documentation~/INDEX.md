# Advanced UI Toolkit Documentation

Choose the shortest path for your task:

| If you want to… | Read |
| --- | --- |
| Install and make a working panel | [Getting started](GETTING_STARTED.md) |
| Learn the normal developer workflow | [Developer handbook](DEVELOPER_HANDBOOK.md) |
| Use `UIBindingRoot`, `UIView`, and `UIEventScope` | [API reference](API_REFERENCE.md) |
| Organize multiple station designs | [Style profiles](STYLE_PROFILES.md) |
| Create UI from another Editor package | [External Editor API](EXTERNAL_EDITOR_API.md) and [Batch Creation API](BATCH_CREATION_API.md) |
| Customize generated C# | [Custom code templates](CUSTOM_CODE_TEMPLATES.md) |
| Avoid unwanted recompilation | [Generation modes](GENERATION_MODES.md) |
| Debug binding or prefab issues | [Troubleshooting](TROUBLESHOOTING.md) |
| Understand ownership and implementation | [Architecture](ARCHITECTURE.md) and [Technical overview](TECHNICAL_OVERVIEW.md) |
| Verify upgrades and profiling | [v1.1.0 release notes](V1_1_STABLE_RELEASE_NOTES.md) and [final acceptance](V1_1_STABLE_ACCEPTANCE.md) |
| Upgrade from an `Assets/` install | [Migration guide](MIGRATION_TO_ADVANCED_UI_TOOLKIT.md) |
| Understand activation/pooling | [API Reference](API_REFERENCE.md) |
| Check safe prefab generation | [Architecture](ARCHITECTURE.md) |

## Importable samples

- **Weapon Panel — Buttons:** three generated button references and lifecycle-safe click listeners (`Samples~/WeaponPanel`).
- **Settings Panel — Controls:** slider, toggle, legacy input field, legacy dropdown and label (`Samples~/SettingsPanel`).
- **Station Style Libraries:** Main Menu / Base Station / Sonar style reuse (`Samples~/StationLibraries`).
- **Code Generation Template:** tailor C# layout with markers (`Samples~/CodeGeneration`).

Samples are source and guided scene construction; they do not include prebuilt `.unity` scenes or `.prefab` files. Package Manager imports examples into the **consumer project's** Assets, not the framework's `Runtime` assembly.

## Package boundaries

`Runtime/` is gameplay-safe code. `Editor/` contains UnityEditor code; player builds must not include it. `Samples~/` and `Documentation~/` are supporting learning resources. **Generated bindings and game-specific style assets belong in the consuming project's `Assets/`, not inside the package.**


## Release and migration

- [v1.1.0 release notes and upgrade guidance](V1_1_STABLE_RELEASE_NOTES.md)

- [Migration to Advanced UI Toolkit](MIGRATION_TO_ADVANCED_UI_TOOLKIT.md)
- [Release verification checklist](V1_1_RELEASE_GATES.md)

## Foundation development

- [Per-property XML summaries](BINDER_XML_SUMMARIES.md) — author XML comments from Binder above each generated property name.
- [Foundation improvement roadmap](FOUNDATION_ROADMAP.md) — UX, safety, performance, and release gates; no additional features planned.

- [Preview 11 integration stabilization](STABILIZATION_PREVIEW11.md) — final preview, native-control Undo isolation and realistic end-to-end test coverage.
- [Final acceptance checklist](V1_1_STABLE_ACCEPTANCE.md) — manual sign-off for Unity workflows, Profiler captures and player builds. [RC1 evidence template](RC1_EVIDENCE_CHECKLIST.md) is retained as release history.
- [Preview 10 performance polish](PERFORMANCE_POLISH_PREVIEW10.md) — direct target lookup and disabled-override repaint improvements.
- [Preview 9 stabilization](STABILIZATION_PREVIEW9.md) — source-label identity, Undo isolation, regression coverage.
- [Prefab Text Override](PREFAB_TEXT_OVERRIDE.md) — Editor-only label editing before instantiating registered prefabs.

- [Headless Binder API (v1.1.4)](HEADLESS_BINDER_API.md)

- [Component extensibility — v1.1.5](COMPONENT_EXTENSIBILITY_API.md)
