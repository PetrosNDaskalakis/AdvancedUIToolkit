# Advanced UI Toolkit Documentation

Choose the shortest path for your task:

| If you want to… | Read |
| --- | --- |
| Install and make a working panel | [Getting started](GETTING_STARTED.md) |
| Learn the normal developer workflow | [Developer handbook](DEVELOPER_HANDBOOK.md) |
| Use `UIBindingRoot`, `UIView`, and `UIEventScope` | [API reference](API_REFERENCE.md) |
| Organize multiple station designs | [Style profiles](STYLE_PROFILES.md) |
| Customize generated C# | [Custom code templates](CUSTOM_CODE_TEMPLATES.md) and [Preview](GENERATION_PREVIEW_096.md) |
| Avoid unwanted recompilation | [Generation modes](GENERATION_MODES.md) |
| Debug binding or prefab issues | [Troubleshooting](TROUBLESHOOTING.md) |
| Understand ownership and implementation | [Architecture](ARCHITECTURE.md) and [Technical overview](TECHNICAL_OVERVIEW.md) |
| Verify upgrades and benchmarks | [Test plan](TEST_PLAN.md) and [Performance](PERFORMANCE_AND_TESTING_097.md) |
| Upgrade from an `Assets/` install | [Migration guide](MIGRATION_TO_ADVANCED_UI_TOOLKIT.md) |
| Understand activation/pooling | [Lifecycle](LIFECYCLE_093.md) |
| Check safe prefab generation | [Prefab safety](PREFAB_SAFETY_094.md) |

## Importable samples

- **Weapon Panel — Buttons:** three generated button references and lifecycle-safe click listeners (`Samples~/WeaponPanel`).
- **Settings Panel — Controls:** slider, toggle, legacy input field, legacy dropdown and label (`Samples~/SettingsPanel`).
- **Station Style Libraries:** Main Menu / Base Station / Sonar style reuse (`Samples~/StationLibraries`).
- **Code Generation Template:** tailor C# layout with markers (`Samples~/CodeGeneration`).

Samples are source and guided scene construction; they do not include prebuilt `.unity` scenes or `.prefab` files. Package Manager imports examples into the **consumer project's** Assets, not the framework's `Runtime` assembly.

## Package boundaries

`Runtime/` is gameplay-safe code. `Editor/` contains UnityEditor code; player builds must not include it. `Tests/` contains Unity Test Framework assemblies. `Samples~/` and `Documentation~/` are supporting learning resources. **Generated bindings and game-specific style assets belong in the consuming project's `Assets/`, not inside the package.**

## v0.9.9 Release Candidate

- [Release candidate verification & exit criteria](RELEASE_CANDIDATE_099.md)

- [Simplified v0.9.10 workflow](UX_WORKFLOW_0910.md) — one Workspace, quick prefab creation, and optional advanced tools.

- [Migrating to Advanced UI Toolkit](MIGRATION_TO_ADVANCED_UI_TOOLKIT.md) — required changes for projects installed before the complete rename.
