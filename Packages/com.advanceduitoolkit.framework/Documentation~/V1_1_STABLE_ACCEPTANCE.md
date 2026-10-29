# Advanced UI Toolkit v1.1.0 — Final acceptance checklist

[← Documentation Index](INDEX.md) · [Release notes](V1_1_STABLE_RELEASE_NOTES.md) · [Full release gates](V1_1_RELEASE_GATES.md)

This sheet captures **hands-on Unity verification**. No test/benchmark Editor panels are bundled with the compact package. Mark only checks actually performed, using **Unity Profiler** for profiling.

## Test environment

- Unity version and platform:
- Operating system:
- Package ZIP SHA-256:
- Target build platform:
- TMP installed (yes/no):
- Reviewer and date:

## Minimum release acceptance

- [ ] Clean UPM installation: `package.json` version **1.1.0**, no compiler or missing-script errors.
- [ ] Binder and UI Library open, fit compact/narrow dock layouts, and follow regular Canvas/Panel/Header/Body/Buttons parent selections.
- [ ] Prefab instances and prefab children **cannot** become authoring parents; ordinary `RectTransform` containers can.
- [ ] Create three linked prefabs (`BTN_Load`, `BTN_Arm`, `BTN_Fire`) and independently override the label on each. Source prefab remains unchanged.
- [ ] Save/reopen scene and confirm instance text, parent, prefab links, and bindings persist. Check optional TMP in a TMP-equipped project.
- [ ] Binder discovers controls, generates typed accessors, preserves XML summaries and handles rename/delete/Undo/Redo in Manual and Automatic modes.
- [ ] Generation no-op and unsafe-overwrite prevention behave correctly; inspect the resulting `.cs` source.
- [ ] Factory Undo/Redo restores each creation without combining it with an unrelated edit.
- [ ] Play Mode: registered UI events fire once; disable/re-enable does not duplicate callbacks.
- [ ] Import a representative sample and build/run at least one target-platform player with no Editor assembly included.
- [ ] Use **Unity Profiler** on representative scenes; capture noteworthy CPU/GC anomalies, if any.
- [ ] Review package ownership, licensing, and distribution permissions before any public release.

## Issues found

| ID | Reproduction / Unity Console text | Severity | Fixed and retested? |
| --- | --- | --- | --- |
| | | | |

## Decision

- [ ] **APPROVED:** Unity-side and publication-appropriate checks above completed; no release blockers.
- [ ] **BLOCKED:** one or more checks are missing or failed. Do not declare validated Stable or publish publicly.

Signed by:

Date:
