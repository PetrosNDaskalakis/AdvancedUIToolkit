# Preview 7 — Editor performance polish

> **Historical release note:** The compact Preview 11 package does not include the synthetic benchmark UI/scripts or bundled EditMode/PlayMode tests mentioned below. Use [Unity Profiler and manual verification](PERFORMANCE_AND_TESTING.md) instead.

[← README](../README.md) · [Foundation roadmap](FOUNDATION_ROADMAP.md) · [API reference](API_REFERENCE.md)

## Goal

Reduce unnecessary Editor allocations and preview memory usage without adding functionality or changing how Binder, UI Library, or source generation behaves.

## Implemented changes

| Area | Previous behavior | Preview 7 behavior |
| --- | --- | --- |
| Binder component list | Created a new list and one display object per binding every time a UI view requested the list | Retains one display snapshot until a scan, root change, or staged property/XML edit |
| Binder filtered rows | Allocated a new matching-row list each repaint and resolved the authoring selection repeatedly per row | Reuses a list buffer and resolves the authoring selection once per draw |
| UI Library profiles | Repeatedly traversed Style Profile inheritance for header, details, and gallery within one GUI event | Resolves effective templates once per GUI pass; next GUI pass always uses fresh data |
| Preview cache | Could retain unlimited 1024×512 previews until a global cache clear | At most 64 entries, with LRU eviction of finished previews; no pending entries are evicted |
| Preview captures | Queued as many as 48 captures | At most 16 captures queued at once, rendered incrementally |
| Preview CPU memory | Kept an additional readable pixel copy | Checks for blank captures, then uploads a non-readable full-resolution texture |
| Benchmarks | Synthetic sizes 16, 64, 128 controls | Synthetic sizes 32, 128, 256 controls |

**Memory clarification:** A 1024×512 RGBA32 texture is about 2 MiB before render overhead. A limit of 64 completed entries bounds that basic pixel storage around 128 MiB GPU-side. The actual Unity graphics-memory usage depends on pipeline and platform; this is not a measured Editor memory footprint. CPU-readable copies are released once captures are validated.

**Trade-off:** If a Library has more distinct prefab templates than the cache capacity, returning to an evicted item triggers another preview capture. The cached image resolution is unchanged.

## Regression cases

1. Repeated Binder display reads reuse the same snapshot while the hierarchy and staged metadata are unchanged.
2. Editing a property's XML summary invalidates the snapshot and the new text is immediately visible.
3. Adding a UI control and rescanning invalidates the previous snapshot.
4. Switching between separate UI roots cannot show stale bindings from the previous root.
5. The preview cache capacity remains bounded.

## Unity performance acceptance plan

Before installing Preview 7, record a baseline with Preview 6 if available. Then compare Preview 7 using the same Unity version, render pipeline, machine, scene and open windows:

- Typical prefabs: 25, 100 and 250 controls in a scene under one binding root.
- Style libraries: a handful of custom prefabs, then a larger library with 30–100 entries.
- Measure Editor repaint allocations, time spent drawing Binder and UI Library, Hierarchy rename/duplicate latency, preview memory, and first-preview/second-preview cost.
- Run the built-in synthetic reference/event benchmark with 32, 128, and 256 controls, but **do not** use those results alone to judge Editor drawing performance.
- Confirm that `BTN_Load`, `BTN_Arm`, and `BTN_Fire` generate the same bindings as before; that manual/automatic generation still works; and that custom XML summaries persist through a hierarchy change.
- Run EditMode, PlayMode and a player build. There should be no Runtime differences.

**Status:** Source and archive checks do not execute Unity. Record real Editor timings before asserting a performance improvement.
