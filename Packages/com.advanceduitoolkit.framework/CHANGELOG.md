## [1.1.5] — Component Extensibility (Editor-only)

- Added `AdvancedUIComponentRegistry` as a public, validated prefix-to-Component registry for external Editor packages.
- Binder, naming validation, and diagnostics share registered component types without replacing existing built-in prefixes.
- Registration changes invalidate Binder scan fingerprints and schedule a monitoring refresh.
- Added optional `AllowUnbound` for external single and batch **Custom** prefab creation; ordinary decorative names are accepted without exposing a generated accessor.
- Kept all existing default Library/Factory behavior, public Runtime API, serialized assets, and Unity GUIDs unchanged.
- All new C# declarations use explicit types; no bundled test or benchmark windows.
- Unity compilation and external consumer verification must be performed in the target project.

## [1.1.4] - Headless Binder Integration

- Added the public `AdvancedUIBindingIntegration` Editor facade: read-only binding inspection, safe noninteractive regeneration, and first-time binding initialization.
- Added explicit opt-in for removing generated accessors through external Editor clients.
- Added nestable automation suspension to avoid background C# updates during hierarchy transactions; batch creation uses the same suspension automatically.
- Preserved the compact Binder/Library UI, existing Runtime API, serialized manifests, and explicit C# type declarations.
- No bundled EditMode, PlayMode, or synthetic benchmarking tools. Unity compilation and consumer-package verification are required.

## [1.1.3] — Batch Creation Safety (Unity validation pending)

- Introduced the Editor-only `AdvancedUIBatchIntegration` public API with ordered container/prefab steps, stable step IDs and parent references, full preflight validation, and a map of created objects.
- Batch operations use one isolated Undo group. On failed creation, Unity Undo rolls back the group; the original selection is preserved. Source prefab text overrides still use source-component identity checks.
- Added an optional internal `joinUndoGroup` parameter to the existing Factory creation methods, leaving individual Library actions and their Undo behavior unchanged.
- The batch API does not generate C# bindings or suspend Binder automation; these are separate upcoming integration milestones.
- No changes to Runtime source, UI layout, existing Unity GUIDs, bundled tests, or benchmark tools. All new C# declarations use explicit types.
- Static audits are not Unity compilation or real Undo/Redo verification.

## [1.1.2] — Explicit C# Types (maintenance; Unity validation pending)

- Replaced 120 locally inferred `var` declarations across 16 production C# files with explicit C# type declarations, including local variables, `foreach`, `for`, `using`, and `out` declarations.
- Preserved the `"var"` literal in the reserved C# keyword validation table: it is an input value, not an inferred declaration.
- No changes to methods, execution order, generated bindings, serialized assets, editor layout, feature scope, or public APIs.
- This maintenance release follows 1.1.1. The subsequent batch-creation milestone will use a later version rather than reusing 1.1.2.
- Static package checks passed; actual Unity compilation and Edit/Play behavior must be validated in the target Unity project.

## [1.1.1] — External API Foundation (Unity validation pending)

- Added documented Editor-only `AdvancedUIEditorIntegration` public API for other packages.
- Added explicit Style Profile resolution, prefab-creation validation, linked instance creation, and safe single-label text overrides.
- Added container creation and source label discovery to the public integration API.
- External creation preserves the current Unity selection by default; interactive Library behavior is unchanged.
- Shared Factory preflight validation between its existing UI workflow and public integration calls.
- No Runtime changes, new windows, bundled testing assemblies or benchmark tools.
- Batch creation, automation suspension, headless Binder generation and custom binding adapters remain future milestones.

## 1.1.0 — Stable-version package prepared (Unity acceptance pending)

- Promoted the frozen RC1 source into the v1.1.0 package; **no Runtime, Binder, Factory, or other production C# changes**.
- Kept the compact distribution with no bundled EditMode/PlayMode suites, synthetic benchmarks, or Editor benchmark foldouts.
- Updated version metadata, installation and upgrade guidance, final acceptance checklist and release documentation.
- **Release gate:** static package validation has been performed; clean Unity compilation, manual workflows, player build, Profiler, and publication approval are not claimed. Do not label as certified until these are recorded.

## 1.1.0-rc.1 — Release candidate (compact distribution)

- Promoted the **working Preview 11 Compact** foundation to a release-candidate package without changing Binder, Factory, generated C#, Runtime APIs, or prefab behavior.
- Kept EditMode/PlayMode test assemblies, synthetic benchmark tools, and the Binder benchmark foldout out of the distributed package.
- Removed an orphaned `InternalsVisibleTo` Editor assembly declaration used only by the previously removed Editor test assembly.
- Aligned installation, manual verification, release gates, and package documentation with RC1, including a clear upgrade path from Preview 11 Compact.
- **Verification status:** static packaging checks only for this RC1 build. Developer reported Preview 11 Compact functioning in Unity; this is not a clean-install/player-build certification. Stable release remains gated on documented Unity verification and publication permission.

## 1.1.0-preview.11 — Final foundation stabilization and integration coverage (Unity validation pending)

### Preview 11 compact package cleanup

- Removed bundled EditMode and PlayMode suites and their assembly definitions to reduce package size.
- Removed synthetic performance benchmarks, the Binder performance foldout, and the Test Runner shortcut.
- Updated documentation and static audit; real performance analysis should use Unity Profiler.
- No changes to Runtime APIs, Binder/Factory authoring features, generation, or existing production Unity GUIDs.

- Fixed an Undo isolation inconsistency in the native Unity default-control fallback: creation now starts a separate Undo group **before** creating the control, just like styled prefabs and empty containers.
- Original Preview 11 (unbundled) added cross-tool EditMode regression tests reproducing Canvas → Panel → Body → Buttons parent and Footer, instantiating three linked buttons with independent displayed text and verifying Binder's strongly typed discovery and generated XML output.
- Original Preview 11 (unbundled) added collision/renaming integration coverage and an Undo/Redo isolation test for the regular native Button creation path.
- Maintained the existing UI Library and Binder workflow, prefab selection policy, instance-only text overrides, serialized manifests, and Runtime assembly without modification.
- Static package checks do not substitute for Unity compilation, manual workflow verification, sample import, actual editor profiling, and player builds. Preview 11 is **not** an RC or Stable release.

## 1.1.0-preview.10 — Factory text-target performance polish (Unity validation pending)

- Replaced allocation-heavy full target snapshots during source identity resolution and instance application with direct component traversal. Target ordering and the Preview 9 fail-closed identity check remain unchanged.
- Disabled **Override Displayed Text** now uses an availability check instead of allocating hierarchy paths, target objects and original strings on each Factory GUI repaint.
- Expanded opt-in synthetic performance diagnostics to include availability and source-identity lookup times across a configurable control hierarchy. These are microbenchmarks, not verified speedup or GC-allocation claims.
- Added EditMode regression coverage for empty/inactive discovery, traversal parity, foreign-source identity refusal, invalid application indices, and diagnostic metrics.
- Restored unchanged distribution resources omitted by the provided Preview 9 `.7z` snapshot (samples, documentation and publication checklist) to preserve a complete UPM installation; the official Preview 9 script and asset GUIDs remain unchanged; uploaded source was used for implementation review.
- Existing Runtime APIs, Binder, generated code, scene serialization and Style Profile formats are unchanged. Prior `.meta` files and GUIDs remain intact.
- Preview 9 Unity tests were reported as passing by the developer; Preview 10 must be compiled and tested separately.

## 1.1.0-preview.9 — Foundation stabilization (Unity validation pending)

- Factory's selected text target now retains its source Component identity rather than trusting a stale traversal index. Label reordering keeps the selected target; deletion or prefab replacement requires re-selection and blocks creation safely.
- Creation resolves the selected component's current source index before prefab instantiation; instantiation still uses the linked prefab and records instance-only text property modifications.
- Factory prefab and container creation begin an explicit Undo group rather than merging with unrelated user edits.
- Expanded EditMode regressions for source-label deletion/reordering, duplicate names, variants, nested prefabs, and Undo isolation. These tests are included but **have not been executed in Unity**.
- Clarified 1.1.0 release checklist and text override limitations. Existing `.meta` GUIDs, Runtime, Binder, generated C# and serialized style profiles are unchanged.
- Static package verification alone does **not** constitute a stable-release sign-off. See `Documentation~/STABILIZATION_PREVIEW9.md`.

## 1.1.0-preview.8 — Prefab Text Override

- Added an optional **Override Displayed Text** field in UI Library's Selected Component details.
- Supports serialized root, nested, and inactive legacy Unity Text controls and optional TMP text via reflection (no required TMP package).
- Multiple text targets use an explicit popup; each creation changes only one chosen label.
- Disabled overrides retain the original prefab text; enabled empty strings deliberately create blank labels.
- The Factory's existing prefab creation path records the label modification using Undo and prefab-instance overrides, without modifying the source asset or previously created instances.
- Selected quick-create actions use the pending label; unselected entries use their own original source text.
- Added eight EditMode scenarios plus one optional TMP test for text discovery, override isolation, validation and Undo behavior.
- Editor-only; Runtime APIs, generated bindings and existing Unity metadata are unchanged. Requires Unity compilation and test execution before stabilization.

## 1.1.0-preview.7 — Editor performance polish

- Cached Binder's display snapshot across unchanged Editor repaints. Hierarchy rescans, property edits, and switching roots invalidate it immediately.
- Reused the Binder search-row buffer rather than allocating a new list each repaint; resolved the selected authoring container once per list draw.
- Shared one effective Style Profile template list within a UI Library GUI pass, avoiding repeated inheritance walks without keeping stale profile data across frames.
- Bounded the high-resolution prefab preview cache to 64 entries, with least-recently-used eviction of completed captures; limited the capture queue to 16 pending jobs. The resolution remains 1024 × 512.
- Released CPU-side thumbnail pixel storage after fallback validation. High-resolution textures remain available for Editor drawing.
- Expanded the existing optional synthetic benchmark controls to 32, 128 and 256, and removed outdated report-version text.
- Added five EditMode regression tests for display-cache reuse, invalidation and scope changes.
- No changes to Runtime, serialized binding manifests, selection behavior, custom XML summaries, generated C# shape, or Factory controls.
- Requires Unity Editor compilation, EditMode execution and before/after measurements for performance acceptance; no speedup claim is made from static validation alone.

## 1.1.0-preview.6 — Generation reliability and recovery

- Rechecked the validated C# source immediately before replacement; external modifications or unexpected files block generation rather than being overwritten.
- Added recoverable generation commits: old generated source is backed up before replacement and restored if the serialized manifest cannot be committed; newly created source is removed if the initial manifest commit fails.
- Restored saved manifest entries and generation-profile values when persistence fails, without introducing Unity Undo entries.
- Skipped source writes and Unity imports when the output C# text is unchanged, while still saving reviewed manifest metadata.
- Added six EditMode tests covering failure rollback, unexpected external edits, stale first-generation output, no-op writes, and successful generation.
- No changes to Runtime, prefabs, public generated binding API, or UI component discovery. Requires Unity Editor compilation and EditMode test execution.

## 1.1.0-preview.5.1 — Factory compile hotfix

- Fixed `CS0103` in `UIComponentLibraryWindow.DrawSelectedTemplateDetails`: the `valid` and `reason` variables are now in scope for the inline validation message below the name field.
- No other UI workflow, Runtime, generated binding, or serialized metadata changes.
- Replaces `1.1.0-preview.5`; requires Unity Editor compilation verification.

## 1.1.0-preview.5 — Non-prefab container selection and creation

- Binder and UI Library share one selection rule: ordinary scene UI containers (Canvas, Panel, arbitrary RectTransform groups) are valid selections, but prefab instances and all their children are not authoring targets.
- Clicking a prefab under `Buttons parent` makes `Buttons parent` the active Factory parent, even when a different group was previously selected. Binder shows the container and keeps generation scoped to its owning binding root.
- Added a **Container** name field and **+ Create** action in UI Library to create editable non-prefab RectTransform groups such as Header, Body, Buttons parent, Toggles parent, and Footer without registering a prefab template.
- The existing **GameObject → Advanced UI Toolkit → RectTransform (Empty)** action now creates a regular container independent of Style Profiles. Regular Factory menu creation also uses the closest non-prefab parent.
- Newly created prefab instances cannot become the parent of later instances; nested prefab controls are still discovered and bound, not excluded from generated code.
- Added EditMode tests for prefab selection, prefab children, sibling groups, existing containers, Binder context, non-prefab creation and prefab assets.
- Existing Runtime and binding format unchanged. Unity compilation and tests still require verification.

## 1.1.0-preview.4 — Exact GameObject selection in Binder & UI Library

- UI Library now follows the **exact selected uGUI GameObject** (any `RectTransform`), including buttons, images and arbitrarily named child containers, instead of restricting automatic creation targets to panels/marked roots.
- UI Library labels the creation target **Parent** to distinguish where instances are created from a Binder manifest root. When the Factory selects an object it just created, the parent is retained so repeated Create actions remain siblings.
- Binder now shows **Selected** (the exact Hierarchy GameObject) separately from **Binding Root** (the scope of generated references). The selected control is highlighted in the list, including an image component nested under a selected prefab wrapper.
- The Binder → Factory shortcut respects the exact selected UI object. The Factory → Binder shortcut resolves the owning binding root, so generation stays safe.
- Non-uGUI scene objects and prefab assets are not valid Factory creation parents; the last valid UI parent is retained. The selected object still appears in Binder when appropriate.
- Updated selection regression tests, help text, and README. No changes to Runtime, code generation, manifest format, XML summaries, or custom templates.
- **Unity Editor compilation and actual EditMode test execution remain required.**

## 1.1.0-preview.3 — Binder / UI Library workflow refinement

- Removed Binder's redundant **Use Selected** button. Binder already tracks meaningful Hierarchy selections; explicit manual root selection remains possible through its Object field.
- Kept the selected screen's component list, search and scroll positions predictable when switching roots, and cleared stale "generated" feedback after hierarchy changes.
- Applied the same component search to the XML-summary/property editor and the regular component list; added clear-search controls in both Binder and UI Library.
- Removed duplicated blocking-error text around disabled Binder actions and clarified empty-state guidance.
- Factory quick-create actions now respect a valid custom instance name when that prefab is selected. Other prefab cards still use their own defaults, and invalid edited names block creation instead of silently falling back.
- Made Factory list rows expand with dock width rather than using a hard-coded pixel width. The selected prefab name is clickable in tile view.
- Kept a manually selected Library profile when opening Factory from a root with no explicit Style Context; a root with a Style Context still selects its intended profile.
- No new runtime systems, component types, serialized fields, template directives, or generation behavior changes.
- Source inspections and static package checks are not Unity compilation or EditMode test execution.

## 1.1.0-preview.2 — Binder hierarchy sync and duplicate prefab repairs

- Fixed Binder retaining a stale scan after adding or Ctrl+D-duplicating a prefab while public property/XML summary fields had ungenerated edits. Hierarchy rescans now merge the staged metadata by Unity Component identity.
- Corrected Unity-generated duplicate names such as `BTN_Fire (1)` to a unique, legal name such as `BTN_Fire_2` in interactive Binder scans. Existing `_2` names are skipped to avoid collisions; explicitly duplicated names without Unity suffixes still block generation.
- Manual Refresh preserves staged XML documentation instead of discarding it; clearing or reverting fields correctly resets staged-edit tracking.
- Disabled Preview and Generate controls now display the first blocking validation reason directly below them.
- Kept unattended automation inspection read-only. The interactive Binder owns duplicate-name normalization; background scans continue to report invalid source names until they are corrected.
- Added EditMode regression tests for duplicated controls, collisions, staged metadata on existing and new controls, deliberate conflicts, and refresh persistence.
- No Runtime API changes, binding-manifest schema changes, or new feature areas. Unity compile and test execution remain pending.

## 1.1.0-preview.1 — Per-property XML summaries & foundation polish

- Added a text field for each generated property's XML summary directly above its editable public property name in Binder.
- Custom summaries persist with each binding in `UIBindingRoot.Entry.XmlSummary` and survive hierarchy renames and rescans.
- Only generating writes the edited XML documentation to C#; changing a field does not trigger compilation.
- The default generator and the custom `ExampleScript.cs` template both honor per-property comments; blank fields keep existing template defaults.
- XML punctuation is escaped before generation; comment-only edits count as pending changes.
- Added Editor tests for XML escaping, default fallback, templates, staged edits, and manifest serialization.
- No new runtime UI behaviors, layout systems, or unrelated Editor features.

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
