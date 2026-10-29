# Preview 9 — Foundation stabilization

> **Historical release note:** The compact Preview 11 package does not include the synthetic benchmark UI/scripts or bundled EditMode/PlayMode tests mentioned below. Use [Unity Profiler and manual verification](PERFORMANCE_AND_TESTING.md) instead.

[← Documentation Index](INDEX.md) · [Text Override](PREFAB_TEXT_OVERRIDE.md) · [v1.1 release gates](V1_1_RELEASE_GATES.md)

**Package version:** `1.1.0-preview.9` · **Required base for patch:** `1.1.0-preview.8` · **Status:** source review and static validation only; Unity validation outstanding.

## Why this preview exists

In Preview 8, choosing the first of two text components and then deleting or reordering that label could redirect the override to a different component. Preview 9 retains a reference to the selected **source** `Component` and verifies its presence on the saved prefab immediately before instantiation. The current index is resolved only at creation time. A missing or replaced target blocks creation; the UI offers explicit reselection. Identical label names, inactive children, prefab variants and nested prefabs are included in targeted tests.

The internal index-only request is retained for existing non-UI internal tests/callers; only the UI Library's component-identity path offers edit-safety guarantees. There is no new runtime data-binding feature or migration of serialized bindings.

The Factory also starts a fresh Undo group before creating either a linked prefab or an empty UI container. This is intended to prevent a single Undo from removing prior unrelated edits; verify actual Unity behavior using the new Undo isolation tests.

## Changed files from Preview 8

| File | Change |
| --- | --- |
| `Editor/UIPrefabTextOverride.cs` | Source-component validation, resolved index and single instance discovery |
| `Editor/UIComponentLibraryWindow.cs` | Remember selected component, guard stale selections and require explicit retargeting |
| `Editor/UIStyleFactory.cs` | Resolve targets pre-instantiation, separate Undo groups, document public-facing creation behavior |
| `Tests/Editor/AdvancedUIToolkitPrefabTextOverrideTests.cs` | Six extra EditMode tests, including variant/nested and Undo regressions |
| `package.json` | Preview 9 version |
| `README.md`, `CHANGELOG.md`, `Documentation~/PREFAB_TEXT_OVERRIDE.md`, `Documentation~/DEVELOPER_HANDBOOK.md`, `Documentation~/INDEX.md` | Version, semantics and navigation |
| `Documentation~/STABILIZATION_PREVIEW9.md`, `Documentation~/V1_1_RELEASE_GATES.md` | New release notes and release checklist |

All pre-existing `.meta` files retain their original bytes, including GUIDs. No Runtime file, existing binder manifest, generated binding source, or style profile is modified.

## Verification in Unity

1. Install the full package in a **clean Unity 2022.3 LTS project**, then run EditMode and PlayMode tests. Repeat on your actual production Unity version.
2. Try a two-label prefab: select first label, delete it in the prefab asset, return to Factory. Creation must be blocked rather than modifying the second label. Explicitly reselect and create again.
3. Reorder two identically named labels, test a prefab variant, and test a nested prefab text target. Only the selected text component should change; source assets and sibling instances must remain untouched.
4. Perform an unrelated Inspector edit immediately before Factory creation. Undo creation and check the unrelated edit remains; Redo restores the exact instance name, parent, prefab link and text. Repeat for an empty container.
5. Save and reopen the scene; inspect persisted text modifications on created instances. With TMP installed, repeat on a saved TMP prefab, reimport and reload. Also compile/test without TMP installed.
6. Profile Factory target selection and creation with the Unity Profiler, tracking CPU time **and GC allocated bytes**, before making speed claims. Baseline data from Preview 8 is required.
7. Complete all gates in [the 1.1 release checklist](V1_1_RELEASE_GATES.md), including player builds and migration from the existing project.

`Tools~/verify_package.py` checks portable packaging and GUID shape, **not** Unity C# compilation or test behavior. Any observed Unity compile/test failures should be fixed in another preview before naming a stable release.
