# Advanced UI Toolkit (A.U.T.)

**Build Unity uGUI screens with reusable prefabs, reusable layouts, and strongly typed C# bindings.**

A.U.T. is designed primarily for **Unity developers**. Its editor tools reduce repetitive UI setup while keeping the resulting hierarchy and gameplay code readable and editable. AI-assisted workflows can use the same APIs, but AI is not required.

## At a glance

| Tool | What it does |
| --- | --- |
| **Library** | Create UI controls from your saved prefab templates and style profiles. |
| **Layouts** | Capture a reusable container hierarchy as a mold, then create new independent layouts. |
| **Binder** | Map named UI components to generated, strongly typed C# properties. |
| **Runtime** | Resolve serialized references and manage UI event subscriptions through `UIView<TBindings>`. |

**Example workflow:** Create a panel → place prefab controls with Library → generate references with Binder → use `UI.Fire` in your view script. Optionally create the panel structure from a saved Layout mold.

## Packages

| Package | Version | Required? |
| --- | --- | --- |
| `com.advanceduitoolkit.framework` | `1.2.0-preview.4` | Yes — Binder, Library, runtime |
| `com.advanceduitoolkit.authoring` | `0.5.0-preview.9` | Optional — Layouts and compatibility authoring |

Unity **2022.3+** is declared in both package manifests. The packages use **uGUI**, not Unity UI Toolkit (UIElements). The Authoring package depends on the Framework package. These are **preview releases**; production readiness has not been established by independent field testing.

## Install in a Unity project

1. Copy `Packages/com.advanceduitoolkit.framework` into your Unity project's `Packages/` directory, including `.meta` files.
2. Optionally copy `Packages/com.advanceduitoolkit.authoring` beside it for Layouts.
3. Open Unity, let scripts compile, and use **Tools → Advanced UI Toolkit → Library / Binder / Layouts**.

Keep your own project's package manifest. Do not duplicate toolkit scripts under `Assets/` or install an obsolete standalone integration package.

## Start here

- [Overview](OVERVIEW.md) — problem, audience, capabilities and boundaries
- [Quick Start](QUICKSTART.md) — first UI screen, step by step
- [API Reference](API.md) — essential runtime and editor-facing entry points
- [Architecture](ARCHITECTURE.md) — packages, dependencies, generation and lifecycle

Advanced reference material: [`Document~/`](Document~/). Sample projects and package changelogs remain inside their respective package directories.

## Validation and limitations

A.U.T. includes sample workflows and test sources. You can run the Framework's static package check from the repository root:

```sh
python3 Packages/com.advanceduitoolkit.framework/Tools~/verify_package.py
```

This is not a substitute for Unity compilation, EditMode tests, manual UI testing or player-build verification. Layout molds deliberately omit prefab branches; add controls through Library afterward. Legacy JSON tools are kept under the **Compatibility** menus.

## License

No `LICENSE` file was present in the supplied archive. Do not assume an open-source license until one is added by the repository owner.
