# Advanced UI Toolkit — Foundation Improvement Roadmap

[← Documentation Index](INDEX.md) · [Binder XML Summaries](BINDER_XML_SUMMARIES.md)

## Goal and scope

**Goal:** make the existing Binder, Factory, code generator, and runtime APIs easier to use and safer to maintain before expanding the framework.

**Feature status:** per-property XML summaries and the small Editor-only Prefab Text Override convenience feature are already implemented. Preview 11 is the final planned foundation preview; it adds no new authoring features. No JSON layout schemas, reactive data binding, new widget families, runtime theme engine, or screen navigation system before the verified v1.1.0 stable release.

## Milestone 1 — Property documentation (v1.1 preview)

**Feature:** display an XML Summary field directly above each editable Public Property name in Binder.

- Store the text on the existing binding manifest and preserve it across scans and renames.
- Include the text in the read-only C# preview and both generators; blank uses the existing default.
- Escape invalid XML and make documentation-only edits count as pending work.
- Do not regenerate C# on each keystroke.

**Exit criteria:** a saved Load/Arm/Fire panel retains its three comments after reopening; tests cover default/templated output, escaping, and change detection.

## Milestone 2 — Binder and Factory workflow polish (no new features)

**Preview 3 implementation (pending Unity verification):** removed Binder's redundant root button, kept component and XML editor filtering in sync, improved empty-state messages and compact warnings, and corrected Factory quick-create naming and responsive rows. Preview 4 additionally corrects the selection workflow for ordinary UI GameObjects while keeping generation scoped to owning roots; see [Selection synchronization](SELECTION_SYNC_PREVIEW4.md). Remaining sign-off requires narrow/medium/wide Editor visual testing and the full three-button workflow.

- Eliminate redundant clicks and inconsistent labels between Binder and Factory.
- Keep root selection predictable when selecting controls, screens, or independent nested roots.
- Show pending changes and blocking errors concisely, with detailed messages on demand.
- Refine narrow dock layouts, property-field label widths, scroll behavior, and button placement.
- Ensure Empty State guidance tells a developer the next valid action.

**Exit criteria:** one clear path for a new user to create a 3-button panel, register its prefabs, generate bindings, and edit documentation without moving between duplicate settings; no clipped controls at supported dock sizes.

## Milestone 3 — Generation safety and consistency (no new features)

- Re-test creation, renaming, deletion, Undo, and Redo under both Manual and Automatic modes.
- Verify nested prefab scopes, inactive children, and ambiguous component discovery.
- Keep generated-file ownership checks, template validation, and source-edit protection consistent.
- Ensure failed file writes never leave the manifest partially committed.
- Verify comment-only changes cannot silently overwrite developer-modified source.

**Exit criteria:** every supported hierarchy operation either regenerates valid code or presents a precise, actionable blocking error; generated manifest/source mismatches are never silently accepted.

## Milestone 4 — Responsiveness and measured performance (no new features)

- Profile Binder scanning, diff construction, generation, preview cache, and Factory list painting.
- Use repeatable 25-, 100-, and 250-component scenes captured with Unity Profiler.
- Remove redundant rescans and UI allocations found by profiling; preserve correctness.
- Track scan/generate duration and C# recompilation count before and after optimization.

**Exit criteria:** no unnecessary file writes when a panel is unchanged; Profiler regressions require explanation; editor interactions remain usable on the largest test panel.

## Milestone 5 — Compatibility, manual verification, and developer documentation (no new features)

- Manually verify metadata persistence, generation modes, events, prefab validation, and Undo/Redo.
- Verify Play Mode lifecycle behavior and a clean player build in supported Unity versions.
- Verify package install, sample import, migration from the previous stable version, and GUID preservation.
- Align README, API reference, installation guide, examples, troubleshooting, and screenshot tutorials with the tested release.

**Exit criteria:** clean installation; all manual release gates passed; successful player build; documented migration/upgrade steps; no missing script references.

## Milestone 6 — Release and freeze

- Tag the next stable release only after Milestones 1–5 pass their Unity-side acceptance checks.
- Publish a concise changelog, validated sample tutorial, and measured compatibility matrix.
- Freeze the current APIs and maintenance workflow before evaluating future features.

**Exit criteria:** repeatable test evidence and a release package that matches the GitHub documentation.

## Out of scope until the base is stable

JSON-authored UI layouts, live data binding, additional runtime UI widgets, graph editors, runtime visual styling, theme animation systems, and other new subsystems remain on hold.

## Delivered convenience feature: Prefab Text Override

Preview 8 introduced the **Displayed Text** field in UI Library for one supported root/child text component. Preview 9 strengthened source-label identity checks and Preview 10 reduced unnecessary target-discovery work. See [Prefab Text Override](PREFAB_TEXT_OVERRIDE.md). Preview 11 closes the preview cycle; **RC1 is complete and the v1.1.0 stable-version package has been prepared; final Unity acceptance remains outstanding**. Do not add more feature previews unless a release-blocking issue is discovered. Phase 3 begins only after verified Stable and targets JSON schema authoring.
