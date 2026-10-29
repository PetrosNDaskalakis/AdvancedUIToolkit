# Preview 6 — Reliable generated bindings

[README](../README.md) · [Foundation roadmap](FOUNDATION_ROADMAP.md) · [Troubleshooting](TROUBLESHOOTING.md)

## Purpose

Keep a generated C# binding class and its serialized `UIBindingRoot` manifest in agreement when Editor generation encounters a write error, external source edit, or failed serialized commit. This preview improves the **existing** pipeline; it adds no new feature system.

## Generation safeguards

1. Binder validates the hierarchy, naming, generated file ownership, previous source content and template.
2. Before writing, it checks that the generated file still matches the exact source observed during validation. Unexpected edits or unexpectedly created/deleted files block the operation.
3. Changed C# is written to a staging file, and the old C# file is backed up before replacement.
4. The matching binding manifest and generation profile are committed after staging/replacing the source.
5. If manifest commit fails, Advanced UI Toolkit restores the old source (or deletes a newly created source) and restores previous manifest/profile values where possible.
6. When proposed C# equals the previous source, the Editor saves applicable manifest changes without rewriting or importing the file again.

## Limitations

- This is **best-effort failure recovery inside one Editor operation**, not a disk/Unity atomic transaction. A power loss, Editor crash, disk failure, or scene not saved can still require manual recovery.
- A backup is intentionally retained if filesystem rollback itself fails. Check the error message for its location.
- Generated source files remain outside Unity's Undo history. Automatic mode rescans on Undo/Redo; it does not undo file writes directly.
- If generated accessors are intentionally deleted/renamed, scripts consuming those properties must still be updated.
- Automatic generation still respects source ownership, template validity, and prefab editing restrictions.

## Unity verification

- [ ] Existing screen: generate, then generate again without changes. Second run must not rewrite/import the C# file.
- [ ] Add a button, generate, confirm both the C# accessor and serialized manifest entry update.
- [ ] Delete a button, generate, Undo, confirm Automatic mode regenerates the restored accessor.
- [ ] Modify generated C# externally, then attempt generation; verify that it blocks unless intentionally reviewed/approved.
- [ ] Duplicate a control with Ctrl+D and confirm existing naming and XML summaries remain intact.
- [ ] Run `AdvancedUIToolkitGenerationTransactionTests` in Unity Test Runner.
- [ ] Run complete EditMode/PlayMode suites and compile one player build before promoting the preview.

## Scope

Only Editor generation source, an internal recovery helper, tests, and documentation changed. Unity Runtime scripts and the serialized manifest schema are unchanged.
