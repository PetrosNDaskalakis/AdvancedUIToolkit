## 1.0.0-rc.13 — Nested uGUI components in prefab templates

- Factory accepts prefab templates whose required control type is on a child rather than the prefab root.
- Binder resolves the named wrapper to its own component or a unique matching descendant, including inactive children, and ignores independently nested binding roots.
- Ambiguous descendant matches are reported rather than bound arbitrarily.
- Project diagnostics, the component Library validator, and docs follow the same rule.
- Added regression tests for nested Image binding, prefab instantiation, and diagnostics.
- No runtime API, manifest format, or existing Unity `.meta` GUID changes.

## 1.0.0-rc.12 — Complete Advanced UI Toolkit source migration

- Migrated every included runtime, editor, test, and sample script's old framework identifiers.
- Renamed branded script files, assembly definitions, generated C# template markers, package ID, and Editor preference keys.
- Preserved Unity `.meta` GUID data for all renamed scripts and assembly definitions.
- Updated documentation and repaired documentation links; added migration and release verification guides.
- Added a companion migration utility for consumer project `Assets` scripts and folder trees.
- **Breaking change:** existing gameplay scripts and asmdef references must be migrated.
- Static audits are not a substitute for Unity compilation, EditMode/PlayMode tests, or player builds.

## [1.0.0-rc.10] — Advanced UI Toolkit brand refresh

- Renamed Unity-facing menus, package display name, messages and guides to **Advanced UI Toolkit**.
- Preserved the `AdvancedUIToolkit` C# namespace, `.asmdef` identifiers, `com.advanceduitoolkit.framework` package ID, generated template markers, preference keys and Unity script GUIDs.
- New UI roots and component libraries default to `Assets/AdvancedUIToolkit/` output directories. Existing generated files and saved generation locations are unchanged.
- No changes to binder functionality or runtime behavior beyond visible log text.

## [1.0.0-rc.9] — Release documentation & acceptance audit

### Changed
- Consolidated v1.0 onboarding around the current **Binder** and **Factory** windows; removed obsolete top-level tab instructions from the primary guides.
- Added explicit clean-install, Undo/Redo, editor UX, player-build, and test-run release gates.
- Strengthened the portable package audit and made its expected version derive from package metadata rather than a stale hard-coded RC number.
- No changes to Runtime, generated binding C# format, UI style assets, or the existing Unity script GUIDs.

### Verification
- Portable static package checks run in Python. Full compilation, Editor UX testing, EditMode/PlayMode tests, and a player build must be performed in Unity before stable sign-off.

## [1.0.0-rc.8] — Undo/Redo binding synchronization

### Fixed
- Automatic binding generation no longer records derived manifest changes as separate Unity Undo operations. This keeps Ctrl+Z available for restoring a deleted UI control or prefab instance.
- Undo/Redo events schedule an additional deferred hierarchy rescan after Unity restores objects.
- Pending rescan requests are retained while another binding-generation pass is in progress.
- Automatic mode can re-add an accessor when a deleted UI control is restored, provided its generated source still matches the saved manifest and passes ownership checks.

### Tests
- Added EditMode regression tests for delete → manifest update → Undo → newly detected binding and for retained generation-profile metadata.

### Compatibility
- No Runtime API, generated-source format, Editor menu or Style Profile changes.
- Original Unity script GUIDs are retained; only Editor source and one new test are changed.
- Unity compilation and actual Undo/Redo synchronization must still be verified in a Unity project.

## 1.0.0-rc.7 — UI Library selection & preview simplification

- Removed the Visual State selector and alternate-state preview rendering. Component thumbnails now use their normal prefab appearance only.
- The UI Library follows newly selected roots in the Hierarchy automatically. The manual **Use Selected** button is removed.
- Selecting an ordinary control or creating a prefab from the Factory keeps the established creation root. Selecting a different screen/panel or independent nested UI scope switches it.
- When a selected root has a style context, the Library switches to its associated Style Profile.
- Added EditMode regression tests for automatic root selection and nested/sibling UI scopes.
- No runtime API or generated binding-format changes.

## [1.0.0-rc.5] - 2026-10-26

### Editor usability
- Binder now groups warnings into an expandable summary, limits tall component lists to seven rows, and keeps generation controls directly below the content.
- Binder's advanced settings remain on the same scrollable page rather than replacing the primary binding workflow.
- UI Library now uses a full-width library picker at every dock size, flexible prefab rows/cards and a compact selected-prefab area.
- Style Profile validation reports a short issue count before developers request individual messages; profile editing remains under Show More.
- Preserved Runtime source, existing generated binding format, stable script GUIDs, and manual/automatic generation behavior.

### Verification
- Static package checks only; test IMGUI layout in both narrow Inspector docks and wide standalone windows using Unity.

## [1.0.0-rc.2] - 2026-10-26

### Fixed
- Deleted uGUI components and prefab instances now queue removal of stale generated accessors.
- The last bound component can be deleted: generating leaves an empty, valid bindings class and empty manifest.
- Automatic mode regenerates removals after ownership and source-integrity checks; Manual mode queues them until confirmed.
- Both the Workspace and Automation batch generation show breaking-API warnings for removed accessors.

### Limitations
- Code consuming a deleted accessor must be updated before the project can compile successfully.
- Automatic monitoring only covers editable scene roots and the open Prefab Stage; other prefab assets must be opened for editing.

## [1.0.0-rc.1] — Release-readiness candidate

- Corrected Workspace selection of controls inside independent nested binding roots.
- Added four EditMode regression cases covering nested roots, regular child controls, unrelated objects and independent roots.
- Added explicit 1.0 release gates and compatibility notes.
- Runtime APIs, existing Unity GUIDs, generated bindings and Factory behavior are unchanged.
- **Status:** static verification only; Unity compilation, EditMode/PlayMode tests, and player-build validation remain necessary.

## [0.9.10] — UI authoring experience polish

- Replaced the Workspace's four top-level tabs with a single **Build & Bind** flow: select root, review components, preview C#, and generate.
- Moved generation settings, automation, diagnostics and benchmarks into optional collapsed Advanced sections.
- Simplified the Factory's everyday gallery and moved profile editing/validation into **Manage**.
- Improved selection behavior when Factory creates objects and added readable default generated class names from UI root names.
- Continued tracking renamed GameObjects as pending manifest changes even when generated API names stay stable.
- Added EditMode tests for root selection, safe preview, suggested names and preserved-API renames.
- Retained all existing Runtime/Editor script GUIDs; no generated C# or serialized profile migration required.

# Changelog

## [0.9.9] - 2026-10-26 — Release Candidate

### Added
- `Tools~/verify_package.py` standard-library audit for UPM metadata, asmdefs, sample paths, meta GUID uniqueness, and obvious non-portable assembly references.
- EditMode release-candidate tests for style-profile inheritance, cyclic profile safety, and deterministic documented code generation.
- `Documentation~/RELEASE_CANDIDATE_099.md` with clean installation, prefab, generated-code, migration, test-runner, and player build release gates.

### Unchanged
- No changes to Runtime or Editor production `.cs` files, existing serialized fields, Unity script GUIDs, generated binding APIs, UI menus, or Manual generation behavior.

### Verification
- Static package validation passed. No Unity Editor compilation, automated test execution, or player build has been performed in this environment. Treat as an unverified release candidate.

---


## [0.9.8] - 2026-10-23

### Added
- A documentation index, beginner quick start, developer handbook, runtime API reference, style profile guide, troubleshooting guide, and team onboarding checklist.
- Importable **Settings Panel — Controls** example demonstrating slider, toggle, legacy InputField, legacy Dropdown, Text and managed UI event callbacks.
- Importable **Station Style Libraries** guide for station-specific prefab profiles and inheritance.
- Expanded Weapon Panel sample instructions and fully documented example classes.
- Expanded Package Manager sample registration and an updated Unity verification matrix.

### Changed
- Reorganized README around the actual v0.9.8 editor workflow and current APIs.
- Corrected architectural lifecycle information that was stale after v0.9.3.
- Updated `package.json` version and documentation; clarified Unity compatibility and limits.

### Unchanged
- No changes to existing Runtime/Editor C# sources, Unity script GUIDs, generated binding schema, or serialized `UIBindingRoot` / style profile data.

### Verification
- Static source/package/documentation checks only. Unity Editor compilation, sample hierarchy assembly, import and test execution remain to be verified in the consuming project.

---

## [0.9.7] - Automated Testing & Performance

- Added an opt-in Performance tab to the consolidated Workspace (no extra top-level menu).
- Added repeatable microbenchmarks for cached binding lookups, cache rebuilds, and event scope suspend/resume.
- Reports medians across five samples and Gen0 collection counts; no timing promises or hard budgets.
- Added stress tests for 128 reference entries, destroyed references, wrong types, 200 event cycles, and 40 pooled-view cycles.
- No changes to v0.9.6 runtime source, generated code format, or manual-generation settings.

## [0.9.6] - Generation review and documentation polish

- Added read-only generated C# preview with Proposed, Changes, and Current tabs.
- Added a line-based diff and Copy action; preview writes no source files.
- Added optional `// <ADVANCEDUITOOLKIT:PROPERTY_DOC>` custom XML summary formatting to `ExampleScript.cs`.
- XML-escaped generated summaries, including UI names containing XML punctuation.
- Preserved the default v0.9.5 generator shape and Manual/Automatic modes.
- Added editor regression tests and documentation for preview and custom summaries.

## [0.9.5] - Deferred Code Generation

- Replaced the automatic-generation checkbox with an explicit **Manual / Automatic** generation mode in Workspace > Automation.
- In Manual mode, scanning and diagnostics continue without writing any generated C# source until the developer clicks **Generate Pending Changes**.
- Added bulk generation of validated safe changes, using Unity's asset-editing batch to reduce repeated asset imports.
- Kept individual generation and the existing breaking-change protections.
- Preserved the previous auto-generation preference when upgrading, so existing opt-in behavior is not silently changed.
- Explained that Unity's global Auto Refresh preference controls unrelated C# imports and compilations independently.

## [0.9.4] - Prefab Safety & Validation

- Shared Editor safety checks for nested binding scope ownership, missing references, incorrect types, and duplicate stable IDs / generated accessor keys.
- Warning for bindable controls inside nested prefabs without their own UI binding root.
- Block direct binding generation on prefab instances, requiring the source prefab to be opened in Prefab Mode (including dedicated prefab variants).
- Protect generated C# output paths against collisions with unrelated UI roots and accidental destination changes.
- Stage source writes before mutating serialized binding manifests; preserve previous references if the write fails.
- Diagnose duplicate API keys and stable IDs even when the referenced component has been deleted.
- Add focused EditMode regression tests and `Documentation~/PREFAB_SAFETY_094.md`.
- Keep runtime source and serialized field names unchanged; no binding regeneration required.

## [0.9.3] - UI Lifecycle Improvements

- Added optional view lifecycle hooks: `OnViewInitialized`, `OnViewEnabled`, `OnViewDisabled`, `OnViewDestroyed`.
- Preserved `OnInitialize`, `OnDispose`, the `UIView<T>` generic constraints, and generated accessor format.
- Added reversible `UIEventScope.Suspend()` / `Resume()` to prevent UI clicks while views are disabled and avoid duplicate subscriptions after pooling.
- Added EditMode scope tests and PlayMode view/pooling lifecycle tests.
- Added lifecycle reference documentation and manual migration tests; no regeneration needed.
- Behavior change: only subscriptions registered through `Events` automatically pause while a view is disabled.

## [0.9.2] - Stabilization

- Added non-throwing `UIBindingRoot.TryGet<T>` and post-deserialization cache invalidation.
- Added scoped event listeners for Toggle, Slider, Scrollbar, legacy InputField and Dropdown.
- Added EditMode regression tests for lookups and listener cleanup.
- Improved runtime XML documentation while preserving serialized field names and existing script GUIDs.
- No changes to generated binding output, UI menus, or saved prefab/style structures.

---


## 0.9.1 — Pre-1.0 polish

- Consolidated AutoBinder, change automation and diagnostics into one **Workspace** window.
- Kept Component Library / Factory as a separate Editor window.
- Added XML summaries to runtime, Editor, sample and test members; generated bindings now include XML documentation.
- Added optional, per-root `ExampleScript.cs` templates with two explicit insertion markers.
- Added template validation, safe source preservation and generation tests.
- Kept serializable manifests and existing `UIView<T>` APIs unchanged.
- Automatic generation requires manual review when the example template changes.

## [0.9.0] - 2026-10-23
- Reorganized v0.8.2 framework into a Unity Package Manager package.
- Added Runtime/Editor assembly definitions and explicit uGUI dependency.
- Added Editor tests and a sample import folder.
- Kept generated binding scripts and application style profiles under project Assets.
- Added output-directory guard to prevent writing generated code into Packages or external paths.
- Documented safe migration, code architecture, known limitations, and IP review.

## [0.8.2] - 2026-10-23
- Component preview renderer and preview caching (carried over unchanged).

## [0.8.1] - 2026-10-23
- Responsive Component Library window (carried over unchanged).

## [0.7.0] - 2026-10-22
- Component libraries and style profiles (carried over unchanged).
