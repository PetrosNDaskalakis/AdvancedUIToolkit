# Advanced UI Toolkit v1.1.0 RC1 — compact release candidate

[← Documentation Index](INDEX.md) · [Manual verification plan](TEST_PLAN.md) · [Release gates](V1_1_RELEASE_GATES.md)

**Package version:** `1.1.0-rc.1` · **Baseline:** `1.1.0-preview.11` **Compact** · **Status:** Release candidate, not Stable.

## Scope

This candidate freezes the existing Binder and UI Library/Factory workflows. It contains no new authoring features, no additional panels, no bundled EditMode/PlayMode suites, and no synthetic benchmark tooling.

**Production behavior is intentionally unchanged from Preview 11 Compact.** The only removed Editor source is `AdvancedUIToolkitAssemblyInfo.cs`, which granted an absent test assembly access to Editor internals; it was not involved in the production workflow. Runtime source files, generated code patterns, style profile formats, and retained Unity script GUIDs are unchanged.

## Supported workflow

- Binder: selected regular UI container and owning `UIBindingRoot`, typed C# generation, Manual/Automatic mode, XML summaries, and change safety.
- UI Library: Style Profiles, normal non-prefab containers as creation parents, prefab previews, and linked prefab creation.
- Prefab Text Override: one target per creation, legacy Unity Text and optional TMP; nested/inactive labels, explicit selection when multiple labels exist, and safe fail-closed target identity.
- Undo/Redo: isolated Factory creation actions and safe Binder rescan/regeneration.

## Installation and upgrade

**Full installation:** Extract the RC1 UPM ZIP with `com.advanceduitoolkit.framework/` as the top folder. Replace the prior embedded package while Unity is closed, or use Package Manager → Add package from disk and select the extracted `package.json` in a new project. Avoid duplicate package installations.

**Upgrade patch:** Apply **only** over Preview 11 Compact. Overwrite files and delete `Editor/AdvancedUIToolkitAssemblyInfo.cs` and its `.meta` file (the patch includes explicit cleanup instructions). For any older baseline, replace the entire package with the complete RC1 UPM ZIP after backing up. The exact package folder should contain one `package.json` showing `1.1.0-rc.1`.

## Manual verification before Stable

1. Open a clean Unity 2022.3+ project, verify the package imports without compiler errors and both Editor windows open.
2. Create a Canvas → Panel → Body → Buttons parent hierarchy. Create three prefab-backed buttons from one library, each with its own displayed text. Verify prefab assets remain unchanged and prefab instances are not selectable as authoring parents.
3. Generate bindings in Manual mode; check the accessor names, XML summaries, Preview C#, and no-op generation. Repeat a hierarchy edit under Automatic mode and undo/redo it.
4. Reorder or remove a prefab text label and verify the chosen target never silently changes.
5. Enter Play Mode and confirm event callbacks and disable/re-enable lifecycle behavior; save/reopen the scene and verify text overrides.
6. Try without TMP and with TMP when your team uses it. Import relevant sample scripts.
7. Profile representative authoring operations through **Window → Analysis → Profiler**; then compile and run at least one target-platform player.
8. Record actual results in [the evidence checklist](RC1_EVIDENCE_CHECKLIST.md). Resolve blocking issues before publishing v1.1.0 Stable.

## Verification status

- **Reported by the developer:** Preview 11 Compact works in their Unity project.
- **Static verification for RC1:** package structure, metadata, GUID preservation, assembly boundary, archive integrity, and production-source parity (reported with the downloadable artifact).
- **Not claimed:** RC1 clean install in Unity, all Unity version combinations, player-build success, Unity Profiler measurements, publication permissions, or Stable certification.

## Publication

No open-source license is bundled. Obtain required ownership/licensing approval before making the package public. See [Publication checklist](../PUBLICATION_CHECKLIST.md).

## Next phase

Once v1.1.0 Stable passes its release gates, begin Phase 3 with **v1.2 JSON UI Schemas**. Do not change the existing core architecture during RC1 sign-off.
