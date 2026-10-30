# Framework changelog

## 1.2.0-preview.3

- Added Library search/kind/inherited filtering controls, matching/available counts and empty-state recovery.
- Preserved search, kind and list/tile choices through ordinary domain reload.
- Added scene/hierarchy target context, Locate and follow/pinned target presentation.
- Hid selected-entry details while filtered out without discarding transient edits.
- Applied consistent creation guards during compilation, import and Play Mode.
- Updated the static package audit for the Library/Binder menus and official documentation.
- Consolidated the user guides and documented the matching Authoring/Layout workflow. Package source and versions are unchanged by the documentation cleanup.

## 1.2.0 preview series

- Established separate Library, Layouts and Binder responsibilities.
- Kept Framework independent of optional Authoring; moved layout input/state out of Library.
- Preserved runtime APIs and serialized identities during Editor navigation changes.

## Earlier API milestones

| Version | Change |
| --- | --- |
| 1.1.5 | Public component extensibility and decorative/unbound prefab support |
| 1.1.4 | Headless Binder inspection, initial generation and automation suspension |
| 1.1.3 | Batch creation validation, grouped Undo and rollback |
| 1.1.2 | Explicit C# type declarations |
| 1.1.1 | Public Editor creation facade |
| 1.1.0 series | XML summaries, hierarchy synchronization, selection safety, prefab text overrides and generation reliability |
| 1.0.0 release-candidate series | Advanced UI Toolkit naming/namespace migration and composite prefab component discovery |
| 0.9 series | View lifecycle hooks, prefab safety, deferred generation and custom-template improvements |

Version history describes source changes, not a Unity certification record. See [Testing](Documentation~/TESTING.md) for verification scope and [Migration](Documentation~/MIGRATION.md) for upgrade requirements.
