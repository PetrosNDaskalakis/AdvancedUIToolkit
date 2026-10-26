# UI Core v0.9.1 — Pre-1.0 workflow polish

**Status: pre-release / experimental. Unity compilation and upgrade testing are still required.**

UI Core is an editor-first, uGUI-specific toolkit: automatic UI component discovery by
name, serialized reference manifests, generated typed accessors, safe rename assistance,
auto-validation, custom prefab factories, hierarchical style profiles, and component previews.
The runtime has **no reference to any game-specific assembly** in the supplied source.

## Requirements

- Unity 2022.3 LTS or newer **targeted**, not yet tested across versions.
- Unity UI/uGUI (`com.unity.ugui`). The package declares a `1.0.0` dependency for
  2022.3 compatibility; Unity 6 normally provides its built-in compatible version.
- Git is required only for Git URL installs, not local or embedded installs.

## Install: local package for development

1. Extract this entire folder anywhere **outside `Assets/`**.
2. Open Unity > Window > Package Manager > **+** > **Add package from disk**.
3. Select this folder's `package.json`.
4. Check the Console, then open **Tools > UI Core > Workspace**. The three tabs are Bindings, Automation and Diagnostics.
5. Open **Tools > UI Core > Factory** for reusable components and style libraries.
5. For editing package code alongside a Unity project, use an **embedded package**
   by placing this folder at `YourProject/Packages/com.uicore.framework/`.

## GitHub install (after publication approval)

1. Put the *contents of this folder* at the root of your Git repository.
2. Create and push a release/tag, e.g. `v0.9.0`.
3. In Unity Package Manager use **Add package from git URL**:

   `https://github.com/OWNER/REPOSITORY.git#v0.9.0`

This URL is a **template**, not a live repository. There is no public GitHub repo yet.

## Quick start

1. In a scene, create a Canvas and UI panel.
2. Use **GameObject > UI Core > Button** to create `BTN_Arm`, `BTN_Fire`, `BTN_Load`.
3. Select panel root; run **Tools > UI Core > Workspace > Bindings**.
4. Generate a class named `ScreenBindings`, namespace `Game.UI.Generated`.
5. Generate under **Assets/UICore/Generated** (project code, not within the package).
6. Implement a view using `UIView<ScreenBindings>` (see `Samples~`).
7. Customize station UI with **UIStyleProfile** assets and the Component Library.

## Unified Editor workflow

Only two top-level Editor windows are exposed:

- **Tools > UI Core > Workspace:** Bindings, Automation and Diagnostics tabs in one dockable window.
- **Tools > UI Core > Factory:** Component Library and prefab creation, separate from binding management.

The `GameObject > UI Core` creation menu continues to work. Prior internal methods that open AutoBinder, Automation or Diagnostics now navigate to the matching Workspace tab.

## Custom C# generation templates

The **Bindings** tab has an optional **Example Script** selector. Choose a compilable C# script containing a namespace, a class implementing `IUIBindings`, an `Initialize(UIBindingRoot root)` method, and exactly two comments:

```csharp
// <UICORE:PROPERTIES>
// <UICORE:ASSIGNMENTS>
```

Place the properties marker inside the class and the assignments marker inside `Initialize`.
The generator retains the example's imports, indentation, helper code and documentation. It substitutes the chosen namespace/class name and expands the markers with documented cached accessors and lookups. The example is **read-only**; its source is never overwritten.

To try it, import the **Code Generation Template** sample from Package Manager, or copy `Samples~/CodeGeneration/ExampleScript.cs` under your project's `Assets`. Assign the resulting `MonoScript` asset in Workspace > Bindings.

Use **Set Default** to apply the example to newly configured roots, or override it per root. Each UI root remembers its template path. When the example changes, auto-generation blocks until a manual review/regeneration. Generated code always writes under `Assets/`, never into package source.

**Template limits:** This is a controlled insertion-point template, not arbitrary C# parsing or automatic rewriting of game logic. Custom classes must remain valid C# independently. The template contains the initialization method that receives generated assignments.

## Code ownership & publication

**Do not publish publicly until the project's ownership, third-party assets, license,
and company approval are confirmed.** No open-source license is granted by this
archive. See `PUBLICATION_CHECKLIST.md`.

## Technical docs

- `Documentation~/TECHNICAL_OVERVIEW.md` — manager-oriented technical outline
- `Documentation~/ARCHITECTURE.md` — code architecture and data flows
- `Documentation~/MIGRATION_FROM_ASSETS.md` — critical GUID-safe migration steps
- `Documentation~/TEST_PLAN.md` — Unity verification matrix

## Upgrading from v0.9.0

Back up your project and preserve existing `.meta` identifiers. Replace the package source with v0.9.1 (or use the source-only upgrade patch). Your existing `UIView<T>`, binding manifest, and generated class names remain compatible. Open the **Workspace > Bindings** tab and regenerate each screen **once** to adopt the XML-documented generator output. This is a deliberate, manual migration; the automation monitor will not overwrite files that differ from its expected generated content.

Run the included EditMode tests and the prior button/prefab/rename tests before treating this release as stable.

## Known limitations

- uGUI only; not a full runtime data-binding system or UI layout engine.
- No schema/JSON-driven UI creation yet.
- Code-gen still requires explicit binding generation profile per UI root.
- Nested binding roots, prefab variants, and source-code renames require careful testing.
- Thumbnail rendering is best-effort and may fail for custom shaders.
- Component preview quality and full UPM migration require testing inside Unity.
