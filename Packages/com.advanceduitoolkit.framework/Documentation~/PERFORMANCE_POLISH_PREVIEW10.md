# Preview 10 — Factory text-target performance polish

> **Historical release note:** The compact Preview 11 package does not include the synthetic benchmark UI/scripts or bundled EditMode/PlayMode tests mentioned below. Use [Unity Profiler and manual verification](PERFORMANCE_AND_TESTING.md) instead.

[← Documentation Index](INDEX.md) · [Prefab Text Override](PREFAB_TEXT_OVERRIDE.md) · [Release gates](V1_1_RELEASE_GATES.md)

**Version:** `1.1.0-preview.10` · **Upgrade patch requires:** `1.1.0-preview.9` · **Status:** static checks only; Preview 10 Unity tests and performance measurements pending.

## What changed

**Creation flow:** `TryResolveSourceIndex` now traverses supported components directly, retaining source-component identity validation (no index fallback), including inactive descendants. `Apply` resolves only the selected instance component. Both avoid building selection popup labels, hierarchy path strings, or reading text values for all unrelated labels.

**Factory GUI:** when Displayed Text override is disabled, the Factory checks only whether an eligible text component exists before drawing the toggle. Full target discovery still runs when the user enables an override, so the popup and its current-text values are preserved.

**Performance diagnostics:** the existing opt-in Binder performance panel now measures text-target availability and source identity lookup for a hierarchy with a single text label after a configurable number of controls. Each metric uses the existing 5-sample median microsecond timing; Gen0 count is a coarse indicator, **not GC allocated bytes**. Run the same settings in Preview 9 and Preview 10 on the same Unity installation before reporting any speedup. Never compare warm-vs-cold Editor runs.

**No persistent cache:** there is no cross-repaint text target cache. This avoids invalidation pitfalls after prefab reimport, hierarchy mutations, Undo/Redo and domain reload. The optimized path still calls `GetComponentsInChildren<Component>(true)`, which returns an allocated array; **this release does not claim zero allocations**.

## Unity checks

1. Open the full Preview 10 package in Unity 2022.3 LTS and your production Editor. Run **all EditMode and PlayMode tests**, paying special attention to `AdvancedUIToolkitPrefabTextOverrideTests` and `AdvancedUIToolkitPerformanceRegressionTests`.
2. Open Factory for a prefab with zero labels, one root label, multiple nested legacy labels, inactive descendants and TMP where installed. Toggle Displayed Text on and off. Confirm no surprise target selection.
3. Reorder duplicate-named labels and delete the selected source label between authoring and creation. Reordering should keep identity, deletion must block creation without mutating another label.
4. Create instances from nested/variant prefabs. Undo/Redo must preserve prefab links and the selected instance-only text override.
5. In Binder's optional **Performance** view, capture matching reports in Preview 9 and Preview 10 with 32, 128 and 256 controls. Measure CPU time and GC allocated bytes separately in the Unity Profiler for real Factory GUI idle repaint and prefab creation with representative prefabs. There are **no supplied timings**.
6. Confirm the restored sample paths are visible in Package Manager, and check scene save/reload and a player build.

Only the package validator has been run outside Unity. User-reported Preview 9 passes are not evidence that Preview 10 is validated.
