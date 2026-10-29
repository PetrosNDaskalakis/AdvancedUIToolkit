# Advanced UI Toolkit v1.1.0 Preview 11 — Final foundation stabilization

[← Documentation Index](INDEX.md) · [Release gates](V1_1_RELEASE_GATES.md)

**Version:** `1.1.0-preview.11` · **Baseline:** `1.1.0-preview.10` · **Status:** static validation completed; Unity tests and player builds pending.

## Purpose

Preview 11 closes the planned preview sequence. It addresses a concrete native-control Undo inconsistency and originally added an integrated regression fixture (not distributed in the compact package). It introduces **no new authoring features or Runtime API changes**. A further preview is warranted only if Unity testing reveals a blocking defect.

## Correctness change

The native fallback path behind **GameObject → Advanced UI Toolkit → Button** (and other native uGUI controls) now starts an isolated Unity Undo group **before** creating the GameObject. Previously, it reused Unity's current group and could combine the new control with an unrelated preceding edit. Prefab-backed and ordinary-container creations already used separate Undo groups; all three paths now follow the same policy.

## Compact distribution

The original Preview 11 developer-only test fixtures were removed from the distributable package at the user’s request, together with the synthetic benchmark tools and Binder performance foldout. Production Runtime, Binder, and Factory functionality is unchanged. The Undo fix remains included.

## Required RC1 acceptance sequence

1. **Install:** import the complete Preview 11 package in a clean Unity 2022.3 LTS project; repeat in the actual production Unity version if different. Verify assembly import and both Editor tools.
2. **Editor workflows:** execute [manual verification](TEST_PLAN.md) with prefab creation, text overrides, Binder binding discovery, Undo/Redo, and scene save/reload.
3. **Play Mode:** validate enabled/disabled event callbacks, repeated instantiation and pooling in a representative scene.
4. **Real workflows:** manually create a panel with Header, Body, Buttons parent, Toggle parent, Footer; create three prefab buttons with different labels, generate source, run with Manual and Automatic modes, perform rename/delete/Undo/Redo and verify ownership-safe regeneration.
5. **Compatibility:** compile without TMP and, separately, with TMP installed. Verify the selected text target survives hierarchy reorder, rejects removed targets, and that prefab-instance overrides persist after scene save/reopen.
6. **Performance:** capture real Unity Profiler CPU and **GC Allocated In Frame** data for Binder scans, Library idle GUI, and Factory prefab creation on comparable setups. Synthetic diagnostics alone are insufficient.
7. **Player:** build an actual player for at least one supported target platform and confirm Editor assemblies aren't referenced in player code.
8. **Publication:** inspect package samples/docs/attribution and obtain any required organizational approval before publishing.

Record Unity version, OS, manual verification results, failures, profiler captures, project-specific migration issues, and a player build log in the release evidence checklist. **Do not tag v1.1.0 RC1 or Stable before the corresponding checks are passed.**

## Known limitations

- Text Override changes one serialized Text or optional TMP target per instance and does not resize the prefab or provide localization.
- Native control UI fallback uses the legacy Unity DefaultControls composition; it is intended as a backward-compatible fallback, not a new UI system.
- No Unity Editor executable is available in the archive-validation environment; Unity compilation and execution are not claimed.
