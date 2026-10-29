# Performance & verification — compact package

[← Documentation Index](INDEX.md) · [Manual test plan](TEST_PLAN.md)

Advanced UI Toolkit v1.1.0 RC1 compact distribution does not ship synthetic benchmark scripts, a performance foldout in Binder, or EditMode/PlayMode test assemblies. These are developer-only tools, not part of the everyday authoring workflow.

## Profile with Unity Profiler

1. Open **Window → Analysis → Profiler** in a representative Unity project.
2. Capture CPU Usage and GC Allocated In Frame while using Binder (scan and generation) and UI Library (idle, selection, and prefab creation).
3. Profile both a typical panel and a larger UI hierarchy under comparable conditions.
4. Compare recordings from the same Unity version, hardware, and project state; record any regressions.

Use Unity Profiler results for performance decisions rather than synthetic test numbers. This package does not claim measured improvements or zero allocations.

## Verify correctness manually

- Create ordinary containers, then instantiate text-overridden prefab buttons under them. Prefab instances must not become authoring parents.
- Confirm the original prefab assets remain unchanged and instance overrides persist after saving/reopening a scene.
- Generate bindings and verify Manual/Automatic modes, XML comments, rename/delete, and Undo/Redo.
- Enter Play Mode and verify event subscription, enable/disable behavior, and no duplicate callbacks.
- Compile without optional TMP and with TMP present where applicable; complete a player build.

See the [release checklist](RC1_EVIDENCE_CHECKLIST.md). Static package validation checks structure, not Unity Editor behavior.
